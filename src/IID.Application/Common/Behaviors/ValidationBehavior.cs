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

        // Return Result.Failure(ValidationFailed) for Result types instead of throwing.
        var message = string.Join("; ", failures.Select(f => f.ErrorMessage));
        return CreateValidationFailure(message);
    }

    /// <summary>
    /// Builds a typed Result.Failure(ValidationFailed) for TResponse.
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
            var failureMethod = responseType.GetMethod(nameof(Result.Failure), [typeof(ErrorKind), typeof(string)]);
            if (failureMethod is not null)
            {
                var failure = failureMethod.Invoke(null, [ErrorKind.ValidationFailed, message])!;
                return (TResponse)failure;
            }
        }

        // Fall back to throwing if response is not a Result type.
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
        // Scan and register all IValidator<T> implementations.
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
