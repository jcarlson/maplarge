/**
 * Entry point, loaded by `index.html`. The only module that touches the browser's
 * location: it turns the current hash into a state and hands it to the App.
 */
import { App } from "./app.js";
import { parseState } from "./state.js";

const app = new App();
app.navigate(parseState(location.hash));
