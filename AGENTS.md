# AGENTS.md: apptbk-backend

.NET 10 backend for ApptBk (physician/professional booking). PostgreSQL (Neon) via Dapper.

## Context budget rules (read first)
- **Never read or search:** `bin/`, `obj/`, `.vs/`, `PhysicianBooking.CPanel/wwwroot/` (about 4.9k vendor theme files: assets, dist, lib, uploads).
- Scope searches with a path or glob, e.g. `--glob '*.cs'` under one project. No repo-wide greps.
- Read the file map below before exploring. Read only the files the task needs, and use offset/limit on files over 200 lines (`GooglePlacesService.cs` is 409 lines; `Domain/Modal/PlacesResponse.cs` is 344).
- Don't re-read files you just edited. Keep answers short. Don't paste whole files.
- Never print secrets. Values live in user-secrets or Azure, not in `appsettings.json`.

## File map
| Path | What |
|---|---|
| `PhysicianBooking.API/Program.cs` | DI, CORS (`Cors:AllowedOrigins`), Swagger (Dev only), `/health` |
| `PhysicianBooking.API/Controllers/` | `GooglePlace`, `Inquiry`, `Leads`, `Physio`, `Specialties` (+ template `WeatherForecast`) |
| `PhysicianBooking.Services/Interface/` | `I*Repository`, `IEmailService`, `IGooglePlacesService` |
| `PhysicianBooking.Services/Implementation/` | Dapper repositories, `GooglePlacesService`, `EmailJsService` |
| `PhysicianBooking.Domain/` | Models (`Modal/`), view models (`ViewModel/`), `PagedResult` |
| `PhysicianBooking.CPanel/` | Admin MVC: `Controllers/Home` (login), `Controllers/Leads`, `Views/` |
| `PhysicianBooking.API/PhysicianBooking.API.sln` | The solution (includes all projects) |
| `.github/workflows/backend-ci-cd.yml` | CI/CD pipeline |
| `docs/DEPLOYMENT.md` | Environments, gates, Azure/GitHub setup |
| (private) `apptbk-client-app/docs/CODE-REVIEW.md` | Known issues and backlog (kept out of this public repo) |

## Commands
```bash
dotnet build PhysicianBooking.API/PhysicianBooking.API.sln -c Release -v q
dotnet test  PhysicianBooking.API/PhysicianBooking.API.sln
dotnet run --project PhysicianBooking.API      # http://localhost:5192, https://localhost:7271
```

## Conventions
- Branches: work on a feature branch, open a PR into `Development` (auto-deploys to Dev), then a PR into `main`. Production deploys only by a manual workflow run.
- Configuration keys: `ConnectionStrings:Postgres`, `GoogleApi:ApiKey`, `EmailJsSettings:*`, `Cors:AllowedOrigins`. In Azure, use `__` instead of `:`.
- New services need an interface in `Services/Interface` and must be registered in `Program.cs`.
- SQL must stay parameterized (Dapper params). Never concatenate strings into queries.
- New code needs unit tests (xUnit + Moq, in a `PhysicianBooking.Tests` project, to be created).
