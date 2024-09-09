namespace OdinEye.Core.DomainEvents;

public abstract record GenerationEvent
{
    public required int Id { get; init; }
}

public record GenerationQueued : GenerationEvent { }

public record GenerationStarting : GenerationEvent { }

public record GenerationProgress : GenerationEvent { }

public record GenerationComplete : GenerationEvent { }