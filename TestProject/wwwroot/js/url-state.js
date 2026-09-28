/**
 * Converts between the URL fragment and the UI state it encodes, in both directions.
 * The fragment looks like `#/photos/2024?q=summer`: the path of the directory being
 * viewed, then optional `key=value` pairs. Pure string logic; reading and writing
 * `location.hash` is the caller's job.
 */

/**
 * @typedef {object} UrlState
 * @property {string} pwd Directory being viewed, always with a leading `/`; `/` is the base.
 * @property {Record<string, string>} query Every `key=value` pair after the `?`; `{}` when there are none.
 */

/**
 * Reads a `UrlState` out of a fragment such as `location.hash`. Tolerant of what a
 * hand-edited URL can contain: a missing `#` or leading `/`, repeated or trailing
 * slashes, and percent-escapes that do not decode (kept as typed).
 * @param {string} hash
 * @returns {UrlState}
 */
export function parseState(hash) {
    const fragment = hash.startsWith("#") ? hash.slice(1) : hash;
    const queryStart = fragment.indexOf("?");
    const pathPart = queryStart < 0 ? fragment : fragment.slice(0, queryStart);
    const queryPart = queryStart < 0 ? "" : fragment.slice(queryStart + 1);
    return {
        pwd: "/" + segments(pathPart).map(decodeSegment).join("/"),
        query: Object.fromEntries(new URLSearchParams(queryPart)),
    };
}

/**
 * Writes a `UrlState` as a fragment, ready to assign to `location.hash`. The base
 * directory with no query is `#/`; the `?` is left out when `query` is empty.
 * @param {UrlState} state
 * @returns {string}
 */
export function serializeState({ pwd, query }) {
    const pathPart = segments(pwd).map(encodeURIComponent).join("/");
    const queryPart = new URLSearchParams(query).toString();
    return "#/" + pathPart + (queryPart ? "?" + queryPart : "");
}

/**
 * The fragment for the current state with `changes` applied: parse `hash`, overwrite
 * the given fields, serialize. The merge is shallow, so a `query` in `changes`
 * replaces the whole query rather than adding to it.
 * @param {string} hash The current fragment, such as `location.hash`.
 * @param {Partial<UrlState>} [changes] The fields to overwrite.
 * @returns {string}
 */
export function mergeState(hash, changes = {}) {
    return serializeState({ ...parseState(hash), ...changes });
}

/**
 * The non-empty segments of a path, so leading, trailing and doubled slashes vanish.
 * @param {string} path
 */
function segments(path) {
    return path.split("/").filter(Boolean);
}

/** @param {string} segment */
function decodeSegment(segment) {
    try {
        return decodeURIComponent(segment);
    } catch {
        return segment;
    }
}
