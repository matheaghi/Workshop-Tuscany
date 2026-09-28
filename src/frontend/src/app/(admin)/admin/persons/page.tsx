import type { Metadata } from "next";
import { createPersonAction, deletePersonAction } from "./actions";

// Datamodell for spilleroversikt — tilsvarer API-respons fra /api/v1/persons
type PersonSummaryResponse = {
  id: string;
  firstName: string;
  lastName: string;
  departmentId?: string;
  hasProfileImage: boolean;
};

// API-basis-URL — hentes fra miljøvariabel, kun tilgjengelig server-side
const API_BASE = process.env.API_BASE_URL ?? "http://localhost:5000";

// Henter alle spillere fra backend — returnerer tom liste ved feil slik at siden alltid rendres
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

export const metadata: Metadata = {
  title: "Spillere",
};

// Admin-side for spilleradministrasjon — CRUD for spillerregistre
export default async function AdminPersonsPage() {
  const persons = await getPersons();

  // Sorterer alfabetisk på etternavn, deretter fornavn
  const sorted = persons
    .slice()
    .sort(
      (a, b) =>
        a.lastName.localeCompare(b.lastName, "nb") ||
        a.firstName.localeCompare(b.firstName, "nb")
    );

  return (
    <div>
      {/* Sideoverskrift */}
      <header className="mb-6">
        <h1 className="text-2xl font-semibold mb-1">Spillere</h1>
        <p className="text-sm text-gray-600">
          Opprett, vis og slett spillerprofiler i TrønderLeikan.
        </p>
      </header>

      {/* ---- Opprett ny spiller ---- */}
      <section
        className="border border-gray-200 rounded p-4 mb-6"
        aria-labelledby="create-panel-title"
      >
        <h2 className="text-lg font-semibold mb-3" id="create-panel-title">
          Legg til ny spiller
        </h2>

        {/* Skjema bruker Server Action direkte — ingen klient-JS nødvendig */}
        <form
          action={createPersonAction}
          className="flex flex-wrap gap-3 items-end"
        >
          <div className="flex-1 min-w-[160px]">
            <label htmlFor="firstName" className="block text-sm font-medium mb-1">
              Fornavn
            </label>
            <input
              id="firstName"
              name="firstName"
              type="text"
              required
              placeholder="Ola"
              autoComplete="given-name"
              className="w-full rounded border border-gray-300 px-2 py-1.5 text-sm"
            />
          </div>

          <div className="flex-1 min-w-[160px]">
            <label htmlFor="lastName" className="block text-sm font-medium mb-1">
              Etternavn
            </label>
            <input
              id="lastName"
              name="lastName"
              type="text"
              required
              placeholder="Nordmann"
              autoComplete="family-name"
              className="w-full rounded border border-gray-300 px-2 py-1.5 text-sm"
            />
          </div>

          <button
            type="submit"
            className="rounded bg-gray-900 px-3 py-1.5 text-sm text-white hover:bg-gray-700 disabled:opacity-50"
          >
            Opprett spiller
          </button>
        </form>
      </section>

      {/* ---- Spillerliste ---- */}
      <section
        className="border border-gray-200 rounded"
        aria-labelledby="players-table-title"
      >
        {/* Tabelloverskrift med teller */}
        <div className="flex items-center justify-between px-4 py-3 border-b border-gray-200">
          <h2 className="text-lg font-semibold" id="players-table-title">
            Registrerte spillere
          </h2>
          {sorted.length > 0 && (
            <span className="text-sm text-gray-600" aria-live="polite">
              {sorted.length} {sorted.length === 1 ? "spiller" : "spillere"}
            </span>
          )}
        </div>

        {sorted.length === 0 ? (
          /* Tom-tilstand — ingen spillere ennå */
          <p className="p-4 text-gray-500">
            Ingen spillere registrert. Bruk skjemaet ovenfor til å legge til
            den første spilleren.
          </p>
        ) : (
          <div className="relative overflow-x-auto">
            <table className="w-full text-sm" aria-label="Spillerliste">
              <thead>
                <tr className="border-b border-gray-200">
                  <th scope="col" className="text-left font-medium px-4 py-2">
                    Fornavn
                  </th>
                  <th scope="col" className="text-left font-medium px-4 py-2">
                    Etternavn
                  </th>
                  <th scope="col" className="text-left font-medium px-4 py-2">
                    Bilde
                  </th>
                  <th scope="col" className="text-left font-medium px-4 py-2">
                    ID
                  </th>
                  <th scope="col" className="text-left font-medium px-4 py-2">
                    <span className="sr-only">Handlinger</span>
                  </th>
                </tr>
              </thead>
              <tbody>
                {sorted.map((person) => (
                  <tr key={person.id} className="border-b border-gray-200">
                    {/* Fornavn */}
                    <td className="px-4 py-2">{person.firstName}</td>

                    {/* Etternavn */}
                    <td className="px-4 py-2">{person.lastName}</td>

                    {/* Profilbilde-status */}
                    <td className="px-4 py-2">
                      {person.hasProfileImage ? (
                        <span aria-label="Bilde lastet opp">Ja</span>
                      ) : (
                        <span aria-label="Ingen bilde">Nei</span>
                      )}
                    </td>

                    {/* Spiller-ID — forkortet for lesbarhet */}
                    <td className="px-4 py-2 text-gray-500" title={person.id}>
                      {person.id.slice(0, 8)}…
                    </td>

                    {/* Slett-knapp via inline Server Action i form */}
                    <td className="px-4 py-2">
                      <form
                        action={async () => {
                          "use server";
                          await deletePersonAction(person.id);
                        }}
                      >
                        <button
                          type="submit"
                          className="rounded border border-gray-300 px-3 py-1.5 text-sm text-red-600 hover:bg-gray-100"
                          aria-label={`Slett ${person.firstName} ${person.lastName}`}
                        >
                          Slett
                        </button>
                      </form>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>
    </div>
  );
}
