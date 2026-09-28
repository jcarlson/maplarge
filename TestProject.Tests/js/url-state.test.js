import { test } from "node:test";
import assert from "node:assert/strict";
import { mergeState, parseState, serializeState } from "../../TestProject/wwwroot/js/url-state.js";

test("parseState reads an empty or bare fragment as the base directory", () => {
    for (const hash of ["", "#", "#/"]) {
        assert.deepEqual(parseState(hash), { pwd: "/", query: {} }, `hash ${JSON.stringify(hash)}`);
    }
});

test("parseState reads the path with a leading slash", () => {
    assert.deepEqual(parseState("#/photos/2024"), { pwd: "/photos/2024", query: {} });
});

test("parseState decodes percent-encoded path segments", () => {
    assert.equal(parseState("#/photos/2024%20summer/caf%C3%A9").pwd, "/photos/2024 summer/café");
});

test("parseState keeps a segment that does not decode as typed", () => {
    assert.equal(parseState("#/100%").pwd, "/100%");
});

test("parseState ignores repeated and trailing slashes", () => {
    assert.equal(parseState("#//photos//2024/").pwd, "/photos/2024");
});

test("parseState tolerates a missing # and a missing leading slash", () => {
    assert.deepEqual(parseState("photos?q=x"), { pwd: "/photos", query: { q: "x" } });
});

test("parseState reads every key=value pair after the ?", () => {
    assert.deepEqual(parseState("#/?q=summer"), { pwd: "/", query: { q: "summer" } });
    assert.deepEqual(parseState("#/photos?q=a+b%26c&sort=size").query, { q: "a b&c", sort: "size" });
});

test("parseState keeps a key with an empty value", () => {
    assert.deepEqual(parseState("#/photos?q=").query, { q: "" });
});

test("serializeState writes the base directory with no query as #/", () => {
    assert.equal(serializeState({ pwd: "/", query: {} }), "#/");
});

test("serializeState percent-encodes each path segment", () => {
    assert.equal(serializeState({ pwd: "/photos/2024 summer/café", query: {} }), "#/photos/2024%20summer/caf%C3%A9");
});

test("serializeState normalises a pwd without a leading slash or with stray slashes", () => {
    assert.equal(serializeState({ pwd: "photos", query: {} }), "#/photos");
    assert.equal(serializeState({ pwd: "/photos//2024/", query: {} }), "#/photos/2024");
});

test("serializeState appends the query pairs and leaves the ? out when there are none", () => {
    assert.equal(serializeState({ pwd: "/photos", query: { q: "a b&c", sort: "size" } }), "#/photos?q=a+b%26c&sort=size");
    assert.equal(serializeState({ pwd: "/photos", query: {} }), "#/photos");
});

test("serializeState and parseState round-trip", () => {
    const states = [
        { pwd: "/", query: {} },
        { pwd: "/", query: { q: "summer" } },
        { pwd: "/photos/2024 summer", query: {} },
        { pwd: "/a?b/c#d/e%f", query: { q: "x/y?z=&w", "k=y": "" } },
    ];
    for (const state of states) {
        assert.deepEqual(parseState(serializeState(state)), state, JSON.stringify(state));
    }
});

test("mergeState keeps the directory when only the query changes", () => {
    assert.equal(mergeState("#/photos/2024", { query: { q: "cat" } }), "#/photos/2024?q=cat");
});

test("mergeState keeps the query when only the directory changes", () => {
    assert.equal(mergeState("#/photos?q=cat", { pwd: "/docs" }), "#/docs?q=cat");
});

test("mergeState replaces the whole query rather than adding to it", () => {
    assert.equal(mergeState("#/photos?q=cat&sort=size", { query: { q: "dog" } }), "#/photos?q=dog");
    assert.equal(mergeState("#/photos?q=cat", { query: {} }), "#/photos");
});

test("mergeState with no changes re-serializes the current state", () => {
    assert.equal(mergeState("#photos//2024/"), "#/photos/2024");
    assert.equal(mergeState(""), "#/");
});
