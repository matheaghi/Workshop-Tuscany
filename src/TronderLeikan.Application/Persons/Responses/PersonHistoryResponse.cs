namespace TronderLeikan.Application.Persons.Responses;

// En persons historikk: alle ferdige spill de har vært med i, gruppert per turnering og sortert etter dato
public record PersonHistoryResponse(
    Guid PersonId,
    string FirstName,
    string LastName,
    RoleSummaryResponse RoleSummary,
    IReadOnlyList<TournamentHistoryResponse> Tournaments);

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
    IReadOnlyList<GameHistoryResponse> Games);

public record GameHistoryResponse(
    Guid GameId,
    string Name,
    DateOnly? PlayedOn,
    IReadOnlyList<GameRole> Roles,
    int? Placement,
    GamePointsResponse Points);

public record GamePointsResponse(int Participation, int Placement, int Organizing, int Spectating, int Total);

public enum GameRole
{
    Participant,
    Organizer,
    Spectator
}
