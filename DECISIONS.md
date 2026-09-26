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

## 9. Downloads are served as a physical-file result

**Decision:** `GET /api/file?path=<relative path>` takes the `FileInfo` from `GetFileInfo` and returns
MVC's `PhysicalFile` result with the file's own name as the attachment name, a MIME type looked up from
the extension via `FileExtensionContentTypeProvider` (falling back to `application/octet-stream`), and
range processing enabled. `path` is a required query parameter, so a request without one is rejected
with 400 by model validation before the action runs. As in the directory endpoint, `ArgumentException`
maps to 400 and `FileNotFoundException` to 404 inside the action.

**Why:** `PhysicalFile` owns the open and close of the file, streams it through Kestrel's sendfile path
without copying it through managed buffers, and implements HTTP Range and conditional requests, so it is
both the simplest and the fastest way to serve a download. Using the framework's extension map avoids
maintaining a MIME table by hand; `application/octet-stream` for unknown extensions makes the browser
save the file rather than try to render it. The exception-to-status mapping is now repeated in two
controllers; it is still two small catch blocks, so the exception filter anticipated in decision 7 is
deferred until a third endpoint makes the duplication worth removing.

## 10. Listings are a `Listing` view model built from `FileSystemInfo`, ordered directories-first on the server

**Decision:** `FileSystemService.ListDirectory` now returns a `Listing` instead of a list of path strings. `Listing`
is constructed from the base directory and an `IEnumerable<FileSystemInfo>` (what
`DirectoryInfo.EnumerateFileSystemInfos()` yields) and immediately maps each item to an `Entry(Path, IsDirectory)`
record, keeping none of the framework objects. An entry is a directory when the info `is DirectoryInfo`, not by
reading `FileSystemInfo.Attributes`. Entries are ordered directories first, then files, each group in ordinal
path order. The controller's only change is its return type; `GET /api/dir` now serialises as
`{"entries":[{"path":"photos","isDirectory":true},{"path":"notes.txt","isDirectory":false}]}`. Decision 5's
"ordinal order" clause is superseded by the directories-first rule; its "relative to the base" clause stands.

**Why:** The response needs more than a path per entry, and serialising `FileSystemInfo` directly was rejected
because its `FullName` and `DirectoryName` would leak the absolute base path that decision 8 keeps server-side.
A view model that maps in its constructor and exposes only plain properties means the controller stays a
one-line translation and the JSON shape is exactly the model's public surface. The same shape will serve
search results, which are likewise a set of entries with per-set metadata, so the class is named for what it
holds rather than for the directory endpoint. Deciding kind by type rather than `Attributes` matters for
testing: `Attributes` throws for a path that does not exist, whereas `FileInfo` and `DirectoryInfo` can be
constructed for any path, so `Listing` is unit-tested without touching the disk. Ordering is done on the server
so the client can render the array as given: the SPA has no test harness (adding one would drag in the build
tooling the exercise steers away from), so logic is kept in C# where it is tested, and only one display order
is planned. Recursive directory sizes (`du`-style) are out of scope; a directory listing behaves like `ls`.

## 11. Entries carry a file size; `Listing` totals them; reading the disk moves out of `Listing`

**Decision:** `Entry` gains a third positional property, `long? Size`: a file's length in bytes, `null` for a
directory. `Listing` gains `TotalSize`, the sum of the sizes of the entries it holds (directories contribute
nothing, an empty listing totals zero). `GET /api/dir` now serialises as
`{"entries":[{"path":"photos","isDirectory":true,"size":null},{"path":"notes.txt","isDirectory":false,"size":5}],"totalSize":5}`.
To make room for the size, the mapping from `FileSystemInfo` moves out of the `Listing` constructor into a
static `Entry.From(baseDirectory, info)`; `Listing` now takes `IEnumerable<Entry>` and only orders and totals.
`FileSystemService.ListDirectory` composes the two.

**Why:** A file's size has to come from the disk (`FileInfo.Length` throws for a path that does not exist),
which breaks the property decision 10 relied on for testing `Listing` without touching the disk. Rather than
give up that property, the class is split along the seam that now exists: `Entry.From` is the one place that
reads a `FileSystemInfo`, tested against real temp files, and `Listing` is pure ordering and aggregation over
`Entry` values, tested with hand-built entries. `Size` is `null` for a directory rather than zero so the
client can tell "no size" from "empty" without consulting `isDirectory`, and so a recursive directory total
can be added later without changing the meaning of an existing value. `TotalSize` is a `long` computed once
in the constructor; it counts only the files directly in the listing, matching decision 10's `ls`-not-`du`
scope. No extra I/O is incurred: `EnumerateFileSystemInfos` already populates the length of each `FileInfo`
it yields.

## 12. Uploads are a `POST` of one multipart file into a directory, and never overwrite

**Decision:** `POST /api/file?path=<directory>` takes a multipart form with one `file` part and writes it
into the directory `path` (the base itself when `path` is omitted) under the name the browser sent. The
target name must be free: an existing file *or* directory of that name is a 409 Conflict, so nothing is
ever overwritten. A directory that does not exist is a 404, an escaping path or a file name that is not a
single path segment (empty, `.`, `..`, or containing a separator) is a 400. Success is 201 with the new
`Entry` as the body and its download URL in `Location`. On the service side this is
`FileSystemService.WriteFileAsync(directoryPath, fileName, stream)`, which opens the target with
`FileMode.CreateNew` and returns `Entry.From` for the written file. The name clash is signalled with a new
`EntryExistsException` (an `IOException` subclass) so the controller can map it to 409 without catching
every I/O failure.

