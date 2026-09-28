using AwesomeAssertions;
using TronderLeikan.Domain.Games;
using TronderLeikan.Domain.Tournaments;

namespace TronderLeikan.Domain.Tests.Tournaments;

public class TournamentPointRulesTests
{
    [Fact]
    public void Default_ReturnererRiktigeStandardverdier()
    {
        var rules = TournamentPointRules.Default();

        rules.Participation.Should().Be(3);
        rules.FirstPlace.Should().Be(3);
        rules.SecondPlace.Should().Be(2);
        rules.ThirdPlace.Should().Be(1);
        rules.OrganizedWithParticipation.Should().Be(1);
        rules.OrganizedWithoutParticipation.Should().Be(3);
        rules.Spectator.Should().Be(1);
    }

    [Fact]
    public void Custom_SetsAlleVerdier()
    {
        var rules = TournamentPointRules.Custom(
            participation: 5,
            firstPlace: 5,
            secondPlace: 3,
            thirdPlace: 1,
            organizedWithParticipation: 2,
            organizedWithoutParticipation: 4,
            spectator: 0
        );

        rules.Participation.Should().Be(5);
        rules.FirstPlace.Should().Be(5);
        rules.SecondPlace.Should().Be(3);
        rules.ThirdPlace.Should().Be(1);
        rules.OrganizedWithParticipation.Should().Be(2);
        rules.OrganizedWithoutParticipation.Should().Be(4);
        rules.Spectator.Should().Be(0);
    }

    [Fact]
    public void PointsFor_DeltakerPåFørsteplass_FårDeltakerOgPlasspoeng()
    {
        var person = Guid.NewGuid();
        var game = Game.Create("Kubb", Guid.NewGuid());
        game.AddParticipant(person);
        game.Complete([person], [], []);

        var points = TournamentPointRules.Default().PointsFor(game, person);

        points.Should().Be(new GamePoints(Participation: 3, Placement: 3, Organizing: 0, Spectating: 0));
        points.Total.Should().Be(6);
    }

    [Fact]
    public void PointsFor_DeltFørsteplass_BeggeFårFørsteplasspoeng()
    {
        var alice = Guid.NewGuid();
        var bob = Guid.NewGuid();
        var game = Game.Create("Dart", Guid.NewGuid());
        game.AddParticipant(alice);
        game.AddParticipant(bob);
        game.Complete([alice, bob], [], []);

        var rules = TournamentPointRules.Default();

        rules.PointsFor(game, alice).Placement.Should().Be(3);
        rules.PointsFor(game, bob).Placement.Should().Be(3);
    }

    [Fact]
    public void PointsFor_ArrangørUtenDeltakelse_FårBareArrangørpoeng()
    {
        var arrangør = Guid.NewGuid();
        var game = Game.Create("Boccia", Guid.NewGuid());
        game.AddOrganizer(arrangør, withParticipation: false);

        var points = TournamentPointRules.Default().PointsFor(game, arrangør);

        points.Should().Be(new GamePoints(Participation: 0, Placement: 0, Organizing: 3, Spectating: 0));
    }

    [Fact]
    public void PointsFor_ArrangørMedDeltakelse_FårDeltakerpoengOgArrangørtillegg()
    {
        var arrangør = Guid.NewGuid();
        var game = Game.Create("Vinquiz", Guid.NewGuid());
        game.AddOrganizer(arrangør, withParticipation: true);
        game.Complete([], [arrangør], []);

        var points = TournamentPointRules.Default().PointsFor(game, arrangør);

        points.Should().Be(new GamePoints(Participation: 3, Placement: 2, Organizing: 1, Spectating: 0));
        points.Total.Should().Be(6);
    }

    [Fact]
    public void PointsFor_ArrangørMedDeltakelseSomOgsåErDeltaker_FårDeltakerpoengBareÉnGang()
    {
        var arrangør = Guid.NewGuid();
        var game = Game.Create("Vinquiz", Guid.NewGuid());
        game.AddOrganizer(arrangør, withParticipation: true);
        game.AddParticipant(arrangør);
        game.Complete([arrangør], [], []);

        var points = TournamentPointRules.Default().PointsFor(game, arrangør);

        points.Should().Be(new GamePoints(Participation: 3, Placement: 3, Organizing: 1, Spectating: 0));
        points.Total.Should().Be(7);
    }

    [Fact]
    public void PointsFor_Tilskuer_FårTilskuerpoeng()
    {
        var tilskuer = Guid.NewGuid();
        var game = Game.Create("Pizzabaking", Guid.NewGuid());
        game.AddSpectator(tilskuer);

        var points = TournamentPointRules.Default().PointsFor(game, tilskuer);

        points.Should().Be(new GamePoints(Participation: 0, Placement: 0, Organizing: 0, Spectating: 1));
    }

