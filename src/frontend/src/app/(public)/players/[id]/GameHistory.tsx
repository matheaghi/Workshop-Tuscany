"use client";

import { useState } from "react";
import Link from "next/link";
import { formatPlayedOn, formatRoles, type HistoryGame } from "./history";
import { HistoryChart, type ChartPoint } from "./HistoryChart";

type TournamentOption = { slug: string; name: string; rank: number };

const shortDate = new Intl.DateTimeFormat("nb-NO", {
  day: "numeric",
  month: "short",
  timeZone: "UTC",
});

// Poeng per spill i én turnering, eldste først. Spill uten dato havner sist; samme dag sorteres på navn.
function pointsPerGame(games: HistoryGame[]): ChartPoint[] {
  return [...games]
    .sort((a, b) => {
      if (a.playedOn !== b.playedOn) {
        if (a.playedOn === null) return 1;
        if (b.playedOn === null) return -1;
        return a.playedOn.localeCompare(b.playedOn);
      }
      return a.name.localeCompare(b.name, "nb");
    })
    .map((g) => ({
      key: g.gameId,
      label: `${g.name} · ${formatPlayedOn(g.playedOn)}`,
      tick: g.playedOn ? shortDate.format(new Date(g.playedOn)) : "Uten dato",
      value: g.points.total,
      valueText: `${g.points.total} poeng`,
    }));
}

// Plassering per turnering, eldste først (tournaments kommer med nyeste først)
function rankPerTournament(tournaments: TournamentOption[]): ChartPoint[] {
  return [...tournaments].reverse().map((t) => ({
    key: t.slug,
    label: t.name,
    tick: t.name,
    value: t.rank,
    valueText: `${t.rank}. plass`,
  }));
}

// Én rad i historikken: dato, spill, turnering, rolle, plassering og poeng
function HistoryRow({ game }: { game: HistoryGame }) {
  return (
    <li className="flex flex-wrap items-baseline justify-between gap-x-4 gap-y-1 border-b border-gray-200 py-3">
      <div className="min-w-0">
        <div className="text-sm text-gray-600">
          {game.playedOn ? (
            <time dateTime={game.playedOn}>{formatPlayedOn(game.playedOn)}</time>
          ) : (
            formatPlayedOn(null)
          )}
        </div>
        <Link
          href={`/tournaments/${game.tournamentSlug}/games/${game.gameId}`}
          className="font-medium underline"
        >
          {game.name}
        </Link>
        <div className="text-sm text-gray-600">
          {game.tournamentName} · {formatRoles(game.roles)}
        </div>
      </div>

      <div className="text-right">
        {game.placement !== null && (
          <div className="text-sm font-medium">{game.placement}. plass</div>
        )}
        <div className="text-lg font-semibold">{game.points.total} poeng</div>
      </div>
    </li>
  );
}

// Spillhistorikk med nedtrekksliste for å vise spillene fra én turnering, eller «Alle».
// games er sortert med nyeste først, tournaments med nyeste turnering først.
export function GameHistory({
  games,
  tournaments,
}: {
  games: HistoryGame[];
  tournaments: TournamentOption[];
}) {
  // Tom streng betyr «Alle»
  const [selected, setSelected] = useState("");

  const shown =
    selected === "" ? games : games.filter((g) => g.tournamentSlug === selected);

  // Diagrammet trenger minst to punkter for å vise en utvikling
  const selectedName = tournaments.find((t) => t.slug === selected)?.name;
  const chart =
    selected === ""
      ? tournaments.length > 1 && {
          title: "Plassering per turnering",
          points: rankPerTournament(tournaments),
          invert: true,
        }
      : shown.length > 1 && {
          title: `Poeng per spill i ${selectedName}`,
          points: pointsPerGame(shown),
          invert: false,
        };

  return (
    <section aria-labelledby="history-heading" className="mt-8">
      <div className="flex flex-wrap items-center justify-between gap-2 mb-2">
        <h2 id="history-heading" className="text-lg font-semibold">
          Spillhistorikk
        </h2>

        <div className="flex items-center gap-3">
          {/* Med bare én turnering viser «Alle» og turneringen det samme, så filteret utelates */}
          {tournaments.length > 1 && (
            <label className="flex items-center gap-2 text-sm">
              Turnering
              <select
                value={selected}
                onChange={(e) => setSelected(e.target.value)}
                className="border border-gray-300 rounded px-2 py-1"
              >
                <option value="">Alle</option>
                {tournaments.map((t) => (
                  <option key={t.slug} value={t.slug}>
                    {t.name}
                  </option>
                ))}
              </select>
            </label>
          )}

          {shown.length > 0 && (
            <span className="text-sm text-gray-600">{shown.length} spill</span>
          )}
        </div>
      </div>

      {chart && (
        <HistoryChart
          // Ny key per valg, så pekertilstanden nullstilles når turneringen byttes
          key={selected}
          title={chart.title}
          points={chart.points}
          invert={chart.invert}
        />
      )}

      {shown.length === 0 ? (
        <p className="text-gray-500">Ingen ferdige spill ennå.</p>
      ) : (
        <ol>
          {shown.map((game) => (
            <HistoryRow key={game.gameId} game={game} />
          ))}
        </ol>
      )}
    </section>
  );
}
