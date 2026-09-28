# Workshop: Agentisk utvikling

Dette er styringsdokumentet for begge dagene.
Ha det oppe på egen maskin, så jobber du i eget tempo.
Fasilitator roper opp til teori og recap.
Klokkeslett får du på stedet.

Oppgavene bruker user stories fra [docs/backlog.md](docs/backlog.md).
Domenet er beskrevet i [docs/TRONDER_LEIKAN.md](docs/TRONDER_LEIKAN.md).
Teknisk oppsett står i [README.md](README.md).

## Forberedelser

Gjør dette hjemme, minst tre dager før workshopen.
Vi bruker ikke workshoptid på oppstartsfeil.

**Påkrevd**

1. Lag ditt eget repo fra templaten: åpne `github.com/kongebra/leikan-template` og klikk «Use this template».
2. Klon repoet ditt.
   Laget du repoet før materialet ble oppdatert? Hent de nye versjonene:

   ```
   git remote add template https://github.com/kongebra/leikan-template.git
   git fetch template
   git checkout template/main -- WORKSHOP.md docs/backlog.md
   git commit -m "Oppdater workshopmateriell fra templaten"
   ```

3. Kjør `./bootstrap.sh` (macOS/Linux) eller `pwsh -ExecutionPolicy Bypass -File .\bootstrap.ps1` (Windows). Alt påkrevd skal være grønt.
   Windows-scriptet krever PowerShell 7 eller nyere. Windows PowerShell 5.1, som følger med Windows, holder ikke.
   Sier Windows at `pwsh` ikke finnes, installer det og åpne en ny terminal:

   ```
   winget install --id Microsoft.PowerShell --source winget
   ```

4. Kjør `dotnet run --project src/TronderLeikan.AppHost`. Første gang tar 2-5 minutter.
5. Åpne Aspire-dashboardet fra lenken i terminalen. Alle ressurser skal bli grønne.
6. Klikk på lenken til `frontend`. Du skal se to turneringer med scoreboard.
7. Legg til `/admin` på samme adresse, logg inn med `zitadel-admin@zitadel.localhost` og `Password1!`.
8. Du har lisens. De fleste har Claude Code via Enterprise og GitHub Copilot via Enterprise. Mangler du lisens, si fra i Slack-tråden før helgen.
9. Harnessen din er installert og innlogget. Fasilitator kjører Claude Code, men alle harnesser kan brukes, se «Andre harnesser» bakerst. Bruker du Claude Code, skal `claude --version` vise 2.1.281 eller nyere. Start og avslutt én sesjon etter oppgraderingen, før workshopen.

**Anbefalt**

- GitHub CLI, innlogget med `gh auth login`. Brukes til issues og PR-er.
- Aspire MCP, så agenten kan lese logger og ressursstatus selv. Sjekk med `claude mcp list`.
- Playwright MCP med `npx playwright install chromium`, så agenten kan bevise ting i nettleseren.

**Ta med**

- Din personlige instruksfil slik den er i dag, hvis du har en. For Claude Code er det `~/.claude/CLAUDE.md`.

