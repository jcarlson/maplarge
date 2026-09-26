/**
 * @typedef {object} Entry One file or directory, as the server's `Entry` serialises.
 * @property {string} path Path relative to the browsable base directory.
 * @property {boolean} isDirectory
 * @property {number | null} size Bytes for a file, `null` for a directory.
 */

/**
 * @typedef {object} Listing A set of entries, as the server's `Listing` serialises.
 * @property {Entry[]} entries Directories first, then files, each in ordinal path order.
 * @property {number} totalSize Sum of the file sizes in `entries`, in bytes.
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
 * Talks to the server's JSON API. It knows the routes and their shapes and nothing
 * about the DOM; `fetch` is passed in so tests can supply a fake.
 */
export class ApiClient {
    /** @param {typeof fetch} fetch */
    constructor(fetch) {
        this._fetch = fetch;
    }

    /**
     * Lists the entries directly inside `path`. An empty path lists the base itself.
     * @param {string} path Directory path relative to the base.
     * @returns {Promise<Listing>}
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
