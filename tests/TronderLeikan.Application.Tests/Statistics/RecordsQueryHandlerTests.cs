using TronderLeikan.Application.Common.Errors;
using TronderLeikan.Application.Statistics.Queries.GetRecords;
using TronderLeikan.Application.Statistics.Queries.GetTournamentRecords;
using TronderLeikan.Application.Statistics.Responses;
using TronderLeikan.Domain.Games;
using TronderLeikan.Domain.Persons;
using TronderLeikan.Domain.Tournaments;

namespace TronderLeikan.Application.Tests.Statistics;

public sealed class RecordsQueryHandlerTests
{
    private static Game Spill(Tournament t, int dag, Guid[] deltakere, Guid vinner, Guid[]? tilskuere = null)
    {
        var game = Game.Create($"Spill {dag}", t.Id);
        game.UpdatePlayedOn(new DateOnly(2026, 6, dag));
        foreach (var id in deltakere) game.AddParticipant(id);
        foreach (var id in tilskuere ?? []) game.AddSpectator(id);
        game.Complete([vinner], [], []);
        return game;
    }

    private static RecordHolderResponse Innehaver(Person p) => new(p.Id, p.FirstName, p.LastName);

    [Fact]
    public async Task GetRecords_ReturnererVerdiOgNavnForHverRekordPåTversAvTurneringer()
    {
        await using var db = TestAppDbContext.Create();
        var kari = Person.Create("Kari", "Nordmann");
        var ola = Person.Create("Ola", "Hansen");
        var vår = Tournament.Create("Vår", "var");
        var høst = Tournament.Create("Høst", "host");
        db.AddRange(kari, ola, vår, høst,
            Spill(vår, 1, [kari.Id, ola.Id], kari.Id),
            Spill(høst, 2, [kari.Id, ola.Id], kari.Id),
            Spill(høst, 3, [kari.Id], kari.Id, tilskuere: [ola.Id]));
        await db.SaveChangesAsync();

        var result = await new GetRecordsQueryHandler(db).Handle(new GetRecordsQuery());

        Assert.True(result.IsSuccess);
        var r = result.Value!;
        Assert.Equal(new RecordResponse(3, [Innehaver(kari)]), r.MostWins, RecordComparer.Instance);
        Assert.Equal(new RecordResponse(3, [Innehaver(kari)]), r.LongestWinStreak, RecordComparer.Instance);
        Assert.Equal(new RecordResponse(3, [Innehaver(kari)]), r.CurrentWinStreak, RecordComparer.Instance);
        Assert.Equal(new RecordResponse(1, [Innehaver(ola)]), r.MostSpectated, RecordComparer.Instance);
        Assert.Equal(new RecordResponse(3, [Innehaver(kari)]), r.MostGamesPlayed, RecordComparer.Instance);
        Assert.Equal(new RecordResponse(3, [Innehaver(kari), Innehaver(ola)]), r.LongestParticipationStreak, RecordComparer.Instance);
        Assert.Equal(new RecordResponse(3, [Innehaver(kari), Innehaver(ola)]), r.LongestCurrentParticipationStreak, RecordComparer.Instance);
    }

    [Fact]
    public async Task GetRecords_UtenSpill_HarIngenInnehavere()
    {
        await using var db = TestAppDbContext.Create();

        var result = await new GetRecordsQueryHandler(db).Handle(new GetRecordsQuery());

        Assert.True(result.IsSuccess);
        Assert.Equal(new RecordResponse(0, []), result.Value!.MostWins, RecordComparer.Instance);
    }

    [Fact]
    public async Task GetTournamentRecords_TellerBareTurneringensSpill()
    {
        await using var db = TestAppDbContext.Create();
        var kari = Person.Create("Kari", "Nordmann");
        var ola = Person.Create("Ola", "Hansen");
        var vår = Tournament.Create("Vår", "var");
        var høst = Tournament.Create("Høst", "host");
        db.AddRange(kari, ola, vår, høst,
            Spill(vår, 1, [kari.Id, ola.Id], ola.Id, tilskuere: []),
            Spill(høst, 2, [kari.Id], kari.Id, tilskuere: [ola.Id]),
            Spill(høst, 3, [kari.Id], kari.Id, tilskuere: [ola.Id]));
        await db.SaveChangesAsync();

        var result = await new GetTournamentRecordsQueryHandler(db).Handle(new GetTournamentRecordsQuery(vår.Id));

        Assert.True(result.IsSuccess);
        var r = result.Value!;
        Assert.Equal(new RecordResponse(1, [Innehaver(ola)]), r.MostWins, RecordComparer.Instance);
        Assert.Equal(new RecordResponse(1, [Innehaver(ola)]), r.LongestWinStreak, RecordComparer.Instance);
        Assert.Equal(new RecordResponse(0, []), r.MostSpectated, RecordComparer.Instance);
        Assert.Equal(new RecordResponse(1, [Innehaver(kari), Innehaver(ola)]), r.MostGamesPlayed, RecordComparer.Instance);
    }

    [Fact]
    public async Task GetTournamentRecords_UkjentTurnering_ReturnererNotFound()
    {
        await using var db = TestAppDbContext.Create();

        var result = await new GetTournamentRecordsQueryHandler(db).Handle(new GetTournamentRecordsQuery(Guid.NewGuid()));

        Assert.False(result.IsSuccess);
        Assert.Equal(TournamentErrors.NotFound, result.Error);
    }

    // Records med lister sammenlignes på innhold, ikke referanse; rekkefølgen på innehaverne spiller ingen rolle
    private sealed class RecordComparer : IEqualityComparer<RecordResponse>
    {
        public static readonly RecordComparer Instance = new();

        public bool Equals(RecordResponse? x, RecordResponse? y) =>
            x is not null && y is not null && x.Value == y.Value &&
            x.Holders.OrderBy(h => h.PersonId).SequenceEqual(y.Holders.OrderBy(h => h.PersonId));

        public int GetHashCode(RecordResponse obj) => obj.Value;
    }
}
