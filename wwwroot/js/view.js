import { listingRows, listingSummary } from "./view-helpers.js";

/**
 * Renders a listing into a container element. This is the DOM side of the SPA:
 * it copies what it is given onto elements and decides nothing itself, so it
 * stays thin and is left untested on Node (decision 14). Text goes through
 * `textContent`, never `innerHTML`, so file names cannot inject markup.
 */
export class View {
    /** @param {HTMLElement} container The element whose contents the view replaces. */
    constructor(container) {
        this._container = container;
    }

    /**
     * @param {string} pwd The directory being listed, as `UrlState.pwd`.
     * @param {import("./api-client.js").Entry[]} entries
     */
    showListing(pwd, entries) {
        const body = document.createElement("tbody");
        for (const { name, href, size } of listingRows(pwd, entries)) {
            const link = document.createElement("a");
            link.href = href;
            link.textContent = name;
            body.append(row("td", [link, size]));
        }

        const { counts, totalSize } = listingSummary(entries);
        const table = document.createElement("table");
        table.append(
            section("thead", row("th", ["Name", "Size"])),
            body,
            section("tfoot", row("td", [counts, totalSize])),
        );
        this._container.replaceChildren(table);
    }
}

/**
 * A `<tr>` with one cell per value: a string becomes the cell's text, a node its child.
 * @param {"th" | "td"} cellTag
 * @param {(string | Node)[]} cells
 */
function row(cellTag, cells) {
    const tr = document.createElement("tr");
    for (const cell of cells) {
        const td = document.createElement(cellTag);
        td.append(cell);
        tr.append(td);
    }
    return tr;
}

/**
 * A table section holding one row.
 * @param {"thead" | "tfoot"} tag
 * @param {HTMLTableRowElement} tr
 */
function section(tag, tr) {
    const element = document.createElement(tag);
    element.append(tr);
    return element;
}