    [Fact]
    public void PointsFor_PersonSomIkkeVarMed_FårIngenPoeng()
    {
        var game = Game.Create("Kubb", Guid.NewGuid());
        game.AddParticipant(Guid.NewGuid());
        game.Complete([], [], []);

        var points = TournamentPointRules.Default().PointsFor(game, Guid.NewGuid());

        points.Should().Be(new GamePoints(0, 0, 0, 0));
        points.Total.Should().Be(0);
    }

    [Fact]
    public void PointsFor_EgnePoengregler_BrukesIBeregningen()
    {
        var person = Guid.NewGuid();
        var game = Game.Create("Mario Kart", Guid.NewGuid());
        game.AddParticipant(person);
        game.Complete([], [], [person]);
        var rules = TournamentPointRules.Custom(
            participation: 2, firstPlace: 5, secondPlace: 3, thirdPlace: 1,
            organizedWithParticipation: 1, organizedWithoutParticipation: 2, spectator: 1);

        var points = rules.PointsFor(game, person);

        points.Should().Be(new GamePoints(Participation: 2, Placement: 1, Organizing: 0, Spectating: 0));
        points.Total.Should().Be(3);
    }

    [Fact]
    public void PointLinesFor_DeltakerPåAndreplass_FårPostForDeltakelseOgAndreplass()
    {
        var person = Guid.NewGuid();
        var game = Game.Create("Kubb", Guid.NewGuid());
        game.AddParticipant(person);
        game.Complete([], [person], []);

        var lines = TournamentPointRules.Default().PointLinesFor(game, person);

        lines.Should().Equal(
            new PointLine(PointReason.Participation, 3),
            new PointLine(PointReason.SecondPlace, 2));
    }

    [Fact]
    public void PointLinesFor_ArrangørUtenDeltakelse_FårBareArrangørpost()
    {
        var arrangør = Guid.NewGuid();
        var game = Game.Create("Boccia", Guid.NewGuid());
        game.AddOrganizer(arrangør, withParticipation: false);

        var lines = TournamentPointRules.Default().PointLinesFor(game, arrangør);

        lines.Should().Equal(new PointLine(PointReason.OrganizedWithoutParticipation, 3));
    }

    [Fact]
    public void PointLinesFor_ArrangørMedDeltakelseSomOgsåErDeltakerPåFørsteplass_FårDeltakerpostBareÉnGang()
    {
        var arrangør = Guid.NewGuid();
        var game = Game.Create("Vinquiz", Guid.NewGuid());
        game.AddOrganizer(arrangør, withParticipation: true);
        game.AddParticipant(arrangør);
        game.Complete([arrangør], [], []);

        var lines = TournamentPointRules.Default().PointLinesFor(game, arrangør);

        lines.Should().Equal(
            new PointLine(PointReason.Participation, 3),
            new PointLine(PointReason.FirstPlace, 3),
            new PointLine(PointReason.OrganizedWithParticipation, 1));
    }

    [Fact]
    public void PointLinesFor_Tilskuer_FårTilskuerpost()
    {
        var tilskuer = Guid.NewGuid();
        var game = Game.Create("Pizzabaking", Guid.NewGuid());
        game.AddSpectator(tilskuer);

        var lines = TournamentPointRules.Default().PointLinesFor(game, tilskuer);

        lines.Should().Equal(new PointLine(PointReason.Spectator, 1));
    }

    [Fact]
    public void PointLinesFor_RegelGirNullPoeng_PostenVisesLikevel()
    {
        var tilskuer = Guid.NewGuid();
        var game = Game.Create("Pizzabaking", Guid.NewGuid());
        game.AddSpectator(tilskuer);
        var rules = TournamentPointRules.Custom(
            participation: 3, firstPlace: 3, secondPlace: 2, thirdPlace: 1,
            organizedWithParticipation: 1, organizedWithoutParticipation: 3, spectator: 0);

        var lines = rules.PointLinesFor(game, tilskuer);

        lines.Should().Equal(new PointLine(PointReason.Spectator, 0));
    }

    [Fact]
    public void PointLinesFor_PersonSomIkkeVarMed_FårIngenPoster()
    {
        var game = Game.Create("Kubb", Guid.NewGuid());
        game.AddParticipant(Guid.NewGuid());
        game.Complete([], [], []);

        var lines = TournamentPointRules.Default().PointLinesFor(game, Guid.NewGuid());

        lines.Should().BeEmpty();
    }
}
