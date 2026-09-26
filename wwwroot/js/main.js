/**
 * Entry point, loaded by `index.html`. The only module that touches the browser's
 * location: it turns the hash into a state and hands it to the App, once on load
 * and again whenever the hash changes (links, back and forward all arrive this way).
 */
import { ApiClient } from "./api-client.js";
import { App } from "./app.js";
import { parseState } from "./state.js";

// fetch must be called on the window; passed bare, the client would call it on itself.
const app = new App(new ApiClient(fetch.bind(globalThis)));

const navigateToHash = () => app.navigate(parseState(location.hash));
addEventListener("hashchange", navigateToHash);
navigateToHash();
