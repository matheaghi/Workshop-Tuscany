import Link from "next/link";

// Rekorder — tilsvarer RecordResponse fra /api/v1/records og /api/v1/tournaments/:id/records
export type RecordHolder = { personId: string; firstName: string; lastName: string };

// Rekordverdien og alle som deler den. Tom liste når ingen har rekorden ennå.
export type RecordResponse = { value: number; holders: RecordHolder[] };

export type RecordKey =
  | "mostWins"
  | "longestWinStreak"
  | "currentWinStreak"
  | "mostSpectated"
  | "mostGamesPlayed"
  | "longestParticipationStreak"
  | "longestCurrentParticipationStreak";

// Entall for 1, flertall ellers
function count(value: number, singular: string, plural: string): string {
  return `${value} ${value === 1 ? singular : plural}`;
}

// Emoji, overskrift og hvordan verdien skrives, per rekord
const recordConfig: Record<RecordKey, { emoji: string; label: string; format: (value: number) => string }> = {
  mostWins: { emoji: "🏆", label: "Flest seire", format: (v) => count(v, "seier", "seire") },
  longestWinStreak: { emoji: "🔥", label: "Lengst seiersrekke", format: (v) => `${v} på rad` },
  currentWinStreak: { emoji: "⚡", label: "Nåværende seiersrekke", format: (v) => `${v} på rad` },
  mostSpectated: {
    emoji: "👀",
    label: "Mest lojale tilskuer",
    format: (v) => `${count(v, "spill", "spill")} som tilskuer`,
  },
  mostGamesPlayed: { emoji: "🎯", label: "Flest spill spilt", format: (v) => count(v, "spill", "spill") },
  longestParticipationStreak: {
    emoji: "📅",
    label: "Lengst deltakerrekke",
    format: (v) => `${count(v, "spill", "spill")} på rad`,
  },
  longestCurrentParticipationStreak: {
    emoji: "🔁",
    label: "Lengst pågående deltakerrekke",
    format: (v) => `${count(v, "spill", "spill")} på rad`,
  },
};

// «Kari, Ola og Per» — hvert navn lenker til spillersiden
function Holders({ holders }: { holders: RecordHolder[] }) {
  return (
    <>
      {holders.map((h, i) => (
        <span key={h.personId}>
          {i > 0 && (i === holders.length - 1 ? " og " : ", ")}
          <Link href={`/players/${h.personId}`} className="underline">
            {h.firstName} {h.lastName}
          </Link>
        </span>
      ))}
    </>
  );
}

// Liste med rekorder i rekkefølgen de sendes inn, én rad per rekord
export function RecordList({ records }: { records: { key: RecordKey; record: RecordResponse }[] }) {
  return (
    <ul className="grid gap-2 sm:grid-cols-2">
      {records.map(({ key, record }) => {
        const { emoji, label, format } = recordConfig[key];
        return (
          <li key={key} className="border border-gray-200 rounded p-4">
            <div className="text-sm text-gray-600">
              <span aria-hidden="true">{emoji}</span> {label}
            </div>
            {record.holders.length === 0 ? (
              <div className="text-gray-500">Ingen ennå</div>
            ) : (
              <div className="flex flex-wrap items-baseline justify-between gap-x-4">
                <span className="font-semibold">
                  <Holders holders={record.holders} />
                </span>
                <span className="text-sm text-gray-600">{format(record.value)}</span>
              </div>
            )}
          </li>
        );
      })}
    </ul>
  );
}
