import { test } from "node:test";
import assert from "node:assert/strict";
import { ApiError } from "../../wwwroot/js/api-client.js";
import { breadcrumbs, directoryHref, entryHref, entryName, entrySize, errorMessage, formatSize, listingRows, listingSummary, parentPath, searchRows, uploadErrorMessage } from "../../wwwroot/js/view-helpers.js";

const directory = { path: "photos/2024 summer", isDirectory: true, size: null };
const file = { path: "photos/cat.jpg", isDirectory: false, size: 12 };

test("breadcrumbs start at the base, link every ancestor, and leave the current directory unlinked", () => {
    assert.deepEqual(breadcrumbs("/photos/2024 summer"), [
        { name: "/", href: "#/" },
        { name: "photos", href: "#/photos" },
        { name: "2024 summer" },
    ]);
});

test("breadcrumbs for the base are the base alone, unlinked", () => {
    assert.deepEqual(breadcrumbs("/"), [{ name: "/" }]);
});

test("directoryHref links to a directory's listing with no search", () => {
    assert.equal(directoryHref("/photos/2024 summer"), "#/photos/2024%20summer");
    assert.equal(directoryHref("/"), "#/");
});

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
    const rows = listingRows("/photos", [directory, file]);

    assert.deepEqual(rows, [
        { name: ".", href: "#/photos", size: "--" },
        { name: "..", href: "#/", size: "--" },
        { name: "2024 summer/", href: "#/photos/2024%20summer", size: "--" },
        { name: "cat.jpg", href: "/api/file?path=photos%2Fcat.jpg", size: "12 B" },
    ]);
});

test("listingRows at the base points both . and .. at the base", () => {
    const rows = listingRows("/", []);

    assert.deepEqual(rows.map(row => row.href), ["#/", "#/"]);
});

test("searchRows names each match by its path below the searched directory, with no . or ..", () => {
    const deep = { path: "photos/2024 summer/cats/tabby.jpg", isDirectory: false, size: 3 };

    const rows = searchRows("/photos", [directory, deep]);

    assert.deepEqual(rows, [
        { name: "2024 summer/", href: "#/photos/2024%20summer", size: "--" },
        { name: "2024 summer/cats/tabby.jpg", href: "/api/file?path=photos%2F2024+summer%2Fcats%2Ftabby.jpg", size: "3 B" },
    ]);
});

test("searchRows from the base uses the whole path", () => {
    assert.equal(searchRows("/", [file])[0].name, "photos/cat.jpg");
});

test("listingSummary counts folders and files and totals the file sizes", () => {
    const big = { path: "big.bin", isDirectory: false, size: 1524 };

    const summary = listingSummary([directory, directory, file, big]);

    assert.deepEqual(summary, { counts: "2 folders, 2 files", totalSize: "1.50 K" });
});

test("listingSummary uses the singular for one and the plural for zero", () => {
    assert.equal(listingSummary([directory]).counts, "1 folder, 0 files");
    assert.equal(listingSummary([]).counts, "0 folders, 0 files");
});

test("listingSummary totals zero for no files, ignoring directories", () => {
    assert.equal(listingSummary([directory]).totalSize, "0 B");
});

test("errorMessage explains each API status in terms of the directory", () => {
    assert.equal(errorMessage("/nope", new ApiError(404, "/api/dir")), "There is no directory at /nope.");
    assert.equal(errorMessage("/../x", new ApiError(400, "/api/dir")), "/../x is not a path this browser can show.");
    assert.equal(errorMessage("/", new ApiError(503, "/api/dir")), "The server could not answer (status 503).");
});

test("errorMessage falls back to the error's own message for anything else", () => {
    assert.equal(errorMessage("/", new TypeError("Failed to fetch")), "Something went wrong: Failed to fetch");
    assert.equal(errorMessage("/", "boom"), "Something went wrong: boom");
});

test("uploadErrorMessage explains each API status in terms of the file and directory", () => {
    assert.equal(uploadErrorMessage("/photos", "cat.jpg", new ApiError(409, "/api/file")), "/photos already has an entry named cat.jpg.");
    assert.equal(uploadErrorMessage("/nope", "cat.jpg", new ApiError(404, "/api/file")), "There is no directory at /nope to upload into.");
    assert.equal(uploadErrorMessage("/", "..", new ApiError(400, "/api/file")), ".. is not a name that can be uploaded.");
    assert.equal(uploadErrorMessage("/", "cat.jpg", new ApiError(500, "/api/file")), "The server could not take the upload (status 500).");
});

test("uploadErrorMessage falls back to the error's own message for anything else", () => {
    assert.equal(uploadErrorMessage("/", "cat.jpg", new TypeError("Failed to fetch")), "Something went wrong: Failed to fetch");
});
