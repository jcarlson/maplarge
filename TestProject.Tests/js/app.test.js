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

/** A stand-in for View that records what it was asked to show. */
function fakeView() {
    const shown = [];
    return {
        shown,
        showListing(pwd, listing) {
            shown.push({ pwd, listing });
        },
    };
}

test("navigate lists the directory the URL state points at", async () => {
    const api = fakeApi();
    const app = new App(api, fakeView());

    await app.navigate({ pwd: "/photos", query: {} });

    assert.deepEqual(api.calls, ["/photos"]);
});

test("navigate shows the listing it loaded, with the directory it is for", async () => {
    const view = fakeView();
    const app = new App(fakeApi(), view);

    await app.navigate({ pwd: "/photos", query: {} });

    assert.deepEqual(view.shown, [{ pwd: "/photos", listing }]);
});

test("each navigation lists and shows its own directory", async () => {
    const api = fakeApi();
    const view = fakeView();
    const app = new App(api, view);

    await app.navigate({ pwd: "/photos", query: {} });
    await app.navigate({ pwd: "/", query: {} });

    assert.deepEqual(api.calls, ["/photos", "/"]);
    assert.equal(view.shown.length, 2);
});
