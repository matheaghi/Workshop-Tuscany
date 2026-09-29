using TronderLeikan.Application.Common.Interfaces;
using TronderLeikan.Application.Statistics.Responses;

namespace TronderLeikan.Application.Statistics.Queries.GetTournamentRecords;
public record GetTournamentRecordsQuery(Guid TournamentId) : IQuery<TournamentRecordsResponse>;
