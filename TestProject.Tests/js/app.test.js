import { test } from "node:test";
import assert from "node:assert/strict";
import { App } from "../../wwwroot/js/app.js";

const entries = [{ path: "photos/cat.jpg", isDirectory: false, size: 12 }];

/** A stand-in for ApiClient that records the calls made to it. */
function fakeApi() {
    const calls = [];
    return {
        calls,
        async listDirectory(path) {
            calls.push({ listDirectory: path });
            return entries;
        },
        async search(path, query) {
            calls.push({ search: [path, query] });
            return entries;
        },
    };
}

/** A stand-in for View that records what it was asked to show. */
function fakeView() {
    const shown = [];
    return {
        shown,
        showListing(pwd, entries) {
            shown.push({ listing: { pwd, entries } });
        },
        showSearchResults(pwd, query, entries) {
            shown.push({ search: { pwd, query, entries } });
        },
    };
}

test("navigate lists the directory the URL state points at", async () => {
    const api = fakeApi();
    const app = new App(api, fakeView());

    await app.navigate({ pwd: "/photos", query: {} });

    assert.deepEqual(api.calls, [{ listDirectory: "/photos" }]);
});

test("navigate shows the entries it loaded, with the directory they are in", async () => {
    const view = fakeView();
    const app = new App(fakeApi(), view);

    await app.navigate({ pwd: "/photos", query: {} });

    assert.deepEqual(view.shown, [{ listing: { pwd: "/photos", entries } }]);
});

test("navigate with a q searches the directory and shows the results", async () => {
    const api = fakeApi();
    const view = fakeView();
    const app = new App(api, view);

    await app.navigate({ pwd: "/photos", query: { q: "cat" } });

    assert.deepEqual(api.calls, [{ search: ["/photos", "cat"] }]);
    assert.deepEqual(view.shown, [{ search: { pwd: "/photos", query: "cat", entries } }]);
});

test("navigate with an empty q lists rather than searches", async () => {
    const api = fakeApi();
    const app = new App(api, fakeView());

    await app.navigate({ pwd: "/photos", query: { q: "" } });

    assert.deepEqual(api.calls, [{ listDirectory: "/photos" }]);
});

test("each navigation lists and shows its own directory", async () => {
    const api = fakeApi();
    const view = fakeView();
    const app = new App(api, view);

    await app.navigate({ pwd: "/photos", query: {} });
    await app.navigate({ pwd: "/", query: {} });

    assert.deepEqual(api.calls, [{ listDirectory: "/photos" }, { listDirectory: "/" }]);
    assert.equal(view.shown.length, 2);
});
