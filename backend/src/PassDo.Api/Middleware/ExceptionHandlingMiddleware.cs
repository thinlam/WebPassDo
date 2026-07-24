using System.Net;
using System.Text.Json;
using PassDo.Application.Common.Exceptions;
using PassDo.Application.Common.Models;

namespace PassDo.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, response) = exception switch
        {
            ValidationException validationException => (
                HttpStatusCode.BadRequest,
                ApiResponse<object>.Fail(validationException.Message, validationException.Errors)),

            NotFoundException notFoundException => (
                HttpStatusCode.NotFound,
                ApiResponse<object>.Fail(notFoundException.Message)),

            UnauthorizedException unauthorizedException => (
                HttpStatusCode.Unauthorized,
                ApiResponse<object>.Fail(unauthorizedException.Message)),

            ForbiddenException forbiddenException => (
                HttpStatusCode.Forbidden,
                ApiResponse<object>.Fail(forbiddenException.Message)),

            ConflictException conflictException => (
                HttpStatusCode.Conflict,
                ApiResponse<object>.Fail(conflictException.Message)),

            _ => (
                HttpStatusCode.InternalServerError,
                ApiResponse<object>.Fail(
                    _environment.IsDevelopment()
                        ? exception.Message
                        : "An unexpected error occurred."))
        };

        if (statusCode == HttpStatusCode.InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception");
        }
        else
        {
            _logger.LogWarning(exception, "Handled exception: {Message}", exception.Message);
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, jsonOptions));
    }
}
