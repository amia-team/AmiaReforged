/**
 * golden-layout-bridge.js
 * 
 * ES module that wraps Golden Layout v2's VirtualLayout to integrate with
 * Blazor Server. Blazor pre-renders panel elements in Razor markup; this
 * bridge locates them within the layout's panel root and positions them using
 * Golden Layout's virtual-component events. Prefer data-gl-panel="type"; the
 * legacy bl-panel-{type} id convention remains supported.
 *
 * Multi-instance: each layout is keyed by an instanceId string, allowing
 * multiple simultaneous GL layouts (e.g. tabbed editor panes).
 *
 * Exported functions are called from Blazor via IJSObjectReference interop.
 */

import { VirtualLayout } from './lib/golden-layout.js';

// ── Instance registry ───────────────────────────────────────────────

/**
 * @typedef {Object} LayoutInstance
 * @property {VirtualLayout}     layout
 * @property {DotNetObjectReference} dotNetRef
 * @property {HTMLElement}       glRootElement
 * @property {HTMLElement|Document} panelRootElement
 * @property {DOMRect|null}      glRootRect
 * @property {Map<object, { element: HTMLElement, componentType: string }>} componentMap
 * @property {Map<string, object>} typeToContainer
 * @property {Map<string, { width: number, height: number }>} pendingResizes
 * @property {boolean}           resizeFlushPending
 */

/** @type {Map<string, LayoutInstance>} */
const instances = new Map();

/**
 * Get an instance or throw.
 * @param {string} instanceId
 * @returns {LayoutInstance}
 */
function getInstance(instanceId) {
    const inst = instances.get(instanceId);
    if (!inst) throw new Error(`[GL Bridge] No layout instance '${instanceId}'`);
    return inst;
}

// ── Public API ──────────────────────────────────────────────────────

/**
 * Initialise Golden Layout inside a host element.
 * @param {string}  instanceId        Unique key for this layout instance
 * @param {string}  containerId       DOM id of the GL host element
 * @param {string}  layoutConfigJson  Serialised LayoutConfig JSON
 * @param {object}  blazorRef         DotNetObjectReference for callbacks
 * @param {string=} panelRootId       Optional element that contains the pre-rendered Blazor panels
 */
export function init(instanceId, containerId, layoutConfigJson, blazorRef, panelRootId) {
    // Destroy any previous instance with the same id
    if (instances.has(instanceId)) {
        destroy(instanceId);
    }

    const glRootElement = document.getElementById(containerId);
    if (!glRootElement) throw new Error(`GL host element #${containerId} not found`);

    const panelRootElement = panelRootId
        ? document.getElementById(panelRootId)
        : (glRootElement.parentElement ?? document);
    if (!panelRootElement) throw new Error(`GL panel root #${panelRootId} not found`);

    // Ensure the host is positioned so absolute children work
    const pos = getComputedStyle(glRootElement).position;
    if (pos === 'static') glRootElement.style.position = 'relative';

    /** @type {LayoutInstance} */
    const inst = {
        layout: null,
        dotNetRef: blazorRef,
        glRootElement,
        panelRootElement,
        glRootRect: null,
        componentMap: new Map(),
        typeToContainer: new Map(),
        pendingResizes: new Map(),
        resizeFlushPending: false,
    };

    inst.layout = new VirtualLayout(
        glRootElement,
        (container, itemConfig) => handleBind(inst, instanceId, container, itemConfig),
        (container) => handleUnbind(inst, instanceId, container)
    );

    // Auto-resize the layout when the container element changes size.
    // GL defaults this to false for non-body containers, but Blazor
    // renders asynchronously so the container may gain its final
    // dimensions *after* loadLayout runs.
    inst.layout.resizeWithContainerAutomatically = true;

    // Cache the root rect once before a batch of recting events
    inst.layout.beforeVirtualRectingEvent = () => {
        inst.glRootRect = glRootElement.getBoundingClientRect();
    };

    instances.set(instanceId, inst);

    const config = JSON.parse(layoutConfigJson);
    inst.layout.loadLayout(config);

    // Force a size recalc once the browser has painted the final layout.
    // This covers the common case where the Blazor-rendered container
    // starts at 0-height and only reaches its real size after the first
    // animation frame.
    requestAnimationFrame(() => {
        if (inst.layout) {
            inst.layout.updateSizeFromContainer();
        }
    });
}

/**
 * Programmatically add a panel.
 * @param {string}  instanceId     Layout instance key
 * @param {string}  componentType  Matches the bl-panel-{type} element id
 * @param {string}  title          Tab title
 * @param {object=} state          Optional componentState
 */
export function addPanel(instanceId, componentType, title, state) {
    const inst = instances.get(instanceId);
    if (!inst?.layout) return;
    inst.layout.addComponent(componentType, state ?? {}, title);
}

