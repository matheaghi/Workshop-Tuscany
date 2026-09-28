import type { Metadata } from "next";
import Link from "next/link";
import { createTournamentAction } from "./actions";

// Datamodell for turneringsoversikt — tilsvarer API-respons fra /api/v1/tournaments
type TournamentSummaryResponse = {
  id: string;
  name: string;
  slug: string;
};

// API-basis-URL — hentes fra miljøvariabel, kun tilgjengelig server-side
const API_BASE = process.env.API_BASE_URL ?? "http://localhost:5000";

// Henter alle turneringer fra backend — returnerer tom liste ved feil slik at siden alltid rendres
async function getTournaments(): Promise<TournamentSummaryResponse[]> {
  try {
    const res = await fetch(`${API_BASE}/api/v1/tournaments`, {
      cache: "no-store",
    });
    if (!res.ok) return [];
    return res.json();
  } catch {
    return [];
  }
}

export const metadata: Metadata = {
  title: "Turneringer",
};

// Admin-side for turneringsadministrasjon — liste og opprett
export default async function AdminTournamentsPage() {
  const tournaments = await getTournaments();

  // Sorterer alfabetisk på navn
  const sorted = tournaments
    .slice()
    .sort((a, b) => a.name.localeCompare(b.name, "nb"));

  return (
    <>
      {/* Sideoverskrift */}
      <header className="mb-6">
        <h1 className="text-2xl font-semibold mb-1">Turneringer</h1>
        <p className="text-sm text-gray-600">
          Opprett og administrer turneringer og poengregler i TrønderLeikan.
        </p>
      </header>

      {/* ---- Opprett ny turnering ---- */}
      <section
        className="border border-gray-200 rounded p-4 mb-6"
        aria-labelledby="create-panel-title"
      >
        <h2 className="text-lg font-semibold mb-3" id="create-panel-title">
          Opprett ny turnering
        </h2>

        {/* Skjema bruker Server Action direkte — ingen klient-JS nødvendig */}
        <form
          action={createTournamentAction}
          className="flex flex-wrap gap-3 items-end"
        >
          <div className="flex-1 min-w-[220px]">
            <label htmlFor="name" className="block text-sm font-medium mb-1">
              Navn
            </label>
            <input
              id="name"
              name="name"
              type="text"
              required
              placeholder="TrønderLeikan 2026"
              className="w-full rounded border border-gray-300 px-2 py-1.5 text-sm"
            />
          </div>

          <button
            type="submit"
            className="rounded bg-gray-900 px-3 py-1.5 text-sm text-white hover:bg-gray-700 disabled:opacity-50"
          >
            Opprett turnering
          </button>
        </form>
      </section>

      {/* ---- Turneringsliste ---- */}
      <section
        className="border border-gray-200 rounded"
        aria-labelledby="tournaments-table-title"
      >
        {/* Tabelloverskrift med teller */}
        <div className="flex items-center justify-between p-4 border-b border-gray-200">
          <h2 className="text-lg font-semibold" id="tournaments-table-title">
            Registrerte turneringer
          </h2>
          {sorted.length > 0 && (
            <span className="text-sm text-gray-500" aria-live="polite">
              {sorted.length}{" "}
              {sorted.length === 1 ? "turnering" : "turneringer"}
            </span>
          )}
        </div>

        {sorted.length === 0 ? (
          /* Tom-tilstand — ingen turneringer ennå */
          <div className="p-4">
            <p className="text-gray-500">
              Ingen turneringer registrert. Bruk skjemaet ovenfor til å
              opprette den første turneringen.
            </p>
          </div>
        ) : (
          <div className="relative overflow-x-auto">
            <table
              className="w-full text-sm"
              aria-label="Turneringsliste"
            >
              <thead>
                <tr className="border-b border-gray-200">
                  <th scope="col" className="text-left font-medium p-4">
                    Navn
                  </th>
                  <th scope="col" className="text-left font-medium p-4">
                    Slug
                  </th>
                  <th scope="col" className="text-left font-medium p-4">
                    ID
                  </th>
                  <th scope="col" className="text-left font-medium p-4">
                    <span className="sr-only">Handlinger</span>
                  </th>
                </tr>
              </thead>
              <tbody>
                {sorted.map((tournament) => (
                  <tr key={tournament.id} className="border-b border-gray-200">
                    {/* Turneringsnavn */}
                    <td className="p-4">{tournament.name}</td>

                    {/* Slug — brukes i offentlig URL */}
                    <td className="p-4 text-gray-500">{tournament.slug}</td>

                    {/* Turnerings-ID — forkortet for lesbarhet */}
                    <td className="p-4 text-gray-500" title={tournament.id}>
                      {tournament.id.slice(0, 8)}…
                    </td>

                    {/* Åpne-lenke til detaljside */}
                    <td className="p-4">
                      <Link
                        href={`/admin/tournaments/${tournament.slug}`}
                        className="underline"
                      >
                        Åpne
                      </Link>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>
    </>
  );
}
