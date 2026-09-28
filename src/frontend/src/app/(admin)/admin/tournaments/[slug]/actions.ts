"use server";
import { revalidatePath } from "next/cache";

// API-basis-URL — hentes fra miljøvariabel eller faller tilbake til localhost
const API_BASE = process.env.API_BASE_URL ?? "http://localhost:5000";

// Oppdaterer poengregler for en turnering via PUT /api/v1/tournaments/:id/point-rules
export async function updatePointRulesAction(
  tournamentId: string,
  tournamentSlug: string,
  formData: FormData
) {
  const body = {
    tournamentId,
    participation: Number(formData.get("participation")),
    firstPlace: Number(formData.get("firstPlace")),
    secondPlace: Number(formData.get("secondPlace")),
    thirdPlace: Number(formData.get("thirdPlace")),
    organizedWithParticipation: Number(
      formData.get("organizedWithParticipation")
    ),
    organizedWithoutParticipation: Number(
      formData.get("organizedWithoutParticipation")
    ),
    spectator: Number(formData.get("spectator")),
  };

  const res = await fetch(
    `${API_BASE}/api/v1/tournaments/${tournamentId}/point-rules`,
    {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(body),
    }
  );

  if (!res.ok) throw new Error("Kunne ikke oppdatere poengregler");
  revalidatePath(`/admin/tournaments/${tournamentSlug}`);
}

// Oppretter et nytt spill i turneringen via POST /api/v1/games
export async function createGameAction(
  tournamentId: string,
  tournamentSlug: string,
  formData: FormData
) {
  const body = {
    tournamentId,
    name: formData.get("name") as string,
    gameType: formData.get("gameType") as string,
    isOrganizersParticipating:
      formData.get("isOrganizersParticipating") === "on",
  };

  const res = await fetch(`${API_BASE}/api/v1/games`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body),
  });

  if (!res.ok) throw new Error("Kunne ikke opprette spill");
  revalidatePath(`/admin/tournaments/${tournamentSlug}`);
}
