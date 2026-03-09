/**
 * Glyph Visual Editor — Canvas-based node graph editor for Blazor interop.
 *
 * This is a self-contained, vanilla-JS node editor that stores/produces
 * a GlyphGraph JSON structure compatible with the server-side GlyphInterpreter.
 *
 * Exported API (ES module):
 *   initGlyphEditor(containerId, catalogJson, graphJson, dotNetRef) — mount the editor
 *   getGraphJson() — serialize the current graph state
 *   addNode(nodeDefJson) — add a node from the palette
 *   getSelectedNode() — get the currently selected node info (for property panel)
 *   setPropertyOverride(nodeId, pinId, value) — set a property override on a node's input pin
 *   destroy() — clean up
 */

// ==================== Constants ====================

const PIN_RADIUS = 7;
const PIN_SPACING = 24;
const NODE_HEADER_HEIGHT = 28;
const NODE_MIN_WIDTH = 180;
const NODE_PADDING = 12;
const GRID_SIZE = 20;
const ZOOM_SPEED = 0.001;
const ZOOM_MIN = 0.2;
const ZOOM_MAX = 3.0;

const COLOR_MAP = {
    event:  '#e74c3c',
    flow:   '#3498db',
    action: '#2ecc71',
    getter: '#9b59b6',
    math:   '#f39c12',
    logic:  '#1abc9c',
};

const DATA_TYPE_COLORS = {
    Exec:     '#ffffff',
    Bool:     '#cc0000',
    Int:      '#1abc9c',
    Float:    '#2ecc71',
    String:   '#f1c40f',
    NwObject: '#e67e22',
    Location: '#9b59b6',
    Effect:   '#3498db',
    List:     '#e91e63',
};

// ==================== State ====================

let canvas, ctx;
let catalog = [];       // GlyphNodeCatalogEntryDto[]
let nodes = [];         // Runtime node instances { id, typeId, x, y, pins[], width, height, def }
let edges = [];         // { id, srcNodeId, srcPinId, tgtNodeId, tgtPinId }
let variables = [];     // { name, dataType, defaultValue }
let graphEventType = ''; // EventType loaded from the graph JSON — preserved across save cycles

// Camera
let camX = 0, camY = 0, zoom = 1.0;

// Interaction
let draggingNode = null;
let dragOffsetX = 0, dragOffsetY = 0;
let panStartX = 0, panStartY = 0;
let isPanning = false;
let connectingFrom = null; // { nodeId, pinId, pin, x, y }
let mouseX = 0, mouseY = 0;
let selectedNodeId = null;
let hoveredPin = null;
let blazorRef = null; // DotNet object reference for callbacks

// ==================== Graph JSON Helpers ====================

function generateId() {
    return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, c => {
        const r = Math.random() * 16 | 0;
        return (c === 'x' ? r : (r & 0x3 | 0x8)).toString(16);
    });
}

function buildGraphJson() {
    return JSON.stringify({
        Id: generateId(),
        Name: '',
        Description: '',
        EventType: graphEventType,
        Nodes: nodes.map(n => ({
            InstanceId: n.id,
            TypeId: n.typeId,
            PositionX: Math.round(n.x),
            PositionY: Math.round(n.y),
            PropertyOverrides: n.propertyOverrides || {},
            Comment: null,
        })),
        Edges: edges.map(e => ({
            Id: e.id,
            SourceNodeId: e.srcNodeId,
            SourcePinId: e.srcPinId,
            TargetNodeId: e.tgtNodeId,
            TargetPinId: e.tgtPinId,
        })),
        Variables: variables,
    });
}

