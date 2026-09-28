# File Browser

A file and directory browser for a configurable server-side folder: an ASP.NET Core 8 JSON API and a
single-page app written in plain JavaScript, with no UI framework and no build step. You can browse,
search, download and upload, and every view is a deep link.

## Notes to the reviewer

A few things worth knowing before you read the code.

**I wrote none of this code by hand, by design.** Every line of C#, JavaScript, CSS and prose in this
repository was produced with Claude Code, and every commit says so in its trailer. I chose to work this
way because I think that is where software engineering is going, and because I read this exercise as a
test of how I think about and structure an application rather than how well I know a particular
syntax. What I did by hand was the engineering: deciding what to build in each step, what shape the
API and the client should take, when an abstraction was warranted and when it was not, what to test and
how, and what to reject. `CLAUDE.md` holds the working rules I set (one deliberate step at a time, tests
in every commit, a decision log, no feature built ahead of being asked), `DECISIONS.md` records each
design choice and its reasoning as it was made, and the Git history shows the order the work happened
in. I am happy to walk through any class, any decision, or any commit and explain why it is the way it
is.

## Running it

You need the .NET 8 SDK or newer. Nothing else is required to build or run.

```bash
dotnet run
```

Then open <http://localhost:5120/>. The launch profile opens a browser for you when run from an IDE.

The app browses the `public/` directory in the repository by default. It is seeded with a few sample
files (a README, some notes, a CSV, two images and the original requirements PDF) so there is
something to look at, search for and download. Files you upload land there as well; Git ignores
everything in `public/` except the seed files.

To browse a different folder, change `FileBrowser:BaseDirectory` in `appsettings.json` or set the
`FileBrowser__BaseDirectory` environment variable. Relative paths resolve against the project directory.

```bash
FileBrowser__BaseDirectory=/some/other/folder dotnet run
```

## Using it

- Click a folder to open it, `..` to go up, or any crumb in the breadcrumb bar. `.` reloads the folder.
- Click a file to download it.
- The footer shows how many folders and files the view holds and the total size of the files.
- Type in the search box and press Enter or Search to find files and folders whose name contains the
  text, anywhere below the current folder. Results show their path relative to that folder. Clear
  returns to it.
- Choose a file and click Upload to put it in the current folder. Existing files are never overwritten;
  a clash is reported next to the form.

The URL fragment holds the whole UI state, so any view can be bookmarked or shared:

```
#/notes                 the notes folder
#/notes?q=meeting       a search for "meeting" within notes
```

## Running the tests

The C# tests use xUnit:

```bash
dotnet test
```

The JavaScript tests use Node's built-in test runner and need Node 22 or newer. Node is used for the
tests only, never to build or run the app.

```bash
node --test
```

## The API

All paths are relative to the base directory and use `/` as the separator on every platform. Responses are JSON arrays of entries, each
`{ path, isDirectory, size }` with `size` in bytes for a file and `null` for a directory, directories
first and then files, each group in ordinal order.

| Request | Meaning |
|---|---|
| `GET /api/dir?path=notes` | The entries directly inside a directory. 404 if it does not exist. |
| `GET /api/search?path=notes&q=meeting` | Entries anywhere under `path` whose name contains `q`, ignoring case. |
| `GET /api/file?path=notes/todo.txt` | Downloads a file. Range requests are supported. |
| `POST /api/file?path=notes` | Uploads the multipart `file` part into a directory. 201 with the new entry, 409 if the name is taken. |

A path that would escape the base directory is a 400 in every case.

## Where to look

- `Controllers/`, `Services/` and `Models/` are the server. `PathResolver` is the piece that keeps every
  request inside the base directory; `FileSystemService` does the file system work; the controllers only
  translate HTTP.
- `wwwroot/js/` is the client. `app.js` decides what the URL means and what the user's actions do;
  `router.js` is the only code that touches the location and `view.js` the only code that touches the
  DOM; `url-state.js`, `view-helpers.js` and `api-client.js` are pure and tested.
- `TestProject.Tests/` holds the xUnit tests and, under `js/`, the Node tests.
- `DECISIONS.md` is a numbered log of the design decisions and the reasoning behind each, written as
  the work progressed. The Git history follows the same order.
