/**
 * Region Graph Visual Editor — Cytoscape.js interop for the Regions admin page.
 * Uses compound (parent) nodes for region grouping, right-click context menus,
 * drag-and-drop reassignment, and POI badges on area nodes.
 *
 * Requires: cytoscape.js, cytoscape-fcose (optional, falls back to cose).
 */
window.regionGraph = (function () {
    /** @type {import('cytoscape').Core | null} */
    let cy = null;
    /** @type {DotNet.DotNetObject | null} */
    let dotNetRef = null;

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

    // ===== Register fcose if available =====
    var hasFcose = false;
    try {
        if (typeof cytoscapeFcose !== 'undefined') {
            cytoscape.use(cytoscapeFcose);
            hasFcose = true;
        }
    } catch (e) { /* fcose not available, fall back to cose */ }

    /**
     * Initialize the region graph visual editor.
     * @param {string} containerId DOM id of the container div
     * @param {string} nodesJson JSON array of AreaNodeDto (connected)
     * @param {string} edgesJson JSON array of AreaEdgeDto
     * @param {string} disconnectedJson JSON array of AreaNodeDto (disconnected)
     * @param {string} regionsJson JSON array of { tag, name, areaResRefs: string[], poiCounts: {resRef: count} }
     * @param {DotNet.DotNetObject} blazorRef .NET object ref for callbacks
     */
    function init(containerId, nodesJson, edgesJson, disconnectedJson, regionsJson, blazorRef) {
        console.log('[regionGraph] init v5 called, regionsJson type:', typeof regionsJson,
            'length:', regionsJson ? regionsJson.length : 0);
        destroy();
        dotNetRef = blazorRef || null;

        var container = document.getElementById(containerId);
        if (!container) {
            console.error('[regionGraph] Container not found:', containerId);
            return;
        }

        try {

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

        var elements = [];

        // Add region parent (compound) nodes
        regions.forEach(function (r) {
            var tag = String(r.tag || '');
            var name = String(r.name || '') || tag;
            var color = regionColorMap[tag.toLowerCase()] || COLORS.unassigned;
            elements.push({
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

            elements.push({
                group: 'nodes',
                data: data
            });
        });

        edges.forEach(function (e, idx) {
            elements.push({
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

        cy = cytoscape({
            container: container,
            elements: [],
            minZoom: 0.1,
            maxZoom: 3,
            wheelSensitivity: 0.2,
            textureOnViewport: true,
            hideEdgesOnViewport: true,
            boxSelectionEnabled: true,
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
                        'font-size': '14px',
                        'color': 'data(regionColor)',
                        'text-valign': 'top',
                        'text-halign': 'center',
                        'text-margin-y': -8,
                        'font-weight': 'bold',
                        'text-background-color': '#1a1a1a',
                        'text-background-opacity': 0.85,
                        'text-background-padding': '4px',
                        'text-background-shape': 'roundrectangle',
                        'padding': '25px',
                        'shape': 'roundrectangle',
                        'compound-sizing-wrt-labels': 'include',
                        'min-width': '120px',
                        'min-height': '80px'
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
                        'font-size': '9px',
                        'color': COLORS.nodeText,
                        'text-valign': 'center',
                        'text-halign': 'center',
                        'text-wrap': 'wrap',
                        'text-max-width': '90px',
                        'width': 35,
                        'height': 35,
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
                        'width': 28,
                        'height': 28,
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
                        'width': 50,
                        'height': 50,
                        'z-index': 999,
                        'font-size': '12px',
                        'font-weight': 'bold'
                    }
                },
                {
                    selector: 'node.region-highlight',
                    style: {
                        'border-width': 4,
                        'width': 48,
                        'height': 48,
                        'z-index': 998,
                        'font-size': '11px',
                        'font-weight': 'bold'
                    }
                },
                {
                    selector: 'node.selected-node',
                    style: {
                        'background-color': COLORS.selectedNode,
                        'border-color': '#e65100',
                        'border-width': 4,
                        'width': 50,
                        'height': 50,
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

        cy.startBatch();
        cy.add(elements);
        cy.endBatch();

        // Run layout
        var layoutName = hasFcose ? 'fcose' : 'cose';
        var layoutOpts = {
            name: layoutName,
            animate: 'end',
            animationDuration: 600,
            nodeRepulsion: function () { return 8000; },
            idealEdgeLength: function () { return 100; },
            edgeElasticity: function () { return 100; },
            gravity: 0.3,
            numIter: 500,
            padding: 30,
            randomize: true
        };

        // fcose-specific options for compound nodes
        if (hasFcose) {
            layoutOpts.quality = 'default';
            layoutOpts.nodeSeparation = 75;
            layoutOpts.packComponents = true;
        }

        cy.layout(layoutOpts).run();

        // ===== Node click — select =====
        cy.on('tap', 'node[isRegionParent != "yes"]', function (evt) {
            var node = evt.target;
            var data = node.data();

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
        });

        // ===== Region parent click =====
        cy.on('tap', 'node[isRegionParent = "yes"]', function (evt) {
            var node = evt.target;
            cy.nodes().removeClass('selected-node');
            node.addClass('selected-node');

            if (dotNetRef) {
                dotNetRef.invokeMethodAsync('OnRegionParentSelected', node.data('regionTag'));
            }
        });

        // ===== Background click — deselect =====
        cy.on('tap', function (evt) {
            if (evt.target === cy) {
                cy.nodes().removeClass('selected-node');
                closeContextMenu();
                if (dotNetRef) {
                    dotNetRef.invokeMethodAsync('OnGraphNodeDeselected');
                }
            }
        });

        // ===== Right-click context menu =====
        cy.on('cxttap', 'node[isRegionParent != "yes"]', function (evt) {
            evt.originalEvent.preventDefault();
            var node = evt.target;
            var pos = evt.originalEvent;
            showAreaContextMenu(node.data(), pos.clientX, pos.clientY);
        });

        cy.on('cxttap', 'node[isRegionParent = "yes"]', function (evt) {
            evt.originalEvent.preventDefault();
            var node = evt.target;
            var pos = evt.originalEvent;
            showRegionContextMenu(node.data(), pos.clientX, pos.clientY);
        });

        cy.on('cxttap', function (evt) {
            if (evt.target === cy) {
                evt.originalEvent.preventDefault();
                var pos = evt.originalEvent;
                showBackgroundContextMenu(pos.clientX, pos.clientY);
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

        console.log('[regionGraph] init v5 complete — all event handlers registered');

        } catch (err) {
            console.error('[regionGraph] init() failed:', err);
        }
    }

    // ========== Context Menu Functions ==========

    function showAreaContextMenu(nodeData, x, y) {
        closeContextMenu();

        var menu = document.createElement('div');
        menu.className = 'region-context-menu';
        menu.style.left = x + 'px';
        menu.style.top = y + 'px';

        var regionTag = nodeData.region || '';

        // "Edit in Panel" item
        addMenuItem(menu, '✏️ Edit in Panel', function () {
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
        addMenuItem(menu, '🔗 Highlight Connections', function () {
            closeContextMenu();
            highlightNode(nodeData.resRef);
        });

        addDivider(menu);

        // "Assign to Region" submenu
        var assignItem = document.createElement('div');
        assignItem.className = 'region-context-menu__item';
        assignItem.innerHTML = '📌 Assign to Region <span class="region-context-menu__submenu-arrow">▸</span>';
        assignItem.style.position = 'relative';

        var submenu = document.createElement('div');
        submenu.className = 'region-context-submenu';
        submenu.style.display = 'none';

        var regionTags = Object.keys(regionColorMap);
        if (regionTags.length === 0) {
            var empty = document.createElement('div');
            empty.className = 'region-context-submenu__item';
            empty.style.color = 'var(--text-secondary)';
            empty.textContent = 'No regions defined';
            submenu.appendChild(empty);
        } else {
            regionTags.forEach(function (tag) {
                var color = regionColorMap[tag];
                var item = document.createElement('div');
                item.className = 'region-context-submenu__item';
                item.innerHTML = '<span class="region-context-submenu__swatch" style="background:' + color + '"></span>' + tag;
                item.addEventListener('click', function (e) {
                    e.stopPropagation();
                    closeContextMenu();
                    assignNodeToRegion(nodeData.resRef, tag);
                });
                submenu.appendChild(item);
            });
        }

        assignItem.appendChild(submenu);
        assignItem.addEventListener('mouseenter', function () { submenu.style.display = 'block'; });
        assignItem.addEventListener('mouseleave', function () { submenu.style.display = 'none'; });
        menu.appendChild(assignItem);

        // "Remove from Region" (only if assigned)
        if (regionTag) {
            addMenuItem(menu, '❌ Remove from Region', function () {
                closeContextMenu();
                unassignNode(nodeData.resRef);
            }, true);
        }

        document.body.appendChild(menu);
        contextMenuEl = menu;

        // Ensure menu stays within viewport
        clampMenuPosition(menu);
    }

    function showRegionContextMenu(nodeData, x, y) {
        closeContextMenu();

        var menu = document.createElement('div');
        menu.className = 'region-context-menu';
        menu.style.left = x + 'px';
        menu.style.top = y + 'px';

        addMenuItem(menu, '✏️ Edit Region', function () {
            closeContextMenu();
            if (dotNetRef) {
                dotNetRef.invokeMethodAsync('OnRegionParentSelected', nodeData.regionTag);
            }
        });

        addMenuItem(menu, '🔍 Highlight All Areas', function () {
            closeContextMenu();
            highlightRegion(nodeData.label);
        });

        addDivider(menu);

        addMenuItem(menu, '🗑️ Delete Region', function () {
            closeContextMenu();
            if (dotNetRef) {
                dotNetRef.invokeMethodAsync('OnContextMenuAction', 'deleteRegion', nodeData.regionTag);
            }
        }, true);

        document.body.appendChild(menu);
        contextMenuEl = menu;
        clampMenuPosition(menu);
    }

    function showBackgroundContextMenu(x, y) {
        closeContextMenu();

        var menu = document.createElement('div');
        menu.className = 'region-context-menu';
        menu.style.left = x + 'px';
        menu.style.top = y + 'px';

        addMenuItem(menu, '➕ New Region', function () {
            closeContextMenu();
            if (dotNetRef) {
                dotNetRef.invokeMethodAsync('OnContextMenuAction', 'newRegion', '');
            }
        });

        addMenuItem(menu, '🔎 Show Unassigned', function () {
            closeContextMenu();
            highlightOrphans();
        });

        addDivider(menu);

        addMenuItem(menu, '📐 Fit View', function () {
            closeContextMenu();
            fitView();
        });

        addMenuItem(menu, '🧹 Clear Highlights', function () {
            closeContextMenu();
            clearHighlight();
            if (dotNetRef) {
                dotNetRef.invokeMethodAsync('OnGraphNodeDeselected');
            }
        });

        document.body.appendChild(menu);
        contextMenuEl = menu;
        clampMenuPosition(menu);
    }

    function addMenuItem(menu, text, handler, isDanger) {
        var item = document.createElement('div');
        item.className = 'region-context-menu__item' + (isDanger ? ' region-context-menu__item--danger' : '');
        item.textContent = text;
        item.addEventListener('click', function (e) {
            e.stopPropagation();
            handler();
        });
        menu.appendChild(item);
    }

    function addDivider(menu) {
        var d = document.createElement('div');
        d.className = 'region-context-menu__divider';
        menu.appendChild(d);
    }

    function closeContextMenu() {
        if (contextMenuEl && contextMenuEl.parentNode) {
            contextMenuEl.parentNode.removeChild(contextMenuEl);
        }
        contextMenuEl = null;
    }

    function clampMenuPosition(menu) {
        requestAnimationFrame(function () {
            var rect = menu.getBoundingClientRect();
            if (rect.right > window.innerWidth) {
                menu.style.left = (window.innerWidth - rect.width - 8) + 'px';
            }
            if (rect.bottom > window.innerHeight) {
                menu.style.top = (window.innerHeight - rect.height - 8) + 'px';
            }
        });
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

    return {
        init: init,
        highlightRegion: highlightRegion,
        highlightOrphans: highlightOrphans,
        highlightNode: highlightNode,
        clearHighlight: clearHighlight,
        fitView: fitView,
        resize: resizeGraph,
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
