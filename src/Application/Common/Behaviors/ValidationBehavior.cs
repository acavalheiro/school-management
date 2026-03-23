using Application.Common.Mediator;
using Domain.Common;
using FluentValidation;

namespace Application.Common.Behaviors;

public sealed class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators)
    : IRequestHandler<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    // This is a decorator — wraps around the inner handler.
    // Registration wires this up in front of the real handler.
    private IRequestHandler<TRequest, TResponse>? _inner;

    public void SetInner(IRequestHandler<TRequest, TResponse> inner) => _inner = inner;

    public async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken)
    {
        if (!validators.Any())
            return await _inner!.Handle(request, cancellationToken);

        var context = new ValidationContext<TRequest>(request);
        var failures = validators
            .Select(v => v.Validate(context))
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count != 0)
        {
            var errors = failures
                .Select(f => new { f.PropertyName, f.ErrorMessage })
                .ToList();

            // Build a generic validation error response
            // The caller is responsible for checking Result.IsFailure
            var errorMessage = string.Join("; ", errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}"));
            var error = new Error("Validation.Failed", errorMessage);

            // Attempt to cast to a Result-based response
            if (typeof(TResponse) == typeof(Result))
                return (TResponse)(object)Result.Failure(error);

            var resultType = typeof(TResponse);
            if (resultType.IsGenericType && resultType.GetGenericTypeDefinition() == typeof(Result<>))
            {
                var innerType = resultType.GetGenericArguments()[0];
                var failureMethod = typeof(Result<>)
                    .MakeGenericType(innerType)
                    .GetMethod(nameof(Result<object>.Failure))!;
                return (TResponse)failureMethod.Invoke(null, [error])!;
            }

            throw new ValidationException(failures);
        }

        return await _inner!.Handle(request, cancellationToken);
    }
}
