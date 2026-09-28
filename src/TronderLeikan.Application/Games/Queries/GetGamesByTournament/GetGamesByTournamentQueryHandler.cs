using Microsoft.EntityFrameworkCore;
using TronderLeikan.Application.Common.Errors;
using TronderLeikan.Application.Common.Interfaces;
using TronderLeikan.Application.Common.Results;
using TronderLeikan.Application.Games.Responses;

namespace TronderLeikan.Application.Games.Queries.GetGamesByTournament;

public sealed class GetGamesByTournamentQueryHandler(IAppDbContext db)
    : IQueryHandler<GetGamesByTournamentQuery, GameSummaryResponse[]>
{
    public async Task<Result<GameSummaryResponse[]>> Handle(GetGamesByTournamentQuery query, CancellationToken ct = default)
    {
        if (!await db.Tournaments.AnyAsync(t => t.Id == query.TournamentId, ct))
            return TournamentErrors.NotFound;

        return await db.Games
            .Where(g => g.TournamentId == query.TournamentId)
            .OrderBy(g => g.Name)
            .Select(g => new GameSummaryResponse(g.Id, g.TournamentId, g.Name, g.Location, g.IsDone, g.GameType))
            .ToArrayAsync(ct);
    }
}
