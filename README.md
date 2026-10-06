# Mahdi Solo Companion

A companion web app for the **Mahdi Solo Mode** of *Dune: War for Arrakis*. You play House Atreides on the board; the app runs the Harkonnen automa: it rolls (or records) the Harkonnen dice, draws the Tactical cards, tracks the Reinforcements deck, Imperium markers, Bans, Supremacy and Leaders, and shows one clear instruction per Harkonnen turn.

The app cannot see the board, so each Harkonnen result is shown as a short fallback chain ("do the first one that is possible"), already filled in with this round's Target Sietch and Harvesting Sector. On the normal path it takes one tap per Harkonnen turn.

## Features

- **War Table** for landscape tablets, with a compact layout for phones in landscape.
- **Dice mode per game**: the app rolls, or you roll the physical die and tap the face (blocked results are greyed out).
- **Desert Parchment** (light) and **Arrakis Night** (dark) themes.
- Saved in the browser (localStorage) after every step, with **undo**.
- Works offline once loaded (PWA). No server, no account.
- Base game only. The Spacing Guild and Desert War are officially not compatible with Mahdi Solo Mode.

## Project layout

| Path | What it is |
|---|---|
| `src/Mahdi.Engine` | Pure C# rules engine: event-sourced game state, solo rules, guidance texts. Game data lives in `Content/*.json`. |
| `src/Mahdi.App` | Blazor WebAssembly PWA (War Table, phase screens, event sheet, themes). |
| `tests/Mahdi.Engine.Tests` | NUnit unit tests for the engine. |
| `tests/Mahdi.App.E2E` | Playwright + NUnit end-to-end tests on WebKit, iPad Pro 11 and iPhone 15 Pro in landscape. |
| `.github/workflows` | Build, test and deploy to Azure Static Web Apps. |

The rulebook PDFs and component photos used to build the content data are kept locally in `docs/Rules` and `docs/Components` and are excluded from git.

## Run locally

Requires the .NET 10 SDK.

```pwsh
dotnet run --project src/Mahdi.App
```

## Tests

```pwsh
# Engine unit tests
dotnet test tests/Mahdi.Engine.Tests

# End-to-end tests (first time: install WebKit for Playwright)
dotnet build tests/Mahdi.App.E2E
pwsh tests/Mahdi.App.E2E/bin/Debug/net10.0/playwright.ps1 install webkit
dotnet test tests/Mahdi.App.E2E
```

The E2E fixture publishes the app with the `Test` environment and serves it locally. In that environment `new?seed=<n>` starts a reproducible game; production builds ignore the seed. Screenshots per device are written to `tests/Mahdi.App.E2E/bin/<config>/net10.0/screenshots`. Set `MAHDI_E2E_SITE` to an already published `wwwroot` folder to skip the publish step.

## Deploy to Azure Static Web Apps

1. Create a Static Web App in Azure (Free plan is enough) with **Other** as the deployment source.
2. Copy its deployment token and add it to the GitHub repository as the secret `AZURE_STATIC_WEB_APPS_API_TOKEN`.
3. Push to `main`. The workflow runs the unit and E2E tests, publishes the app and uploads `publish/wwwroot`. Pull requests get preview environments that are removed when the PR closes.

`src/Mahdi.App/wwwroot/staticwebapp.config.json` provides the SPA fallback to `index.html` and cache headers.

## Disclaimer

Unofficial fan-made tool. *Dune: War for Arrakis* is published by CMON under licence from Gale Force Nine and Legendary. The app paraphrases the solo rules and uses no card art; the rulebook is authoritative. Fonts (Cinzel, Cormorant Garamond, Inter) are under the SIL Open Font License; see `src/Mahdi.App/wwwroot/fonts`.
