import type { Metadata } from "next";
import { RecordList, type RecordKey, type RecordResponse } from "@/components/RecordList";

// Datamodell for rekorder — tilsvarer API-respons fra /api/v1/records
type RecordsResponse = {
  mostWins: RecordResponse;
  longestWinStreak: RecordResponse;
  currentWinStreak: RecordResponse;
  mostSpectated: RecordResponse;
  mostGamesPlayed: RecordResponse;
  longestParticipationStreak: RecordResponse;
  longestCurrentParticipationStreak: RecordResponse;
};

// Henter rekorder på tvers av alle turneringer. Returnerer null ved feil.
async function getRecords(): Promise<RecordsResponse | null> {
  try {
    const res = await fetch(
      `${process.env.API_BASE_URL ?? "http://localhost:5000"}/api/v1/records`,
      { cache: "no-store" }
    );
    if (!res.ok) return null;
    return res.json();
  } catch {
    return null;
  }
}

export const metadata: Metadata = {
  title: "Rekorder",
  description: "Rekorder på tvers av alle turneringer i TrønderLeikan.",
};

// Rekkefølgen rekordene vises i på siden
const recordOrder: RecordKey[] = [
  "mostWins",
  "longestWinStreak",
  "currentWinStreak",
  "mostGamesPlayed",
  "mostSpectated",
  "longestParticipationStreak",
  "longestCurrentParticipationStreak",
];

// Rekorder-side — henter data server-side og viser alle rekorder i en liste
export default async function RekorderPage() {
  const records = await getRecords();

  return (
    <div className="mx-auto max-w-5xl px-4 py-8">
      <h1 className="text-2xl font-semibold mb-4">Rekorder</h1>
      <p className="text-sm text-gray-600 mb-4">
        Rekorder på tvers av alle turneringer.
      </p>

      {records === null ? (
        <p className="text-gray-500">
          Klarte ikke å hente rekorder. Prøv igjen senere.
        </p>
      ) : (
        <RecordList
          records={recordOrder.map((key) => ({ key, record: records[key] }))}
        />
      )}
    </div>
  );
}
