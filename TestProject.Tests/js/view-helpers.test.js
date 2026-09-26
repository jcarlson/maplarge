import { test } from "node:test";
import assert from "node:assert/strict";
import { entryHref } from "../../wwwroot/js/view-helpers.js";

test("a directory links to its own listing in the hash", () => {
    const href = entryHref({ path: "photos/2024 summer", isDirectory: true, size: null });

    assert.equal(href, "#/photos/2024%20summer");
});

test("a file links to the download endpoint", () => {
    const href = entryHref({ path: "photos/cat.jpg", isDirectory: false, size: 12 });

    assert.equal(href, "/api/file?path=photos%2Fcat.jpg");
});
