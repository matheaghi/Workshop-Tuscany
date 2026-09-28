// Lasteskjerm for spillerliste — vises av Next.js mens server-side data hentes.

export default function Loading() {
  return (
    <div className="mx-auto max-w-5xl px-4 py-8">
      <div className="animate-pulse space-y-4">
        <div className="bg-gray-100 rounded h-8 w-40" />
        <div className="bg-gray-100 rounded h-4 w-64 max-w-full" />
        <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-6 gap-4 mt-4">
          {Array.from({ length: 12 }).map((_, i) => (
            <div key={i} className="border border-gray-200 rounded p-4 flex flex-col items-center gap-2">
              <div className="bg-gray-100 rounded-full h-16 w-16" />
              <div className="bg-gray-100 rounded h-4 w-3/4" />
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}
