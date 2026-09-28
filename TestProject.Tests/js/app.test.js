import { test } from "node:test";
import assert from "node:assert/strict";
import { ApiError } from "../../TestProject/wwwroot/js/api-client.js";
import { App } from "../../TestProject/wwwroot/js/app.js";

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
        async upload(path, file) {
            calls.push({ upload: [path, file.name] });
            return entries[0];
        },
    };
}

/**
 * A stand-in for Router at a given state. It records `go` calls and lets a test
 * raise a change through `change`.
 * @param {import("../../TestProject/wwwroot/js/url-state.js").UrlState} state
 */
function fakeRouter(state = { pwd: "/", query: {} }) {
    const went = [];
    let listener;
    return {
        went,
        current: () => state,
        go(changes) {
            went.push(changes);
        },
        listen(callback) {
            listener = callback;
        },
        change(newState) {
            state = newState;
            return listener(newState);
        },
    };
}

/** A stand-in for View that records the app it was given and what it was asked to show. */
function fakeView() {
    const shown = [];
    return {
        shown,
        app: undefined,
        setApp(app) {
            this.app = app;
        },
        showListing(pwd, entries) {
            shown.push({ listing: { pwd, entries } });
        },
        showSearchResults(pwd, query, entries) {
            shown.push({ search: { pwd, query, entries } });
        },
        showError(pwd, error) {
            shown.push({ error: { pwd, error } });
        },
        showUploadError(pwd, fileName, error) {
            shown.push({ uploadError: { pwd, fileName, error } });
        },
    };
}

/** A stand-in for ApiClient whose every call rejects with `error`. */
function failingApi(error) {
    return {
        listDirectory: async () => { throw error; },
        search: async () => { throw error; },
        upload: async () => { throw error; },
    };
}

/** An App built as main.js builds it, with the initial navigation done. */
async function startedApp({ api = fakeApi(), router = fakeRouter(), view = fakeView() } = {}) {
    const app = new App(api, router, view);
    await app.start();
    return app;
}

test("constructing an App gives the view the app to report to", () => {
    const view = fakeView();

    const app = new App(fakeApi(), fakeRouter(), view);

    assert.equal(view.app, app);
});

test("start navigates to the state the URL names now", async () => {
    const api = fakeApi();
    const view = fakeView();

    await startedApp({ api, router: fakeRouter({ pwd: "/photos", query: {} }), view });

    assert.deepEqual(api.calls, [{ listDirectory: "/photos" }]);
    assert.deepEqual(view.shown, [{ listing: { pwd: "/photos", entries } }]);
});

test("start follows every later URL change", async () => {
    const api = fakeApi();
    const router = fakeRouter();
    await startedApp({ api, router });

    await router.change({ pwd: "/docs", query: {} });

    assert.deepEqual(api.calls, [{ listDirectory: "/" }, { listDirectory: "/docs" }]);
});

test("a state with a q searches the directory and shows the results", async () => {
    const api = fakeApi();
    const view = fakeView();

    await startedApp({ api, router: fakeRouter({ pwd: "/photos", query: { q: "cat" } }), view });

    assert.deepEqual(api.calls, [{ search: ["/photos", "cat"] }]);
    assert.deepEqual(view.shown, [{ search: { pwd: "/photos", query: "cat", entries } }]);
});

test("a state with an empty q lists rather than searches", async () => {
    const api = fakeApi();

    await startedApp({ api, router: fakeRouter({ pwd: "/photos", query: { q: "" } }) });

    assert.deepEqual(api.calls, [{ listDirectory: "/photos" }]);
});

test("search moves to the same directory with the trimmed text as the query", async () => {
    const router = fakeRouter({ pwd: "/photos", query: {} });
    const app = await startedApp({ router });

    app.search("  cat ");

    assert.deepEqual(router.went, [{ query: { q: "cat" } }]);
});

test("search with blank text drops the query", async () => {
    const router = fakeRouter({ pwd: "/photos", query: { q: "cat" } });
    const app = await startedApp({ router });

    app.search("   ");

    assert.deepEqual(router.went, [{ query: {} }]);
});

test("a failed listing is shown as an error for that directory, and navigate resolves", async () => {
    const error = new ApiError(404, "/api/dir?path=%2Fnope");
    const view = fakeView();

    await startedApp({ api: failingApi(error), router: fakeRouter({ pwd: "/nope", query: {} }), view });

    assert.deepEqual(view.shown, [{ error: { pwd: "/nope", error } }]);
});

test("a failed search is shown as an error for the directory searched", async () => {
    const error = new ApiError(404, "/api/search?path=%2Fnope&q=cat");
    const view = fakeView();

    await startedApp({ api: failingApi(error), router: fakeRouter({ pwd: "/nope", query: { q: "cat" } }), view });

    assert.deepEqual(view.shown, [{ error: { pwd: "/nope", error } }]);
});

test("a request that never reaches the server is shown as an error too", async () => {
    const error = new TypeError("Failed to fetch");
    const view = fakeView();

    await startedApp({ api: failingApi(error), view });

    assert.deepEqual(view.shown, [{ error: { pwd: "/", error } }]);
});

test("upload sends the file into the current directory and shows that directory again", async () => {
    const api = fakeApi();
    const view = fakeView();
    const app = await startedApp({ api, router: fakeRouter({ pwd: "/photos", query: {} }), view });

    await app.upload(new File(["hi"], "notes.txt"));

    assert.deepEqual(api.calls, [{ listDirectory: "/photos" }, { upload: ["/photos", "notes.txt"] }, { listDirectory: "/photos" }]);
    assert.equal(view.shown.length, 2);
});

test("upload during a search shows the search again, so a matching new file appears", async () => {
    const api = fakeApi();
    const app = await startedApp({ api, router: fakeRouter({ pwd: "/photos", query: { q: "note" } }) });

    await app.upload(new File([""], "notes.txt"));

    assert.deepEqual(api.calls.at(-1), { search: ["/photos", "note"] });
});

test("a failed upload is reported for the file and directory, and the page is not reloaded", async () => {
    const error = new ApiError(409, "/api/file");
    const view = fakeView();
    const app = new App(failingApi(error), fakeRouter({ pwd: "/photos", query: {} }), view);

    await app.upload(new File([""], "notes.txt"));

    assert.deepEqual(view.shown, [{ uploadError: { pwd: "/photos", fileName: "notes.txt", error } }]);
});
