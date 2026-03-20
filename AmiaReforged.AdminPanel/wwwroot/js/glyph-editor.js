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

// Types that can implicitly convert to String when connecting to a String input pin
const STRING_CONVERTIBLE_TYPES = new Set(['Int', 'Float', 'Bool', 'NwObject']);

const STAGE_NODE_COLOR = '#0088aa';
const STAGE_NODE_MIN_WIDTH = 240;
const STAGE_TYPE_IDS = [
    'stage.interaction_attempted',
    'stage.interaction_started',
    'stage.interaction_tick',
    'stage.interaction_completed',
];
const STAGE_NODE_HEADER_HEIGHT = 44;
const STAGE_LABELS = {
    'stage.interaction_attempted': '1. Attempted',
    'stage.interaction_started':   '2. Started',
    'stage.interaction_tick':      '3. Tick',
    'stage.interaction_completed': '4. Completed',
};
const STAGE_SUBTITLES = {
    'stage.interaction_attempted': 'Entry point \u00b7 fires on attempt',
    'stage.interaction_started':   'Entry point \u00b7 fires on start',
    'stage.interaction_tick':      'Entry point \u00b7 fires each round (~6s)',
    'stage.interaction_completed': 'Entry point \u00b7 fires on completion',
};

const LOOP_BODY_WIRE_COLOR = '#00bcd4'; // Cyan for loop-body exec wires
const LOOP_NODE_TYPE_IDS = ['flow.for_each']; // Node types that get a loop badge

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
let hoveredEdge = null;   // Edge object currently under the cursor
let contextMenuEl = null; // Floating context menu DOM element
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
                // Merge saved overrides on top of definition defaults
                nodeInst.propertyOverrides = { ...nodeInst.propertyOverrides, ...n.PropertyOverrides };
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

    const isStage = def.TypeId && def.TypeId.startsWith('stage.');
    const headerH = isStage ? STAGE_NODE_HEADER_HEIGHT : NODE_HEADER_HEIGHT;
    const pinCount = Math.max(inputPins.length, outputPins.length, 1);
    const height = headerH + pinCount * PIN_SPACING + NODE_PADDING;

    // Measure text widths for node sizing
    let maxTextWidth = NODE_MIN_WIDTH;
    if (ctx) {
        ctx.font = '12px monospace';
        for (const p of [...inputPins, ...outputPins]) {
            const tw = ctx.measureText(p.Name).width + 50;
            if (tw > maxTextWidth) maxTextWidth = tw;
        }
    }
    const minW = isStage ? STAGE_NODE_MIN_WIDTH : NODE_MIN_WIDTH;
    const width = Math.max(maxTextWidth, minW);

    // Calculate pin positions (relative to node top-left)
    for (const p of inputPins) {
        p.nodeX = 0;
        p.nodeY = headerH + p.index * PIN_SPACING + PIN_SPACING / 2;
    }
    for (const p of outputPins) {
        p.nodeX = width;
        p.nodeY = headerH + p.index * PIN_SPACING + PIN_SPACING / 2;
    }

    // Seed property overrides with defaults from the definition's Properties list
    const defaultOverrides = {};
    if (def.Properties) {
        for (const prop of def.Properties) {
            if (prop.DefaultValue != null && prop.DefaultValue !== '') {
                defaultOverrides[prop.Id] = prop.DefaultValue;
            }
        }
    }

    return {
        id: existingId || generateId(),
        typeId: def.TypeId,
        x, y, width, height,
        def,
        inputPins,
        outputPins,
        propertyOverrides: defaultOverrides,
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

    // Draw pipeline guide lines between stage nodes
    drawPipelineGuides();

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
    const isStage = isStageNode(node);
    const colorKey = node.def.ColorClass?.replace('node-', '') || '';
    const color = isStage ? STAGE_NODE_COLOR : (COLOR_MAP[colorKey] || '#7f8c8d');
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
    const headerH = isStage ? STAGE_NODE_HEADER_HEIGHT : NODE_HEADER_HEIGHT;
    ctx.fillStyle = color;
    ctx.beginPath();
    ctx.moveTo(node.x + 6, node.y);
    ctx.lineTo(node.x + node.width - 6, node.y);
    ctx.arcTo(node.x + node.width, node.y, node.x + node.width, node.y + 6, 6);
    ctx.lineTo(node.x + node.width, node.y + headerH);
    ctx.lineTo(node.x, node.y + headerH);
    ctx.lineTo(node.x, node.y + 6);
    ctx.arcTo(node.x, node.y, node.x + 6, node.y, 6);
    ctx.closePath();
    ctx.fill();

    // Header text
    ctx.fillStyle = '#ffffff';
    ctx.font = 'bold 11px sans-serif';
    ctx.textBaseline = 'middle';
    ctx.textAlign = 'center';
    const headerLabel = STAGE_LABELS[node.typeId] || node.def.DisplayName;
    const headerTextY = isStage ? node.y + headerH / 2 - 6 : node.y + headerH / 2;
    ctx.fillText(headerLabel, node.x + node.width / 2, headerTextY);

    // Subtitle for stage nodes
    if (isStage && STAGE_SUBTITLES[node.typeId]) {
        ctx.fillStyle = 'rgba(255,255,255,0.7)';
        ctx.font = 'italic 9px sans-serif';
        ctx.fillText(STAGE_SUBTITLES[node.typeId], node.x + node.width / 2, node.y + headerH / 2 + 7);
    }

    // Loop badge for ForEach / loop nodes
    if (LOOP_NODE_TYPE_IDS.includes(node.typeId)) {
        const badgeX = node.x + node.width - 20;
        const badgeY = node.y + headerH / 2;
        ctx.fillStyle = LOOP_BODY_WIRE_COLOR;
        ctx.font = 'bold 14px sans-serif';
        ctx.textAlign = 'center';
        ctx.textBaseline = 'middle';
        ctx.fillText('\u27F3', badgeX, badgeY);
    }

    // Show property values as subtitle on non-stage nodes that have definition properties
    if (!isStage && node.def.Properties && node.def.Properties.length > 0) {
        const overrides = node.propertyOverrides || {};
        const propSummary = node.def.Properties
            .map(p => overrides[p.Id] ?? p.DefaultValue ?? '')
            .filter(v => v !== '')
            .join(', ');
        if (propSummary) {
            ctx.fillStyle = 'rgba(255,255,255,0.55)';
            ctx.font = 'italic 9px sans-serif';
            ctx.textAlign = 'center';
            ctx.textBaseline = 'middle';
            ctx.fillText(propSummary, node.x + node.width / 2, node.y + headerH - 4);
        }
    }

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

    // Special styling for loop-body exec wires
    const isLoopBody = srcPin.DataType === 'Exec' && srcPin.Id === 'loop_body';

    // Detect implicit conversion (different data types on an allowed connection)
    const isImplicitConversion = srcPin.DataType !== tgtPin.DataType;

    const isHovered = hoveredEdge && hoveredEdge.id === edge.id;

    drawWire(
        srcNode.x + srcPin.nodeX, srcNode.y + srcPin.nodeY,
        tgtNode.x + tgtPin.nodeX, tgtNode.y + tgtPin.nodeY,
        srcPin.DataType || 'Exec',
        isHovered ? '#ffffff' : (isLoopBody ? LOOP_BODY_WIRE_COLOR : null),
        isHovered ? 3.5 : (isLoopBody ? 2.5 : null),
        isHovered ? null : (isImplicitConversion ? tgtPin.DataType : null)
    );
}

