import type { Metadata } from "next";
import Link from "next/link";
import { notFound } from "next/navigation";
import { updatePointRulesAction, createGameAction } from "./actions";

// Datamodell for turnering med detaljer — inkluderer poengregler
type TournamentDetailResponse = {
  id: string;
  name: string;
  slug: string;
  pointRules: {
    participation: number;
    firstPlace: number;
    secondPlace: number;
    thirdPlace: number;
    organizedWithParticipation: number;
    organizedWithoutParticipation: number;
    spectator: number;
  };
};

// Datamodell for spill — brukes i spilliste
type GameSummaryResponse = {
  id: string;
  tournamentId: string;
  name: string;
  isDone: boolean;
  gameType: string;
};

// API-basis-URL — hentes fra miljøvariabel, kun tilgjengelig server-side
const API_BASE = process.env.API_BASE_URL ?? "http://localhost:5000";

// Henter turnering med detaljer fra backend på slug — returnerer null ved feil
async function getTournament(
  slug: string
): Promise<TournamentDetailResponse | null> {
  try {
    const res = await fetch(`${API_BASE}/api/v1/tournaments/${slug}`, {
      cache: "no-store",
    });
    if (!res.ok) return null;
    return res.json();
  } catch {
    return null;
  }
}

// Henter alle spill for en turnering — returnerer tom liste ved feil
async function getGames(tournamentId: string): Promise<GameSummaryResponse[]> {
  try {
    const res = await fetch(
      `${API_BASE}/api/v1/tournaments/${tournamentId}/games`,
      { cache: "no-store" }
    );
    if (!res.ok) return [];
    return res.json();
  } catch {
    return [];
  }
}

// Next.js 16 async params — params er et Promise
type Props = {
  params: Promise<{ slug: string }>;
};

export async function generateMetadata({ params }: Props): Promise<Metadata> {
  // Avventer async params før vi kan hente turneringsnavnet
  const { slug } = await params;
  const tournament = await getTournament(slug);
  return {
    title: tournament?.name ?? "Turnering",
  };
}

