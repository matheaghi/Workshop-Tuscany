using TronderLeikan.Application.Games.Commands.CreateGame;
using TronderLeikan.Application.Games.Commands.UpdateGame;
using TronderLeikan.Domain.Games;

namespace TronderLeikan.Application.Tests.Games;

// Nye spill må ha en dato, og en dato kan endres men ikke fjernes
public sealed class GameCommandValidatorTests
{
    [Fact]
    public void CreateGame_UtenDato_ErUgyldig()
    {
        var result = new CreateGameCommandValidator().Validate(
            new CreateGameCommand(Guid.NewGuid(), "Dartspill", GameType.Standard));
        Assert.False(result.IsValid);
        Assert.Equal("Game.PlayedOnRequired", result.Errors[0].ErrorCode);
    }

    [Fact]
    public void CreateGame_MedDato_ErGyldig()
    {
        var result = new CreateGameCommandValidator().Validate(
            new CreateGameCommand(Guid.NewGuid(), "Dartspill", GameType.Standard, new DateOnly(2026, 6, 12)));
        Assert.True(result.IsValid);
    }

    [Fact]
    public void UpdateGame_UtenDato_ErUgyldig()
    {
        var result = new UpdateGameCommandValidator().Validate(
            new UpdateGameCommand(Guid.NewGuid(), "Dartspill", null));
        Assert.False(result.IsValid);
        Assert.Equal("Game.PlayedOnRequired", result.Errors[0].ErrorCode);
    }

    [Fact]
    public void UpdateGame_MedDato_ErGyldig()
    {
        var result = new UpdateGameCommandValidator().Validate(
            new UpdateGameCommand(Guid.NewGuid(), "Dartspill", null, new DateOnly(2026, 6, 12)));
        Assert.True(result.IsValid);
    }
}
