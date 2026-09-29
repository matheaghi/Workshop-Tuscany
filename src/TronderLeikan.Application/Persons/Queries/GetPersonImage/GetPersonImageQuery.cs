using TronderLeikan.Application.Common.Interfaces;
using TronderLeikan.Application.Persons.Responses;

namespace TronderLeikan.Application.Persons.Queries.GetPersonImage;
public record GetPersonImageQuery(Guid PersonId) : IQuery<PersonImageResponse>;
