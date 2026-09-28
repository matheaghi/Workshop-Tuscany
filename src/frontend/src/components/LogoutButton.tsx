"use client";

import { authClient } from "@/lib/auth-client";
import { useRouter } from "next/navigation";

// Utloggingsknapp - klientkomponent siden den kaller authClient.signOut()
export function LogoutButton() {
  const router = useRouter();

  async function handleSignOut() {
    await authClient.signOut({
      fetchOptions: {
        onSuccess: () => {
          router.push("/login");
        },
      },
    });
  }

  return (
    <button
      type="button"
      onClick={handleSignOut}
      className="rounded border border-gray-300 px-3 py-1 text-sm hover:bg-gray-100"
    >
      Logg ut
    </button>
  );
}
