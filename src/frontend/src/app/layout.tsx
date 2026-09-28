import type { Metadata } from "next";
import Link from "next/link";
import "./globals.css";

export const metadata: Metadata = {
  title: {
    default: "TrønderLeikan",
    template: "%s - TrønderLeikan",
  },
  description:
    "Plattform for turneringsstyring og poengberegning i Trøndelag. Følg med på rangeringer, resultater og spillerstatistikk.",
  metadataBase: new URL("https://tronderleikan.no"),
};

const navLinks = [
  { href: "/", label: "Turneringer" },
  { href: "/players", label: "Spillere" },
] as const;

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="no">
      <body className="flex min-h-dvh flex-col bg-white text-gray-900 antialiased">
        <header className="border-b border-gray-200">
          <div className="mx-auto flex h-14 max-w-5xl items-center justify-between px-4">
            <Link href="/" className="font-semibold">
              TrønderLeikan
            </Link>
            <nav aria-label="Primærnavigasjon">
              <ul className="flex gap-4 text-sm">
                {navLinks.map(({ href, label }) => (
                  <li key={href}>
                    <Link href={href} className="text-gray-600 hover:text-gray-900">
                      {label}
                    </Link>
                  </li>
                ))}
              </ul>
            </nav>
          </div>
        </header>

        <main className="flex-1">{children}</main>

        <footer className="border-t border-gray-200">
          <div className="mx-auto max-w-5xl px-4 py-4 text-sm text-gray-500">
            TrønderLeikan
          </div>
        </footer>
      </body>
    </html>
  );
}
