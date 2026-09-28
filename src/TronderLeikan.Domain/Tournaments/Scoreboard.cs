using TronderLeikan.Domain.Games;

namespace TronderLeikan.Domain.Tournaments;

public sealed record ScoreboardEntry(Guid PersonId, int TotalPoints, int Rank);

// Summerer poeng fra ferdige spill og rangerer personene.
// Lik poengsum deler plassering, og neste person hopper tilsvarende ned (6, 6, 5 → 1, 1, 3).
public static class Scoreboard
{
    public static IReadOnlyList<ScoreboardEntry> Calculate(IEnumerable<Game> games, TournamentPointRules rules)
    {
        var points = new Dictionary<Guid, int>();

        // Bare spill som er markert som ferdige teller
        foreach (var game in games.Where(g => g.IsDone))
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

        var sorted = points.OrderByDescending(kv => kv.Value).ToList();
        var entries = new List<ScoreboardEntry>(sorted.Count);
        var rank = 1;
        for (var i = 0; i < sorted.Count; i++)
        {
            if (i > 0 && sorted[i].Value < sorted[i - 1].Value)
                rank = i + 1;
            entries.Add(new ScoreboardEntry(sorted[i].Key, sorted[i].Value, rank));
        }

        return entries;
    }
}
