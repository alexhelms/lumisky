namespace OdinEye.Core.DomainEvents;

public abstract record DayNightEvent { }

public record BecomingDaytimeEvent : DayNightEvent { }

public record BecomingNighttimeEvent : DayNightEvent { }
