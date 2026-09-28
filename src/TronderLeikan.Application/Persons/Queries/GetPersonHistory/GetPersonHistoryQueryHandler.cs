using Microsoft.EntityFrameworkCore;
using TronderLeikan.Application.Common.Errors;
using TronderLeikan.Application.Common.Interfaces;
using TronderLeikan.Application.Common.Results;
using TronderLeikan.Application.Persons.Responses;
using TronderLeikan.Domain.Games;
using TronderLeikan.Domain.Tournaments;

namespace TronderLeikan.Application.Persons.Queries.GetPersonHistory;

public sealed class GetPersonHistoryQueryHandler(IAppDbContext db)
    : IQueryHandler<GetPersonHistoryQuery, PersonHistoryResponse>
{
    public async Task<Result<PersonHistoryResponse>> Handle(GetPersonHistoryQuery query, CancellationToken ct = default)
    {
        var person = await db.Persons.FindAsync([query.PersonId], ct);
        if (person is null) return PersonErrors.NotFound;

        // Personlistene ligger i private felt og kan ikke filtreres i spørringen, så alle ferdige spill
        // hentes og filtreres i minnet. Rangeringen trenger uansett alle spillene i turneringen.
        var doneGames = await db.Games.Where(g => g.IsDone).ToListAsync(ct);

        var tournamentIds = doneGames
            .Where(g => IsInvolved(g, person.Id))
            .Select(g => g.TournamentId)
            .Distinct()
            .ToList();

        var tournaments = await db.Tournaments
            .Where(t => tournamentIds.Contains(t.Id))
            .ToListAsync(ct);

        var history = tournaments
            .Select(t => ToTournamentHistory(t, doneGames.Where(g => g.TournamentId == t.Id).ToList(), person.Id))
            .OrderBy(t => t.FirstPlayedOn is null)
            .ThenBy(t => t.FirstPlayedOn)
            .ThenBy(t => t.Name)
            .ToList();

        var allGames = history.SelectMany(t => t.Games).ToList();
        var roleSummary = new RoleSummaryResponse(
            Participated: allGames.Count(g => g.Roles.Contains(GameRole.Participant)),
            Organized: allGames.Count(g => g.Roles.Contains(GameRole.Organizer)),
            Spectated: allGames.Count(g => g.Roles.Contains(GameRole.Spectator)));

        return new PersonHistoryResponse(person.Id, person.FirstName, person.LastName, roleSummary, history);
    }

    private static TournamentHistoryResponse ToTournamentHistory(Tournament tournament, List<Game> games, Guid personId)
    {
        // Samme beregning som scoreboardet, så poeng og plassering alltid stemmer overens
        var entry = Scoreboard.Calculate(games, tournament.PointRules).Single(e => e.PersonId == personId);

        var dates = games.Where(g => g.PlayedOn is not null).Select(g => g.PlayedOn!.Value).ToList();

        var personGames = games
            .Where(g => IsInvolved(g, personId))
            .OrderBy(g => g.PlayedOn is null)
            .ThenBy(g => g.PlayedOn)
            .ThenBy(g => g.Name)
            .Select(g => ToGameHistory(g, tournament.PointRules, personId))
            .ToList();

        return new TournamentHistoryResponse(
            tournament.Id, tournament.Name, tournament.Slug,
            dates.Count > 0 ? dates.Min() : null,
            dates.Count > 0 ? dates.Max() : null,
            entry.TotalPoints, entry.Rank, personGames);
    }

    private static GameHistoryResponse ToGameHistory(Game game, TournamentPointRules rules, Guid personId)
    {
        var isOrganizer = game.Organizers.Contains(personId);

        // Arrangør som spiller regnes også som deltaker, slik poengreglene gjør
        var roles = new List<GameRole>();
        if (game.Participants.Contains(personId) || (isOrganizer && game.IsOrganizersParticipating))
            roles.Add(GameRole.Participant);
        if (isOrganizer)
            roles.Add(GameRole.Organizer);
        if (game.Spectators.Contains(personId))
            roles.Add(GameRole.Spectator);

        int? placement =
            game.FirstPlace.Contains(personId) ? 1 :
            game.SecondPlace.Contains(personId) ? 2 :
            game.ThirdPlace.Contains(personId) ? 3 : null;

        var points = rules.PointsFor(game, personId);

        return new GameHistoryResponse(
            game.Id, game.Name, game.PlayedOn, roles, placement,
            new GamePointsResponse(points.Participation, points.Placement, points.Organizing, points.Spectating, points.Total));
    }

    // Samme utvalg som scoreboardet: personen står i minst én av spillets lister
    private static bool IsInvolved(Game game, Guid personId) =>
        game.Participants.Contains(personId) ||
        game.Organizers.Contains(personId) ||
        game.Spectators.Contains(personId) ||
        game.FirstPlace.Contains(personId) ||
        game.SecondPlace.Contains(personId) ||
        game.ThirdPlace.Contains(personId);
}
