# Harkonnen Mentat

Companion app for the Mahdi Solo Mode of *Dune: War for Arrakis*. The player runs House Atreides on the physical board; the app runs the Harkonnen automa. Blazor WebAssembly PWA (.NET 10), no server, state in localStorage. Live at https://thankful-hill-01b04ab03.3.azurestaticapps.net.

## Core design constraint

The app **cannot see the board**. It owns everything off the map (dice, Tactical cards, Reinforcements, deck alternation, spice/Imperium/Bans, Supremacy, Leaders, phase flow). The player owns the map: each Harkonnen result is shown as a fallback chain ("do the first one that is possible") with this round's Target Sietch and Harvesting Sector filled in. Keep it at **one tap per Harkonnen turn** and **buttons only, never typed numbers**.

## Layout

- `src/Mahdi.Engine`: pure C#, no UI.
  - `Model/`: immutable `GameState` records, enums, `GameContent`.
  - `Events/GameEvents.cs`: polymorphic events (JSON `$type` discriminators). **Random and rule-derived outcomes are recorded in events**, so replay never depends on RNG or rule code.
  - `Commands/`: player intents. `Rules/CommandHandler.cs` validates and turns them into events (throws `CommandRejectedException`).
  - `GameReducer.cs`: pure `Apply(content, state, event)`. Supremacy triggers (Leader entry, Atreides Bene Gesserit reminders, victory), Regeneration Tank, Bans and notices live here.
  - `Game.cs`: history grouped per command; undo means dropping the last batch and replaying. RNG per command is `SeededRandomSource(seed, commandIndex)`, so an undone roll repeats.
  - `Rules/`: `TacticalDeck`, `DiceRules`, `LeaderRules`, `SpiceRules`.
  - `Guidance/GuidanceBuilder.cs`: all player-facing rules text (paraphrased, with rulebook page references).
  - `Content/*.json`: embedded game data (dice faces, Spice Must Flow table, Supremacy track, Tactical cards, Leaders, Bans). Fix data here, not in code.
  - `Persistence/SaveData.cs`: save format `{version, diceMode, seed, history}`, source-generated JSON.
- `src/Mahdi.App`: Blazor UI.
  - `Services/GameSession.cs` is the single source of truth: executes commands, saves after every change, raises `Changed`.
  - `GameStore` wraps localStorage. `ThemeService` and `wwwroot/js/app.js` handle the theme, applied before Blazor starts.
  - Pages: `Home`, `NewGame` (dice mode; `?seed=` honoured only outside Production), `GamePage` (switches on `Phase`), `Reference`.
  - Components: `WarTable`, `StateRail` (collapses to an icon strip on short landscape screens), `EventSheet`, `CombatHelper`, `FacePicker`, `GuidanceCard`, `SpicePanel`, `TacticalCards`, `DieIcon` (original glyphs, no game art).
  - CSS: `wwwroot/css/app.css` (theme tokens for light "Desert Parchment" and dark "Arrakis Night", base styles, buttons, shell) and `wwwroot/css/game.css` (screens and responsive rules). Short landscape phones use `@media (max-height: 520px)`.
- `tests/Mahdi.Engine.Tests`: NUnit unit tests.
- `tests/Mahdi.App.E2E`: Playwright + NUnit, WebKit, fixtures for "iPad Pro 11 landscape" and "iPhone 15 Pro landscape" (`DeviceTest`). `AppServer` publishes the app with `-p:WasmApplicationEnvironmentName=Test` and serves it with an index.html fallback.
- `docs/Rules`, `docs/Components`: rulebook PDFs and component photos. **Local only, git-ignored (copyrighted).** Use them to check rules; never commit them.

## Commands

```pwsh
dotnet run --project src/Mahdi.App                       # dev server
dotnet test tests/Mahdi.Engine.Tests                     # unit tests
pwsh tests/Mahdi.App.E2E/bin/Debug/net10.0/playwright.ps1 install webkit   # once, after building the E2E project
dotnet test tests/Mahdi.App.E2E                          # E2E (publishes the app itself, about 1 min)
dotnet test DuneMahdiSolo.slnx                           # everything
```

E2E screenshots go to `tests/Mahdi.App.E2E/bin/<config>/net10.0/screenshots/<device>/`. Set `MAHDI_E2E_SITE` to a published `wwwroot` folder to skip publishing.

## Conventions

- Tests: NUnit (not xUnit). New UI behaviour gets an E2E scenario; new rules get engine unit tests.
- Interactive elements carry `data-testid`; E2E tests select only by test id. State is exposed via attributes such as `data-phase`, `data-count` and `data-value`.
- Main action buttons stay sticky at the bottom (E2E asserts `ToBeInViewportAsync`).
- `TreatWarningsAsErrors` is on (see `Directory.Build.props`).
- Commit in natural phases (feature, then tests, then separate `Fix: ...` commits). Commit messages end with the Co-Authored-By attribution line.
- Shell heredocs containing larger C#/Razor/CSS content have failed in this environment's Bash tool; write files with the file tools instead.

## Rule interpretations (revisit if the player disagrees)

- With all Imperium markers on the top step and 7+ spice, the whole excess buys the Supremacy point (no Reserve).
- Rabban's and Feyd-Rautha's specials are offered only when their figure is on the board.
- A killed Leader enters the Regeneration Tank on "Start here" and returns after 5 Harkonnen turns, with its card ready.
- Truthtrance limits follow the solo text: 3 spent, or 2 for Deployment/House.
- Tactical card Sectors were read from component photos (`Content/tactical-cards.json`); Windgap and Hobars Gap are the Central cards.
- After a regular Mentat result the next deck stays the same (2 cards alternate). After the Hawat or Mohiam special it flips to the other deck.
- Deployment names one Named Leader in reserve (Rabban/Feyd first); others are listed as "if you can, you choose".

## Scope

Base game only. The Spacing Guild and Desert War rulebooks state they are not compatible with Mahdi Solo; Smugglers lacks solo rules for parts of the automa (e.g. Thopter Battle). `Modules/IRulesModule.cs` is the seam for adding expansions later.

## Deployment

Pushing to `main` triggers `.github/workflows/azure-static-web-apps.yml`: unit tests, then E2E, then `dotnet publish`, then `Azure/static-web-apps-deploy` (`publish/wwwroot`, `skip_app_build`). Pull requests get preview environments. Azure resources: Static Web App `harkonnen-mentat` (Free, West Europe) in resource group `rg-harkonnen-mentat`; the GitHub secret `AZURE_STATIC_WEB_APPS_API_TOKEN` holds its deploy token. `wwwroot/staticwebapp.config.json` holds the SPA fallback and cache headers.
