using TronderLeikan.Domain.Games;

namespace TronderLeikan.Application.Games.Responses;

// Kortform av et spill til lister, uten deltakere og plasseringer
public record GameSummaryResponse(
    Guid Id,
    Guid TournamentId,
    string Name,
    string? Location,
    bool IsDone,
    GameType GameType);
