import { listingRows } from "./view-helpers.js";

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
     * @param {import("./api-client.js").Listing} listing
     */
    showListing(pwd, listing) {
        const table = document.createElement("table");
        table.append(row("th", ["Name", "Size"]));
        for (const { name, href, size } of listingRows(pwd, listing)) {
            const link = document.createElement("a");
            link.href = href;
            link.textContent = name;
            table.append(row("td", [link, size]));
        }
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
