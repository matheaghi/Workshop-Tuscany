// Oppretter OIDC-appen frontend bruker mot Zitadel, slik at ingen må klikke seg gjennom Zitadel-konsollen.
// Kjøres første gang frontend startes, og er idempotent: eksisterende prosjekt og app gjenbrukes.

using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

internal sealed class ZitadelOidcAppProvisioner(string zitadelBaseUrl, string bootstrapDirectory, string frontendBaseUrl)
{
    private const string ProjectName = "TronderLeikan";
    private const string AppName = "frontend";
    private static readonly TimeSpan ReadyTimeout = TimeSpan.FromMinutes(5);

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    internal sealed record OidcClient(string ClientId, string ClientSecret, string ProjectId, string AppId);

    private string CredentialsPath => Path.Combine(bootstrapDirectory, "frontend-oidc.json");
    private string AdminPatPath => Path.Combine(bootstrapDirectory, "admin.pat");

    /// <summary>
    /// Returnerer client id og secret for frontend-appen. Leser fra fil hvis den finnes og sørger for at
    /// redirect-URI-ene i Zitadel matcher frontendens adresse. Ellers opprettes prosjekt og app i Zitadel
    /// og resultatet lagres i bootstrap-mappen.
    /// </summary>
    public async Task<OidcClient> EnsureAsync(CancellationToken ct)
    {
        using var http = new HttpClient { BaseAddress = new Uri(zitadelBaseUrl) };
        await WaitUntilReadyAsync(http, ct);

        var pat = (await File.ReadAllTextAsync(AdminPatPath, ct)).Trim();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", pat);

        // Finnes appen fortsatt i Zitadel gjenbrukes den. Er Zitadel nullstilt (nytt volum) provisjoneres alt på nytt.
        if (await ReadCachedAsync(ct) is { } cached && await EnsureRedirectUrisAsync(http, cached.ProjectId, cached.AppId, ct))
            return cached;

        var projectId = await FindProjectIdAsync(http, ct) ?? await CreateProjectAsync(http, ct);
        var client = await FindAppAsync(http, projectId, ct) is { } existing
            ? new OidcClient(existing.ClientId, await RegenerateSecretAsync(http, projectId, existing.AppId, ct), projectId, existing.AppId)
            : await CreateAppAsync(http, projectId, ct);

        await File.WriteAllTextAsync(CredentialsPath, JsonSerializer.Serialize(client, Json), ct);
        return client;
    }

    // Eldre filer uten prosjekt- og app-id regnes som ugyldige, da provisjoneres appen på nytt
    private async Task<OidcClient?> ReadCachedAsync(CancellationToken ct)
    {
        if (!File.Exists(CredentialsPath))
            return null;

        var cached = JsonSerializer.Deserialize<OidcClient>(await File.ReadAllTextAsync(CredentialsPath, ct), Json);
        return cached is { ClientId.Length: > 0, ClientSecret.Length: > 0, ProjectId.Length: > 0, AppId.Length: > 0 }
            ? cached
            : null;
    }

    // better-auth sin generic-oauth bruker samme callback som innebygde providere
    private string RedirectUri => $"{frontendBaseUrl}/api/auth/callback/zitadel";

    // Frontendens adresse kan endre seg (for eksempel annen port), da må Zitadel oppdateres, ellers svarer authorize med 400.
    // Returnerer false hvis appen ikke finnes lenger.
    private async Task<bool> EnsureRedirectUrisAsync(HttpClient http, string projectId, string appId, CancellationToken ct)
    {
        var app = await GetAsync<AppEnvelope>(http, $"/management/v1/projects/{projectId}/apps/{appId}", ct);
        if (app?.App?.OidcConfig is null)
            return false;

        var current = app.App.OidcConfig.RedirectUris ?? [];
        if (current.Count == 1 && current[0] == RedirectUri)
            return true;

        var body = new
        {
            redirectUris = new[] { RedirectUri },
            postLogoutRedirectUris = new[] { frontendBaseUrl },
            responseTypes = new[] { "OIDC_RESPONSE_TYPE_CODE" },
            grantTypes = new[] { "OIDC_GRANT_TYPE_AUTHORIZATION_CODE" },
            appType = "OIDC_APP_TYPE_WEB",
            authMethodType = "OIDC_AUTH_METHOD_TYPE_BASIC",
            accessTokenType = "OIDC_TOKEN_TYPE_JWT",
            devMode = true
        };
        using var response = await http.PutAsJsonAsync($"/management/v1/projects/{projectId}/apps/{appId}/oidc_config", body, Json, ct);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(
                $"Zitadel oidc_config svarte {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync(ct)}");
        return true;
    }

    // Venter til Zitadel svarer på ready-endepunktet, og deretter kort på at admin-PAT er skrevet.
    // PAT skrives bare ved første initialisering av instansen, så mangler den etter at Zitadel er klar
    // er databasen initialisert fra før uten bootstrap-mappen, og vi feiler raskt med tydelig beskjed.
    private async Task WaitUntilReadyAsync(HttpClient http, CancellationToken ct)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(ReadyTimeout);

