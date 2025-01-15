﻿using LumiSky.Core.Primitives;

namespace LumiSky.Core.DomainEvents;

public record NewFocusEvent
{
    public required string Filename { get; init; }
    public required Size Size { get; init; }
}
