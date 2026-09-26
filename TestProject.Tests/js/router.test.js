import { test } from "node:test";
import assert from "node:assert/strict";
import { Router } from "../../wwwroot/js/router.js";

/**
 * A stand-in for `window` with a hash and a `hashchange` event that `fire` raises.
 * @param {string} hash
 */
function fakeWindow(hash) {
    const listeners = [];
    return {
        location: { hash },
        addEventListener(type, listener) {
            assert.equal(type, "hashchange");
            listeners.push(listener);
        },
        fire() {
            listeners.forEach(listener => listener());
        },
    };
}

test("current parses the window's hash", () => {
    const router = new Router(fakeWindow("#/photos?q=cat"));

    assert.deepEqual(router.current(), { pwd: "/photos", query: { q: "cat" } });
});

test("go merges the changes into the hash", () => {
    const window = fakeWindow("#/photos?q=cat");

    new Router(window).go({ query: {} });

    assert.equal(window.location.hash, "#/photos");
});

test("listen reports the new state on every hashchange", () => {
    const window = fakeWindow("#/");
    const seen = [];
    new Router(window).listen(state => seen.push(state));

    window.location.hash = "#/photos";
    window.fire();
    window.location.hash = "#/docs?q=x";
    window.fire();

    assert.deepEqual(seen, [
        { pwd: "/photos", query: {} },
        { pwd: "/docs", query: { q: "x" } },
    ]);
});
