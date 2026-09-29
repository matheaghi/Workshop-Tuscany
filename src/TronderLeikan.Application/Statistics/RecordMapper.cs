using Microsoft.EntityFrameworkCore;
using TronderLeikan.Application.Common.Interfaces;
using TronderLeikan.Application.Statistics.Responses;
using TronderLeikan.Domain.Tournaments;

namespace TronderLeikan.Application.Statistics;

// Slår opp navnene til rekordinnehaverne. Innehavere uten person (slettet) utelates.
internal sealed class RecordMapper
{
    private readonly Dictionary<Guid, RecordHolderResponse> _holders;

    private RecordMapper(Dictionary<Guid, RecordHolderResponse> holders) => _holders = holders;

    public static async Task<RecordMapper> Create(IAppDbContext db, IEnumerable<RecordHolders> records, CancellationToken ct)
    {
        var personIds = records.SelectMany(r => r.PersonIds).Distinct().ToList();
        var holders = await db.Persons
            .Where(p => personIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => new RecordHolderResponse(p.Id, p.FirstName, p.LastName), ct);
        return new RecordMapper(holders);
    }

    public RecordResponse Map(RecordHolders record) =>
        new(record.Value, record.PersonIds.Where(_holders.ContainsKey).Select(id => _holders[id]).ToList());
}
