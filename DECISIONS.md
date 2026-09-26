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

## 3. Symbolic links and other special files are out of scope

**Decision:** Only regular files and directories are modeled. Symbolic links, devices, sockets and pipes
are excluded, and will be omitted from listings rather than reported or followed.

**Why:** Following links would let a link inside the root point anywhere on disk. Reporting them as a
distinct entry type is a reasonable feature but not needed for the exercise; hiding them is the simplest
safe behavior and can be revisited if time allows.

## 4. Base directory is a single configuration value, resolved against the content root

**Decision:** The browsable root is read from `FileBrowser:BaseDirectory` in `appsettings.json` (or the
`FileBrowser__BaseDirectory` environment variable) as a plain string in `Program.cs`, resolved to an absolute
path against the app's content root, and handed to `DirectoryLister` as a constructor argument. There is no
options class. The checked-in default is `.`, the project directory, so the app runs out of the box; missing
or non-existent directories fail at startup.

**Why:** One value does not justify an options class and `IOptions<T>` plumbing. Taking the directory as a
constructor string keeps `DirectoryLister` free of framework types, so tests build it directly against a
temp directory. Resolving relative values against the content root rather than the working directory makes
the behaviour independent of where `dotnet run` is invoked from. Failing fast at startup surfaces
misconfiguration immediately instead of on the first request.

## 5. Listings return paths relative to the base directory

**Decision:** `DirectoryLister.List()` returns entries as paths relative to the base directory, sorted
ordinally, and never exposes the absolute location of the base.

**Why:** The base directory is a server-side detail; clients only need to address entries within it, and
hiding the absolute prefix avoids leaking filesystem layout to the browser. Ordinal sorting makes the output
deterministic across platforms, since the OS enumeration order is unspecified.
