using System.Security.Authentication;
using System.Text.Json;
using System.IO;
using System.ComponentModel.DataAnnotations;
using System.Net.Http;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Data.Common;
using System.ComponentModel;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.EntityFrameworkCore;
using System.Security;
using Application.DTOs.Responses;
using Application.Services;
using Domain.Exceptions;

namespace WebAPI.Middlewares
{
    /// <summary>
    /// Middleware that catches unhandled exceptions and returns a standardized API response.
    /// </summary>
    public class ExceptionHandlingMiddleware : IMiddleware
    {
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;
        private readonly ILocalizationService _localizer;
        private static readonly Dictionary<Type, (int StatusCode, LogLevel Level)> _exceptionMap =
            new()
            {
                [typeof(BadRequestException)] = (StatusCodes.Status400BadRequest, LogLevel.Warning),
                [typeof(ValidationException)] = (StatusCodes.Status400BadRequest, LogLevel.Warning),
                [typeof(FormatException)] = (StatusCodes.Status400BadRequest, LogLevel.Warning),
                [typeof(JsonException)] = (StatusCodes.Status400BadRequest, LogLevel.Warning),
                [typeof(InvalidDataException)] = (StatusCodes.Status400BadRequest, LogLevel.Warning),
                [typeof(ArgumentException)] = (StatusCodes.Status400BadRequest, LogLevel.Warning),

                [typeof(NotFoundException)] = (StatusCodes.Status404NotFound, LogLevel.Warning),
                [typeof(KeyNotFoundException)] = (StatusCodes.Status404NotFound, LogLevel.Warning),
                [typeof(FileNotFoundException)] = (StatusCodes.Status404NotFound, LogLevel.Warning),
                [typeof(DirectoryNotFoundException)] = (StatusCodes.Status404NotFound, LogLevel.Warning),

                [typeof(AuthenticationException)] = (StatusCodes.Status401Unauthorized, LogLevel.Warning),
                [typeof(UnauthorizedAccessException)] = (StatusCodes.Status401Unauthorized, LogLevel.Warning),
                [typeof(SecurityException)] = (StatusCodes.Status403Forbidden, LogLevel.Warning),

                [typeof(DbUpdateConcurrencyException)] = (StatusCodes.Status409Conflict, LogLevel.Warning),
                [typeof(DbUpdateException)] = (StatusCodes.Status409Conflict, LogLevel.Error),

                [typeof(NotSupportedException)] = (StatusCodes.Status501NotImplemented, LogLevel.Warning),
                [typeof(NotImplementedException)] = (StatusCodes.Status501NotImplemented, LogLevel.Warning),

                [typeof(HttpRequestException)] = (StatusCodes.Status503ServiceUnavailable, LogLevel.Error),
                [typeof(SocketException)] = (StatusCodes.Status503ServiceUnavailable, LogLevel.Error),
                [typeof(IOException)] = (StatusCodes.Status503ServiceUnavailable, LogLevel.Error),
                [typeof(OperationCanceledException)] = (StatusCodes.Status503ServiceUnavailable, LogLevel.Warning),

                [typeof(TimeoutException)] = (StatusCodes.Status504GatewayTimeout, LogLevel.Error),
                [typeof(TaskCanceledException)] = (StatusCodes.Status504GatewayTimeout, LogLevel.Error),

                [typeof(InvalidOperationException)] = (StatusCodes.Status500InternalServerError, LogLevel.Error),
                [typeof(NullReferenceException)] = (StatusCodes.Status500InternalServerError, LogLevel.Error),
                [typeof(ObjectDisposedException)] = (StatusCodes.Status500InternalServerError, LogLevel.Error),
                [typeof(OverflowException)] = (StatusCodes.Status500InternalServerError, LogLevel.Error),
                [typeof(AccessViolationException)] = (StatusCodes.Status500InternalServerError, LogLevel.Error),
                [typeof(OutOfMemoryException)] = (StatusCodes.Status500InternalServerError, LogLevel.Critical),
                [typeof(ApplicationException)] = (StatusCodes.Status500InternalServerError, LogLevel.Error),
                [typeof(ArgumentNullException)] = (StatusCodes.Status400BadRequest, LogLevel.Warning),
                [typeof(ArgumentOutOfRangeException)] = (StatusCodes.Status400BadRequest, LogLevel.Warning),
                [typeof(IndexOutOfRangeException)] = (StatusCodes.Status500InternalServerError, LogLevel.Error),
                [typeof(InvalidCastException)] = (StatusCodes.Status400BadRequest, LogLevel.Warning),
                [typeof(DivideByZeroException)] = (StatusCodes.Status500InternalServerError, LogLevel.Error),
                [typeof(StackOverflowException)] = (StatusCodes.Status500InternalServerError, LogLevel.Critical),
                [typeof(InsufficientMemoryException)] = (StatusCodes.Status500InternalServerError, LogLevel.Critical),
                [typeof(ThreadAbortException)] = (StatusCodes.Status500InternalServerError, LogLevel.Critical),
                [typeof(ThreadInterruptedException)] = (StatusCodes.Status500InternalServerError, LogLevel.Error),
                [typeof(ThreadStateException)] = (StatusCodes.Status500InternalServerError, LogLevel.Error),
                [typeof(UriFormatException)] = (StatusCodes.Status400BadRequest, LogLevel.Warning),
                [typeof(PathTooLongException)] = (StatusCodes.Status400BadRequest, LogLevel.Warning),
                [typeof(FileLoadException)] = (StatusCodes.Status500InternalServerError, LogLevel.Error),
                [typeof(WebException)] = (StatusCodes.Status503ServiceUnavailable, LogLevel.Error),
                [typeof(DbException)] = (StatusCodes.Status503ServiceUnavailable, LogLevel.Error),
                [typeof(Win32Exception)] = (StatusCodes.Status500InternalServerError, LogLevel.Error),
                [typeof(ExternalException)] = (StatusCodes.Status500InternalServerError, LogLevel.Error),
                [typeof(TargetInvocationException)] = (StatusCodes.Status500InternalServerError, LogLevel.Error),
                [typeof(SystemException)] = (StatusCodes.Status500InternalServerError, LogLevel.Error)
            };

