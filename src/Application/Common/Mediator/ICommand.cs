using Domain.Common;

namespace Application.Common.Mediator;

public interface ICommand : IRequest<Result>;

public interface ICommand<TResponse> : IRequest<Result<TResponse>>;
