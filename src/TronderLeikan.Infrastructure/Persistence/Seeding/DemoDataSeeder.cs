using Microsoft.EntityFrameworkCore;
using TronderLeikan.Domain.Departments;
using TronderLeikan.Domain.Games;
using TronderLeikan.Domain.Persons;
using TronderLeikan.Domain.Tournaments;

namespace TronderLeikan.Infrastructure.Persistence.Seeding;

/// <summary>
/// Fyller en tom database med demodata slik at scoreboard, spillerprofiler og admin-sider
/// har innhold fra første oppstart. Kjøres av DbMigrator etter migrasjoner.
/// Gjør ingenting hvis databasen allerede har turneringer, og er dermed trygg å kjøre flere ganger.
/// </summary>
internal static class DemoDataSeeder
{
    public static async Task<bool> SeedIfEmptyAsync(AppDbContext db, CancellationToken ct = default)
    {
        // Turneringer er rotaggregatet for alt annet; finnes det én, regnes databasen som i bruk
        if (await db.Tournaments.AnyAsync(ct))
            return false;

        // Avdelinger
        var utvikling = Department.Create("Utvikling");
        var plattform = Department.Create("Plattform");
        var design = Department.Create("Design");
        db.Departments.AddRange(utvikling, plattform, design);

        // Personer - fiktive navn fordelt på avdelingene
        var kari = Person.Create("Kari", "Nordmann", utvikling.Id);
        var ola = Person.Create("Ola", "Hansen", utvikling.Id);
        var ingrid = Person.Create("Ingrid", "Berg", utvikling.Id);
        var magnus = Person.Create("Magnus", "Dahl", utvikling.Id);
        var silje = Person.Create("Silje", "Aune", plattform.Id);
        var tor = Person.Create("Tor", "Eide", plattform.Id);
        var mari = Person.Create("Mari", "Solberg", plattform.Id);
        var henrik = Person.Create("Henrik", "Lund", design.Id);
        var astrid = Person.Create("Astrid", "Vik", design.Id);
        var jonas = Person.Create("Jonas", "Fjeld", null);
        db.Persons.AddRange(kari, ola, ingrid, magnus, silje, tor, mari, henrik, astrid, jonas);

        // Turnering 1: lagtur med standard poengregler
        var lagtur = Tournament.Create("Lagtur Toscana 2026", "lagtur-toscana-2026");

        // Turnering 2: fredagspils med egne poengregler, seier veier tyngre
        var fredagspils = Tournament.Create("Fredagspils 2026", "fredagspils-2026");
        fredagspils.UpdatePointRules(TournamentPointRules.Custom(
            participation: 2,
            firstPlace: 5,
            secondPlace: 3,
            thirdPlace: 1,
            organizedWithParticipation: 1,
            organizedWithoutParticipation: 2,
            spectator: 1));

        db.Tournaments.AddRange(lagtur, fredagspils);

        // Boccia: ferdig, delt andreplass, arrangør deltar ikke
        var boccia = Game.Create("Boccia", lagtur.Id);
        boccia.UpdateDescription("Klassisk boccia på grusbanen. Best av tre runder.");
        boccia.UpdateLocation("Hagen");
        AddParticipants(boccia, kari, ola, ingrid, magnus, silje, henrik);
        boccia.AddOrganizer(tor.Id, withParticipation: false);
        boccia.AddSpectator(astrid.Id);
        boccia.Complete([kari.Id], [ola.Id, ingrid.Id], [magnus.Id]);

        // Vinquiz: ferdig, delt tredjeplass, arrangør deltar selv.
        // Arrangør med deltakelse skal ikke også stå som deltaker, scoreboardet gir deltakerpoeng via arrangørrollen.
        var vinquiz = Game.Create("Vinquiz", lagtur.Id);
        vinquiz.UpdateDescription("Ti runder med blindsmaking og spørsmål om toskansk vin.");
        vinquiz.UpdateLocation("Terrassen");
        AddParticipants(vinquiz, kari, silje, henrik, jonas, astrid, tor);
        vinquiz.AddOrganizer(mari.Id, withParticipation: true);
        vinquiz.Complete([silje.Id], [henrik.Id], [kari.Id, jonas.Id]);

        // Pizzabaking: ferdig, uten ties
        var pizza = Game.Create("Pizzabaking", lagtur.Id);
        pizza.UpdateDescription("Hver deltaker baker én pizza. Juryen er resten av gjengen.");
        pizza.UpdateLocation("Kjøkkenet");
        AddParticipants(pizza, ingrid, magnus, tor, jonas);
        pizza.AddOrganizer(ola.Id, withParticipation: false);
        pizza.AddSpectator(kari.Id);
        pizza.AddSpectator(silje.Id);
        pizza.Complete([ingrid.Id], [magnus.Id], [tor.Id]);

        // Petanque: planlagt, ikke spilt enda
        var petanque = Game.Create("Petanque", lagtur.Id);
        petanque.UpdateDescription("Spilles siste kveld. Vinneren får siste ord i årsrapporten.");
        petanque.UpdateLocation("Plassen foran huset");
        AddParticipants(petanque, kari, ola, silje, henrik, astrid);
        petanque.AddOrganizer(magnus.Id, withParticipation: true);

        // Simracing Mugello: ferdig, plassering avledet av rundetid, to like tider
        var mugello = Game.Create("Simracing: Mugello", lagtur.Id, GameType.Simracing);
        mugello.UpdateDescription("Én flying lap hver på Mugello. Raskeste tid vinner.");
        mugello.UpdateLocation("Stua");
        var mugelloTimes = new (Person Person, long RaceTimeMs)[]
        {
            (magnus, 108_412),
            (jonas, 109_030),
            (ola, 109_030),
            (kari, 110_875),
            (tor, 112_204),
            (henrik, 115_990)
        };
        SeedSimracingGame(db, mugello, mugelloTimes);
        mugello.AddSpectator(silje.Id);
        mugello.AddSpectator(mari.Id);

        db.Games.AddRange(boccia, vinquiz, pizza, petanque, mugello);

        // Mario Kart: ferdig, første spill i fredagspils
        var marioKart = Game.Create("Mario Kart", fredagspils.Id);
        marioKart.UpdateDescription("Fire grand prix, sammenlagt plassering teller.");
        marioKart.UpdateLocation("Kontoret, sofakroken");
        AddParticipants(marioKart, jonas, henrik, kari, astrid, silje, magnus);
        marioKart.AddOrganizer(ingrid.Id, withParticipation: false);
        marioKart.Complete([jonas.Id], [henrik.Id], [kari.Id]);

        // Dart: ferdig, delt førsteplass
        var dart = Game.Create("Dart", fredagspils.Id);
        dart.UpdateDescription("501 dobbelt ut. Første til to sett.");
        dart.UpdateLocation("Kontoret, kjøkkenet");
        AddParticipants(dart, tor, mari, ola, kari, henrik);
        dart.AddOrganizer(astrid.Id, withParticipation: false);
        dart.AddSpectator(jonas.Id);
        dart.Complete([tor.Id, mari.Id], [], [ola.Id]);

        // Shuffleboard: planlagt
        var shuffleboard = Game.Create("Shuffleboard", fredagspils.Id);
        shuffleboard.UpdateDescription("Neste fredag. Ta med egne pucker om du har.");
        shuffleboard.UpdateLocation("Kontoret, kjøkkenet");
        AddParticipants(shuffleboard, kari, ola, ingrid, silje);

        db.Games.AddRange(marioKart, dart, shuffleboard);

        await db.SaveChangesAsync(ct);
        return true;
    }

    private static void AddParticipants(Game game, params Person[] persons)
    {
        foreach (var person in persons)
            game.AddParticipant(person.Id);
    }

    // Registrerer tider og fullfører spillet med samme grupperingslogikk som CompleteSimracingGameCommandHandler:
    // like tider deler plassering
    private static void SeedSimracingGame(AppDbContext db, Game game, (Person Person, long RaceTimeMs)[] times)
    {
        foreach (var (person, raceTimeMs) in times)
        {
            game.AddParticipant(person.Id);
            db.SimracingResults.Add(SimracingResult.Register(game.Id, person.Id, raceTimeMs));
        }

        var groups = times
            .GroupBy(t => t.RaceTimeMs)
            .OrderBy(g => g.Key)
            .Select(g => g.Select(t => t.Person.Id).ToArray())
            .ToList();

        game.Complete(
            groups.Count > 0 ? groups[0] : [],
            groups.Count > 1 ? groups[1] : [],
            groups.Count > 2 ? groups[2] : []);
    }
}