function loadGraphJson(json) {
    nodes = [];
    edges = [];
    variables = [];
    graphEventType = '';

    if (!json || json === '{}') return;

    let g;
    try { g = JSON.parse(json); } catch { return; }

    if (g.EventType) graphEventType = g.EventType;
    if (g.Variables) variables = g.Variables;

    if (g.Nodes) {
        for (const n of g.Nodes) {
            const def = catalog.find(d => d.TypeId === n.TypeId);
            if (!def) continue;
            const nodeInst = createNodeInstance(def, n.PositionX, n.PositionY, n.InstanceId);
            if (n.PropertyOverrides) {
                nodeInst.propertyOverrides = { ...n.PropertyOverrides };
            }
            nodes.push(nodeInst);
        }
    }

    if (g.Edges) {
        for (const e of g.Edges) {
            edges.push({
                id: e.Id,
                srcNodeId: e.SourceNodeId,
                srcPinId: e.SourcePinId,
                tgtNodeId: e.TargetNodeId,
                tgtPinId: e.TargetPinId,
            });
        }
    }
}

// ==================== Node Instance Builder ====================

function createNodeInstance(def, x, y, existingId) {
    const inputPins = (def.InputPins || []).map((p, i) => ({
        ...p, dir: 'input', index: i, nodeX: 0, nodeY: 0,
    }));
    const outputPins = (def.OutputPins || []).map((p, i) => ({
        ...p, dir: 'output', index: i, nodeX: 0, nodeY: 0,
    }));

    const pinCount = Math.max(inputPins.length, outputPins.length, 1);
    const height = NODE_HEADER_HEIGHT + pinCount * PIN_SPACING + NODE_PADDING;

    // Measure text widths for node sizing
    let maxTextWidth = NODE_MIN_WIDTH;
    if (ctx) {
        ctx.font = '12px monospace';
        for (const p of [...inputPins, ...outputPins]) {
            const tw = ctx.measureText(p.Name).width + 50;
            if (tw > maxTextWidth) maxTextWidth = tw;
        }
    }
    const width = Math.max(maxTextWidth, NODE_MIN_WIDTH);

    // Calculate pin positions (relative to node top-left)
    for (const p of inputPins) {
        p.nodeX = 0;
        p.nodeY = NODE_HEADER_HEIGHT + p.index * PIN_SPACING + PIN_SPACING / 2;
    }
    for (const p of outputPins) {
        p.nodeX = width;
        p.nodeY = NODE_HEADER_HEIGHT + p.index * PIN_SPACING + PIN_SPACING / 2;
    }

    return {
        id: existingId || generateId(),
        typeId: def.TypeId,
        x, y, width, height,
        def,
        inputPins,
        outputPins,
        propertyOverrides: {},
    };
}

// ==================== Drawing ====================

function draw() {
    if (!ctx) return;

    const w = canvas.width;
    const h = canvas.height;

    ctx.clearRect(0, 0, w, h);
    ctx.save();

    // Background
    ctx.fillStyle = '#12121f';
    ctx.fillRect(0, 0, w, h);

    // Grid
    drawGrid(w, h);

    // Apply camera transform
    ctx.translate(w / 2 + camX, h / 2 + camY);
    ctx.scale(zoom, zoom);

    // Draw edges
    for (const edge of edges) {
        drawEdge(edge);
    }

    // Draw connecting wire (in progress)
    if (connectingFrom) {
        drawWire(
            connectingFrom.x, connectingFrom.y,
            (mouseX - w / 2 - camX) / zoom,
            (mouseY - h / 2 - camY) / zoom,
            connectingFrom.pin.DataType || 'Exec'
        );
    }

    // Draw nodes
    for (const node of nodes) {
        drawNode(node);
    }

    ctx.restore();
    requestAnimationFrame(draw);
}

function drawGrid(w, h) {
    const step = GRID_SIZE * zoom;
    if (step < 5) return;

    ctx.strokeStyle = '#1e1e35';
    ctx.lineWidth = 1;

    const offsetX = (camX + w / 2) % step;
    const offsetY = (camY + h / 2) % step;

    ctx.beginPath();
    for (let x = offsetX; x < w; x += step) {
        ctx.moveTo(x, 0);
        ctx.lineTo(x, h);
    }
    for (let y = offsetY; y < h; y += step) {
        ctx.moveTo(0, y);
        ctx.lineTo(w, y);
    }
    ctx.stroke();
}