/**
 * Remove a panel by its componentType.
 * @param {string} instanceId
 * @param {string} componentType
 */
export function removePanel(instanceId, componentType) {
    const inst = instances.get(instanceId);
    if (!inst) return;
    const container = inst.typeToContainer.get(componentType);
    if (container && container.parent) {
        container.parent.remove();
    }
}

/**
 * Focus (activate) a panel's tab so it becomes visible in its stack.
 * @param {string} instanceId
 * @param {string} componentType
 */
export function focusPanel(instanceId, componentType) {
    const inst = instances.get(instanceId);
    if (!inst) return;
    const container = inst.typeToContainer.get(componentType);
    if (container) {
        // container is a ComponentContainer; its parent is a ComponentItem
        // which has a focus() method that activates its tab in the stack
        try { container.focus(); } catch { /* ignore */ }
    }
}

/**
 * Save the current layout configuration as JSON.
 * @param {string} instanceId
 * @returns {string} Serialised layout config
 */
export function saveLayout(instanceId) {
    const inst = instances.get(instanceId);
    if (!inst?.layout) return '{}';
    return JSON.stringify(inst.layout.saveLayout());
}

/**
 * Replace the current layout with a new config.
 * @param {string} instanceId
 * @param {string} configJson  Serialised LayoutConfig
 */
export function loadLayout(instanceId, configJson) {
    const inst = instances.get(instanceId);
    if (!inst?.layout) return;
    inst.layout.loadLayout(JSON.parse(configJson));
}

/**
 * Tear down a single layout instance and clean up.
 * @param {string} instanceId
 */
export function destroy(instanceId) {
    const inst = instances.get(instanceId);
    if (!inst) return;

    if (inst.layout) {
        inst.layout.destroy();
        inst.layout = null;
    }
    inst.componentMap.forEach(entry => {
        entry.element.style.display = 'none';
    });
    inst.componentMap.clear();
    inst.typeToContainer.clear();
    inst.glRootElement = null;
    inst.panelRootElement = null;
    inst.glRootRect = null;
    inst.dotNetRef = null;
    instances.delete(instanceId);
}

/**
 * Tear down ALL layout instances. Useful on page navigation / dispose.
 */
export function destroyAll() {
    for (const id of Array.from(instances.keys())) {
        destroy(id);
    }
}

/**
 * Get the list of currently bound component types for an instance.
 * @param {string} instanceId
 * @returns {string[]}
 */
export function getBoundPanels(instanceId) {
    const inst = instances.get(instanceId);
    if (!inst) return [];
    return Array.from(inst.typeToContainer.keys());
}

// ── Virtual component event handlers ────────────────────────────────

/**
 * Called by GL when a component needs to be bound.
 * Finds the pre-rendered Blazor panel in this layout's panel root and wires
 * positioning events. data-gl-panel is preferred; bl-panel-{type} is retained
 * for backward compatibility.
 * @param {LayoutInstance} inst
 * @param {string}         instanceId
 */
function handleBind(inst, instanceId, container, itemConfig) {
    const typeName = itemConfig.componentType ?? itemConfig.type;
    if (!typeName) {
        console.error('[GL Bridge] bindComponentEvent: no componentType in config', itemConfig);
        return { component: undefined, virtual: true };
    }

    const panelId = `bl-panel-${typeName}`;
    const escapedType = CSS.escape(String(typeName));
    const escapedPanelId = CSS.escape(panelId);
    const blazorEl =
        inst.panelRootElement?.querySelector?.(`[data-gl-panel="${escapedType}"]`) ??
        inst.panelRootElement?.querySelector?.(`#${escapedPanelId}`) ??
        document.getElementById(panelId);

    if (!blazorEl) {
        console.error(
            `[GL Bridge] No Blazor panel found for '${typeName}'. ` +
            `Add data-gl-panel="${typeName}" or id="${panelId}" inside the panel root.`
        );
        return { component: undefined, virtual: true };
    }

    // Position absolutely inside the GL root (or wherever Blazor placed it)
    blazorEl.style.position = 'absolute';
    blazorEl.style.overflow = 'hidden';
    blazorEl.style.display = 'none'; // Hidden until first rect event

    inst.componentMap.set(container, { element: blazorEl, componentType: typeName });
    inst.typeToContainer.set(typeName, container);

    // Wire virtual-component events
    container.virtualRectingRequiredEvent =
        (c, w, h) => handleRect(inst, instanceId, c, w, h);
    container.virtualVisibilityChangeRequiredEvent =
        (c, visible) => handleVisibility(inst, instanceId, c, visible);
    container.virtualZIndexChangeRequiredEvent =
        (c, _logicalZ, defaultZ) => handleZIndex(inst, c, defaultZ);

    return { component: undefined, virtual: true };
}

