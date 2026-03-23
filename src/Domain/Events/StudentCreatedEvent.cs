using Domain.Common;

namespace Domain.Events;

public record StudentCreatedEvent(Guid StudentId) : IDomainEvent;
