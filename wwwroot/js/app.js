/**
 * The file browser application. It reacts to navigation by loading the listing
 * for the directory the URL names and handing it to the view. The entry module
 * owns the browser's location and translates each change into a `navigate` call;
 * the view owns the DOM. App itself touches neither, so it is tested with fakes.
 */
export class App {
    /**
     * @param {import("./api-client.js").ApiClient} api
     * @param {{ showListing(pwd: string, listing: import("./api-client.js").Listing): void }} view
     */
    constructor(api, view) {
        this._api = api;
        this._view = view;
    }

    /**
     * Responds to a navigation by loading the listing for its directory and showing
     * it. The server accepts `pwd` as is, since a leading slash means the base
     * directory to it too.
     * @param {import("./url-state.js").UrlState} urlState The parsed URL state to move to.
     */
    async navigate(urlState) {
        const listing = await this._api.listDirectory(urlState.pwd);
        this._view.showListing(urlState.pwd, listing);
    }
}