        public ExceptionHandlingMiddleware(ILogger<ExceptionHandlingMiddleware> logger, ILocalizationService localizer)
        {
            _logger = logger;
            _localizer = localizer;
        }

        private static async Task WriteResponseAsync(HttpContext context, int statusCode, string message, IEnumerable<string>? errors = null)
        {
            if (context.Response.HasStarted)
            {
                return;
            }

            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";
            var response = new ApiResponse<string>(statusCode, message, null, errors);
            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            var json = JsonSerializer.Serialize(response, options);
            await context.Response.WriteAsync(json);
        }

        private (int StatusCode, string Message, LogLevel Level, IEnumerable<string>? Errors) MapException(Exception ex)
        {
            if (ex is AggregateException agg && agg.InnerException is not null)
            {
                return MapException(agg.InnerException);
            }

            var type = ex.GetType();
            foreach (var kvp in _exceptionMap)
            {
                if (kvp.Key.IsAssignableFrom(type))
                {
                    var message = kvp.Key == typeof(UnauthorizedAccessException) || kvp.Key == typeof(AuthenticationException)
                        ? _localizer["Unauthorized"]
                        : ex.Message;
                    return (kvp.Value.StatusCode, message, kvp.Value.Level, null);
                }
            }

            return (StatusCodes.Status500InternalServerError, _localizer["UnexpectedError"], LogLevel.Error, new[] { ex.Message });
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            try
            {
                await next(context);
            }
            catch (OperationCanceledException ex) when (context.RequestAborted.IsCancellationRequested)
            {
                _logger.LogInformation(ex, "Request was cancelled by the client");
            }
            catch (Exception ex)
            {
                var (statusCode, message, level, errors) = MapException(ex);
                _logger.Log(level, ex, "Handled exception");
                await WriteResponseAsync(context, statusCode, message, errors);
            }
        }
    }
}
