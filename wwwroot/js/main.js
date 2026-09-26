/**
 * Entry point, loaded by `index.html`: builds the pieces and starts the app.
 */
import { ApiClient } from "./api-client.js";
import { App } from "./app.js";
import { Router } from "./router.js";
import { View } from "./view.js";

// fetch must be called on the window; passed bare, the client would call it on itself.
const api = new ApiClient(fetch.bind(globalThis));
const view = new View(document.getElementById("listing"), document.getElementById("search"));
const app = new App(api, new Router(window), view);
await app.start();