function drawNode(node) {
    const color = COLOR_MAP[node.def.ColorClass?.toLowerCase()] || '#7f8c8d';
    const isSelected = node.id === selectedNodeId;

    // Shadow
    ctx.shadowColor = 'rgba(0,0,0,0.5)';
    ctx.shadowBlur = 8;
    ctx.shadowOffsetX = 2;
    ctx.shadowOffsetY = 2;

    // Body
    ctx.fillStyle = '#2a2a40';
    ctx.strokeStyle = isSelected ? '#ffffff' : color;
    ctx.lineWidth = isSelected ? 2.5 : 1.5;

    roundRect(ctx, node.x, node.y, node.width, node.height, 6);
    ctx.fill();
    ctx.stroke();

    // Reset shadow
    ctx.shadowColor = 'transparent';
    ctx.shadowBlur = 0;
    ctx.shadowOffsetX = 0;
    ctx.shadowOffsetY = 0;

    // Header bar
    ctx.fillStyle = color;
    ctx.beginPath();
    ctx.moveTo(node.x + 6, node.y);
    ctx.lineTo(node.x + node.width - 6, node.y);
    ctx.arcTo(node.x + node.width, node.y, node.x + node.width, node.y + 6, 6);
    ctx.lineTo(node.x + node.width, node.y + NODE_HEADER_HEIGHT);
    ctx.lineTo(node.x, node.y + NODE_HEADER_HEIGHT);
    ctx.lineTo(node.x, node.y + 6);
    ctx.arcTo(node.x, node.y, node.x + 6, node.y, 6);
    ctx.closePath();
    ctx.fill();

    // Header text
    ctx.fillStyle = '#ffffff';
    ctx.font = 'bold 11px sans-serif';
    ctx.textBaseline = 'middle';
    ctx.textAlign = 'center';
    ctx.fillText(node.def.DisplayName, node.x + node.width / 2, node.y + NODE_HEADER_HEIGHT / 2);

    // Pins
    for (const pin of node.inputPins) {
        drawPin(node.x + pin.nodeX, node.y + pin.nodeY, pin, 'input', node);
    }
    for (const pin of node.outputPins) {
        drawPin(node.x + pin.nodeX, node.y + pin.nodeY, pin, 'output', node);
    }
}

function drawPin(x, y, pin, dir, node) {
    const typeColor = DATA_TYPE_COLORS[pin.DataType] || '#aaaaaa';
    const isExec = pin.DataType === 'Exec';
    const isHovered = hoveredPin && hoveredPin.pin === pin;

    ctx.fillStyle = isHovered ? '#ffffff' : typeColor;
    ctx.strokeStyle = typeColor;
    ctx.lineWidth = 1.5;

    if (isExec) {
        // Triangle for exec pins
        const size = PIN_RADIUS;
        ctx.beginPath();
        if (dir === 'input') {
            ctx.moveTo(x - size, y - size);
            ctx.lineTo(x + size, y);
            ctx.lineTo(x - size, y + size);
        } else {
            ctx.moveTo(x + size, y - size);
            ctx.lineTo(x - size, y);
            ctx.lineTo(x + size, y + size);
        }
        ctx.closePath();
        ctx.fill();
        ctx.stroke();
    } else {
        // Circle for data pins
        ctx.beginPath();
        ctx.arc(x, y, PIN_RADIUS, 0, Math.PI * 2);
        ctx.fill();
        ctx.stroke();
    }

    // Pin label
    ctx.fillStyle = '#cccccc';
    ctx.font = '10px sans-serif';
    ctx.textBaseline = 'middle';
    if (dir === 'input') {
        ctx.textAlign = 'left';
        ctx.fillText(pin.Name, x + PIN_RADIUS + 5, y);

        // Show override or default value on unconnected data input pins
        if (!isExec && node && !isPinConnected(node.id, pin.Id, 'input')) {
            const overrides = node.propertyOverrides || {};
            const val = overrides[pin.Id] !== undefined ? overrides[pin.Id] : (pin.DefaultValue ?? '');
            if (val !== '' && val !== undefined && val !== null) {
                ctx.fillStyle = '#777799';
                ctx.font = 'italic 9px sans-serif';
                const labelWidth = ctx.measureText(pin.Name).width;
                ctx.fillText('= ' + val, x + PIN_RADIUS + 10 + labelWidth, y);
            }
        }
    } else {
        ctx.textAlign = 'right';
        ctx.fillText(pin.Name, x - PIN_RADIUS - 5, y);
    }
}

