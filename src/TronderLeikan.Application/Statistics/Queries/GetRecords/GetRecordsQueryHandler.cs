using Microsoft.EntityFrameworkCore;
using TronderLeikan.Application.Common.Interfaces;
using TronderLeikan.Application.Common.Results;
using TronderLeikan.Application.Statistics.Responses;
using TronderLeikan.Domain.Tournaments;

namespace TronderLeikan.Application.Statistics.Queries.GetRecords;

public sealed class GetRecordsQueryHandler(IAppDbContext db) : IQueryHandler<GetRecordsQuery, RecordsResponse>
{
    public async Task<Result<RecordsResponse>> Handle(GetRecordsQuery query, CancellationToken ct = default)
    {
        // Alle ferdige spill på tvers av turneringer
        var games = await db.Games.Where(g => g.IsDone).ToListAsync(ct);

        RecordHolders[] records =
        [
            Records.MostWins(games),
            Records.LongestWinStreak(games),
            Records.CurrentWinStreakOfLatestWinner(games),
            Records.MostSpectated(games),
            Records.MostGamesPlayed(games),
            Records.LongestParticipationStreak(games),
            Records.LongestCurrentParticipationStreak(games),
        ];
        var mapper = await RecordMapper.Create(db, records, ct);

        return new RecordsResponse(
            mapper.Map(records[0]), mapper.Map(records[1]), mapper.Map(records[2]), mapper.Map(records[3]),
            mapper.Map(records[4]), mapper.Map(records[5]), mapper.Map(records[6]));
    }
}