function drawWire(x1, y1, x2, y2, dataType, colorOverride, widthOverride, endDataType) {
    const startColor = colorOverride || DATA_TYPE_COLORS[dataType] || '#aaaaaa';
    const dx = Math.abs(x2 - x1) * 0.5;

    ctx.lineWidth = widthOverride || 2;

    // If there's a different end type (implicit conversion), draw a gradient wire
    if (endDataType && endDataType !== dataType && !colorOverride) {
        const endColor = DATA_TYPE_COLORS[endDataType] || '#aaaaaa';
        const gradient = ctx.createLinearGradient(x1, y1, x2, y2);
        gradient.addColorStop(0, startColor);
        gradient.addColorStop(1, endColor);
        ctx.strokeStyle = gradient;
    } else {
        ctx.strokeStyle = startColor;
    }

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

function isStageNode(node) {
    return node.typeId.startsWith('stage.');
}

function drawPipelineGuides() {
    const stageNodes = nodes
        .filter(n => STAGE_TYPE_IDS.includes(n.typeId))
        .sort((a, b) => STAGE_TYPE_IDS.indexOf(a.typeId) - STAGE_TYPE_IDS.indexOf(b.typeId));

    if (stageNodes.length < 2) return;

    // Calculate timeline Y — above the tallest stage node
    const minY = Math.min(...stageNodes.map(n => n.y));
    const timelineY = minY - 30;
    const leftX = stageNodes[0].x + stageNodes[0].width / 2;
    const rightX = stageNodes[stageNodes.length - 1].x + stageNodes[stageNodes.length - 1].width / 2;

    ctx.save();

    // Dotted connecting line (non-directional)
    ctx.strokeStyle = '#445566';
    ctx.lineWidth = 1.5;
    ctx.setLineDash([4, 4]);
    ctx.beginPath();
    ctx.moveTo(leftX, timelineY);
    ctx.lineTo(rightX, timelineY);
    ctx.stroke();
    ctx.setLineDash([]);

    // Stage marker circles
    for (const node of stageNodes) {
        const cx = node.x + node.width / 2;
        ctx.beginPath();
        ctx.arc(cx, timelineY, 5, 0, Math.PI * 2);
        ctx.fillStyle = STAGE_NODE_COLOR;
        ctx.strokeStyle = '#667788';
        ctx.lineWidth = 1.5;
        ctx.fill();
        ctx.stroke();
    }

    // "Pipeline Stages" label
    ctx.fillStyle = '#667788';
    ctx.font = 'italic 10px sans-serif';
    ctx.textAlign = 'center';
    ctx.textBaseline = 'bottom';
    ctx.fillText('Pipeline Stages (independent entry points)', (leftX + rightX) / 2, timelineY - 10);

    ctx.restore();
}

// ==================== Hit Testing ====================

/**
 * Evaluate a cubic bezier at parameter t.
 */
function bezierPoint(p0, p1, p2, p3, t) {
    const u = 1 - t;
    return u * u * u * p0 + 3 * u * u * t * p1 + 3 * u * t * t * p2 + t * t * t * p3;
}

/**
 * Find the closest edge to a world-space point by sampling along each bezier curve.
 * Returns { edge, dist } or null if nothing is within threshold.
 */
function hitTestEdge(wx, wy, threshold = 12) {
    let closestEdge = null;
    let closestDist = threshold;

    for (const edge of edges) {
        const srcNode = nodes.find(n => n.id === edge.srcNodeId);
        const tgtNode = nodes.find(n => n.id === edge.tgtNodeId);
        if (!srcNode || !tgtNode) continue;

        const srcPin = srcNode.outputPins.find(p => p.Id === edge.srcPinId);
        const tgtPin = tgtNode.inputPins.find(p => p.Id === edge.tgtPinId);
        if (!srcPin || !tgtPin) continue;

        const x1 = srcNode.x + srcPin.nodeX;
        const y1 = srcNode.y + srcPin.nodeY;
        const x2 = tgtNode.x + tgtPin.nodeX;
        const y2 = tgtNode.y + tgtPin.nodeY;
        const dx = Math.abs(x2 - x1) * 0.5;

        // Control points of the cubic bezier (matches drawWire)
        const cp1x = x1 + dx, cp1y = y1;
        const cp2x = x2 - dx, cp2y = y2;

        // Sample 20 points along the curve
        const SAMPLES = 20;
        let minD = Infinity;
        for (let i = 0; i <= SAMPLES; i++) {
            const t = i / SAMPLES;
            const bx = bezierPoint(x1, cp1x, cp2x, x2, t);
            const by = bezierPoint(y1, cp1y, cp2y, y2, t);
            const d = Math.hypot(wx - bx, wy - by);
            if (d < minD) minD = d;
        }

        if (minD < closestDist) {
            closestDist = minD;
            closestEdge = edge;
        }
    }

    return closestEdge ? { edge: closestEdge, dist: closestDist } : null;
}

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

    // Update hovered pin and hovered edge
    hoveredPin = hitTestPin(wp.x, wp.y);
    if (!hoveredPin && !connectingFrom) {
        const edgeHit = hitTestEdge(wp.x, wp.y);
        hoveredEdge = edgeHit ? edgeHit.edge : null;
    } else {
        hoveredEdge = null;
    }

    // Change cursor when hovering an edge
    if (canvas) {
        canvas.style.cursor = hoveredEdge ? 'pointer' : '';
    }

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

                const typesMatch = srcPin && tgtPin && (
                    srcPin.DataType === tgtPin.DataType ||
                    (tgtPin.DataType === 'String' && STRING_CONVERTIBLE_TYPES.has(srcPin.DataType)) ||
                    (srcPin.DataType === 'String' && STRING_CONVERTIBLE_TYPES.has(tgtPin.DataType))
                );
                if (typesMatch) {
                    // Remove existing connection to target pin if not multi-connect
                    if (!tgtPin.AllowMultipleConnections) {
                        edges = edges.filter(e => !(e.tgtNodeId === tgtNodeId && e.tgtPinId === tgtPinId));
                    }
                    // Data output pins always allow fan-out (one source → many targets).
                    // Only Exec outputs are restricted to a single outgoing connection.
                    if (srcPin.DataType === 'Exec' && !srcPin.AllowMultipleConnections) {
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
            // Prevent deletion of pipeline stage nodes
            const selNode = nodes.find(n => n.id === selectedNodeId);
            if (selNode && isStageNode(selNode)) return;

            // Remove node and its edges
            nodes = nodes.filter(n => n.id !== selectedNodeId);
            edges = edges.filter(e => e.srcNodeId !== selectedNodeId && e.tgtNodeId !== selectedNodeId);
            selectedNodeId = null;
            notifySelectionChanged();
        }
    }
}

