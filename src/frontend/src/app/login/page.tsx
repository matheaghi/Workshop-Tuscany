"use client";

import { authClient } from "@/lib/auth-client";
import { useState } from "react";

// Innloggingsside — klientkomponent siden den kaller authClient.signIn.social()
export default function LoginPage() {
  const [isLoading, setIsLoading] = useState(false);

  async function handleSignIn() {
    setIsLoading(true);
    try {
      // Starter Zitadel OIDC-innloggingsflyt med omdirigering til /admin etter suksess
      // Zitadel registreres av genericOAuth-pluginen som en social provider
      await authClient.signIn.social({
        provider: "zitadel",
        callbackURL: "/admin",
      });
    } catch {
      // Feil ved innlogging — tilbakestill lastestatus
      setIsLoading(false);
    }
  }

  return (
    <div className="mx-auto max-w-sm px-4 py-8">
      <h1 className="text-2xl font-semibold mb-4">Adminpanel</h1>
      <p className="text-sm text-gray-600 mb-6">
        Logg inn med din organisasjonskonto for å administrere turneringer og
        spillere.
      </p>

      <button
        onClick={handleSignIn}
        disabled={isLoading}
        aria-busy={isLoading}
        className="w-full rounded bg-gray-900 px-3 py-1.5 text-sm text-white hover:bg-gray-700 disabled:opacity-50"
      >
        {isLoading ? "Logger inn..." : "Logg inn med Zitadel"}
      </button>
    </div>
  );
}
