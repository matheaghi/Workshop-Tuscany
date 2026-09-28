import type { Metadata } from "next";
import Link from "next/link";

// Datamodell for turnering — tilsvarer API-respons fra /api/v1/tournaments
type TournamentSummaryResponse = {
  id: string;
  name: string;
  slug: string;
};

// Henter turneringer fra backend. Returnerer tomt array ved utilgjengelighet,
// slik at siden alltid rendres — selv uten API-tilkobling under utvikling.
async function getTournaments(): Promise<TournamentSummaryResponse[]> {
  try {
    const res = await fetch(
      `${process.env.API_BASE_URL ?? "http://localhost:5000"}/api/v1/tournaments`,
      { cache: "no-store" }
    );
    if (!res.ok) return [];
    return res.json();
  } catch {
    return [];
  }
}

export const metadata: Metadata = {
  title: "Turneringer",
  description:
    "Oversikt over alle turneringer i TrønderLeikan. Følg med på rangeringer og resultater.",
};

// Turneringskort — lenker til turneringsdetaljsiden
function TournamentCard({ tournament }: { tournament: TournamentSummaryResponse }) {
  return (
    <Link
      href={`/tournaments/${tournament.slug}`}
      className="block border border-gray-200 rounded p-4 hover:underline"
    >
      <h2 className="text-lg font-semibold">{tournament.name}</h2>
    </Link>
  );
}

// Tom-tilstand — vises når ingen turneringer er registrert ennå
function EmptyState() {
  return (
    <p className="text-gray-500">
      Ingen turneringer ennå. Turneringer vil dukke opp her når de er opprettet.
    </p>
  );
}

// Hjem-side — Server Component som henter turneringer og viser kortgrid
export default async function HomePage() {
  const tournaments = await getTournaments();

  return (
    <div className="mx-auto max-w-5xl px-4 py-8">
      <h1 className="text-2xl font-semibold mb-4">TrønderLeikan</h1>
      <p className="text-sm text-gray-600 mb-4">
        Plattform for turneringsstyring og poengberegning i Trøndelag.
      </p>

      {tournaments.length === 0 ? (
        <EmptyState />
      ) : (
        <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 gap-4">
          {tournaments.map((tournament) => (
            <TournamentCard key={tournament.id} tournament={tournament} />
          ))}
        </div>
      )}
    </div>
  );
}
