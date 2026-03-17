/**
 * golden-layout-bridge.js
 * 
 * ES module that wraps Golden Layout v2's VirtualLayout to integrate with
 * Blazor Server.  Blazor pre-renders panel <div>s in the Razor markup; this
 * bridge positions them absolutely inside the GL host element using GL's
 * virtual-component events.
 *
 * Exported functions are called from Blazor via IJSObjectReference interop.
 */

import { VirtualLayout } from './lib/golden-layout.js';

// ── Module state ────────────────────────────────────────────────────
let /** @type {VirtualLayout|null} */     layout = null;
let /** @type {DotNetObjectReference} */  dotNetRef = null;
let /** @type {HTMLElement|null} */        glRootElement = null;
let /** @type {DOMRect|null} */           glRootRect = null;

/** @type {Map<object, { element: HTMLElement, componentType: string }>} */
const componentMap = new Map();

/** @type {Map<string, object>} componentType → container (for removePanel) */
const typeToContainer = new Map();

// ── Public API ──────────────────────────────────────────────────────

/**
 * Initialise Golden Layout inside a host element.
 * @param {string}  containerId       DOM id of the GL host element
 * @param {string}  layoutConfigJson  Serialised LayoutConfig JSON
 * @param {object}  blazorRef         DotNetObjectReference for callbacks
 */
export function init(containerId, layoutConfigJson, blazorRef) {
    dotNetRef = blazorRef;
    glRootElement = document.getElementById(containerId);
    if (!glRootElement) throw new Error(`GL host element #${containerId} not found`);

    // Ensure the host is positioned so absolute children work
    const pos = getComputedStyle(glRootElement).position;
    if (pos === 'static') glRootElement.style.position = 'relative';

    layout = new VirtualLayout(
        glRootElement,
        handleBind,
        handleUnbind
    );

    // Cache the root rect once before a batch of recting events
    layout.beforeVirtualRectingEvent = () => {
        glRootRect = glRootElement.getBoundingClientRect();
    };

    const config = JSON.parse(layoutConfigJson);
    layout.loadLayout(config);
}

/**
 * Programmatically add a panel.
 * @param {string}  componentType  Matches the bl-panel-{type} element id
 * @param {string}  title          Tab title
 * @param {object=} state          Optional componentState
 */
export function addPanel(componentType, title, state) {
    if (!layout) return;
    layout.addComponent(componentType, state ?? {}, title);
}

/**
 * Remove a panel by its componentType.
 * @param {string} componentType
 */
export function removePanel(componentType) {
    const container = typeToContainer.get(componentType);
    if (container && container.parent) {
        container.parent.remove();
    }
}

/**
 * Save the current layout configuration as JSON.
 * @returns {string} Serialised layout config
 */
export function saveLayout() {
    if (!layout) return '{}';
    return JSON.stringify(layout.saveLayout());
}

/**
 * Replace the current layout with a new config.
 * @param {string} configJson  Serialised LayoutConfig
 */
export function loadLayout(configJson) {
    if (!layout) return;
    layout.loadLayout(JSON.parse(configJson));
}

/**
 * Tear down the layout and clean up.
 */
export function destroy() {
    if (layout) {
        layout.destroy();
        layout = null;
    }
    componentMap.forEach(entry => {
        entry.element.style.display = 'none';
    });
    componentMap.clear();
    typeToContainer.clear();
    glRootElement = null;
    glRootRect = null;
    dotNetRef = null;
}

/**
 * Get the list of currently bound component types.
 * @returns {string[]}
 */
export function getBoundPanels() {
    return Array.from(typeToContainer.keys());
}

// ── Virtual component event handlers ────────────────────────────────

/**
 * Called by GL when a component needs to be bound.
 * Finds the pre-rendered Blazor <div id="bl-panel-{type}"> and wires
 * positioning events.
 */
function handleBind(container, itemConfig) {
    const typeName = itemConfig.componentType ?? itemConfig.type;
    if (!typeName) {
        console.error('[GL Bridge] bindComponentEvent: no componentType in config', itemConfig);
        return { component: undefined, virtual: true };
    }

    const panelId = `bl-panel-${typeName}`;
    const blazorEl = document.getElementById(panelId);
    if (!blazorEl) {
        console.error(`[GL Bridge] No Blazor panel element found: #${panelId}`);
        return { component: undefined, virtual: true };
    }

    // Position absolutely inside the GL root (or wherever Blazor placed it)
    blazorEl.style.position = 'absolute';
    blazorEl.style.overflow = 'hidden';
    blazorEl.style.display = 'none'; // Hidden until first rect event

    componentMap.set(container, { element: blazorEl, componentType: typeName });
    typeToContainer.set(typeName, container);

    // Wire virtual-component events
    container.virtualRectingRequiredEvent =
        (c, w, h) => handleRect(c, w, h);
    container.virtualVisibilityChangeRequiredEvent =
        (c, visible) => handleVisibility(c, visible);
    container.virtualZIndexChangeRequiredEvent =
        (c, _logicalZ, defaultZ) => handleZIndex(c, defaultZ);

    return { component: undefined, virtual: true };
}

/**
 * Called by GL when a component is removed from the layout.
 */
function handleUnbind(container) {
    const entry = componentMap.get(container);
    if (!entry) return;

    entry.element.style.display = 'none';
    componentMap.delete(container);
    typeToContainer.delete(entry.componentType);

    // Notify Blazor
    if (dotNetRef) {
        dotNetRef.invokeMethodAsync('OnPanelRemoved', entry.componentType)
            .catch(err => console.warn('[GL Bridge] OnPanelRemoved failed:', err));
    }
}

/**
 * Position and size a panel element to match its GL container.
 */
function handleRect(container, width, height) {
    const entry = componentMap.get(container);
    if (!entry) return;

    const containerRect = container.element.getBoundingClientRect();
    if (!glRootRect) {
        glRootRect = glRootElement.getBoundingClientRect();
    }

    const left = containerRect.left - glRootRect.left;
    const top = containerRect.top - glRootRect.top;

    const el = entry.element;
    el.style.left = `${left}px`;
    el.style.top = `${top}px`;
    el.style.width = `${width}px`;
    el.style.height = `${height}px`;
    el.style.display = '';

    // Notify Blazor of panel resize (canvas needs to update dimensions)
    if (dotNetRef) {
        dotNetRef.invokeMethodAsync('OnPanelResized', entry.componentType, width, height)
            .catch(err => console.warn('[GL Bridge] OnPanelResized failed:', err));
    }
}

/**
 * Show or hide a panel when GL tabs switch.
 */
function handleVisibility(container, visible) {
    const entry = componentMap.get(container);
    if (!entry) return;
    entry.element.style.display = visible ? '' : 'none';

    // Notify Blazor of visibility change
    if (dotNetRef) {
        dotNetRef.invokeMethodAsync('OnPanelVisibilityChanged', entry.componentType, visible)
            .catch(err => console.warn('[GL Bridge] OnPanelVisibilityChanged failed:', err));
    }
}

/**
 * Update z-index when GL reorders overlapping panels.
 */
function handleZIndex(container, defaultZIndex) {
    const entry = componentMap.get(container);
    if (!entry) return;
    entry.element.style.zIndex = defaultZIndex;
}
