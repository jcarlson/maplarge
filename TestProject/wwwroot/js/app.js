/**
 * The file browser application: what the URL means and what happens when the user
 * acts. It reacts to navigation by loading what the URL names, a directory's
 * listing or a search within it, and handing it to the view; a search typed by the
 * user becomes a new URL state, and a chosen file is uploaded into the current
 * directory. The router owns the location and the view owns the DOM; App touches
 * neither, so it is tested with fakes for all three dependencies.
 */
export class App {
    #api;
    #router;
    #view;

    /**
     * @param {import("./api-client.js").ApiClient} api
     * @param {import("./router.js").Router} router
     * @param {View} view Rendered into, and told about this app so it can report the user's actions.
     */
    constructor(api, router, view) {
        this.#api = api;
        this.#router = router;
        this.#view = view;
        view.setApp(this);
    }

    /**
     * Follows the URL: every later change navigates, and the state it names now is
     * shown before this resolves.
     */
    async start() {
        this.#router.listen(state => this.navigate(state));
        await this.navigate(this.#router.current());
    }

    /**
     * Responds to a navigation: with a `q` in the query it searches the directory
     * and below, otherwise it lists the directory, and shows the result. The server
     * accepts `pwd` as is, since a leading slash means the base directory to it too.
     * Whatever goes wrong, an `ApiError` such as a 404 or a failed request, is shown
     * in the view for that directory instead; this never rejects, so the router's
     * listener can drop the promise.
     * @param {import("./url-state.js").UrlState} urlState The parsed URL state to move to.
     */
    async navigate({ pwd, query }) {
        try {
            if (query.q) {
                const entries = await this.#api.search(pwd, query.q);
                this.#view.showSearchResults(pwd, query.q, entries);
            } else {
                const entries = await this.#api.listDirectory(pwd);
                this.#view.showListing(pwd, entries);
            }
        } catch (error) {
            this.#view.showError(pwd, error);
        }
    }

    /**
     * The user asked to search the current directory for `text`. That is a change
     * of URL state, keeping the directory and setting the query, or dropping the
     * query when the text is blank, so it navigates like a link and can be deep-linked.
     * @param {string} text As typed; surrounding whitespace is ignored.
     */
    search(text) {
        const q = text.trim();
        this.#router.go({ query: q ? { q } : {} });
    }

    /**
     * The user chose a file to upload. It goes into the directory the URL names now,
     * and the current view is loaded again so the new file appears in it. A failure,
     * such as a name already taken, is reported next to the form; the table stays.
     * @param {File} file
     */
    async upload(file) {
        const state = this.#router.current();
        try {
            await this.#api.upload(state.pwd, file);
        } catch (error) {
            this.#view.showUploadError(state.pwd, file.name, error);
            return;
        }
        await this.navigate(state);
    }
}

/**
 * @typedef {object} View What App needs from the DOM side.
 * @property {(app: App) => void} setApp Where to report the user's actions.
 * @property {(pwd: string, entries: import("./api-client.js").Entry[]) => void} showListing
 * @property {(pwd: string, query: string, entries: import("./api-client.js").Entry[]) => void} showSearchResults
 * @property {(pwd: string, error: unknown) => void} showError
 * @property {(pwd: string, fileName: string, error: unknown) => void} showUploadError
 */
