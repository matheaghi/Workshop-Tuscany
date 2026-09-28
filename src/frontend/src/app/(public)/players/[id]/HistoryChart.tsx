"use client";

import { useEffect, useRef, useState } from "react";

// Ett punkt i diagrammet. tick er den korte aksteksten, label og valueText vises i verktøytipset.
export type ChartPoint = {
  key: string;
  label: string;
  tick: string;
  value: number;
  valueText: string;
};

// Linjefargen er validert mot hvit bakgrunn (lyshet, metning og kontrast ≥ 3:1)
const LINE_COLOR = "#2a78d6";
const HEIGHT = 200;
const MARGIN = { top: 28, right: 16, bottom: 28, left: 32 };
// Avstand fra plottkanten til første og siste punkt, så prikkene ikke kuttes
const INSET = 12;

// Runde heltall på y-aksen, omtrent fire steg
function niceStep(range: number): number {
  return Math.max(1, Math.ceil(range / 4));
}

// Linjediagram med én serie, tegnet som SVG. invert=true snur y-aksen (brukes for plassering, der 1 er best).
export function HistoryChart({
  title,
  points,
  invert = false,
}: {
  title: string;
  points: ChartPoint[];
  invert?: boolean;
}) {
  const wrapperRef = useRef<HTMLDivElement>(null);
  const [width, setWidth] = useState<number | null>(null);
  const [active, setActive] = useState<number | null>(null);

  // Mål bredden, så teksten holder samme størrelse på mobil i stedet for å skaleres ned
  useEffect(() => {
    const el = wrapperRef.current;
    if (!el) return;
    const observer = new ResizeObserver(([entry]) => setWidth(entry.contentRect.width));
    observer.observe(el);
    return () => observer.disconnect();
  }, []);

  const values = points.map((p) => p.value);

  // Y-domene: poeng fra 0, plassering fra 1 (øverst) til dårligste plassering
  const step = invert
    ? niceStep(Math.max(...values) - 1)
    : niceStep(Math.max(...values, 1));
  const min = invert ? 1 : 0;
  const max = invert
    ? Math.max(1 + step, 1 + step * Math.ceil((Math.max(...values) - 1) / step))
    : step * Math.ceil(Math.max(...values, 1) / step);
  const yTicks: number[] = [];
  for (let v = min; v <= max; v += step) yTicks.push(v);

  return (
    <figure className="my-4">
      <figcaption className="text-sm font-medium mb-2">{title}</figcaption>
      <div ref={wrapperRef} className="relative" style={{ height: HEIGHT }}>
        {width !== null && width > 0 && (
          <Plot
            width={width}
            title={title}
            points={points}
            invert={invert}
            min={min}
            max={max}
            yTicks={yTicks}
            active={active}
            setActive={setActive}
          />
        )}
      </div>
    </figure>
  );
}

