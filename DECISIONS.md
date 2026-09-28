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
are not modelled or detected: a listing reports whatever the operating system reports for the entry,
and a link is neither marked as one nor filtered out.

**Why:** Reporting links as a distinct entry type is a reasonable feature but not needed for the
exercise. The base directory is the operator's own folder, uploads only ever create regular files, and
nothing in the app creates a link, so a link that points outside the base can only be one the operator
placed there. Detecting and hiding links was considered and deferred; it belongs with delete, move and
copy, which are the first operations that would have to decide what a link means.

## 4. Base directory is a single configuration value, resolved against the content root

**Decision:** The browsable root is read from `FileBrowser:BaseDirectory` in `appsettings.json` (or the
`FileBrowser__BaseDirectory` environment variable) as a plain string in `Program.cs`, resolved to an absolute
path against the app's content root, and handed to `FileSystemService` as a constructor argument. There is no
options class. The checked-in default is `public`, a seeded sample directory in the repository (see the
README), so the app runs out of the box; a missing or non-existent directory fails at startup.

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
now appears in three actions, the threshold decision 9 set for moving it into an exception filter. On
reflection it stays in the actions: each action catches a different set (`FileNotFoundException` for a
download, `DirectoryNotFoundException` for a listing, `EntryExistsException` only here), so a shared
filter would either map exceptions an action never expects or grow per-action rules, and four short
catch blocks next to the documentation of what each status means are easier to read than either.

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

## 15. UI state lives in the URL fragment as `#/path/to/dir?key=value`, converted by a pure `url-state` module

**Decision:** The SPA keeps its state in the URL fragment rather than in the path via the History API.
The fragment reads like a path plus a query, `#/photos/2024?q=summer`: the directory being viewed, then
any number of generic `key=value` pairs. `wwwroot/js/url-state.js` converts between that string and a plain
`{ pwd, query }` object in both directions (`parseState`, `serializeState`) and never touches the DOM;
reading and assigning `location.hash` is left to the caller. `pwd` always carries a leading `/` and is
`/` for the base; `query` is an object of every pair after the `?`, so the module does not know what
`q` or any future key means and needs no change when one is added. Each path segment is percent-encoded
on its own, the pairs go through `URLSearchParams`, and the `?` is omitted when there are no pairs.
Parsing is deliberately tolerant of what a hand-edited link can contain: a missing `#` or leading `/`,
repeated or trailing slashes, and a percent-escape that does not decode is kept as typed rather than
failing the whole page.

**Why:** Deep-linking is a requirement, and both the fragment and the History API satisfy it, but the
History API needs the server to answer every non-API path with `index.html`. That is one line in
`Program.cs`, yet it couples the client to server routing and creates a namespace clash: a directory in
the browsable root named `js` or `api` would produce URLs that the static file middleware or the API claim
first, which would force a `/browse/` prefix to avoid. The fragment needs no server cooperation, which
also matters for the "component in a dialog on a host page" bonus: a hash-driven browser works on any page
at any path, whereas a path-driven one requires the host page to be served at every URL the component can
produce. Back and forward navigation come free too, since assigning `location.hash` creates a history
entry and fires `hashchange`. The path-like layout was chosen over a flat query string
(`#path=photos%2F2024&q=summer`) because it is what a person expects to read and type in a link, at the
cost of a few lines to split the path from the query. Keeping the conversion in a DOM-free module follows
decision 14: it is tested on Node in both directions, including round-trips of paths containing `?`, `#`
and `%`, and `App` remains the only code that reads the browser's location.

## 16. `.` and `..` rows are added on the client, through a row model the view copies verbatim

**Decision:** Every directory listing starts with `.` (the directory itself) and `..` (its parent, the
base being its own parent), as `ls -a` shows them. They are not part of the API's `Listing`: the client
adds them in `listingRows(pwd, listing)` in `wwwroot/js/view-helpers.js`, which turns the current path
and the server's listing into an array of `{ name, href, size }` rows that the view copies onto table
rows without looking at them. `App` passes the `pwd` it navigated to along with the listing so the view
can be given both.

**Why:** `Listing` is the shape of every listing-style response, including search results (decision 13),
where `.` and `..` have no meaning, and the file and folder counts and total size that the UI must show
are about the real entries, so pseudo-entries would have to be filtered back out of a listing that
included them. Injecting them on the server would therefore complicate the model to serve one
presentation choice. On the client they cost a few lines in a pure module and are tested like every
other view decision. The row model is a small step beyond the per-entry helpers used until now, taken
because `.` and `..` are not entries and would otherwise have needed special cases inside the view, which
decision 14 keeps free of decisions; with rows, the view is the same loop for every kind of row.

