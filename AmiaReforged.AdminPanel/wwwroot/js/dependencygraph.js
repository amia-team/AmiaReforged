/**
 * PwEngine Dependency Graph — Cytoscape.js interop.
 * Uses compound (parent) nodes for namespace grouping.
 * Inspired by regiongraph.js but simplified: no drag-and-drop, no context menus.
 *
 * Requires: cytoscape.js, cytoscape-fcose (optional), cytoscape-dagre (optional).
 */
(function () {
    try {
        if (typeof cytoscape !== 'undefined') {
            if (typeof cytoscapeFcose !== 'undefined') {
                cytoscape.use(cytoscapeFcose);
            }
            if (typeof cytoscapeDagre !== 'undefined') {
                cytoscape.use(cytoscapeDagre);
            }
        }
    } catch (e) {
        console.warn('[depGraph] extension registration:', e);
    }
})();

window.depGraph = (function () {
    /** @type {import('cytoscape').Core | null} */
    let cy = null;
    /** @type {DotNet.DotNetObject | null} */
    let dotNetRef = null;

    let _initGeneration = 0;
    const BATCH_SIZE_NODES = 80;
    const BATCH_SIZE_EDGES = 200;

    // 12 distinct namespace group colors
    const NS_PALETTE = [
        '#e6194b', '#3cb44b', '#4363d8', '#f58231',
        '#911eb4', '#42d4f4', '#f032e6', '#bfef45',
        '#fabed4', '#469990', '#dcbeff', '#9A6324'
    ];

    // Edge colors by relationship type
    const EDGE_COLORS = {
        'Inherits': '#5ba4b5',
        'Implements': '#4ecca3',
        'ConstructorDep': '#f58231',
        'FieldDep': '#888888'
    };

    // Node shape by type kind
    const NODE_SHAPES = {
        'Class': 'round-rectangle',
        'Interface': 'diamond',
        'Struct': 'hexagon',
        'Enum': 'triangle',
        'Record': 'round-rectangle'
    };

    const COLORS = {
        nodeText: '#d0ccc2',
        highlightNode: '#ffc107',
        highlightEdge: '#ffc107',
        selectedNode: '#ff9800',
        nsBg: 'rgba(255,255,255,0.04)',
        nsBorder: 'rgba(255,255,255,0.15)'
    };

    let nsColorMap = {};

    function _yieldFrame() {
        return new Promise(function (resolve) { requestAnimationFrame(resolve); });
    }

    function _reportProgress(percent, phase) {
        if (dotNetRef) {
            try {
                dotNetRef.invokeMethodAsync('OnGraphLoadProgress', Math.round(percent), phase);
            } catch (e) { }
        }
    }

    async function _addElementsProgressively(elements, batchSize, basePercent, phaseWeight, phaseLabel, generation) {
        var total = elements.length;
        if (total === 0) return true;
        for (var i = 0; i < total; i += batchSize) {
            if (_initGeneration !== generation) return false;
            var chunk = elements.slice(i, Math.min(i + batchSize, total));
            cy.startBatch();
            cy.add(chunk);
            cy.endBatch();
            var pct = basePercent + ((i + chunk.length) / total) * phaseWeight;
            _reportProgress(pct, phaseLabel);
            await _yieldFrame();
        }
        return true;
    }

    function darken(hex, amount) {
        var r = parseInt(hex.slice(1, 3), 16);
        var g = parseInt(hex.slice(3, 5), 16);
        var b = parseInt(hex.slice(5, 7), 16);
        r = Math.max(0, Math.floor(r * (1 - amount)));
        g = Math.max(0, Math.floor(g * (1 - amount)));
        b = Math.max(0, Math.floor(b * (1 - amount)));
        return '#' + [r, g, b].map(function (c) { return c.toString(16).padStart(2, '0'); }).join('');
    }

    /**
     * Initialize the dependency graph.
     * @param {string} containerId DOM id of container div
     * @param {string} graphJson JSON serialized DependencyGraphDto
     * @param {DotNet.DotNetObject} blazorRef .NET object ref for callbacks
     * @param {string|null} positionsJson JSON object mapping node IDs to {x, y}
     */
    function init(containerId, graphJson, blazorRef, positionsJson) {
        var hasServerPositions = positionsJson && positionsJson !== 'null' && positionsJson !== '';
        console.log('[depGraph] init called, server positions:', !!hasServerPositions);
        destroy();
        dotNetRef = blazorRef || null;

        var generation = ++_initGeneration;
        var container = document.getElementById(containerId);
        if (!container) {
            console.error('[depGraph] Container not found:', containerId);
            return;
        }

        _reportProgress(0, 'Parsing data...');

        var graph = JSON.parse(graphJson);
        var serverPositions = hasServerPositions ? JSON.parse(positionsJson) : null;
        var namespaces = graph.namespaces || [];
        var types = graph.types || [];
        var edges = graph.edges || [];

        // Build namespace -> color map
        nsColorMap = {};
        namespaces.forEach(function (ns, idx) {
            nsColorMap[ns.id] = NS_PALETTE[idx % NS_PALETTE.length];
        });

        // ===== Prepare Cytoscape elements =====

        var nsElements = [];
        var typeElements = [];
        var edgeElements = [];

        // Namespace parent (compound) nodes
        namespaces.forEach(function (ns) {
            var color = nsColorMap[ns.id] || '#666';
            var elem = {
                group: 'nodes',
                data: {
                    id: ns.id,
                    label: ns.label + ' (' + ns.typeCount + ')',
                    fullName: ns.fullName,
                    isNamespace: 'yes',
                    nsColor: color,
                    typeCount: ns.typeCount
                }
            };
            if (serverPositions) {
                var pos = serverPositions[ns.id];
                if (pos) elem.position = { x: pos.x, y: pos.y };
            }
            nsElements.push(elem);
        });

        // Type nodes
        types.forEach(function (t) {
            var nsColor = nsColorMap[t.namespaceId] || '#666';
            var shape = NODE_SHAPES[t.kind] || 'ellipse';
            var borderWidth = t.isAbstract ? 3 : (t.kind === 'Interface' ? 2 : 1);

            var data = {
                id: t.id,
                label: t.label,
                fullName: t.fullName,
                parent: t.namespaceId,
                kind: t.kind,
                isAbstract: t.isAbstract ? 'yes' : '',
                isStatic: t.isStatic ? 'yes' : '',
                baseClass: t.baseClass || '',
                interfaces: (t.interfaces || []).join(', '),
                inbound: t.inboundCount,
                outbound: t.outboundCount,
                nodeColor: nsColor,
                nodeBorder: darken(nsColor, 0.3),
                nodeShape: shape,
                nodeBorderWidth: borderWidth
            };

            var nodeElem = { group: 'nodes', data: data };
            if (serverPositions) {
                var pos = serverPositions[t.id];
                if (pos) nodeElem.position = { x: pos.x, y: pos.y };
            } else {
                nodeElem.position = { x: Math.random() * 1200 - 600, y: Math.random() * 1200 - 600 };
            }

            typeElements.push(nodeElem);
        });

        // Edges
        edges.forEach(function (e, idx) {
            edgeElements.push({
                group: 'edges',
                data: {
                    id: 'e' + idx,
                    source: e.source,
                    target: e.target,
                    relationship: e.relationship,
                    edgeLabel: e.label || '',
                    edgeColor: EDGE_COLORS[e.relationship] || '#666'
                }
            });
        });

        // ===== Create Cytoscape instance =====

        cy = cytoscape({
            container: container,
            elements: [],
            minZoom: 0.05,
            maxZoom: 3,
            wheelSensitivity: 0.2,
            textureOnViewport: true,
            hideEdgesOnViewport: true,
            pixelRatio: 1,
            style: [
                // Namespace parent (compound) nodes
                {
                    selector: 'node[isNamespace = "yes"]',
                    style: {
                        'background-color': 'data(nsColor)',
                        'background-opacity': 0.08,
                        'border-color': 'data(nsColor)',
                        'border-width': 2,
                        'border-opacity': 0.25,
                        'label': 'data(label)',
                        'font-size': '16px',
                        'font-weight': 'bold',
                        'color': 'data(nsColor)',
                        'text-opacity': 0.7,
                        'text-valign': 'top',
                        'text-halign': 'center',
                        'text-margin-y': -8,
                        'padding': '30px',
                        'shape': 'round-rectangle',
                        'min-width': '100px',
                        'min-height': '60px'
                    }
                },
                // Type nodes (children)
                {
                    selector: 'node[!isNamespace]',
                    style: {
                        'background-color': 'data(nodeColor)',
                        'border-color': 'data(nodeBorder)',
                        'border-width': 'data(nodeBorderWidth)',
                        'label': 'data(label)',
                        'font-size': '10px',
                        'color': COLORS.nodeText,
                        'text-valign': 'center',
                        'text-halign': 'center',
                        'width': 'label',
                        'height': 'label',
                        'padding': '8px',
                        'shape': 'data(nodeShape)',
                        'text-wrap': 'wrap',
                        'text-max-width': '120px'
                    }
                },
                // Abstract types: dashed border
                {
                    selector: 'node[isAbstract = "yes"]',
                    style: {
                        'border-style': 'dashed'
                    }
                },
                // Static types: double border
                {
                    selector: 'node[isStatic = "yes"]',
                    style: {
                        'border-style': 'double',
                        'border-width': 4
                    }
                },
                // Interface nodes
                {
                    selector: 'node[kind = "Interface"]',
                    style: {
                        'background-opacity': 0.6
                    }
                },
                // Edges
                {
                    selector: 'edge',
                    style: {
                        'width': 1.5,
                        'line-color': 'data(edgeColor)',
                        'target-arrow-color': 'data(edgeColor)',
                        'target-arrow-shape': 'triangle',
                        'curve-style': 'bezier',
                        'arrow-scale': 0.8,
                        'opacity': 0.6
                    }
                },
                // Implements: dashed
                {
                    selector: 'edge[relationship = "Implements"]',
                    style: {
                        'line-style': 'dashed',
                        'line-dash-pattern': [6, 3]
                    }
                },
                // FieldDep: dotted
                {
                    selector: 'edge[relationship = "FieldDep"]',
                    style: {
                        'line-style': 'dotted',
                        'width': 1,
                        'opacity': 0.4
                    }
                },
                // ConstructorDep: slightly thicker
                {
                    selector: 'edge[relationship = "ConstructorDep"]',
                    style: {
                        'width': 2
                    }
                },
                // Inherits: solid thick
                {
                    selector: 'edge[relationship = "Inherits"]',
                    style: {
                        'width': 2.5,
                        'target-arrow-shape': 'triangle-tee'
                    }
                },
                // Highlighted node
                {
                    selector: '.highlighted',
                    style: {
                        'border-color': COLORS.highlightNode,
                        'border-width': 3,
                        'z-index': 999
                    }
                },
                // Selected node
                {
                    selector: ':selected',
                    style: {
                        'border-color': COLORS.selectedNode,
                        'border-width': 4,
                        'z-index': 999
                    }
                },
                // Dimmed (for search filtering)
                {
                    selector: '.dimmed',
                    style: {
                        'opacity': 0.15
                    }
                },
                // Neighbor highlight for selected node
                {
                    selector: '.neighbor-highlight',
                    style: {
                        'border-color': COLORS.highlightNode,
                        'border-width': 2,
                        'opacity': 1,
                        'z-index': 998
                    }
                },
                // Connected edge highlight
                {
                    selector: '.edge-highlight',
                    style: {
                        'opacity': 1,
                        'width': 3,
                        'z-index': 998
                    }
                }
            ]
        });

        // ===== Progressive rendering =====
        (async function () {
            var ok;

            _reportProgress(5, 'Adding namespace groups...');
            ok = await _addElementsProgressively(nsElements, BATCH_SIZE_NODES, 5, 10, 'Adding namespace groups...', generation);
            if (!ok) return;

            _reportProgress(15, 'Adding type nodes...');
            ok = await _addElementsProgressively(typeElements, BATCH_SIZE_NODES, 15, 40, 'Adding type nodes...', generation);
            if (!ok) return;

            _reportProgress(55, 'Adding dependency edges...');
            ok = await _addElementsProgressively(edgeElements, BATCH_SIZE_EDGES, 55, 25, 'Adding dependency edges...', generation);
            if (!ok) return;

            // Layout
            if (serverPositions) {
                _reportProgress(85, 'Applying positions...');
                cy.fit(undefined, 40);
            } else {
                _reportProgress(80, 'Running layout...');
                var layoutName = 'cose';
                try {
                    cy.layout({
                        name: layoutName,
                        animate: false,
                        nodeDimensionsIncludeLabels: true,
                        idealEdgeLength: 120,
                        nodeRepulsion: 8000,
                        numIter: 300,
                        gravity: 0.3,
                        nestingFactor: 1.2,
                        randomize: false
                    }).run();
                } catch (e) {
                    console.warn('[depGraph] layout error, falling back to grid:', e);
                    cy.layout({ name: 'grid', animate: false }).run();
                }
                cy.fit(undefined, 40);
            }

            _reportProgress(95, 'Setting up interactions...');
            _setupInteractions();
            _reportProgress(100, 'Complete');
            console.log('[depGraph] init complete —', types.length, 'types,', edges.length, 'edges');
        })();

        } catch (e) {
            console.error('[depGraph] init error:', e);
        }
    }

    function _setupInteractions() {
        if (!cy) return;

        // Click on type node
        cy.on('tap', 'node[!isNamespace]', function (evt) {
            var node = evt.target;
            _clearHighlights();

            // Highlight neighbors
            var neighborhood = node.neighborhood();
            cy.elements().not(node).not(neighborhood).addClass('dimmed');
            neighborhood.nodes().addClass('neighbor-highlight');
            neighborhood.edges().addClass('edge-highlight');

            // Build info for Blazor callback
            var data = node.data();
            var connectedEdges = node.connectedEdges();
            var inbound = [];
            var outbound = [];
            connectedEdges.forEach(function (edge) {
                var ed = edge.data();
                if (ed.target === data.id) {
                    inbound.push({ source: ed.source, relationship: ed.relationship, label: ed.edgeLabel });
                } else {
                    outbound.push({ target: ed.target, relationship: ed.relationship, label: ed.edgeLabel });
                }
            });

            var info = {
                id: data.id,
                label: data.label,
                fullName: data.fullName,
                kind: data.kind,
                isAbstract: data.isAbstract === 'yes',
                isStatic: data.isStatic === 'yes',
                baseClass: data.baseClass || null,
                interfaces: data.interfaces || '',
                inboundCount: data.inbound,
                outboundCount: data.outbound,
                inboundEdges: inbound,
                outboundEdges: outbound
            };

            if (dotNetRef) {
                try {
                    dotNetRef.invokeMethodAsync('OnNodeSelected', JSON.stringify(info));
                } catch (e) { }
            }
        });

        // Click on namespace (compound parent)
        cy.on('tap', 'node[isNamespace = "yes"]', function (evt) {
            var node = evt.target;
            _clearHighlights();

            var data = node.data();
            var children = node.children();
            cy.elements().not(node).not(children).addClass('dimmed');
            children.addClass('neighbor-highlight');

            if (dotNetRef) {
                try {
                    dotNetRef.invokeMethodAsync('OnNamespaceSelected', JSON.stringify({
                        id: data.id,
                        fullName: data.fullName,
                        label: data.label,
                        typeCount: data.typeCount
                    }));
                } catch (e) { }
            }
        });

        // Click on background → deselect
        cy.on('tap', function (evt) {
            if (evt.target === cy) {
                _clearHighlights();
                if (dotNetRef) {
                    try {
                        dotNetRef.invokeMethodAsync('OnNodeDeselected');
                    } catch (e) { }
                }
            }
        });
    }

    function _clearHighlights() {
        if (!cy) return;
        cy.elements().removeClass('dimmed neighbor-highlight edge-highlight highlighted');
    }

    /**
     * Highlight a specific node by id (for search).
     */
    function highlightNode(nodeId) {
        if (!cy) return;
        _clearHighlights();
        var node = cy.getElementById(nodeId);
        if (node && node.length > 0) {
            var neighborhood = node.neighborhood();
            cy.elements().not(node).not(neighborhood).addClass('dimmed');
            node.addClass('highlighted');
            neighborhood.nodes().addClass('neighbor-highlight');
            neighborhood.edges().addClass('edge-highlight');
            cy.animate({ center: { eles: node }, zoom: 1.5, duration: 400 });
        }
    }

    /**
     * Clear all highlights.
     */
    function clearHighlight() {
        _clearHighlights();
    }

    /**
     * Fit the graph to the viewport.
     */
    function fitView() {
        if (cy) cy.fit(undefined, 40);
    }

    /**
     * Run a layout algorithm.
     */
    function runLayout(name, options) {
        if (!cy) return;
        var opts = options || {};
        opts.name = name;
        opts.animate = opts.animate !== undefined ? opts.animate : false;
        opts.nodeDimensionsIncludeLabels = true;
        try {
            cy.layout(opts).run();
            cy.fit(undefined, 40);
        } catch (e) {
            console.warn('[depGraph] layout error:', e);
        }
    }

    /**
     * Apply server-computed positions.
     */
    function applyPositions(positionsJson) {
        if (!cy || !positionsJson) return;
        var positions = JSON.parse(positionsJson);
        cy.startBatch();
        Object.keys(positions).forEach(function (id) {
            var node = cy.getElementById(id);
            if (node && node.length > 0) {
                node.position(positions[id]);
            }
        });
        cy.endBatch();
        cy.fit(undefined, 40);
    }

    /**
     * Filter visibility — show only edges of certain relationship types.
     */
    function filterEdges(relationships) {
        if (!cy) return;
        cy.edges().forEach(function (edge) {
            var rel = edge.data('relationship');
            if (relationships.indexOf(rel) >= 0) {
                edge.style('display', 'element');
            } else {
                edge.style('display', 'none');
            }
        });
    }

    /**
     * Show all edges (reset filter).
     */
    function showAllEdges() {
        if (!cy) return;
        cy.edges().style('display', 'element');
    }

    /**
     * Get node count (for testing).
     */
    function getNodeCount() {
        return cy ? cy.nodes().length : 0;
    }

    /**
     * Destroy and clean up.
     */
    function destroy() {
        if (cy) {
            cy.destroy();
            cy = null;
        }
        dotNetRef = null;
        nsColorMap = {};
    }

    return {
        init: init,
        highlightNode: highlightNode,
        clearHighlight: clearHighlight,
        fitView: fitView,
        runLayout: runLayout,
        applyPositions: applyPositions,
        filterEdges: filterEdges,
        showAllEdges: showAllEdges,
        getNodeCount: getNodeCount,
        destroy: destroy
    };
})();
