using AwesomeAssertions;
using TronderLeikan.Domain.Games;
using TronderLeikan.Domain.Tournaments;

namespace TronderLeikan.Domain.Tests.Tournaments;

public class ScoreboardTests
{
    private static Game FerdigSpill(Guid[] deltakere, Guid[] første, Guid[] andre, Guid[] tredje)
    {
        var game = Game.Create("Spill", Guid.NewGuid());
        foreach (var id in deltakere) game.AddParticipant(id);
        game.Complete(første, andre, tredje);
        return game;
    }

    [Fact]
    public void Calculate_UtenSpill_ReturnererTomListe()
    {
        Scoreboard.Calculate([], TournamentPointRules.Default()).Should().BeEmpty();
    }

    [Fact]
    public void Calculate_SummererPoengPåTversAvSpill()
    {
        var kari = Guid.NewGuid();
        var spill1 = FerdigSpill([kari], [kari], [], []);   // 3 + 3
        var spill2 = FerdigSpill([kari], [], [], [kari]);   // 3 + 1

        var board = Scoreboard.Calculate([spill1, spill2], TournamentPointRules.Default());

        board.Should().ContainSingle().Which.Should().Be(new ScoreboardEntry(kari, TotalPoints: 10, Rank: 1));
    }

    [Fact]
    public void Calculate_SpillSomIkkeErFerdig_TellerIkke()
    {
        var kari = Guid.NewGuid();
        var planlagt = Game.Create("Petanque", Guid.NewGuid());
        planlagt.AddParticipant(kari);

        Scoreboard.Calculate([planlagt], TournamentPointRules.Default()).Should().BeEmpty();
    }

    [Fact]
    public void Calculate_LikPoengsum_DelerPlasseringOgNesteHopperNed()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var c = Guid.NewGuid();
        var spill = FerdigSpill([a, b, c], [a, b], [c], []);

        var board = Scoreboard.Calculate([spill], TournamentPointRules.Default());

        board.Should().Equal(
            new ScoreboardEntry(a, 6, 1),
            new ScoreboardEntry(b, 6, 1),
            new ScoreboardEntry(c, 5, 3));
    }

    [Fact]
    public void Calculate_BrukerGittePoengregler()
    {
        var jonas = Guid.NewGuid();
        var spill = FerdigSpill([jonas], [jonas], [], []);
        var regler = TournamentPointRules.Custom(
            participation: 2, firstPlace: 5, secondPlace: 3, thirdPlace: 1,
            organizedWithParticipation: 1, organizedWithoutParticipation: 2, spectator: 1);

        Scoreboard.Calculate([spill], regler).Should().ContainSingle()
            .Which.TotalPoints.Should().Be(7);
    }
}
