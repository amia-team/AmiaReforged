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
    let _expandedNamespace = null;
    let _visibleRelationships = ['Inherits', 'Implements', 'ConstructorDep', 'FieldDep'];

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

        try {

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

        // Edges — skip any where source or target is missing from the graph
        var typeIdSet = {};
        types.forEach(function (t) { typeIdSet[t.id] = true; });

        edges.forEach(function (e, idx) {
            if (!typeIdSet[e.source] || !typeIdSet[e.target]) {
                console.warn('[depGraph] skipping edge e' + idx + ': missing node', e.source, '->', e.target);
                return;
            }
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
                        'padding': '50px',
                        'shape': 'round-rectangle',
                        'min-width': '140px',
                        'min-height': '80px'
                    }
                },
                // Collapsed namespace (dashed border, compact)
                {
                    selector: 'node.ns-collapsed',
                    style: {
                        'border-style': 'dashed',
                        'background-opacity': 0.12,
                        'min-width': '200px',
                        'min-height': '45px',
                        'padding': '10px',
                        'text-valign': 'center',
                        'text-margin-y': 0,
                        'font-size': '13px'
                    }
                },
                // Expanded namespace
                {
                    selector: 'node.ns-expanded',
                    style: {
                        'border-style': 'solid',
                        'border-width': 3,
                        'background-opacity': 0.08
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

                // Prefer fcose — it has proper compound node support with
                // no-overlap constraints so namespace groups don't collide.
                var hasFcose = typeof cy.layout({ name: 'fcose' }).options !== 'undefined';
                try {
                    // Quick test if fcose is actually registered
                    cy.layout({ name: 'fcose' });
                    hasFcose = true;
                } catch (_) {
                    hasFcose = false;
                }

                try {
                    if (hasFcose) {
                        cy.layout({
                            name: 'fcose',
                            animate: false,
                            quality: 'proof',                 // highest quality placement
                            nodeDimensionsIncludeLabels: true,
                            packComponents: true,             // pack disconnected components
                            // -- Spacing --
                            idealEdgeLength: 180,             // spread edges out
                            nodeRepulsion: function () { return 25000; },
                            edgeElasticity: function () { return 0.1; },
                            // -- Compound / Nesting --
                            nestingFactor: 0.15,              // tighten children inside parents
                            gravity: 0.2,                     // gentle pull to center
                            gravityRange: 1.5,
                            gravityCompound: 0.5,             // keep children near parent center
                            gravityRangeCompound: 2.0,
                            // -- Iterations --
                            numIter: 5000,                    // plenty of iterations for convergence
                            // -- Overlap removal --
                            tile: true,                       // tile disconnected components
                            tilingPaddingVertical: 40,
                            tilingPaddingHorizontal: 40,
                            nodeSeparation: 100,              // min gap between sibling nodes
                            piTol: 0.0000001,
                            // -- Compound padding --
                            componentSpacing: 120,            // spacing between disconnected components
                            randomize: true                   // random start for better results
                        }).run();
                    } else {
                        // Fallback to cose with generous spacing
                        cy.layout({
                            name: 'cose',
                            animate: false,
                            nodeDimensionsIncludeLabels: true,
                            idealEdgeLength: 200,
                            nodeRepulsion: 50000,
                            numIter: 500,
                            gravity: 0.15,
                            nestingFactor: 5,
                            nodeOverlap: 40,
                            randomize: true
                        }).run();
                    }
                } catch (e) {
                    console.warn('[depGraph] layout error, falling back to grid:', e);
                    cy.layout({ name: 'grid', animate: false }).run();
                }
                cy.fit(undefined, 60);
            }

            _reportProgress(90, 'Setting up interactions...');
            _setupInteractions();

            _reportProgress(95, 'Collapsing namespaces...');
            _collapseAll();

            _reportProgress(100, 'Complete');
            console.log('[depGraph] init complete —', types.length, 'types,', edges.length, 'edges');
        })();

        } catch (e) {
            console.error('[depGraph] init error:', e);
        }
    }

    // ===== Collapse / Expand namespaces =====

    function _collapseAll() {
        if (!cy) return;
        _expandedNamespace = null;
        cy.startBatch();
        // Hide all type nodes
        cy.nodes('[!isNamespace]').style('display', 'none');
        // Hide all edges
        cy.edges().style('display', 'none');
        // Mark all namespaces as collapsed with ▶ indicator
        cy.nodes('[isNamespace = "yes"]').forEach(function (ns) {
            var base = (ns.data('baseLabel') || ns.data('label')).replace(/^[▶▼] /, '');
            ns.data('baseLabel', base);
            ns.data('label', '▶ ' + base);
            ns.removeClass('ns-expanded');
            ns.addClass('ns-collapsed');
        });
        cy.endBatch();
        cy.fit(undefined, 40);
    }

    function _expandNamespace(nsId) {
        if (!cy) return;
        cy.startBatch();

        // Collapse previously expanded namespace
        if (_expandedNamespace && _expandedNamespace !== nsId) {
            var prevNs = cy.getElementById(_expandedNamespace);
            if (prevNs && prevNs.length > 0) {
                prevNs.children().style('display', 'none');
                prevNs.children().connectedEdges().style('display', 'none');
                var prevBase = (prevNs.data('baseLabel') || prevNs.data('label')).replace(/^[▶▼] /, '');
                prevNs.data('baseLabel', prevBase);
                prevNs.data('label', '▶ ' + prevBase);
                prevNs.removeClass('ns-expanded');
                prevNs.addClass('ns-collapsed');
            }
        }

        _expandedNamespace = nsId;
        var nsNode = cy.getElementById(nsId);
        if (!nsNode || nsNode.length === 0) {
            cy.endBatch();
            return;
        }

        // Show children of this namespace
        nsNode.children().style('display', 'element');

        // Show edges where BOTH endpoints are visible AND relationship passes filter
        cy.edges().forEach(function (edge) {
            var src = cy.getElementById(edge.data('source'));
            var tgt = cy.getElementById(edge.data('target'));
            var srcVisible = src.style('display') !== 'none';
            var tgtVisible = tgt.style('display') !== 'none';
            var relOk = _visibleRelationships.indexOf(edge.data('relationship')) >= 0;
            if (srcVisible && tgtVisible && relOk) {
                edge.style('display', 'element');
            } else {
                edge.style('display', 'none');
            }
        });

        // Update label to expanded indicator
        var base = (nsNode.data('baseLabel') || nsNode.data('label')).replace(/^[▶▼] /, '');
        nsNode.data('baseLabel', base);
        nsNode.data('label', '▼ ' + base);
        nsNode.removeClass('ns-collapsed');
        nsNode.addClass('ns-expanded');

        cy.endBatch();

        // Animate to focus on the expanded namespace
        cy.animate({
            fit: { eles: nsNode, padding: 80 },
            duration: 400,
            easing: 'ease-out'
        });
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

        // Double-click on namespace → toggle expand/collapse
        cy.on('dbltap', 'node[isNamespace = "yes"]', function (evt) {
            var nsId = evt.target.data('id');
            if (_expandedNamespace === nsId) {
                _collapseAll();
            } else {
                _expandNamespace(nsId);
            }
            // Notify Blazor
            if (dotNetRef) {
                try {
                    dotNetRef.invokeMethodAsync('OnNamespaceExpandChanged', _expandedNamespace || '');
                } catch (e) { }
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
            // If node is hidden (in collapsed namespace), expand its parent first
            if (node.style('display') === 'none') {
                var parentId = node.data('parent');
                if (parentId) {
                    _expandNamespace(parentId);
                }
            }
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
        _visibleRelationships = relationships;
        cy.edges().forEach(function (edge) {
            var rel = edge.data('relationship');
            var relOk = relationships.indexOf(rel) >= 0;
            // Respect collapse state: only show if both endpoints are visible
            var src = cy.getElementById(edge.data('source'));
            var tgt = cy.getElementById(edge.data('target'));
            var endpointsVisible = src.style('display') !== 'none' && tgt.style('display') !== 'none';
            if (relOk && endpointsVisible) {
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
        _visibleRelationships = ['Inherits', 'Implements', 'ConstructorDep', 'FieldDep'];
        // Respect collapse state: only show edges where both endpoints are visible
        cy.edges().forEach(function (edge) {
            var src = cy.getElementById(edge.data('source'));
            var tgt = cy.getElementById(edge.data('target'));
            if (src.style('display') !== 'none' && tgt.style('display') !== 'none') {
                edge.style('display', 'element');
            } else {
                edge.style('display', 'none');
            }
        });
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
        _expandedNamespace = null;
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
        collapseAll: function () { _collapseAll(); },
        expandNamespace: function (nsId) { _expandNamespace(nsId); },
        destroy: destroy
    };
})();
