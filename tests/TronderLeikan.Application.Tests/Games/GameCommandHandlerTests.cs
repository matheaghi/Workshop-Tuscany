using TronderLeikan.Application.Games.Commands.CreateGame;
using TronderLeikan.Application.Games.Commands.AddParticipant;
using TronderLeikan.Application.Games.Commands.CompleteGame;
using TronderLeikan.Application.Games.Commands.UpdateGame;
using TronderLeikan.Domain.Games;
using TronderLeikan.Domain.Persons;
using TronderLeikan.Domain.Tournaments;

namespace TronderLeikan.Application.Tests.Games;

public sealed class GameCommandHandlerTests
{
    [Fact]
    public async Task CreateGame_LagrerSpill()
    {
        await using var db = TestAppDbContext.Create();
        var tournament = Tournament.Create("NM", "nm");
        db.Tournaments.Add(tournament);
        await db.SaveChangesAsync();
        var result = await new CreateGameCommandHandler(db).Handle(new CreateGameCommand(tournament.Id, "Dartspill", GameType.Standard));
        Assert.True(result.IsSuccess);
        Assert.Single(db.Games.ToList());
    }

    [Fact]
    public async Task CreateGame_MedSpilldato_LagrerSpilldato()
    {
        await using var db = TestAppDbContext.Create();
        var tournament = Tournament.Create("NM", "nm");
        db.Tournaments.Add(tournament);
        await db.SaveChangesAsync();
        var result = await new CreateGameCommandHandler(db).Handle(
            new CreateGameCommand(tournament.Id, "Dartspill", GameType.Standard, new DateOnly(2026, 6, 12)));
        Assert.True(result.IsSuccess);
        var game = await db.Games.FindAsync(result.Value);
        Assert.Equal(new DateOnly(2026, 6, 12), game!.PlayedOn);
    }

    [Fact]
    public async Task UpdateGame_MedSpilldato_OppdatererSpilldato()
    {
        await using var db = TestAppDbContext.Create();
        var game = Game.Create("Spill", Guid.NewGuid());
        db.Games.Add(game);
        await db.SaveChangesAsync();
        var result = await new UpdateGameCommandHandler(db).Handle(
            new UpdateGameCommand(game.Id, "Spill", null, new DateOnly(2026, 6, 12)));
        Assert.True(result.IsSuccess);
        var updated = await db.Games.FindAsync(game.Id);
        Assert.Equal(new DateOnly(2026, 6, 12), updated!.PlayedOn);
    }

    [Fact]
    public async Task AddParticipant_LeggerTilDeltaker()
    {
        await using var db = TestAppDbContext.Create();
        var game = Game.Create("Spill", Guid.NewGuid());
        db.Games.Add(game);
        var person = Person.Create("Ola", "Nordmann");
        db.Persons.Add(person);
        await db.SaveChangesAsync();
        var result = await new AddParticipantCommandHandler(db).Handle(new AddParticipantCommand(game.Id, person.Id));
        Assert.True(result.IsSuccess);
        var updated = await db.Games.FindAsync(game.Id);
        Assert.Contains(person.Id, updated!.Participants);
    }

    [Fact]
    public async Task CompleteGame_SetterIsDoneOgPlasseringer()
    {
        await using var db = TestAppDbContext.Create();
        var personA = Person.Create("A", "A");
        var personB = Person.Create("B", "B");
        db.Persons.AddRange(personA, personB);
        var game = Game.Create("Spill", Guid.NewGuid());
        db.Games.Add(game);
        await db.SaveChangesAsync();
        var result = await new CompleteGameCommandHandler(db).Handle(new CompleteGameCommand(game.Id, [personA.Id], [personB.Id], []));
        Assert.True(result.IsSuccess);
        var updated = await db.Games.FindAsync(game.Id);
        Assert.True(updated!.IsDone);
        Assert.Contains(personA.Id, updated.FirstPlace);
    }
}
