// Lasteskjerm for spillerprofil — vises mens server-side data hentes.

export default function Loading() {
  return (
    <div className="mx-auto max-w-5xl px-4 py-8">
      <div className="animate-pulse space-y-4">
        <div className="bg-gray-100 rounded h-4 w-32" />
        <div className="flex flex-col items-start gap-4">
          <div className="bg-gray-100 rounded-full h-[7.5rem] w-[7.5rem]" />
          <div className="bg-gray-100 rounded h-8 w-64" />
        </div>
        {/* Plassholdere for spillhistorikken */}
        <div className="pt-4 space-y-3">
          <div className="bg-gray-100 rounded h-6 w-40" />
          {[0, 1, 2].map((i) => (
            <div key={i} className="bg-gray-100 rounded h-16 w-full" />
          ))}
        </div>
      </div>
    </div>
  );
}
