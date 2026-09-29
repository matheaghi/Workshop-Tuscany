"use client";

import { useId, useState } from "react";
import Link from "next/link";
import {
  formatPlayedOn,
  formatRoles,
  formatSummaryReason,
  reasonLabels,
  type HistoryGame,
  type PointSummary,
} from "./history";
import { HistoryChart, type ChartPoint } from "./HistoryChart";

type TournamentOption = {
  slug: string;
  name: string;
  rank: number;
  totalPoints: number;
  pointsSummary: PointSummary[];
};

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

// Én rad i historikken: dato, spill, turnering, rolle, plassering og poeng.
// «?»-knappen viser hvordan poengsummen er satt sammen.
function HistoryRow({ game }: { game: HistoryGame }) {
  const [open, setOpen] = useState(false);
  const explanationId = useId();
  const { total, lines } = game.points;

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
          className="font-medium underline focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2"
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
        <div className="flex items-center justify-end gap-2">
          <span className="text-lg font-semibold">{total} poeng</span>
          <button
            type="button"
            onClick={() => setOpen((o) => !o)}
            aria-expanded={open}
            aria-controls={explanationId}
            aria-label={`Hvorfor ${total} poeng?`}
            // Treffområdet er 44px, men sirkelen er fortsatt 24px; negativ marg holder raden like høy.
            // Fokusringen tegnes rundt sirkelen, der den var før.
            className="group -m-2.5 inline-flex size-11 items-center justify-center outline-none"
          >
            <span
              aria-hidden="true"
              className="inline-flex size-6 items-center justify-center rounded-full border border-gray-400 text-sm font-semibold leading-none text-gray-700 group-hover:bg-gray-100 group-focus-visible:outline group-focus-visible:outline-2 group-focus-visible:outline-offset-2"
            >
              ?
            </span>
          </button>
        </div>
      </div>

      {/* Alltid i DOM-en, så aria-controls peker på noe også når forklaringen er lukket */}
      <div id={explanationId} hidden={!open} className="basis-full rounded bg-gray-50 px-3 py-2 text-sm">
        <p>
          {lines.length === 0
            ? "Ingen poeng i dette spillet."
            : `${lines.map((l) => `${reasonLabels[l.reason]} ${l.points}`).join(" + ")} = ${total} poeng`}
        </p>
        <Link
          href={`/tournaments/${game.tournamentSlug}`}
          className="underline text-gray-600 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2"
        >
          Se poengreglene i {game.tournamentName}
        </Link>
      </div>
    </li>
  );
}

// Sammenlagt for én turnering: hver grunn med antall og poeng, og totalen som gir plasseringen
function TournamentSummary({ tournament }: { tournament: TournamentOption }) {
  return (
    <section
      aria-label={`Poengene i ${tournament.name}`}
      className="mb-4 border border-gray-200 rounded p-4"
    >
      <h3 className="font-semibold mb-2">Slik ble poengene i {tournament.name}</h3>
      <dl className="text-sm">
        {tournament.pointsSummary.map((s) => (
          <div key={s.reason} className="flex justify-between gap-4 py-0.5">
            <dt>{formatSummaryReason(s)}</dt>
            <dd>{s.points} poeng</dd>
          </div>
        ))}
        <div className="flex justify-between gap-4 border-t border-gray-200 mt-1 pt-1 font-semibold">
          <dt>Totalt</dt>
          <dd>
            {tournament.totalPoints} poeng · {tournament.rank}. plass
          </dd>
        </div>
      </dl>
      <Link
        href={`/tournaments/${tournament.slug}`}
        className="text-sm underline text-gray-600 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2"
      >
        Se poengreglene
      </Link>
    </section>
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

  // Med bare én turnering er den alltid «valgt», siden nedtrekkslisten da er skjult
  const selectedTournament =
    tournaments.length === 1 ? tournaments[0] : tournaments.find((t) => t.slug === selected);

  // Diagrammet trenger minst to punkter for å vise en utvikling
  const selectedName = selectedTournament?.name;
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
                className="min-h-11 border border-gray-300 rounded px-3 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2"
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

          {/* Leses opp når filteret endres, så skjermleserbrukere hører at listen er byttet */}
          <span aria-live="polite" className="text-sm text-gray-600">
            {shown.length} spill
          </span>
        </div>
      </div>

      {selectedTournament && <TournamentSummary tournament={selectedTournament} />}

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
