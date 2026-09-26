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

test("a new App has no listing until it navigates", () => {
    assert.equal(new App(fakeApi()).listing, undefined);
});

test("navigate lists the directory the URL state points at", async () => {
    const api = fakeApi();
    const app = new App(api);

    await app.navigate({ pwd: "/photos", query: {} });

    assert.deepEqual(api.calls, ["/photos"]);
    assert.deepEqual(app.listing, listing);
});

test("each navigation lists its own directory", async () => {
    const api = fakeApi();
    const app = new App(api);

    await app.navigate({ pwd: "/photos", query: {} });
    await app.navigate({ pwd: "/", query: {} });

    assert.deepEqual(api.calls, ["/photos", "/"]);
});
