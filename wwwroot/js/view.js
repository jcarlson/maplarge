import { breadcrumbs, directoryHref, errorMessage, listingRows, listingSummary, searchRows } from "./view-helpers.js";

/**
 * The DOM side of the SPA. It renders what the App gives it and reports what the
 * user does (a submitted search) to the App. It copies values onto elements and
 * decides nothing itself, so it stays thin and is left untested on Node (decision
 * 14). Text goes through `textContent`, never `innerHTML`, so file names cannot
 * inject markup.
 */
export class View {
    /** @type {import("./app.js").App | undefined} */
    _app;

    /**
     * @param {HTMLElement} container The element whose contents the view replaces.
     * @param {HTMLFormElement} searchForm The form with the `q` input and the clear link.
     */
    constructor(container, searchForm) {
        this._container = container;
        this._searchForm = searchForm;
        searchForm.addEventListener("submit", event => {
            event.preventDefault();
            this._app?.search(searchForm.elements.q.value);
        });
    }

    /**
     * Sets the app the user's actions are reported to; until then they go nowhere.
     * Called by the App's constructor, which closes the loop between the two.
     * @param {import("./app.js").App} app
     */
    setApp(app) {
        this._app = app;
    }

    /**
     * Shows a directory's contents, with the search box empty.
     * @param {string} pwd The directory being listed, as `UrlState.pwd`.
     * @param {import("./api-client.js").Entry[]} entries
     */
    showListing(pwd, entries) {
        this._showSearchForm(pwd, "");
        this._show(pwd, listingRows(pwd, entries), entries);
    }

    /**
     * Shows the matches of a search within a directory, with the search box
     * holding the query so a deep link arrives with it filled in.
     * @param {string} pwd The directory that was searched, as `UrlState.pwd`.
     * @param {string} query The text that was searched for.
     * @param {import("./api-client.js").Entry[]} entries The matches.
     */
    showSearchResults(pwd, query, entries) {
        this._showSearchForm(pwd, query);
        this._show(pwd, searchRows(pwd, entries), entries);
    }

    /**
     * Fills the search box with `query` and points the clear link at `pwd` without a
     * search; the link is shown only while a search is active.
     * @param {string} pwd
     * @param {string} query
     */
    _showSearchForm(pwd, query) {
        const form = this._searchForm;
        form.elements.q.value = query;
        const clear = form.querySelector("a");
        clear.href = directoryHref(pwd);
        clear.hidden = query === "";
    }

    /**
     * Shows that loading `pwd` failed, in place of its table. The breadcrumbs stay,
     * so a directory that does exist is one click away.
     * @param {string} pwd The directory that could not be shown.
     * @param {unknown} error What the load threw.
     */
    showError(pwd, error) {
        this._showSearchForm(pwd, "");
        const message = document.createElement("p");
        message.className = "error";
        message.textContent = errorMessage(pwd, error);
        this._container.replaceChildren(breadcrumbBar(pwd), message);
    }

    /**
     * Replaces the container with the breadcrumb bar for `pwd` and a table of `rows`,
     * summarised in its footer.
     * @param {string} pwd
     * @param {import("./view-helpers.js").Row[]} rows
     * @param {import("./api-client.js").Entry[]} entries The entries the rows came from.
     */
    _show(pwd, rows, entries) {
        const body = document.createElement("tbody");
        for (const { name, href, size } of rows) {
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
        this._container.replaceChildren(breadcrumbBar(pwd), table);
    }
}

/**
 * The `<nav>` of breadcrumbs for `pwd`.
 * @param {string} pwd
 */
function breadcrumbBar(pwd) {
    const nav = document.createElement("nav");
    breadcrumbs(pwd).forEach(({ name, href }, index) => {
        if (index > 0) {
            nav.append(" \u203a ");
        }
        const crumb = document.createElement(href ? "a" : "span");
        if (href) {
            crumb.href = href;
        }
        crumb.textContent = name;
        nav.append(crumb);
    });
    return nav;
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
