using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
namespace IID.Application.Common.Behaviors;

public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        if (!validators.Any()) return await next(ct);

        var ctx = new ValidationContext<TRequest>(request);
        var results = await Task.WhenAll(validators.Select(v => v.ValidateAsync(ctx, ct)));
        var failures = results.SelectMany(r => r.Errors).Where(f => f is not null).ToList();
        if (failures.Count == 0) return await next(ct);

        // Convert FluentValidation failures into Result.Failure(ValidationFailed) so the
        // existing ResultMapper pipeline returns a clean 422 with the API's standard
        // error envelope — no thrown exception leaks out as a 500.
        var message = string.Join("; ", failures.Select(f => $"{f.PropertyName}: {f.ErrorMessage}"));
        return CreateValidationFailure(message);
    }

    /// <summary>
    /// Builds a typed <c>Result.Failure(ValidationFailed)</c> via reflection because
    /// <c>TResponse</c> is unknown at compile time. Only handles the <c>Result</c> /
    /// <c>Result&lt;T&gt;</c> shapes this codebase actually returns from handlers.
    /// </summary>
    private static TResponse CreateValidationFailure(string message)
    {
        var responseType = typeof(TResponse);

        if (responseType == typeof(Result))
        {
            return (TResponse)(object)Result.Failure(ErrorKind.ValidationFailed, message);
        }

        if (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(Result<>))
        {
            var valueType = responseType.GetGenericArguments()[0];
            var failureMethod = typeof(Result)
                .GetMethods()
                .First(m => m.Name == nameof(Result.Failure) && m.IsGenericMethodDefinition && m.GetParameters().Length == 2)
                .MakeGenericMethod(valueType);
            var failure = failureMethod.Invoke(null, new object[] { ErrorKind.ValidationFailed, message })!;
            return (TResponse)failure;
        }

        // Non-Result response type: fall back to throwing. The endpoint-level ResultMapper
        // path is preferred, so this only fires for endpoints that bypass Result.
        throw new ValidationException(message, new List<ValidationFailure>
        {
            new(string.Empty, message)
        });
    }
}

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddIidApplication(this IServiceCollection services)
    {
        var assembly = typeof(ApplicationServiceCollectionExtensions).Assembly;
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        // Manually register every validator so we don't depend on FluentValidation DI extensions
        var validatorType = typeof(IValidator<>);
        foreach (var t in assembly.GetTypes())
        {
            foreach (var iface in t.GetInterfaces())
            {
                if (iface.IsGenericType && iface.GetGenericTypeDefinition() == validatorType)
                    services.AddTransient(iface, t);
            }
        }
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        return services;
    }
}