// ==================== Context Menu ====================

/**
 * Show a floating context menu at the given screen position.
 * @param {number} clientX  Mouse clientX
 * @param {number} clientY  Mouse clientY
 * @param {{ label: string, icon?: string, danger?: boolean, action: () => void }[]} items
 */
function showContextMenu(clientX, clientY, items) {
    hideContextMenu();
    if (!items.length) return;

    const menu = document.createElement('div');
    menu.className = 'glyph-context-menu';
    Object.assign(menu.style, {
        position: 'fixed',
        zIndex: '10000',
        background: '#1e1e2e',
        border: '1px solid #3a3a5c',
        borderRadius: '6px',
        boxShadow: '0 4px 16px rgba(0,0,0,0.5)',
        padding: '4px 0',
        minWidth: '180px',
        fontFamily: 'system-ui, sans-serif',
        fontSize: '13px',
        color: '#d4d4e8',
        userSelect: 'none',
    });

    for (const item of items) {
        if (item === 'separator') {
            const sep = document.createElement('div');
            Object.assign(sep.style, {
                height: '1px',
                background: '#3a3a5c',
                margin: '4px 0',
            });
            menu.appendChild(sep);
            continue;
        }

        const row = document.createElement('div');
        Object.assign(row.style, {
            padding: '6px 14px',
            cursor: 'pointer',
            display: 'flex',
            alignItems: 'center',
            gap: '8px',
            color: item.danger ? '#e74c3c' : '#d4d4e8',
            transition: 'background 0.1s',
        });
        row.addEventListener('mouseenter', () => row.style.background = '#2a2a44');
        row.addEventListener('mouseleave', () => row.style.background = 'transparent');
        row.addEventListener('click', () => {
            hideContextMenu();
            item.action();
        });

        if (item.icon) {
            const iconSpan = document.createElement('span');
            iconSpan.textContent = item.icon;
            iconSpan.style.width = '16px';
            iconSpan.style.textAlign = 'center';
            row.appendChild(iconSpan);
        }

        const label = document.createElement('span');
        label.textContent = item.label;
        row.appendChild(label);

        menu.appendChild(row);
    }

    document.body.appendChild(menu);

    // Position: ensure it stays within viewport
    const menuRect = menu.getBoundingClientRect();
    let left = clientX;
    let top = clientY;
    if (left + menuRect.width > window.innerWidth) left = window.innerWidth - menuRect.width - 4;
    if (top + menuRect.height > window.innerHeight) top = window.innerHeight - menuRect.height - 4;
    menu.style.left = left + 'px';
    menu.style.top = top + 'px';

    contextMenuEl = menu;

    // Dismiss on click-away or Escape (next tick to avoid immediate dismiss)
    requestAnimationFrame(() => {
        const dismiss = (evt) => {
            if (!menu.contains(evt.target)) {
                hideContextMenu();
            }
        };
        const dismissKey = (evt) => {
            if (evt.key === 'Escape') hideContextMenu();
        };
        document.addEventListener('mousedown', dismiss, { once: true, capture: true });
        document.addEventListener('keydown', dismissKey, { once: true });
        // Store for cleanup
        menu._dismissHandlers = { dismiss, dismissKey };
    });
}

