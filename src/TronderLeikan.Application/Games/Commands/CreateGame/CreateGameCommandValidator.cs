using FluentValidation;
namespace TronderLeikan.Application.Games.Commands.CreateGame;
public sealed class CreateGameCommandValidator : AbstractValidator<CreateGameCommand>
{
    public CreateGameCommandValidator()
    {
        RuleFor(c => c.Name).NotEmpty().MaximumLength(500);
        RuleFor(c => c.TournamentId).NotEmpty();
        // Nye spill må ha en dato — eldre spill uten dato er fortsatt gyldige
        RuleFor(c => c.PlayedOn).NotNull()
            .WithErrorCode("Game.PlayedOnRequired").WithMessage("Spillet må ha en dato.");
    }
}
