import type { Metadata } from "next";
import { notFound } from "next/navigation";
import Link from "next/link";

// Datamodell for spilldetaljer — tilsvarer API-respons fra /api/v1/games/:id
type GameDetailResponse = {
  id: string;
  tournamentId: string;
  name: string;
  description?: string;
  isDone: boolean;
  gameType: string;
  hasBanner: boolean;
  isOrganizersParticipating: boolean;
  participants: string[];  // GUIDs
  organizers: string[];    // GUIDs
  spectators: string[];    // GUIDs
  firstPlace: string[];    // GUIDs
  secondPlace: string[];   // GUIDs
  thirdPlace: string[];    // GUIDs
};

// Datamodell for personsammendrag — brukes til å slå opp navn fra GUID
type PersonSummaryResponse = {
  id: string;
  firstName: string;
  lastName: string;
  hasProfileImage: boolean;
};

// Henter spilldetaljer via ID. Returnerer null ved feil eller manglende ressurs.
async function getGame(id: string): Promise<GameDetailResponse | null> {
  try {
    const res = await fetch(
      `${process.env.API_BASE_URL ?? "http://localhost:5000"}/api/v1/games/${id}`,
      { cache: "no-store" }
    );
    if (!res.ok) return null;
    return res.json();
  } catch {
    return null;
  }
}

