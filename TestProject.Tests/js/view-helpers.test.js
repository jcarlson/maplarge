import { test } from "node:test";
import assert from "node:assert/strict";
import { entryHref, entryName, entrySize } from "../../wwwroot/js/view-helpers.js";

const directory = { path: "photos/2024 summer", isDirectory: true, size: null };
const file = { path: "photos/cat.jpg", isDirectory: false, size: 12 };

test("entryHref: a directory links to its own listing in the hash", () => {
    assert.equal(entryHref(directory), "#/photos/2024%20summer");
});

test("entryHref: a file links to the download endpoint", () => {
    assert.equal(entryHref(file), "/api/file?path=photos%2Fcat.jpg");
});

test("entryName is the last path segment, with a trailing slash for a directory", () => {
    assert.equal(entryName(directory), "2024 summer/");
    assert.equal(entryName(file), "cat.jpg");
});

test("entryName of a top-level entry is the whole path", () => {
    assert.equal(entryName({ path: "notes.txt", isDirectory: false, size: 0 }), "notes.txt");
});

test("entrySize is the byte count for a file and -- for a directory", () => {
    assert.equal(entrySize(file), "12");
    assert.equal(entrySize(directory), "--");
});
