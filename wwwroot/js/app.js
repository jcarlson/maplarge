/**
 * The file browser application. It holds the current UI state and reacts to
 * navigation; the entry module owns the browser's location and translates each
 * change into a `navigate` call.
 */
export class App {
    /** @type {import("./state.js").State | undefined} */
    state;

    /**
     * Responds to a navigation: `state` becomes the current state.
     * @param {import("./state.js").State} state The parsed URL state to move to.
     */
    navigate(state) {
        this.state = state;
    }
}
