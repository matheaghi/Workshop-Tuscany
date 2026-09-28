using Microsoft.EntityFrameworkCore;
using TronderLeikan.Application.Common.Errors;
using TronderLeikan.Application.Common.Interfaces;
using TronderLeikan.Application.Common.Results;
using TronderLeikan.Application.Tournaments.Responses;
using TronderLeikan.Domain.Tournaments;

namespace TronderLeikan.Application.Tournaments.Queries.GetScoreboard;

public sealed class GetScoreboardQueryHandler(IAppDbContext db)
    : IQueryHandler<GetScoreboardQuery, ScoreboardEntryResponse[]>
{
    public async Task<Result<ScoreboardEntryResponse[]>> Handle(GetScoreboardQuery query, CancellationToken ct = default)
    {
        var tournament = await db.Tournaments.FindAsync([query.TournamentId], ct);
        if (tournament is null)
            return TournamentErrors.NotFound;

        // Hent alle fullførte spill i turneringen
        var games = await db.Games
            .Where(g => g.TournamentId == query.TournamentId && g.IsDone)
            .ToListAsync(ct);

        var allPersonIds = games.SelectMany(g =>
            g.Participants.Concat(g.Organizers).Concat(g.Spectators)).Distinct().ToList();

        var persons = await db.Persons
            .Where(p => allPersonIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, ct);

        // Poeng og rangering beregnes i domenet — samme beregning som personhistorikken bruker
        return Scoreboard.Calculate(games, tournament.PointRules)
            .Where(e => persons.ContainsKey(e.PersonId))
            .Select(e =>
            {
                var person = persons[e.PersonId];
                return new ScoreboardEntryResponse(e.PersonId, person.FirstName, person.LastName, e.TotalPoints, e.Rank);
            })
            .ToArray();
    }
}
