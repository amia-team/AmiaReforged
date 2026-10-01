// Browser-local search over a projection of compiler metadata supplied by Blazor.
// Blazor owns categories/details; this module owns only the empty search-results subtree.
const panels = new WeakMap();
const normalize = text => [...text].filter(c => /[\p{L}\p{Nd}]/u.test(c)).join('').toLowerCase();
export function rankReference(fields, query) {
    const tokens = query.split(/\s+/u).map(normalize).filter(Boolean);
    let rank = 0;
    for (const token of tokens) {
        const score = fields[0] === token || fields[1] === token ? 0 : fields[0].startsWith(token) ? 1
            : fields[2].includes(token) ? 2 : fields[3].includes(token) ? 3 : fields[4].includes(token) ? 4 : -1;
        if (score < 0) return -1;
        rank = Math.max(rank, score);
    }
    return rank;
}
function unavailable(entry, panel) {
    return entry.availableIn && panel.context?.event && panel.events.includes(panel.context.event)
        && !entry.availableIn.some(a => a.event === panel.context.event && (!panel.context.stage || a.stage === panel.context.stage));
}
function render(panel) {
    const query = panel.input.value;
    const searching = !!query.trim() && panel.entries.length > 0;
    panel.host.classList.toggle('glyph-reference--searching', searching);
    panel.results.replaceChildren();
    if (!searching) return;
    const matches = panel.entries.filter(e => e.tab === panel.tab).map(e => ({ entry: e, rank: rankReference(e.searchFields, query) }))
        .filter(e => e.rank >= 0).sort((a, b) => a.rank - b.rank
            || (panel.prioritize ? Number(!!unavailable(a.entry, panel)) - Number(!!unavailable(b.entry, panel)) : 0)
            || (a.entry.name < b.entry.name ? -1 : a.entry.name > b.entry.name ? 1 : 0));
    const count = document.createElement('small');
    count.textContent = matches.length ? `${matches.length} results in ${panel.tab}` : `No matching ${panel.tab.toLowerCase()}.`;
    panel.results.append(count);
    for (const { entry } of matches.slice(0, panel.limit)) {
        const row = document.createElement('button');
        row.type = 'button'; row.className = 'glyph-reference-row'; row.title = entry.name;
        row.classList.toggle('glyph-reference-unavailable', !!unavailable(entry, panel));
        row.setAttribute('aria-pressed', String(panel.selected === entry.name));
        const name = document.createElement('code'); name.textContent = entry.name; row.append(name);
        if (entry.deprecated) { const badge = document.createElement('small'); badge.textContent = 'Deprecated'; row.append(badge); }
        const select = insert => {
            panel.selected = entry.name;
            for (const button of panel.results.querySelectorAll('.glyph-reference-row')) button.setAttribute('aria-pressed', String(button === row));
            panel.callback.invokeMethodAsync('OnReferenceSelected', entry.tab, entry.name, insert, panel.generation).catch(() => {});
        };
        row.addEventListener('click', () => select(false));
        row.addEventListener('dblclick', () => select(true));
        row.addEventListener('keydown', event => {
            const rows = [...panel.results.querySelectorAll('.glyph-reference-row')];
            if (event.key === 'ArrowDown' || event.key === 'ArrowUp') {
                event.preventDefault(); rows[Math.max(0, Math.min(rows.length - 1, rows.indexOf(row) + (event.key === 'ArrowDown' ? 1 : -1)))].focus();
            }
        });
        panel.results.append(row);
    }
    if (matches.length > panel.limit) {
        const more = document.createElement('button'); more.type = 'button'; more.className = 'glyph-reference-more'; more.textContent = 'Show more results';
        more.addEventListener('click', () => { panel.limit += 80; render(panel); }); panel.results.append(more);
    }
}
export function setReferenceSearch(host, callback, generation, entries, events) {
    let panel = panels.get(host);
    if (!panel) {
        const input = host.querySelector('input[type=search]');
        const results = host.querySelector('.glyph-reference-client-results');
        if (!input || !results) return;
        panel = { host, input, results, tab: 'Functions', prioritize: true, context: null, selected: null, limit: 80 };
        panel.listener = event => { event.stopPropagation(); panel.limit = 80; render(panel); };
        input.addEventListener('input', panel.listener);
        results.setAttribute('aria-label', 'Reference search results');
        panel.observer = new MutationObserver(() => { if (!host.isConnected) destroyReferenceSearch(host); });
        panel.observer.observe(document.body, { childList: true, subtree: true });
        panels.set(host, panel);
    }
    if (panel.generation > generation) return;
    Object.assign(panel, { callback, generation, entries, events, selected: null, limit: 80 });
    render(panel);
}
export function setReferenceSearchState(host, tab, prioritize, context, selected) {
    const panel = panels.get(host);
    if (!panel) return;
    const changed = panel.tab !== tab || panel.prioritize !== prioritize || JSON.stringify(panel.context) !== JSON.stringify(context);
    Object.assign(panel, { tab, prioritize, context, selected });
    if (changed) { panel.limit = 80; render(panel); }
    else for (const row of panel.results.querySelectorAll('.glyph-reference-row')) row.setAttribute('aria-pressed', String(row.title === selected));
}
export function destroyReferenceSearch(host) {
    const panel = panels.get(host);
    if (!panel) return;
    panel.input.removeEventListener('input', panel.listener); panel.observer.disconnect();
    panel.results.replaceChildren(); host.classList.remove('glyph-reference--searching'); panels.delete(host);
}
