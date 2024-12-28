namespace LumiSky.Core.DomainEvents;

public abstract record DayNightEvent { }

public record NightToDayEvent : DayNightEvent { }

public record DayToNightEvent : DayNightEvent { }
