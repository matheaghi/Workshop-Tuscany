using TronderLeikan.Application.Common.Interfaces;
using TronderLeikan.Application.Games.Responses;

namespace TronderLeikan.Application.Games.Queries.GetGamesByTournament;
public record GetGamesByTournamentQuery(Guid TournamentId) : IQuery<GameSummaryResponse[]>;
