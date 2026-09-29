// Lasteskjerm for spillerprofil — vises mens server-side data hentes.

export default function Loading() {
  return (
    <div className="mx-auto max-w-5xl px-4 py-8">
      <div className="animate-pulse motion-reduce:animate-none space-y-4">
        <div className="bg-gray-100 rounded h-4 w-32" />
        {/* Avatar og navn på samme rad, som på profilsiden */}
        <div className="flex items-center gap-4">
          <div className="bg-gray-100 rounded-full size-20 sm:size-[7.5rem] shrink-0" />
          <div className="bg-gray-100 rounded h-8 w-64 max-w-full" />
        </div>
        {/* Plassholdere for rekkene */}
        <div className="grid grid-cols-2 gap-2 sm:flex">
          <div className="bg-gray-100 rounded h-20 sm:w-44" />
          <div className="bg-gray-100 rounded h-20 sm:w-44" />
        </div>
        {/* Plassholdere for oppsummeringen */}
        <div className="pt-4 space-y-2">
          <div className="bg-gray-100 rounded h-6 w-36" />
          <div className="bg-gray-100 rounded h-24 w-full" />
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
