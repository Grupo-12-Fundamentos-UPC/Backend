using System.Diagnostics;
using System.Text.Json;
using FluentValidation;
using HairyPaws.Application.Common.Exceptions;
using HairyPaws.Contracts.Common.Responses;

namespace HairyPaws.Api.Common.Middleware;

public sealed class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(context, exception);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        logger.LogError(exception, "Unhandled exception while processing request {Path}", context.Request.Path);

        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;
        var response = exception switch
        {
            ValidationException validationException => CreateValidationResponse(validationException, traceId),
            AppException appException => new ErrorResponse(appException.Code, appException.Message, appException.Details, traceId),
            UnauthorizedAccessException => new ErrorResponse("UNAUTHORIZED", "Authentication is required.", null, traceId),
            _ => new ErrorResponse("INTERNAL_SERVER_ERROR", "An unexpected error occurred.", null, traceId)
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = exception switch
        {
            ValidationException => StatusCodes.Status400BadRequest,
            AppException appException => appException.StatusCode,
            UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
            _ => StatusCodes.Status500InternalServerError
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }

    private static ErrorResponse CreateValidationResponse(ValidationException exception, string traceId)
    {
        var details = exception.Errors
            .GroupBy(static error => error.PropertyName)
            .ToDictionary(
                static group => group.Key,
                static group => group.Select(error => error.ErrorMessage).Distinct().ToArray());

        return new ErrorResponse(
            "VALIDATION_ERROR",
            "One or more validation errors occurred.",
            details,
            traceId);
    }
}
