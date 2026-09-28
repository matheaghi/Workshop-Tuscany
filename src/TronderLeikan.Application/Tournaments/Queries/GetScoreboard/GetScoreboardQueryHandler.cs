using Microsoft.EntityFrameworkCore;
using TronderLeikan.Application.Common.Errors;
using TronderLeikan.Application.Common.Interfaces;
using TronderLeikan.Application.Common.Results;
using TronderLeikan.Application.Tournaments.Responses;

namespace TronderLeikan.Application.Tournaments.Queries.GetScoreboard;

public sealed class GetScoreboardQueryHandler(IAppDbContext db)
    : IQueryHandler<GetScoreboardQuery, ScoreboardEntryResponse[]>
{
    public async Task<Result<ScoreboardEntryResponse[]>> Handle(GetScoreboardQuery query, CancellationToken ct = default)
    {
        var tournament = await db.Tournaments.FindAsync([query.TournamentId], ct);
        if (tournament is null)
            return TournamentErrors.NotFound;

        var rules = tournament.PointRules;

        // Hent alle fullførte spill i turneringen
        var games = await db.Games
            .Where(g => g.TournamentId == query.TournamentId && g.IsDone)
            .ToListAsync(ct);

        var allPersonIds = games.SelectMany(g =>
            g.Participants.Concat(g.Organizers).Concat(g.Spectators)).Distinct().ToList();

        var persons = await db.Persons
            .Where(p => allPersonIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, ct);

        // Akkumuler poeng per person — poengreglene ligger i TournamentPointRules.PointsFor
        var points = new Dictionary<Guid, int>();

        foreach (var game in games)
        {
            var personIdsInGame = game.Participants
                .Concat(game.Organizers)
                .Concat(game.Spectators)
                .Concat(game.FirstPlace)
                .Concat(game.SecondPlace)
                .Concat(game.ThirdPlace)
                .Distinct();

            foreach (var personId in personIdsInGame)
                points[personId] = points.GetValueOrDefault(personId) + rules.PointsFor(game, personId).Total;
        }

        // Sorter synkende, beregn rank med ties
        var sorted = points.OrderByDescending(kv => kv.Value).ToList();
        var entries = new List<ScoreboardEntryResponse>();
        var rank = 1;
        for (var i = 0; i < sorted.Count; i++)
        {
            if (i > 0 && sorted[i].Value < sorted[i - 1].Value)
                rank = i + 1;
            var personId = sorted[i].Key;
            if (!persons.TryGetValue(personId, out var person))
                continue;
            entries.Add(new ScoreboardEntryResponse(personId, person.FirstName, person.LastName, sorted[i].Value, rank));
        }

        return entries.ToArray();
    }
}
