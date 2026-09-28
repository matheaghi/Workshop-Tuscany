import { createAuthClient } from "better-auth/client";

// Klientside-instans for better-auth.
// Ingen baseURL: kallene går mot samme origin som siden, uavhengig av hvilken port Aspire eksponerer.
export const authClient = createAuthClient();
