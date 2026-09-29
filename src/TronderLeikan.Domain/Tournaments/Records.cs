using TronderLeikan.Domain.Games;

namespace TronderLeikan.Domain.Tournaments;

// Rekordverdien og alle som deler den. Ingen innehavere når toppverdien er 0.
public sealed record RecordHolders(int Value, IReadOnlyList<Guid> PersonIds);

// Morsom statistikk over ferdige spill. Samme funksjoner brukes for én turnering og for alle spill.
public static class Records
{
    public static RecordHolders MostWins(IEnumerable<Game> games) =>
        Top(Done(games).SelectMany(g => g.FirstPlace));

    public static RecordHolders MostSpectated(IEnumerable<Game> games) =>
        Top(Done(games).SelectMany(g => g.Spectators));

    public static RecordHolders MostGamesPlayed(IEnumerable<Game> games) =>
        Top(Done(games).SelectMany(g => PersonIds(g).Where(g.HasPlayed)));

    // Seiersrekke: seire på rad. Ethvert spill uten seier bryter, også spill personen ikke deltok i.
    public static RecordHolders LongestWinStreak(IEnumerable<Game> games) =>
        Top(WinStreaks(games).Select(s => (s.PersonId, s.Longest)));

    // Deltakerrekke: spill på rad der personen var med i en hvilken som helst rolle
    public static RecordHolders LongestParticipationStreak(IEnumerable<Game> games) =>
        Top(ParticipationStreaks(games).Select(s => (s.PersonId, s.Longest)));

    // Bare vinneren av siste spill har en pågående seiersrekke, så toppen er alltid siste vinner
    public static RecordHolders CurrentWinStreakOfLatestWinner(IEnumerable<Game> games) =>
        Top(WinStreaks(games).Select(s => (s.PersonId, s.Current)));

    public static RecordHolders LongestCurrentParticipationStreak(IEnumerable<Game> games) =>
        Top(ParticipationStreaks(games).Select(s => (s.PersonId, s.Current)));

    // 0 når personen ikke vant siste spill
    public static int CurrentWinStreak(IEnumerable<Game> games, Guid personId) =>
        WinStreaks(games).Where(s => s.PersonId == personId).Select(s => s.Current).FirstOrDefault();

    public static int CurrentParticipationStreak(IEnumerable<Game> games, Guid personId) =>
        ParticipationStreaks(games).Where(s => s.PersonId == personId).Select(s => s.Current).FirstOrDefault();

    private static IEnumerable<(Guid PersonId, int Longest, int Current)> WinStreaks(IEnumerable<Game> games) =>
        Streaks(games, (g, id) => g.FirstPlace.Contains(id));

    private static IEnumerable<(Guid PersonId, int Longest, int Current)> ParticipationStreaks(IEnumerable<Game> games) =>
        Streaks(games, (g, id) => g.IsInvolved(id));

    private static List<Game> Done(IEnumerable<Game> games) => games.Where(g => g.IsDone).ToList();

    // Rekker regnes over daterte, ferdige spill i dato- og navnerekkefølge; spill uten dato er utelatt
    private static List<Game> InOrder(IEnumerable<Game> games) =>
        Done(games)
            .Where(g => g.PlayedOn is not null)
            .OrderBy(g => g.PlayedOn)
            .ThenBy(g => g.Name)
            .ToList();

    // Lengste rekke og rekken som pågår etter siste spill, per person
    private static IEnumerable<(Guid PersonId, int Longest, int Current)> Streaks(
        IEnumerable<Game> games, Func<Game, Guid, bool> counts)
    {
        var ordered = InOrder(games);
        foreach (var personId in ordered.SelectMany(PersonIds).Distinct())
        {
            int longest = 0, current = 0;
            foreach (var game in ordered)
            {
                current = counts(game, personId) ? current + 1 : 0;
                longest = Math.Max(longest, current);
            }
            yield return (personId, longest, current);
        }
    }

    private static IEnumerable<Guid> PersonIds(Game game) =>
        game.Participants
            .Concat(game.Organizers)
            .Concat(game.Spectators)
            .Concat(game.FirstPlace)
            .Concat(game.SecondPlace)
            .Concat(game.ThirdPlace)
            .Distinct();

    // Teller forekomster per person og returnerer alle med høyest antall
    private static RecordHolders Top(IEnumerable<Guid> occurrences) =>
        Top(occurrences.GroupBy(id => id).Select(g => (g.Key, g.Count())));

    private static RecordHolders Top(IEnumerable<(Guid PersonId, int Value)> values)
    {
        var list = values.ToList();
        var max = list.Count > 0 ? list.Max(v => v.Value) : 0;
        if (max == 0) return new RecordHolders(0, []);

        return new RecordHolders(max, list.Where(v => v.Value == max).Select(v => v.PersonId).ToList());
    }
}
