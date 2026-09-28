namespace TronderLeikan.Api.Tests.Persons;

[Collection(nameof(ApiTestCollection))]
public class PersonsApiTests(TronderLeikanApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task POST_persons_returnerer_201_med_guid()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/persons", new
        {
            firstName = "Ola",
            lastName = "Nordmann"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var id = await response.Content.ReadFromJsonAsync<Guid>();
        id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GET_person_som_ikke_finnes_returnerer_404_problem_details()
    {
        var response = await _client.GetAsync($"/api/v1/persons/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("title").GetString().Should().Be("Person.NotFound");
        body.GetProperty("status").GetInt32().Should().Be(404);
        body.GetProperty("detail").GetString().Should().Be("Personen finnes ikke.");
    }

    [Fact]
    public async Task GET_historikk_for_person_som_ikke_finnes_returnerer_404()
    {
        var response = await _client.GetAsync($"/api/v1/persons/{Guid.NewGuid()}/history");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("title").GetString().Should().Be("Person.NotFound");
    }

    [Fact]
    public async Task GET_historikk_viser_ferdig_spill_med_poeng_og_rolle()
    {
        var personId = await (await _client.PostAsJsonAsync("/api/v1/persons",
            new { firstName = "Historie", lastName = "Testesen" })).Content.ReadFromJsonAsync<Guid>();
        var tournamentId = await (await _client.PostAsJsonAsync("/api/v1/tournaments",
            new { name = "Historikktest", slug = $"h-{Guid.NewGuid():N}" })).Content.ReadFromJsonAsync<Guid>();
        var gameId = await (await _client.PostAsJsonAsync("/api/v1/games",
            new { tournamentId, name = "Kubb", gameType = 0, playedOn = "2026-06-12" })).Content.ReadFromJsonAsync<Guid>();
        await _client.PostAsJsonAsync($"/api/v1/games/{gameId}/participants", new { gameId, personId });
        await _client.PostAsJsonAsync($"/api/v1/games/{gameId}/complete",
            new { gameId, firstPlace = new[] { personId }, secondPlace = Array.Empty<Guid>(), thirdPlace = Array.Empty<Guid>() });

        var response = await _client.GetAsync($"/api/v1/persons/{personId}/history");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("roleSummary").GetProperty("participated").GetInt32().Should().Be(1);
        var turnering = body.GetProperty("tournaments")[0];
        turnering.GetProperty("rank").GetInt32().Should().Be(1);
        var spill = turnering.GetProperty("games")[0];
        spill.GetProperty("playedOn").GetString().Should().Be("2026-06-12");
        spill.GetProperty("roles")[0].GetString().Should().Be("Participant");
        spill.GetProperty("placement").GetInt32().Should().Be(1);
        spill.GetProperty("points").GetProperty("total").GetInt32().Should().Be(6);
    }

    [Fact]
    public async Task GET_historikk_forklarer_poengene_med_poster_per_spill_og_sammenlagt()
    {
        var personId = await (await _client.PostAsJsonAsync("/api/v1/persons",
            new { firstName = "Poeng", lastName = "Forklaresen" })).Content.ReadFromJsonAsync<Guid>();
        var tournamentId = await (await _client.PostAsJsonAsync("/api/v1/tournaments",
            new { name = "Poengtest", slug = $"p-{Guid.NewGuid():N}" })).Content.ReadFromJsonAsync<Guid>();
        var gameId = await (await _client.PostAsJsonAsync("/api/v1/games",
            new { tournamentId, name = "Kubb", gameType = 0 })).Content.ReadFromJsonAsync<Guid>();
        await _client.PostAsJsonAsync($"/api/v1/games/{gameId}/participants", new { gameId, personId });
        await _client.PostAsJsonAsync($"/api/v1/games/{gameId}/complete",
            new { gameId, firstPlace = new[] { personId }, secondPlace = Array.Empty<Guid>(), thirdPlace = Array.Empty<Guid>() });

        var body = await (await _client.GetAsync($"/api/v1/persons/{personId}/history")).Content.ReadFromJsonAsync<JsonElement>();

        var turnering = body.GetProperty("tournaments")[0];
        var poster = turnering.GetProperty("games")[0].GetProperty("points").GetProperty("lines");
        poster.GetArrayLength().Should().Be(2);
        poster[0].GetProperty("reason").GetString().Should().Be("Participation");
        poster[0].GetProperty("points").GetInt32().Should().Be(3);
        poster[1].GetProperty("reason").GetString().Should().Be("FirstPlace");
        poster[1].GetProperty("points").GetInt32().Should().Be(3);
        var sammenlagt = turnering.GetProperty("pointsSummary");
        sammenlagt[1].GetProperty("reason").GetString().Should().Be("FirstPlace");
        sammenlagt[1].GetProperty("count").GetInt32().Should().Be(1);
        sammenlagt[1].GetProperty("points").GetInt32().Should().Be(3);
    }

    [Fact]
    public async Task Opprett_og_hent_person_happy_path()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/v1/persons", new
        {
            firstName = "Kari",
            lastName = "Nordmann",
            departmentId = (Guid?)null
        });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var id = await createResponse.Content.ReadFromJsonAsync<Guid>();

        var getResponse = await _client.GetAsync($"/api/v1/persons/{id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await getResponse.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("firstName").GetString().Should().Be("Kari");
        body.GetProperty("lastName").GetString().Should().Be("Nordmann");
        body.GetProperty("hasProfileImage").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task PUT_person_oppdaterer_navn()
    {
        var id = await (await _client.PostAsJsonAsync("/api/v1/persons",
            new { firstName = "Gammel", lastName = "Navn" }))
            .Content.ReadFromJsonAsync<Guid>();

        var updateResponse = await _client.PutAsJsonAsync($"/api/v1/persons/{id}", new
        {
            personId = id,
            firstName = "Nytt",
            lastName = "Navn"
        });
        updateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var body = await (await _client.GetAsync($"/api/v1/persons/{id}"))
            .Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("firstName").GetString().Should().Be("Nytt");
    }

    [Fact]
    public async Task DELETE_person_returnerer_204()
    {
        var id = await (await _client.PostAsJsonAsync("/api/v1/persons",
            new { firstName = "Slett", lastName = "Meg" }))
            .Content.ReadFromJsonAsync<Guid>();

        var deleteResponse = await _client.DeleteAsync($"/api/v1/persons/{id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DELETE_person_som_ikke_finnes_returnerer_404()
    {
        var response = await _client.DeleteAsync($"/api/v1/persons/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GET_persons_returnerer_liste()
    {
        var postResponse = await _client.PostAsJsonAsync("/api/v1/persons", new { firstName = "Liste", lastName = "Test" });
        postResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var newId = await postResponse.Content.ReadFromJsonAsync<Guid>();

        var response = await _client.GetAsync("/api/v1/persons");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var ids = body.EnumerateArray()
            .Select(p => p.GetProperty("id").GetGuid())
            .ToList();
        ids.Should().Contain(newId);
    }
}
