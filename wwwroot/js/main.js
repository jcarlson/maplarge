/**
 * Entry point, loaded by `index.html`. The only module that touches the browser's
 * location: it turns the hash into a state and hands it to the App, once on load
 * and again whenever the hash changes (links, back and forward all arrive this way),
 * and it turns a submitted search into a hash change so it flows the same way.
 */
import { ApiClient } from "./api-client.js";
import { App } from "./app.js";
import { View } from "./view.js";
import { mergeState, parseState } from "./url-state.js";

// fetch must be called on the window; passed bare, the client would call it on itself.
const api = new ApiClient(fetch.bind(globalThis));
const searchForm = document.getElementById("search");
const view = new View(document.getElementById("listing"), searchForm);
const app = new App(api, view);

const navigateToHash = () => app.navigate(parseState(location.hash));
addEventListener("hashchange", navigateToHash);
navigateToHash();

// A search is a state change: keep the directory, set (or with an empty box, drop) the query.
searchForm.addEventListener("submit", event => {
    event.preventDefault();
    const q = searchForm.elements.q.value.trim();
    location.hash = mergeState(location.hash, { query: q ? { q } : {} });
});
