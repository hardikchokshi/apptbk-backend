# ApptBk — Deployment & CI/CD

Covers the whole platform. Frontend-specific notes live in
`apptbk-client-app/docs/DEPLOYMENT.md`.

## 0. Status and next tasks (updated 2026-11-26)

**Done**
- Neon project `ApptBk` has branch `production` (default, all 7 tables imported) and child branch `dev`.
- Azure resource group `apptbk-prod` exists. Production apps are blocked: the subscription's B1 quota is 0 in every region, and a quota request has been filed with Microsoft (reply expected within ~2 business days).
- Dev apps (F1 Windows plan, resource group `PhysicianBookingAPI`): `PhysicianBookingAPI20260728210051` (API), `PhysicianBookingAdminPanel` (CPanel) and `Physician-Client-App` (frontend). SCM Basic Auth on; API and CPanel on .NET 10; client Node `~22`.
- Dev app settings: API `ConnectionStrings__Postgres` and `Cors__AllowedOrigins__0`; CPanel `ConnectionStrings__Postgres`. Both connection strings use the **key=value** format (see section 6).
- GitHub `dev` environment variables and secrets (`*_PUBLISH_PROFILE`) are set in both repos.
- **CI/CD is live on `Development` in both repos.** Every merge builds, tests and deploys to dev. All three dev apps are deployed and working:
  - **API:** `/health` returns "Healthy", and the data endpoints return results.
  - **Frontend:** home page, `/physicians` and the home search work. Fixed by frontend PR #2 (`web.config` serves `/` through Node; SSR `allowedHosts` in `angular.json`) and PR #3 (the home search splits "City, ST 12345" into zip, city and state).
  - **CPanel:** login page and Leads load with data. Fixed by backend PR #3 (removed 437 MB of unused `wwwroot/uploads` images that broke the deploy).
- Security and code-quality findings are tracked in the private `apptbk-client-app/docs/CODE-REVIEW.md`, not in this public repo.

**Next tasks.** Each one is small, so run each in a new Claude session:
1. **Security items (urgent):** see the private `CODE-REVIEW.md`, items B1 and B2. The user rotates the credentials; the admin-panel fix is code work.
2. **`/health` should check the database:** it reported "Healthy" while every data request failed. Add a Postgres check and a CPanel smoke test to the pipeline.
3. **CORS for local dev:** the Azure setting `Cors__AllowedOrigins__0` replaces `http://localhost:4200`. Add `Cors__AllowedOrigins__1 = http://localhost:4200` on the dev API and note it in section 6.
4. **EmailJS on the dev API:** add the 4 `EmailJsSettings__*` values. The user pastes them.
5. **Production, once the quota is approved:** create plan `apptbk-prod-plan` (Linux B1, East US 2) and apps `apptbk-api`, `apptbk-cpanel` and `apptbk-web`. Then add the settings (section 6), the GitHub `production` env, merge to `main`, and run the manual deploy. Add the `apptbk-web` azurewebsites host to `allowedHosts` in the frontend `angular.json`.
6. **Domain:** set up apptbk.com, www and api with DNS, managed certificates and HTTPS Only (section 7).
7. **Frontend repo:** create a `main` branch for production releases. Only `Development` exists on GitHub so far.
8. **Code review follow-ups:** add a `PhysicianBooking.Tests` project (the backend test gate currently runs 0 tests); add tests for the Google Places feature flag and the frontend SEO logic; stop hard-coding `www.apptbk.com` in `index.html`; settle Windows vs Linux for the production frontend in both DEPLOYMENT.md files.
9. **Later:** apply for Microsoft for Startups credits (section 6a), then SEO (sitemap, meta tags, structured data) and the nightly ZIP import job.

## 1. Repositories

| Repo | Contents | Visibility |
|---|---|---|
| `apptbk-backend` | .NET 10: `PhysicianBooking.API` (REST API), `PhysicianBooking.CPanel` (admin MVC), `Services`, `Domain` | Public |
| `apptbk-client-app` | Angular 22 SSR (Node 22) | Private |

## 2. Environments

| | Dev | Production |
|---|---|---|
| Hosting | Existing Azure App Services (Windows, **F1 Free**, West Central US) | Resource group `apptbk-prod`, plan `apptbk-prod-plan` (**Linux B1**, East US 2): apps `apptbk-api`, `apptbk-cpanel`, `apptbk-web` |
| Website | `dev.apptbk.com` (or `*.azurewebsites.net`) | `www.apptbk.com` and `apptbk.com` |
| API | `api-dev.apptbk.com` (currently `physicianbookingapi20260728210051-…azurewebsites.net`) | `api.apptbk.com` |
| Admin (CPanel) | `admin-dev.apptbk.com` | `admin.apptbk.com` |
| Database | Neon project `ApptBk` (fancy-shadow-37840064), branch `dev` | Same project, branch `production` (default) |
| Git branch | `Development` | `main` |
| Indexed by Google | No (`noindex`) | Yes |

