# Architecture Decisions

A running log of the key decisions made while building this project, in the order they were made. Each entry
records what was decided and why, so a reviewer can follow the reasoning without reconstructing it from the
diff history.

## 1. Local-only, HTTP-only hosting

**Decision:** The app runs only on a developer's machine over plain HTTP (`http://localhost:5120`). HTTPS,
TLS certificates, reverse proxies, and IIS hosting are out of scope.

**Why:** This is a proof-of-concept exercise, not a deployed service. Production hosting concerns would add
configuration and setup friction for reviewers without demonstrating anything about the file-browser design.
Removing the HTTPS redirect middleware, the HTTPS launch URL, and the IIS Express profile keeps the project
minimal and avoids the certificate-trust prompts and redirect warnings that come with local HTTPS.

## 2. Single environment, single configuration file

**Decision:** There is one environment. All configuration lives in `appsettings.json`; there is no
`appsettings.Development.json` or other per-environment override.

**Why:** With only local runs, environment-specific overrides have nothing to override. The template's
Development file duplicated `appsettings.json` exactly. The launch profile still sets
`ASPNETCORE_ENVIRONMENT=Development` so ASP.NET Core's developer exception page stays on, which is useful
while iterating.
