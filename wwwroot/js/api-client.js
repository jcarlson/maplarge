/**
 * @typedef {object} Entry One file or directory, as the server's `FileSystemEntry` serialises.
 * @property {string} path Path relative to the browsable base directory.
 * @property {boolean} isDirectory
 * @property {number | null} size Bytes for a file, `null` for a directory.
 */

/** Thrown when the server answers with a non-2xx status. */
export class ApiError extends Error {
    /**
     * @param {number} status The HTTP status code.
     * @param {string} url The URL that was requested.
     */
    constructor(status, url) {
        super(`${status} from ${url}`);
        this.name = "ApiError";
        this.status = status;
    }
}

/**
 * The URL that serves the file at `path`, for use as a link's `href`: the browser
 * follows it as a download, so no `fetch` is involved.
 * @param {string} path File path relative to the base.
 */
export function fileUrl(path) {
    return "/api/file?" + new URLSearchParams({ path });
}

/**
 * Talks to the server's JSON API. It knows the routes and their shapes and nothing
 * about the DOM; `fetch` is passed in so tests can supply a fake.
 */
export class ApiClient {
    /** @param {typeof fetch} fetch */
    constructor(fetch) {
        this._fetch = fetch;
    }

    /**
     * Lists the entries directly inside `path`, directories first, then files, each in
     * ordinal path order. An empty path lists the base itself.
     * @param {string} path Directory path relative to the base.
     * @returns {Promise<Entry[]>}
     * @throws {ApiError} 400 if the path escapes the base, 404 if the directory does not exist.
     */
    async listDirectory(path) {
        const url = "/api/dir?" + new URLSearchParams({ path });
        const response = await this._fetch(url);
        if (!response.ok) {
            throw new ApiError(response.status, url);
        }
        return response.json();
    }
}
