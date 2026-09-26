/**
 * Entry point, loaded by `index.html`. The only module that touches the browser's
 * location: it turns the current hash into a state and hands it to the App.
 */
import { ApiClient } from "./api-client.js";
import { App } from "./app.js";
import { parseState } from "./state.js";

// fetch must be called on the window; passed bare, the client would call it on itself.
const app = new App(new ApiClient(fetch.bind(globalThis)));
app.navigate(parseState(location.hash));