function drawEdge(edge) {
    const srcNode = nodes.find(n => n.id === edge.srcNodeId);
    const tgtNode = nodes.find(n => n.id === edge.tgtNodeId);
    if (!srcNode || !tgtNode) return;

    const srcPin = srcNode.outputPins.find(p => p.Id === edge.srcPinId);
    const tgtPin = tgtNode.inputPins.find(p => p.Id === edge.tgtPinId);
    if (!srcPin || !tgtPin) return;

    drawWire(
        srcNode.x + srcPin.nodeX, srcNode.y + srcPin.nodeY,
        tgtNode.x + tgtPin.nodeX, tgtNode.y + tgtPin.nodeY,
        srcPin.DataType || 'Exec'
    );
}

function drawWire(x1, y1, x2, y2, dataType) {
    const color = DATA_TYPE_COLORS[dataType] || '#aaaaaa';
    const dx = Math.abs(x2 - x1) * 0.5;

    ctx.strokeStyle = color;
    ctx.lineWidth = 2;
    ctx.beginPath();
    ctx.moveTo(x1, y1);
    ctx.bezierCurveTo(x1 + dx, y1, x2 - dx, y2, x2, y2);
    ctx.stroke();
}

function roundRect(ctx, x, y, w, h, r) {
    ctx.beginPath();
    ctx.moveTo(x + r, y);
    ctx.lineTo(x + w - r, y);
    ctx.arcTo(x + w, y, x + w, y + r, r);
    ctx.lineTo(x + w, y + h - r);
    ctx.arcTo(x + w, y + h, x + w - r, y + h, r);
    ctx.lineTo(x + r, y + h);
    ctx.arcTo(x, y + h, x, y + h - r, r);
    ctx.lineTo(x, y + r);
    ctx.arcTo(x, y, x + r, y, r);
    ctx.closePath();
}

function isPinConnected(nodeId, pinId, dir) {
    if (dir === 'input') {
        return edges.some(e => e.tgtNodeId === nodeId && e.tgtPinId === pinId);
    }
    return edges.some(e => e.srcNodeId === nodeId && e.srcPinId === pinId);
}

// ==================== Hit Testing ====================

function screenToWorld(sx, sy) {
    const w = canvas.width;
    const h = canvas.height;
    return {
        x: (sx - w / 2 - camX) / zoom,
        y: (sy - h / 2 - camY) / zoom,
    };
}

function hitTestNode(wx, wy) {
    // Iterate in reverse for top-most first
    for (let i = nodes.length - 1; i >= 0; i--) {
        const n = nodes[i];
        if (wx >= n.x && wx <= n.x + n.width && wy >= n.y && wy <= n.y + n.height) {
            return n;
        }
    }
    return null;
}

function hitTestPin(wx, wy) {
    const threshold = PIN_RADIUS + 4;
    for (const node of nodes) {
        for (const pin of node.inputPins) {
            const px = node.x + pin.nodeX;
            const py = node.y + pin.nodeY;
            if (Math.hypot(wx - px, wy - py) < threshold) {
                return { node, pin, dir: 'input', x: px, y: py };
            }
        }
        for (const pin of node.outputPins) {
            const px = node.x + pin.nodeX;
            const py = node.y + pin.nodeY;
            if (Math.hypot(wx - px, wy - py) < threshold) {
                return { node, pin, dir: 'output', x: px, y: py };
            }
        }
    }
    return null;
}

// ==================== Input Handling ====================

