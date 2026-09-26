/**
 * The file browser application. It holds the current UI state and the listing
 * for it, and reacts to navigation; the entry module owns the browser's location
 * and translates each change into a `navigate` call.
 */
export class App {
    /** @type {import("./state.js").State | undefined} */
    state;

    /** @type {import("./api-client.js").Listing | undefined} */
    listing;

    /** @param {import("./api-client.js").ApiClient} api */
    constructor(api) {
        this._api = api;
    }

    /**
     * Responds to a navigation: `state` becomes the current state and the listing
     * for its directory is loaded. The server accepts `pwd` as is, since a leading
     * slash means the base directory to it too.
     * @param {import("./state.js").State} state The parsed URL state to move to.
     */
    async navigate(state) {
        this.state = state;
        this.listing = await this._api.listDirectory(state.pwd);
    }
}
