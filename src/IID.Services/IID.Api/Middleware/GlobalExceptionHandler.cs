using IID.Application.Common.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace IID.Api.Middleware;

/// <summary>
/// Global exception handler middleware that translates unhandled exceptions into standard RFC 7807 ProblemDetails responses.
/// </summary>
public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (status, title, detail, errors) = exception switch
        {
            ValidationException validation => (
                StatusCodes.Status400BadRequest,
                "Validation failed",
                validation.Message,
                (IDictionary<string, string[]>?)validation.Errors),
            FluentValidation.ValidationException fluentValidation => (
                StatusCodes.Status400BadRequest,
                "Validation failed",
                "One or more validation failures occurred.",
                (IDictionary<string, string[]>?)fluentValidation.Errors
                    .GroupBy(e => e.PropertyName, e => e.ErrorMessage)
                    .ToDictionary(g => g.Key, g => g.ToArray())),
            NotFoundException notFound => (
                StatusCodes.Status404NotFound,
                "Resource not found",
                notFound.Message,
                null),
            ConflictException conflict => (
                StatusCodes.Status409Conflict,
                "Conflict",
                conflict.Message,
                null),
            InvalidOperationException invalidOp => (
                StatusCodes.Status400BadRequest,
                "Invalid operation",
                invalidOp.Message,
                null),
            ArgumentException arg => (
                StatusCodes.Status400BadRequest,
                "Invalid argument",
                arg.Message,
                null),
            _ => (
                StatusCodes.Status500InternalServerError,
                "Server error",
                "An unexpected error occurred.",
                null)
        };

        if (status >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception occurred while processing request to {Path}", httpContext.Request.Path);
        }
        else
        {
            logger.LogWarning(exception, "Handled exception ({Title}) while processing request to {Path}", title, httpContext.Request.Path);
        }

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path
        };

        if (errors is not null)
        {
            problem.Extensions["errors"] = errors;
        }

        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }
}