function hideContextMenu() {
    if (contextMenuEl) {
        if (contextMenuEl._dismissHandlers) {
            document.removeEventListener('mousedown', contextMenuEl._dismissHandlers.dismiss, { capture: true });
            document.removeEventListener('keydown', contextMenuEl._dismissHandlers.dismissKey);
        }
        contextMenuEl.remove();
        contextMenuEl = null;
    }
}

/**
 * Describe an edge's connection for display in the context menu.
 */
function describeEdge(edge) {
    const srcNode = nodes.find(n => n.id === edge.srcNodeId);
    const tgtNode = nodes.find(n => n.id === edge.tgtNodeId);
    const srcName = srcNode?.def?.DisplayName || 'Unknown';
    const tgtName = tgtNode?.def?.DisplayName || 'Unknown';
    const srcPin = srcNode?.outputPins.find(p => p.Id === edge.srcPinId);
    const tgtPin = tgtNode?.inputPins.find(p => p.Id === edge.tgtPinId);
    return `${srcName}.${srcPin?.Name || '?'} → ${tgtName}.${tgtPin?.Name || '?'}`;
}

function onContextMenu(e) {
    e.preventDefault();
    hideContextMenu();

    const rect = canvas.getBoundingClientRect();
    const sx = (e.clientX - rect.left) * (canvas.width / rect.width);
    const sy = (e.clientY - rect.top) * (canvas.height / rect.height);
    const wp = screenToWorld(sx, sy);

    // 1. Right-click on a node?
    const nodeHit = hitTestNode(wp.x, wp.y);
    if (nodeHit) {
        const items = [];

        // Select the node
        selectedNodeId = nodeHit.id;
        notifySelectionChanged();

        if (!isStageNode(nodeHit)) {
            items.push({
                label: 'Delete Node',
                icon: '🗑',
                danger: true,
                action: () => {
                    nodes = nodes.filter(n => n.id !== nodeHit.id);
                    edges = edges.filter(e => e.srcNodeId !== nodeHit.id && e.tgtNodeId !== nodeHit.id);
                    selectedNodeId = null;
                    notifySelectionChanged();
                },
            });
        }

        // Disconnect all edges on this node
        const nodeEdgeCount = edges.filter(e => e.srcNodeId === nodeHit.id || e.tgtNodeId === nodeHit.id).length;
        if (nodeEdgeCount > 0) {
            items.push({
                label: `Disconnect All (${nodeEdgeCount})`,
                icon: '⛓',
                action: () => {
                    edges = edges.filter(e => e.srcNodeId !== nodeHit.id && e.tgtNodeId !== nodeHit.id);
                },
            });
        }

        // Context Nodes — show if this node is a context provider (has matching getters in catalog)
        const hasContextNodes = catalog.some(d => d.ContextSourceTypeId === nodeHit.typeId);
        if (hasContextNodes && blazorRef) {
            items.push({
                label: 'Context Nodes',
                icon: '📌',
                action: () => {
                    blazorRef.invokeMethodAsync('OnShowContextNodes', nodeHit.typeId)
                        .catch(err => console.warn('[Glyph] Context nodes not supported on this page:', err.message));
                },
            });
        }

        if (items.length > 0) {
            showContextMenu(e.clientX, e.clientY, items);
        }
        return;
    }

    // 2. Right-click near an edge?
    const edgeHit = hitTestEdge(wp.x, wp.y);
    if (edgeHit) {
        const edge = edgeHit.edge;
        const desc = describeEdge(edge);

        showContextMenu(e.clientX, e.clientY, [
            {
                label: 'Remove Connection',
                icon: '✂',
                danger: true,
                action: () => {
                    edges = edges.filter(e => e.id !== edge.id);
                },
            },
            'separator',
            {
                label: desc,
                icon: '🔗',
                action: () => {}, // Info-only, no-op
            },
        ]);
        return;
    }

    // 3. Right-click on empty canvas — no menu (browser default is suppressed)
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

    // Build property definitions with current values for the Blazor property panel
    const properties = (node.def.Properties || []).map(prop => ({
        id: prop.Id,
        displayName: prop.DisplayName,
        defaultValue: prop.DefaultValue ?? '',
        allowedValues: prop.AllowedValues ?? [],
        currentValue: (node.propertyOverrides || {})[prop.Id] ?? prop.DefaultValue ?? '',
    }));

    return {
        nodeId: node.id,
        typeId: node.typeId,
        displayName: node.def.DisplayName,
        inputPins: inputPinData,
        properties: properties,
    };
}

