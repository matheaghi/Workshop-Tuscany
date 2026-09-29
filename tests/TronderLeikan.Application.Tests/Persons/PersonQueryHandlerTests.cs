using TronderLeikan.Application.Persons.Queries.GetPersons;
using TronderLeikan.Application.Persons.Queries.GetPersonById;
using TronderLeikan.Application.Persons.Queries.GetPersonImage;
using TronderLeikan.Application.Persistence.Images;
using TronderLeikan.Domain.Persons;

namespace TronderLeikan.Application.Tests.Persons;

public sealed class PersonQueryHandlerTests
{
    [Fact]
    public async Task GetPersons_ReturnererAllePersoner()
    {
        await using var db = TestAppDbContext.Create();
        db.Persons.AddRange(Person.Create("Ola", "Nordmann"), Person.Create("Kari", "Traa"));
        await db.SaveChangesAsync();
        var result = await new GetPersonsQueryHandler(db).Handle(new GetPersonsQuery());
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Length);
    }

    [Fact]
    public async Task GetPersonById_EksisterendePerson_ReturnererDetaljer()
    {
        await using var db = TestAppDbContext.Create();
        var person = Person.Create("Ola", "Nordmann");
        db.Persons.Add(person);
        await db.SaveChangesAsync();
        var result = await new GetPersonByIdQueryHandler(db).Handle(new GetPersonByIdQuery(person.Id));
        Assert.True(result.IsSuccess);
        Assert.Equal("Ola", result.Value!.FirstName);
    }

    [Fact]
    public async Task GetPersonById_IkkeEksisterende_ReturnererFeil()
    {
        await using var db = TestAppDbContext.Create();
        var result = await new GetPersonByIdQueryHandler(db).Handle(new GetPersonByIdQuery(Guid.NewGuid()));
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task GetPersonImage_MedBilde_ReturnererBildedataOgInnholdstype()
    {
        await using var db = TestAppDbContext.Create();
        var person = Person.Create("Ola", "Nordmann");
        person.SetProfileImage();
        db.Persons.Add(person);
        db.PersonImages.Add(new PersonImage { PersonId = person.Id, ImageData = [1, 2, 3], ContentType = "image/webp" });
        await db.SaveChangesAsync();
        var result = await new GetPersonImageQueryHandler(db).Handle(new GetPersonImageQuery(person.Id));
        Assert.True(result.IsSuccess);
        Assert.Equal(new byte[] { 1, 2, 3 }, result.Value!.Data);
        Assert.Equal("image/webp", result.Value.ContentType);
    }

    [Fact]
    public async Task GetPersonImage_UtenBilde_ReturnererImageNotFound()
    {
        await using var db = TestAppDbContext.Create();
        var person = Person.Create("Ola", "Nordmann");
        db.Persons.Add(person);
        await db.SaveChangesAsync();
        var result = await new GetPersonImageQueryHandler(db).Handle(new GetPersonImageQuery(person.Id));
        Assert.False(result.IsSuccess);
        Assert.Equal("Person.ImageNotFound", result.Error!.Code);
    }

    [Fact]
    public async Task GetPersonImage_UkjentPerson_ReturnererNotFound()
    {
        await using var db = TestAppDbContext.Create();
        var result = await new GetPersonImageQueryHandler(db).Handle(new GetPersonImageQuery(Guid.NewGuid()));
        Assert.False(result.IsSuccess);
        Assert.Equal("Person.NotFound", result.Error!.Code);
    }
}
