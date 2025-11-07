using System.Linq;
using System.Reflection;
using Application.DTOs.Dashboard;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;

namespace Infrastructure.Services;

/// <summary>
/// Service for discovering and listing all API endpoints in the system
/// </summary>
public class DashboardService : IDashboardService
{
    private readonly IActionDescriptorCollectionProvider _actionDescriptorCollectionProvider;
    private readonly EndpointDataSource _endpointDataSource;

    public DashboardService(
        IActionDescriptorCollectionProvider actionDescriptorCollectionProvider,
        EndpointDataSource endpointDataSource)
    {
        _actionDescriptorCollectionProvider = actionDescriptorCollectionProvider;
        _endpointDataSource = endpointDataSource;
    }

    public Task<DashboardResponseDto> GetAllEndpointsAsync()
    {
        var endpoints = new List<EndpointInfoDto>();
        var endpointsByController = new Dictionary<string, List<EndpointInfoDto>>();

        // Create a dictionary of route patterns from endpoints for lookup
        var routePatterns = new Dictionary<string, string>();
        foreach (var endpoint in _endpointDataSource.Endpoints)
        {
            if (endpoint is RouteEndpoint routeEndpoint)
            {
                var actionDescriptor = endpoint.Metadata.GetMetadata<ControllerActionDescriptor>();
                if (actionDescriptor != null)
                {
                    var key = $"{actionDescriptor.ControllerName}.{actionDescriptor.ActionName}";
                    routePatterns[key] = routeEndpoint.RoutePattern.RawText ?? routeEndpoint.RoutePattern.ToString();
                }
            }
        }

        // Process action descriptors
        foreach (var actionDescriptor in _actionDescriptorCollectionProvider.ActionDescriptors.Items)
        {
            // Skip if not a controller action
            if (actionDescriptor is not ControllerActionDescriptor controllerAction)
                continue;

            // Skip if not an API controller
            if (!controllerAction.ControllerTypeInfo.IsDefined(typeof(ApiControllerAttribute), false))
                continue;

            var key = $"{controllerAction.ControllerName}.{controllerAction.ActionName}";
            var routePattern = routePatterns.GetValueOrDefault(key);

            var endpointInfo = ExtractEndpointInfo(controllerAction, routePattern);
            endpoints.Add(endpointInfo);

            // Group by controller
            if (!endpointsByController.ContainsKey(endpointInfo.Controller))
            {
                endpointsByController[endpointInfo.Controller] = new List<EndpointInfoDto>();
            }
            endpointsByController[endpointInfo.Controller].Add(endpointInfo);
        }

        // Sort endpoints within each controller by route
        foreach (var controllerGroup in endpointsByController.Values)
        {
            controllerGroup.Sort((a, b) => string.Compare(a.Route, b.Route, StringComparison.OrdinalIgnoreCase));
        }

        // Sort all endpoints
        endpoints.Sort((a, b) =>
        {
            var controllerCompare = string.Compare(a.Controller, b.Controller, StringComparison.OrdinalIgnoreCase);
            if (controllerCompare != 0) return controllerCompare;
            return string.Compare(a.Route, b.Route, StringComparison.OrdinalIgnoreCase);
        });

        var response = new DashboardResponseDto
        {
            TotalEndpoints = endpoints.Count,
            EndpointsByController = endpointsByController,
            AllEndpoints = endpoints
        };

        return Task.FromResult(response);
    }