/**
 * Called by GL when a component is removed from the layout.
 * @param {LayoutInstance} inst
 * @param {string}         instanceId
 */
function handleUnbind(inst, instanceId, container) {
    const entry = inst.componentMap.get(container);
    if (!entry) return;

    entry.element.style.display = 'none';
    inst.componentMap.delete(container);
    inst.typeToContainer.delete(entry.componentType);

    // Notify Blazor
    if (inst.dotNetRef) {
        inst.dotNetRef.invokeMethodAsync('OnPanelRemoved', instanceId, entry.componentType)
            .catch(err => console.warn('[GL Bridge] OnPanelRemoved failed:', err));
    }
}

/**
 * Position and size a panel element to match its GL container.
 * @param {LayoutInstance} inst
 * @param {string}         instanceId
 */
function handleRect(inst, instanceId, container, width, height) {
    const entry = inst.componentMap.get(container);
    if (!entry) return;

    const containerRect = container.element.getBoundingClientRect();
    if (!inst.glRootRect) {
        inst.glRootRect = inst.glRootElement.getBoundingClientRect();
    }

    const left = containerRect.left - inst.glRootRect.left;
    const top = containerRect.top - inst.glRootRect.top;

    const el = entry.element;
    el.style.left = `${left}px`;
    el.style.top = `${top}px`;
    el.style.width = `${width}px`;
    el.style.height = `${height}px`;
    el.style.display = 'block';

    // Notify Blazor of panel resize (canvas needs to update dimensions).
    // Batch multiple recting events per animation frame into a single SignalR call.
    if (inst.dotNetRef) {
        inst.pendingResizes.set(entry.componentType, { width, height });
        if (!inst.resizeFlushPending) {
            inst.resizeFlushPending = true;
            requestAnimationFrame(() => {
                inst.resizeFlushPending = false;
                const dotNetRef = inst.dotNetRef;
                const pending = inst.pendingResizes;
                inst.pendingResizes = new Map();
                if (!dotNetRef) return;
                for (const [componentType, dims] of pending) {
                    dotNetRef.invokeMethodAsync('OnPanelResized', instanceId, componentType, dims.width, dims.height)
                        .catch(err => console.warn('[GL Bridge] OnPanelResized failed:', err));
                }
            });
        }
    }
}

/**
 * Show or hide a panel when GL tabs switch.
 * @param {LayoutInstance} inst
 * @param {string}         instanceId
 */
function handleVisibility(inst, instanceId, container, visible) {
    const entry = inst.componentMap.get(container);
    if (!entry) return;
    entry.element.style.display = visible ? 'block' : 'none';

    // Notify Blazor of visibility change
    if (inst.dotNetRef) {
        inst.dotNetRef.invokeMethodAsync('OnPanelVisibilityChanged', instanceId, entry.componentType, visible)
            .catch(err => console.warn('[GL Bridge] OnPanelVisibilityChanged failed:', err));
    }
}

/**
 * Update z-index when GL reorders overlapping panels.
 * @param {LayoutInstance} inst
 */
function handleZIndex(inst, container, defaultZIndex) {
    const entry = inst.componentMap.get(container);
    if (!entry) return;
    entry.element.style.zIndex = defaultZIndex;
}

/**
 * Poll a container element until it has non-zero dimensions, or timeout.
 * Used by InteractionEditor to wait for GL to position the canvas panel
 * before initializing the glyph editor.
 * @param {string} containerId  DOM id of the element to wait for
 * @param {number} timeoutMs    Max wait time in ms (default 2000)
 * @returns {Promise<void>}
 */
export function waitForContainerReady(containerId, timeoutMs = 2000) {
    return new Promise((resolve) => {
        const start = Date.now();
        console.log(`[GL Bridge] waitForContainerReady: polling #${containerId} (timeout=${timeoutMs}ms)`);
        function check() {
            const el = document.getElementById(containerId);
            if (el && el.clientHeight > 0 && el.clientWidth > 0) {
                console.log(`[GL Bridge] waitForContainerReady: #${containerId} ready (${el.clientWidth}x${el.clientHeight}) after ${Date.now() - start}ms`);
                resolve();
            } else if (Date.now() - start > timeoutMs) {
                const dims = el ? `${el.clientWidth}x${el.clientHeight}` : 'not found';
                console.warn(`[GL Bridge] waitForContainerReady: #${containerId} TIMEOUT after ${Date.now() - start}ms (dims=${dims})`);
                resolve(); // resolve anyway, caller handles the error
            } else {
                requestAnimationFrame(check);
            }
        }
        requestAnimationFrame(check);
    });
}
