// Typer og formatering for spillerhistorikken — brukes både av siden (server) og GameHistory (klient)

// Datamodell for spillerhistorikk — tilsvarer API-respons fra /api/v1/persons/:id/history
export type GameRole = "Participant" | "Organizer" | "Spectator";

export type GameHistoryResponse = {
  gameId: string;
  name: string;
  playedOn: string | null;
  roles: GameRole[];
  placement: number | null;
  points: {
    participation: number;
    placement: number;
    organizing: number;
    spectating: number;
    total: number;
  };
};

export type PersonHistoryResponse = {
  personId: string;
  // Antall spill per rolle — arrangør som spiller teller både som deltaker og arrangør
  roleSummary: {
    participated: number;
    organized: number;
    spectated: number;
  };
  // Sortert med eldste turnering først
  tournaments: {
    tournamentId: string;
    name: string;
    slug: string;
    rank: number;
    games: GameHistoryResponse[];
  }[];
};

// Ett spill i den flate historikklisten, med turneringen det hørte til
export type HistoryGame = GameHistoryResponse & {
  tournamentName: string;
  tournamentSlug: string;
};

// Gjør turneringsgrupperingen om til én liste med nyeste spill øverst. Spill uten dato havner sist.
export function toNewestFirst(history: PersonHistoryResponse): HistoryGame[] {
  return history.tournaments
    .flatMap((t) =>
      t.games.map((g) => ({ ...g, tournamentName: t.name, tournamentSlug: t.slug }))
    )
    .sort((a, b) => {
      if (a.playedOn !== b.playedOn) {
        if (a.playedOn === null) return 1;
        if (b.playedOn === null) return -1;
        // ISO-datoer (ÅÅÅÅ-MM-DD) sorteres riktig som tekst
        return b.playedOn.localeCompare(a.playedOn);
      }
      return a.name.localeCompare(b.name, "nb");
    });
}

// UTC hindrer at datoen forskyves én dag når serveren står i en annen tidssone
const dateFormat = new Intl.DateTimeFormat("nb-NO", {
  day: "numeric",
  month: "long",
  year: "numeric",
  timeZone: "UTC",
});

export function formatPlayedOn(playedOn: string | null): string {
  return playedOn ? dateFormat.format(new Date(playedOn)) : "Dato ukjent";
}

const roleLabels: Record<GameRole, string> = {
  Participant: "Deltaker",
  Organizer: "Arrangør",
  Spectator: "Tilskuer",
};

// «Deltaker og arrangør» — første rolle med stor forbokstav, resten med liten
export function formatRoles(roles: GameRole[]): string {
  return roles
    .map((role, i) => (i === 0 ? roleLabels[role] : roleLabels[role].toLowerCase()))
    .join(" og ");
}
