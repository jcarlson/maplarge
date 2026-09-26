import { test } from "node:test";
import assert from "node:assert/strict";
import { entryHref, entryName, entrySize, formatSize, listingRows, parentPath } from "../../wwwroot/js/view-helpers.js";

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

test("entrySize is the formatted size for a file and -- for a directory", () => {
    assert.equal(entrySize(file), "12 B");
    assert.equal(entrySize({ path: "big.iso", isDirectory: false, size: 3 * 1024 ** 3 }), "3.00 G");
    assert.equal(entrySize(directory), "--");
});

test("formatSize shows whole bytes below 1 K", () => {
    assert.equal(formatSize(0), "0 B");
    assert.equal(formatSize(1), "1 B");
    assert.equal(formatSize(1023), "1,023 B");
});

test("formatSize scales to the largest unit that keeps the number under 1,024", () => {
    assert.equal(formatSize(1024), "1.00 K");
    assert.equal(formatSize(1024 ** 2), "1.00 M");
    assert.equal(formatSize(1024 ** 3), "1.00 G");
    assert.equal(formatSize(1024 ** 4), "1.00 T");
});

test("formatSize rounds to two decimals", () => {
    assert.equal(formatSize(1372), "1.34 K");
    assert.equal(formatSize(1536), "1.50 K");
    assert.equal(formatSize(2.345 * 1024 ** 2), "2.35 M");
});

test("formatSize separates thousands", () => {
    assert.equal(formatSize(1000 * 1024 ** 2), "1,000.00 M");
});

test("formatSize stays in T beyond 1,024 T", () => {
    assert.equal(formatSize(2048 * 1024 ** 4), "2,048.00 T");
});

test("parentPath drops the last segment; the base is its own parent", () => {
    assert.equal(parentPath("/photos/2024"), "/photos");
    assert.equal(parentPath("/photos"), "/");
    assert.equal(parentPath("/"), "/");
});

test("listingRows starts with . and .. then one row per entry", () => {
    const rows = listingRows("/photos", { entries: [directory, file], totalSize: 12 });

    assert.deepEqual(rows, [
        { name: ".", href: "#/photos", size: "--" },
        { name: "..", href: "#/", size: "--" },
        { name: "2024 summer/", href: "#/photos/2024%20summer", size: "--" },
        { name: "cat.jpg", href: "/api/file?path=photos%2Fcat.jpg", size: "12 B" },
    ]);
});

test("listingRows at the base points both . and .. at the base", () => {
    const rows = listingRows("/", { entries: [], totalSize: 0 });

    assert.deepEqual(rows.map(row => row.href), ["#/", "#/"]);
});
