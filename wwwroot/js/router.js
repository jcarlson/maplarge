import { mergeState, parseState } from "./url-state.js";

/**
 * The browser's location seen as a `UrlState`. This is the only code that reads or
 * writes `location`; it translates between the hash and the state object and
 * decides nothing, so what the URL means stays in `App`. It takes the window as a
 * dependency so tests can pass a fake with a `location` and `addEventListener`.
 */
export class Router {
    #window;

    /** @param {Window} window */
    constructor(window) {
        this.#window = window;
    }

    /**
     * The state the URL currently names.
     * @returns {import("./url-state.js").UrlState}
     */
    current() {
        return parseState(this.#window.location.hash);
    }

    /**
     * Moves to the current state with `changes` applied, which adds a history entry
     * and, in a browser, fires `hashchange`.
     * @param {Partial<import("./url-state.js").UrlState>} changes
     */
    go(changes) {
        this.#window.location.hash = mergeState(this.#window.location.hash, changes);
    }

    /**
     * Calls `callback` with the new state whenever the URL changes, whether by a
     * link, `go`, or the back and forward buttons.
     * @param {(state: import("./url-state.js").UrlState) => void} callback
     */
    listen(callback) {
        this.#window.addEventListener("hashchange", () => callback(this.current()));
    }
}
