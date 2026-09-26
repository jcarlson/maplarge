/**
 * Renders a listing into a container element. This is the DOM side of the SPA:
 * it copies what it is given onto elements and decides nothing itself, so it
 * stays thin and is left untested on Node (decision 14). Text goes through
 * `textContent`, never `innerHTML`, so file names cannot inject markup.
 */
export class ListingView {
    /** @param {HTMLElement} container The element whose contents the view replaces. */
    constructor(container) {
        this._container = container;
    }

    /** @param {import("./api-client.js").Listing} listing */
    showListing(listing) {
        const list = document.createElement("ul");
        for (const entry of listing.entries) {
            const item = document.createElement("li");
            item.textContent = entry.path;
            list.append(item);
        }
        this._container.replaceChildren(list);
    }
}
