// Videresender profilbildet fra API-et, siden nettleseren ikke når API_BASE_URL direkte.
export async function GET(_request: Request, { params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;
  try {
    const res = await fetch(
      `${process.env.API_BASE_URL ?? "http://localhost:5000"}/api/v1/persons/${encodeURIComponent(id)}/image`,
      { cache: "no-store" }
    );
    return new Response(res.body, {
      status: res.status,
      headers: { "Content-Type": res.headers.get("Content-Type") ?? "application/octet-stream" },
    });
  } catch {
    return new Response(null, { status: 502 });
  }
}
