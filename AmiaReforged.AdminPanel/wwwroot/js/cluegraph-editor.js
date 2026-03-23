/**
 * cluegraph-editor.js — Cytoscape.js DAG editor for quest ClueGraph objectives.
 *
 * Nodes  = Clues       (id, name, triggerTag)
 * Edges  = Deductions  (from → to, i.e. required clue → unlocked clue)
 * A single "conclusion" deduction is highlighted in gold.
 *
 * Pattern: IIFE → window.clueGraphEditor (matches dependencygraph.js)
 * Blazor interop via DotNetObjectReference.
 */
window.clueGraphEditor = (function () {
    "use strict";

    /* ── Private state ───────────────────────────────────── */
    let cy = null;
    let dotNetRef = null;
    let _containerId = null;
    let _nextClueId = 1;
    let _conclusionEdgeId = null; // id of the edge marked as the conclusion deduction

    /* ── Colour palette ──────────────────────────────────── */
    const CLR_CLUE       = "#4a90d9";
    const CLR_CLUE_LABEL = "#ffffff";
    const CLR_EDGE       = "#888888";
    const CLR_CONCLUSION = "#c9a84c"; // gold
    const CLR_SELECTED   = "#ff6060";
    const CLR_BG         = "#1e1e2e";

    /* ── Cytoscape style ─────────────────────────────────── */
    const STYLESHEET = [
        {
            selector: "node",
            style: {
                "background-color": CLR_CLUE,
                label: "data(label)",
                color: CLR_CLUE_LABEL,
                "text-valign": "center",
                "text-halign": "center",
                "font-size": "11px",
                width: 100,
                height: 40,
                shape: "round-rectangle",
                "text-max-width": "90px",
                "text-wrap": "ellipsis",
                "border-width": 0,
            },
        },
        {
            selector: "node:selected",
            style: {
                "border-width": 3,
                "border-color": CLR_SELECTED,
            },
        },
        {
            selector: "edge",
            style: {
                width: 2,
                "line-color": CLR_EDGE,
                "target-arrow-color": CLR_EDGE,
                "target-arrow-shape": "triangle",
                "curve-style": "bezier",
                "arrow-scale": 0.8,
            },
        },
        {
            selector: "edge:selected",
            style: {
                width: 3,
                "line-color": CLR_SELECTED,
                "target-arrow-color": CLR_SELECTED,
            },
        },
        {
            selector: ".conclusion",
            style: {
                "line-color": CLR_CONCLUSION,
                "target-arrow-color": CLR_CONCLUSION,
                width: 3,
                "line-style": "solid",
            },
        },
    ];

    /* ── Initialise ──────────────────────────────────────── */
    /**
     * @param {string}  containerId  DOM id of the canvas div
     * @param {string}  graphJson    Serialised { clues:[], deductions:[], conclusionEdgeId? }
     * @param {object}  blazorRef    DotNetObjectReference
     */
    function init(containerId, graphJson, blazorRef) {
        destroy(); // tear down any previous instance

        _containerId = containerId;
        dotNetRef = blazorRef;

        const container = document.getElementById(containerId);
        if (!container) {
            console.error("[clueGraphEditor] container not found:", containerId);
            return;
        }

        let data = { clues: [], deductions: [], conclusionEdgeId: null };
        try {
            if (graphJson) data = JSON.parse(graphJson);
        } catch (e) {
            console.warn("[clueGraphEditor] bad graphJson, starting empty", e);
        }

        // Derive next id
        _nextClueId = 1;
        if (data.clues && data.clues.length > 0) {
            const maxId = Math.max(...data.clues.map((c) => parseInt(c.id, 10) || 0));
            _nextClueId = maxId + 1;
        }
        _conclusionEdgeId = data.conclusionEdgeId || null;

        // Build elements
        const elements = [];
        (data.clues || []).forEach((c) => {
            elements.push({
                group: "nodes",
                data: {
                    id: String(c.id),
                    label: c.name || "Clue " + c.id,
                    triggerTag: c.triggerTag || "",
                },
                position: c.position || undefined,
            });
        });
        (data.deductions || []).forEach((d) => {
            const edgeId = d.id || d.source + "->" + d.target;
            const cls = edgeId === _conclusionEdgeId ? "conclusion" : "";
            elements.push({
                group: "edges",
                data: { id: edgeId, source: String(d.source), target: String(d.target) },
                classes: cls,
            });
        });

        cy = cytoscape({
            container: container,
            elements: elements,
            style: STYLESHEET,
            layout: { name: elements.length > 0 ? "dagre" : "preset" },
            boxSelectionEnabled: false,
            wheelSensitivity: 0.3,
        });

        // Background colour
        container.style.backgroundColor = CLR_BG;

        /* ── Event wiring ──────────────────────────────────── */
        cy.on("tap", "node", function (evt) {
            const n = evt.target;
            _showSidebar("clue", {
                id: n.id(),
                name: n.data("label"),
                triggerTag: n.data("triggerTag"),
            });
            _invoke("OnClueSelected", JSON.stringify({
                id: n.id(),
                name: n.data("label"),
                triggerTag: n.data("triggerTag"),
            }));
        });

        cy.on("tap", "edge", function (evt) {
            const e = evt.target;
            _showSidebar("deduction", {
                id: e.id(),
                source: e.data("source"),
                target: e.data("target"),
                isConclusion: e.id() === _conclusionEdgeId,
            });
            _invoke("OnDeductionSelected", JSON.stringify({
                id: e.id(),
                source: e.data("source"),
                target: e.data("target"),
                isConclusion: e.id() === _conclusionEdgeId,
            }));
        });

        cy.on("tap", function (evt) {
            if (evt.target === cy) {
                _hideSidebar();
                _invoke("OnSelectionCleared", "");
            }
        });

        // Double-click to add clue at position
        cy.on("dbltap", function (evt) {
            if (evt.target === cy) {
                const pos = evt.position;
                addClue("Clue " + _nextClueId, "", pos);
            }
        });

        // Wiring: sidebar inputs → live update node data
        _bindSidebarInputs();

        if (elements.length > 0) {
            cy.fit(undefined, 30);
        }
    }

    /* ── Public API ──────────────────────────────────────── */
    function addClue(name, triggerTag, position) {
        if (!cy) return null;
        const id = String(_nextClueId++);
        const pos = position || { x: 200 + Math.random() * 200, y: 200 + Math.random() * 200 };
        cy.add({
            group: "nodes",
            data: { id: id, label: name || "Clue " + id, triggerTag: triggerTag || "" },
            position: pos,
        });
        _runLayout();
        _notifyGraphChanged();
        return id;
    }

    function removeSelected() {
        if (!cy) return;
        const sel = cy.$(":selected");
        if (sel.length === 0) return;
        // If removing a node, also remove connected edges
        sel.forEach((el) => {
            if (el.isNode()) {
                el.connectedEdges().remove();
            }
        });
        sel.remove();
        _hideSidebar();
        _notifyGraphChanged();
    }

    function addDeduction(sourceId, targetId) {
        if (!cy) return null;
        const edgeId = sourceId + "->" + targetId;
        // Prevent duplicates
        if (cy.getElementById(edgeId).length > 0) return edgeId;
        cy.add({
            group: "edges",
            data: { id: edgeId, source: String(sourceId), target: String(targetId) },
        });
        _runLayout();
        _notifyGraphChanged();
        return edgeId;
    }

    function setConclusionEdge(edgeId) {
        if (!cy) return;
        // Clear old
        if (_conclusionEdgeId) {
            const old = cy.getElementById(_conclusionEdgeId);
            if (old.length > 0) old.removeClass("conclusion");
        }
        _conclusionEdgeId = edgeId;
        if (edgeId) {
            const edge = cy.getElementById(edgeId);
            if (edge.length > 0) edge.addClass("conclusion");
        }
        _notifyGraphChanged();
    }

    function fitView() {
        if (cy) cy.fit(undefined, 30);
    }

    function validate() {
        if (!cy) return JSON.stringify({ valid: false, errors: ["No graph initialised"] });
        const errors = [];
        const nodes = cy.nodes();
        const edges = cy.edges();

        if (nodes.length === 0) errors.push("Graph has no clues.");

        // Ensure we have at least one conclusion deduction
        if (!_conclusionEdgeId) {
            errors.push("No conclusion deduction set. Right-click an edge → Set as Conclusion.");
        } else if (cy.getElementById(_conclusionEdgeId).length === 0) {
            errors.push("Conclusion edge references a deleted edge.");
        }

        // Check for cycles (DAG requirement)
        if (_hasCycle()) {
            errors.push("Graph contains a cycle. Clue graphs must be acyclic (DAG).");
        }

        // Check for orphan nodes (no edges)
        nodes.forEach((n) => {
            if (n.connectedEdges().length === 0 && nodes.length > 1) {
                errors.push("Clue '" + n.data("label") + "' has no connections.");
            }
        });

        return JSON.stringify({ valid: errors.length === 0, errors: errors });
    }

    function getGraphJson() {
        if (!cy) return "{}";
        const clues = [];
        cy.nodes().forEach((n) => {
            clues.push({
                id: n.id(),
                name: n.data("label"),
                triggerTag: n.data("triggerTag"),
                position: n.position(),
            });
        });
        const deductions = [];
        cy.edges().forEach((e) => {
            deductions.push({
                id: e.id(),
                source: e.data("source"),
                target: e.data("target"),
            });
        });
        return JSON.stringify({
            clues: clues,
            deductions: deductions,
            conclusionEdgeId: _conclusionEdgeId,
        });
    }

    function destroy() {
        if (cy) {
            cy.destroy();
            cy = null;
        }
        dotNetRef = null;
        _containerId = null;
        _nextClueId = 1;
        _conclusionEdgeId = null;
    }

    /* ── Connection mode ─────────────────────────────────── */
    let _connectMode = false;
    let _connectSource = null;

    function enableConnectMode() {
        _connectMode = true;
        _connectSource = null;
        if (cy) {
            cy.nodes().ungrabify();
            const container = document.getElementById(_containerId);
            if (container) container.style.cursor = "crosshair";
        }
    }

    function disableConnectMode() {
        _connectMode = false;
        _connectSource = null;
        if (cy) {
            cy.nodes().grabify();
            const container = document.getElementById(_containerId);
            if (container) container.style.cursor = "";
        }
    }

    /* ── Private helpers ─────────────────────────────────── */
    function _runLayout() {
        if (!cy || cy.nodes().length < 2) return;
        cy.layout({
            name: "dagre",
            rankDir: "TB",
            nodeSep: 40,
            rankSep: 60,
            animate: true,
            animationDuration: 300,
        }).run();
    }

    function _hasCycle() {
        // Kahn's algorithm
        const adj = {};
        const inDeg = {};
        cy.nodes().forEach((n) => {
            adj[n.id()] = [];
            inDeg[n.id()] = 0;
        });
        cy.edges().forEach((e) => {
            const s = e.data("source"),
                t = e.data("target");
            adj[s].push(t);
            inDeg[t] = (inDeg[t] || 0) + 1;
        });
        const queue = Object.keys(inDeg).filter((k) => inDeg[k] === 0);
        let count = 0;
        while (queue.length > 0) {
            const n = queue.shift();
            count++;
            (adj[n] || []).forEach((t) => {
                inDeg[t]--;
                if (inDeg[t] === 0) queue.push(t);
            });
        }
        return count !== cy.nodes().length;
    }

    function _invoke(method, arg) {
        if (dotNetRef) {
            dotNetRef.invokeMethodAsync(method, arg).catch((e) => {
                console.warn("[clueGraphEditor] invoke failed:", method, e);
            });
        }
    }

    function _notifyGraphChanged() {
        _invoke("OnGraphChanged", getGraphJson());
    }

    /* ── Sidebar binding ─────────────────────────────────── */
    function _showSidebar(type, data) {
        const clueSec = document.getElementById("ce-graph-sidebar-clue");
        const dedSec = document.getElementById("ce-graph-sidebar-deduction");
        if (!clueSec || !dedSec) return;

        if (type === "clue") {
            clueSec.style.display = "block";
            dedSec.style.display = "none";
            const nameInput = document.getElementById("ce-graph-clue-name");
            const triggerInput = document.getElementById("ce-graph-clue-trigger");
            if (nameInput) nameInput.value = data.name || "";
            if (triggerInput) triggerInput.value = data.triggerTag || "";
            // Store selected id for live binding
            nameInput?.setAttribute("data-node-id", data.id);
            triggerInput?.setAttribute("data-node-id", data.id);
        } else if (type === "deduction") {
            clueSec.style.display = "none";
            dedSec.style.display = "block";
            const reqDiv = document.getElementById("ce-graph-deduction-required");
            const unlDiv = document.getElementById("ce-graph-deduction-unlocks");
            if (reqDiv) {
                const srcNode = cy.getElementById(data.source);
                reqDiv.textContent = srcNode.length > 0 ? srcNode.data("label") : data.source;
            }
            if (unlDiv) {
                const tgtNode = cy.getElementById(data.target);
                unlDiv.textContent = tgtNode.length > 0 ? tgtNode.data("label") : data.target;
            }
        }
    }

    function _hideSidebar() {
        const clueSec = document.getElementById("ce-graph-sidebar-clue");
        const dedSec = document.getElementById("ce-graph-sidebar-deduction");
        if (clueSec) clueSec.style.display = "none";
        if (dedSec) dedSec.style.display = "none";
    }

    function _bindSidebarInputs() {
        // Live-update node data when sidebar inputs change
        const nameInput = document.getElementById("ce-graph-clue-name");
        const triggerInput = document.getElementById("ce-graph-clue-trigger");

        if (nameInput) {
            nameInput.addEventListener("input", function () {
                const nodeId = this.getAttribute("data-node-id");
                if (nodeId && cy) {
                    const node = cy.getElementById(nodeId);
                    if (node.length > 0) {
                        node.data("label", this.value);
                        _notifyGraphChanged();
                    }
                }
            });
        }
        if (triggerInput) {
            triggerInput.addEventListener("input", function () {
                const nodeId = this.getAttribute("data-node-id");
                if (nodeId && cy) {
                    const node = cy.getElementById(nodeId);
                    if (node.length > 0) {
                        node.data("triggerTag", this.value);
                        _notifyGraphChanged();
                    }
                }
            });
        }

        // Connect mode: tap node to set source/target
        // Uses a separate cy event that checks _connectMode
        if (cy) {
            cy.on("tap", "node", function (evt) {
                if (!_connectMode) return;
                const nodeId = evt.target.id();
                if (!_connectSource) {
                    _connectSource = nodeId;
                    evt.target.style("border-width", 3);
                    evt.target.style("border-color", "#44ff44");
                } else {
                    if (_connectSource !== nodeId) {
                        addDeduction(_connectSource, nodeId);
                    }
                    // Reset
                    const srcNode = cy.getElementById(_connectSource);
                    if (srcNode.length > 0) {
                        srcNode.style("border-width", 0);
                    }
                    _connectSource = null;
                }
            });
        }
    }

    /* ── Public API ──────────────────────────────────────── */
    return {
        init: init,
        destroy: destroy,
        addClue: addClue,
        removeSelected: removeSelected,
        addDeduction: addDeduction,
        setConclusionEdge: setConclusionEdge,
        enableConnectMode: enableConnectMode,
        disableConnectMode: disableConnectMode,
        fitView: fitView,
        validate: validate,
        getGraphJson: getGraphJson,
    };
})();