function Plot({
  width,
  title,
  points,
  invert,
  min,
  max,
  yTicks,
  active,
  setActive,
}: {
  width: number;
  title: string;
  points: ChartPoint[];
  invert: boolean;
  min: number;
  max: number;
  yTicks: number[];
  active: number | null;
  setActive: (i: number | null) => void;
}) {
  const plotLeft = MARGIN.left;
  const plotRight = width - MARGIN.right;
  const plotTop = MARGIN.top;
  const plotBottom = HEIGHT - MARGIN.bottom;

  const x = (i: number) =>
    points.length === 1
      ? (plotLeft + plotRight) / 2
      : plotLeft + INSET + (i * (plotRight - plotLeft - 2 * INSET)) / (points.length - 1);
  const y = (v: number) => {
    const t = (v - min) / (max - min);
    return invert ? plotTop + t * (plotBottom - plotTop) : plotBottom - t * (plotBottom - plotTop);
  };

  // Tynn ut akstekstene hvis de ville kollidert; siste punkt får alltid tekst
  const widest = Math.max(...points.map((p) => p.tick.length * 7 + 12));
  const every = Math.max(1, Math.ceil((points.length * widest) / (plotRight - plotLeft)));
  const showTick = (i: number) => i === points.length - 1 || (i % every === 0 && points.length - 1 - i >= every);

  const path = points.map((p, i) => `${i === 0 ? "M" : "L"}${x(i)},${y(p.value)}`).join(" ");
  const last = points.length - 1;

  // Finn nærmeste punkt langs x-aksen — leseren sikter på en posisjon, ikke på en 2px linje
  function nearest(clientX: number, svg: SVGSVGElement): number {
    const px = clientX - svg.getBoundingClientRect().left;
    let best = 0;
    for (let i = 1; i < points.length; i++) {
      if (Math.abs(x(i) - px) < Math.abs(x(best) - px)) best = i;
    }
    return best;
  }

  const activePoint = active !== null ? points[active] : null;

  return (
    <>
      <svg
        width={width}
        height={HEIGHT}
        role="group"
        aria-label={title}
        className="overflow-visible"
        onPointerMove={(e) => setActive(nearest(e.clientX, e.currentTarget))}
        onPointerLeave={() => setActive(null)}
      >
        {/* Rutenett og y-akse: hårfine, heltrukne og tilbaketrukne */}
        {yTicks.map((v) => (
          <g key={v}>
            <line x1={plotLeft} x2={plotRight} y1={y(v)} y2={y(v)} stroke="#e5e7eb" strokeWidth={1} />
            <text
              x={plotLeft - 8}
              y={y(v)}
              textAnchor="end"
              dominantBaseline="middle"
              className="fill-gray-500 text-xs tabular-nums"
            >
              {invert ? `${v}.` : v}
            </text>
          </g>
        ))}

        {/* X-akse-tekster: første venstrejustert, siste høyrejustert, resten sentrert */}
        {points.map(
          (p, i) =>
            showTick(i) && (
              <text
                key={p.key}
                x={x(i)}
                y={HEIGHT - 8}
                textAnchor={points.length > 1 && i === 0 ? "start" : i === last && points.length > 1 ? "end" : "middle"}
                dx={points.length > 1 && i === 0 ? -INSET : i === last && points.length > 1 ? INSET : 0}
                className="fill-gray-500 text-xs"
              >
                {p.tick}
              </text>
            )
        )}

        {/* Siktelinje som følger pekeren */}
        {active !== null && (
          <line x1={x(active)} x2={x(active)} y1={plotTop} y2={plotBottom} stroke="#d1d5db" strokeWidth={1} />
        )}

        <path d={path} fill="none" stroke={LINE_COLOR} strokeWidth={2} strokeLinejoin="round" strokeLinecap="round" />

        {points.map((p, i) => (
          <g key={p.key}>
            {/* Synlig prikk med hvit ring, så den skiller seg fra linjen */}
            <circle cx={x(i)} cy={y(p.value)} r={active === i ? 6 : 4} fill={LINE_COLOR} stroke="#ffffff" strokeWidth={2} />
            {/* Større, usynlig treffområde som også kan nås med tastaturet */}
            <circle
              cx={x(i)}
              cy={y(p.value)}
              r={12}
              fill="transparent"
              tabIndex={0}
              role="img"
              aria-label={`${p.label}: ${p.valueText}`}
              className="outline-none focus-visible:stroke-gray-900 focus-visible:[stroke-width:2]"
              onFocus={() => setActive(i)}
              onBlur={() => setActive(null)}
            />
          </g>
        ))}

        {/* Direkte etikett bare på siste punkt */}
        <text
          x={x(last)}
          y={y(points[last].value) - 12}
          textAnchor={points.length > 1 ? "end" : "middle"}
          dx={points.length > 1 ? 6 : 0}
          className="fill-gray-900 text-xs font-semibold"
        >
          {points[last].valueText}
        </text>
      </svg>

      {/* Verktøytips: verdien først, beskrivelsen etter */}
      {activePoint && active !== null && (
        <div
          className="pointer-events-none absolute z-10 rounded border border-gray-200 bg-white px-2 py-1 text-xs shadow-sm whitespace-nowrap"
          style={{
            left: Math.min(Math.max(x(active), 80), width - 80),
            top: y(activePoint.value) - 12,
            transform: "translate(-50%, -100%)",
          }}
        >
          <div className="font-semibold text-gray-900">{activePoint.valueText}</div>
          <div className="text-gray-600">{activePoint.label}</div>
        </div>
      )}
    </>
  );
}
