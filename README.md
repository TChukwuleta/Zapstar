# Zapstar

Zap sats to your favorite GitHub repos and their contributors, directly via Lightning. 

## How it works

1. Visit a GitHub repo or profile with the extension installed
2. If the repo has a `.github/FUNDING.yml` with a `lightning:` key, or a user has a Lightning Address in their bio or profile README, a Zap button shows up
3. Click it, pick an amount, pay from your own wallet
4. If the recipient's wallet supports LUD-21 verify, payment gets picked up automatically and the modal resets. If not, there's an "I've Paid" button to reset it manually

## Structure

- `Zapstar.Api/` — ASP.NET Core minimal API on .NET 10. Resolves whether a repo or user has a working Lightning Address, generates invoices via LNURL-pay, checks payment status. Runs on Coolify via Docker.
- `Zapstar.Api.Tests/` — xUnit tests for the resolver and LNURL logic.
- `extension/` — Manifest V3 browser extension. Detects GitHub pages, shows the zap button, handles the payment modal.
- `PRIVACY.md` — privacy policy, required for the Chrome Web Store listing.

## API

### Running it locally

```bash
cd Zapstar.Api
dotnet restore
dotnet run
```

Check the console output for the localhost port to check where the project started on.

Set a GitHub token to avoid the 60 req/hour unauthenticated rate limit:

```bash
dotnet user-secrets init
dotnet user-secrets set "GitHub:Token" "ghp_yourtokenhere"
```

### Tests

```bash
cd Zapstar.Api.Tests
dotnet test
```

Runs automatically on every pull request via GitHub Actions. Merging to `master` is blocked until tests pass.
