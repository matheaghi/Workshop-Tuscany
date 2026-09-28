import type { Metadata } from "next";
import { notFound } from "next/navigation";
import Link from "next/link";

// Datamodell for turneringsdetaljer — tilsvarer API-respons fra /api/v1/tournaments/:slug
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

// Datamodell for scoreboard-innslag — en rad i rangeringslisten
type ScoreboardEntryResponse = {
  personId: string;
  firstName: string;
  lastName: string;
  totalPoints: number;
  rank: number;
};

// Henter turneringsdetaljer via slug. Returnerer null ved feil eller manglende ressurs.
async function getTournamentBySlug(
  slug: string
): Promise<TournamentDetailResponse | null> {
  try {
    const res = await fetch(
      `${process.env.API_BASE_URL ?? "http://localhost:5000"}/api/v1/tournaments/${slug}`,
      { cache: "no-store" }
    );
    if (!res.ok) return null;
    return res.json();
  } catch {
    return null;
  }
}

// Henter scoreboard for en gitt turnering. Returnerer tomt array ved feil.
async function getScoreboard(
  id: string
): Promise<ScoreboardEntryResponse[]> {
  try {
    const res = await fetch(
      `${process.env.API_BASE_URL ?? "http://localhost:5000"}/api/v1/tournaments/${id}/scoreboard`,
      { cache: "no-store" }
    );
    if (!res.ok) return [];
    return res.json();
  } catch {
    return [];
  }
}

// Dynamisk metadata basert på turneringsnavn — brukes av søkemotorer og sosiale medier
export async function generateMetadata({
  params,
}: {
  params: Promise<{ slug: string }>;
}): Promise<Metadata> {
  const { slug } = await params;
  const tournament = await getTournamentBySlug(slug);

  if (!tournament) {
    return { title: "Turnering ikke funnet" };
  }

  return {
    title: tournament.name,
    description: `Poengregler, scoreboard og resultater for ${tournament.name} i TrønderLeikan.`,
  };
}

// Poengregelkort — viser en enkelt poengkategori
function PointRuleCard({
  label,
  points,
}: {
  label: string;
  points: number;
}) {
  return (
    <div className="border border-gray-200 rounded p-4">
      <div className="text-sm text-gray-600">{label}</div>
      <div className="text-lg font-semibold">{points} poeng</div>
    </div>
  );
}

// Scoreboard-rad — én deltaker med rank, navn og poengsum
function ScoreboardRow({ entry }: { entry: ScoreboardEntryResponse }) {
  return (
    <tr className="border-b border-gray-200">
      <td className="py-2 pr-4">{entry.rank}</td>
      <td className="py-2 pr-4">
        {entry.firstName} {entry.lastName}
      </td>
      <td className="py-2 text-right">{entry.totalPoints}</td>
    </tr>
  );
}

// Turneringsdetalj-side — henter data server-side og rendrer poengregler og scoreboard
export default async function TournamentDetailPage({
  params,
}: {
  params: Promise<{ slug: string }>;
}) {
  const { slug } = await params;

  // Hent turnering — vis 404 hvis ikke funnet
  const tournament = await getTournamentBySlug(slug);
  if (!tournament) notFound();

  // Hent scoreboard parallelt med at turnerings-data allerede er tilgjengelig
  const scoreboard = await getScoreboard(tournament.id);

  const { pointRules } = tournament;

  // Poengregler konfigurert som liste for enkel rendering
  const pointRuleItems = [
    { label: "Deltaker", points: pointRules.participation },
    { label: "1. plass", points: pointRules.firstPlace },
    { label: "2. plass", points: pointRules.secondPlace },
    { label: "3. plass", points: pointRules.thirdPlace },
    {
      label: "Arrangør (med deltakelse)",
      points: pointRules.organizedWithParticipation,
    },
    {
      label: "Arrangør (uten deltakelse)",
      points: pointRules.organizedWithoutParticipation,
    },
    { label: "Tilskuer", points: pointRules.spectator },
  ];

  return (
    <div className="mx-auto max-w-5xl px-4 py-8">
      <div className="mb-4">
        <Link href="/" className="text-sm underline">
          Alle turneringer
        </Link>
      </div>

      <h1 className="text-2xl font-semibold mb-4">{tournament.name}</h1>

      <div className="grid gap-8 md:grid-cols-2">
        <section aria-labelledby="point-rules-heading">
          <h2 id="point-rules-heading" className="text-lg font-semibold mb-2">
            Poengregler
          </h2>

          <p className="text-sm text-gray-600 mb-4">
            Poeng for 1.–3. plass legges oppå deltakerpoengene. En spiller kan
            samle poeng fra flere spill i turneringen.
          </p>

          <div className="grid grid-cols-2 gap-2">
            {pointRuleItems.map((rule) => (
              <PointRuleCard
                key={rule.label}
                label={rule.label}
                points={rule.points}
              />
            ))}
          </div>
        </section>

        <section aria-labelledby="scoreboard-heading">
          <div className="flex items-center justify-between mb-2">
            <h2 id="scoreboard-heading" className="text-lg font-semibold">
              Scoreboard
            </h2>

            {scoreboard.length > 0 && (
              <span className="text-sm text-gray-600">
                {scoreboard.length} deltaker{scoreboard.length !== 1 ? "e" : ""}
              </span>
            )}
          </div>

          {scoreboard.length === 0 ? (
            <p className="text-gray-500">
              Ingen resultater ennå. Scoreboard oppdateres når spill er
              fullført.
            </p>
          ) : (
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-gray-200">
                  <th className="text-left font-medium py-2 pr-4">#</th>
                  <th className="text-left font-medium py-2 pr-4">Navn</th>
                  <th className="text-right font-medium py-2">Poeng</th>
                </tr>
              </thead>
              <tbody>
                {scoreboard
                  .slice()
                  .sort((a, b) => a.rank - b.rank)
                  .map((entry) => (
                    <ScoreboardRow key={entry.personId} entry={entry} />
                  ))}
              </tbody>
            </table>
          )}
        </section>
      </div>
    </div>
  );
}
