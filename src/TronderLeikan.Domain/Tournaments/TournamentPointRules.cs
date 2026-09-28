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
        var isOrganizer = game.Organizers.Contains(personId);

        // Arrangør som spiller får deltakerpoeng — men bare én gang, selv om personen også er lagt til som deltaker
        var isPlaying = game.Participants.Contains(personId) || (isOrganizer && game.IsOrganizersParticipating);
        var participation = isPlaying ? Participation : 0;

        // Plasspoeng kommer i tillegg til deltakerpoeng
        var placement =
            (game.FirstPlace.Contains(personId) ? FirstPlace : 0) +
            (game.SecondPlace.Contains(personId) ? SecondPlace : 0) +
            (game.ThirdPlace.Contains(personId) ? ThirdPlace : 0);

        var organizing = !isOrganizer ? 0
            : game.IsOrganizersParticipating ? OrganizedWithParticipation : OrganizedWithoutParticipation;

        var spectating = game.Spectators.Contains(personId) ? Spectator : 0;

        return new GamePoints(participation, placement, organizing, spectating);
    }
}
