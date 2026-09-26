import { fileUrl } from "./api-client.js";
import { serializeState } from "./url-state.js";

/**
 * @typedef {object} Row One row of the listing table, ready to copy onto elements.
 * @property {string} name The link text.
 * @property {string} href The link target.
 * @property {string} size The size column.
 */

/**
 * The rows for a directory listing, `ls -a` style: `.` (the directory itself) and
 * `..` (its parent; the base's parent is the base) first, then one row per entry.
 * @param {string} pwd The directory being listed, as `UrlState.pwd`.
 * @param {import("./api-client.js").Listing} listing
 * @returns {Row[]}
 */
export function listingRows(pwd, listing) {
    return [
        { name: ".", href: directoryHref(pwd), size: "--" },
        { name: "..", href: directoryHref(parentPath(pwd)), size: "--" },
        ...listing.entries.map(entry => ({ name: entryName(entry), href: entryHref(entry), size: entrySize(entry) })),
    ];
}

/**
 * The footer of the listing table: how many folders and files it holds, and their
 * total size. Counts are taken from the entries; the total is the server's.
 * @param {import("./api-client.js").Listing} listing
 * @returns {{ counts: string, totalSize: string }}
 */
export function listingSummary(listing) {
    const folders = listing.entries.filter(entry => entry.isDirectory).length;
    const files = listing.entries.length - folders;
    return {
        counts: `${plural(folders, "folder")}, ${plural(files, "file")}`,
        totalSize: formatSize(listing.totalSize),
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

/** The hash link that lists `path`, with no search. @param {string} path */
function directoryHref(path) {
    return serializeState({ pwd: path, query: {} });
}