function notifySelectionChanged() {
    if (blazorRef) {
        const data = getSelectedNodeData();
        blazorRef.invokeMethodAsync('OnNodeSelectionChanged', JSON.stringify(data));
    }
}

// ==================== Layout Helpers ====================

/**
 * Returns the bounding box of all nodes in world coordinates.
 */
function getNodesBoundingBox() {
    if (nodes.length === 0) return null;
    let minX = Infinity, minY = Infinity, maxX = -Infinity, maxY = -Infinity;
    for (const n of nodes) {
        if (n.x < minX) minX = n.x;
        if (n.y < minY) minY = n.y;
        if (n.x + n.width > maxX) maxX = n.x + n.width;
        if (n.y + n.height > maxY) maxY = n.y + n.height;
    }
    return { minX, minY, maxX, maxY, width: maxX - minX, height: maxY - minY };
}

/**
 * Checks whether node positions are excessively spread out relative to
 * what a compact layout would require.  Returns true if auto-arrange
 * should be triggered on load.
 */
function isGraphTooSpread() {
    if (nodes.length < 2) return false;
    const bb = getNodesBoundingBox();
    if (!bb) return false;

    // Total area that the nodes themselves occupy if tightly packed
    let totalNodeArea = 0;
    for (const n of nodes) {
        totalNodeArea += (n.width + 40) * (n.height + 20); // add small gap per node
    }

    const bbArea = bb.width * bb.height;
    // If bounding-box area is more than 6× the ideal packed area, it's too spread
    if (bbArea > totalNodeArea * 6) return true;

    // Also check if the bb is just absurdly wide/tall for the node count
    const idealColumns = Math.ceil(Math.sqrt(nodes.length));
    const idealWidth = idealColumns * (NODE_MIN_WIDTH + 60);
    const idealHeight = idealColumns * (80 + 30);
    if (bb.width > idealWidth * 4 || bb.height > idealHeight * 4) return true;

    return false;
}

