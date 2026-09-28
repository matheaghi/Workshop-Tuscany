using TronderLeikan.Domain.Games;

namespace TronderLeikan.Domain.Tournaments;

public sealed class TournamentPointRules
{
    private TournamentPointRules() { }

    public int Participation { get; private set; } = 3;
    public int FirstPlace { get; private set; } = 3;
    public int SecondPlace { get; private set; } = 2;
    public int ThirdPlace { get; private set; } = 1;
    public int OrganizedWithParticipation { get; private set; } = 1;
    public int OrganizedWithoutParticipation { get; private set; } = 3;
    public int Spectator { get; private set; } = 1;

    public static TournamentPointRules Default() => new();

    public static TournamentPointRules Custom(
        int participation,
        int firstPlace,
        int secondPlace,
        int thirdPlace,
        int organizedWithParticipation,
        int organizedWithoutParticipation,
        int spectator) =>
        new()
        {
            Participation = participation,
            FirstPlace = firstPlace,
            SecondPlace = secondPlace,
            ThirdPlace = thirdPlace,
            OrganizedWithParticipation = organizedWithParticipation,
            OrganizedWithoutParticipation = organizedWithoutParticipation,
            Spectator = spectator
        };

    // Beregner poengene en person får i et spill etter disse reglene.
    // Sjekker ikke om spillet er ferdig — det er kallerens ansvar å bare ta med ferdige spill.
    public GamePoints PointsFor(Game game, Guid personId)
    {
        var lines = PointLinesFor(game, personId);
        int Sum(params PointReason[] reasons) => lines.Where(l => reasons.Contains(l.Reason)).Sum(l => l.Points);

        return new GamePoints(
            Participation: Sum(PointReason.Participation),
            Placement: Sum(PointReason.FirstPlace, PointReason.SecondPlace, PointReason.ThirdPlace),
            Organizing: Sum(PointReason.OrganizedWithParticipation, PointReason.OrganizedWithoutParticipation),
            Spectating: Sum(PointReason.Spectator));
    }

    // Forklarer poengene en person får i et spill, én post per grunn.
    // En post tas med når rollen gjelder, også om regelen gir 0 poeng, så forklaringen alltid viser hva som telte.
    public IReadOnlyList<PointLine> PointLinesFor(Game game, Guid personId)
    {
        var lines = new List<PointLine>();
        var isOrganizer = game.Organizers.Contains(personId);

        // Arrangør som spiller får deltakerpoeng — men bare én gang, selv om personen også er lagt til som deltaker
        if (game.Participants.Contains(personId) || (isOrganizer && game.IsOrganizersParticipating))
            lines.Add(new PointLine(PointReason.Participation, Participation));

        // Plasspoeng kommer i tillegg til deltakerpoeng
        if (game.FirstPlace.Contains(personId))
            lines.Add(new PointLine(PointReason.FirstPlace, FirstPlace));
        if (game.SecondPlace.Contains(personId))
            lines.Add(new PointLine(PointReason.SecondPlace, SecondPlace));
        if (game.ThirdPlace.Contains(personId))
            lines.Add(new PointLine(PointReason.ThirdPlace, ThirdPlace));

        if (isOrganizer)
            lines.Add(game.IsOrganizersParticipating
                ? new PointLine(PointReason.OrganizedWithParticipation, OrganizedWithParticipation)
                : new PointLine(PointReason.OrganizedWithoutParticipation, OrganizedWithoutParticipation));

        if (game.Spectators.Contains(personId))
            lines.Add(new PointLine(PointReason.Spectator, Spectator));

        return lines;
    }
}
