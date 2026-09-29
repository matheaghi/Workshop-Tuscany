import type { Metadata } from "next";
import Link from "next/link";

// Datamodell for spilleroversikt — tilsvarer API-respons fra /api/v1/persons
type PersonSummaryResponse = {
  id: string;
  firstName: string;
  lastName: string;
  departmentId?: string;
  hasProfileImage: boolean;
};

// Henter alle spillere fra backend. Returnerer null ved feil, slik at siden kan skille
// mellom «ingen spillere» og «klarte ikke å hente».
async function getPersons(): Promise<PersonSummaryResponse[] | null> {
  try {
    const res = await fetch(
      `${process.env.API_BASE_URL ?? "http://localhost:5000"}/api/v1/persons`,
      { cache: "no-store" }
    );
    if (!res.ok) return null;
    return res.json();
  } catch {
    return null;
  }
}

export const metadata: Metadata = {
  title: "Spillere",
  description:
    "Oversikt over alle spillere i TrønderLeikan. Se profiler og resultater.",
};

// Henter initialer fra for- og etternavn for avatar-fallback
function getInitials(firstName: string, lastName: string): string {
  return `${firstName.charAt(0)}${lastName.charAt(0)}`.toUpperCase();
}

// Initialer-avatar — vises når hasProfileImage er false
function InitialsAvatar({
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
      className="w-16 h-16 rounded-full border border-gray-200 bg-gray-100 flex items-center justify-center text-base font-semibold text-gray-600 shrink-0"
    >
      {initials}
    </div>
  );
}

// Spillerkort — lenker til profil og viser profilbilde eller initialer.
// Bildet har tom alt, siden navnet står rett under og ellers ville blitt lest opp to ganger.
function PlayerCard({ person }: { person: PersonSummaryResponse }) {
  return (
    <Link
      href={`/players/${person.id}`}
      className="flex h-full flex-col items-center gap-2 p-4 border border-gray-200 rounded hover:underline focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2"
    >
      {person.hasProfileImage ? (
        /* eslint-disable-next-line @next/next/no-img-element */
        <img
          src={`/api/v1/persons/${person.id}/image`}
          alt=""
          width={64}
          height={64}
          className="w-16 h-16 rounded-full object-cover border border-gray-200"
        />
      ) : (
        <InitialsAvatar firstName={person.firstName} lastName={person.lastName} />
      )}

      <p className="text-sm font-medium text-gray-900 text-center break-words w-full">
        {person.firstName} {person.lastName}
      </p>
    </Link>
  );
}

// Tom-tilstand — vises når ingen spillere er registrert ennå
function EmptyState() {
  return (
    <p className="text-gray-500">
      Ingen spillere ennå. Spillerprofiler vil dukke opp her når de er registrert.
    </p>
  );
}

// Spillerliste-side — henter data server-side og viser responsivt kortgrid
export default async function PlayersPage() {
  const persons = await getPersons();

  // Sorter alfabetisk på etternavn, deretter fornavn
  const sorted = (persons ?? [])
    .slice()
    .sort(
      (a, b) =>
        a.lastName.localeCompare(b.lastName, "nb") ||
        a.firstName.localeCompare(b.firstName, "nb")
    );

  return (
    <div className="mx-auto max-w-5xl px-4 py-8">
      <h1 className="text-2xl font-semibold mb-4">Spillere</h1>
      <p className="text-sm text-gray-600 mb-4">
        {sorted.length > 0
          ? `${sorted.length} spiller${sorted.length !== 1 ? "e" : ""} registrert i TrønderLeikan.`
          : "Registrerte spillere i TrønderLeikan vises her."}
      </p>

      {persons === null ? (
        <p className="text-gray-500">
          Klarte ikke å hente spillere. Prøv igjen senere.
        </p>
      ) : sorted.length === 0 ? (
        <EmptyState />
      ) : (
        <ul className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-6 gap-4">
          {sorted.map((person) => (
            <li key={person.id}>
              <PlayerCard person={person} />
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
