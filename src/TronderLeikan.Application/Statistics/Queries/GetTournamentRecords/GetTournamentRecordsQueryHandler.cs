using Microsoft.EntityFrameworkCore;
using TronderLeikan.Application.Common.Errors;
using TronderLeikan.Application.Common.Interfaces;
using TronderLeikan.Application.Common.Results;
using TronderLeikan.Application.Statistics.Responses;
using TronderLeikan.Domain.Tournaments;

namespace TronderLeikan.Application.Statistics.Queries.GetTournamentRecords;

public sealed class GetTournamentRecordsQueryHandler(IAppDbContext db)
    : IQueryHandler<GetTournamentRecordsQuery, TournamentRecordsResponse>
{
    public async Task<Result<TournamentRecordsResponse>> Handle(GetTournamentRecordsQuery query, CancellationToken ct = default)
    {
        var tournament = await db.Tournaments.FindAsync([query.TournamentId], ct);
        if (tournament is null)
            return TournamentErrors.NotFound;

        var games = await db.Games
            .Where(g => g.TournamentId == query.TournamentId && g.IsDone)
            .ToListAsync(ct);

        RecordHolders[] records =
        [
            Records.MostWins(games),
            Records.LongestWinStreak(games),
            Records.MostSpectated(games),
            Records.MostGamesPlayed(games),
        ];
        var mapper = await RecordMapper.Create(db, records, ct);

        return new TournamentRecordsResponse(
            mapper.Map(records[0]), mapper.Map(records[1]), mapper.Map(records[2]), mapper.Map(records[3]));
    }
}