// Henter personsammendrag via ID. Returnerer null ved ukjent GUID eller feil.
async function getPerson(id: string): Promise<PersonSummaryResponse | null> {
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

// Henter navn for en liste med GUIDs parallelt og filtrerer bort nullverdier
async function resolvePersons(ids: string[]): Promise<PersonSummaryResponse[]> {
  const results = await Promise.all(ids.map((id) => getPerson(id)));
  return results.filter((p): p is PersonSummaryResponse => p !== null);
}

// Dynamisk metadata basert på spillnavn — brukes av søkemotorer og sosiale medier
export async function generateMetadata({
  params,
}: {
  params: Promise<{ slug: string; id: string }>;
}): Promise<Metadata> {
  const { id } = await params;
  const game = await getGame(id);

  if (!game) {
    return { title: "Spill ikke funnet" };
  }

  return {
    title: game.name,
    description: game.description ?? `Spilldetaljer for ${game.name} i TrønderLeikan.`,
  };
}

// Én plasseringslinje med plassering og deltakernavn
function PlacementRow({
  place,
  persons,
}: {
  place: 1 | 2 | 3;
  persons: PersonSummaryResponse[];
}) {
  const placeLabels: Record<1 | 2 | 3, string> = {
    1: "1. plass",
    2: "2. plass",
    3: "3. plass",
  };

  if (persons.length === 0) return null;

  return (
    <div className="border border-gray-200 rounded p-4">
      <div className="text-sm text-gray-600 mb-1">{placeLabels[place]}</div>
      {/* Viser alle som deler plassen (ties) på separate linjer */}
      {persons.map((p) => (
        <div key={p.id} className="font-semibold">
          {p.firstName} {p.lastName}
        </div>
      ))}
    </div>
  );
}

// Personliste — viser en gruppe deltakere, arrangører eller tilskuere
function PersonList({
  persons,
  emptyText,
}: {
  persons: PersonSummaryResponse[];
  emptyText: string;
}) {
  if (persons.length === 0) {
    return <p className="text-gray-500">{emptyText}</p>;
  }

  return (
    <ul className="space-y-1">
      {persons.map((p) => (
        <li
          key={p.id}
          className="border border-gray-200 rounded px-3 py-1.5 text-sm"
        >
          {p.firstName} {p.lastName}
        </li>
      ))}
    </ul>
  );
}

// Seksjonstittel — gjenbrukbar overskrift for seksjoner
function SectionHeading({
  id,
  children,
  count,
}: {
  id: string;
  children: React.ReactNode;
  count?: number;
}) {
  return (
    <h2 id={id} className="text-lg font-semibold mb-2">
      {children}
      {count !== undefined && (
        <span className="text-sm text-gray-600 font-normal"> ({count})</span>
      )}
    </h2>
  );
}

// Spilldetalj-side — henter data server-side og rendrer plasseringer og deltakere
export default async function GamePage({
  params,
}: {
  params: Promise<{ slug: string; id: string }>;
}) {
  const { slug, id } = await params;

  // Hent spilldata — vis 404 hvis ikke funnet
  const game = await getGame(id);
  if (!game) notFound();

  // Hent navn for alle GUIDs parallelt — plasseringer og deltakerlister
  const [
    firstPlacePersons,
    secondPlacePersons,
    thirdPlacePersons,
    participantPersons,
    organizerPersons,
    spectatorPersons,
  ] = await Promise.all([
    resolvePersons(game.firstPlace),
    resolvePersons(game.secondPlace),
    resolvePersons(game.thirdPlace),
    resolvePersons(game.participants),
    resolvePersons(game.organizers),
    resolvePersons(game.spectators),
  ]);

  // Avgjør om plasseringsseksjonen skal vises
  const hasResults =
    game.isDone &&
    (firstPlacePersons.length > 0 ||
      secondPlacePersons.length > 0 ||
      thirdPlacePersons.length > 0);

  return (
    <div className="mx-auto max-w-5xl px-4 py-8">
      <div className="mb-4">
        <Link
          href={`/tournaments/${slug}`}
          className="text-sm underline"
        >
          Tilbake til turneringen
        </Link>
      </div>

      <h1 className="text-2xl font-semibold">{game.name}</h1>

      <div className="mt-1 mb-6 flex flex-wrap items-center gap-2 text-sm text-gray-600">
        <span>{game.gameType}</span>
        <span>·</span>
        <span>{game.isDone ? "Ferdig" : "Pågår"}</span>
        {game.isOrganizersParticipating && (
          <>
            <span>·</span>
            <span>Arrangør deltar</span>
          </>
        )}
      </div>

      {/* Valgfri beskrivelse */}
      {game.description && (
        <p className="text-gray-600 mb-6">{game.description}</p>
      )}

      <div className="grid gap-8 md:grid-cols-2">
        {/* Plasseringsseksjon — kun synlig når spillet er ferdig og har resultater */}
        {game.isDone ? (
          <section aria-labelledby="placements-heading">
            <SectionHeading id="placements-heading">Plasseringer</SectionHeading>

            {hasResults ? (
              <div className="space-y-2">
                <PlacementRow place={1} persons={firstPlacePersons} />
                <PlacementRow place={2} persons={secondPlacePersons} />
                <PlacementRow place={3} persons={thirdPlacePersons} />
              </div>
            ) : (
              /* Tomt plasseringsresultat — spillet er ferdig men ingen registrert */
              <p className="text-gray-500">
                Ingen plasseringer registrert. Resultater er ikke lagt inn for
                dette spillet ennå.
              </p>
            )}
          </section>
        ) : (
          /* Spillet pågår — vis info i stedet for plasseringer */
          <section aria-labelledby="status-heading">
            <SectionHeading id="status-heading">Status</SectionHeading>
            <div className="border border-gray-200 rounded p-4">
              <p className="font-medium mb-1">Spillet pågår</p>
              <p className="text-sm text-gray-600">
                Plasseringer og poeng registreres når spillet er fullført.
              </p>
            </div>
          </section>
        )}

        {/* Deltakerlister — deltakere, arrangører og tilskuere */}
        <div className="space-y-6">
          <section aria-labelledby="participants-heading">
            <SectionHeading id="participants-heading" count={participantPersons.length}>
              Deltakere
            </SectionHeading>
            <PersonList
              persons={participantPersons}
              emptyText="Ingen deltakere registrert."
            />
          </section>

          <section aria-labelledby="organizers-heading">
            <SectionHeading id="organizers-heading" count={organizerPersons.length}>
              Arrangører
            </SectionHeading>
            <PersonList
              persons={organizerPersons}
              emptyText="Ingen arrangører registrert."
            />
          </section>

          {/* Tilskuere — vises kun hvis det finnes tilskuere */}
          {spectatorPersons.length > 0 && (
            <section aria-labelledby="spectators-heading">
              <SectionHeading id="spectators-heading" count={spectatorPersons.length}>
                Tilskuere
              </SectionHeading>
              <PersonList
                persons={spectatorPersons}
                emptyText="Ingen tilskuere registrert."
              />
            </section>
          )}
        </div>
      </div>
    </div>
  );
}
