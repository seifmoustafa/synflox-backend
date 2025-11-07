using System.Net;
using System.Text;

namespace WebAPI.Middlewares;

/// <summary>
/// Middleware to handle non-ASCII characters in HTTP headers, particularly for Arabic file names
/// </summary>
public class UnicodeHeaderMiddleware : IMiddleware
{
    private readonly ILogger<UnicodeHeaderMiddleware> _logger;

    public UnicodeHeaderMiddleware(ILogger<UnicodeHeaderMiddleware> logger)
    {
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            _logger.LogInformation("UnicodeHeaderMiddleware: Processing request {Method} {Path}",
                context.Request.Method, context.Request.Path);

            // Check if this is an upload completion request
            if (context.Request.Path.StartsWithSegments("/api/uploads") &&
                context.Request.Method == "POST" &&
                context.Request.Path.Value?.Contains("/complete") == true)
            {
                _logger.LogInformation("UnicodeHeaderMiddleware: Detected upload completion request");

                // Check headers first for non-ASCII characters and encode them
                var headersToModify = new Dictionary<string, string>();
                foreach (var header in context.Request.Headers)
                {
                    if (ContainsNonAscii(header.Key) || header.Value.Any(v => ContainsNonAscii(v)))
                    {
                        _logger.LogWarning("UnicodeHeaderMiddleware: Found non-ASCII characters in header {HeaderName}: {HeaderValue}",
                            header.Key, string.Join(", ", header.Value));

                        // Encode the header value
                        var encodedValues = header.Value.Select(v => WebUtility.UrlEncode(v)).ToArray();
                        headersToModify[header.Key] = string.Join(", ", encodedValues);
                    }
                }

                // Apply header modifications
                foreach (var kvp in headersToModify)
                {
                    context.Request.Headers[kvp.Key] = kvp.Value;
                    _logger.LogInformation("UnicodeHeaderMiddleware: Encoded header {HeaderName}: {EncodedValue}", kvp.Key, kvp.Value);
                }

                // Read the request body to get the file name
                context.Request.EnableBuffering();
                var originalBodyStream = context.Request.Body;

                using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
                var body = await reader.ReadToEndAsync();

                _logger.LogInformation("UnicodeHeaderMiddleware: Request body: {Body}", body);

                if (!string.IsNullOrEmpty(body))
                {
                    try
                    {
                        // Parse JSON to get the file name
                        using var doc = System.Text.Json.JsonDocument.Parse(body);
                        if (doc.RootElement.TryGetProperty("fileName", out var fileNameElement))
                        {
                            var fileName = fileNameElement.GetString();
                            _logger.LogInformation("UnicodeHeaderMiddleware: Found fileName: {FileName}", fileName);

                            if (!string.IsNullOrEmpty(fileName) && ContainsNonAscii(fileName))
                            {
                                _logger.LogInformation("UnicodeHeaderMiddleware: Detected non-ASCII file name: {FileName}", fileName);

                                // URL encode the file name
                                var encodedFileName = WebUtility.UrlEncode(fileName);

                                // Update the JSON with encoded file name - use safe JSON parsing
                                string updatedBody;
                                try
                                {
                                    var jsonOptions = new System.Text.Json.JsonSerializerOptions 
                                    { 
                                        WriteIndented = false,
                                        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                                    };
                                    var dict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object?>>(body, jsonOptions);
                                    if (dict != null && dict.ContainsKey("fileName"))
                                    {
                                        dict["fileName"] = encodedFileName;
                                        updatedBody = System.Text.Json.JsonSerializer.Serialize(dict, jsonOptions);
                                    }
                                    else
                                    {
                                        updatedBody = body;
                                    }
                                }
                                catch
                                {
                                    // Fallback to string replacement if JSON parsing fails
                                    var index = body.IndexOf($"\"fileName\":\"", StringComparison.Ordinal);
                                    if (index >= 0)
                                    {
                                        var startIndex = index + "\"fileName\":\"".Length;
                                        var endIndex = body.IndexOf("\"", startIndex, StringComparison.Ordinal);
                                        if (endIndex > startIndex)
                                        {
                                            updatedBody = body.Substring(0, startIndex) + 
                                                        encodedFileName + 
                                                        body.Substring(endIndex);
                                        }
                                        else
                                        {
                                            updatedBody = body;
                                        }
                                    }
                                    else
                                    {
                                        updatedBody = body;
                                    }
                                }

                                // Create new request body stream
                                var newBodyBytes = Encoding.UTF8.GetBytes(updatedBody);
                                context.Request.Body = new MemoryStream(newBodyBytes);
                                context.Request.ContentLength = newBodyBytes.Length;

                                _logger.LogInformation("UnicodeHeaderMiddleware: Encoded file name: {OriginalFileName} -> {EncodedFileName}", fileName, encodedFileName);
                            }
                        }
                        else
                        {
                            _logger.LogWarning("UnicodeHeaderMiddleware: No fileName property found in request body");
                        }
                    }
                    catch (System.Text.Json.JsonException ex)
                    {
                        _logger.LogWarning(ex, "UnicodeHeaderMiddleware: Failed to parse JSON body for file name encoding");
                    }
                }

                // Reset the stream position
                context.Request.Body.Position = 0;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UnicodeHeaderMiddleware: Error in UnicodeHeaderMiddleware");
        }

        await next(context);
    }

    private static bool ContainsNonAscii(string input)
    {
        return input.Any(c => c > 127);
    }
}
