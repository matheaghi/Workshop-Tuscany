using TronderLeikan.Application.Common.Errors;
using TronderLeikan.Application.Common.Interfaces;
using TronderLeikan.Application.Common.Results;
using TronderLeikan.Application.Persons.Responses;

namespace TronderLeikan.Application.Persons.Queries.GetPersonImage;

// Henter profilbildet — profilbilde er valgfritt, så manglende bilde er en egen feil
public sealed class GetPersonImageQueryHandler(IAppDbContext db)
    : IQueryHandler<GetPersonImageQuery, PersonImageResponse>
{
    public async Task<Result<PersonImageResponse>> Handle(GetPersonImageQuery query, CancellationToken ct = default)
    {
        var person = await db.Persons.FindAsync([query.PersonId], ct);
        if (person is null) return PersonErrors.NotFound;
        var image = await db.PersonImages.FindAsync([query.PersonId], ct);
        if (image is null) return PersonErrors.ImageNotFound;
        return new PersonImageResponse(image.ImageData, image.ContentType);
    }
}
