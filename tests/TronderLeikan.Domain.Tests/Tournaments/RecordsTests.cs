using AwesomeAssertions;
using TronderLeikan.Domain.Games;
using TronderLeikan.Domain.Tournaments;

namespace TronderLeikan.Domain.Tests.Tournaments;

public class RecordsTests
{
    private static readonly Guid Kari = Guid.NewGuid();
    private static readonly Guid Ola = Guid.NewGuid();
    private static readonly Guid Per = Guid.NewGuid();

    // Ferdig spill der alle deltakerne spiller og vinnerne står på førsteplass
    private static Game Spill(Guid[] deltakere, Guid[] vinnere, Guid[]? tilskuere = null, int? dag = null, string navn = "Spill")
    {
        var game = Game.Create(navn, Guid.NewGuid());
        foreach (var id in deltakere) game.AddParticipant(id);
        foreach (var id in tilskuere ?? []) game.AddSpectator(id);
        if (dag is not null) game.UpdatePlayedOn(new DateOnly(2026, 1, 1).AddDays(dag.Value));
        game.Complete(vinnere, [], []);
        return game;
    }

    [Fact]
    public void MostWins_TellerFørsteplasser_DeltFørsteplassErSeierForAlle()
    {
        var games = new[]
        {
            Spill([Kari, Ola], [Kari]),
            Spill([Kari, Ola], [Kari, Ola]),
            Spill([Kari, Ola], [Kari]),
        };

        Records.MostWins(games).Should().BeEquivalentTo(new RecordHolders(3, [Kari]));
    }

    [Fact]
    public void MostWins_LiktAntall_AlleDelerRekorden()
    {
        var games = new[] { Spill([Kari, Ola], [Kari]), Spill([Kari, Ola], [Ola]) };

        Records.MostWins(games).Should().BeEquivalentTo(new RecordHolders(1, [Kari, Ola]));
    }

    [Fact]
    public void MostSpectated_TellerSpillSomTilskuer()
    {
        var games = new[]
        {
            Spill([Kari], [Kari], tilskuere: [Ola, Per]),
            Spill([Kari], [Kari], tilskuere: [Ola]),
        };

        Records.MostSpectated(games).Should().BeEquivalentTo(new RecordHolders(2, [Ola]));
    }

    [Fact]
    public void MostGamesPlayed_SpillendeArrangørTeller_TilskuerTellerIkke()
    {
        var medArrangør = Game.Create("Quiz", Guid.NewGuid());
        medArrangør.AddOrganizer(Kari, withParticipation: true);
        medArrangør.AddParticipant(Ola);
        medArrangør.AddSpectator(Per);
        medArrangør.Complete([Ola], [], []);

        var games = new[] { medArrangør, Spill([Kari], [Kari], tilskuere: [Per]) };

        Records.MostGamesPlayed(games).Should().BeEquivalentTo(new RecordHolders(2, [Kari]));
    }

    [Fact]
    public void LongestWinStreak_TapOgSpillManIkkeDeltokIBryterRekken()
    {
        var games = new[]
        {
            Spill([Kari, Ola], [Kari], dag: 1),
            Spill([Kari, Ola], [Kari], dag: 2),
            Spill([Ola], [Ola], dag: 3),          // Kari deltok ikke — rekken brytes
            Spill([Kari, Ola], [Kari], dag: 4),
            Spill([Kari, Ola], [Ola], dag: 5),    // Kari tapte
            Spill([Kari, Ola], [Kari], dag: 6),
        };

        Records.LongestWinStreak(games).Should().BeEquivalentTo(new RecordHolders(2, [Kari]));
    }

    [Fact]
    public void LongestWinStreak_SorteresEtterDato_IkkeRekkefølgenISamlingen()
    {
        var games = new[]
        {
            Spill([Kari, Ola], [Kari], dag: 3),
            Spill([Kari, Ola], [Ola], dag: 2),
            Spill([Kari, Ola], [Kari], dag: 1),
        };

        Records.LongestWinStreak(games).Should().BeEquivalentTo(new RecordHolders(1, [Kari, Ola]));
    }