## 3. Branching and release flow

```
feature branch ──PR──▶ Development ──PR──▶ main ──(manual "Run workflow")──▶ Production
                         │
                         └─ auto-deploys to Dev
```

## 4. Pipeline gates

Workflow files: `.github/workflows/backend-ci-cd.yml` and `.github/workflows/frontend-ci-cd.yml`.

| Gate | Trigger | What happens |
|---|---|---|
| 1. Build & test | Pull request into `Development` or `main` | Restore, build, unit tests. A failure blocks the merge, provided branch protection is on. |
| 2. Dev | Push to `Development` | Build and test, deploy to the `dev` environment, then a smoke test (`/health` for the API, the home page for the frontend) |
| 3. Production | **Manual only:** Actions → workflow → **Run workflow**, branch `main`, type `deploy-production` | Build and test, deploy to the `production` environment, then a smoke test |

Pushing to `main` only builds and tests; it never deploys. The `production` environment only accepts the `main` branch.
Only users with write access can start the workflow by hand.

### Releasing to production
1. Open a PR from `Development` into `main` and merge it once checks are green.
2. Go to **Actions → Backend CI/CD → Run workflow**, choose branch `main`, type `deploy-production`, and click **Run**.
3. Do the same for **Frontend CI/CD**. Deploy the backend first if the API changed.
4. Check that the smoke-test step is green, then open the site.

### Rollback
Revert the bad commit on `main` (via a PR), then run the production workflow again.
For an emergency, in the Azure Portal go to App Service → **Deployment Center → Logs** and redeploy the previous deployment.

## 5. GitHub setup (once per repo)

**Settings → Environments**
- `dev`: no rules.
- `production`: under Deployment branches, allow only `main`.

Environment secrets and variables use the same names in `dev` and `production`, with different values:

| Repo | Secrets | Variables |
|---|---|---|
| backend | `API_PUBLISH_PROFILE`, `CPANEL_PUBLISH_PROFILE` | `API_APP_NAME`, `CPANEL_APP_NAME`, `API_URL` (no trailing slash) |
| frontend | `FRONTEND_PUBLISH_PROFILE` | `FRONTEND_APP_NAME`, `SITE_URL` |

Get each publish profile from Azure: App Service → **Overview → Download publish profile**, then paste the whole XML.
First turn on SCM Basic Auth: Configuration → General settings → **SCM Basic Auth Publishing Credentials = On**.

**Settings → Branches:** add rules for `main` and `Development`:
- require a pull request
- require the status check **Build & test**

**Azure Deployment Center:** if it is connected to GitHub, disconnect it. Otherwise every push deploys twice.

## 6. Azure App Service configuration

Set these under App Service → **Settings → Environment variables**. Never put them in `appsettings.json`.

**API app**
| Name | Value |
|---|---|
| `ConnectionStrings__Postgres` | Neon connection string (the `dev` branch for Dev, `production` for Prod) in **key=value** format, no quotes: `Host=<endpoint>-pooler.<region>.aws.neon.tech;Database=neondb;Username=neondb_owner;Password=<password>;SSL Mode=Require`. In Neon: **Connect → .NET** gives this format. The `postgresql://...` URL format does **not** work with Npgsql. |
| `GoogleApi__ApiKey` | Google Places key. **Dev only**: production doesn't need it because the site reads from the DB. |
| `Features__GooglePlacesImport` | `true` on **Dev only**. Turns on the paid Google import endpoints `/api/GooglePlace/*`. Leave unset in Prod (defaults to off, which returns 404). |
| `EmailJsSettings__ServiceId` / `__TemplateId` / `__PublicKey` / `__PrivateKey` | EmailJS values |
| `Cors__AllowedOrigins__0` | Prod: `https://www.apptbk.com`, Dev: the dev site URL |
| `Cors__AllowedOrigins__1` | Prod: `https://apptbk.com` |

**CPanel app:** `ConnectionStrings__Postgres` (same format as the API).

**Uploads:** never commit runtime uploads (`**/wwwroot/uploads/` is git-ignored). A large deploy package fails on F1 (OneDeploy 500).

**Runtime stacks**
- Production (Linux): API and CPanel use **.NET 10 (LTS)**. Azure no longer offers .NET 9, which reached end of support in Nov 2026. The frontend uses **Node 22** with the startup command `node server.js`. Linux ignores `web.config`.
- Dev (Windows F1): the frontend runs through iisnode and `web.config`.