// Admin-detaljside for en enkelt turnering — poengregler og spilliste
export default async function AdminTournamentDetailPage({ params }: Props) {
  // Avventer async params — Next.js 16-krav
  const { slug } = await params;

  // Turneringen slås opp på slug; spillene trenger turneringens id og hentes etterpå
  const tournament = await getTournament(slug);

  // Returnerer 404 dersom turneringen ikke finnes
  if (!tournament) {
    notFound();
  }

  const games = await getGames(tournament.id);

  // Hjelpefunksjon — genererer bundet Server Action for poengregler
  const updateRules = updatePointRulesAction.bind(null, tournament.id, tournament.slug);
  // Hjelpefunksjon — genererer bundet Server Action for opprett spill
  const createGame = createGameAction.bind(null, tournament.id, tournament.slug);

  // Sorterer spill: uferdige øverst, deretter alfabetisk
  const sortedGames = games
    .slice()
    .sort((a, b) => {
      if (a.isDone !== b.isDone) return a.isDone ? 1 : -1;
      return a.name.localeCompare(b.name, "nb");
    });

  return (
    <>
      {/* Tilbake-lenke til turneringsliste */}
      <Link href="/admin/tournaments" className="text-sm underline mb-6 inline-block">
        Alle turneringer
      </Link>

      {/* Sideoverskrift */}
      <header className="mb-6">
        <h1 className="text-2xl font-semibold mb-1">{tournament.name}</h1>
        <p className="text-sm text-gray-500">/{tournament.slug}</p>
      </header>

      {/* ---- Poengregler ---- */}
      <section
        className="border border-gray-200 rounded p-4 mb-6"
        aria-labelledby="point-rules-title"
      >
        <h2 className="text-lg font-semibold mb-4" id="point-rules-title">
          Poengregler
        </h2>

        {/*
          Poengregler er additive: plasseringspoeng legges oppå deltakerpoeng.
          Se domenelogikk i CLAUDE.md for detaljer om beregningsregler.
        */}
        <form action={updateRules}>
          <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 gap-4 mb-4">
            <div>
              <label htmlFor="participation" className="block text-sm font-medium mb-1">
                Deltaker
              </label>
              <input
                id="participation"
                name="participation"
                type="number"
                min="0"
                defaultValue={tournament.pointRules.participation}
                required
                className="w-full rounded border border-gray-300 px-2 py-1.5 text-sm"
              />
            </div>

            <div>
              <label htmlFor="firstPlace" className="block text-sm font-medium mb-1">
                1. plass (additivt)
              </label>
              <input
                id="firstPlace"
                name="firstPlace"
                type="number"
                min="0"
                defaultValue={tournament.pointRules.firstPlace}
                required
                className="w-full rounded border border-gray-300 px-2 py-1.5 text-sm"
              />
            </div>

            <div>
              <label htmlFor="secondPlace" className="block text-sm font-medium mb-1">
                2. plass (additivt)
              </label>
              <input
                id="secondPlace"
                name="secondPlace"
                type="number"
                min="0"
                defaultValue={tournament.pointRules.secondPlace}
                required
                className="w-full rounded border border-gray-300 px-2 py-1.5 text-sm"
              />
            </div>

            <div>
              <label htmlFor="thirdPlace" className="block text-sm font-medium mb-1">
                3. plass (additivt)
              </label>
              <input
                id="thirdPlace"
                name="thirdPlace"
                type="number"
                min="0"
                defaultValue={tournament.pointRules.thirdPlace}
                required
                className="w-full rounded border border-gray-300 px-2 py-1.5 text-sm"
              />
            </div>

            <div>
              <label
                htmlFor="organizedWithParticipation"
                className="block text-sm font-medium mb-1"
              >
                Arrangør m/ deltakelse
              </label>
              <input
                id="organizedWithParticipation"
                name="organizedWithParticipation"
                type="number"
                min="0"
                defaultValue={
                  tournament.pointRules.organizedWithParticipation
                }
                required
                className="w-full rounded border border-gray-300 px-2 py-1.5 text-sm"
              />
            </div>

            <div>
              <label
                htmlFor="organizedWithoutParticipation"
                className="block text-sm font-medium mb-1"
              >
                Arrangør u/ deltakelse
              </label>
              <input
                id="organizedWithoutParticipation"
                name="organizedWithoutParticipation"
                type="number"
                min="0"
                defaultValue={
                  tournament.pointRules.organizedWithoutParticipation
                }
                required
                className="w-full rounded border border-gray-300 px-2 py-1.5 text-sm"
              />
            </div>

            <div>
              <label htmlFor="spectator" className="block text-sm font-medium mb-1">
                Tilskuer
              </label>
              <input
                id="spectator"
                name="spectator"
                type="number"
                min="0"
                defaultValue={tournament.pointRules.spectator}
                required
                className="w-full rounded border border-gray-300 px-2 py-1.5 text-sm"
              />
            </div>
          </div>

          <button
            type="submit"
            className="rounded bg-gray-900 px-3 py-1.5 text-sm text-white hover:bg-gray-700 disabled:opacity-50"
          >
            Lagre poengregler
          </button>
        </form>
      </section>

      {/* ---- Opprett spill ---- */}
      <section
        className="border border-gray-200 rounded p-4 mb-6"
        aria-labelledby="create-game-title"
      >
        <h2 className="text-lg font-semibold mb-4" id="create-game-title">
          Legg til nytt spill
        </h2>

        <form action={createGame} className="flex flex-wrap gap-3 items-end">
          <div className="flex-1 min-w-[180px]">
            <label htmlFor="gameName" className="block text-sm font-medium mb-1">
              Navn
            </label>
            <input
              id="gameName"
              name="name"
              type="text"
              required
              placeholder="Bowling"
              className="h-9 w-full rounded border border-gray-300 px-2 text-sm"
            />
          </div>

          <div className="flex-1 min-w-[180px]">
            <label htmlFor="gameType" className="block text-sm font-medium mb-1">
              Spilltype
            </label>
            <select
              id="gameType"
              name="gameType"
              className="h-9 w-full rounded border border-gray-300 px-2 text-sm"
            >
              <option value="Standard">Standard</option>
              <option value="Simracing">Simracing</option>
            </select>
          </div>

          <div className="flex h-9 items-center gap-2">
            <input
              id="isOrganizersParticipating"
              name="isOrganizersParticipating"
              type="checkbox"
              className="h-4 w-4"
            />
            <label htmlFor="isOrganizersParticipating" className="text-sm">
              Arrangørene deltar
            </label>
          </div>

          <button
            type="submit"
            className="h-9 rounded bg-gray-900 px-3 text-sm text-white hover:bg-gray-700 disabled:opacity-50"
          >
            Opprett spill
          </button>
        </form>
      </section>

      {/* ---- Spilliste ---- */}
      <section className="border border-gray-200 rounded" aria-labelledby="games-list-title">
        {/* Listeoverskrift med teller */}
        <div className="flex items-center justify-between p-4 border-b border-gray-200">
          <h2 className="text-lg font-semibold" id="games-list-title">
            Spill i denne turneringen
          </h2>
          {sortedGames.length > 0 && (
            <span className="text-sm text-gray-500" aria-live="polite">
              {sortedGames.length}{" "}
              {sortedGames.length === 1 ? "spill" : "spill"}
            </span>
          )}
        </div>

        {sortedGames.length === 0 ? (
          /* Tom-tilstand — ingen spill ennå */
          <div className="p-4">
            <p className="text-gray-500">
              Ingen spill ennå. Bruk skjemaet ovenfor til å legge til det
              første spillet.
            </p>
          </div>
        ) : (
          <div>
            {sortedGames.map((game) => (
              <div
                key={game.id}
                className="flex items-center justify-between gap-4 p-4 border-b border-gray-200 last:border-b-0"
              >
                <div className="flex items-center gap-3 min-w-0">
                  <span className="text-sm font-medium">{game.name}</span>
                  <span className="text-sm text-gray-500">
                    {game.gameType}
                  </span>
                  {game.isDone && (
                    <span className="text-sm text-green-700">Ferdig</span>
                  )}
                </div>

                {/* Lenke til spillets detaljside */}
                <Link
                  href={`/admin/tournaments/${tournament.slug}/games/${game.id}`}
                  className="underline text-sm"
                >
                  Åpne
                </Link>
              </div>
            ))}
          </div>
        )}
      </section>
    </>
  );
}