function onMouseDown(e) {
    const rect = canvas.getBoundingClientRect();
    const sx = (e.clientX - rect.left) * (canvas.width / rect.width);
    const sy = (e.clientY - rect.top) * (canvas.height / rect.height);
    const wp = screenToWorld(sx, sy);

    // Check for pin click -> start connection
    const pinHit = hitTestPin(wp.x, wp.y);
    if (pinHit) {
        if (pinHit.dir === 'output') {
            connectingFrom = {
                nodeId: pinHit.node.id,
                pinId: pinHit.pin.Id,
                pin: pinHit.pin,
                x: pinHit.x,
                y: pinHit.y,
            };
        } else {
            // Allow dragging from input pin (reverse connection)
            connectingFrom = {
                nodeId: pinHit.node.id,
                pinId: pinHit.pin.Id,
                pin: pinHit.pin,
                x: pinHit.x,
                y: pinHit.y,
                reverse: true,
            };
        }
        return;
    }

    // Check for node click -> start drag
    const nodeHit = hitTestNode(wp.x, wp.y);
    if (nodeHit) {
        draggingNode = nodeHit;
        dragOffsetX = wp.x - nodeHit.x;
        dragOffsetY = wp.y - nodeHit.y;
        selectedNodeId = nodeHit.id;
        notifySelectionChanged();

        // Bring to front
        const idx = nodes.indexOf(nodeHit);
        if (idx >= 0) {
            nodes.splice(idx, 1);
            nodes.push(nodeHit);
        }
        return;
    }

    // Pan
    isPanning = true;
    panStartX = e.clientX;
    panStartY = e.clientY;
    selectedNodeId = null;
    notifySelectionChanged();
}

function onMouseMove(e) {
    const rect = canvas.getBoundingClientRect();
    mouseX = (e.clientX - rect.left) * (canvas.width / rect.width);
    mouseY = (e.clientY - rect.top) * (canvas.height / rect.height);
    const wp = screenToWorld(mouseX, mouseY);

    // Update hovered pin
    hoveredPin = hitTestPin(wp.x, wp.y);

    if (draggingNode) {
        draggingNode.x = wp.x - dragOffsetX;
        draggingNode.y = wp.y - dragOffsetY;

        // Snap to grid
        draggingNode.x = Math.round(draggingNode.x / GRID_SIZE) * GRID_SIZE;
        draggingNode.y = Math.round(draggingNode.y / GRID_SIZE) * GRID_SIZE;
    }

    if (isPanning) {
        camX += (e.clientX - panStartX);
        camY += (e.clientY - panStartY);
        panStartX = e.clientX;
        panStartY = e.clientY;
    }
}

function onMouseUp(e) {
    const rect = canvas.getBoundingClientRect();
    const sx = (e.clientX - rect.left) * (canvas.width / rect.width);
    const sy = (e.clientY - rect.top) * (canvas.height / rect.height);
    const wp = screenToWorld(sx, sy);

    if (connectingFrom) {
        const pinHit = hitTestPin(wp.x, wp.y);
        if (pinHit && pinHit.node.id !== connectingFrom.nodeId) {
            // Determine src/tgt based on direction
            let srcNodeId, srcPinId, tgtNodeId, tgtPinId;
            if (connectingFrom.reverse) {
                // Started from input, landing on output
                if (pinHit.dir === 'output') {
                    srcNodeId = pinHit.node.id;
                    srcPinId = pinHit.pin.Id;
                    tgtNodeId = connectingFrom.nodeId;
                    tgtPinId = connectingFrom.pinId;
                }
            } else {
                // Started from output, landing on input
                if (pinHit.dir === 'input') {
                    srcNodeId = connectingFrom.nodeId;
                    srcPinId = connectingFrom.pinId;
                    tgtNodeId = pinHit.node.id;
                    tgtPinId = pinHit.pin.Id;
                }
            }

            if (srcNodeId && tgtNodeId) {
                // Validate types match (or Exec→Exec)
                const srcNode = nodes.find(n => n.id === srcNodeId);
                const tgtNode = nodes.find(n => n.id === tgtNodeId);
                const srcPin = srcNode?.outputPins.find(p => p.Id === srcPinId);
                const tgtPin = tgtNode?.inputPins.find(p => p.Id === tgtPinId);

                if (srcPin && tgtPin && srcPin.DataType === tgtPin.DataType) {
                    // Remove existing connection to target pin if not multi-connect
                    if (!tgtPin.AllowMultipleConnections) {
                        edges = edges.filter(e => !(e.tgtNodeId === tgtNodeId && e.tgtPinId === tgtPinId));
                    }
                    if (!srcPin.AllowMultipleConnections && srcPin.DataType !== 'Exec') {
                        edges = edges.filter(e => !(e.srcNodeId === srcNodeId && e.srcPinId === srcPinId));
                    }

                    // Prevent duplicate edges
                    const exists = edges.some(e =>
                        e.srcNodeId === srcNodeId && e.srcPinId === srcPinId &&
                        e.tgtNodeId === tgtNodeId && e.tgtPinId === tgtPinId);

                    if (!exists) {
                        edges.push({
                            id: generateId(),
                            srcNodeId, srcPinId,
                            tgtNodeId, tgtPinId,
                        });
                    }
                }
            }
        }
        connectingFrom = null;
    }

    draggingNode = null;
    isPanning = false;
}