Hvis noe stopper: sjekk «Feilsøking» i [README.md](README.md#feilsøking) først, og skriv så i Slack-tråden med feilmeldingen.

## Modulene

Hver modul har navn etter det vi jobber med.
Navnet er ikke nødvendigvis en fil du skal lage.

| Modul | Hva vi jobber med |
|---|---|
| `AGENTS.md` | Instruksene agenten får hver gang |
| `PLAN.md` | Å planlegge og utføre en oppgave med en agent |
| `SKILL.md` | Å gjøre gjentakelser om til noe gjenbrukbart |
| `hooks.json` | Det som aldri skal skje: hooks, deny-regler og sandkasse |
| `SUBAGENTS.md` | Å delegere, og review før PR |
| `SUMMARY.md` | Å bruke alt, og oppsummere |

Dag 1 er mest mulig hands-on.
Bruk agenten til alt, og start en fersk økt per oppgave.
Dag 2 starter med `/insights`, som leser øktene dine fra dag 1.

I `PLAN.md` velger du én story, og den tar du med deg videre.
Ingen feiler workshopen fordi storyen ikke ble ferdig.
Det som ikke gikk, er også læring.

## Hva du sitter igjen med

1. Hva som hører hjemme i `AGENTS.md`.
2. Når en `SKILL.md` lønner seg.
3. Hvordan hooks og sandkasse stopper det som aldri skal skje.
4. Erfaring med review med subagent før PR.
5. Kunnskap til å foreslå et oppsett hos kunden din.

## Slik leser du en modul

- **Mål:** hva du kan etterpå.
- **Start:** det første alle gjør.
- **Utforsk:** retninger og spørsmål, ikke oppskrifter.
- **Ferdig tidlig:** for deg som har tid til overs.
- **Ferdig når:** hva som skal være sant når modulen er slutt.
- **Recap:** spørsmålene vi tar i smågrupper og plenum.

Oppgavene er bevisst åpne.
Den som limer inn en story og trykker enter lærer ingenting.
Tolk, utforsk koden og gi agenten kontekst, gjerne sammen med andre.

## Dag 1

### `AGENTS.md`

**Mål:** en fersk agent vet hvordan repoet kjøres, testes og er bygd opp, uten at du må forklare det.

**Start**

Start en fersk sesjon.
Repoet har ingen `AGENTS.md` i rota ennå, bare en i `src/frontend` som lastes når agenten leser filer der.
Spør hvordan appen kjøres og testes.
Noter alt agenten bommer på eller må lete lenge etter.

**Utforsk**

- Kjør `/init` og les resultatet kritisk. Hva er nyttig, og hva er støy? Slett `CLAUDE.md` som `/init` lagde før du går videre, ellers leser ikke Claude Code `AGENTS.md`-en din.
- Skriv din egen `AGENTS.md` for hånd, under omtrent 100 linjer. Det er vårt eget mål; Anthropic anbefaler under 200. Fjern det som ikke hjelper.
- For hver linje: gjør agenten feil uten den? Hvis ikke, kutt den.
- Mye som bare gjelder én del av repoet, kan ligge i en egen `AGENTS.md` i den mappa. Claude Code, opencode og Copilot CLI laster den når de leser filer der. Codex og pi laster den bare hvis du starter agenten i mappa.
- Pek til detaljer i stedet for å lime dem inn, for eksempel «Detaljer om migrasjoner: docs/migrations.md». En `@`-import lastes alltid, en vanlig sti leses bare ved behov.
- Flytt det som gjelder alle prosjektene dine til din personlige instruksfil. Har du en fra før, gjør den bedre.
- Kjør `/context` og `/memory` før og etter. Stemmer det med det du trodde?
- Ny fersk sesjon, samme spørsmål. Sammenlign med starten.

Claude Code leser `AGENTS.md` direkte fra versjon 2.1.281, så lenge det ikke finnes noen `CLAUDE.md` eller `CLAUDE.local.md` i prosjektet.
Eldre versjoner trenger en `CLAUDE.md` med én linje: `@AGENTS.md`.

Når konteksten er forgiftet, gjentar agenten feil du har rettet, følger noe du limte inn i stedet for kodebasen, eller glemmer krav fra tidlig i sesjonen.
Bruk `/clear` for en ny oppgave eller en forgiftet kontekst.
Bruk `/compact` når du fortsatt jobber med samme oppgave, men konteksten er full.

Noter prompter du skriver flere ganger. Du trenger dem i `SKILL.md` i ettermiddag.

Ferdig tidlig:

- Legg regler for én del av repoet i `.claude/rules/` med `paths:`.
- Se hva auto memory har skrevet om deg.
- Sammenlign din `AGENTS.md` med naboens.
- Jobber du i store repoer hos kunde: les [Claude Code i store kodebaser](https://code.claude.com/docs/en/large-codebases).

**Ferdig når:** `AGENTS.md` er committet, og agenten klarer seg selv.

**Recap:**

1. Hva fjernet du fordi det ikke hjalp?
2. Hvordan skiller fila di seg fra `/init` og fra naboens?
3. Hva hører hjemme i din personlige fil?

### `PLAN.md`

**Mål:** du lar agenten utforske og planlegge før den bygger, og du vet hva grilling tilfører briefen din.

**Start**

Velg en story fra [docs/backlog.md](docs/backlog.md). Den tar du med deg resten av workshopen.
Skriv en brief på tre linjer: hva som skal være sant etterpå, hva som ikke skal røres (hvis du vet det, ellers finner du det ut underveis), og hvordan vi vet at det virker.
Start en fersk sesjon og gå i plan mode: skriv `/plan` foran briefen, eller trykk `Shift+Tab` til statuslinja viser plan mode.
Med Enterprise starter Claude Code i Manual, og da trykker du `Shift+Tab` to ganger. På Pro, Max og Team starter den i auto, og da trykker du tre ganger. Statuslinja viser hvor du er.
Ikke godkjenn planen før du faktisk er enig i den.

Start så en ny fersk sesjon på samme story.
La agenten grille deg før den planlegger, med `grill-me` fra [mattpocock/skills](https://github.com/mattpocock/skills) eller `brainstorming` fra [obra/superpowers](https://github.com/obra/superpowers).
Legg de to planene side om side, velg den beste, og bygg videre på den.

**Utforsk**

- La agenten utforske koden før den planlegger. Hva fant den som du ikke visste?
- Kjør `/context` etter utforskningen. Hvor mye kostet det å lese seg opp?
- Hva måtte du forklare agenten som den burde visst om repoet? Legg det inn i `AGENTS.md`.
- Agenten skal bevise at det virker. Test først, rød før grønn, og agenten endrer ikke testen for å bestå. Bevis mot kjørende system, med Playwright eller kall mot API-et. Grønne tester er ikke nok.

Ferdig tidlig:

- Kjør samme brief uten plan mode i en ny sesjon. Hva ble annerledes?
- Ta neste steg i manual-modus og i auto-modus. Hvor mange ganger måtte du svare?

**Ferdig når:** planene er sammenlignet, den beste er valgt, og første skive bygger.

**Recap:**

1. Hva gjorde agenten som du ikke forutså?
2. Hva måtte du forklare som agenten burde visst?
3. Hva fant grillingen som briefen din manglet?

### `SKILL.md`

**Mål:** du har gjort noe du gjentar til en skill, og agenten finner den selv når du trenger den.

**Start**

Gjør flyten fra `PLAN.md` til en skill: grilling, plan mode og bevis.
Eller plukk én prompt du har skrevet flere ganger i dag.
En skill er egentlig bare en prompt med et navn og en beskrivelse.

**Utforsk**

- Lag skillen i `.claude/skills/<navn>/SKILL.md`, eller personlig i `~/.claude/skills/`.
- Bruk den på neste steg i storyen din, i en fersk sesjon.
- Test begge veier: med `/navn`, og med naturlig språk uten å nevne skillen. Hentes den ikke, er beskrivelsen feil.
- Forbedre den etter første kjøring.
- Skal andre harnesser bruke den: legg den også i `.agents/skills`. Claude Code leser ikke noe under `.agents/`.

Inspirasjon: [mattpocock/skills](https://github.com/mattpocock/skills), [obra/superpowers](https://github.com/obra/superpowers), [anthropics/skills](https://github.com/anthropics/skills) og [hesreallyhim/awesome-claude-code](https://github.com/hesreallyhim/awesome-claude-code).
Sjekk lisensen før du kopierer: awesome-claude-code er CC BY-NC-ND, så les den for ideer.

Verdien bestemmer du selv.
En skill som bare er morsom, teller også.
Bruk skillen resten av workshopen, og forbedre den når den bommer.

Ferdig tidlig:

- Del skillen opp i flere filer som lastes ved behov.
- Legg ved et script skillen kan kjøre.

**Ferdig når:** skillen er testet med `/navn` og med naturlig språk, og forbedret minst én gang.

**Recap:**

1. Hvilken prompt slutter du å skrive nå?
2. Når hentet agenten skillen, og når ikke?
3. Hvilken skill tar du med til kunden mandag?

### Før du går

Hvor hører en feil agenten gjorde hjemme?

| Agenten gjorde feil | Løs det med |
|---|---|
| Den visste det ikke | regel i `AGENTS.md` |
| Det kan sjekkes | test |
| Det må aldri skje | hook |
| Det gjentar seg | skill |
| Det er mekanisk | script |

Ingen lekser. Hvil hodet.
I morgen starter vi med å se om `AGENTS.md`-en din holder, og med `/insights` på øktene fra i dag.

## Dag 2

### Holder `AGENTS.md`-en din?

Start `/insights` i en egen terminal først, så går den mens du tester.

Start en fersk sesjon.
Ta neste steg i storyen din, uten hjelp.
Klarer agenten seg med det du skrev i går?

### `/insights`

`/insights` leser de siste øktene dine på maskinen og lager en rapport om hvordan du jobber: hva som går galt, hva du kan prøve, og forslag til instruksfila.
Rapporten ligger i `~/.claude/usage-data/report.html`.
Den viser navnene på prosjektene dine, så ikke del den på skjerm.

Les rapporten og plukk ett funn.
Sorter det med «Hvor hører feilen hjemme?» fra i går:

- **Regel:** legg den inn i `AGENTS.md` eller den personlige fila nå. Spør om agenten gjør feil uten den, før du legger den inn.
- **Skill:** forbedre skillen fra i går, eller lag en ny.
- **Hook:** noter den. Du lager den i `hooks.json` rett etterpå.

Økter eldre enn 30 dager slettes som standard, så kjør den gjerne hver sprint og gjør ett funn om til en regel, skill eller hook.

**Ferdig når:** regelen er lagt inn, skillen er forbedret, eller hooken er notert.

### `hooks.json`

**Mål:** du har minst én regel agenten ikke kan bryte, og du vet hvem som faktisk sier nei.

**Start**

Lag en hook for noe som aldri skal skje.
Ta gjerne hook-funnet fra `/insights`, eller det AGENTS.md-testen viste.
En instruks i `AGENTS.md` er noe agenten bør gjøre. En hook er noe den ikke kan la være.

**Utforsk**

- Trigg hooken med vilje. En hook som aldri har fyrt, er bare en hypotese.
- Klassikere: blokker endringer i testfiler, eller la en Stop-hook kjøre testene før agenten sier at den er ferdig.
- Legg inn en deny-regel som nekter agenten å lese secrets. I Leikan ligger de ikke i `.env`, men i `src/TronderLeikan.AppHost/zitadel-bootstrap/` og i user secrets under hjemmemappen (`~/.microsoft/usersecrets/`, på Windows `%APPDATA%\Microsoft\UserSecrets`). Be agenten lese `admin.pat` først, legg inn regelen, og prøv igjen.
- En `Read`-deny stopper ikke `grep -r` i Bash. Det gjør bare en sandkasse, altså et miljø der agenten ikke når filene i det hele tatt. Harnessene har ulike løsninger, og kunder kan ha egne krav, for eksempel devcontainers. Hva ville kunden din krevd?
- Tekst fra web, issues og PR-er kan inneholde instrukser. Prompt injection er ikke løst, så tillatelsene dine er grensen.
- Vi ser på et forslag til felles standard for teamet i plenum. Hva ville du lagt i din egen `~/.claude/settings.json`?

Ferdig tidlig:

- Lag en hook til, av en annen type enn den første.
- Del hooken din med en som jobber annerledes enn deg. Passer den for dem?

**Ferdig når:** hooken og deny-regelen er trigget med vilje.

**Recap:**

1. Når prøvde agenten å komme seg forbi?
2. Hva i jobben din burde vært en hook og ikke en instruks?
3. Blir vi enige om den felles standarden?

### `SUBAGENTS.md`

**Mål:** du delegerer til subagenter, og du reviewer endringene dine med en subagent før du lager PR.

**Start**

Be hovedagenten starte en subagent som reviewer endringene dine i fersk kontekst, før du lager PR.
Skriv hand-offen selv: målet, hvilke filer den skal se på, hva som allerede er bestemt, og nøyaktig hva du vil ha tilbake.
Subagenten vet ingenting om samtalen din.
Claude Code kan også velge en fork, som arver hele samtalen og hovedmodellen. Skriv «ikke fork, bruk en fersk subagent» i hand-offen.

**Utforsk**

- La subagenten kjøre på en mindre og raskere modell. Legg regelen i din personlige instruksfil.
- Kjør `/context` før og etter at du delegerer. Hva ble igjen i hovedsesjonen?
- Lag en definert subagent i `.claude/agents/`. Start gjerne fra en samling, men les den før du bruker den: [VoltAgent/awesome-claude-code-subagents](https://github.com/VoltAgent/awesome-claude-code-subagents), [hesreallyhim/awesome-claude-code](https://github.com/hesreallyhim/awesome-claude-code), [github/awesome-copilot](https://github.com/github/awesome-copilot) eller [VoltAgent/awesome-codex-subagents](https://github.com/VoltAgent/awesome-codex-subagents).
- Claude Code har også `/code-review` og `/security-review`.
- Har du Copilot code review i GitHub: se hva den finner på PR-en etter at subagenten har reviewet.

Ferdig tidlig:

- Gjør reviewen til en definert subagent eller en skill, og bruk den før hver PR.

**Ferdig når:** funnene er fulgt opp, PR-en er oppe, og subagenten kjørte på en billigere modell.
PR-en kan godt være en draft.

**Recap:**

1. Hva fant reviewen som du ikke så selv?
2. Hvordan så hand-offen din ut, og hva manglet?
3. Hva ville du stolt på en agent-review med hos kunden?

### `SUMMARY.md`

**Mål:** du bruker alt fra de to dagene på egen hånd.

Velg selv:

- Fullfør storyen, eller ta en ny.
- Ta det med til ditt eget repo. Kundekode bare der kunden har sagt ja til agenten du bruker.
- Jobb parallelt i en worktree: følg opp et review-funn i en egen sesjon mens hovedsesjonen jobber videre, og merge tilbake. Bruk `git worktree add ../leikan-fix -b fix/review-funn` fra storybranchen. `claude --worktree` starter fra `main` med mindre du setter `"worktree": { "baseRef": "head" }` i `.claude/settings.local.json`. AppHost kjører bare fra hovedklonen, se [README.md](README.md#feilsøking), så kjør bare tester i worktreen.
- Finn feilen i tre PR-er fra fasilitator. Finn den selv først, så med en agent i fersk kontekst, og sammenlign. Lot agenten seg lure av teksten den leste?
- Lag skillen, hooken eller subagenten du mangler.

### Hva lærte vi?

- Hva fungerte?
- Hva fungerte ikke?
- Hva tar vi med oss?
- Lærte du ingenting nytt? Hvorfor ikke?

## Andre harnesser

Prinsippene er de samme.
Bruker du noe annet enn Claude Code, må du gjøre en liten innsats selv.

| Begrep | Claude Code | Codex CLI | GitHub Copilot | pi |
|---|---|---|---|---|
| Instrukser | `AGENTS.md` fra 2.1.281, ellers `CLAUDE.md` med `@AGENTS.md` | `AGENTS.md` | `.github/copilot-instructions.md`, `AGENTS.md` | `AGENTS.md` |
| Planlegging | Plan mode, `Shift+Tab` | `/plan` | Plan-agent og plan mode | Extension |
| Skills | `.claude/skills/` | `.agents/skills/` | `.github/skills/`, `.claude/skills/`, `.agents/skills/` | Skills |
| Hooks | `.claude/settings.json` | `hooks.json` eller `config.toml` | `.github/hooks/*.json`; CLI leser også hooks i `.claude/settings.json` | TypeScript-extensions |
| Subagenter | Naturlig språk eller `.claude/agents/` | `.codex/agents/` | `.github/agents/*.agent.md` | Extension eller flere prosesser |
| Worktrees | `git worktree add`, eller `claude --worktree` med `worktree.baseRef: "head"` | `codex --worktree` eller `/worktree` (detached HEAD, lag en branch før du merger), eller `git worktree` | `git worktree` | `git worktree` |

## Vokabular

| Begrep | Betydning |
|---|---|
| Harness | Verktøyet som gir modellen hender: filsystem, shell, git, nettleser. Claude Code, Codex, Copilot, pi. |
| Brief | Hva som skal være sant etterpå, hva som ikke skal røres, og hvordan vi vet at det virker. |
| Plan mode | Agenten utforsker og planlegger uten å endre filer før du godkjenner. |
| `AGENTS.md` | Instruksene agenten leser ved oppstart. Kort, sant, gjelder alltid. |
| Skill | En prompt med navn og beskrivelse som agenten henter når den trengs. |
| Hook | Kommando som kjøres automatisk før eller etter en handling. Kan nekte agenten. |
| Deny | Regel som nekter agenten et verktøy eller en fil, uansett hva den vil. |
| Subagent | Agent startet av en agent, med egen fersk kontekst. Bare svaret kommer tilbake. |
| Hand-off | Instruksen du gir en subagent. Den må stå på egne ben. |
| Worktree | Egen arbeidskopi av repoet på egen branch, så to sesjoner kan jobbe parallelt. |
| Forgiftet kontekst | Konteksten har tatt med seg noe som styrer agenten feil. |
| `/context` | Viser hva agenten faktisk bærer med seg i sesjonen. |
