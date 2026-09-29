import Link from "next/link";

// Vises når spilleren ikke finnes (notFound() i profilsiden)
export default function PlayerNotFound() {
  return (
    <div className="mx-auto max-w-5xl px-4 py-8">
      <h1 className="text-2xl font-semibold mb-4">Fant ikke spilleren</h1>
      <p className="text-gray-500 mb-4">Spilleren finnes ikke, eller er fjernet.</p>
      <Link
        href="/players"
        className="inline-flex min-h-11 items-center -my-3 text-sm underline focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2"
      >
        Alle spillere
      </Link>
    </div>
  );
}
