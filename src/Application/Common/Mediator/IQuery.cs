using Domain.Common;

namespace Application.Common.Mediator;

public interface IQuery<TResponse> : IRequest<Result<TResponse>>;
