import type { Metadata } from "next";
import Link from "next/link";
import { notFound } from "next/navigation";
import { addParticipantAction, completeGameAction } from "./actions";

// Datamodell for spill med detaljer - inkluderer deltakere og plasseringer
type GameDetailResponse = {
  id: string;
  tournamentId: string;
  name: string;
  description?: string;
  isDone: boolean;
  gameType: string;
  isOrganizersParticipating: boolean;
  participants: string[];
  organizers: string[];
  spectators: string[];
  firstPlace: string[];
  secondPlace: string[];
  thirdPlace: string[];
};

// Datamodell for spilleroversikt - brukes for å slå opp navn på person-ID
type PersonSummaryResponse = {
  id: string;
  firstName: string;
  lastName: string;
  hasProfileImage: boolean;
};

// API-basis-URL - hentes fra miljøvariabel, kun tilgjengelig server-side
const API_BASE = process.env.API_BASE_URL ?? "http://localhost:5000";

// Henter ett spill fra backend - returnerer null ved feil
async function getGame(gameId: string): Promise<GameDetailResponse | null> {
  try {
    const res = await fetch(`${API_BASE}/api/v1/games/${gameId}`, {
      cache: "no-store",
    });
    if (!res.ok) return null;
    return res.json();
  } catch {
    return null;
  }
}

// Henter alle spillere fra backend - returnerer tom liste ved feil
async function getPersons(): Promise<PersonSummaryResponse[]> {
  try {
    const res = await fetch(`${API_BASE}/api/v1/persons`, {
      cache: "no-store",
    });
    if (!res.ok) return [];
    return res.json();
  } catch {
    return [];
  }
}

// Next.js 16 async params - params er et Promise
type Props = {
  params: Promise<{ slug: string; gameId: string }>;
};

export async function generateMetadata({ params }: Props): Promise<Metadata> {
  // Avventer async params før vi kan hente spillnavnet
  const { gameId } = await params;
  const game = await getGame(gameId);
  return {
    title: game?.name ?? "Spill",
  };
}

