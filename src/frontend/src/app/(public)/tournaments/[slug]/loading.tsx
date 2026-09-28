// Lasteskjerm for turneringsdetaljsiden — vises mens server-side data hentes.

export default function Loading() {
  return (
    <div className="mx-auto max-w-5xl px-4 py-8">
      <div className="animate-pulse space-y-6">
        <div className="h-4 w-32 bg-gray-100 rounded" />
        <div className="h-8 w-64 bg-gray-100 rounded" />
        <div className="grid gap-8 md:grid-cols-2">
          <div className="space-y-2">
            <div className="h-4 w-24 bg-gray-100 rounded" />
            {Array.from({ length: 4 }).map((_, i) => (
              <div key={i} className="h-12 bg-gray-100 rounded" />
            ))}
          </div>
          <div className="space-y-2">
            <div className="h-4 w-24 bg-gray-100 rounded" />
            {Array.from({ length: 5 }).map((_, i) => (
              <div key={i} className="h-6 bg-gray-100 rounded" />
            ))}
          </div>
        </div>
      </div>
    </div>
  );
}