## 17. The `Listing` wrapper is gone: the API returns arrays of `FileSystemEntry`, and the client counts and totals

**Decision:** `GET /api/dir` and `GET /api/search` now return a plain JSON array of `FileSystemEntry`
(the record previously named `Entry`, moved to `Models/FileSystemEntry.cs`). The `Listing` class from
decisions 10 and 11, with its `Entries` and `TotalSize`, is deleted; the directories-first ordinal
ordering it applied now lives in `FileSystemService`, which returns an ordered
`IReadOnlyList<FileSystemEntry>` from both `ListDirectory` and `Search`. The file and folder counts and
the total size shown under a listing are computed on the client, in `listingSummary` in
`wwwroot/js/view-helpers.js`, from the entries it already has.

**Why:** Once the client was counting folders and files for the footer (decision 16 put that with the
other view decisions), it had every entry in hand and summing their sizes was one more line beside the
counts; sending a total the client could trivially derive made the response carry two representations
of the same fact, and a wrapper object whose only other member was the array. Removing it leaves one
model on the server, one array on the wire and one place, the client, that decides what a listing
displays, which is where `.` and `..` and the size formatting already were. Ordering stays on the server
for the reason decision 10 gave: it is the same on every platform and the client renders the list as
given. This reverses part of decision 11, which is the expected outcome of building one step at a time:
the total was the right thing to add when the server was the only place that could compute it, and the
right thing to remove once it was not.

## 18. Search is scoped to a directory: `GET /api/search?path=&q=` walks `path` and below

**Decision:** `FileSystemService.Search(path, query)` starts its recursive walk at the directory `path`,
resolved through `PathResolver` like every other path, instead of always at the base; the controller
takes `path` as a query parameter alongside `q`, and an empty or omitted `path` still searches the whole
base. Result paths stay relative to the base, not to `path`, so an entry is addressed the same way
whichever view it appears in. A missing directory is a 404 and an escaping path a 400, as for a listing.

**Why:** The UI's state is a directory plus an optional query (decision 15), so `#/photos?q=cat` reads as
"cat, within photos", and a global search would make the directory part of the URL meaningless while a
query is present. Scoping also makes clearing a search trivial, a link back to the same directory with
no query, and it is the cheaper search since it walks only the subtree; a global search remains one
click away, from the base. This is the `path` parameter decision 13 deferred until the UI needed it;
the order `(path, query)` follows the other service methods, which take the directory first.

## 19. `Router` owns the location, `View` the DOM, `App` the meaning; `main.js` only wires them

**Decision:** The browser is reached through two thin adapters. `Router` (`wwwroot/js/router.js`) is
the only code that touches `location`: it parses the hash into a `UrlState`, assigns a merged hash, and
reports each `hashchange`, over a `window` passed to its constructor. `View` is the only code that
touches the DOM: it renders what it is given and reports a submitted search to the App. Every decision
sits in `App`: `start()` subscribes to the router and navigates to the state the URL names now;
`navigate` chooses listing or search; `search(text)` turns the user's text into a URL change, dropping
the query when blank. `App` takes the `ApiClient`, the `Router` and the `View` in its constructor.
`App` and `View` need each other; the view is built first, knowing no app, and the App's constructor
closes the loop with `view.setApp(this)`. Until then the view's event reports go nowhere
(`this._app?.search(...)`). `main.js` builds the four objects and awaits `app.start()`, nothing else.

**Why:** The location is a dependency like the API, and while it had no object of its own the logic
about it (parsing, merging, what a submit means) accumulated in whichever module touched it, first
`main.js` and then `View`, both of which decision 14 leaves untested. Giving it a `Router` moves that
logic into `App`, where it is tested with fakes: the App tests drive a fake router through a state
change and assert what `search` asks the router to do, and the router itself is tested with a fake
window. `View` shrinks back to rendering plus one event report, registered in its constructor where the
form arrives. Giving `App` the view in its constructor means it is complete once built, and the one
setter, `setApp`, is the minimum that closes a genuine cycle; `start()` is then only about following the
URL, and one starts an app, not a view. This
supersedes decision 15's remark that `App` reads the location: `Router` does.

## 20. A failed load is shown in place of the table, under the breadcrumbs, and `navigate` never rejects