function onWheel(e) {
    e.preventDefault();
    const delta = -e.deltaY * ZOOM_SPEED;
    zoom = Math.max(ZOOM_MIN, Math.min(ZOOM_MAX, zoom + delta));
}

function onKeyDown(e) {
    if (e.key === 'Delete' || e.key === 'Backspace') {
        if (selectedNodeId && document.activeElement === canvas) {
            // Remove node and its edges
            nodes = nodes.filter(n => n.id !== selectedNodeId);
            edges = edges.filter(e => e.srcNodeId !== selectedNodeId && e.tgtNodeId !== selectedNodeId);
            selectedNodeId = null;
            notifySelectionChanged();
        }
    }
}

function onContextMenu(e) {
    e.preventDefault();

    const rect = canvas.getBoundingClientRect();
    const sx = (e.clientX - rect.left) * (canvas.width / rect.width);
    const sy = (e.clientY - rect.top) * (canvas.height / rect.height);
    const wp = screenToWorld(sx, sy);

    // Right-click on an edge? Find the closest edge and delete it
    let closestEdge = null;
    let closestDist = 20; // Threshold in world units

    for (const edge of edges) {
        const srcNode = nodes.find(n => n.id === edge.srcNodeId);
        const tgtNode = nodes.find(n => n.id === edge.tgtNodeId);
        if (!srcNode || !tgtNode) continue;

        const srcPin = srcNode.outputPins.find(p => p.Id === edge.srcPinId);
        const tgtPin = tgtNode.inputPins.find(p => p.Id === edge.tgtPinId);
        if (!srcPin || !tgtPin) continue;

        // Simplified distance check — midpoint of the bezier
        const mx = (srcNode.x + srcPin.nodeX + tgtNode.x + tgtPin.nodeX) / 2;
        const my = (srcNode.y + srcPin.nodeY + tgtNode.y + tgtPin.nodeY) / 2;
        const dist = Math.hypot(wp.x - mx, wp.y - my);

        if (dist < closestDist) {
            closestDist = dist;
            closestEdge = edge;
        }
    }

    if (closestEdge) {
        edges = edges.filter(e => e.id !== closestEdge.id);
    }
}

// ==================== Selection Helpers ====================

function getSelectedNodeData() {
    if (!selectedNodeId) return null;
    const node = nodes.find(n => n.id === selectedNodeId);
    if (!node) return null;

    const inputPinData = node.inputPins
        .filter(p => p.DataType !== 'Exec')
        .map(p => ({
            id: p.Id,
            name: p.Name,
            dataType: p.DataType,
            defaultValue: p.DefaultValue ?? null,
            overrideValue: (node.propertyOverrides || {})[p.Id] ?? null,
            isConnected: isPinConnected(node.id, p.Id, 'input'),
        }));

    return {
        nodeId: node.id,
        typeId: node.typeId,
        displayName: node.def.DisplayName,
        inputPins: inputPinData,
    };
}

function notifySelectionChanged() {
    if (blazorRef) {
        const data = getSelectedNodeData();
        blazorRef.invokeMethodAsync('OnNodeSelectionChanged', JSON.stringify(data));
    }
}

