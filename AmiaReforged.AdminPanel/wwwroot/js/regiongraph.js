/**
 * Region Graph Visual Editor — Cytoscape.js interop for the Regions admin page.
 * Uses compound (parent) nodes for region grouping, right-click context menus,
 * drag-and-drop reassignment, and POI badges on area nodes.
 *
 * Requires: cytoscape.js, cytoscape-fcose (optional, falls back to cose).
 */
// ===== Register optional Cytoscape extensions =====
(function () {
    try {
        if (typeof cytoscape !== 'undefined') {
            if (typeof cytoscapeFcose !== 'undefined') {
                cytoscape.use(cytoscapeFcose);
                console.log('[regionGraph] registered fcose extension');
            }
            if (typeof cytoscapeDagre !== 'undefined') {
                cytoscape.use(cytoscapeDagre);
                console.log('[regionGraph] registered dagre extension');
            }
        }
    } catch (e) {
        console.warn('[regionGraph] extension registration:', e);
    }
})();

window.regionGraph = (function () {
    /** @type {import('cytoscape').Core | null} */
    let cy = null;
    /** @type {DotNet.DotNetObject | null} */
    let dotNetRef = null;

    /** Currently active layout algorithm name */
    let _currentLayout = 'cose';

    // 12 distinct colors for region groups
    const REGION_PALETTE = [
        '#e6194b', '#3cb44b', '#4363d8', '#f58231',
        '#911eb4', '#42d4f4', '#f032e6', '#bfef45',
        '#fabed4', '#469990', '#dcbeff', '#9A6324'
    ];

    const COLORS = {
        unassigned: '#6b6b6b',
        unassignedBorder: '#444',
        nodeText: '#d0ccc2',
        edgeDoor: '#5ba4b5',
        edgeTrigger: '#4ecca3',
        highlightNode: '#ffc107',
        highlightEdge: '#ffc107',
        selectedNode: '#ff9800',
        regionParentBg: 'rgba(255,255,255,0.04)',
        regionParentBorder: 'rgba(255,255,255,0.15)'
    };

    /** Map of regionTag (lowercase) -> color */
    let regionColorMap = {};
    /** Currently open context menu element */
    let contextMenuEl = null;
    /** Document-level event handlers (for cleanup) */
    let _docClickHandler = null;
    let _docContextHandler = null;
    let _wrapperEl = null;

    /** Monotonic counter to detect stale progressive-render runs */
    let _initGeneration = 0;

    /** Batch size constants for progressive rendering */
    const BATCH_SIZE_NODES = 80;
    const BATCH_SIZE_EDGES = 200;

    /**
     * Yield control back to the browser so the UI thread can paint.
     * @returns {Promise<void>}
     */
    function _yieldFrame() {
        return new Promise(function (resolve) { requestAnimationFrame(resolve); });
    }

    /**
     * Report loading progress back to Blazor.
     * @param {number} percent 0-100
     * @param {string} phase description of current phase
     */
    function _reportProgress(percent, phase) {
        if (dotNetRef) {
            try {
                dotNetRef.invokeMethodAsync('OnGraphLoadProgress', Math.round(percent), phase);
            } catch (e) {
                // Blazor circuit may be disposed — ignore
            }
        }
    }

    /**
     * Add an array of elements to the Cytoscape instance in chunks, yielding
     * between each chunk so the browser stays responsive.
     * @param {Array} elements Cytoscape element descriptors
     * @param {number} batchSize Number of elements per chunk
     * @param {number} basePercent Progress percentage at start of this phase
     * @param {number} phaseWeight How much of the 0-100 range this phase occupies
     * @param {string} phaseLabel Human-readable label for progress reporting
     * @param {number} generation Init generation to check for cancellation
     * @returns {Promise<boolean>} false if cancelled (stale generation)
     */
    async function _addElementsProgressively(elements, batchSize, basePercent, phaseWeight, phaseLabel, generation) {
        var total = elements.length;
        if (total === 0) return true;

        for (var i = 0; i < total; i += batchSize) {
            // Check for cancellation (a newer init() was called)
            if (_initGeneration !== generation) return false;

            var chunk = elements.slice(i, Math.min(i + batchSize, total));
            cy.startBatch();
            cy.add(chunk);
            cy.endBatch();

            var pct = basePercent + ((i + chunk.length) / total) * phaseWeight;
            _reportProgress(pct, phaseLabel);

            // Yield to the browser between chunks
            await _yieldFrame();
        }
        return true;
    }

    /**
     * Initialize the region graph visual editor.
     * Uses progressive (chunked) rendering to keep the browser responsive.
     * @param {string} containerId DOM id of the container div
     * @param {string} nodesJson JSON array of AreaNodeDto (connected)
     * @param {string} edgesJson JSON array of AreaEdgeDto
     * @param {string} disconnectedJson JSON array of AreaNodeDto (disconnected)
     * @param {string} regionsJson JSON array of { tag, name, areaResRefs: string[], poiCounts: {resRef: count} }
     * @param {DotNet.DotNetObject} blazorRef .NET object ref for callbacks
     */
    function init(containerId, nodesJson, edgesJson, disconnectedJson, regionsJson, blazorRef) {
        console.log('[regionGraph] init v9 (progressive) called');
        destroy();
        dotNetRef = blazorRef || null;

        // Increment generation so any in-flight progressive render from a previous call stops
        var generation = ++_initGeneration;

        var container = document.getElementById(containerId);
        if (!container) {
            console.error('[regionGraph] Container not found:', containerId);
            return;
        }

        _wrapperEl = container.closest('.region-editor__graph-wrapper') || container.parentElement;

        try {

        _reportProgress(0, 'Parsing data...');

        var nodes = JSON.parse(nodesJson);
        var edges = JSON.parse(edgesJson);
        var disconnected = JSON.parse(disconnectedJson);
        var regions = JSON.parse(regionsJson);

        // Build region -> color map
        regionColorMap = {};
        regions.forEach(function (r, idx) {
            var tag = String(r.tag || '');
            if (typeof r.tag !== 'string') {
                console.warn('[regionGraph] region.tag is not a string:', typeof r.tag, JSON.stringify(r.tag), 'in region:', JSON.stringify(r));
            }
            regionColorMap[tag.toLowerCase()] = REGION_PALETTE[idx % REGION_PALETTE.length];
        });

        // Build resRef -> regionTag lookup and POI counts
        var areaToRegion = {};
        var poiCounts = {};
        regions.forEach(function (r) {
            var tag = String(r.tag || '');
            (r.areaResRefs || []).forEach(function (ref) {
                areaToRegion[ref.toLowerCase()] = tag;
            });
            var pc = r.poiCounts || {};
            Object.keys(pc).forEach(function (k) {
                poiCounts[k.toLowerCase()] = pc[k];
            });
        });

        // ===== Prepare elements (pure data transform, fast) =====

        var regionParentElements = [];
        var areaNodeElements = [];
        var edgeElements = [];

        // Region parent (compound) nodes
        regions.forEach(function (r) {
            var tag = String(r.tag || '');
            var name = String(r.name || '') || tag;
            var color = regionColorMap[tag.toLowerCase()] || COLORS.unassigned;
            regionParentElements.push({
                group: 'nodes',
                data: {
                    id: 'region_' + tag,
                    label: name,
                    regionTag: tag,
                    isRegionParent: 'yes',
                    regionColor: color
                }
            });
        });

        // Merge connected + disconnected into one node set
        var allNodes = nodes.concat(disconnected);
        var connectedSet = {};
        nodes.forEach(function (n) { connectedSet[n.resRef.toLowerCase()] = true; });

        allNodes.forEach(function (n) {
            var lowerRef = n.resRef.toLowerCase();
            var parentRegionTag = areaToRegion[lowerRef] || (n.region || '');
            var regionTag = parentRegionTag.toLowerCase();
            var color = regionColorMap[regionTag] || COLORS.unassigned;
            var borderColor = regionColorMap[regionTag] ? darken(color, 0.3) : COLORS.unassignedBorder;
            var isDisconnected = !connectedSet[lowerRef];
            var pois = poiCounts[lowerRef] || 0;

            var data = {
                id: n.resRef,
                label: n.name || n.resRef,
                resRef: n.resRef,
                region: parentRegionTag,
                hasSpawnProfile: n.hasSpawnProfile ? 'yes' : '',
                spawnProfileName: n.spawnProfileName || '',
                nodeColor: color,
                nodeBorder: borderColor,
                isDisconnected: isDisconnected ? 'yes' : '',
                poiCount: pois
            };

            // Set parent for compound grouping
            if (parentRegionTag) {
                data.parent = 'region_' + parentRegionTag;
            }

            areaNodeElements.push({
                group: 'nodes',
                data: data,
                position: { x: Math.random() * 800 - 400, y: Math.random() * 800 - 400 }
            });
        });

        edges.forEach(function (e, idx) {
            edgeElements.push({
                group: 'edges',
                data: {
                    id: 'e' + idx,
                    source: e.sourceResRef,
                    target: e.targetResRef,
                    transitionType: e.transitionType,
                    transitionTag: e.transitionTag
                }
            });
        });

        // ===== Create Cytoscape instance (empty, fast) =====

        cy = cytoscape({
            container: container,
            elements: [],
            minZoom: 0.1,
            maxZoom: 3,
            wheelSensitivity: 0.2,
            textureOnViewport: true,
            hideEdgesOnViewport: true,
            boxSelectionEnabled: true,
            // Reduce GPU work during progressive loading
            pixelRatio: 1,
            style: [
                // ===== Region parent (compound) nodes =====
                {
                    selector: 'node[isRegionParent = "yes"]',
                    style: {
                        'background-color': 'data(regionColor)',
                        'background-opacity': 0.08,
                        'border-color': 'data(regionColor)',
                        'border-width': 2,
                        'border-opacity': 0.4,
                        'border-style': 'dashed',
                        'label': 'data(label)',
                        'font-size': '10px',
                        'color': 'data(regionColor)',
                        'text-valign': 'top',
                        'text-halign': 'center',
                        'text-margin-y': -6,
                        'font-weight': 'bold',
                        'text-background-color': '#1a1a1a',
                        'text-background-opacity': 0.85,
                        'text-background-padding': '4px',
                        'text-background-shape': 'roundrectangle',
                        'padding': '8px',
                        'shape': 'roundrectangle',
                        'compound-sizing-wrt-labels': 'include',
                        'min-width': '60px',
                        'min-height': '40px'
                    }
                },
                // ===== Area nodes =====
                {
                    selector: 'node[isRegionParent != "yes"]',
                    style: {
                        'background-color': 'data(nodeColor)',
                        'border-color': 'data(nodeBorder)',
                        'border-width': 2,
                        'label': 'data(label)',
                        'font-size': '7px',
                        'color': COLORS.nodeText,
                        'text-valign': 'center',
                        'text-halign': 'center',
                        'text-wrap': 'wrap',
                        'text-max-width': '60px',
                        'width': 24,
                        'height': 24,
                        'text-background-color': '#26241f',
                        'text-background-opacity': 0.85,
                        'text-background-padding': '2px',
                        'text-background-shape': 'roundrectangle'
                    }
                },
                // ===== Disconnected area nodes (diamond) =====
                {
                    selector: 'node[isDisconnected = "yes"][isRegionParent != "yes"]',
                    style: {
                        'shape': 'diamond',
                        'width': 20,
                        'height': 20,
                        'font-size': '8px'
                    }
                },
                // ===== POI badge overlay =====
                {
                    selector: 'node[poiCount > 0][isRegionParent != "yes"]',
                    style: {
                        'label': function (ele) {
                            return ele.data('label') + '\n\u2605 ' + ele.data('poiCount');
                        },
                        'text-wrap': 'wrap',
                        'border-width': 3
                    }
                },
                // ===== Edges =====
                {
                    selector: 'edge',
                    style: {
                        'width': 1.2,
                        'curve-style': 'bezier',
                        'target-arrow-shape': 'triangle',
                        'target-arrow-color': COLORS.edgeDoor,
                        'line-color': COLORS.edgeDoor,
                        'arrow-scale': 0.7,
                        'opacity': 0.5
                    }
                },
                {
                    selector: 'edge[transitionType = "Trigger"]',
                    style: {
                        'line-color': COLORS.edgeTrigger,
                        'target-arrow-color': COLORS.edgeTrigger,
                        'line-style': 'dashed'
                    }
                },
                // ===== Highlight states =====
                {
                    selector: 'node.highlighted',
                    style: {
                        'background-color': COLORS.highlightNode,
                        'border-color': '#ff9800',
                        'border-width': 4,
                        'width': 36,
                        'height': 36,
                        'z-index': 999,
                        'font-size': '10px',
                        'font-weight': 'bold'
                    }
                },
                {
                    selector: 'node.region-highlight',
                    style: {
                        'border-width': 4,
                        'width': 34,
                        'height': 34,
                        'z-index': 998,
                        'font-size': '10px',
                        'font-weight': 'bold'
                    }
                },
                {
                    selector: 'node.selected-node',
                    style: {
                        'background-color': COLORS.selectedNode,
                        'border-color': '#e65100',
                        'border-width': 4,
                        'width': 36,
                        'height': 36,
                        'z-index': 999
                    }
                },
                {
                    selector: 'edge.highlighted',
                    style: {
                        'line-color': COLORS.highlightEdge,
                        'target-arrow-color': COLORS.highlightEdge,
                        'width': 2.5,
                        'opacity': 1,
                        'z-index': 998
                    }
                },
                {
                    selector: '.dimmed',
                    style: {
                        'opacity': 0.12
                    }
                },
                // ===== Drop target highlight =====
                {
                    selector: 'node.drop-target',
                    style: {
                        'background-opacity': 0.2,
                        'border-width': 3,
                        'border-opacity': 0.8,
                        'border-style': 'solid'
                    }
                }
            ],
            layout: { name: 'preset' }
        });

        // ===== Register event handlers immediately (before progressive add) =====
        _registerEventHandlers(container);

        // ===== Progressive element addition =====
        // Phase 1: Region parents (5%), Phase 2: Area nodes (60%), Phase 3: Edges (25%), Phase 4: Layout (10%)
        _renderProgressively(regionParentElements, areaNodeElements, edgeElements, generation);

        } catch (err) {
            console.error('[regionGraph] init() failed:', err);
            _reportProgress(100, 'Error');
        }
    }

    /**
     * Progressively add elements to the Cytoscape instance in chunks,
     * yielding to the browser between each chunk for responsiveness.
     */
    async function _renderProgressively(regionParentElements, areaNodeElements, edgeElements, generation) {
        try {
            // Phase 1: Add region parent nodes (few, add all at once)
            _reportProgress(2, 'Adding regions...');
            if (regionParentElements.length > 0) {
                cy.startBatch();
                cy.add(regionParentElements);
                cy.endBatch();
            }
            _reportProgress(5, 'Adding regions...');
            await _yieldFrame();

            if (_initGeneration !== generation) return; // cancelled

            // Phase 2: Add area nodes progressively
            var ok = await _addElementsProgressively(
                areaNodeElements, BATCH_SIZE_NODES,
                5,   // basePercent
                60,  // phaseWeight (5% -> 65%)
                'Adding areas...',
                generation
            );
            if (!ok) return; // cancelled

            // Phase 3: Add edges progressively
            ok = await _addElementsProgressively(
                edgeElements, BATCH_SIZE_EDGES,
                65,  // basePercent
                25,  // phaseWeight (65% -> 90%)
                'Adding edges...',
                generation
            );
            if (!ok) return; // cancelled

            // Phase 4: Run layout
            _reportProgress(90, 'Running layout...');
            await _yieldFrame();

            if (_initGeneration !== generation) return; // cancelled

            runLayout(_currentLayout);

            // Restore pixel ratio after loading is complete
            if (cy) {
                cy.renderer().pixelRatio = window.devicePixelRatio || 1;
                cy.resize();
            }

            _reportProgress(100, 'Complete');
            console.log('[regionGraph] progressive init complete — ' +
                regionParentElements.length + ' regions, ' +
                areaNodeElements.length + ' areas, ' +
                edgeElements.length + ' edges');
        } catch (err) {
            console.error('[regionGraph] progressive render failed:', err);
            _reportProgress(100, 'Error');
        }
    }

    /**
     * Register all Cytoscape event handlers on the current cy instance.
     * Separated from init() so it can be called before progressive rendering starts.
     */
    function _registerEventHandlers(container) {
        if (!cy) return;

        // ===== Node click — select =====
        cy.on('tap', 'node[isRegionParent != "yes"]', function (evt) {
            try {
                var node = evt.target;
                var data = node.data();
                console.log('[regionGraph] tap area:', data.label);

                cy.nodes().removeClass('selected-node');
                node.addClass('selected-node');

                if (dotNetRef) {
                    dotNetRef.invokeMethodAsync('OnGraphNodeSelected', JSON.stringify({
                        resRef: data.resRef,
                        name: data.label,
                        region: data.region,
                        hasSpawnProfile: data.hasSpawnProfile === 'yes',
                        spawnProfileName: data.spawnProfileName,
                        poiCount: data.poiCount || 0
                    }));
                }
            } catch (err) {
                console.error('[regionGraph] tap area error:', err);
            }
        });

        // ===== Region parent click =====
        cy.on('tap', 'node[isRegionParent = "yes"]', function (evt) {
            try {
                var node = evt.target;
                console.log('[regionGraph] tap region:', node.data('label'));
                cy.nodes().removeClass('selected-node');
                node.addClass('selected-node');

                if (dotNetRef) {
                    dotNetRef.invokeMethodAsync('OnRegionParentSelected', node.data('regionTag'));
                }
            } catch (err) {
                console.error('[regionGraph] tap region error:', err);
            }
        });

        // ===== Background click — deselect =====
        cy.on('tap', function (evt) {
            try {
                if (evt.target === cy) {
                    console.log('[regionGraph] tap background');
                    cy.nodes().removeClass('selected-node');
                    closeContextMenu();
                    if (dotNetRef) {
                        dotNetRef.invokeMethodAsync('OnGraphNodeDeselected');
                    }
                }
            } catch (err) {
                console.error('[regionGraph] tap background error:', err);
            }
        });

        // ===== Right-click context menu =====
        cy.on('cxttap', 'node[isRegionParent != "yes"]', function (evt) {
            try {
                if (evt.originalEvent) evt.originalEvent.preventDefault();
                var node = evt.target;
                var oe = evt.originalEvent || {};
                console.log('[regionGraph] cxttap area:', node.data('label'), 'clientX:', oe.clientX, 'clientY:', oe.clientY);
                showAreaContextMenu(node.data(), oe.clientX || 200, oe.clientY || 200);
            } catch (err) {
                console.error('[regionGraph] cxttap area error:', err);
            }
        });

        cy.on('cxttap', 'node[isRegionParent = "yes"]', function (evt) {
            try {
                if (evt.originalEvent) evt.originalEvent.preventDefault();
                var node = evt.target;
                var oe = evt.originalEvent || {};
                console.log('[regionGraph] cxttap region:', node.data('label'), 'clientX:', oe.clientX, 'clientY:', oe.clientY);
                showRegionContextMenu(node.data(), oe.clientX || 200, oe.clientY || 200);
            } catch (err) {
                console.error('[regionGraph] cxttap region error:', err);
            }
        });

        cy.on('cxttap', function (evt) {
            try {
                if (evt.target === cy) {
                    if (evt.originalEvent) evt.originalEvent.preventDefault();
                    var oe = evt.originalEvent || {};
                    console.log('[regionGraph] cxttap background clientX:', oe.clientX, 'clientY:', oe.clientY);
                    showBackgroundContextMenu(oe.clientX || 200, oe.clientY || 200);
                }
            } catch (err) {
                console.error('[regionGraph] cxttap background error:', err);
            }
        });

        // ===== Drag-and-drop reassignment =====
        var _userDragging = false;

        cy.on('grab', 'node[isRegionParent != "yes"]', function () {
            _userDragging = true;
        });

        cy.on('free', 'node[isRegionParent != "yes"]', function (evt) {
            if (!_userDragging) return;
            _userDragging = false;
            var node = evt.target;

            // Find the region parent closest to the drop position
            var nodePos = node.position();
            var regionParents = cy.nodes('[isRegionParent = "yes"]');
            var bestParent = null;
            var bestDist = Infinity;

            regionParents.forEach(function (rp) {
                var bb = rp.boundingBox();
                // Check if the node center is inside the parent bounding box
                if (nodePos.x >= bb.x1 && nodePos.x <= bb.x2 &&
                    nodePos.y >= bb.y1 && nodePos.y <= bb.y2) {
                    var cx = (bb.x1 + bb.x2) / 2;
                    var cy2 = (bb.y1 + bb.y2) / 2;
                    var dist = Math.sqrt(Math.pow(nodePos.x - cx, 2) + Math.pow(nodePos.y - cy2, 2));
                    if (dist < bestDist) {
                        bestDist = dist;
                        bestParent = rp;
                    }
                }
            });

            var currentParent = node.data('parent') || '';
            var currentRegion = node.data('region') || '';

            if (bestParent) {
                var newRegionTag = bestParent.data('regionTag');
                var newParentId = 'region_' + newRegionTag;

                if (currentParent !== newParentId) {
                    // Move to new parent
                    var color = regionColorMap[newRegionTag.toLowerCase()] || COLORS.unassigned;
                    node.move({ parent: newParentId });
                    node.data('region', newRegionTag);
                    node.data('nodeColor', color);
                    node.data('nodeBorder', darken(color, 0.3));
                    node.data('parent', newParentId);

                    if (dotNetRef) {
                        dotNetRef.invokeMethodAsync('OnNodeAssignedToRegion', node.data('resRef'), newRegionTag);
                    }
                }
            } else if (currentParent) {
                // Dropped outside all parents — unassign
                node.move({ parent: null });
                node.data('region', '');
                node.data('nodeColor', COLORS.unassigned);
                node.data('nodeBorder', COLORS.unassignedBorder);
                node.data('parent', '');

                if (dotNetRef) {
                    dotNetRef.invokeMethodAsync('OnNodeUnassigned', node.data('resRef'));
                }
            }

            // Clear any drop-target highlights
            regionParents.removeClass('drop-target');
        });

        // ===== Drag hover highlights on region parents =====
        cy.on('drag', 'node[isRegionParent != "yes"]', function (evt) {
            var node = evt.target;
            var nodePos = node.position();
            var regionParents = cy.nodes('[isRegionParent = "yes"]');

            regionParents.removeClass('drop-target');
            regionParents.forEach(function (rp) {
                var bb = rp.boundingBox();
                if (nodePos.x >= bb.x1 && nodePos.x <= bb.x2 &&
                    nodePos.y >= bb.y1 && nodePos.y <= bb.y2) {
                    rp.addClass('drop-target');
                }
            });
        });

        cy.on('dragfree', 'node[isRegionParent != "yes"]', function () {
            cy.nodes('[isRegionParent = "yes"]').removeClass('drop-target');
        });

        // Close context menu on click anywhere & suppress native menu inside graph
        _docClickHandler = function () { closeContextMenu(); };
        _docContextHandler = function (e) {
            if (container.contains(e.target)) {
                e.preventDefault();
            } else {
                closeContextMenu();
            }
        };
        document.addEventListener('click', _docClickHandler);
        document.addEventListener('contextmenu', _docContextHandler);

        console.log('[regionGraph] event handlers registered');
    }

    // ========== Context Menu Functions ==========

    // Inline styles applied to every menu (no CSS dependency)
    var MENU_STYLE = 'position:fixed;z-index:99999;min-width:180px;background:#2a2825;' +
        'border:1px solid #3a3733;border-radius:6px;padding:4px 0;' +
        'box-shadow:0 6px 24px rgba(0,0,0,0.5);font-size:14px;color:#d0ccc2;' +
        'font-family:inherit;';

    var ITEM_STYLE = 'display:flex;align-items:center;justify-content:space-between;' +
        'padding:6px 14px;cursor:pointer;color:#d0ccc2;white-space:nowrap;user-select:none;';

    var ITEM_DANGER_STYLE = ITEM_STYLE + 'color:#dc3545;';

    var DIVIDER_STYLE = 'height:1px;background:#3a3733;margin:4px 0;';

    function _createMenu(x, y) {
        closeContextMenu();
        var menu = document.createElement('div');
        menu.setAttribute('style', MENU_STYLE + 'left:' + x + 'px;top:' + y + 'px;');
        menu.className = 'region-context-menu';
        return menu;
    }

    function _attachMenu(menu) {
        document.body.appendChild(menu);
        contextMenuEl = menu;
        console.log('[regionGraph] menu attached, children:', menu.children.length,
            'rect:', JSON.stringify(menu.getBoundingClientRect()));
        // Clamp into viewport
        requestAnimationFrame(function () {
            if (!menu.parentNode) return;
            var r = menu.getBoundingClientRect();
            if (r.right > window.innerWidth - 4) {
                menu.style.left = Math.max(4, window.innerWidth - r.width - 4) + 'px';
            }
            if (r.bottom > window.innerHeight - 4) {
                menu.style.top = Math.max(4, window.innerHeight - r.height - 4) + 'px';
            }
        });
    }

    function showAreaContextMenu(nodeData, x, y) {
        var menu = _createMenu(x, y);
        var regionTag = nodeData.region || '';

        // "Edit in Panel"
        _addItem(menu, '\u270f\ufe0f Edit in Panel', function () {
            closeContextMenu();
            if (dotNetRef) {
                dotNetRef.invokeMethodAsync('OnGraphNodeSelected', JSON.stringify({
                    resRef: nodeData.resRef,
                    name: nodeData.label,
                    region: nodeData.region,
                    hasSpawnProfile: nodeData.hasSpawnProfile === 'yes',
                    spawnProfileName: nodeData.spawnProfileName,
                    poiCount: nodeData.poiCount || 0
                }));
            }
        });

        // "Highlight Connections"
        _addItem(menu, '\ud83d\udd17 Highlight Connections', function () {
            closeContextMenu();
            highlightNode(nodeData.resRef);
        });

        _addDivider(menu);

        // "Assign to Region" with hover submenu
        var assignItem = document.createElement('div');
        assignItem.setAttribute('style', ITEM_STYLE + 'position:relative;');
        assignItem.innerHTML = '\ud83d\udccc Assign to Region <span style="margin-left:10px;font-size:0.7rem;opacity:0.5">\u25b8</span>';

        var submenu = document.createElement('div');
        submenu.setAttribute('style',
            'display:none;position:absolute;left:100%;top:-4px;min-width:160px;' +
            'background:#2a2825;border:1px solid #3a3733;border-radius:6px;padding:4px 0;' +
            'box-shadow:0 6px 24px rgba(0,0,0,0.5);max-height:280px;overflow-y:auto;');

        var regionTags = Object.keys(regionColorMap);
        if (regionTags.length === 0) {
            var empty = document.createElement('div');
            empty.setAttribute('style', 'padding:5px 12px;color:#888;font-size:13px;white-space:nowrap;');
            empty.textContent = 'No regions defined';
            submenu.appendChild(empty);
        } else {
            regionTags.forEach(function (tag) {
                var color = regionColorMap[tag];
                var si = document.createElement('div');
                si.setAttribute('style',
                    'display:flex;align-items:center;gap:6px;padding:5px 12px;cursor:pointer;' +
                    'color:#d0ccc2;font-size:13px;transition:background 0.1s;white-space:nowrap;');
                si.innerHTML = '<span style="width:8px;height:8px;border-radius:2px;flex-shrink:0;background:' + color + '"></span>' + tag;
                si.addEventListener('mouseenter', function () { si.style.background = 'rgba(201,168,76,0.15)'; });
                si.addEventListener('mouseleave', function () { si.style.background = ''; });
                si.addEventListener('click', function (e) {
                    e.stopPropagation();
                    closeContextMenu();
                    assignNodeToRegion(nodeData.resRef, tag);
                });
                submenu.appendChild(si);
            });
        }

        assignItem.appendChild(submenu);
        assignItem.addEventListener('mouseenter', function () {
            submenu.style.display = 'block';
            assignItem.style.background = 'rgba(201,168,76,0.15)';
        });
        assignItem.addEventListener('mouseleave', function () {
            submenu.style.display = 'none';
            assignItem.style.background = '';
        });
        menu.appendChild(assignItem);

        // "Remove from Region" (only if assigned)
        if (regionTag) {
            _addItem(menu, '\u274c Remove from Region', function () {
                closeContextMenu();
                unassignNode(nodeData.resRef);
            }, true);
        }

        _attachMenu(menu);
    }

    function showRegionContextMenu(nodeData, x, y) {
        var menu = _createMenu(x, y);

        _addItem(menu, '\u270f\ufe0f Edit Region', function () {
            closeContextMenu();
            if (dotNetRef) {
                dotNetRef.invokeMethodAsync('OnRegionParentSelected', nodeData.regionTag);
            }
        });

        _addItem(menu, '\ud83d\udd0d Highlight All Areas', function () {
            closeContextMenu();
            highlightRegion(nodeData.label);
        });

        _addDivider(menu);

        _addItem(menu, '\ud83d\uddd1\ufe0f Delete Region', function () {
            closeContextMenu();
            if (dotNetRef) {
                dotNetRef.invokeMethodAsync('OnContextMenuAction', 'deleteRegion', nodeData.regionTag);
            }
        }, true);

        _attachMenu(menu);
    }

    function showBackgroundContextMenu(x, y) {
        var menu = _createMenu(x, y);

        _addItem(menu, '\u2795 New Region', function () {
            closeContextMenu();
            if (dotNetRef) {
                dotNetRef.invokeMethodAsync('OnContextMenuAction', 'newRegion', '');
            }
        });

        _addItem(menu, '\ud83d\udd0e Show Unassigned', function () {
            closeContextMenu();
            highlightOrphans();
        });

        _addDivider(menu);

        _addItem(menu, '\ud83d\udcd0 Fit View', function () {
            closeContextMenu();
            fitView();
        });

        _addItem(menu, '\ud83e\uddf9 Clear Highlights', function () {
            closeContextMenu();
            clearHighlight();
            if (dotNetRef) {
                dotNetRef.invokeMethodAsync('OnGraphNodeDeselected');
            }
        });

        _attachMenu(menu);
    }

    function _addItem(menu, text, handler, isDanger) {
        var item = document.createElement('div');
        item.setAttribute('style', isDanger ? ITEM_DANGER_STYLE : ITEM_STYLE);
        item.textContent = text;
        item.addEventListener('mouseenter', function () {
            item.style.background = isDanger ? 'rgba(220,53,69,0.15)' : 'rgba(201,168,76,0.15)';
        });
        item.addEventListener('mouseleave', function () { item.style.background = ''; });
        item.addEventListener('click', function (e) {
            e.stopPropagation();
            handler();
        });
        menu.appendChild(item);
    }

    function _addDivider(menu) {
        var d = document.createElement('div');
        d.setAttribute('style', DIVIDER_STYLE);
        menu.appendChild(d);
    }

    function closeContextMenu() {
        if (contextMenuEl && contextMenuEl.parentNode) {
            contextMenuEl.parentNode.removeChild(contextMenuEl);
        }
        contextMenuEl = null;
    }

    // ========== Node Assignment Operations ==========

    function assignNodeToRegion(resRef, regionTag) {
        if (!cy) return;

        var node = cy.getElementById(resRef);
        if (!node || node.length === 0) return;

        var lowerTag = regionTag.toLowerCase();
        var color = regionColorMap[lowerTag] || COLORS.unassigned;

        node.move({ parent: 'region_' + regionTag });
        node.data('region', regionTag);
        node.data('nodeColor', color);
        node.data('nodeBorder', darken(color, 0.3));

        if (dotNetRef) {
            dotNetRef.invokeMethodAsync('OnNodeAssignedToRegion', resRef, regionTag);
        }
    }

    function unassignNode(resRef) {
        if (!cy) return;

        var node = cy.getElementById(resRef);
        if (!node || node.length === 0) return;

        node.move({ parent: null });
        node.data('region', '');
        node.data('nodeColor', COLORS.unassigned);
        node.data('nodeBorder', COLORS.unassignedBorder);

        if (dotNetRef) {
            dotNetRef.invokeMethodAsync('OnNodeUnassigned', resRef);
        }
    }

    // ========== Region Parent Operations ==========

    /**
     * Add a new region compound (parent) node.
     */
    function addRegionParent(regionTag, regionName, regionTagsJson) {
        if (!cy) return;

        var regionTags = JSON.parse(regionTagsJson);
        regionColorMap = {};
        regionTags.forEach(function (tag, idx) {
            regionColorMap[tag.toLowerCase()] = REGION_PALETTE[idx % REGION_PALETTE.length];
        });

        var color = regionColorMap[regionTag.toLowerCase()] || COLORS.unassigned;

        cy.add({
            group: 'nodes',
            data: {
                id: 'region_' + regionTag,
                label: regionName || regionTag,
                regionTag: regionTag,
                isRegionParent: 'yes',
                regionColor: color
            },
            position: { x: cy.width() / 2, y: cy.height() / 2 }
        });
    }

    /**
     * Remove a region compound parent and unparent its children.
     */
    function removeRegionParent(regionTag) {
        if (!cy) return;

        var parentId = 'region_' + regionTag;
        var parent = cy.getElementById(parentId);
        if (!parent || parent.length === 0) return;

        // Unparent children
        var children = parent.children();
        children.forEach(function (child) {
            child.move({ parent: null });
            child.data('region', '');
            child.data('nodeColor', COLORS.unassigned);
            child.data('nodeBorder', COLORS.unassignedBorder);
        });

        cy.remove(parent);
    }

    /**
     * Move a specific node to a region parent.
     */
    function moveNodeToRegion(resRef, regionTag) {
        if (!cy) return;

        var node = cy.getElementById(resRef);
        if (!node || node.length === 0) return;

        var lowerTag = (regionTag || '').toLowerCase();
        var parentId = regionTag ? 'region_' + regionTag : null;
        var color = regionColorMap[lowerTag] || COLORS.unassigned;
        var borderColor = regionColorMap[lowerTag] ? darken(color, 0.3) : COLORS.unassignedBorder;

        node.move({ parent: parentId });
        node.data('region', regionTag || '');
        node.data('nodeColor', color);
        node.data('nodeBorder', borderColor);
    }

    /**
     * Update the POI badge count on a node.
     */
    function updateNodePoiBadge(resRef, count) {
        if (!cy) return;
        var node = cy.getElementById(resRef);
        if (node && node.length > 0) {
            node.data('poiCount', count);
        }
    }

    // ========== Highlight Functions ==========

    function highlightRegion(regionName) {
        if (!cy) return;
        cy.elements().removeClass('highlighted dimmed region-highlight');

        if (!regionName) return;

        var lowerName = regionName.toLowerCase();
        var matched = cy.nodes('[isRegionParent != "yes"]').filter(function (n) {
            return (n.data('region') || '').toLowerCase() === lowerName;
        });

        if (matched.length === 0) return;

        var neighborhood = matched.closedNeighborhood();
        cy.elements().addClass('dimmed');
        neighborhood.removeClass('dimmed');
        // Also un-dim the region parent
        cy.nodes('[isRegionParent = "yes"]').forEach(function (rp) {
            if (rp.data('label').toLowerCase() === lowerName || rp.data('regionTag').toLowerCase() === lowerName) {
                rp.removeClass('dimmed');
            }
        });
        matched.addClass('region-highlight');
        matched.connectedEdges().addClass('highlighted');

        cy.animate({
            fit: { eles: matched, padding: 80 },
            duration: 400
        });
    }

    function highlightOrphans() {
        if (!cy) return;
        cy.elements().removeClass('highlighted dimmed region-highlight');

        var orphans = cy.nodes('[isRegionParent != "yes"]').filter(function (n) {
            return !n.data('region') || n.data('region') === '';
        });

        if (orphans.length === 0) return;

        cy.elements().addClass('dimmed');
        orphans.removeClass('dimmed');
        orphans.addClass('highlighted');

        cy.animate({
            fit: { eles: orphans, padding: 60 },
            duration: 400
        });
    }

    function highlightNode(query) {
        if (!cy) return false;
        cy.elements().removeClass('highlighted dimmed region-highlight');
        if (!query || query.trim() === '') return false;

        var lowerQuery = query.toLowerCase();
        var matched = cy.nodes('[isRegionParent != "yes"]').filter(function (n) {
            return n.data('resRef').toLowerCase().indexOf(lowerQuery) !== -1 ||
                   n.data('label').toLowerCase().indexOf(lowerQuery) !== -1;
        });

        if (matched.length === 0) return false;

        var neighborhood = matched.closedNeighborhood();
        cy.elements().addClass('dimmed');
        neighborhood.removeClass('dimmed');
        matched.addClass('highlighted');
        matched.connectedEdges().addClass('highlighted');

        cy.animate({
            fit: { eles: neighborhood, padding: 60 },
            duration: 400
        });

        return true;
    }

    function clearHighlight() {
        if (!cy) return;
        cy.elements().removeClass('highlighted dimmed region-highlight selected-node drop-target');
    }

    /**
     * Update region assignments and colors on existing nodes in-place.
     * Also updates compound parent relationships.
     * @param {string} regionsJson JSON array of { tag, name, areaResRefs, poiCounts }
     */
    function updateRegionData(regionsJson) {
        if (!cy) return;

        var regions = JSON.parse(regionsJson);

        // Rebuild region -> color map
        regionColorMap = {};
        regions.forEach(function (r, idx) {
            var tag = String(r.tag || '');
            regionColorMap[tag.toLowerCase()] = REGION_PALETTE[idx % REGION_PALETTE.length];
        });

        // Build resRef -> regionTag and POI counts
        var areaToRegion = {};
        var poiCounts = {};
        regions.forEach(function (r) {
            var tag = String(r.tag || '');
            (r.areaResRefs || []).forEach(function (ref) {
                areaToRegion[ref.toLowerCase()] = tag;
            });
            var pc = r.poiCounts || {};
            Object.keys(pc).forEach(function (k) {
                poiCounts[k.toLowerCase()] = pc[k];
            });
        });

        cy.startBatch();

        // Sync region parent nodes
        var existingParents = {};
        cy.nodes('[isRegionParent = "yes"]').forEach(function (rp) {
            existingParents[rp.data('regionTag').toLowerCase()] = rp;
        });

        regions.forEach(function (r) {
            var tag = String(r.tag || '');
            var name = String(r.name || '') || tag;
            var lowerTag = tag.toLowerCase();
            var color = regionColorMap[lowerTag] || COLORS.unassigned;
            if (existingParents[lowerTag]) {
                existingParents[lowerTag].data('label', name);
                existingParents[lowerTag].data('regionColor', color);
                delete existingParents[lowerTag];
            } else {
                cy.add({
                    group: 'nodes',
                    data: {
                        id: 'region_' + tag,
                        label: name,
                        regionTag: tag,
                        isRegionParent: 'yes',
                        regionColor: color
                    }
                });
            }
        });

        // Remove stale region parents
        Object.keys(existingParents).forEach(function (tag) {
            var rp = existingParents[tag];
            rp.children().forEach(function (child) {
                child.move({ parent: null });
                child.data('region', '');
                child.data('nodeColor', COLORS.unassigned);
                child.data('nodeBorder', COLORS.unassignedBorder);
            });
            cy.remove(rp);
        });

        // Update area nodes
        cy.nodes('[isRegionParent != "yes"]').forEach(function (node) {
            var resRef = node.data('resRef');
            var lowerRef = resRef.toLowerCase();
            var newRegion = areaToRegion[lowerRef] || '';
            var regionTag = newRegion.toLowerCase();
            var color = regionColorMap[regionTag] || COLORS.unassigned;
            var borderColor = regionColorMap[regionTag] ? darken(color, 0.3) : COLORS.unassignedBorder;
            var newParent = newRegion ? 'region_' + newRegion : null;

            var currentParent = node.data('parent') || null;
            if (currentParent !== newParent) {
                node.move({ parent: newParent });
            }

            node.data('region', newRegion);
            node.data('nodeColor', color);
            node.data('nodeBorder', borderColor);
            node.data('poiCount', poiCounts[lowerRef] || 0);
        });

        cy.endBatch();
    }

    /**
     * Update a single node's region assignment in-place.
     */
    function updateSingleNode(resRef, regionTag, regionTagsJson) {
        if (!cy) return;

        var regionTags = JSON.parse(regionTagsJson);
        regionColorMap = {};
        regionTags.forEach(function (tag, idx) {
            regionColorMap[tag.toLowerCase()] = REGION_PALETTE[idx % REGION_PALETTE.length];
        });

        var node = cy.getElementById(resRef);
        if (node && node.length > 0) {
            var lowerTag = (regionTag || '').toLowerCase();
            var color = regionColorMap[lowerTag] || COLORS.unassigned;
            var borderColor = regionColorMap[lowerTag] ? darken(color, 0.3) : COLORS.unassignedBorder;
            var newParent = regionTag ? 'region_' + regionTag : null;

            node.move({ parent: newParent });
            node.data('region', regionTag || '');
            node.data('nodeColor', color);
            node.data('nodeBorder', borderColor);
        }
    }

    function fitView() {
        if (!cy) return;
        cy.animate({
            fit: { padding: 30 },
            duration: 300
        });
    }

    function resizeGraph() {
        if (!cy) return;
        cy.resize();
        cy.fit(undefined, 30);
    }

    /**
     * Extract a grouping prefix from an area label/resRef.
     * Strips trailing segment after the last underscore to cluster
     * similarly-named areas (e.g. "ab_wastes_cave" + "ab_wastes_main" → "ab_wastes").
     * Falls back to the full name if there's no underscore.
     */
    function _namePrefix(node) {
        var label = (node.data('resRef') || node.data('label') || node.data('id') || '').toLowerCase();
        var lastUnderscore = label.lastIndexOf('_');
        if (lastUnderscore > 0) {
            return label.substring(0, lastUnderscore);
        }
        return label;
    }

    /**
     * Sort a collection of nodes for placement in the grid.
     * @param {cytoscape.Collection} children Nodes to sort
     * @param {cytoscape.Core} cyRef Cytoscape instance (for edge queries)
     * @param {string} mode 'alpha' | 'degree' | 'proximity'
     * @returns {Array} Ordered array of node references
     */
    function _sortBlockChildren(children, cyRef, mode) {
        var arr = [];
        children.forEach(function (n) { arr.push(n); });

        if (mode === 'degree') {
            // Most-connected nodes first
            arr.sort(function (a, b) {
                var diff = b.degree() - a.degree();
                if (diff !== 0) return diff;
                return (a.data('label') || '').toLowerCase() < (b.data('label') || '').toLowerCase() ? -1 : 1;
            });
            return arr;
        }

        if (mode === 'proximity') {
            // BFS walk within the block's internal subgraph
            var idSet = {};
            arr.forEach(function (n) { idSet[n.id()] = true; });

            // Build adjacency list for edges where both endpoints are in this block
            var adj = {};
            arr.forEach(function (n) { adj[n.id()] = []; });
            cyRef.edges().forEach(function (e) {
                var s = e.source().id();
                var t = e.target().id();
                if (idSet[s] && idSet[t]) {
                    adj[s].push(t);
                    adj[t].push(s);
                }
            });

            // Start BFS from highest-degree node in the block
            arr.sort(function (a, b) { return b.degree() - a.degree(); });
            var start = arr[0];
            var visited = {};
            var ordered = [];
            var queue = [start.id()];
            visited[start.id()] = true;

            while (queue.length > 0) {
                var cur = queue.shift();
                ordered.push(cur);
                // Sort neighbors by degree descending for stable traversal
                var neighbors = (adj[cur] || []).slice();
                neighbors.sort(function (a, b) {
                    return cyRef.getElementById(b).degree() - cyRef.getElementById(a).degree();
                });
                neighbors.forEach(function (nb) {
                    if (!visited[nb]) {
                        visited[nb] = true;
                        queue.push(nb);
                    }
                });
            }

            // Append any unreached nodes (disconnected within block) alphabetically
            arr.forEach(function (n) {
                if (!visited[n.id()]) {
                    ordered.push(n.id());
                }
            });

            return ordered.map(function (id) { return cyRef.getElementById(id); });
        }

        // Default: alphabetical
        arr.sort(function (a, b) {
            var la = (a.data('label') || '').toLowerCase();
            var lb = (b.data('label') || '').toLowerCase();
            return la < lb ? -1 : (la > lb ? 1 : 0);
        });
        return arr;
    }

    /**
     * Sort region blocks for the outer grid placement.
     * @param {Array} blocks Array of {parent, children, count, name, isOrphanGroup}
     * @param {cytoscape.Core} cyRef Cytoscape instance
     * @param {string} mode 'size' | 'alpha' | 'connectivity'
     */
    function _sortRegionBlocks(blocks, cyRef, mode) {
        if (mode === 'alpha') {
            blocks.sort(function (a, b) {
                var na = (a.name || '').toLowerCase();
                var nb = (b.name || '').toLowerCase();
                return na < nb ? -1 : (na > nb ? 1 : 0);
            });
            return;
        }

        if (mode === 'connectivity') {
            // Build block-index lookup: nodeId -> blockIndex
            var nodeToBlock = {};
            blocks.forEach(function (block, idx) {
                block.children.forEach(function (n) {
                    nodeToBlock[n.id()] = idx;
                });
            });

            // Count cross-block edges between each pair of blocks
            var crossEdges = {}; // 'i-j' -> count
            cyRef.edges().forEach(function (e) {
                var si = nodeToBlock[e.source().id()];
                var ti = nodeToBlock[e.target().id()];
                if (si != null && ti != null && si !== ti) {
                    var key = Math.min(si, ti) + '-' + Math.max(si, ti);
                    crossEdges[key] = (crossEdges[key] || 0) + 1;
                }
            });

            // Greedy nearest-neighbor: start with the block with most total cross-edges
            var blockScores = blocks.map(function (_, idx) {
                var total = 0;
                Object.keys(crossEdges).forEach(function (key) {
                    var parts = key.split('-');
                    if (parseInt(parts[0]) === idx || parseInt(parts[1]) === idx) {
                        total += crossEdges[key];
                    }
                });
                return total;
            });

            var placed = {};
            var order = [];
            // Start with highest cross-edge block
            var startIdx = 0;
            var maxScore = -1;
            blockScores.forEach(function (s, i) {
                if (s > maxScore) { maxScore = s; startIdx = i; }
            });
            order.push(startIdx);
            placed[startIdx] = true;

            while (order.length < blocks.length) {
                // Find unplaced block with most edges to already-placed blocks
                var bestIdx = -1;
                var bestCount = -1;
                blocks.forEach(function (_, ci) {
                    if (placed[ci]) return;
                    var count = 0;
                    order.forEach(function (pi) {
                        var key = Math.min(ci, pi) + '-' + Math.max(ci, pi);
                        count += crossEdges[key] || 0;
                    });
                    if (count > bestCount || (count === bestCount && bestIdx === -1)) {
                        bestCount = count;
                        bestIdx = ci;
                    }
                });
                if (bestIdx === -1) {
                    // Pick first unplaced
                    blocks.forEach(function (_, ci) {
                        if (!placed[ci] && bestIdx === -1) bestIdx = ci;
                    });
                }
                order.push(bestIdx);
                placed[bestIdx] = true;
            }

            // Reorder blocks array in-place
            var reordered = order.map(function (i) { return blocks[i]; });
            for (var i = 0; i < blocks.length; i++) {
                blocks[i] = reordered[i];
            }
            return;
        }

        // Default: by size (largest first)
        blocks.sort(function (a, b) { return b.count - a.count; });
    }

    /**
     * Custom compact grid layout: places area nodes in tight grids within each
     * region group, then arranges those region blocks in an outer grid.
     * Unregioned (orphan) areas are grouped by name prefix so similarly-named
     * areas cluster together, with small gaps between prefix groups.
     * Compound parent bounding boxes are auto-computed by Cytoscape.
     *
     * Config options:
     *   nodeSpacing (int), regionGap (int), orphanGroupGap (int),
     *   innerSort ('alpha'|'degree'|'proximity'), outerSort ('size'|'alpha'|'connectivity')
     */
    function _runCompactGridLayout(cyRef, config) {
        var cfg = config || {};
        var nodeSpacing = cfg.nodeSpacing || 32;
        var regionGap = cfg.regionGap || 24;
        var orphanGroupGap = cfg.orphanGroupGap || 12;
        var innerSort = cfg.innerSort || 'proximity';
        var outerSort = cfg.outerSort || 'size';

        // Gather region parents and their children
        var regionParents = cyRef.nodes('[isRegionParent = "yes"]');
        var regionBlocks = [];

        regionParents.forEach(function (rp) {
            var children = rp.children();
            if (children.length === 0) return;
            regionBlocks.push({ parent: rp, children: children, count: children.length, name: rp.data('label') || '' });
        });

        // Orphan nodes (no parent)
        var orphans = cyRef.nodes('[isRegionParent != "yes"]').filter(function (n) {
            return n.parent().length === 0;
        });

        // Group orphans by name prefix
        if (orphans.length > 0) {
            var prefixMap = {};
            orphans.forEach(function (n) {
                var prefix = _namePrefix(n);
                if (!prefixMap[prefix]) {
                    prefixMap[prefix] = [];
                }
                prefixMap[prefix].push(n);
            });

            var prefixKeys = Object.keys(prefixMap).sort();

            prefixKeys.forEach(function (prefix) {
                var nodes = prefixMap[prefix];
                regionBlocks.push({
                    parent: null,
                    children: cyRef.collection().merge(nodes),
                    count: nodes.length,
                    name: prefix,
                    isOrphanGroup: true
                });
            });
        }

        // Sort region blocks by outer sort mode
        _sortRegionBlocks(regionBlocks, cyRef, outerSort);

        // Outer grid: number of columns
        var outerCols = Math.ceil(Math.sqrt(regionBlocks.length));

        // Track current outer grid position
        var outerX = 0;
        var outerY = 0;
        var col = 0;
        var rowMaxH = 0;

        regionBlocks.forEach(function (block) {
            // Sort children within this block
            var sortedChildren = _sortBlockChildren(block.children, cyRef, innerSort);

            var n = sortedChildren.length;
            var innerCols = Math.ceil(Math.sqrt(n));
            var innerRows = Math.ceil(n / innerCols);

            // Position each child in a tight grid within this block
            sortedChildren.forEach(function (child, i) {
                var r = Math.floor(i / innerCols);
                var c = i % innerCols;
                child.position({
                    x: outerX + c * nodeSpacing,
                    y: outerY + r * nodeSpacing
                });
            });

            var blockW = innerCols * nodeSpacing;
            var blockH = innerRows * nodeSpacing;
            rowMaxH = Math.max(rowMaxH, blockH);

            // Orphan sub-groups use a smaller gap between them
            var gap = block.isOrphanGroup ? orphanGroupGap : regionGap;
            outerX += blockW + gap;
            col++;

            if (col >= outerCols) {
                col = 0;
                outerX = 0;
                outerY += rowMaxH + regionGap;
                rowMaxH = 0;
            }
        });

        // Fit the whole graph into the viewport with some padding
        cyRef.fit(undefined, 20);
    }

    function destroy() {
        closeContextMenu();
        if (_docClickHandler) {
            document.removeEventListener('click', _docClickHandler);
            _docClickHandler = null;
        }
        if (_docContextHandler) {
            document.removeEventListener('contextmenu', _docContextHandler);
            _docContextHandler = null;
        }
        if (cy) {
            cy.destroy();
            cy = null;
        }
        dotNetRef = null;
        regionColorMap = {};
        _wrapperEl = null;
    }

    function getRegionColors() {
        return regionColorMap;
    }

    function darken(hex, factor) {
        hex = hex.replace('#', '');
        var r = parseInt(hex.substring(0, 2), 16);
        var g = parseInt(hex.substring(2, 4), 16);
        var b = parseInt(hex.substring(4, 6), 16);
        r = Math.round(r * (1 - factor));
        g = Math.round(g * (1 - factor));
        b = Math.round(b * (1 - factor));
        return '#' + ((1 << 24) + (r << 16) + (g << 8) + b).toString(16).slice(1);
    }

    /**
     * Run a named layout algorithm on the current graph.
     * Uses frame-spread animation for force-directed layouts to avoid hanging.
     * @param {string} name Layout name: compact-grid, cose, fcose, dagre, grid, circle, concentric, breadthfirst
     */
    function runLayout(name, config) {
        if (!cy) return;
        _currentLayout = name || 'compact-grid';
        var cfg = config || {};

        // Stop any running layout before starting a new one
        if (cy._runningLayout) {
            try { cy._runningLayout.stop(); } catch (e) {}
            cy._runningLayout = null;
        }

        if (_currentLayout === 'compact-grid') {
            _reportProgress(92, 'Laying out grid...');
            _runCompactGridLayout(cy, cfg);
            _reportProgress(100, 'Complete');
            return;
        }

        var totalNodes = cy.nodes('[isRegionParent != "yes"]').length;
        var pad = cfg.padding != null ? cfg.padding : 15;
        var opts;

        // For force-directed layouts with large graphs, use per-frame animation
        // (animate: true) instead of 'end' (compute all synchronously then jump).
        // Also reduce iterations to avoid long compute times.
        var isLargeGraph = totalNodes > 150;

        switch (_currentLayout) {
            case 'fcose': {
                var hasFcose = false;
                try { hasFcose = !!cytoscape && cy.layout({ name: 'fcose' }); hasFcose = true; } catch (e) { hasFcose = false; }
                if (!hasFcose) {
                    console.warn('[regionGraph] fcose not available, falling back to cose');
                    _currentLayout = 'cose';
                } else {
                    var defRepF = totalNodes > 200 ? 2000 : 4500;
                    var defEdgF = totalNodes > 200 ? 30 : 50;
                    var defGravF = totalNodes > 200 ? 1.5 : 0.5;
                    var repF = cfg.nodeRepulsion != null ? cfg.nodeRepulsion : defRepF;
                    var edgF = cfg.idealEdgeLength != null ? cfg.idealEdgeLength : defEdgF;
                    var gravF = cfg.gravity != null ? cfg.gravity : defGravF;
                    var nestF = cfg.nestingFactor != null ? cfg.nestingFactor : 0.15;
                    var defIterF = isLargeGraph ? 1500 : 2500;
                    var iterF = cfg.numIter != null ? cfg.numIter : defIterF;
                    opts = {
                        name: 'fcose',
                        quality: 'default',
                        randomize: true,
                        animate: true,
                        animationDuration: isLargeGraph ? 800 : 500,
                        nodeRepulsion: function () { return repF; },
                        idealEdgeLength: function () { return edgF; },
                        edgeElasticity: function () { return 0.45; },
                        nestingFactor: nestF,
                        gravity: gravF,
                        gravityCompound: gravF * 2,
                        numIter: iterF,
                        fit: true,
                        padding: pad,
                        nodeDimensionsIncludeLabels: false
                    };
                    break;
                }
            }
            // falls through to cose if fcose unavailable
            case 'cose': {
                var defRep = totalNodes > 200 ? 800 : (totalNodes > 50 ? 1200 : 1800);
                var defEdg = totalNodes > 200 ? 18 : (totalNodes > 50 ? 25 : 35);
                var defGrav = totalNodes > 200 ? 3.0 : (totalNodes > 50 ? 2.0 : 1.5);
                var rep = cfg.nodeRepulsion != null ? cfg.nodeRepulsion : defRep;
                var edg = cfg.idealEdgeLength != null ? cfg.idealEdgeLength : defEdg;
                var grav = cfg.gravity != null ? cfg.gravity : defGrav;
                var nest = cfg.nestingFactor != null ? cfg.nestingFactor : 0.15;
                var defIter = isLargeGraph ? 400 : 800;
                var iter = cfg.numIter != null ? cfg.numIter : defIter;
                opts = {
                    name: 'cose',
                    animate: true,
                    animationDuration: isLargeGraph ? 800 : 500,
                    nodeRepulsion: function () { return rep; },
                    idealEdgeLength: function () { return edg; },
                    edgeElasticity: function () { return 32; },
                    gravity: grav,
                    gravityCompound: grav * 1.5,
                    gravityRange: 1.5,
                    gravityRangeCompound: 2.0,
                    nestingFactor: nest,
                    numIter: iter,
                    padding: pad,
                    randomize: true,
                    nodeDimensionsIncludeLabels: false,
                    fit: true
                };
                break;
            }
            case 'dagre': {
                var hasDagre = false;
                try { hasDagre = !!cytoscape && cy.layout({ name: 'dagre' }); hasDagre = true; } catch (e) { hasDagre = false; }
                if (!hasDagre) {
                    console.warn('[regionGraph] dagre not available, falling back to breadthfirst');
                    _currentLayout = 'breadthfirst';
                } else {
                    opts = {
                        name: 'dagre',
                        rankDir: cfg.rankDir || 'LR',
                        nodeSep: cfg.nodeSep != null ? cfg.nodeSep : 18,
                        rankSep: cfg.rankSep != null ? cfg.rankSep : 35,
                        edgeSep: cfg.edgeSep != null ? cfg.edgeSep : 10,
                        animate: true,
                        animationDuration: 500,
                        fit: true,
                        padding: pad
                    };
                    break;
                }
            }
            // falls through to breadthfirst if dagre unavailable
            case 'breadthfirst': {
                var defSpace = totalNodes > 200 ? 0.4 : (totalNodes > 50 ? 0.6 : 0.75);
                opts = {
                    name: 'breadthfirst',
                    directed: cfg.directed != null ? cfg.directed : true,
                    spacingFactor: cfg.spacingFactor != null ? cfg.spacingFactor : defSpace,
                    animate: true,
                    animationDuration: 500,
                    fit: true,
                    padding: pad,
                    avoidOverlap: true
                };
                break;
            }
            case 'grid': {
                opts = {
                    name: 'grid',
                    animate: true,
                    animationDuration: 500,
                    fit: true,
                    padding: pad,
                    avoidOverlap: true,
                    condense: cfg.condense != null ? cfg.condense : true,
                    rows: cfg.rows != null && cfg.rows > 0 ? cfg.rows : undefined
                };
                break;
            }
            case 'circle': {
                opts = {
                    name: 'circle',
                    animate: true,
                    animationDuration: 500,
                    fit: true,
                    padding: pad,
                    avoidOverlap: true
                };
                break;
            }
            case 'concentric': {
                var defLW = cfg.levelWidth != null ? cfg.levelWidth : 2;
                opts = {
                    name: 'concentric',
                    animate: true,
                    animationDuration: 500,
                    fit: true,
                    padding: pad,
                    avoidOverlap: true,
                    concentric: function (node) { return node.degree(); },
                    levelWidth: function () { return defLW; },
                    minNodeSpacing: cfg.minNodeSpacing != null ? cfg.minNodeSpacing : 10
                };
                break;
            }
            default: {
                console.warn('[regionGraph] unknown layout:', _currentLayout, '— falling back to compact-grid');
                _runCompactGridLayout(cy, cfg);
                return;
            }
        }

        _reportProgress(92, 'Running ' + _currentLayout + ' layout...');

        var layout = cy.layout(opts);
        cy._runningLayout = layout;

        layout.on('layoutstop', function () {
            cy._runningLayout = null;
            _reportProgress(100, 'Complete');
        });

        layout.run();
    }

    return {
        init: init,
        highlightRegion: highlightRegion,
        highlightOrphans: highlightOrphans,
        highlightNode: highlightNode,
        clearHighlight: clearHighlight,
        fitView: fitView,
        resize: resizeGraph,
        reLayout: function () { if (cy) runLayout(_currentLayout); },
        runLayout: runLayout,
        destroy: destroy,
        getRegionColors: getRegionColors,
        updateRegionData: updateRegionData,
        updateSingleNode: updateSingleNode,
        addRegionParent: addRegionParent,
        removeRegionParent: removeRegionParent,
        moveNodeToRegion: moveNodeToRegion,
        updateNodePoiBadge: updateNodePoiBadge,
        closeContextMenu: closeContextMenu
    };
})();
