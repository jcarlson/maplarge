import { test } from "node:test";
import assert from "node:assert/strict";
import { App } from "../../wwwroot/js/app.js";

const listing = {
    entries: [{ path: "photos/cat.jpg", isDirectory: false, size: 12 }],
    totalSize: 12,
};

/** A stand-in for ApiClient that records the paths it was asked to list. */
function fakeApi() {
    const calls = [];
    return {
        calls,
        async listDirectory(path) {
            calls.push(path);
            return listing;
        },
    };
}

test("a new App has no state or listing until it navigates", () => {
    const app = new App(fakeApi());

    assert.equal(app.state, undefined);
    assert.equal(app.listing, undefined);
});

test("navigate makes the given state current", async () => {
    const app = new App(fakeApi());
    const state = { pwd: "/photos", query: { q: "summer" } };

    await app.navigate(state);

    assert.deepEqual(app.state, state);
});

test("navigate lists the directory the state points at", async () => {
    const api = fakeApi();
    const app = new App(api);

    await app.navigate({ pwd: "/photos", query: {} });

    assert.deepEqual(api.calls, ["/photos"]);
    assert.deepEqual(app.listing, listing);
});

test("navigate replaces the previous state and lists again", async () => {
    const api = fakeApi();
    const app = new App(api);
    await app.navigate({ pwd: "/photos", query: {} });

    await app.navigate({ pwd: "/", query: {} });

    assert.deepEqual(app.state, { pwd: "/", query: {} });
    assert.deepEqual(api.calls, ["/photos", "/"]);
});
