# AGENTS.md

Course practice repo (kontur-web-courses "web-api"). The task statement is `README.md` (8 tasks, Russian).
The executable spec is `Tests/` — make tests pass, never edit tests to make them pass.

## Layout

- `WebApi.MinimalApi/` (net8.0) — the project you actually edit:
  `Controllers/UsersController.cs`, `Models/*Dto.cs`, `Program.cs` (DI, AutoMapper profile, MVC formatters),
  `Domain/` (`IUserRepository`, `InMemoryUserRepository` with seed data).
- `WebApi/` (net5.0) — legacy skeleton (only `Program.cs`/`Startup.cs`, no controllers). README paths refer
  to this project, but nothing references it. Do not edit.
- `Tests/` — NUnit harness; references `WebApi.MinimalApi` (which declares `InternalsVisibleTo Tests`).
- `reveal/`, `web-api.html*`, `assets/` — course slides, not code.

## Commands

- `dotnet build Tests` — fastest build (builds the app project too).
- `dotnet test Tests --filter "FullyQualifiedName~Task3"` — run one task (42 tests total, Task1–Task8).
- `dotnet test Tests` — all fixtures. **Ignores** the allowlist in `Tests/Program.cs`; most tasks fail until
  implemented, which is expected.
- `dotnet run --project Tests` — NUnitLite runner; runs **only** fixtures uncommented in `Tests/Program.cs`
  (only `Task1` is enabled by default).
- `dotnet build web-api.sln` / `dotnet run --project WebApi.MinimalApi` work; SDKs 5/6/8 are installed.

## Testing model (README is stale here)

- Tests boot the app **in-process** via `WebApplicationFactory` (`Tests/AppFactory.cs`, environment `Tests`).
  No server to start, no port, no dev certs needed to run tests.
- Ignore README claims about `https://localhost:5001` and about setting `BaseUrl` in `Tests/Configuration.cs`:
  `BaseUrl` is never read anywhere. The app hardcodes `http://localhost:5000`
  (`builder.WebHost.UseUrls(...)` in `WebApi.MinimalApi/Program.cs` + `Properties/launchSettings.json`).
- `dev-certs/` scripts are only for trusting HTTPS when opening the legacy project in a browser.

## Conventions and gotchas

- README says the work happens in `WebApi`; the real target is `WebApi.MinimalApi`. Likewise
  `WebApi/Samples/AutoMapperTests.cs` in README is actually `WebApi.MinimalApi/Samples/AutomapperTests.cs`
  (runnable NUnit samples; Task1/Task3 hints point at `TestFillBy` / `TestFillByReturnSyntax` there).
- Tests assert exact response shape: camelCase JSON, `Content-Type: application/json; charset=utf-8`,
  XML only with `Accept: application/xml`, 406 for unsupported `Accept`, no body/headers on 404/204.
- `.vscode/launch.json` "Launch WebApi" is broken (path typo `WebApi.MinimapApi`); use `dotnet run` or Rider.
- No CI, no lint/format step — only `.editorconfig` (4-space indent for C#, 2-space for JSON/csproj).
- `Tests/Program.cs` doubles as the "which tasks to run" allowlist for the NUnitLite entry point.