// ==================== Resize ====================

function resizeCanvas() {
    if (!canvas) return;
    const container = canvas.parentElement;
    if (!container) return;

    canvas.width = container.clientWidth;
    canvas.height = container.clientHeight;
}

let resizeObserver = null;

// ==================== Public API ====================

export function initGlyphEditor(containerId, catalogJson, graphJson, dotNetRef) {
    blazorRef = dotNetRef || null;
    const container = document.getElementById(containerId);
    if (!container) {
        console.error('Glyph editor container not found:', containerId);
        return;
    }

    // Create canvas element
    canvas = document.createElement('canvas');
    canvas.style.width = '100%';
    canvas.style.height = '100%';
    canvas.tabIndex = 0; // Make focusable for keyboard events
    container.innerHTML = '';
    container.appendChild(canvas);

    ctx = canvas.getContext('2d');
    resizeCanvas();

    // Parse catalog
    try {
        catalog = JSON.parse(catalogJson);
    } catch {
        console.warn('Failed to parse node catalog');
        catalog = [];
    }

    // Load existing graph
    loadGraphJson(graphJson);

    // If no nodes, center camera at origin
    if (nodes.length > 0) {
        // Center on existing nodes
        let avgX = 0, avgY = 0;
        for (const n of nodes) {
            avgX += n.x + n.width / 2;
            avgY += n.y + n.height / 2;
        }
        avgX /= nodes.length;
        avgY /= nodes.length;
        camX = -avgX * zoom;
        camY = -avgY * zoom;
    }

    // Event listeners
    canvas.addEventListener('mousedown', onMouseDown);
    canvas.addEventListener('mousemove', onMouseMove);
    canvas.addEventListener('mouseup', onMouseUp);
    canvas.addEventListener('wheel', onWheel, { passive: false });
    canvas.addEventListener('keydown', onKeyDown);
    canvas.addEventListener('contextmenu', onContextMenu);

    // Resize observer
    resizeObserver = new ResizeObserver(() => resizeCanvas());
    resizeObserver.observe(container);

    // Start render loop
    requestAnimationFrame(draw);
}

export function getGraphJson() {
    return buildGraphJson();
}

export function addNode(nodeDefJson) {
    let def;
    try { def = JSON.parse(nodeDefJson); } catch { return; }

    // Check singleton
    if (def.IsSingleton) {
        const exists = nodes.some(n => n.typeId === def.TypeId);
        if (exists) return; // Don't add duplicates
    }

    // Place near center of view
    const w = canvas ? canvas.width : 800;
    const h = canvas ? canvas.height : 600;
    const wp = screenToWorld(w / 2, h / 2);

    // Add slight random offset so multiple adds don't stack
    const ox = (Math.random() - 0.5) * 60;
    const oy = (Math.random() - 0.5) * 60;

    const node = createNodeInstance(def, wp.x + ox, wp.y + oy);
    nodes.push(node);
    selectedNodeId = node.id;
}

export function getSelectedNode() {
    return JSON.stringify(getSelectedNodeData());
}

export function setPropertyOverride(nodeId, pinId, value) {
    const node = nodes.find(n => n.id === nodeId);
    if (!node) return;
    if (!node.propertyOverrides) node.propertyOverrides = {};
    if (value === null || value === undefined || value === '') {
        delete node.propertyOverrides[pinId];
    } else {
        node.propertyOverrides[pinId] = value;
    }
}

export function destroy() {
    if (canvas) {
        canvas.removeEventListener('mousedown', onMouseDown);
        canvas.removeEventListener('mousemove', onMouseMove);
        canvas.removeEventListener('mouseup', onMouseUp);
        canvas.removeEventListener('wheel', onWheel);
        canvas.removeEventListener('keydown', onKeyDown);
        canvas.removeEventListener('contextmenu', onContextMenu);
    }

    if (resizeObserver) {
        resizeObserver.disconnect();
        resizeObserver = null;
    }

    canvas = null;
    ctx = null;
    nodes = [];
    edges = [];
    catalog = [];
}