## 6a. Costs, quota and startup credits

| Item | Monthly cost |
|---|---|
| `apptbk-prod-plan` Linux B1 (all 3 prod apps share it) | ~$12.41 |
| Dev apps (F1 Free) | $0 |
| Neon (free tier, ~32 MB of 0.5 GB) | $0 |
| Application Insights (first 5 GB/month free) | ~$0 |
| **Total** | **~$13** |

Avoid Windows B1 (~$55/month) and Premium plans. Azure suggests Premium V3 by default when you create an app, so change it to B1.

**Quota:** New or free-trial subscriptions start with a **B1 quota of 0** ("Operation cannot be completed without additional quota"). To raise it, open **Quotas** (aka.ms/azure-quota) → Provider **App Service** → region **East US 2** → **B1** → New quota request → **3**. You may need to upgrade to pay-as-you-go first.

**To do: apply for Microsoft for Startups (Founders Hub) credits**
1. Go to startups.microsoft.com and choose **Sign up**. Use a personal Microsoft account; some guides say it must not already have Azure.
2. Enter the startup details (ApptBk, apptbk.com, stage) and verify through LinkedIn. A credit card is needed for identity checks.
3. Expected credits: about $1,000 at first, then up to ~$5,000 after business verification, and more at later stages. Check the current amounts. Credits must be activated within about 90 days and expire after a period.
4. The credits arrive as a separate **Azure Sponsorship** subscription. Once you have it, move resource group `apptbk-prod` into it (Resource group → **Move → to another subscription**). Then check custom domains and certificates, and download the publish profiles again for GitHub.

## 7. Custom domain (apptbk.com)

For each production app, go to **Custom domains → Add custom domain**:
- Domain provider: All other domain services
- Certificate: **App Service Managed Certificate**

Then add the DNS records Azure shows you at the domain registrar:

| Type | Host | Value | App |
|---|---|---|---|
| A | `@` | Azure IP | frontend |
| TXT | `asuid` | verification ID | frontend |
| CNAME | `www` | `<frontend>.azurewebsites.net` | frontend |
| TXT | `asuid.www` | verification ID | frontend |
| CNAME | `api` | `<api>.azurewebsites.net` | API |
| TXT | `asuid.api` | verification ID | API |
| CNAME | `admin` | `<cpanel>.azurewebsites.net` | CPanel |
| TXT | `asuid.admin` | verification ID | CPanel |

Custom domains with SSL need the **B1** plan or higher.
After binding, turn on **HTTPS Only** under Configuration → General settings.

## 8. Local development

Secrets come from `dotnet user-secrets`; both projects already have a `UserSecretsId`. Set them for each project:

```bash
cd PhysicianBooking.API
dotnet user-secrets set "ConnectionStrings:Postgres" "<Neon dev connection string>"
dotnet user-secrets set "GoogleApi:ApiKey" "<key>"
dotnet user-secrets set "EmailJsSettings:ServiceId" "<id>"   # and TemplateId, PublicKey, PrivateKey

cd ../PhysicianBooking.CPanel
dotnet user-secrets set "ConnectionStrings:Postgres" "<Neon dev connection string>"
```

## 9. Troubleshooting

| Symptom | Cause / fix |
|---|---|
| CORS error in the browser | `Cors__AllowedOrigins__N` is missing the exact site origin (no trailing slash) |
| Smoke test fails | Check App Service → Log stream. Usually a missing environment variable. |
| API data endpoints return 500 but `/health` is "Healthy" | `/health` does not touch the DB. Read Kudu `LogFiles/eventlog.xml`. `Couldn't set postgresql://...` means the connection string is in URL format; `Format of the initialization string does not conform...` means stray quotes or leftover URL parts. Use the key=value format (section 6). |
| Frontend `/` returns 403 | `web.config` must not skip directories (the `IsDirectory` condition), or the site root never reaches `server.js`. |
| Frontend pages return 400 Bad Request | The host is missing from `security.allowedHosts` in `angular.json` (Angular SSR rejects unknown hosts). |
| Deploy fails with OneDeploy 500 | The package is too big for F1. Look for large committed files (e.g. `wwwroot/uploads`). |
| Site works on a phone but not on your PC ("connection reset" in curl or Chrome) | A router or ISP security filter is blocking the `*.azurewebsites.net` name (seen with a home router's built-in security service). Allow the host names there. |
| Deploy step 401 | SCM Basic Auth is off, or the publish profile is stale. Download it again. |
| Prod job skipped | The run was not started by hand on `main`, or the confirm word was not `deploy-production` |
| Certificate pending | DNS not propagated yet. Check with `nslookup api.apptbk.com`. |
