"use client";

// Feilgrense for hjem-siden — vises hvis siden kaster en uventet feil
// Merk: Error Boundary i Next.js må være en Client Component

type ErrorPageProps = {
  error: Error & { digest?: string };
  reset: () => void;
};

export default function Error({ error, reset }: ErrorPageProps) {
  return (
    <div className="mx-auto max-w-5xl px-4 py-8">
      <div className="border border-red-200 bg-red-50 p-3 rounded max-w-md">
        <h2 className="text-lg font-semibold mb-1">Kunne ikke laste turneringer</h2>
        <p className="text-sm text-red-600 mb-3">
          {error.message
            ? error.message
            : "En uventet feil oppstod. Prøv igjen om litt."}
        </p>
        <button
          onClick={reset}
          className="rounded bg-gray-900 px-3 py-1.5 text-sm text-white hover:bg-gray-700"
        >
          Prøv igjen
        </button>
      </div>
    </div>
  );
}
