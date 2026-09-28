import type { Metadata } from "next";
import { notFound } from "next/navigation";
import Link from "next/link";

// Datamodell for spillerprofil — tilsvarer API-respons fra /api/v1/persons/:id
type PersonDetailResponse = {
  id: string;
  firstName: string;
  lastName: string;
  departmentId?: string;
  hasProfileImage: boolean;
};

// Datamodell for spillerhistorikk — tilsvarer API-respons fra /api/v1/persons/:id/history
type GameRole = "Participant" | "Organizer" | "Spectator";

type GameHistoryResponse = {
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

type PersonHistoryResponse = {
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
type HistoryGame = GameHistoryResponse & {
  tournamentName: string;
  tournamentSlug: string;
};

// Henter én spiller via ID. Returnerer null ved feil eller manglende ressurs.
async function getPersonById(
  id: string
): Promise<PersonDetailResponse | null> {
  try {
    const res = await fetch(
      `${process.env.API_BASE_URL ?? "http://localhost:5000"}/api/v1/persons/${id}`,
      { cache: "no-store" }
    );
    if (!res.ok) return null;
    return res.json();
  } catch {
    return null;
  }
}

// Henter spillerens historikk. Returnerer null ved feil, slik at profilen fortsatt kan vises.
async function getPersonHistory(
  id: string
): Promise<PersonHistoryResponse | null> {
  try {
    const res = await fetch(
      `${process.env.API_BASE_URL ?? "http://localhost:5000"}/api/v1/persons/${id}/history`,
      { cache: "no-store" }
    );
    if (!res.ok) return null;
    return res.json();
  } catch {
    return null;
  }
}

// Gjør turneringsgrupperingen om til én liste med nyeste spill øverst. Spill uten dato havner sist.
function toNewestFirst(history: PersonHistoryResponse): HistoryGame[] {
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

function formatPlayedOn(playedOn: string | null): string {
  return playedOn ? dateFormat.format(new Date(playedOn)) : "Dato ukjent";
}

const roleLabels: Record<GameRole, string> = {
  Participant: "Deltaker",
  Organizer: "Arrangør",
  Spectator: "Tilskuer",
};

// «Deltaker og arrangør» — første rolle med stor forbokstav, resten med liten
function formatRoles(roles: GameRole[]): string {
  return roles
    .map((role, i) => (i === 0 ? roleLabels[role] : roleLabels[role].toLowerCase()))
    .join(" og ");
}

const monthFormat = new Intl.DateTimeFormat("nb-NO", {
  month: "long",
  year: "numeric",
  timeZone: "UTC",
});

// «a», «a og b», «a, b og c»
function joinNorwegian(parts: string[]): string {
  if (parts.length <= 1) return parts.join("");
  return `${parts.slice(0, -1).join(", ")} og ${parts[parts.length - 1]}`;
}

// Rollen som tydelig dominerer (størst antall, uten likhet). Ingen profil ved likhet.
function roleProfile(summary: PersonHistoryResponse["roleSummary"]): string | null {
  const counts = [
    { count: summary.participated, label: "først og fremst en spiller" },
    { count: summary.organized, label: "en ivrig arrangør" },
    { count: summary.spectated, label: "en trofast tilskuer" },
  ].sort((a, b) => b.count - a.count);
  return counts[0].count > counts[1].count ? counts[0].label : null;
}

// Lager en kort fortelling om personen, én setning per element.
// Bruker fornavnet i stedet for pronomen, siden vi ikke vet hvilke pronomen personen bruker.
// games er sortert med nyeste først (se toNewestFirst).
function buildStory(
  name: string,
  history: PersonHistoryResponse,
  games: HistoryGame[]
): string[] {
  if (games.length === 0) {
    return [
      `${name} har ikke vært med på noe spill ennå.`,
      "Neste spill kan bli starten på historien!",
    ];
  }

  const story: string[] = [];
  const { participated, organized, spectated } = history.roleSummary;
  const tournamentCount = history.tournaments.length;
  const datedGames = games.filter((g) => g.playedOn !== null);
  const firstDated = datedGames[datedGames.length - 1];

  // 1. Omfang — eller en velkomst for den som bare har ett spill
  if (games.length === 1) {
    const game = games[0];
    const when = game.playedOn ? ` i ${monthFormat.format(new Date(game.playedOn))}` : "";
    story.push(
      `${name} er ny i Leikan – første spill var ${game.name}${when}, og det ga ${game.points.total} poeng.`
    );
  } else {
    const since = firstDated?.playedOn
      ? ` siden ${monthFormat.format(new Date(firstDated.playedOn))}`
      : "";
    const tournaments = tournamentCount === 1 ? "1 turnering" : `${tournamentCount} turneringer`;
    story.push(`${name} har vært med på ${games.length} spill i ${tournaments}${since}.`);
  }

  // 2. Roller — alltid med antall, og arrangering nevnes alltid
  const notOrganized = organized === 0 ? ", men ikke arrangert noen ennå" : "";
  if (games.length === 1) {
    const roles = formatRoles(games[0].roles).toLowerCase();
    story.push(`Der var ${name} ${roles}${organized === 0 ? ", og har ikke arrangert noe spill ennå" : ""}.`);
  } else {
    const parts = [
      participated > 0 ? `deltatt i ${participated}` : null,
      organized > 0 ? `arrangert ${organized}` : null,
      spectated > 0 ? `sett på ${spectated}` : null,
    ].filter((p): p is string => p !== null);
    const profile = roleProfile(history.roleSummary);
    story.push(
      `Av dem har ${name} ${joinNorwegian(parts)}${notOrganized}${profile ? ` – ${profile}` : ""}.`
    );
  }

  // 3. Pallen — seire og beste plassering
  const podium = games.filter((g) => g.placement !== null);
  const wins = podium.filter((g) => g.placement === 1);
  if (games.length === 1) {
    if (podium.length === 1) story.push(`Det endte med ${podium[0].placement}. plass!`);
  } else if (podium.length === 0) {
    story.push("Pallen venter fortsatt – men hvert spill er en ny sjanse.");
  } else {
    const lead = `${podium.length} av spillene endte på pallen`;
    if (wins.length === 1) {
      story.push(`${lead}, med seier i ${wins[0].name} som høydepunktet.`);
    } else if (wins.length > 1) {
      story.push(`${lead}, med ${wins.length} seire, sist i ${wins[0].name}.`);
    } else {
      const best = Math.min(...podium.map((g) => g.placement!));
      const bestGame = podium.find((g) => g.placement === best)!;
      story.push(`${lead}, og beste plassering er ${best}. plass i ${bestGame.name}.`);
    }
  }

  // 4. Utvikling — første mot siste turnering, bare med minst to turneringer
  if (tournamentCount >= 2) {
    const first = history.tournaments[0];
    const latest = history.tournaments[tournamentCount - 1];
    const trend =
      latest.rank < first.rank
        ? "pila peker oppover"
        : latest.rank > first.rank
          ? "neste turnering kan snu det"
          : "like stødig som før";
    story.push(
      `Fra ${first.rank}. plass i ${first.name} til ${latest.rank}. plass i ${latest.name} – ${trend}.`
    );
  }

  return story;
}

// Én rad i historikken: dato, spill, turnering, rolle, plassering og poeng
function HistoryRow({ game }: { game: HistoryGame }) {
  return (
    <li className="flex flex-wrap items-baseline justify-between gap-x-4 gap-y-1 border-b border-gray-200 py-3">
      <div className="min-w-0">
        <div className="text-sm text-gray-600">
          {game.playedOn ? (
            <time dateTime={game.playedOn}>{formatPlayedOn(game.playedOn)}</time>
          ) : (
            formatPlayedOn(null)
          )}
        </div>
        <Link
          href={`/tournaments/${game.tournamentSlug}/games/${game.gameId}`}
          className="font-medium underline"
        >
          {game.name}
        </Link>
        <div className="text-sm text-gray-600">
          {game.tournamentName} · {formatRoles(game.roles)}
        </div>
      </div>

      <div className="text-right">
        {game.placement !== null && (
          <div className="text-sm font-medium">{game.placement}. plass</div>
        )}
        <div className="text-lg font-semibold">{game.points.total} poeng</div>
      </div>
    </li>
  );
}

// Dynamisk metadata basert på spillerens navn — brukes av søkemotorer og sosiale medier
export async function generateMetadata({
  params,
}: {
  params: Promise<{ id: string }>;
}): Promise<Metadata> {
  const { id } = await params;
  const person = await getPersonById(id);

  if (!person) {
    return { title: "Spiller ikke funnet" };
  }

  return {
    title: `${person.firstName} ${person.lastName}`,
    description: `Profil for ${person.firstName} ${person.lastName} i TrønderLeikan.`,
  };
}

// Henter initialer fra for- og etternavn for avatar-fallback
function getInitials(firstName: string, lastName: string): string {
  return `${firstName.charAt(0)}${lastName.charAt(0)}`.toUpperCase();
}

// Stor initialer-avatar for profilsiden — vises når hasProfileImage er false
function InitialsAvatarLarge({
  firstName,
  lastName,
}: {
  firstName: string;
  lastName: string;
}) {
  const initials = getInitials(firstName, lastName);

  return (
    <div
      aria-hidden="true"
      className="w-[7.5rem] h-[7.5rem] rounded-full border border-gray-200 bg-gray-100 flex items-center justify-center text-3xl font-semibold text-gray-600 shrink-0"
    >
      {initials}
    </div>
  );
}

// Spillerprofil-side — henter data server-side og viser profil med bilde eller initialer
export default async function PlayerProfilePage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = await params;

  // Hent spiller og historikk samtidig — vis 404 hvis spilleren ikke finnes
  const [person, history] = await Promise.all([
    getPersonById(id),
    getPersonHistory(id),
  ]);
  if (!person) notFound();

  const games = history ? toNewestFirst(history) : null;

  return (
    <div className="mx-auto max-w-5xl px-4 py-8">
      <div className="mb-4">
        <Link href="/players" className="text-sm underline">
          Alle spillere
        </Link>
      </div>

      <div className="flex flex-col items-start gap-4">
        {person.hasProfileImage ? (
          /* eslint-disable-next-line @next/next/no-img-element */
          <img
            src={`/api/v1/persons/${person.id}/image`}
            alt={`${person.firstName} ${person.lastName}`}
            width={120}
            height={120}
            className="w-[7.5rem] h-[7.5rem] rounded-full object-cover border border-gray-200"
          />
        ) : (
          <InitialsAvatarLarge firstName={person.firstName} lastName={person.lastName} />
        )}

        <h1 className="text-2xl font-semibold">
          {person.firstName} {person.lastName}
        </h1>
      </div>

      {/* Oppsummering og historikk utelates hvis historikken ikke kunne hentes */}
      {history && games && (
        <section
          aria-label="Oppsummering"
          className="mt-6 border border-gray-200 rounded p-4"
        >
          <p>{buildStory(person.firstName, history, games).join(" ")}</p>
        </section>
      )}

      {games && (
        <section aria-labelledby="history-heading" className="mt-8">
          <div className="flex items-center justify-between mb-2">
            <h2 id="history-heading" className="text-lg font-semibold">
              Spillhistorikk
            </h2>

            {games.length > 0 && (
              <span className="text-sm text-gray-600">
                {games.length} spill
              </span>
            )}
          </div>

          {games.length === 0 ? (
            <p className="text-gray-500">Ingen ferdige spill ennå.</p>
          ) : (
            <ol>
              {games.map((game) => (
                <HistoryRow key={game.gameId} game={game} />
              ))}
            </ol>
          )}
        </section>
      )}
    </div>
  );
}