/**
 * Adjusts camera and zoom so that all nodes fit within the viewport with padding.
 */
function fitToView() {
    if (!canvas || nodes.length === 0) return;
    const bb = getNodesBoundingBox();
    if (!bb) return;

    const padding = 80; // px padding on each side
    const cw = canvas.width - padding * 2;
    const ch = canvas.height - padding * 2;
    if (cw <= 0 || ch <= 0) return;

    const scaleX = cw / bb.width;
    const scaleY = ch / bb.height;
    zoom = Math.min(scaleX, scaleY, ZOOM_MAX);
    zoom = Math.max(zoom, ZOOM_MIN);

    // If all nodes fit comfortably, don't zoom past 1.0
    if (zoom > 1.0) zoom = 1.0;

    const centerX = (bb.minX + bb.maxX) / 2;
    const centerY = (bb.minY + bb.maxY) / 2;
    camX = -centerX * zoom;
    camY = -centerY * zoom;
}

/**
 * Auto-arranges nodes using a Sugiyama-style layered layout via dagre.
 *
 * Uses the dagre library (loaded globally from App.razor) which implements
 * the full Sugiyama algorithm: rank assignment → crossing minimisation →
 * coordinate assignment. This produces clean left-to-right layouts with
 * minimal wire crossings and straight execution spines.
 *
 * Exec-flow edges are given higher weight so the main execution chain
 * stays as straight as possible; data wires are allowed to bend more.
 */
