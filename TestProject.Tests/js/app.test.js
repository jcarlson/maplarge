import { test } from "node:test";
import assert from "node:assert/strict";
import { App } from "../../wwwroot/js/app.js";

test("a new App has no state until it navigates", () => {
    assert.equal(new App().state, undefined);
});

test("navigate makes the given state current", () => {
    const app = new App();
    const state = { pwd: "/photos", query: { q: "summer" } };

    app.navigate(state);

    assert.deepEqual(app.state, state);
});

test("navigate replaces the previous state", () => {
    const app = new App();
    app.navigate({ pwd: "/photos", query: {} });

    app.navigate({ pwd: "/", query: {} });

    assert.deepEqual(app.state, { pwd: "/", query: {} });
});
