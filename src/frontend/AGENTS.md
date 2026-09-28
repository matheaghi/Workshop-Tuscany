<!-- BEGIN:nextjs-agent-rules -->

# This is NOT the Next.js you know

This version has breaking changes — APIs, conventions, and file structure may all differ from your training data. Read the relevant guide in `node_modules/next/dist/docs/` (resolved from this file's directory; in monorepos the `next` package may not be visible from the repo root) before writing any code. Heed deprecation notices.

This block is written and re-added by `next dev` — verify at `node_modules/next/dist/server/lib/generate-agent-files.js`. Removing it from a diff only re-creates the uncommitted change; committing it with your work keeps the tree clean.

<!-- END:nextjs-agent-rules -->

# Frontend

Next.js 16 App Router, React 19, Tailwind 4, better-auth.

- Route groups: `(public)` (scoreboard, players, tournaments) and `(admin)/admin` (protected by `src/proxy.ts`, which redirects to `/login` without a session).
- All API calls are server-side `fetch` to `process.env.API_BASE_URL` in server components and `"use server"` `actions.ts` files, followed by `revalidatePath`.
- Auth: better-auth with Zitadel OIDC (`src/lib/auth.ts`). The AppHost provisions the OIDC client and injects the `ZITADEL_*` and `BETTER_AUTH_*` env vars.
