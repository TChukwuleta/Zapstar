# Zapstar

Zap sats to GitHub repos and their contributors, directly via Lightning. Non-custodial — the API never holds funds, it only resolves addresses and relays LNURL-pay requests. Payment always goes straight from the tipper's wallet to the recipient's own Lightning Address.

## How it works

1. Visit any GitHub repo or profile with the extension installed
2. If the repo has a `.github/FUNDING.yml` with a `lightning:` key (or a user has a Lightning Address in their bio/profile README), a **⚡ Zap** button appears
3. Click it, pick an amount, pay the invoice from your own wallet
4. If the recipient's wallet supports LUD-21 verify, payment is detected automatically and the modal resets — otherwise, mark it paid manually

## Structure

- **`Zapstar.Api/`** — ASP.NET Core minimal API (.NET 10). Resolves whether a repo/user has a tippable Lightning Address, generates BOLT11 invoices via LNURL-pay, and checks payment status.
- **`Zapstar.Api.Tests/`** — xUnit test project covering the resolver and LNURL logic.
- **`extension/`** — Manifest V3 browser extension (Chrome/Edge/Brave; Firefox port planned). Detects GitHub pages, injects the zap button, handles the payment modal.

## API

### Endpoints

| Method | Path | Description |
|---|---|---|
| `GET` | `/repo/{owner}/{repo}` | Resolves a repo's Lightning Address from `.github/FUNDING.yml` |
| `GET` | `/user/{username}` | Resolves a user's Lightning Address from their bio, falling back to their profile README |
| `POST` | `/invoice` | Generates a BOLT11 invoice for a given address and amount |
| `GET` | `/invoice/status?verifyUrl=...` | Checks whether an invoice has been paid (LUD-21 verify) |

Every candidate address is verified against its actual LNURL-pay endpoint before being trusted — this is what stops a plain email address from being mistaken for a Lightning Address, since the two share identical `user@domain.tld` syntax.

### Running locally

```bash
cd Zapstar.Api
dotnet restore
dotnet run
```

Defaults to `http://localhost:5183` (check console output for the actual port).

Optional but recommended — set a GitHub personal access token to avoid the unauthenticated 60 req/hour rate limit:

```bash
dotnet user-secrets init
dotnet user-secrets set "GitHub:Token" "ghp_yourtokenhere"
```

### Running tests

```bash
cd Zapstar.Api.Tests
dotnet test
```

CI runs the full test suite on every pull request targeting `master` via GitHub Actions (`.github/workflows/tests.yml`), and branch protection blocks merging until it passes.

## Extension

### Loading it locally

1. Make sure the API is running (`dotnet run` from `Zapstar.Api/`)
2. In `extension/background.js`, confirm `API_BASE` points at your local API's port
3. Go to `chrome://extensions`, enable **Developer mode**
4. **Load unpacked** → select the `extension/` folder
5. Visit a GitHub repo with a valid `.github/FUNDING.yml` — the zap button should appear in the sidebar under Languages

### How discovery works

- **Repos**: reads `.github/FUNDING.yml` on `main`/`master`, looks for a `lightning:` key
- **Users**: checks the GitHub profile bio first, falls back to the special profile README (`github.com/{username}/{username}/README.md`)
- Every candidate is confirmed against its real LNURL-pay endpoint server-side before the zap button ever appears

### Payment confirmation

If the recipient's Lightning Address supports LUD-21 verify (BTCPay Server does), the extension polls every 30 seconds for up to 5 minutes after an invoice is generated. Once payment is detected, it shows a confirmation message and resets back to the amount picker automatically. If verify isn't supported, an **"I've Paid"** button lets you reset manually.

## Known limitations

- Icons in `extension/icons/` are placeholders — real PNGs needed before publishing
- `Program.cs`'s CORS policy is wide open (`AllowAnyOrigin`) for local dev — tighten to your published extension ID(s) before shipping
- Safari requires a separate native-wrapper build via `safari-web-extension-converter` — not covered yet
- Firefox port not yet tested, though the extension avoids Chrome-specific APIs where possible

## Contributing

PRs welcome. All changes to `master` go through a pull request and must pass CI (`dotnet test`) before merging.
