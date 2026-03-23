/**
 * statemachine-editor.js — Canvas2D finite-state-machine editor for quest StateMachine objectives.
 *
 * States      = rounded rectangles (green border = initial, double-ring = terminal)
 * Transitions = curved arrows with "signalType : targetTag" labels
 *
 * Pattern: IIFE → window.stateMachineEditor (consistent with clueGraphEditor)
 * Blazor interop via DotNetObjectReference.
 */
window.stateMachineEditor = (function () {
    "use strict";

    /* ── Constants ────────────────────────────────────────── */
    const GRID_SIZE       = 20;
    const STATE_W         = 140;
    const STATE_H         = 50;
    const STATE_R         = 10; // corner radius
    const ARROW_SIZE      = 8;
    const HIT_TOLERANCE   = 8;
    const MIN_ZOOM        = 0.25;
    const MAX_ZOOM        = 4;

    /* ── Colours ─────────────────────────────────────────── */
    const CLR_BG          = "#1e1e2e";
    const CLR_GRID        = "#2a2a3a";
    const CLR_STATE_FILL  = "#2d3748";
    const CLR_STATE_TEXT  = "#e2e8f0";
    const CLR_BORDER      = "#4a5568";
    const CLR_INITIAL     = "#48bb78"; // green
    const CLR_TERM_SUCC   = "#48bb78";
    const CLR_TERM_FAIL   = "#fc8181"; // red
    const CLR_TRANSITION  = "#a0aec0";
    const CLR_SELECTED    = "#ff6060";
    const CLR_HOVER       = "#63b3ed";
    const CLR_LABEL_BG    = "rgba(30,30,46,0.85)";

    /* ── Private state ───────────────────────────────────── */
    let canvas = null, ctx = null;
    let dotNetRef = null;
    let _containerId = null;
    let _animId = null;

    let states = [];       // { id, label, description, x, y, isInitial, isTerminalSuccess, isTerminalFailure }
    let transitions = [];  // { id, fromId, toId, signalType, targetTag }
    let _nextStateId = 1;
    let _nextTransId = 1;

    // Camera
    let camX = 0, camY = 0, zoom = 1;

    // Interaction
    let selectedStateId = null;
    let selectedTransId = null;
    let dragState = null;   // { stateId, offX, offY }
    let panDrag = null;     // { startCamX, startCamY, startMX, startMY }
    let connectDrag = null; // { fromId, curX, curY }
    let hoveredStateId = null;

    /* ── Initialise ──────────────────────────────────────── */
    function init(containerId, graphJson, blazorRef) {
        destroy();

        _containerId = containerId;
        dotNetRef = blazorRef;

        const container = document.getElementById(containerId);
        if (!container) {
            console.error("[stateMachineEditor] container not found:", containerId);
            return;
        }

        canvas = document.createElement("canvas");
        canvas.style.width = "100%";
        canvas.style.height = "100%";
        canvas.style.display = "block";
        container.innerHTML = "";
        container.appendChild(canvas);
        ctx = canvas.getContext("2d");

        _resizeCanvas();
        _resizeObserver = new ResizeObserver(() => _resizeCanvas());
        _resizeObserver.observe(container);

        // Parse graph
        let data = { states: [], transitions: [] };
        try {
            if (graphJson) data = JSON.parse(graphJson);
        } catch (e) {
            console.warn("[stateMachineEditor] bad graphJson, starting empty", e);
        }

        states = (data.states || []).map((s) => ({
            id: String(s.id),
            label: s.label || s.id,
            description: s.description || "",
            x: s.x || 200,
            y: s.y || 200,
            isInitial: !!s.isInitial,
            isTerminalSuccess: !!s.isTerminalSuccess,
            isTerminalFailure: !!s.isTerminalFailure,
        }));
        transitions = (data.transitions || []).map((t) => ({
            id: String(t.id),
            fromId: String(t.fromId),
            toId: String(t.toId),
            signalType: t.signalType || "custom",
            targetTag: t.targetTag || "",
        }));

        _nextStateId = 1;
        states.forEach((s) => {
            const n = parseInt(s.id, 10) || 0;
            if (n >= _nextStateId) _nextStateId = n + 1;
        });
        _nextTransId = 1;
        transitions.forEach((t) => {
            const n = parseInt(t.id, 10) || 0;
            if (n >= _nextTransId) _nextTransId = n + 1;
        });

        // Events
        canvas.addEventListener("mousedown", _onMouseDown);
        canvas.addEventListener("mousemove", _onMouseMove);
        canvas.addEventListener("mouseup", _onMouseUp);
        canvas.addEventListener("wheel", _onWheel, { passive: false });
        canvas.addEventListener("dblclick", _onDblClick);
        canvas.addEventListener("contextmenu", _onContextMenu);
        document.addEventListener("keydown", _onKeyDown);

        // Start render loop
        _render();
    }

    let _resizeObserver = null;

    function _resizeCanvas() {
        if (!canvas) return;
        const rect = canvas.parentElement.getBoundingClientRect();
        canvas.width = rect.width * (window.devicePixelRatio || 1);
        canvas.height = rect.height * (window.devicePixelRatio || 1);
        canvas.style.width = rect.width + "px";
        canvas.style.height = rect.height + "px";
    }

    /* ── Render loop ─────────────────────────────────────── */
    function _render() {
        if (!canvas || !ctx) return;
        const dpr = window.devicePixelRatio || 1;
        const w = canvas.width, h = canvas.height;

        ctx.setTransform(1, 0, 0, 1, 0, 0);
        ctx.clearRect(0, 0, w, h);

        // Background
        ctx.fillStyle = CLR_BG;
        ctx.fillRect(0, 0, w, h);

        // Apply camera
        ctx.setTransform(zoom * dpr, 0, 0, zoom * dpr, (-camX * zoom + w / 2), (-camY * zoom + h / 2));

        _drawGrid(w, h, dpr);
        _drawTransitions();
        _drawConnectDrag();
        _drawStates();

        _animId = requestAnimationFrame(_render);
    }

    function _drawGrid(w, h, dpr) {
        const step = GRID_SIZE;
        const halfW = (w / 2) / (zoom * dpr);
        const halfH = (h / 2) / (zoom * dpr);
        const left = camX - halfW;
        const top = camY - halfH;
        const right = camX + halfW;
        const bottom = camY + halfH;

        ctx.strokeStyle = CLR_GRID;
        ctx.lineWidth = 0.5 / zoom;
        ctx.beginPath();
        for (let x = Math.floor(left / step) * step; x <= right; x += step) {
            ctx.moveTo(x, top);
            ctx.lineTo(x, bottom);
        }
        for (let y = Math.floor(top / step) * step; y <= bottom; y += step) {
            ctx.moveTo(left, y);
            ctx.lineTo(right, y);
        }
        ctx.stroke();
    }

    function _drawStates() {
        states.forEach((s) => {
            const x = s.x - STATE_W / 2;
            const y = s.y - STATE_H / 2;
            const selected = s.id === selectedStateId;
            const hovered = s.id === hoveredStateId;

            // Outer ring for terminal states
            if (s.isTerminalSuccess || s.isTerminalFailure) {
                ctx.strokeStyle = s.isTerminalSuccess ? CLR_TERM_SUCC : CLR_TERM_FAIL;
                ctx.lineWidth = 2 / zoom;
                _roundRect(x - 4, y - 4, STATE_W + 8, STATE_H + 8, STATE_R + 2);
                ctx.stroke();
            }

            // Fill
            ctx.fillStyle = CLR_STATE_FILL;
            _roundRect(x, y, STATE_W, STATE_H, STATE_R);
            ctx.fill();

            // Border
            let borderClr = CLR_BORDER;
            let borderW = 1.5 / zoom;
            if (selected) { borderClr = CLR_SELECTED; borderW = 2.5 / zoom; }
            else if (hovered) { borderClr = CLR_HOVER; borderW = 2 / zoom; }
            else if (s.isInitial) { borderClr = CLR_INITIAL; borderW = 2 / zoom; }
            ctx.strokeStyle = borderClr;
            ctx.lineWidth = borderW;
            _roundRect(x, y, STATE_W, STATE_H, STATE_R);
            ctx.stroke();

            // Initial indicator (small filled circle at top-left)
            if (s.isInitial) {
                ctx.fillStyle = CLR_INITIAL;
                ctx.beginPath();
                ctx.arc(x + 10, y + 10, 4, 0, Math.PI * 2);
                ctx.fill();
            }

            // Label
            ctx.fillStyle = CLR_STATE_TEXT;
            ctx.font = (12 / zoom >= 6 ? 12 : 12 / zoom * zoom) + "px sans-serif";
            ctx.textAlign = "center";
            ctx.textBaseline = "middle";
            const displayLabel = s.label.length > 18 ? s.label.substring(0, 16) + "…" : s.label;
            ctx.fillText(displayLabel, s.x, s.y);
        });
    }

    function _drawTransitions() {
        transitions.forEach((t) => {
            const from = states.find((s) => s.id === t.fromId);
            const to = states.find((s) => s.id === t.toId);
            if (!from || !to) return;

            const selected = t.id === selectedTransId;
            const sameDir = transitions.filter((tt) =>
                (tt.fromId === t.fromId && tt.toId === t.toId) ||
                (tt.fromId === t.toId && tt.toId === t.fromId)
            );
            const idx = sameDir.indexOf(t);
            const curvature = sameDir.length > 1 ? (idx - (sameDir.length - 1) / 2) * 40 : 0;

            // Self-loop
            if (t.fromId === t.toId) {
                _drawSelfLoop(from, t, selected);
                return;
            }

            // Compute bezier control point
            const mx = (from.x + to.x) / 2;
            const my = (from.y + to.y) / 2;
            const dx = to.x - from.x;
            const dy = to.y - from.y;
            const len = Math.sqrt(dx * dx + dy * dy) || 1;
            const nx = -dy / len;
            const ny = dx / len;
            const cpx = mx + nx * curvature;
            const cpy = my + ny * curvature;

            // Edge clipping to state rect boundary
            const startPt = _rectEdgePoint(from.x, from.y, STATE_W, STATE_H, cpx, cpy);
            const endPt = _rectEdgePoint(to.x, to.y, STATE_W, STATE_H, cpx, cpy);

            ctx.strokeStyle = selected ? CLR_SELECTED : CLR_TRANSITION;
            ctx.lineWidth = (selected ? 2.5 : 1.5) / zoom;
            ctx.beginPath();
            ctx.moveTo(startPt.x, startPt.y);
            ctx.quadraticCurveTo(cpx, cpy, endPt.x, endPt.y);
            ctx.stroke();

            // Arrow at end
            _drawArrowHead(cpx, cpy, endPt.x, endPt.y, selected);

            // Label
            const labelX = mx + nx * curvature * 0.6;
            const labelY = my + ny * curvature * 0.6 - 8 / zoom;
            const label = t.signalType + (t.targetTag ? " : " + t.targetTag : "");
            _drawEdgeLabel(labelX, labelY, label, selected);
        });
    }

    function _drawSelfLoop(state, trans, selected) {
        const cx = state.x;
        const cy = state.y - STATE_H / 2 - 25;
        const r = 18;

        ctx.strokeStyle = selected ? CLR_SELECTED : CLR_TRANSITION;
        ctx.lineWidth = (selected ? 2.5 : 1.5) / zoom;
        ctx.beginPath();
        ctx.arc(cx, cy, r, 0.3, Math.PI * 2 - 0.3);
        ctx.stroke();

        // Arrow
        const ax = cx + r * Math.cos(Math.PI * 2 - 0.3);
        const ay = cy + r * Math.sin(Math.PI * 2 - 0.3);
        _drawArrowHead(cx, cy - r, ax, ay, selected);

        const label = trans.signalType + (trans.targetTag ? " : " + trans.targetTag : "");
        _drawEdgeLabel(cx, cy - r - 10, label, selected);
    }

    function _drawArrowHead(fromX, fromY, toX, toY, selected) {
        const angle = Math.atan2(toY - fromY, toX - fromX);
        const sz = ARROW_SIZE / zoom;
        ctx.fillStyle = selected ? CLR_SELECTED : CLR_TRANSITION;
        ctx.beginPath();
        ctx.moveTo(toX, toY);
        ctx.lineTo(toX - sz * Math.cos(angle - 0.4), toY - sz * Math.sin(angle - 0.4));
        ctx.lineTo(toX - sz * Math.cos(angle + 0.4), toY - sz * Math.sin(angle + 0.4));
        ctx.closePath();
        ctx.fill();
    }

    function _drawEdgeLabel(x, y, text, selected) {
        if (text.length === 0) return;
        const fontSize = Math.max(9, 10);
        ctx.font = fontSize + "px sans-serif";
        ctx.textAlign = "center";
        ctx.textBaseline = "middle";
        const tm = ctx.measureText(text);
        const pad = 3;
        ctx.fillStyle = CLR_LABEL_BG;
        ctx.fillRect(x - tm.width / 2 - pad, y - fontSize / 2 - pad, tm.width + pad * 2, fontSize + pad * 2);
        ctx.fillStyle = selected ? CLR_SELECTED : CLR_STATE_TEXT;
        ctx.fillText(text, x, y);
    }

    function _drawConnectDrag() {
        if (!connectDrag) return;
        const from = states.find((s) => s.id === connectDrag.fromId);
        if (!from) return;
        ctx.strokeStyle = CLR_HOVER;
        ctx.lineWidth = 2 / zoom;
        ctx.setLineDash([6 / zoom, 4 / zoom]);
        ctx.beginPath();
        ctx.moveTo(from.x, from.y);
        ctx.lineTo(connectDrag.curX, connectDrag.curY);
        ctx.stroke();
        ctx.setLineDash([]);
    }

    /* ── Geometry helpers ────────────────────────────────── */
    function _roundRect(x, y, w, h, r) {
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

    function _rectEdgePoint(cx, cy, w, h, targetX, targetY) {
        const dx = targetX - cx;
        const dy = targetY - cy;
        const hw = w / 2, hh = h / 2;
        if (dx === 0 && dy === 0) return { x: cx, y: cy };
        const absDx = Math.abs(dx), absDy = Math.abs(dy);
        let scale;
        if (absDx / hw > absDy / hh) {
            scale = hw / absDx;
        } else {
            scale = hh / absDy;
        }
        return { x: cx + dx * scale, y: cy + dy * scale };
    }

    function _screenToWorld(sx, sy) {
        const dpr = window.devicePixelRatio || 1;
        const w = canvas.width, h = canvas.height;
        const wx = (sx * dpr - w / 2) / (zoom * dpr) + camX;
        const wy = (sy * dpr - h / 2) / (zoom * dpr) + camY;
        return { x: wx, y: wy };
    }

    function _hitTestState(wx, wy) {
        for (let i = states.length - 1; i >= 0; i--) {
            const s = states[i];
            if (Math.abs(wx - s.x) <= STATE_W / 2 && Math.abs(wy - s.y) <= STATE_H / 2) {
                return s;
            }
        }
        return null;
    }

    function _hitTestTransition(wx, wy) {
        const tol = HIT_TOLERANCE / zoom;
        for (let i = transitions.length - 1; i >= 0; i--) {
            const t = transitions[i];
            const from = states.find((s) => s.id === t.fromId);
            const to = states.find((s) => s.id === t.toId);
            if (!from || !to) continue;
            // Sample the quadratic bezier at 20 points
            const mx = (from.x + to.x) / 2;
            const my = (from.y + to.y) / 2;
            const dx = to.x - from.x;
            const dy = to.y - from.y;
            const len = Math.sqrt(dx * dx + dy * dy) || 1;
            const nx = -dy / len, ny = dx / len;
            const sameDir = transitions.filter((tt) => (tt.fromId===t.fromId&&tt.toId===t.toId)||(tt.fromId===t.toId&&tt.toId===t.fromId));
            const idx = sameDir.indexOf(t);
            const curv = sameDir.length > 1 ? (idx - (sameDir.length - 1) / 2) * 40 : 0;
            const cpx = mx + nx * curv, cpy = my + ny * curv;
            for (let j = 0; j <= 20; j++) {
                const tt = j / 20;
                const bx = (1 - tt) * (1 - tt) * from.x + 2 * (1 - tt) * tt * cpx + tt * tt * to.x;
                const by = (1 - tt) * (1 - tt) * from.y + 2 * (1 - tt) * tt * cpy + tt * tt * to.y;
                if (Math.abs(bx - wx) < tol && Math.abs(by - wy) < tol) return t;
            }
        }
        return null;
    }

    /* ── Event Handlers ──────────────────────────────────── */
    function _onMouseDown(e) {
        const rect = canvas.getBoundingClientRect();
        const sx = e.clientX - rect.left;
        const sy = e.clientY - rect.top;
        const world = _screenToWorld(sx, sy);

        if (e.button === 2) return; // context menu

        const state = _hitTestState(world.x, world.y);

        // Shift-drag from state = connect mode
        if (state && e.shiftKey) {
            connectDrag = { fromId: state.id, curX: world.x, curY: world.y };
            return;
        }

        if (state) {
            selectedStateId = state.id;
            selectedTransId = null;
            dragState = { stateId: state.id, offX: world.x - state.x, offY: world.y - state.y };
            _showStateSidebar(state);
            _invoke("OnStateSelected", JSON.stringify(state));
            return;
        }

        const trans = _hitTestTransition(world.x, world.y);
        if (trans) {
            selectedTransId = trans.id;
            selectedStateId = null;
            _showTransSidebar(trans);
            _invoke("OnTransitionSelected", JSON.stringify(trans));
            return;
        }

        // Pan
        selectedStateId = null;
        selectedTransId = null;
        _hideAllSidebars();
        panDrag = { startCamX: camX, startCamY: camY, startMX: sx, startMY: sy };
        _invoke("OnSelectionCleared", "");
    }

    function _onMouseMove(e) {
        const rect = canvas.getBoundingClientRect();
        const sx = e.clientX - rect.left;
        const sy = e.clientY - rect.top;
        const world = _screenToWorld(sx, sy);

        if (connectDrag) {
            connectDrag.curX = world.x;
            connectDrag.curY = world.y;
            return;
        }

        if (dragState) {
            const s = states.find((st) => st.id === dragState.stateId);
            if (s) {
                s.x = world.x - dragState.offX;
                s.y = world.y - dragState.offY;
            }
            return;
        }

        if (panDrag) {
            const dpr = window.devicePixelRatio || 1;
            camX = panDrag.startCamX - (sx - panDrag.startMX) / zoom;
            camY = panDrag.startCamY - (sy - panDrag.startMY) / zoom;
            return;
        }

        // Hover
        const hit = _hitTestState(world.x, world.y);
        hoveredStateId = hit ? hit.id : null;
        canvas.style.cursor = hit ? "pointer" : (e.shiftKey ? "crosshair" : "default");
    }

    function _onMouseUp(e) {
        if (connectDrag) {
            const rect = canvas.getBoundingClientRect();
            const sx = e.clientX - rect.left;
            const sy = e.clientY - rect.top;
            const world = _screenToWorld(sx, sy);
            const target = _hitTestState(world.x, world.y);
            if (target && target.id !== connectDrag.fromId) {
                addTransition(connectDrag.fromId, target.id, "custom", "");
            }
            connectDrag = null;
            return;
        }

        if (dragState) {
            _notifyGraphChanged();
            dragState = null;
        }
        panDrag = null;
    }

    function _onWheel(e) {
        e.preventDefault();
        const delta = e.deltaY > 0 ? 0.9 : 1.1;
        zoom = Math.min(MAX_ZOOM, Math.max(MIN_ZOOM, zoom * delta));
    }

    function _onDblClick(e) {
        const rect = canvas.getBoundingClientRect();
        const sx = e.clientX - rect.left;
        const sy = e.clientY - rect.top;
        const world = _screenToWorld(sx, sy);
        const hit = _hitTestState(world.x, world.y);
        if (!hit) {
            addState("State " + _nextStateId, "", world);
        }
    }

    function _onContextMenu(e) {
        e.preventDefault();
        const rect = canvas.getBoundingClientRect();
        const sx = e.clientX - rect.left;
        const sy = e.clientY - rect.top;
        const world = _screenToWorld(sx, sy);
        const hit = _hitTestState(world.x, world.y);
        if (hit) {
            _showContextMenu(e.clientX, e.clientY, hit);
        }
    }

    function _onKeyDown(e) {
        if (e.key === "Delete" || e.key === "Backspace") {
            if (document.activeElement && (document.activeElement.tagName === "INPUT" || document.activeElement.tagName === "TEXTAREA" || document.activeElement.tagName === "SELECT")) return;
            removeSelected();
        }
    }

    /* ── Context menu ────────────────────────────────────── */
    let _ctxMenu = null;

    function _showContextMenu(clientX, clientY, state) {
        _removeContextMenu();
        _ctxMenu = document.createElement("div");
        _ctxMenu.style.cssText = "position:fixed;z-index:2000;background:#2d3748;border:1px solid #4a5568;border-radius:4px;padding:4px 0;min-width:160px;font-size:12px;color:#e2e8f0;box-shadow:0 4px 12px rgba(0,0,0,0.5)";
        _ctxMenu.style.left = clientX + "px";
        _ctxMenu.style.top = clientY + "px";

        const items = [
            { label: state.isInitial ? "✓ Initial State" : "  Set Initial", action: () => { setInitial(state.id); } },
            { label: state.isTerminalSuccess ? "✓ Terminal Success" : "  Set Terminal Success", action: () => { toggleTerminalSuccess(state.id); } },
            { label: state.isTerminalFailure ? "✓ Terminal Failure" : "  Set Terminal Failure", action: () => { toggleTerminalFailure(state.id); } },
            { label: "──────────", action: null },
            { label: "  Delete State", action: () => { selectedStateId = state.id; removeSelected(); } },
        ];

        items.forEach((item) => {
            const div = document.createElement("div");
            div.textContent = item.label;
            if (item.action) {
                div.style.cssText = "padding:4px 12px;cursor:pointer;";
                div.onmouseenter = () => div.style.background = "#4a5568";
                div.onmouseleave = () => div.style.background = "";
                div.onclick = () => { item.action(); _removeContextMenu(); };
            } else {
                div.style.cssText = "padding:2px 12px;color:#4a5568;font-size:10px;";
            }
            _ctxMenu.appendChild(div);
        });

        document.body.appendChild(_ctxMenu);
        const dismiss = (ev) => {
            if (!_ctxMenu?.contains(ev.target)) {
                _removeContextMenu();
                document.removeEventListener("mousedown", dismiss);
            }
        };
        setTimeout(() => document.addEventListener("mousedown", dismiss), 0);
    }

    function _removeContextMenu() {
        if (_ctxMenu && _ctxMenu.parentElement) {
            _ctxMenu.parentElement.removeChild(_ctxMenu);
        }
        _ctxMenu = null;
    }

    /* ── Public API ──────────────────────────────────────── */
    function addState(label, description, position) {
        const id = String(_nextStateId++);
        const pos = position || { x: 200 + Math.random() * 200, y: 200 + Math.random() * 200 };
        states.push({
            id: id,
            label: label || "State " + id,
            description: description || "",
            x: pos.x,
            y: pos.y,
            isInitial: states.length === 0, // first state is initial by default
            isTerminalSuccess: false,
            isTerminalFailure: false,
        });
        _notifyGraphChanged();
        return id;
    }

    function addTransition(fromId, toId, signalType, targetTag) {
        // Prevent duplicate
        const exists = transitions.find((t) => t.fromId === fromId && t.toId === toId && t.signalType === signalType && t.targetTag === targetTag);
        if (exists) return exists.id;
        const id = String(_nextTransId++);
        transitions.push({
            id: id,
            fromId: fromId,
            toId: toId,
            signalType: signalType || "custom",
            targetTag: targetTag || "",
        });
        _notifyGraphChanged();
        return id;
    }

    function removeSelected() {
        if (selectedStateId) {
            transitions = transitions.filter((t) => t.fromId !== selectedStateId && t.toId !== selectedStateId);
            states = states.filter((s) => s.id !== selectedStateId);
            selectedStateId = null;
            _hideAllSidebars();
            _notifyGraphChanged();
        } else if (selectedTransId) {
            transitions = transitions.filter((t) => t.id !== selectedTransId);
            selectedTransId = null;
            _hideAllSidebars();
            _notifyGraphChanged();
        }
    }

    function setInitial(stateId) {
        states.forEach((s) => s.isInitial = (s.id === stateId));
        _notifyGraphChanged();
    }

    function toggleTerminalSuccess(stateId) {
        const s = states.find((st) => st.id === stateId);
        if (s) {
            s.isTerminalSuccess = !s.isTerminalSuccess;
            if (s.isTerminalSuccess) s.isTerminalFailure = false;
            _notifyGraphChanged();
        }
    }

    function toggleTerminalFailure(stateId) {
        const s = states.find((st) => st.id === stateId);
        if (s) {
            s.isTerminalFailure = !s.isTerminalFailure;
            if (s.isTerminalFailure) s.isTerminalSuccess = false;
            _notifyGraphChanged();
        }
    }

    function fitView() {
        if (states.length === 0) return;
        let minX = Infinity, minY = Infinity, maxX = -Infinity, maxY = -Infinity;
        states.forEach((s) => {
            minX = Math.min(minX, s.x - STATE_W / 2);
            minY = Math.min(minY, s.y - STATE_H / 2);
            maxX = Math.max(maxX, s.x + STATE_W / 2);
            maxY = Math.max(maxY, s.y + STATE_H / 2);
        });
        const cw = canvas.width / (window.devicePixelRatio || 1);
        const ch = canvas.height / (window.devicePixelRatio || 1);
        const pad = 40;
        const gw = maxX - minX + pad * 2;
        const gh = maxY - minY + pad * 2;
        zoom = Math.min(cw / gw, ch / gh, 2);
        camX = (minX + maxX) / 2;
        camY = (minY + maxY) / 2;
    }

    function validate() {
        const errors = [];
        if (states.length === 0) errors.push("State machine has no states.");
        const initials = states.filter((s) => s.isInitial);
        if (initials.length === 0) errors.push("No initial state set.");
        if (initials.length > 1) errors.push("Multiple initial states set.");
        const terminals = states.filter((s) => s.isTerminalSuccess || s.isTerminalFailure);
        if (terminals.length === 0) errors.push("No terminal state set.");
        // Check for unreachable states
        if (initials.length === 1) {
            const reachable = new Set();
            const queue = [initials[0].id];
            reachable.add(initials[0].id);
            while (queue.length > 0) {
                const cur = queue.shift();
                transitions.filter((t) => t.fromId === cur).forEach((t) => {
                    if (!reachable.has(t.toId)) {
                        reachable.add(t.toId);
                        queue.push(t.toId);
                    }
                });
            }
            states.forEach((s) => {
                if (!reachable.has(s.id)) errors.push("State '" + s.label + "' is unreachable from initial state.");
            });
        }
        return JSON.stringify({ valid: errors.length === 0, errors: errors });
    }

    function getGraphJson() {
        return JSON.stringify({
            states: states.map((s) => ({
                id: s.id,
                label: s.label,
                description: s.description,
                x: s.x,
                y: s.y,
                isInitial: s.isInitial,
                isTerminalSuccess: s.isTerminalSuccess,
                isTerminalFailure: s.isTerminalFailure,
            })),
            transitions: transitions.map((t) => ({
                id: t.id,
                fromId: t.fromId,
                toId: t.toId,
                signalType: t.signalType,
                targetTag: t.targetTag,
            })),
        });
    }

    function destroy() {
        if (_animId) cancelAnimationFrame(_animId);
        _animId = null;
        if (_resizeObserver) { _resizeObserver.disconnect(); _resizeObserver = null; }
        if (canvas) {
            canvas.removeEventListener("mousedown", _onMouseDown);
            canvas.removeEventListener("mousemove", _onMouseMove);
            canvas.removeEventListener("mouseup", _onMouseUp);
            canvas.removeEventListener("wheel", _onWheel);
            canvas.removeEventListener("dblclick", _onDblClick);
            canvas.removeEventListener("contextmenu", _onContextMenu);
        }
        document.removeEventListener("keydown", _onKeyDown);
        _removeContextMenu();
        canvas = null; ctx = null;
        dotNetRef = null;
        _containerId = null;
        states = []; transitions = [];
        selectedStateId = null; selectedTransId = null;
        dragState = null; panDrag = null; connectDrag = null;
    }

    /* ── Sidebar binding ─────────────────────────────────── */
    function _showStateSidebar(state) {
        const sec = document.getElementById("ce-graph-sidebar-state");
        const transSec = document.getElementById("ce-graph-sidebar-transition");
        if (sec) sec.style.display = "block";
        if (transSec) transSec.style.display = "none";
        const idInput = document.getElementById("ce-graph-state-id");
        const descInput = document.getElementById("ce-graph-state-desc");
        const termSucc = document.getElementById("ce-graph-state-terminal-success");
        const termFail = document.getElementById("ce-graph-state-terminal-failure");
        if (idInput) { idInput.value = state.label; idInput.setAttribute("data-state-id", state.id); }
        if (descInput) { descInput.value = state.description; descInput.setAttribute("data-state-id", state.id); }
        if (termSucc) termSucc.checked = state.isTerminalSuccess;
        if (termFail) termFail.checked = state.isTerminalFailure;

        // Bind live updates
        _bindStateSidebar(state.id);
    }

    function _showTransSidebar(trans) {
        const sec = document.getElementById("ce-graph-sidebar-transition");
        const stateSec = document.getElementById("ce-graph-sidebar-state");
        if (sec) sec.style.display = "block";
        if (stateSec) stateSec.style.display = "none";
        const signalSelect = document.getElementById("ce-graph-trans-signal");
        const targetInput = document.getElementById("ce-graph-trans-target");
        if (signalSelect) { signalSelect.value = trans.signalType; signalSelect.setAttribute("data-trans-id", trans.id); }
        if (targetInput) { targetInput.value = trans.targetTag; targetInput.setAttribute("data-trans-id", trans.id); }

        _bindTransSidebar(trans.id);
    }

    function _hideAllSidebars() {
        ["ce-graph-sidebar-state", "ce-graph-sidebar-transition"].forEach((id) => {
            const el = document.getElementById(id);
            if (el) el.style.display = "none";
        });
    }

    let _boundStateListeners = {};
    let _boundTransListeners = {};

    function _bindStateSidebar(stateId) {
        // Remove old listeners
        _unbindSidebarListeners(_boundStateListeners);
        _boundStateListeners = {};

        const idInput = document.getElementById("ce-graph-state-id");
        const descInput = document.getElementById("ce-graph-state-desc");
        const termSucc = document.getElementById("ce-graph-state-terminal-success");
        const termFail = document.getElementById("ce-graph-state-terminal-failure");

        if (idInput) {
            const handler = () => {
                const s = states.find((st) => st.id === stateId);
                if (s) { s.label = idInput.value; _notifyGraphChanged(); }
            };
            idInput.addEventListener("input", handler);
            _boundStateListeners["ce-graph-state-id"] = { el: idInput, event: "input", handler };
        }
        if (descInput) {
            const handler = () => {
                const s = states.find((st) => st.id === stateId);
                if (s) { s.description = descInput.value; _notifyGraphChanged(); }
            };
            descInput.addEventListener("input", handler);
            _boundStateListeners["ce-graph-state-desc"] = { el: descInput, event: "input", handler };
        }
        if (termSucc) {
            const handler = () => { toggleTerminalSuccess(stateId); };
            termSucc.addEventListener("change", handler);
            _boundStateListeners["ce-graph-state-terminal-success"] = { el: termSucc, event: "change", handler };
        }
        if (termFail) {
            const handler = () => { toggleTerminalFailure(stateId); };
            termFail.addEventListener("change", handler);
            _boundStateListeners["ce-graph-state-terminal-failure"] = { el: termFail, event: "change", handler };
        }
    }

    function _bindTransSidebar(transId) {
        _unbindSidebarListeners(_boundTransListeners);
        _boundTransListeners = {};

        const signalSelect = document.getElementById("ce-graph-trans-signal");
        const targetInput = document.getElementById("ce-graph-trans-target");

        if (signalSelect) {
            const handler = () => {
                const t = transitions.find((tr) => tr.id === transId);
                if (t) { t.signalType = signalSelect.value; _notifyGraphChanged(); }
            };
            signalSelect.addEventListener("change", handler);
            _boundTransListeners["ce-graph-trans-signal"] = { el: signalSelect, event: "change", handler };
        }
        if (targetInput) {
            const handler = () => {
                const t = transitions.find((tr) => tr.id === transId);
                if (t) { t.targetTag = targetInput.value; _notifyGraphChanged(); }
            };
            targetInput.addEventListener("input", handler);
            _boundTransListeners["ce-graph-trans-target"] = { el: targetInput, event: "input", handler };
        }
    }

    function _unbindSidebarListeners(map) {
        Object.values(map).forEach((entry) => {
            if (entry.el) entry.el.removeEventListener(entry.event, entry.handler);
        });
    }

    /* ── Blazor interop ──────────────────────────────────── */
    function _invoke(method, arg) {
        if (dotNetRef) {
            dotNetRef.invokeMethodAsync(method, arg).catch((e) => {
                console.warn("[stateMachineEditor] invoke failed:", method, e);
            });
        }
    }

    function _notifyGraphChanged() {
        _invoke("OnGraphChanged", getGraphJson());
    }

    /* ── Return public API ───────────────────────────────── */
    return {
        init: init,
        destroy: destroy,
        addState: addState,
        addTransition: addTransition,
        removeSelected: removeSelected,
        setInitial: setInitial,
        toggleTerminalSuccess: toggleTerminalSuccess,
        toggleTerminalFailure: toggleTerminalFailure,
        fitView: fitView,
        validate: validate,
        getGraphJson: getGraphJson,
    };
})();
