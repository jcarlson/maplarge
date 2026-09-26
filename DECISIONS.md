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
path against the app's content root, and handed to `FileSystemService` as a constructor argument. There is no
options class. The checked-in default is `.`, the project directory, so the app runs out of the box; missing
or non-existent directories fail at startup.

**Why:** One value does not justify an options class and `IOptions<T>` plumbing. Taking the directory as a
constructor string keeps `FileSystemService` free of framework types, so tests build it directly against a
temp directory. Resolving relative values against the content root rather than the working directory makes
the behaviour independent of where `dotnet run` is invoked from. Failing fast at startup surfaces
misconfiguration immediately instead of on the first request.

## 5. Listings return paths relative to the base directory

**Decision:** `FileSystemService.ListDirectory()` returns entries as paths relative to the base directory, sorted
ordinally, and never exposes the absolute location of the base.

**Why:** The base directory is a server-side detail; clients only need to address entries within it, and
hiding the absolute prefix avoids leaking filesystem layout to the browser. Ordinal sorting makes the output
deterministic across platforms, since the OS enumeration order is unspecified.

## 6. Client paths are normalised, then checked for containment

**Decision:** A client-supplied path is joined onto the base directory, fully normalised (`.` and `..`
segments collapsed, repeated and trailing separators removed), and rejected if the result does not lie
inside the base. `Path.Join` rather than `Path.Combine` is used so a leading `/` cannot replace the base;
`/foo/bar` and `foo/bar` both mean `<base>/foo/bar`. This lives in `PathResolver`, a class with no
filesystem dependency, so the rule is unit-tested without creating directories.

**Why:** One general invariant, "the resolved path starts with the base", covers every way of spelling an
escape without enumerating them. An explicit ban on `.` and `..` segments was considered and dropped: it
adds a second rule to maintain, and any further special segment someone might worry about (a `~`, a drive
letter on Windows) is either not expanded by .NET at all or is caught by the same containment check. The
trade-off is that inputs like `foo/../bar` are accepted and normalised to `bar` rather than rejected, which
is harmless because they cannot reach anything outside the base.

## 7. Listing endpoint returns a bare JSON array; the controller maps the lister's exceptions to status codes

**Decision:** `GET /api/dir?path=<relative path>` returns the entries from `FileSystemService.ListDirectory` directly,
serialised as a JSON array of strings such as `["notes.txt","photos"]`, with no wrapping object. The
controller catches the two exceptions the lister documents and maps `ArgumentException` (path escapes
the base) to 400 and `DirectoryNotFoundException` to 404; nothing else is caught, so unexpected failures
still surface as 500s. API routes live under `/api` so they cannot collide with static files served from
`wwwroot`.

**Why:** The lister already produces exactly what the client needs, so wrapping it would add a shape to
maintain for no information gain; if a listing later needs metadata (counts, sizes), the response can grow
into an object at that point. Mapping exceptions inside the one action rather than in a global exception
filter keeps the mapping next to the code that knows what those exceptions mean; a filter becomes worth
having once several endpoints share the same mapping. Because the controller holds no logic of its own,
its tests call the action directly against a `FileSystemService` over a temp directory, which checks the
status-code mapping without hosting the app.

## 8. `GetFileInfo` describes a file instead of opening it

**Decision:** `FileSystemService.GetFileInfo(path)` resolves the path with the same containment rule as
`ListDirectory` and returns a `FileInfo` for it. It opens no handle. A path that resolves to a directory,
or to nothing, throws `FileNotFoundException`; an escaping path throws `ArgumentException` as before. The
absolute path therefore reaches the controller via `FileInfo.FullName`; it is server-side only and must
never be written into a response.

**Why:** A `ReadFile` method returning an open `Stream` was considered and rejected. It would make the
service hand out a handle the caller must remember to dispose, and in a controller the stream has to
outlive the action anyway, so the open and close could never sit in one method. Returning a `FileInfo`
lets the controller use MVC's physical-file result, which opens and closes the file itself, serves it
through Kestrel's sendfile path with no managed-buffer copy, and handles Range requests, so the fastest
download path is available without the service knowing about HTTP. Treating a directory as "no file"
keeps one exception type meaning 404 for the controller. `FileInfo` also carries the length and timestamps
that size totals will want later, though that was not the reason for the choice.
