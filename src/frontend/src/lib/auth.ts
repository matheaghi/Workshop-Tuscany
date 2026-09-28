import { betterAuth } from "better-auth";
import { genericOAuth } from "better-auth/plugins";

// Konfigurerer better-auth med Zitadel som OIDC-leverandør via genericOAuth-plugin.
// discoveryUrl brukes til å hente endepunkter automatisk fra Zitadels OIDC-metadata.
// Miljøvariabler settes av Aspire ved kjøretid.
export const auth = betterAuth({
  secret: process.env.BETTER_AUTH_SECRET!,
  baseURL: process.env.BETTER_AUTH_URL ?? "http://localhost:3000",
  // Stateless sesjoner uten database, etter better-auth sitt eget eksempel:
  // sesjonen ligger kryptert i cookien og fornyes automatisk før den utløper.
  session: {
    cookieCache: {
      enabled: true,
      maxAge: 7 * 24 * 60 * 60,
      strategy: "jwe",
      refreshCache: true,
    },
  },
  account: {
    storeStateStrategy: "cookie",
    storeAccountCookie: true,
  },
  plugins: [
    genericOAuth({
      config: [
        {
          providerId: "zitadel",
          // Zitadel sitt OIDC-discovery-endepunkt
          discoveryUrl: `${process.env.ZITADEL_ISSUER}/.well-known/openid-configuration`,
          clientId: process.env.ZITADEL_CLIENT_ID!,
          clientSecret: process.env.ZITADEL_CLIENT_SECRET!,
          scopes: ["openid", "profile", "email"],
          pkce: true,
        },
      ],
    }),
  ],
});