// Admin-detaljside for ett spill - deltakere, arrangører, tilskuere og fullføring
export default async function AdminGameDetailPage({ params }: Props) {
  // Avventer async params - Next.js 16-krav
  const { slug: tournamentSlug, gameId } = await params;

  // Henter spill og spillere parallelt for å unngå sekvensielle nettverksforespørsler
  const [game, persons] = await Promise.all([getGame(gameId), getPersons()]);

  // Returnerer 404 dersom spillet ikke finnes
  if (!game) {
    notFound();
  }

  // Hjelpefunksjon - slår opp fullt navn på en person via ID
  function getPersonName(personId: string): string {
    const person = persons.find((p) => p.id === personId);
    if (!person) return personId.slice(0, 8) + "…";
    return `${person.firstName} ${person.lastName}`;
  }

  // Spillere som ikke allerede er deltakere i dette spillet - kandidater for å legge til
  const availablePersons = persons.filter(
    (p) => !game.participants.includes(p.id)
  );

  // Sorterer tilgjengelige spillere alfabetisk på etternavn
  const sortedAvailable = availablePersons
    .slice()
    .sort(
      (a, b) =>
        a.lastName.localeCompare(b.lastName, "nb") ||
        a.firstName.localeCompare(b.firstName, "nb")
    );

  // Bundet Server Action - binder gameId og turneringens slug inn i actionen
  const completeGame = completeGameAction.bind(null, gameId, tournamentSlug);

  return (
    <>
      {/* Tilbake-lenke til turnerings-detalj */}
      <Link
        href={`/admin/tournaments/${tournamentSlug}`}
        className="text-sm underline"
      >
        Tilbake til turnering
      </Link>

      {/* Sideoverskrift */}
      <header className="mt-4 mb-6">
        <h1 className="text-2xl font-semibold mb-1">{game.name}</h1>
        <div className="flex items-center gap-2 text-sm text-gray-500">
          <span>{game.gameType}</span>
          <span>·</span>
          <span>{game.isDone ? "Ferdig" : "Pågår"}</span>
          {game.isOrganizersParticipating && (
            <>
              <span>·</span>
              <span>Arrangørene deltar</span>
            </>
          )}
        </div>
      </header>

      {/* ---- Legg til deltaker ---- */}
      {!game.isDone && (
        <section
          className="border border-gray-200 rounded p-4 mb-4"
          aria-labelledby="add-participant-title"
        >
          <h2 className="text-lg font-semibold mb-3" id="add-participant-title">
            Legg til deltaker
          </h2>

          {sortedAvailable.length === 0 ? (
            <p className="text-gray-500">
              Alle registrerte spillere er allerede lagt til som deltakere.
            </p>
          ) : (
            /* Skjema bruker inline Server Action - binder inn gameId og turneringens slug */
            <form
              action={async (formData: FormData) => {
                "use server";
                const personId = formData.get("personId") as string;
                if (personId) {
                  await addParticipantAction(gameId, personId, tournamentSlug);
                }
              }}
              className="flex flex-wrap items-end gap-3"
            >
              <div className="flex-1 min-w-[220px]">
                <label htmlFor="personId" className="block text-sm font-medium mb-1">
                  Spiller
                </label>
                <select
                  id="personId"
                  name="personId"
                  className="rounded border border-gray-300 px-2 py-1.5 text-sm w-full"
                  required
                >
                  <option value="">Velg spiller…</option>
                  {sortedAvailable.map((person) => (
                    <option key={person.id} value={person.id}>
                      {person.lastName}, {person.firstName}
                    </option>
                  ))}
                </select>
              </div>

              <button
                type="submit"
                className="rounded bg-gray-900 px-3 py-1.5 text-sm text-white hover:bg-gray-700 disabled:opacity-50"
              >
                Legg til
              </button>
            </form>
          )}
        </section>
      )}

      {/* ---- Personoversikt - deltakere, arrangører, tilskuere ---- */}
      <div className="grid gap-4 mb-4 sm:grid-cols-2 lg:grid-cols-3">
        {/* Deltakere */}
        <div className="border border-gray-200 rounded p-4">
          <div className="flex items-center justify-between mb-2">
            <h3 className="text-lg font-semibold">Deltakere</h3>
            <span className="text-sm text-gray-500">
              {game.participants.length}
            </span>
          </div>
          {game.participants.length === 0 ? (
            <p className="text-gray-500">Ingen deltakere ennå</p>
          ) : (
            <ul>
              {game.participants.map((personId) => (
                <li
                  key={personId}
                  className="border-b border-gray-200 last:border-b-0 py-1.5 text-sm"
                >
                  {getPersonName(personId)}
                </li>
              ))}
            </ul>
          )}
        </div>

        {/* Arrangører */}
        <div className="border border-gray-200 rounded p-4">
          <div className="flex items-center justify-between mb-2">
            <h3 className="text-lg font-semibold">Arrangører</h3>
            <span className="text-sm text-gray-500">
              {game.organizers.length}
            </span>
          </div>
          {game.organizers.length === 0 ? (
            <p className="text-gray-500">Ingen arrangører registrert</p>
          ) : (
            <ul>
              {game.organizers.map((personId) => (
                <li
                  key={personId}
                  className="border-b border-gray-200 last:border-b-0 py-1.5 text-sm"
                >
                  {getPersonName(personId)}
                </li>
              ))}
            </ul>
          )}
        </div>

        {/* Tilskuere */}
        <div className="border border-gray-200 rounded p-4">
          <div className="flex items-center justify-between mb-2">
            <h3 className="text-lg font-semibold">Tilskuere</h3>
            <span className="text-sm text-gray-500">
              {game.spectators.length}
            </span>
          </div>
          {game.spectators.length === 0 ? (
            <p className="text-gray-500">Ingen tilskuere registrert</p>
          ) : (
            <ul>
              {game.spectators.map((personId) => (
                <li
                  key={personId}
                  className="border-b border-gray-200 last:border-b-0 py-1.5 text-sm"
                >
                  {getPersonName(personId)}
                </li>
              ))}
            </ul>
          )}
        </div>
      </div>

      {/* ---- Plasseringer / Fullfør spill ---- */}
      <section
        className="border border-gray-200 rounded p-4"
        aria-labelledby="placement-title"
      >
        <h2 className="text-lg font-semibold mb-3" id="placement-title">
          {game.isDone ? "Plasseringer" : "Fullfør spill"}
        </h2>

        {game.isDone ? (
          <>
            {/* Spillet er allerede fullført - viser registrerte plasseringer */}
            <p className="text-green-700 text-sm mb-3" role="status">
              Dette spillet er fullført og gir poeng til scoreboard.
            </p>

            <div className="space-y-2 text-sm">
              <div>
                <span className="font-medium">1. plass: </span>
                <span>
                  {game.firstPlace.length > 0
                    ? game.firstPlace.map(getPersonName).join(", ")
                    : "-"}
                </span>
              </div>
              <div>
                <span className="font-medium">2. plass: </span>
                <span>
                  {game.secondPlace.length > 0
                    ? game.secondPlace.map(getPersonName).join(", ")
                    : "-"}
                </span>
              </div>
              <div>
                <span className="font-medium">3. plass: </span>
                <span>
                  {game.thirdPlace.length > 0
                    ? game.thirdPlace.map(getPersonName).join(", ")
                    : "-"}
                </span>
              </div>
            </div>
          </>
        ) : game.participants.length === 0 ? (
          /* Ingen deltakere ennå - kan ikke fullføre */
          <p className="text-gray-500">
            Legg til minst én deltaker før spillet kan fullføres.
          </p>
        ) : (
          /*
            Skjema for å fullføre spillet - multiselect via checkboxer per plassering.
            Plasseringspoeng er additive og legges oppå deltakerpoeng (jf. domenelogikk).
            Flere spillere kan dele samme plassering (ties er tillatt).
          */
          <form action={completeGame}>
            <div className="grid gap-4 mb-4 sm:grid-cols-3">
              {/* 1. plass */}
              <div>
                <p className="text-sm font-medium mb-1">1. plass</p>
                <div className="flex flex-col gap-1">
                  {game.participants.map((personId) => (
                    <label
                      key={personId}
                      className="flex items-center gap-2 text-sm"
                    >
                      <input type="checkbox" name="firstPlace" value={personId} />
                      {getPersonName(personId)}
                    </label>
                  ))}
                </div>
              </div>

              {/* 2. plass */}
              <div>
                <p className="text-sm font-medium mb-1">2. plass</p>
                <div className="flex flex-col gap-1">
                  {game.participants.map((personId) => (
                    <label
                      key={personId}
                      className="flex items-center gap-2 text-sm"
                    >
                      <input type="checkbox" name="secondPlace" value={personId} />
                      {getPersonName(personId)}
                    </label>
                  ))}
                </div>
              </div>

              {/* 3. plass */}
              <div>
                <p className="text-sm font-medium mb-1">3. plass</p>
                <div className="flex flex-col gap-1">
                  {game.participants.map((personId) => (
                    <label
                      key={personId}
                      className="flex items-center gap-2 text-sm"
                    >
                      <input type="checkbox" name="thirdPlace" value={personId} />
                      {getPersonName(personId)}
                    </label>
                  ))}
                </div>
              </div>
            </div>

            <button
              type="submit"
              className="rounded bg-gray-900 px-3 py-1.5 text-sm text-white hover:bg-gray-700 disabled:opacity-50"
            >
              Fullfør spill
            </button>
          </form>
        )}
      </section>
    </>
  );
}