**Why:** The client names the directory and the server derives the resource name from the upload, which
is POST-to-a-collection semantics; PUT would mean the client addresses the exact file URI and replaces
whatever is there. Refusing to overwrite is the safer default for a browser widget where a user can pick a
file with a clashing name by accident, and it gives a single rule ("the name must be free") instead of one
behaviour for files and another for directories. Replacing a file becomes delete-then-upload once delete
exists. `CreateNew` enforces the rule at the OS level so two concurrent uploads of the same name cannot
both succeed; the explicit `Path.Exists` check before it exists only to give the directory clash the same
exception type on every platform. Restricting the file name to a single segment keeps the containment
invariant of decision 6 intact without re-running it on a joined path. One file per request keeps the
action a straight mapping and gives the client a status code per file; multi-file uploads are just
concurrent requests. The response body is the same `Entry` a listing would show so the client can insert
it without re-listing. `IFormFile` was chosen over reading the multipart body by hand: it buffers the
upload (to disk above 64 KB) before the action runs, which costs a second write for large files, but the
hand-rolled `MultipartReader` streaming path is several times the code; it is the obvious change to make
if upload throughput turns out to matter. With this third endpoint the `ArgumentException` → 400 mapping
now appears in three actions, which is the threshold decision 9 set for moving it into an exception filter.

## 13. Search is a recursive, case-insensitive name match served as a `Listing` from `/api/search`

**Decision:** `FileSystemService.Search(query)` walks the whole base directory recursively and returns a
`Listing` of every file and directory whose *own name* contains the query, compared ordinally and ignoring
case; the directories above an entry are never matched, so `photos` finds the `photos` directory but not
`photos/cat.jpg`. The walk uses `DirectoryInfo.EnumerateFileSystemInfos("*", EnumerationOptions)` with
`RecurseSubdirectories`, `IgnoreInaccessible` (an unreadable subdirectory is skipped instead of aborting the
search) and `AttributesToSkip = 0` (so hidden and system entries appear exactly as they do in a directory
listing). Matching is done in C# with `string.Contains`, not by passing `*query*` as the search pattern. A
blank query is an `ArgumentException`. `SearchController` exposes it as `GET /api/search?q=<text>`: 200 with
the listing, 400 for a missing or blank `q`; the search always starts at the base, and there is no `path`
parameter yet.

**Why:** Matching in C# rather than via the enumeration's search pattern avoids having to escape `*` and `?`
in user input and keeps the match rule (substring, ordinal, case-insensitive) in one obvious line; the
traversal, not the comparison, is the cost of a search, so nothing is lost. Case-insensitivity is chosen
because a user typing into a search box does not think about case, and ordinal comparison keeps the result
platform-independent. Name-only matching is what was asked for and matches how people search a file
browser; it also means a result set is not swamped by every descendant of a directory whose name happens to
match. Reusing `Listing` gives search results the same ordering, sizes and total as a directory listing for
free, which decision 10 planned for, and lets the SPA render both views with one code path. Blank queries
are rejected rather than returning everything under the base: such a response could be enormous and is
never what a search box means. Scoping a search to a subdirectory is a natural next step and would be a
`path` parameter resolved through `PathResolver`; it is left out until the UI needs it. Consistent with
decision 3, symbolic links are neither detected nor special-cased, so a directory link that forms a cycle
would be followed by the recursion; handling that belongs with the wider link decision, not here.

## 14. The SPA is plain JavaScript ES modules with no build step; its unit tests run on Node's built-in runner

**Decision:** The browser side is written in plain JavaScript as native ES modules under `wwwroot/js`,
loaded by a single `<script type="module">` in `index.html` and served exactly as written by the static
file middleware. There is no TypeScript, no bundler and no transpile step, so nothing runs between
`dotnet build` and the files a browser receives. Types at module boundaries (the API's `Listing` and
`Entry` shapes, the URL state object, and similar) are documented with JSDoc annotations so editors can
check them. Logic such as URL-state parsing, path handling, size formatting, sorting and the API client is
kept in modules that never touch the DOM, and those modules are unit-tested with Node's built-in test
runner (`node --test` with `node:assert`), which needs no npm packages. Node is therefore a tool for
running the JavaScript tests only; building and running the app requires nothing beyond the .NET SDK.

**Why:** The requirements allow vanilla JavaScript or TypeScript, and a browser cannot run TypeScript, so
choosing it always means adding a compile step. The options were weighed: the official
`Microsoft.TypeScript.MSBuild` package (7.x) bundles a native compiler and runs inside `dotnet build`
with no Node dependency, but it is brand new, needs a workaround before type errors fail the build, and
turns the served JavaScript into a build artefact; an npm-driven pipeline (tsc or esbuild via an MSBuild
`Exec` target) would put Node on every reviewer's build path, which the "must build with the SDK"
requirement argues against; JSDoc gives most of the editor-level type checking with none of that. Plain
modules keep the source, the served files and the Git diffs identical, so reviewers read exactly the code
that runs, and the "minimal boilerplate" guidance is honoured: no `tsconfig.json`, no compiler properties
in the csproj, no ignored output directory. At the size this project reaches (a handful of small
modules), that trade is clearly in favour of plain JavaScript; if the client grows beyond it, renaming
`.js` to `.ts` on already-annotated code is a mechanical change, so the decision is cheap to reverse. For
tests, the built-in runner was chosen over Vitest (a large dependency tree for what is needed here), Jest
(still awkward with ES modules) and browser-driven end-to-end tests via Playwright (a one-time browser
install every reviewer would need). Keeping DOM access out of the logic modules is what makes the
built-in runner sufficient: there is no need for a simulated DOM, and the split mirrors the thin
controllers and plain service classes on the server, the same approach to testability taken on the C#
side throughout this project.
