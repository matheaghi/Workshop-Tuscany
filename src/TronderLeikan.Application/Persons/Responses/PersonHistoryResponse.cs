using TronderLeikan.Domain.Tournaments;

namespace TronderLeikan.Application.Persons.Responses;

// En persons historikk: alle ferdige spill de har vært med i, gruppert per turnering og sortert etter dato.
// Rekkene regnes over alle spill: deltakerrekken er 0 når personen ikke var med i siste spill,
// og seiersrekken er null når personen ikke vant siste spill.
public record PersonHistoryResponse(
    Guid PersonId,
    string FirstName,
    string LastName,
    RoleSummaryResponse RoleSummary,
    IReadOnlyList<TournamentHistoryResponse> Tournaments,
    int CurrentParticipationStreak,
    int? CurrentWinStreak);

// Antall spill personen har hatt hver rolle i. Arrangør som spiller teller både som deltaker og arrangør.
public record RoleSummaryResponse(int Participated, int Organized, int Spectated);

public record TournamentHistoryResponse(
    Guid TournamentId,
    string Name,
    string Slug,
    DateOnly? FirstPlayedOn,
    DateOnly? LastPlayedOn,
    int TotalPoints,
    int Rank,
    IReadOnlyList<PointSummaryResponse> PointsSummary,
    IReadOnlyList<GameHistoryResponse> Games);

// Sammenlagt per grunn i turneringen: hvor mange ganger personen fikk posten og hvor mange poeng det ga
public record PointSummaryResponse(PointReason Reason, int Count, int Points);

public record GameHistoryResponse(
    Guid GameId,
    string Name,
    DateOnly? PlayedOn,
    IReadOnlyList<GameRole> Roles,
    int? Placement,
    GamePointsResponse Points);

public record GamePointsResponse(
    int Participation,
    int Placement,
    int Organizing,
    int Spectating,
    int Total,
    IReadOnlyList<PointLineResponse> Lines);

// Én post i forklaringen av poengene i et spill
public record PointLineResponse(PointReason Reason, int Points);

public enum GameRole
{
    Participant,
    Organizer,
    Spectator
}
