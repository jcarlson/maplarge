/**
 * The file browser application. It reacts to navigation by loading what the URL
 * names, a directory's listing or a search within it, and handing it to the view.
 * The entry module owns the browser's location and translates each change into a
 * `navigate` call; the view owns the DOM. App itself touches neither, so it is
 * tested with fakes.
 */
export class App {
    /**
     * @param {import("./api-client.js").ApiClient} api
     * @param {View} view
     */
    constructor(api, view) {
        this._api = api;
        this._view = view;
    }

    /**
     * Responds to a navigation: with a `q` in the query it searches the directory
     * and below, otherwise it lists the directory, and shows the result. The server
     * accepts `pwd` as is, since a leading slash means the base directory to it too.
     * @param {import("./url-state.js").UrlState} urlState The parsed URL state to move to.
     */
    async navigate({ pwd, query }) {
        if (query.q) {
            const entries = await this._api.search(pwd, query.q);
            this._view.showSearchResults(pwd, query.q, entries);
        } else {
            const entries = await this._api.listDirectory(pwd);
            this._view.showListing(pwd, entries);
        }
    }
}

/**
 * @typedef {object} View What App needs from the DOM side.
 * @property {(pwd: string, entries: import("./api-client.js").Entry[]) => void} showListing
 * @property {(pwd: string, query: string, entries: import("./api-client.js").Entry[]) => void} showSearchResults
 */
