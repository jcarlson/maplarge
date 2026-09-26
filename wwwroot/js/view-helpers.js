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
