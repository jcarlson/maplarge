import { test } from "node:test";
import assert from "node:assert/strict";
import { ApiClient, ApiError, fileUrl } from "../../wwwroot/js/api-client.js";

/**
 * A fake `fetch` that records the URL it was asked for and answers with `body` as JSON.
 * @param {number} status
 * @param {unknown} [body]
 */
function fakeFetch(status, body) {
    const calls = [];
    const fetch = async (url, init) => {
        calls.push(init ? { url, init } : url);
        return new Response(JSON.stringify(body ?? null), {
            status,
            headers: { "Content-Type": "application/json" },
        });
    };
    return { fetch, calls };
}

const entries = [
    { path: "photos", isDirectory: true, size: null },
    { path: "notes.txt", isDirectory: false, size: 12 },
];

test("listDirectory requests /api/dir with the path as a query parameter", async () => {
    const { fetch, calls } = fakeFetch(200, entries);

    await new ApiClient(fetch).listDirectory("photos/2024 summer");

    assert.deepEqual(calls, ["/api/dir?path=photos%2F2024+summer"]);
});

test("listDirectory sends an empty path for the base directory", async () => {
    const { fetch, calls } = fakeFetch(200, entries);

    await new ApiClient(fetch).listDirectory("");

    assert.deepEqual(calls, ["/api/dir?path="]);
});

test("listDirectory returns the entries the server sent", async () => {
    const { fetch } = fakeFetch(200, entries);

    const result = await new ApiClient(fetch).listDirectory("");

    assert.deepEqual(result, entries);
});

test("listDirectory throws an ApiError carrying the status for a non-OK response", async () => {
    const { fetch } = fakeFetch(404);

    await assert.rejects(
        () => new ApiClient(fetch).listDirectory("missing"),
        (error) => error instanceof ApiError && error.status === 404,
    );
});

test("search requests /api/search with the path and query as parameters", async () => {
    const { fetch, calls } = fakeFetch(200, entries);

    await new ApiClient(fetch).search("photos/2024 summer", "cat & dog");

    assert.deepEqual(calls, ["/api/search?path=photos%2F2024+summer&q=cat+%26+dog"]);
});

test("search returns the entries the server sent", async () => {
    const { fetch } = fakeFetch(200, entries);

    const result = await new ApiClient(fetch).search("", "cat");

    assert.deepEqual(result, entries);
});

test("search throws an ApiError carrying the status for a non-OK response", async () => {
    const { fetch } = fakeFetch(400);

    await assert.rejects(
        () => new ApiClient(fetch).search("", ""),
        (error) => error instanceof ApiError && error.status === 400,
    );
});

test("fileUrl points at GET /api/file with the path as a query parameter", () => {
    assert.equal(fileUrl("photos/2024 summer/cat.jpg"), "/api/file?path=photos%2F2024+summer%2Fcat.jpg");
});

test("upload POSTs the file as the multipart 'file' part to /api/file with the directory as a parameter", async () => {
    const { fetch, calls } = fakeFetch(201, entries[1]);
    const file = new File(["hello"], "notes.txt");

    await new ApiClient(fetch).upload("photos/2024 summer", file);

    assert.equal(calls.length, 1);
    assert.equal(calls[0].url, "/api/file?path=photos%2F2024+summer");
    assert.equal(calls[0].init.method, "POST");
    assert.ok(calls[0].init.body instanceof FormData);
    assert.equal(calls[0].init.body.get("file").name, "notes.txt");
});

test("upload returns the new entry the server sent", async () => {
    const { fetch } = fakeFetch(201, entries[1]);

    const result = await new ApiClient(fetch).upload("", new File([""], "notes.txt"));

    assert.deepEqual(result, entries[1]);
});

test("upload throws an ApiError carrying the status for a non-OK response", async () => {
    const { fetch } = fakeFetch(409);

    await assert.rejects(
        () => new ApiClient(fetch).upload("", new File([""], "notes.txt")),
        (error) => error instanceof ApiError && error.status === 409,
    );
});
