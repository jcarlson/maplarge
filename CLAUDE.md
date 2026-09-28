# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

A take-home coding exercise (see `REQUIREMENTS.md`). The
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

## Working step by step

**Do not implement features or functionality ahead of being asked.** The requirements are all visible, but the
work proceeds one deliberate step at a time so each design decision can be discussed. Build exactly what the
current request covers, no more. If a natural next step exists, mention it in a sentence rather than building it.
This applies to helper methods, factories, extra properties and "while I'm here" additions as much as to whole
features.

The corollary is that the design grows **organically**: a class, abstraction, or option is introduced only when a
concrete need for it appears in the current step, never in anticipation of a later requirement. Let the code stay
as simple as the features built so far allow, and refactor when the next feature actually demands it.

## Testing

**Every implementation ships with unit tests** in the same commit. A feature is not done until its behaviour is
covered, and the tests are part of what reviewers evaluate.

Design for testability: keep logic (path resolution, search, size aggregation, validation) in plain classes with
explicit dependencies, and keep controllers and file-system access thin so the interesting code can be tested
without a running host or a real directory tree. If a design choice makes something hard to test, treat that as
a signal to reconsider the design rather than to skip the test.

## Commands

The project targets `net8.0`; the installed SDK is .NET 10, which builds it fine.

```bash
dotnet build                      # build
dotnet run                        # run; serves http://localhost:5120
dotnet watch run                  # hot reload during development
dotnet run --launch-profile TestProject   # same as `dotnet run` (profile from Properties/launchSettings.json)
```

Unit tests (xUnit) live in `TestProject.Tests/`. Run them with `dotnet test`, or use
`dotnet test --filter "FullyQualifiedName~ClassName.MethodName"` to run a single test.

