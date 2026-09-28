using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TronderLeikan.Application.Common.Interfaces;
using TronderLeikan.Infrastructure.Persistence;
using TronderLeikan.Infrastructure.Persistence.Seeding;
using TronderLeikan.Infrastructure.Services;

var builder = Host.CreateApplicationBuilder(args);

// Aspire ServiceDefaults gir helse-sjekk og telemetri
builder.AddServiceDefaults();

// IDateTimeProvider kreves av AppDbContext — må registreres før DbContext
builder.Services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

// Npgsql-kobling via Aspire connection string "tronderleikan"
builder.AddNpgsqlDbContext<AppDbContext>("tronderleikan");

using var host = builder.Build();
await host.StartAsync();

// Kjør alle ventende migrations mot databasen og avslutt
await using var scope = host.Services.CreateAsyncScope();
var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
await db.Database.MigrateAsync();
Console.WriteLine("Migrasjoner fullført.");

// Fyll tom database med demodata så scoreboard og sider ikke er tomme ved første oppstart
var seeded = await DemoDataSeeder.SeedIfEmptyAsync(db);
Console.WriteLine(seeded ? "Demodata lagt inn." : "Databasen har allerede data, hopper over seeding.");
await host.StopAsync();
