// Lasteskjerm — vises av Next.js mens hjem-siden henter data server-side

export default function Loading() {
  return (
    <div className="mx-auto max-w-5xl px-4 py-8">
      <div className="animate-pulse space-y-4">
        <div className="bg-gray-100 rounded h-8 w-64" />
        <div className="bg-gray-100 rounded h-4 w-96 max-w-full" />
        <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 gap-4 mt-4">
          {Array.from({ length: 6 }).map((_, i) => (
            <div key={i} className="border border-gray-200 rounded p-4">
              <div className="bg-gray-100 rounded h-4 w-3/4" />
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}