        while (true)
        {
            try
            {
                using var response = await http.GetAsync("/debug/ready", timeout.Token);
                if (response.IsSuccessStatusCode)
                    break;
            }
            catch (HttpRequestException)
            {
                // Zitadel er ikke oppe enda, prøv igjen
            }
            catch (OperationCanceledException) when (timeout.IsCancellationRequested && !ct.IsCancellationRequested)
            {
                throw new TimeoutException(
                    $"Zitadel svarte ikke på {zitadelBaseUrl}/debug/ready innen {ReadyTimeout.TotalMinutes} minutter.");
            }

            await Task.Delay(TimeSpan.FromSeconds(2), timeout.Token);
        }

        for (var attempt = 0; attempt < 15 && !File.Exists(AdminPatPath); attempt++)
            await Task.Delay(TimeSpan.FromSeconds(2), ct);

        if (!File.Exists(AdminPatPath))
            throw new InvalidOperationException(
                $"Zitadel er klar, men admin-PAT finnes ikke i {AdminPatPath}. " +
                "Den skrives bare ved første initialisering. Stopp AppHost, slett Docker-volumet " +
                "«leikan-postgres-data» og mappen zitadel-bootstrap/, og start på nytt.");
    }

    private static async Task<string?> FindProjectIdAsync(HttpClient http, CancellationToken ct)
    {
        var body = new { queries = new[] { new { nameQuery = new { name = ProjectName, method = "TEXT_QUERY_METHOD_EQUALS" } } } };
        var result = await PostAsync<SearchResult<ProjectResult>>(http, "/management/v1/projects/_search", body, ct);
        return result.Result?.FirstOrDefault()?.Id;
    }

    private static async Task<string> CreateProjectAsync(HttpClient http, CancellationToken ct)
    {
        var result = await PostAsync<ProjectResult>(http, "/management/v1/projects", new { name = ProjectName }, ct);
        return result.Id;
    }

    private static async Task<AppResult?> FindAppAsync(HttpClient http, string projectId, CancellationToken ct)
    {
        var body = new { queries = new[] { new { nameQuery = new { name = AppName, method = "TEXT_QUERY_METHOD_EQUALS" } } } };
        var result = await PostAsync<SearchResult<AppResult>>(http, $"/management/v1/projects/{projectId}/apps/_search", body, ct);
        return result.Result?.FirstOrDefault(a => a.OidcConfig is not null);
    }

    private async Task<OidcClient> CreateAppAsync(HttpClient http, string projectId, CancellationToken ct)
    {
        // devMode tillater http://-redirects, som trengs lokalt
        var body = new
        {
            name = AppName,
            redirectUris = new[] { RedirectUri },
            postLogoutRedirectUris = new[] { frontendBaseUrl },
            responseTypes = new[] { "OIDC_RESPONSE_TYPE_CODE" },
            grantTypes = new[] { "OIDC_GRANT_TYPE_AUTHORIZATION_CODE" },
            appType = "OIDC_APP_TYPE_WEB",
            authMethodType = "OIDC_AUTH_METHOD_TYPE_BASIC",
            accessTokenType = "OIDC_TOKEN_TYPE_JWT",
            devMode = true
        };
        var result = await PostAsync<CreateAppResult>(http, $"/management/v1/projects/{projectId}/apps/oidc", body, ct);
        return new OidcClient(result.ClientId, result.ClientSecret, projectId, result.AppId);
    }

    // Secret returneres bare ved opprettelse, så finnes appen uten lagret fil må den genereres på nytt
    private static async Task<string> RegenerateSecretAsync(HttpClient http, string projectId, string appId, CancellationToken ct)
    {
        var result = await PostAsync<SecretResult>(http, $"/management/v1/projects/{projectId}/apps/{appId}/oidc_config/_generate_client_secret", new { }, ct);
        return result.ClientSecret;
    }

    // Returnerer null ved 404, kaster på andre feil
    private static async Task<T?> GetAsync<T>(HttpClient http, string path, CancellationToken ct) where T : class
    {
        using var response = await http.GetAsync(path, ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException($"Zitadel {path} svarte {(int)response.StatusCode}: {error}");
        }

        return await response.Content.ReadFromJsonAsync<T>(Json, ct)
            ?? throw new InvalidOperationException($"Zitadel {path} returnerte tom respons.");
    }

    private static async Task<T> PostAsync<T>(HttpClient http, string path, object body, CancellationToken ct)
    {
        using var response = await http.PostAsJsonAsync(path, body, Json, ct);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException($"Zitadel {path} svarte {(int)response.StatusCode}: {error}");
        }

        return await response.Content.ReadFromJsonAsync<T>(Json, ct)
            ?? throw new InvalidOperationException($"Zitadel {path} returnerte tom respons.");
    }

    private sealed record SearchResult<T>(List<T>? Result);
    private sealed record ProjectResult(string Id);
    private sealed record OidcConfigResult(string ClientId, List<string>? RedirectUris);
    private sealed record AppEnvelope(AppResult? App);
    private sealed record AppResult(string Id, OidcConfigResult? OidcConfig)
    {
        public string AppId => Id;
        public string ClientId => OidcConfig!.ClientId;
    }
    private sealed record CreateAppResult(string AppId, string ClientId, string ClientSecret);
    private sealed record SecretResult(string ClientSecret);
}
