using TronderLeikan.Application.Common.Interfaces;
using TronderLeikan.Application.Persons.Responses;

namespace TronderLeikan.Application.Persons.Queries.GetPersonHistory;
public record GetPersonHistoryQuery(Guid PersonId) : IQuery<PersonHistoryResponse>;