    [Fact]
    public void Rekker_SpillUtenDatoErUtelatt()
    {
        var games = new[]
        {
            Spill([Kari, Ola], [Kari], dag: 1),
            Spill([Ola], [Ola]),                  // uten dato — bryter ikke Karis rekke
            Spill([Kari, Ola], [Kari], dag: 2),
        };

        Records.LongestWinStreak(games).Should().BeEquivalentTo(new RecordHolders(2, [Kari]));
    }

    [Fact]
    public void Rekker_SammeDag_SorteresEtterNavn()
    {
        var games = new[]
        {
            Spill([Kari, Ola], [Kari], dag: 1, navn: "C"),
            Spill([Ola], [Ola], dag: 1, navn: "B"),   // mellom A og C — bryter Karis rekke
            Spill([Kari, Ola], [Kari], dag: 1, navn: "A"),
        };

        Records.LongestWinStreak(games).Should().BeEquivalentTo(new RecordHolders(1, [Kari, Ola]));
    }

    [Fact]
    public void LongestParticipationStreak_TilskuerOgArrangørHolderRekken_FraværBryter()
    {
        var arrangert = Game.Create("Quiz", Guid.NewGuid());
        arrangert.AddOrganizer(Kari, withParticipation: false);
        arrangert.AddParticipant(Ola);
        arrangert.UpdatePlayedOn(new DateOnly(2026, 1, 3));
        arrangert.Complete([Ola], [], []);

        var games = new[]
        {
            Spill([Kari, Ola], [Ola], dag: 1),
            Spill([Ola], [Ola], tilskuere: [Kari], dag: 2),
            arrangert,
            Spill([Ola], [Ola], dag: 3),          // Kari var ikke med
            Spill([Kari], [Kari], dag: 4),        // Ola var ikke med
        };

        Records.LongestParticipationStreak(games).Should().BeEquivalentTo(new RecordHolders(4, [Ola]));
        Records.LongestParticipationStreak(games.Take(3)).Should().BeEquivalentTo(new RecordHolders(3, [Kari, Ola]));
    }

    [Fact]
    public void CurrentWinStreakOfLatestWinner_ErRekkenTilVinnerenAvSisteSpill()
    {
        var games = new[]
        {
            Spill([Kari, Ola], [Kari], dag: 1),
            Spill([Kari, Ola], [Kari], dag: 2),
            Spill([Kari, Ola], [Kari], dag: 3),
            Spill([Kari, Ola], [Ola], dag: 4),
            Spill([Kari, Ola], [Ola], dag: 5),
        };

        // Karis lengste rekke er 3, men den er brutt — Ola vant de to siste
        Records.CurrentWinStreakOfLatestWinner(games).Should().BeEquivalentTo(new RecordHolders(2, [Ola]));
        Records.CurrentWinStreak(games, Ola).Should().Be(2);
        Records.CurrentWinStreak(games, Kari).Should().Be(0);
    }

    [Fact]
    public void LongestCurrentParticipationStreak_ErRekkenSomPågårEtterSisteSpill()
    {
        var games = new[]
        {
            Spill([Kari, Ola], [Kari], dag: 1),
            Spill([Kari, Ola], [Kari], dag: 2),
            Spill([Kari, Ola], [Kari], dag: 3),
            Spill([Ola, Per], [Ola], dag: 4),
            Spill([Ola], [Ola], tilskuere: [Per], dag: 5),
        };

        Records.LongestCurrentParticipationStreak(games).Should().BeEquivalentTo(new RecordHolders(5, [Ola]));
        Records.CurrentParticipationStreak(games, Per).Should().Be(2);
        Records.CurrentParticipationStreak(games, Kari).Should().Be(0);
    }

    [Fact]
    public void Rekorder_SpillSomIkkeErFerdig_TellerIkke()
    {
        var planlagt = Game.Create("Petanque", Guid.NewGuid());
        planlagt.AddParticipant(Kari);

        Records.MostGamesPlayed([planlagt]).Should().BeEquivalentTo(new RecordHolders(0, []));
    }

    [Fact]
    public void Rekorder_UtenSpill_HarIngenInnehavere()
    {
        Records.MostWins([]).Should().BeEquivalentTo(new RecordHolders(0, []));
        Records.MostSpectated([]).Should().BeEquivalentTo(new RecordHolders(0, []));
        Records.MostGamesPlayed([]).Should().BeEquivalentTo(new RecordHolders(0, []));
    }
}
