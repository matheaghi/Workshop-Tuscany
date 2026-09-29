namespace TronderLeikan.Api.Tests.Statistics;

[Collection(nameof(ApiTestCollection))]
public class RecordsApiTests(TronderLeikanApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    // Oppretter en person som vinner ett ferdig spill i en ny turnering
    private async Task<(Guid PersonId, Guid TournamentId)> VinnerEttSpill(string fornavn, string? playedOn)
    {
        var personId = await (await _client.PostAsJsonAsync("/api/v1/persons",
            new { firstName = fornavn, lastName = "Testesen" })).Content.ReadFromJsonAsync<Guid>();
        var tournamentId = await (await _client.PostAsJsonAsync("/api/v1/tournaments",
            new { name = "Rekordtest", slug = $"r-{Guid.NewGuid():N}" })).Content.ReadFromJsonAsync<Guid>();
        var gameId = await (await _client.PostAsJsonAsync("/api/v1/games",
            new { tournamentId, name = "Kubb", gameType = 0, playedOn })).Content.ReadFromJsonAsync<Guid>();
        await _client.PostAsJsonAsync($"/api/v1/games/{gameId}/participants", new { gameId, personId });
        await _client.PostAsJsonAsync($"/api/v1/games/{gameId}/complete",
            new { gameId, firstPlace = new[] { personId }, secondPlace = Array.Empty<Guid>(), thirdPlace = Array.Empty<Guid>() });
        return (personId, tournamentId);
    }

    [Fact]
    public async Task GET_tournament_records_viser_rekordene_i_turneringen()
    {
        var (personId, tournamentId) = await VinnerEttSpill("Turnering", "2026-06-12");

        var response = await _client.GetAsync($"/api/v1/tournaments/{tournamentId}/records");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var flestSeire = body.GetProperty("mostWins");
        flestSeire.GetProperty("value").GetInt32().Should().Be(1);
        var innehaver = flestSeire.GetProperty("holders")[0];
        innehaver.GetProperty("personId").GetGuid().Should().Be(personId);
        innehaver.GetProperty("firstName").GetString().Should().Be("Turnering");
        innehaver.GetProperty("lastName").GetString().Should().Be("Testesen");
        body.GetProperty("longestWinStreak").GetProperty("value").GetInt32().Should().Be(1);
        body.GetProperty("mostGamesPlayed").GetProperty("value").GetInt32().Should().Be(1);
        body.GetProperty("mostSpectated").GetProperty("holders").GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task GET_tournament_records_for_turnering_som_ikke_finnes_returnerer_404()
    {
        var response = await _client.GetAsync($"/api/v1/tournaments/{Guid.NewGuid()}/records");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("title").GetString().Should().Be("Tournament.NotFound");
    }

    // Databasen deles med andre tester, så spillet får en dato ingen andre bruker: det blir siste spill,
    // og bare personen i det har pågående rekker
    [Fact]
    public async Task GET_records_og_historikk_viser_pågående_rekker_etter_siste_spill()
    {
        var (personId, _) = await VinnerEttSpill("Sistevinner", "2099-12-31");

        var records = await (await _client.GetAsync("/api/v1/records")).Content.ReadFromJsonAsync<JsonElement>();

        var seiersrekke = records.GetProperty("currentWinStreak");
        seiersrekke.GetProperty("value").GetInt32().Should().Be(1);
        seiersrekke.GetProperty("holders")[0].GetProperty("personId").GetGuid().Should().Be(personId);
        var deltakerrekke = records.GetProperty("longestCurrentParticipationStreak");
        deltakerrekke.GetProperty("holders")[0].GetProperty("firstName").GetString().Should().Be("Sistevinner");
        foreach (var felt in new[] { "mostWins", "longestWinStreak", "mostSpectated", "mostGamesPlayed", "longestParticipationStreak" })
            records.GetProperty(felt).GetProperty("holders").ValueKind.Should().Be(JsonValueKind.Array);

        var historikk = await (await _client.GetAsync($"/api/v1/persons/{personId}/history")).Content.ReadFromJsonAsync<JsonElement>();

        historikk.GetProperty("currentParticipationStreak").GetInt32().Should().Be(1);
        historikk.GetProperty("currentWinStreak").GetInt32().Should().Be(1);
    }
}
