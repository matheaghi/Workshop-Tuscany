using FluentValidation;
namespace TronderLeikan.Application.Games.Commands.UpdateGame;
public sealed class UpdateGameCommandValidator : AbstractValidator<UpdateGameCommand>
{
    public UpdateGameCommandValidator()
    {
        // En dato kan endres, men ikke fjernes
        RuleFor(c => c.PlayedOn).NotNull()
            .WithErrorCode("Game.PlayedOnRequired").WithMessage("Spillet må ha en dato.");
    }
}
