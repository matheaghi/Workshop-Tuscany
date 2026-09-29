"use client";

// Feilgrense for spillerlisten og spillerprofilene — vises hvis siden kaster en uventet feil
// Merk: Error Boundary i Next.js må være en Client Component

type ErrorPageProps = {
  error: Error & { digest?: string };
  retry: () => void;
};

export default function Error({ retry }: ErrorPageProps) {
  return (
    <div className="mx-auto max-w-5xl px-4 py-8">
      <div className="border border-red-200 bg-red-50 p-3 rounded max-w-md">
        <h2 className="text-lg font-semibold mb-1">Kunne ikke laste spillere</h2>
        <p className="text-sm text-red-600 mb-3">En uventet feil oppstod. Prøv igjen om litt.</p>
        <button
          onClick={() => retry()}
          className="rounded bg-gray-900 px-3 py-1.5 text-sm text-white hover:bg-gray-700 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2"
        >
          Prøv igjen
        </button>
      </div>
    </div>
  );
}