function autoArrange() {
    if (nodes.length === 0) return;

    // Guard: dagre must be loaded globally (via <script> in App.razor)
    if (typeof dagre === 'undefined') {
        console.warn('[Glyph] dagre is not loaded — falling back to no-op');
        return;
    }

    // --- Build the dagre graph ---
    const g = new dagre.graphlib.Graph({ directed: true, multigraph: true });

    g.setGraph({
        rankdir:  'LR',              // left-to-right flow
        nodesep:  20,                 // vertical gap between nodes in same rank
        ranksep:  60,                 // horizontal gap between ranks
        edgesep:  10,                 // gap between edges
        marginx:  20,
        marginy:  20,
        acyclicer: 'greedy',          // break cycles (loop-back wires)
        ranker:   'network-simplex'   // best-quality rank assignment
    });

    g.setDefaultEdgeLabel(() => ({}));

    // Register every node with its measured dimensions
    for (const n of nodes) {
        g.setNode(n.id, { width: n.width, height: n.height });
    }

    // Register edges. Use multigraph name = edge id so multiple wires
    // between the same pair of nodes are handled correctly.
    // Give exec-flow edges higher weight to keep the execution spine straight.
    for (const e of edges) {
        const srcNode = nodes.find(n => n.id === e.srcNodeId);
        const srcPin  = srcNode?.outputPins.find(p => p.Id === e.srcPinId);
        const isExec  = srcPin?.DataType === 'Exec';

        g.setEdge(e.srcNodeId, e.tgtNodeId, {
            weight: isExec ? 2 : 1,
            minlen: 1
        }, e.id);
    }

    // --- Run layout ---
    dagre.layout(g);

    // --- Apply positions back to our nodes ---
    // dagre returns CENTER coordinates; we store top-left, so offset.
    // Snap to GRID_SIZE for visual consistency.
    for (const n of nodes) {
        const laid = g.node(n.id);
        if (!laid) continue;
        n.x = Math.round((laid.x - laid.width  / 2) / GRID_SIZE) * GRID_SIZE;
        n.y = Math.round((laid.y - laid.height / 2) / GRID_SIZE) * GRID_SIZE;
    }

    fitToView();
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

// ==================== Drag & Drop from Palette ====================

function onDragOver(e) {
    if (e.dataTransfer.types.includes('application/glyph-node')) {
        e.preventDefault(); // Allow the drop
        e.dataTransfer.dropEffect = 'copy';
    }
}

function onDrop(e) {
    e.preventDefault();
    const typeId = e.dataTransfer.getData('application/glyph-node');
    if (!typeId) return;

    const def = catalog.find(d => d.TypeId === typeId);
    if (!def) return;

    // Singleton check
    if (def.IsSingleton && nodes.some(n => n.typeId === def.TypeId)) return;

    // Convert drop position (relative to canvas) to world coordinates
    const rect = canvas.getBoundingClientRect();
    const sx = e.clientX - rect.left;
    const sy = e.clientY - rect.top;
    const wp = screenToWorld(sx, sy);

    const node = createNodeInstance(def, wp.x, wp.y);
    nodes.push(node);
    selectedNodeId = node.id;
    notifySelectionChanged();
}

// ==================== Public API ==

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

    // If nodes are excessively spread out, auto-arrange to compact positions
    if (nodes.length > 0) {
        if (isGraphTooSpread()) {
            autoArrange(); // also calls fitToView internally
        } else {
            fitToView();
        }
    }

    // Event listeners
    canvas.addEventListener('mousedown', onMouseDown);
    canvas.addEventListener('mousemove', onMouseMove);
    canvas.addEventListener('mouseup', onMouseUp);
    canvas.addEventListener('wheel', onWheel, { passive: false });
    canvas.addEventListener('keydown', onKeyDown);
    canvas.addEventListener('contextmenu', onContextMenu);

    // Drag-and-drop from palette
    canvas.addEventListener('dragover', onDragOver);
    canvas.addEventListener('drop', onDrop);

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

    // Auto-layout: place near selected node or find a compact spot.
    let placeX, placeY;
    const hGap = 60;
    const vGap = 20;
    const selectedNode = selectedNodeId ? nodes.find(n => n.id === selectedNodeId) : null;

    if (selectedNode) {
        // Place right of selected node at same Y
        placeX = selectedNode.x + selectedNode.width + hGap;
        placeY = selectedNode.y;
    } else if (nodes.length > 0) {
        // Find a compact position: below the lowest node in the rightmost column,
        // or start a new column if current column is tall.
        const bb = getNodesBoundingBox();
        const columnThreshold = 500; // max column height before starting new column

        // Find nodes in the rightmost column (within one node-width of maxX)
        const rightEdge = bb.maxX;
        const columnNodes = nodes.filter(n => n.x + n.width >= rightEdge - NODE_MIN_WIDTH);
        const columnBottom = Math.max(...columnNodes.map(n => n.y + n.height));

        if (columnBottom - bb.minY > columnThreshold) {
            // Column is tall — start a new column to the right
            placeX = rightEdge + hGap;
            placeY = bb.minY;
        } else {
            // Add below the last node in the rightmost column
            placeX = columnNodes[0]?.x ?? bb.maxX;
            placeY = columnBottom + vGap;
        }
    } else {
        // Empty graph — place at center of viewport
        const w = canvas ? canvas.width : 800;
        const h = canvas ? canvas.height : 600;
        const wp = screenToWorld(w / 2, h / 2);
        placeX = wp.x;
        placeY = wp.y;
    }

    const node = createNodeInstance(def, placeX, placeY);
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

export function fitAll() {
    fitToView();
}

export function arrangeNodes() {
    autoArrange();
}

/**
 * Resize the canvas to an explicit width and height.
 * Called by the Golden Layout bridge when the canvas panel is resized,
 * bypassing the ResizeObserver for deterministic sizing.
 * @param {number} w  Width in pixels
 * @param {number} h  Height in pixels
 */
export function resizeCanvasToSize(w, h) {
    if (!canvas) return;
    canvas.width = w;
    canvas.height = h;
}

export function destroy() {
    hideContextMenu();

    if (canvas) {
        canvas.removeEventListener('mousedown', onMouseDown);
        canvas.removeEventListener('mousemove', onMouseMove);
        canvas.removeEventListener('mouseup', onMouseUp);
        canvas.removeEventListener('wheel', onWheel);
        canvas.removeEventListener('keydown', onKeyDown);
        canvas.removeEventListener('contextmenu', onContextMenu);
        canvas.removeEventListener('dragover', onDragOver);
        canvas.removeEventListener('drop', onDrop);
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
    hoveredEdge = null;
}
