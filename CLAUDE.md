# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

A take-home coding exercise (see `REQUIREMENTS.pdf`, extract with `pdftotext -layout REQUIREMENTS.pdf -`). The
starting point is a near-empty ASP.NET Core 8 web project (`TestProject`) with one stub controller and a
"Hello World" `wwwroot/index.html`. The deliverable is a **file & directory browser**: a JSON Web API plus a
single-page app, both written from scratch. The reviewers evaluate original code and architecture in a code
review, so the design of the classes matters more than polish.

### Requirements (from the PDF)

Required:
- JSON Web API to **browse and search** files and folders under a server-side home directory that is
  **configurable via a variable** (e.g. appsettings / env var).
- **Deep-linkable** SPA: UI state (current path, search, etc.) lives in the URL.
- **All HTML rendered client-side** by vanilla JavaScript or TypeScript. No server-side HTML rendering and no
  React/Angular/other UI libraries.
- **Upload and download** files from the browser.
- Show **file and folder counts and total sizes** for the current view.
- Must build with VS 2022+, Rider, VS Code, or the command-line SDK.

Bonus: delete/move/copy files and folders; the whole component contained in a dialog widget opened by a trigger
element; performance (explicitly "highly valued"); anything else interesting.

Guidance from the PDF that shapes how to work here:
- "Simple is better": minimal boilerplate, delete default/template files that aren't used.
- Prefer substantial original C# and JavaScript over framework/template usage.
- Styling is unimportant; functionality and code quality are what get reviewed.

## Commands

The project targets `net8.0`; the installed SDK is .NET 10, which builds it fine.

```bash
dotnet build                      # build
dotnet run                        # run; serves https://localhost:7146 and http://localhost:5120
dotnet watch run                  # hot reload during development
dotnet run --launch-profile TestProject   # same as `dotnet run` (profile from Properties/launchSettings.json)
```

There are no tests yet. If a test project is added, use `dotnet test` and
`dotnet test --filter "FullyQualifiedName~ClassName.MethodName"` to run a single test.


## Structure

- `Program.cs` — minimal host: `AddControllers()`, `UseHttpsRedirection()`, `UseStaticFiles()`,
  `MapControllers()`. Static files are served from `wwwroot/`, so the SPA is just `wwwroot/index.html` plus
  whatever JS/TS is added alongside it.
- `Controllers/TestController.cs` — placeholder API controller (`GET /Test` returns a string). Attribute-routed
  via `[Route("[controller]")]`. Replace or delete it; it exists only as a starting point.
- `wwwroot/index.html` — placeholder page, launched by default (`launchUrl: index.html`).
- `appsettings*.json` — logging config only so far; the configurable home directory belongs here (bound via
  `IConfiguration`/options) or in an environment variable.
- `Properties/launchSettings.json` — the `IIS Express` profile still references `weatherforecast` from the
  template and is dead; the `TestProject` profile is the one that matters.

Nullable reference types and implicit usings are enabled in the csproj.

## Committing

The Git history is part of the deliverable: reviewers will read it to understand how the problem was approached.

- **Every commit is independent and self-contained.** Each one builds and runs on its own and makes sense
  without the commits around it. Don't string related changes across several commits, and don't bundle
  unrelated changes into one.
- **Keep scope focused.** When a change touches separate, unrelated concerns, split it into multiple commits.
- **Messages explain what and why**, succinctly. A short subject line, and a brief body when the reasoning
  isn't obvious from the diff. A reviewer should be able to follow the design decisions from the log alone.
- **Recommend commit checkpoints** as work progresses rather than letting changes pile up.
- **Never commit without confirmation.** Before any commit, show a summary of exactly what will be committed
  (files and the proposed message) and wait for approval.
