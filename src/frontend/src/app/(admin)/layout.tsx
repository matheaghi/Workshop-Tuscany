import { headers } from "next/headers";
import { redirect } from "next/navigation";
import Link from "next/link";
import { auth } from "@/lib/auth";
import { LogoutButton } from "@/components/LogoutButton";
import type { Metadata } from "next";

export const metadata: Metadata = {
  title: {
    default: "Admin",
    template: "%s - Admin",
  },
};

const adminNavLinks = [
  { href: "/admin", label: "Dashboard" },
  { href: "/admin/tournaments", label: "Turneringer" },
  { href: "/admin/persons", label: "Spillere" },
] as const;

// Admin-layout med sesjonsbeskyttelse - omdirigerer ikke-autentiserte til /login
export default async function AdminLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  // Sjekker sesjon på serveren - better-auth leser cookie fra request-headers
  const session = await auth.api.getSession({ headers: await headers() });

  if (!session) {
    redirect("/login");
  }

  const displayName = session.user.name || session.user.email;

  return (
    <div className="mx-auto max-w-5xl px-4 py-6">
      <div className="mb-6 flex flex-wrap items-center justify-between gap-4 border-b border-gray-200 pb-4">
        <nav aria-label="Admin-navigasjon">
          <ul className="flex gap-4 text-sm">
            {adminNavLinks.map(({ href, label }) => (
              <li key={href}>
                <Link href={href} className="text-gray-600 hover:text-gray-900">
                  {label}
                </Link>
              </li>
            ))}
          </ul>
        </nav>
        <div className="flex items-center gap-3 text-sm">
          <span className="text-gray-600">{displayName}</span>
          <LogoutButton />
        </div>
      </div>
      {children}
    </div>
  );
}
