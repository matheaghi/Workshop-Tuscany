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

  // Hent spiller — vis 404 hvis ikke funnet
  const person = await getPersonById(id);
  if (!person) notFound();

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
    </div>
  );
}
