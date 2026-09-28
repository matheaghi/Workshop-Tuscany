namespace TronderLeikan.Domain.Tournaments;

// Hva en poengpost ble gitt for. Rekkefølgen er den postene vises i: deltakelse, pall, arrangering, tilskuer.
public enum PointReason
{
    Participation,
    FirstPlace,
    SecondPlace,
    ThirdPlace,
    OrganizedWithParticipation,
    OrganizedWithoutParticipation,
    Spectator
}

// Én post i forklaringen av en persons poeng i et spill
public sealed record PointLine(PointReason Reason, int Points);
