import Link from "next/link";
import type { Metadata } from "next";

export const metadata: Metadata = {
  title: "Dashboard",
};

// Hurtiglenke-kort for de to admin-seksjonene
const sections = [
  {
    href: "/admin/tournaments",
    label: "Turneringer",
    description: "Administrer turneringer, runder og spill.",
  },
  {
    href: "/admin/persons",
    label: "Spillere",
    description: "Legg til, rediger og administrer spillerregistre.",
  },
] as const;

// Admin-dashbord — oversiktsside med snarveier til de to hoveddelseksjonene
export default function AdminDashboardPage() {
  return (
    <div>
      <header className="mb-6">
        <h1 className="text-2xl font-semibold mb-1">Dashboard</h1>
        <p className="text-sm text-gray-600">
          Administrer turneringer og spillere for TrønderLeikan.
        </p>
      </header>

      <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
        {sections.map(({ href, label, description }) => (
          <Link
            key={href}
            href={href}
            className="block border border-gray-200 rounded p-4 hover:bg-gray-50"
          >
            <h2 className="text-lg font-semibold mb-1">{label}</h2>
            <p className="text-sm text-gray-600">{description}</p>
          </Link>
        ))}
      </div>
    </div>
  );
}
