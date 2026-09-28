using TronderLeikan.Application.Common.Errors;
using TronderLeikan.Application.Persons.Queries.GetPersonHistory;
using TronderLeikan.Application.Persons.Responses;
using TronderLeikan.Domain.Games;
using TronderLeikan.Domain.Persons;
using TronderLeikan.Domain.Tournaments;

namespace TronderLeikan.Application.Tests.Persons;

public sealed class GetPersonHistoryQueryHandlerTests
{
    private static async Task<PersonHistoryResponse> Historikk(TestAppDbContext db, Guid personId)
    {
        var result = await new GetPersonHistoryQueryHandler(db).Handle(new GetPersonHistoryQuery(personId));
        Assert.True(result.IsSuccess);
        return result.Value!;
    }

    private static Game Spill(Tournament t, string navn, DateOnly? dato)
    {
        var game = Game.Create(navn, t.Id);
        game.UpdatePlayedOn(dato);
        return game;
    }

    [Fact]
    public async Task GetPersonHistory_UkjentPerson_ReturnererNotFound()
    {
        await using var db = TestAppDbContext.Create();

        var result = await new GetPersonHistoryQueryHandler(db).Handle(new GetPersonHistoryQuery(Guid.NewGuid()));

        Assert.False(result.IsSuccess);
        Assert.Equal(PersonErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task GetPersonHistory_PersonUtenSpill_ReturnererTomHistorikk()
    {
        await using var db = TestAppDbContext.Create();
        var kari = Person.Create("Kari", "Nordmann");
        db.Persons.Add(kari);
        await db.SaveChangesAsync();

        var historikk = await Historikk(db, kari.Id);

        Assert.Equal("Kari", historikk.FirstName);
        Assert.Equal(new RoleSummaryResponse(0, 0, 0), historikk.RoleSummary);
        Assert.Empty(historikk.Tournaments);
    }

    [Fact]
    public async Task GetPersonHistory_Deltaker_ViserRollePlasseringOgPoengfordeling()
    {
        await using var db = TestAppDbContext.Create();
        var kari = Person.Create("Kari", "Nordmann");
        var t = Tournament.Create("Lagtur", "lagtur");
        var boccia = Spill(t, "Boccia", new DateOnly(2026, 6, 10));
        boccia.AddParticipant(kari.Id);
        boccia.Complete([kari.Id], [], []);
        db.AddRange(kari, t, boccia);
        await db.SaveChangesAsync();

        var spill = Assert.Single(Assert.Single((await Historikk(db, kari.Id)).Tournaments).Games);

        Assert.Equal(boccia.Id, spill.GameId);
        Assert.Equal("Boccia", spill.Name);
        Assert.Equal(new DateOnly(2026, 6, 10), spill.PlayedOn);
        Assert.Equal([GameRole.Participant], spill.Roles);
        Assert.Equal(1, spill.Placement);
        Assert.Equal(3, spill.Points.Participation);
        Assert.Equal(3, spill.Points.Placement);
        Assert.Equal(0, spill.Points.Organizing);
        Assert.Equal(0, spill.Points.Spectating);
        Assert.Equal(6, spill.Points.Total);
        Assert.Equal(
            [new PointLineResponse(PointReason.Participation, 3), new PointLineResponse(PointReason.FirstPlace, 3)],
            spill.Points.Lines);
    }

    [Fact]
    public async Task GetPersonHistory_UtenPallplass_HarIngenPlassering()
    {
        await using var db = TestAppDbContext.Create();
        var kari = Person.Create("Kari", "Nordmann");
        var t = Tournament.Create("Lagtur", "lagtur");
        var pizza = Spill(t, "Pizzabaking", null);
        pizza.AddSpectator(kari.Id);
        pizza.Complete([], [], []);
        db.AddRange(kari, t, pizza);
        await db.SaveChangesAsync();

        var spill = Assert.Single(Assert.Single((await Historikk(db, kari.Id)).Tournaments).Games);

        Assert.Equal([GameRole.Spectator], spill.Roles);
        Assert.Null(spill.Placement);
        Assert.Equal(1, spill.Points.Total);
    }

    [Fact]
    public async Task GetPersonHistory_ArrangørSomSpiller_TellerSomBådeDeltakerOgArrangør()
    {
        await using var db = TestAppDbContext.Create();
        var mari = Person.Create("Mari", "Solberg");
        var t = Tournament.Create("Lagtur", "lagtur");
        var vinquiz = Spill(t, "Vinquiz", null);
        vinquiz.AddOrganizer(mari.Id, withParticipation: true);
        vinquiz.Complete([], [], []);
        var boccia = Spill(t, "Boccia", null);
        boccia.AddOrganizer(mari.Id, withParticipation: false);
        boccia.Complete([], [], []);
        db.AddRange(mari, t, vinquiz, boccia);
        await db.SaveChangesAsync();

        var historikk = await Historikk(db, mari.Id);

        var spill = historikk.Tournaments.Single().Games;
        Assert.Equal([GameRole.Participant, GameRole.Organizer], spill.Single(g => g.GameId == vinquiz.Id).Roles);
        Assert.Equal([GameRole.Organizer], spill.Single(g => g.GameId == boccia.Id).Roles);
        Assert.Equal(new RoleSummaryResponse(Participated: 1, Organized: 2, Spectated: 0), historikk.RoleSummary);
    }

    [Fact]
    public async Task GetPersonHistory_SpillSomIkkeErFerdig_TellerIkke()
    {
        await using var db = TestAppDbContext.Create();
        var kari = Person.Create("Kari", "Nordmann");
        var t = Tournament.Create("Lagtur", "lagtur");
        var petanque = Spill(t, "Petanque", null);
        petanque.AddParticipant(kari.Id);
        db.AddRange(kari, t, petanque);
        await db.SaveChangesAsync();

        var historikk = await Historikk(db, kari.Id);

        Assert.Empty(historikk.Tournaments);
        Assert.Equal(new RoleSummaryResponse(0, 0, 0), historikk.RoleSummary);
    }

    [Fact]
    public async Task GetPersonHistory_ViserTotalpoengOgPlasseringITurneringen()
    {
        await using var db = TestAppDbContext.Create();
        var kari = Person.Create("Kari", "Nordmann");
        var ola = Person.Create("Ola", "Hansen");
        var t = Tournament.Create("Lagtur", "lagtur");
        var spill1 = Spill(t, "Boccia", null);
        spill1.AddParticipant(kari.Id);
        spill1.AddParticipant(ola.Id);
        spill1.Complete([ola.Id], [kari.Id], []);          // Ola 6, Kari 5
        var spill2 = Spill(t, "Dart", null);
        spill2.AddParticipant(kari.Id);
        spill2.Complete([], [], [kari.Id]);                 // Kari 4
        var spill3 = Spill(t, "Kubb", null);
        spill3.AddParticipant(ola.Id);
        spill3.Complete([ola.Id], [], []);                  // Ola 6
        db.AddRange(kari, ola, t, spill1, spill2, spill3);
        await db.SaveChangesAsync();

        var turnering = Assert.Single((await Historikk(db, kari.Id)).Tournaments);

        Assert.Equal(t.Id, turnering.TournamentId);
        Assert.Equal("Lagtur", turnering.Name);
        Assert.Equal("lagtur", turnering.Slug);
        Assert.Equal(9, turnering.TotalPoints);
        Assert.Equal(2, turnering.Rank);                    // bak Ola med 12
        Assert.Equal(2, turnering.Games.Count);             // bare spillene Kari var med i
    }

    [Fact]
    public async Task GetPersonHistory_SortererTurneringerOgSpillEtterDato_UtenDatoSist()
    {
        await using var db = TestAppDbContext.Create();
        var kari = Person.Create("Kari", "Nordmann");
        var lagtur = Tournament.Create("Lagtur", "lagtur");
        var fredagspils = Tournament.Create("Fredagspils", "fredagspils");
        var udatert = Tournament.Create("Udatert", "udatert");

        Game Deltar(Tournament t, string navn, DateOnly? dato)
        {
            var g = Spill(t, navn, dato);
            g.AddParticipant(kari.Id);
            g.Complete([], [], []);
            return g;
        }

        db.AddRange(kari, lagtur, fredagspils, udatert,
            Deltar(lagtur, "Pizzabaking", new DateOnly(2026, 6, 11)),
            Deltar(lagtur, "Boccia", new DateOnly(2026, 6, 10)),
            Deltar(lagtur, "Uten dato", null),
            Deltar(fredagspils, "Dart", new DateOnly(2026, 2, 13)),
            Deltar(fredagspils, "Mario Kart", new DateOnly(2026, 1, 16)),
            Deltar(udatert, "Quiz", null));
        await db.SaveChangesAsync();

        var turneringer = (await Historikk(db, kari.Id)).Tournaments;

        Assert.Equal(["Fredagspils", "Lagtur", "Udatert"], turneringer.Select(t => t.Name));
        Assert.Equal(["Mario Kart", "Dart"], turneringer[0].Games.Select(g => g.Name));
        Assert.Equal(["Boccia", "Pizzabaking", "Uten dato"], turneringer[1].Games.Select(g => g.Name));
        Assert.Equal(new DateOnly(2026, 1, 16), turneringer[0].FirstPlayedOn);
        Assert.Equal(new DateOnly(2026, 2, 13), turneringer[0].LastPlayedOn);
        Assert.Null(turneringer[2].FirstPlayedOn);
    }

    [Fact]
    public async Task GetPersonHistory_OppsummererPoengPerGrunnITurneringen()
    {
        await using var db = TestAppDbContext.Create();
        var kari = Person.Create("Kari", "Nordmann");
        var t = Tournament.Create("Lagtur", "lagtur");
        var boccia = Spill(t, "Boccia", null);
        boccia.AddParticipant(kari.Id);
        boccia.Complete([], [kari.Id], []);                // deltok 3 + 2. plass 2
        var dart = Spill(t, "Dart", null);
        dart.AddParticipant(kari.Id);
        dart.Complete([], [], [kari.Id]);                  // deltok 3 + 3. plass 1
        var pizza = Spill(t, "Pizzabaking", null);
        pizza.AddOrganizer(kari.Id, withParticipation: false);
        pizza.Complete([], [], []);                        // arrangerte uten å spille 3
        var kubb = Spill(t, "Kubb", null);
        kubb.AddSpectator(kari.Id);
        kubb.Complete([], [], []);                         // så på 1
        db.AddRange(kari, t, boccia, dart, pizza, kubb);
        await db.SaveChangesAsync();

        var turnering = Assert.Single((await Historikk(db, kari.Id)).Tournaments);

        Assert.Equal(
            [
                new PointSummaryResponse(PointReason.Participation, Count: 2, Points: 6),
                new PointSummaryResponse(PointReason.SecondPlace, Count: 1, Points: 2),
                new PointSummaryResponse(PointReason.ThirdPlace, Count: 1, Points: 1),
                new PointSummaryResponse(PointReason.OrganizedWithoutParticipation, Count: 1, Points: 3),
                new PointSummaryResponse(PointReason.Spectator, Count: 1, Points: 1),
            ],
            turnering.PointsSummary);
        Assert.Equal(13, turnering.TotalPoints);
        Assert.Equal(turnering.TotalPoints, turnering.PointsSummary.Sum(s => s.Points));
    }
}