    private EndpointInfoDto ExtractEndpointInfo(ControllerActionDescriptor actionDescriptor, string? routePattern = null)
    {
        var endpointInfo = new EndpointInfoDto
        {
            Controller = actionDescriptor.ControllerName,
            Action = actionDescriptor.ActionName
        };

        // Extract HTTP method from action method attributes
        var methodInfo = actionDescriptor.MethodInfo;
        if (methodInfo.IsDefined(typeof(HttpGetAttribute), false))
            endpointInfo.Method = "GET";
        else if (methodInfo.IsDefined(typeof(HttpPostAttribute), false))
            endpointInfo.Method = "POST";
        else if (methodInfo.IsDefined(typeof(HttpPutAttribute), false))
            endpointInfo.Method = "PUT";
        else if (methodInfo.IsDefined(typeof(HttpDeleteAttribute), false))
            endpointInfo.Method = "DELETE";
        else if (methodInfo.IsDefined(typeof(HttpPatchAttribute), false))
            endpointInfo.Method = "PATCH";
        else
            endpointInfo.Method = "GET"; // Default

        // Extract route
        if (!string.IsNullOrEmpty(routePattern))
        {
            endpointInfo.Route = routePattern;
        }
        else
        {
            // Fallback: construct route from attributes
            var routeAttribute = actionDescriptor.ControllerTypeInfo.GetCustomAttribute<RouteAttribute>();
            var actionRouteAttribute = actionDescriptor.MethodInfo.GetCustomAttribute<RouteAttribute>();
            
            var controllerRoute = routeAttribute?.Template ?? $"api/{actionDescriptor.ControllerName.ToLower()}";
            var actionRoute = actionRouteAttribute?.Template ?? string.Empty;

            // Build full route
            if (!string.IsNullOrEmpty(actionRoute))
            {
                if (actionRoute.StartsWith("/"))
                    endpointInfo.Route = actionRoute;
                else if (controllerRoute.EndsWith("/"))
                    endpointInfo.Route = $"{controllerRoute}{actionRoute}";
                else
                    endpointInfo.Route = $"{controllerRoute}/{actionRoute}";
            }
            else
            {
                endpointInfo.Route = controllerRoute;
            }
        }

        // Ensure route starts with /
        if (!string.IsNullOrEmpty(endpointInfo.Route) && !endpointInfo.Route.StartsWith("/"))
            endpointInfo.Route = $"/{endpointInfo.Route}";

        // Extract authorization info
        var authorizeAttribute = actionDescriptor.MethodInfo.GetCustomAttribute<AuthorizeAttribute>();
        var allowAnonymousAttribute = actionDescriptor.MethodInfo.GetCustomAttribute<AllowAnonymousAttribute>();
        var controllerAuthorize = actionDescriptor.ControllerTypeInfo.GetCustomAttribute<AuthorizeAttribute>();

        if (allowAnonymousAttribute != null)
        {
            endpointInfo.AllowAnonymous = true;
            endpointInfo.AuthorizationPolicy = null;
        }
        else if (authorizeAttribute != null)
        {
            endpointInfo.AllowAnonymous = false;
            endpointInfo.AuthorizationPolicy = authorizeAttribute.Policy ?? "Authorize";
        }
        else if (controllerAuthorize != null)
        {
            endpointInfo.AllowAnonymous = false;
            endpointInfo.AuthorizationPolicy = controllerAuthorize.Policy ?? "Authorize";
        }
        else
        {
            endpointInfo.AllowAnonymous = true;
            endpointInfo.AuthorizationPolicy = null;
        }

        // Extract XML documentation summary from Summary attribute or XML comments
        var summaryAttribute = actionDescriptor.MethodInfo.GetCustomAttribute<System.ComponentModel.DescriptionAttribute>();
        endpointInfo.Summary = summaryAttribute?.Description;

        // Extract parameters
        var methodParameters = actionDescriptor.MethodInfo.GetParameters();
        foreach (var parameter in actionDescriptor.Parameters)
        {
            var methodParam = methodParameters.FirstOrDefault(p => p.Name == parameter.Name);
            
            var paramInfo = new ParameterInfoDto
            {
                Name = parameter.Name ?? "unknown",
                Type = parameter.ParameterType.Name,
                IsOptional = methodParam?.IsOptional ?? false
            };

            // Determine parameter source
            if (parameter.BindingInfo?.BindingSource != null)
            {
                paramInfo.Source = parameter.BindingInfo.BindingSource.DisplayName;
            }
            else if (methodParam != null)
            {
                // Check attributes on the method parameter
                if (methodParam.GetCustomAttribute<FromRouteAttribute>() != null)
                    paramInfo.Source = "Route";
                else if (methodParam.GetCustomAttribute<FromQueryAttribute>() != null)
                    paramInfo.Source = "Query";
                else if (methodParam.GetCustomAttribute<FromBodyAttribute>() != null)
                    paramInfo.Source = "Body";
                else if (methodParam.GetCustomAttribute<FromFormAttribute>() != null)
                    paramInfo.Source = "Form";
                else if (methodParam.GetCustomAttribute<FromHeaderAttribute>() != null)
                    paramInfo.Source = "Header";
                else
                    paramInfo.Source = "ModelBinding";
            }
            else
            {
                paramInfo.Source = "ModelBinding";
            }

            endpointInfo.Parameters.Add(paramInfo);
        }

        return endpointInfo;
    }
}


