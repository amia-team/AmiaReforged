/**
 * splitter.js
 *
 * Minimal splitter logic for ResizableSplit.razor. No layout framework:
 * pointer drag updates the first pane's flex-basis directly in the DOM
 * (no Blazor re-render per mousemove); the width is persisted to
 * localStorage on pointer-up and restored on init.
 *
 * Also exports observeResize for canvas editors (Cytoscape, glyph canvas):
 * a debounced ResizeObserver that notifies .NET only when the size settles.
 *
 * readJson/writeJson back LayoutPresetService persistence.
 */

const STORAGE_PREFIX = 'we-split:';

/** Read a raw string value (or null) from localStorage. */
export function readJson(key) {
    try {
        return localStorage.getItem(key);
    } catch {
        return null;
    }
}

/** Write a raw string value to localStorage (best-effort). */
export function writeJson(key, value) {
    try {
        localStorage.setItem(key, value);
    } catch {
        // Storage unavailable (private mode) — session state still works.
    }
}

function storageKey(persistKey) {
    return STORAGE_PREFIX + persistKey;
}

function readStored(persistKey, defaultWidth) {
    if (!persistKey) return defaultWidth ?? null;
    try {
        const raw = localStorage.getItem(storageKey(persistKey));
        if (raw == null) return defaultWidth ?? null;
        const value = parseInt(raw, 10);
        return Number.isFinite(value) && value > 0 ? value : (defaultWidth ?? null);
    } catch {
        return defaultWidth ?? null;
    }
}

function writeStored(persistKey, width) {
    if (!persistKey) return;
    try {
        localStorage.setItem(storageKey(persistKey), String(Math.round(width)));
    } catch {
        // Storage unavailable (private mode) — drag still works for the session.
    }
}

function panesOf(root) {
    const panes = root.querySelectorAll(':scope > .we-split__pane');
    const handle = root.querySelector(':scope > .we-split__handle');
    return { first: panes[0] ?? null, second: panes[1] ?? null, handle };
}

/**
 * Wire drag handling for one split root. Applies the stored width (if any)
 * and returns it so the Blazor component can stay in sync.
 *
 * @param {HTMLElement} root
 * @param {{direction:string, firstMin:number, secondMin:number, persistKey:string, defaultWidth:number}} options
 * @returns {number|null} the width applied
 */
export function initSplit(root, options) {
    const { direction = 'horizontal', firstMin = 180, secondMin = 240, persistKey = '', defaultWidth = 320, invert = false } = options ?? {};
    const vertical = direction === 'vertical';

    const stored = readStored(persistKey, defaultWidth);
    const { first, handle } = panesOf(root);
    if (first && stored != null) {
        first.style.flexBasis = `${stored}px`;
    }

    if (!handle || handle.dataset.splitWired === 'true') return stored;
    handle.dataset.splitWired = 'true';

    let dragging = false;
    let moved = false;
    let startPos = 0;
    let startWidth = 0;

    handle.addEventListener('pointerdown', (e) => {
        const { first: fp } = panesOf(root);
        if (!fp) return;
        dragging = true;
        moved = false;
        startPos = vertical ? e.clientY : e.clientX;
        startWidth = fp.getBoundingClientRect()[vertical ? 'height' : 'width'];
        try { handle.setPointerCapture(e.pointerId); } catch { /* ignore */ }
        e.preventDefault();
    });

    handle.addEventListener('pointermove', (e) => {
        if (!dragging) return;
        const { first: fp, second: sp } = panesOf(root);
        if (!fp) return;
        const delta = ((vertical ? e.clientY : e.clientX) - startPos) * (invert ? -1 : 1);
        if (Math.abs(delta) > 2) moved = true;

        const containerSize = root.getBoundingClientRect()[vertical ? 'height' : 'width'];
        const max = Math.max(firstMin, containerSize - secondMin - 6);
        const next = Math.min(max, Math.max(firstMin, startWidth + delta));
        fp.style.flexBasis = `${next}px`;
        fp.style.flexGrow = '0';
        fp.style.flexShrink = '0';
        if (sp) { sp.style.flex = '1'; sp.style.minWidth = '0'; sp.style.minHeight = '0'; }
    });

    const finish = (e) => {
        if (!dragging) return;
        dragging = false;
        const { first: fp } = panesOf(root);
        if (fp && moved) {
            const w = fp.getBoundingClientRect()[vertical ? 'height' : 'width'];
            writeStored(persistKey, w);
        }
        // Suppress the click-to-collapse toggle after a real drag.
        if (moved && e) {
            e.stopPropagation();
            e.preventDefault();
            // Reset for the next gesture; the Blazor click handler checks this flag.
            handle.dataset.suppressClick = 'true';
            requestAnimationFrame(() => { handle.dataset.suppressClick = 'false'; });
        }
    };

    handle.addEventListener('pointerup', finish);
    handle.addEventListener('pointercancel', () => finish(null));

    // Blazor's @onclick still fires after pointerup; skip collapse when suppressed.
    handle.addEventListener('click', (e) => {
        if (handle.dataset.suppressClick === 'true') {
            e.stopImmediatePropagation();
            e.preventDefault();
        }
    }, true);

    return stored;
}

/**
 * Observe an element's size and notify .NET (debounced). For canvas editors
 * that need to resize their backing surface when the split moves.
 *
 * @param {HTMLElement} el
 * @param {object} dotNetRef DotNetObjectReference with a method (width, height)
 * @param {string} methodName
 * @param {number} debounceMs
 * @returns {{dispose:Function}}
 */
export function observeResize(el, dotNetRef, methodName, debounceMs = 200) {
    let timer = 0;
    let lastW = 0;
    let lastH = 0;

    const observer = new ResizeObserver((entries) => {
        const rect = entries[0]?.contentRect;
        if (!rect) return;
        const w = Math.round(rect.width);
        const h = Math.round(rect.height);
        if (w === lastW && h === lastH) return;
        lastW = w;
        lastH = h;
        clearTimeout(timer);
        timer = setTimeout(() => {
            dotNetRef?.invokeMethodAsync(methodName, w, h)
                .catch((err) => console.warn('[splitter] resize callback failed:', err));
        }, debounceMs);
    });

    observer.observe(el);

    return {
        dispose() {
            clearTimeout(timer);
            observer.disconnect();
        }
    };
}

/** Observe an element by id; returns the observer handle or null. */
export function observeResizeById(id, dotNetRef, methodName, debounceMs = 200) {
    const el = document.getElementById(id);
    if (!el) return null;
    return observeResize(el, dotNetRef, methodName, debounceMs);
}
