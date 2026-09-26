import { ApiError, fileUrl } from "./api-client.js";
import { serializeState } from "./url-state.js";

/**
 * @typedef {object} Row One row of the listing table, ready to copy onto elements.
 * @property {string} name The link text.
 * @property {string} href The link target.
 * @property {string} size The size column.
 */

/**
 * What to tell the user when loading `pwd` failed. The server's status codes each
 * have a meaning here (decision 20); anything else, such as a request that never
 * reached the server, gets its own message.
 * @param {string} pwd The directory that was being loaded.
 * @param {unknown} error What the load threw.
 * @returns {string}
 */
export function errorMessage(pwd, error) {
    if (error instanceof ApiError) {
        switch (error.status) {
            case 404: return `There is no directory at ${pwd}.`;
            case 400: return `${pwd} is not a path this browser can show.`;
            default: return `The server could not answer (status ${error.status}).`;
        }
    }
    return `Something went wrong: ${error instanceof Error ? error.message : String(error)}`;
}

/**
 * @typedef {object} Crumb One item in the breadcrumb bar.
 * @property {string} name The text: `/` for the base, otherwise the segment.
 * @property {string} [href] That directory's listing; absent for the current directory, which is not a link.
 */

/**
 * The breadcrumb bar for a `pwd`: the base first, then one crumb per segment. Every
 * ancestor links to its listing; the last crumb is the current directory and has no
 * link, since the page already shows it.
 * @param {string} pwd The current directory, as `UrlState.pwd`.
 * @returns {Crumb[]}
 */
export function breadcrumbs(pwd) {
    const crumbs = [{ name: "/", href: directoryHref("/") }];
    let path = "";
    for (const segment of pwd.split("/").filter(Boolean)) {
        path += "/" + segment;
        crumbs.push({ name: segment, href: directoryHref(path) });
    }
    delete crumbs[crumbs.length - 1].href;
    return crumbs;
}

/**
 * The rows for a directory listing, `ls -a` style: `.` (the directory itself) and
 * `..` (its parent; the base's parent is the base) first, then one row per entry.
 * @param {string} pwd The directory being listed, as `UrlState.pwd`.
 * @param {import("./api-client.js").Entry[]} entries
 * @returns {Row[]}
 */
export function listingRows(pwd, entries) {
    return [
        { name: ".", href: directoryHref(pwd), size: "--" },
        { name: "..", href: directoryHref(parentPath(pwd)), size: "--" },
        ...entries.map(entry => ({ name: entryName(entry), href: entryHref(entry), size: entrySize(entry) })),
    ];
}

/**
 * The rows for search results: one per entry, named by its path relative to the
 * directory that was searched, since a match can sit at any depth below it. No
 * `.` or `..`, which have no meaning in a result set.
 * @param {string} pwd The directory that was searched, as `UrlState.pwd`.
 * @param {import("./api-client.js").Entry[]} entries Matches, with base-relative paths.
 * @returns {Row[]}
 */
export function searchRows(pwd, entries) {
    const prefix = pwd === "/" ? "" : pwd.slice(1) + "/";
    return entries.map(entry => ({
        name: entry.path.slice(prefix.length) + (entry.isDirectory ? "/" : ""),
        href: entryHref(entry),
        size: entrySize(entry),
    }));
}

/**
 * The footer of the listing table: how many folders and files it holds, and the
 * total size of the files (directories have none and contribute nothing).
 * @param {import("./api-client.js").Entry[]} entries
 * @returns {{ counts: string, totalSize: string }}
 */
export function listingSummary(entries) {
    const folders = entries.filter(entry => entry.isDirectory).length;
    const files = entries.length - folders;
    const bytes = entries.reduce((sum, entry) => sum + (entry.size ?? 0), 0);
    return {
        counts: `${plural(folders, "folder")}, ${plural(files, "file")}`,
        totalSize: formatSize(bytes),
    };
}

/** `1 file`, `2 files`. @param {number} count @param {string} noun */
function plural(count, noun) {
    return `${count} ${noun}${count === 1 ? "" : "s"}`;
}

/**
 * The parent of a `pwd`; the base directory is its own parent.
 * @param {string} pwd
 * @returns {string}
 */
export function parentPath(pwd) {
    const parent = pwd.slice(0, pwd.lastIndexOf("/"));
    return parent || "/";
}

/**
 * The `href` for an entry in a listing. A directory links to its own listing, so
 * clicking it is a hash change the entry module already handles; a file links to
 * the download endpoint. Any search in the current URL is dropped when entering a
 * directory.
 * @param {import("./api-client.js").Entry} entry
 * @returns {string}
 */
export function entryHref(entry) {
    return entry.isDirectory ? directoryHref(entry.path) : fileUrl(entry.path);
}

/**
 * The text shown for an entry: its own name, not the path from the base, with a
 * trailing slash marking a directory.
 * @param {import("./api-client.js").Entry} entry
 * @returns {string}
 */
export function entryName(entry) {
    const name = entry.path.slice(entry.path.lastIndexOf("/") + 1);
    return entry.isDirectory ? name + "/" : name;
}

/**
 * The size column for an entry: a file's size via `formatSize`, `--` for a
 * directory, which has no size of its own.
 * @param {import("./api-client.js").Entry} entry
 * @returns {string}
 */
export function entrySize(entry) {
    return entry.isDirectory ? "--" : formatSize(entry.size ?? 0);
}

const UNITS = ["B", "K", "M", "G", "T"];

/**
 * A byte count for people: scaled to the largest binary unit (1 K = 1,024 B) that
 * keeps the number under 1,024, with two decimals and thousands separators.
 * Whole bytes are shown without decimals.
 * @param {number} bytes
 * @returns {string}
 */
export function formatSize(bytes) {
    let value = bytes;
    let unit = 0;
    while (value >= 1024 && unit < UNITS.length - 1) {
        value /= 1024;
        unit++;
    }
    const digits = unit === 0 ? 0 : 2;
    const number = value.toLocaleString("en-US", { minimumFractionDigits: digits, maximumFractionDigits: digits });
    return `${number} ${UNITS[unit]}`;
}

/**
 * The hash link that lists `path` with no search: an entry's directory, or the
 * current one when a search is cleared.
 * @param {string} path
 * @returns {string}
 */
export function directoryHref(path) {
    return serializeState({ pwd: path, query: {} });
}
