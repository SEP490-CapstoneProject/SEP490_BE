using System.Text.Json;
namespace Challenge.API.Middleware;


/// <summary>
/// Global exception handling middleware
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var response = new ErrorResponse
        {
            Message = exception.Message,
            StatusCode = context.Response.StatusCode
        };

        return exception switch
        {
            InvalidOperationException => HandleInvalidOperationException(context, response),
            ArgumentException => HandleArgumentException(context, response),
            UnauthorizedAccessException => HandleUnauthorizedAccessException(context, response),
            _ => HandleGenericException(context, response)
        };
    }

    private static Task HandleInvalidOperationException(
        HttpContext context, ErrorResponse response)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        response.StatusCode = context.Response.StatusCode;
        return context.Response.WriteAsJsonAsync(response);
    }

    private static Task HandleArgumentException(
        HttpContext context, ErrorResponse response)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        response.StatusCode = context.Response.StatusCode;
        return context.Response.WriteAsJsonAsync(response);
    }

    private static Task HandleUnauthorizedAccessException(
        HttpContext context, ErrorResponse response)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        response.StatusCode = context.Response.StatusCode;
        response.Message = "Unauthorized";
        return context.Response.WriteAsJsonAsync(response);
    }

    private static Task HandleGenericException(
        HttpContext context, ErrorResponse response)
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        response.StatusCode = context.Response.StatusCode;
        response.Message = "An internal server error occurred";
        return context.Response.WriteAsJsonAsync(response);
    }
}

public class ErrorResponse
{
    public int StatusCode { get; set; }
    public string Message { get; set; }
}