JavaScript unit tests live in `TestProject.Tests/js/` as `*.test.js` files and run on Node's built-in
runner (see `DECISIONS.md` #14): `node --test` from the repo root, or `node --test path/to/x.test.js`
for one file. Node 22+ is needed only for these tests, never to build or run the app.


## Structure

- `Program.cs` — minimal host: `AddControllers()`, `UseDefaultFiles()`, `UseStaticFiles()`,
  `MapControllers()`. HTTP only; see `DECISIONS.md`. Static files are served from `wwwroot/`, and `/`
  serves `wwwroot/index.html`, the SPA.
- `Controllers/DirectoryController.cs` — the JSON API, routed under `/api/dir`. `GET /api/dir?path=` lists a
  directory via `FileSystemService` as a JSON array of `FileSystemEntry` (400 if the path escapes the
  base, 404 if it does not exist).
- `Controllers/FileController.cs` — `GET /api/file?path=` serves a file via `FileSystemService.GetFileInfo`
  as a physical-file result: attachment named after the file, MIME type from the extension, range
  requests enabled (400 if the path escapes the base or is missing, 404 if there is no file there).
  `POST /api/file?path=` with a multipart `file` part writes the upload into the directory `path` via
  `FileSystemService.WriteFileAsync`, never overwriting: 201 with the new `FileSystemEntry` and a `Location` header,
  400 for an escaping path or a non-plain file name, 404 if the directory is missing, 409 if the name is taken.
- `Controllers/SearchController.cs` — `GET /api/search?path=&q=` finds files and folders anywhere under the
  directory `path` (the whole base when omitted) whose name contains `q` (case-insensitive) via
  `FileSystemService.Search`, returned as a JSON array of `FileSystemEntry` with base-relative paths (400
  if `q` is missing or blank or `path` escapes the base, 404 if the directory is missing).
- `wwwroot/index.html` — the SPA's page, served at `/`: the stylesheet link and the
  `<script type="module" src="js/main.js">` tag in the head (module scripts are deferred, so it runs
  after the DOM is parsed), a title, then two labelled sections: the `<form id="search">` with its `q`
  input, submit button and clear link, and the `<form id="upload">` with its required `file` input,
  submit button and `status` output, followed by the `<main id="listing">` container the view renders
  into. The ids and names are what `main.js` and `View` look up; the rest is layout.
- `wwwroot/style.css` — the only stylesheet: a readable single column, the two tool panels in a grid,
  breadcrumbs with the current folder in bold, and the listing table with padded cells and the size
  column right-aligned in tabular figures. Selectors use the elements and the `.error` class the view
  already produces, so styling never touches JavaScript.
- `wwwroot/js/main.js` — the entry point, wiring only: builds the `ApiClient` over the window's `fetch`,
  the `Router` over `window`, the `View` over the container and the two forms, and the `App` over all
  three, then awaits `app.start()`. Untested, per decision 14.
- `wwwroot/js/app.js` — the `App` class, the root of the SPA: what the URL means and what the user's
  actions do. Constructed with an `ApiClient`, a `Router` and a `View`, which it gives itself via
  `setApp` (decision 19). `start()` subscribes to the router and returns the navigation to the state
  the URL names now.
  `navigate(urlState)` searches `pwd` and below when `query.q` is set, otherwise lists `pwd` (the
  server accepts the leading slash), and passes the result to the view's `showSearchResults` or
  `showListing`. Whatever the load throws is passed to the view's `showError`, so `navigate` never
  rejects (decision 20). `search(text)` trims the text and moves the router to the same directory with
  it as `q`, or with no query when blank. `upload(file)` posts the file into the current directory and
  navigates to the current state again so it appears, or reports a failure via `showUploadError` and
  leaves the page alone (decision 21). It touches neither the location nor the DOM and stores nothing:
  the hash is the source of truth for state. Tested with fakes for all three dependencies.
- `wwwroot/js/router.js` — `Router`, the only code that reads or writes `location`, over a `window`
  passed to its constructor: `current()` parses the hash into a `UrlState`, `go(changes)` assigns the
  merged hash (a history entry, and a `hashchange`), and `listen(callback)` reports the new state on
  every `hashchange`. Decides nothing. Tested with a fake window in `TestProject.Tests/js/router.test.js`.
- `wwwroot/js/view.js` — `View`, the DOM side: constructed with a container element, the search form
  and the upload form, whose submits it handles by calling `app.search` with the box's text and
  `app.upload` with the chosen file, once `setApp` has given it an app (before that a submit goes
  nowhere).
  `showListing(pwd, entries)` and `showSearchResults(pwd, query, entries)` fill the search box
  (empty for a listing, the query for results, so a deep link arrives filled in), point the clear link
  at `directoryHref(pwd)` and show it only during a search, then replace the container's contents with a
  `<nav>` of `breadcrumbs` (the base, then each segment, separated by `›`; ancestors are links, the
  current directory plain text) and a
  `<table>`: a `<thead>` with Name and Size, a `<tbody>` with one row per `listingRows` or `searchRows`
  row (a link with its `href` and `name`, and a cell with its `size`), and a `<tfoot>` row from
  `listingSummary`, all via `createElement` and `textContent` (never `innerHTML`). `showError(pwd,
  error)` keeps the breadcrumbs and shows `errorMessage` in a `<p class="error">` instead of the table.
  `showUploadError(pwd, fileName, error)` writes `uploadErrorMessage` into the upload form's output;
  every show method resets the upload form. Navigation needs no click handlers: a
  directory link changes the hash and `main.js` reacts. The view decides nothing itself, so it stays
  thin and untested on Node; anything that needs a decision (formatting, counts) belongs in a pure,
  tested module. The entry-point module owns
  `location.hash` and calls it on each change. Tests in `TestProject.Tests/js/app.test.js` use a fake
  client.
- `wwwroot/js/api-client.js` — `ApiClient`, the browser's view of the JSON API, with `fetch` injected
  through the constructor so tests pass a fake. `listDirectory(path)` calls `GET /api/dir` and
  `search(path, query)` calls `GET /api/search`; both resolve to the `Entry[]`, and a non-2xx status
  rejects with an `ApiError` carrying the HTTP status (shared by a private `_getJson`).
  `upload(path, file)` POSTs the file as the multipart `file` part to `/api/file` and resolves to the
  new `Entry`. The module
  also exports `fileUrl(path)`, the `GET /api/file` link for a download (followed by the browser, not
  fetched). The JSDoc typedef for `Entry` (the client's name for the server's `FileSystemEntry`) lives
  here too.
- `wwwroot/js/view-helpers.js` — pure functions the view calls, tested in
  `TestProject.Tests/js/view-helpers.test.js`. `directoryHref(path)` is the hash link that lists a
  directory with no search. `errorMessage(pwd, error)` is the wording for a failed load, keyed on the
  `ApiError` status with a generic fallback (decision 20). `uploadErrorMessage(pwd, fileName, error)`
  is the same for a failed upload (409 name taken, 404 no directory, 400 bad name). `breadcrumbs(pwd)` is the breadcrumb bar: the base, then one
  `{ name, href }` per segment linking to that directory, except the last (the current directory),
  which has no `href`. `listingRows(pwd, entries)` is the row model:
  `.` and `..`
  (via `parentPath`; the base is its own parent) then one `{ name, href, size }` per entry, built from
  the per-entry helpers (decision 16). `searchRows(pwd, entries)` is the row model for search results:
  no `.` or `..`, and each match named by its path below the searched directory, since matches sit at
  any depth. `listingSummary(entries)` is the footer: `counts` ("2 folders,
  1 file") and `totalSize` (the file sizes summed, via `formatSize`), both computed on the client
  (decision 17).
  `entryHref(entry)` is the link for an entry: a directory
  links to its own listing in the hash (via `serializeState`, dropping any search), a file to `fileUrl`.
  `entryName(entry)` is the last path segment, with a trailing `/` for a directory. `entrySize(entry)`
  is a file's size via `formatSize` and `--` for a directory. `formatSize(bytes)` scales to the largest
  binary unit (B, K, M, G, T; 1 K = 1,024 B) that keeps the number under 1,024, with two decimals and
  thousands separators; whole bytes have no decimals.
- `wwwroot/js/url-state.js` — the `UrlState` `{ pwd, query }` and its URL-fragment form
  `#/path/to/dir?key=value` (decision 15): `parseState(hash)`, `serializeState(state)` and
  `mergeState(hash, changes)` (parse, shallow-overwrite, serialize; a `query` in `changes` replaces the
  whole query), pure string logic with no DOM access. `pwd` always starts with `/`; `query` is a generic object of the pairs after
  the `?`. Tested in both directions in `TestProject.Tests/js/url-state.test.js`.
- `Models/FileSystemEntry.cs` — the `FileSystemEntry(Path, IsDirectory, Size)` record, the shape every API
  response is made of, whose static `From` builds one from the base directory and a `FileSystemInfo` (the
  only place a `FileSystemInfo` is read). Paths are base-relative and use `/` on every platform (decision 22); `Size` is bytes for a file, `null` for a
  directory. There is no wrapper model: listings are plain arrays, and counts and totals are the client's
  job (decision 17).
- `Services/FileSystemService.cs` — the operations on the configured base directory (`ListDirectory`,
  `Search`, `GetFileInfo`, `WriteFileAsync`), taking paths relative to the base. `ListDirectory` and
  `Search` return an `IReadOnlyList<FileSystemEntry>` ordered directories first, then files, each group in
  ordinal path order; `Search(path, query)` walks the directory `path` recursively and matches the entry
  name only, returning base-relative paths.
  `WriteFileAsync` creates a new file (never overwrites) and returns its `FileSystemEntry`. Constructed in
  `Program.cs` from `FileBrowser:BaseDirectory` and registered as a singleton.
- `Services/EntryExistsException.cs` — thrown by the service when a create would clash with an existing
  file or directory; the controller maps it to 409.
- `Services/PathResolver.cs` — maps client-supplied relative paths to absolute paths inside the base;
  normalises the path and rejects anything that escapes the base. Pure string logic, no filesystem access.
- `TestProject.Tests/` — xUnit test project, excluded from the web project's default source globs via
  `DefaultItemExcludes` in `TestProject.csproj`. Its `js/` folder holds the Node tests for `wwwroot/js`,
  kept here rather than under `wwwroot` so they are never served as static files.
- `appsettings.json` — the single config file. `FileBrowser:BaseDirectory` is the browsable root (relative
  values resolve against the content root), `public` by default; override with the
  `FileBrowser__BaseDirectory` environment variable.
- `public/` — the default base directory, seeded with sample files to browse, search and download: a
  README, notes, a CSV, an SVG and a PNG, and the original requirements PDF. Uploads land here too, so
  `.gitignore` ignores everything under it except the seed files (each subdirectory has to be
  un-ignored before a file inside it can be).
- `Properties/launchSettings.json` — one `TestProject` profile, HTTP only, opening the browser at `/`.
- `README.md` — the reviewer-facing guide: notes to the reviewer (how the code was produced and why), how to run and test, the `public/` sample directory and how
  to point the app elsewhere, how to use the UI, the API table, and where to look in the code. Keep it
  in step with behaviour changes.
- `DECISIONS.md` — numbered log of architecture decisions with rationale. **Append an entry whenever a
  non-obvious design choice is made** (scope cuts, API shape, data model, performance trade-offs). Reviewers
  read it alongside the Git history.

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
