using TronderLeikan.Application.Tournaments.Queries.GetTournaments;
using TronderLeikan.Application.Tournaments.Queries.GetTournamentBySlug;
using TronderLeikan.Application.Tournaments.Queries.GetScoreboard;
using TronderLeikan.Domain.Games;
using TronderLeikan.Domain.Persons;
using TronderLeikan.Domain.Tournaments;

namespace TronderLeikan.Application.Tests.Tournaments;

public sealed class TournamentQueryHandlerTests
{
    [Fact]
    public async Task GetTournaments_ReturnererAlleTurneringer()
    {
        await using var db = TestAppDbContext.Create();
        db.Tournaments.Add(Tournament.Create("NM 2026", "nm-2026"));
        await db.SaveChangesAsync();
        var result = await new GetTournamentsQueryHandler(db).Handle(new GetTournamentsQuery());
        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!);
    }

    [Fact]
    public async Task GetTournamentBySlug_FinnerTurnering()
    {
        await using var db = TestAppDbContext.Create();
        db.Tournaments.Add(Tournament.Create("NM 2026", "nm-2026"));
        await db.SaveChangesAsync();
        var result = await new GetTournamentBySlugQueryHandler(db).Handle(new GetTournamentBySlugQuery("nm-2026"));
        Assert.True(result.IsSuccess);
        Assert.Equal("NM 2026", result.Value!.Name);
    }

    [Fact]
    public async Task GetScoreboard_BeregnerPoengRiktig()
    {
        await using var db = TestAppDbContext.Create();
        var tournament = Tournament.Create("NM", "nm");
        db.Tournaments.Add(tournament);
        var personOla = Person.Create("Ola", "Nordmann");
        var personKari = Person.Create("Kari", "Traa");
        db.Persons.AddRange(personOla, personKari);
        // Ola 1. plass, Kari 2. plass — begge deltakere
        var game = Game.Create("Spill 1", tournament.Id);
        game.AddParticipant(personOla.Id);
        game.AddParticipant(personKari.Id);
        game.Complete([personOla.Id], [personKari.Id], []);
        db.Games.Add(game);
        await db.SaveChangesAsync();

        var result = await new GetScoreboardQueryHandler(db).Handle(new GetScoreboardQuery(tournament.Id));

        // Ola: participation(3) + firstPlace(3) = 6, Kari: participation(3) + secondPlace(2) = 5
        Assert.True(result.IsSuccess);
        var ola = result.Value!.Single(e => e.PersonId == personOla.Id);
        var kari = result.Value!.Single(e => e.PersonId == personKari.Id);
        Assert.Equal(6, ola.TotalPoints);
        Assert.Equal(5, kari.TotalPoints);
        Assert.Equal(1, ola.Rank);
        Assert.Equal(2, kari.Rank);
    }

    // Karakteriseringstester: låser dagens scoreboard-oppførsel før poengreglene flyttes til domenet

    // Oppretter turnering, personer og ett spill, og returnerer scoreboardet
    private static async Task<Dictionary<Guid, (int Points, int Rank)>> Scoreboard(
        Action<Game> oppsett, TournamentPointRules? rules = null, params Person[] persons)
    {
        await using var db = TestAppDbContext.Create();
        var tournament = Tournament.Create("NM", "nm");
        if (rules is not null) tournament.UpdatePointRules(rules);
        db.Tournaments.Add(tournament);
        db.Persons.AddRange(persons);
        var game = Game.Create("Spill", tournament.Id);
        oppsett(game);
        db.Games.Add(game);
        await db.SaveChangesAsync();

        var result = await new GetScoreboardQueryHandler(db).Handle(new GetScoreboardQuery(tournament.Id));
        Assert.True(result.IsSuccess);
        return result.Value!.ToDictionary(e => e.PersonId, e => (e.TotalPoints, e.Rank));
    }

    [Fact]
    public async Task GetScoreboard_ArrangørUtenDeltakelse_FårArrangørpoengUtenDeltakerpoeng()
    {
        var arrangør = Person.Create("Tor", "Eide");
        var board = await Scoreboard(g =>
        {
            g.AddOrganizer(arrangør.Id, withParticipation: false);
            g.Complete([], [], []);
        }, persons: arrangør);

        Assert.Equal(3, board[arrangør.Id].Points);
    }

    [Fact]
    public async Task GetScoreboard_ArrangørMedDeltakelse_FårDeltakerpoengArrangørtilleggOgPlasspoeng()
    {
        var arrangør = Person.Create("Mari", "Solberg");
        var board = await Scoreboard(g =>
        {
            g.AddOrganizer(arrangør.Id, withParticipation: true);
            g.Complete([arrangør.Id], [], []);
        }, persons: arrangør);

        // deltakelse(3) + arrangørtillegg(1) + førsteplass(3)
        Assert.Equal(7, board[arrangør.Id].Points);
    }

    [Fact]
    public async Task GetScoreboard_Tilskuer_FårTilskuerpoeng()
    {
        var tilskuer = Person.Create("Astrid", "Vik");
        var board = await Scoreboard(g =>
        {
            g.AddSpectator(tilskuer.Id);
            g.Complete([], [], []);
        }, persons: tilskuer);

        Assert.Equal(1, board[tilskuer.Id].Points);
    }

    [Fact]
    public async Task GetScoreboard_SpillSomIkkeErFerdig_TellerIkke()
    {
        var deltaker = Person.Create("Ola", "Hansen");
        var board = await Scoreboard(g => g.AddParticipant(deltaker.Id), persons: deltaker);

        Assert.Empty(board);
    }

    [Fact]
    public async Task GetScoreboard_LikPoengsum_DelerPlasseringOgNesteHopperNed()
    {
        var a = Person.Create("A", "A");
        var b = Person.Create("B", "B");
        var c = Person.Create("C", "C");
        var board = await Scoreboard(g =>
        {
            g.AddParticipant(a.Id);
            g.AddParticipant(b.Id);
            g.AddParticipant(c.Id);
            g.Complete([a.Id, b.Id], [c.Id], []);
        }, persons: [a, b, c]);

        // a og b: 3 + 3 = 6, c: 3 + 2 = 5
        Assert.Equal((6, 1), board[a.Id]);
        Assert.Equal((6, 1), board[b.Id]);
        Assert.Equal((5, 3), board[c.Id]);
    }

    [Fact]
    public async Task GetScoreboard_BrukerTurneringensEgnePoengregler()
    {
        var deltaker = Person.Create("Jonas", "Fjeld");
        var arrangør = Person.Create("Astrid", "Vik");
        var tilskuer = Person.Create("Henrik", "Lund");
        var regler = TournamentPointRules.Custom(
            participation: 2, firstPlace: 5, secondPlace: 3, thirdPlace: 1,
            organizedWithParticipation: 1, organizedWithoutParticipation: 4, spectator: 0);
        var board = await Scoreboard(g =>
        {
            g.AddParticipant(deltaker.Id);
            g.AddOrganizer(arrangør.Id, withParticipation: false);
            g.AddSpectator(tilskuer.Id);
            g.Complete([deltaker.Id], [], []);
        }, regler, deltaker, arrangør, tilskuer);

        Assert.Equal(7, board[deltaker.Id].Points);
        Assert.Equal(4, board[arrangør.Id].Points);
        Assert.Equal(0, board[tilskuer.Id].Points);
    }
}
