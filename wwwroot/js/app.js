/**
 * The file browser application. It reacts to navigation by loading the listing
 * for the directory the URL names; the entry module owns the browser's location
 * and translates each change into a `navigate` call.
 */
export class App {
    /** @type {import("./api-client.js").Listing | undefined} */
    listing;

    /** @param {import("./api-client.js").ApiClient} api */
    constructor(api) {
        this._api = api;
    }

    /**
     * Responds to a navigation by loading the listing for its directory. The server
     * accepts `pwd` as is, since a leading slash means the base directory to it too.
     * @param {import("./url-state.js").UrlState} urlState The parsed URL state to move to.
     */
    async navigate(urlState) {
        this.listing = await this._api.listDirectory(urlState.pwd);
    }
}
