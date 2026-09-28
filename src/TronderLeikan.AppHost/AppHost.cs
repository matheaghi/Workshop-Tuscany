using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);

// Hemmeligheter genereres første gang og lagres i user secrets, så «dotnet run» virker uten manuelle steg.
// Overstyr ved behov: dotnet user-secrets set "Parameters:postgres-password" "<passord>" --project src/TronderLeikan.AppHost
var postgresPassword = builder.AddParameter("postgres-password",
    new GenerateParameterDefault { MinLength = 24, Special = false }, secret: true, persist: true);

// Zitadel krever en masterkey på nøyaktig 32 tegn
var zitadelMasterKey = builder.AddParameter("zitadel-masterkey",
    new GenerateParameterDefault { MinLength = 32, Special = false }, secret: true, persist: true);

var betterAuthSecret = builder.AddParameter("better-auth-secret",
    new GenerateParameterDefault { MinLength = 32, Special = false }, secret: true, persist: true);

// PostgreSQL — database for TrønderLeikan og Zitadel på samme instans
// md5-autentisering brukes istedenfor scram-sha-256 fordi Zitadels Go pgx-driver
// ikke støtter scram-sha-256-plus (channel binding) mot Aspires SSL-konfigurerte postgres
var postgres = builder.AddPostgres("postgres", password: postgresPassword)
    .WithEnvironment("POSTGRES_HOST_AUTH_METHOD", "md5")
    .WithEnvironment("POSTGRES_INITDB_ARGS", "--auth-host=md5 --auth-local=md5")
    // NB: Endre volum-navn (eller slett eksisterende volum) ved bytte av auth-oppsett,
    // ellers vil initdb-innstillingene over ikke kjøres på allerede initialisert data.
    .WithDataVolume("leikan-postgres-data")
    .WithLifetime(ContainerLifetime.Persistent);
var tronderleikanDb = postgres.AddDatabase("tronderleikan");

// Zitadel bruker en separat database på samme Postgres-instans
var zitadelDb = postgres.AddDatabase("zitadel");

// Zitadel v4-stack: api + login UI + Traefik proxy. Traefik er eneste inngangspunkt.
// Porten må være stabil mellom kjøringer fordi Zitadel lagrer login-URL-ene i databasen ved første init.
// Er 8080 opptatt: dotnet user-secrets set "Zitadel:Port" 8081 --project src/TronderLeikan.AppHost, og nullstill med reset-local.
var zitadelPort = builder.Configuration.GetValue("Zitadel:Port", 8080);
var zitadel = builder.AddZitadel("zitadel", zitadelDb, postgresPassword, zitadelMasterKey, zitadelPort);

// DbMigrator kjøres automatisk ved oppstart, etter at PostgreSQL er klar.
// Kjører migrasjoner og legger inn demodata hvis databasen er tom. API venter til den er ferdig.
var migrator = builder.AddProject<Projects.TronderLeikan_DbMigrator>("migrator")
    .WithReference(tronderleikanDb)
    .WaitFor(tronderleikanDb);

var api = builder.AddProject<Projects.TronderLeikan_API>("api")
    .WithReference(tronderleikanDb)
    // Eksponerer Zitadel-endepunktet som services__zitadel-proxy__http__0
    .WithReference(zitadel.GetEndpoint("http"))
    .WaitFor(migrator)
    .WaitFor(zitadel)
    .WithHttpHealthCheck("/health");

// Frontend — Next.js via npm. Aspire kjører «npm install» og deretter «npm run dev», så en fersk klon starter uten manuelle steg.
// better-auth trenger ZITADEL_ISSUER, CLIENT_ID, CLIENT_SECRET og BETTER_AUTH_SECRET
var frontend = builder.AddJavaScriptApp("frontend", "../frontend")
    .WithReference(api)
    .WithReference(zitadel.GetEndpoint("http"))
    .WithEnvironment("API_BASE_URL", api.GetEndpoint("http"))
    .WithEnvironment("ZITADEL_ISSUER", zitadel.GetEndpoint("http"))
    .WithEnvironment("BETTER_AUTH_SECRET", betterAuthSecret)
    // Ingen fast port: Aspire velger en ledig port og Next.js leser den fra PORT. Adressen står i dashboardet.
    // Provisioneren under oppdaterer redirect-URI i Zitadel hvis porten endrer seg mellom kjøringer.
    .WithHttpEndpoint(env: "PORT", name: "http")
    .WaitFor(api)
    .WaitFor(zitadel);

frontend.WithEnvironment("BETTER_AUTH_URL", frontend.GetEndpoint("http"));

// OIDC-klienten opprettes i Zitadel første gang frontend starter, og lagres i zitadel-bootstrap/ (gitignored).
// Dermed slipper alle å opprette appen manuelt i Zitadel-konsollen.
frontend.WithEnvironment(async ctx =>
{
    var provisioner = new ZitadelOidcAppProvisioner(
        zitadelBaseUrl: zitadel.GetEndpoint("http").Url,
        bootstrapDirectory: Path.Combine(builder.AppHostDirectory, "zitadel-bootstrap"),
        frontendBaseUrl: frontend.GetEndpoint("http").Url);

    var client = await provisioner.EnsureAsync(ctx.CancellationToken);
    ctx.EnvironmentVariables["ZITADEL_CLIENT_ID"] = client.ClientId;
    ctx.EnvironmentVariables["ZITADEL_CLIENT_SECRET"] = client.ClientSecret;
});

builder.Build().Run();