**Decision:** `App.navigate` catches whatever loading a directory or a search throws and calls
`view.showError(pwd, error)`; it therefore never rejects. The view keeps the breadcrumb bar for the
directory that failed and puts a message where the table would be. The wording is a pure, tested helper,
`errorMessage(pwd, error)`: a 404 says there is no directory at that path, a 400 that the path is not one
the browser can show, any other status that the server could not answer with that status, and anything
that is not an `ApiError`, such as a request that never reached the server, is reported as "Something
went wrong" with the error's own message. The search box is reset and the clear link hidden.

**Why:** The requirements say nothing about errors, but a deep-linkable browser will be given links to
directories that have since been renamed or deleted, and a blank page with an error in the console is
not an answer. Keeping the breadcrumbs is the recovery path: every ancestor is one click away, and the
base is always valid. Handling it in `App` rather than the view or the router keeps the decision where
the other decisions are and where it is tested with a fake API that rejects; it also settles the
question left by decision 19 of the promise the router's listener drops, since a function that never
rejects is safe to leave un-awaited. The catch is deliberately broad: a rendering bug would surface as
a "Something went wrong" message with its text rather than as silence, which is the better failure for
a user and no worse for a developer, who still has the message. Status codes are translated on the
client rather than sending messages from the server so the API stays a plain data API and the wording
can change without touching C#.

## 21. Upload is a form beside search: the file goes into the current directory and the view reloads

**Decision:** `index.html` has an upload form with a required file input, a submit button and an
`<output>`. The view reports the chosen file to `App.upload(file)`, which posts it through
`ApiClient.upload(pwd, file)` into the directory the URL names now, then navigates to the current
state again so the new file appears where the user is looking, in the listing or, during a search,
in the results if it matches. A failure is reported in the form's `<output>` by `uploadErrorMessage`
(409: the name is taken, 404: no such directory, 400: not an uploadable name, other statuses and
network failures as for a load) and nothing else on the page changes. Every navigation resets the
form, clearing the chosen file and any message. `ApiClient` sends a `FormData` with one `file` part,
and its `_getJson` grew a `RequestInit` parameter rather than gaining a second request helper.

**Why:** Uploading into "where I am" is what a file browser means by upload, and it needs no extra
UI to pick a destination: the breadcrumbs already show it. Reloading the current state after success
reuses `navigate` instead of inserting a row by hand, which keeps ordering, counts and totals right
for free and works identically for a listing and a search. An upload failure is not a navigation
failure, so it must not replace the table the way decision 20 does for a load: the user's place is
intact and the message sits next to the thing they were doing. `<output>` is the element made for a
form's result and takes part in `form.reset()`, so one reset call clears both the file and the message.
The multipart request was checked end to end against the running server from Node's `FormData`, since
a mismatch in the part name is the kind of bug unit tests with a fake `fetch` cannot see.

## 22. Entry paths use `/` on every platform

**Decision:** `FileSystemEntry.From` replaces the platform's directory separator with `/` in the
relative path it builds, so `GET /api/dir` and `GET /api/search` return `photos/cat.jpg` on Windows as
well as on macOS and Linux. Paths coming in from the client are unaffected: `PathResolver` already
accepts either separator on Windows through `Path.GetFullPath`.

**Why:** `Path.GetRelativePath` returns `photos\cat.jpg` on Windows, and the client splits paths on
`/` to show an entry's name, build the breadcrumbs, find the parent for `..`, and strip the searched
directory from a search result. On Windows every one of those displayed the whole path as a single
segment and `..` went to the base rather than the parent. The C# tests did not catch it because they
built their expected paths with `Path.Combine`, which hides the separator; they now spell out `/`, so
the same expectation holds on every platform. The separator is normalised on the server rather than
tolerated on the client because the path is part of the API's contract, and one representation on the
wire is simpler than two.

## 23. Listings and search results are not paged

**Decision:** `GET /api/dir` and `GET /api/search` return every entry in one array, and the client
renders every row. There is no page size, cursor or result cap.

**Why:** A scope cut. Paging is the right answer for a directory or a search with tens of thousands of
entries, and the shape is clear: a page size and cursor on both endpoints (ordering is already
deterministic, so a cursor is a path), and on the client either pagination controls or incremental
rendering that appends each page's rows instead of replacing the table. It was left out to keep the
project to the exercise's size; the README's notes to the reviewer say the same, so the omission is
visible rather than discovered.
