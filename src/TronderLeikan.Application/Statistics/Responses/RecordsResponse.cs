namespace TronderLeikan.Application.Statistics.Responses;

// Én rekord: verdien og alle som deler den. Tom liste når ingen har rekorden ennå.
public record RecordResponse(int Value, IReadOnlyList<RecordHolderResponse> Holders);

public record RecordHolderResponse(Guid PersonId, string FirstName, string LastName);

// Rekorder over alle spill, for rekordsiden
public record RecordsResponse(
    RecordResponse MostWins,
    RecordResponse LongestWinStreak,
    RecordResponse CurrentWinStreak,
    RecordResponse MostSpectated,
    RecordResponse MostGamesPlayed,
    RecordResponse LongestParticipationStreak,
    RecordResponse LongestCurrentParticipationStreak);

// Rekorder innenfor én turnering, for turneringssiden
public record TournamentRecordsResponse(
    RecordResponse MostWins,
    RecordResponse LongestWinStreak,
    RecordResponse MostSpectated,
    RecordResponse MostGamesPlayed);
