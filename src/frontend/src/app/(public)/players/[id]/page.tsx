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
  tournaments: {
    tournamentId: string;
    name: string;
    slug: string;
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

      {/* Historikken utelates hvis den ikke kunne hentes */}
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
