import { test } from "node:test";
import assert from "node:assert/strict";
import { App } from "../../wwwroot/js/app.js";

test("start() runs without error", () => {
    const app = new App();

    assert.doesNotThrow(() => app.start());
});
