import { fileUrl } from "./api-client.js";
import { serializeState } from "./url-state.js";

/**
 * The `href` for an entry in a listing. A directory links to its own listing, so
 * clicking it is a hash change the entry module already handles; a file links to
 * the download endpoint. Any search in the current URL is dropped when entering a
 * directory.
 * @param {import("./api-client.js").Entry} entry
 * @returns {string}
 */
export function entryHref(entry) {
    return entry.isDirectory
        ? serializeState({ pwd: entry.path, query: {} })
        : fileUrl(entry.path);
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
 * The size column for an entry: the byte count of a file, `--` for a directory,
 * which has no size of its own.
 * @param {import("./api-client.js").Entry} entry
 * @returns {string}
 */
export function entrySize(entry) {
    return entry.isDirectory ? "--" : String(entry.size);
}
