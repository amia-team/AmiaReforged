/**
 * Region Graph — Cytoscape.js interop for the Regions admin page.
 * Color-codes area nodes by region assignment, supports region highlighting,
 * orphan highlighting, and node selection with Blazor callbacks.
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
        selectedNode: '#ff9800'
    };

    /** Map of regionTag -> color */
    let regionColorMap = {};

    /**
     * Initialize the region graph.
     * @param {string} containerId DOM id of the container div
     * @param {string} nodesJson JSON array of AreaNodeDto (connected)
     * @param {string} edgesJson JSON array of AreaEdgeDto
     * @param {string} disconnectedJson JSON array of AreaNodeDto (disconnected)
     * @param {string} regionTagsJson JSON array of region tag strings (for color assignment)
     * @param {DotNet.DotNetObject} blazorRef .NET object ref for callbacks
     */
    function init(containerId, nodesJson, edgesJson, disconnectedJson, regionTagsJson, blazorRef) {
        destroy();
        dotNetRef = blazorRef || null;

        var container = document.getElementById(containerId);
        if (!container) {
            console.error('[regionGraph] Container not found:', containerId);
            return;
        }

        var nodes = JSON.parse(nodesJson);
        var edges = JSON.parse(edgesJson);
        var disconnected = JSON.parse(disconnectedJson);
        var regionTags = JSON.parse(regionTagsJson);

        // Build region -> color map
        regionColorMap = {};
        regionTags.forEach(function (tag, idx) {
            regionColorMap[tag.toLowerCase()] = REGION_PALETTE[idx % REGION_PALETTE.length];
        });

        // Merge connected + disconnected into one node set
        var allNodes = nodes.concat(disconnected);

        var elements = [];

        allNodes.forEach(function (n) {
            var regionTag = (n.region || '').toLowerCase();
            var color = regionColorMap[regionTag] || COLORS.unassigned;
            var borderColor = regionColorMap[regionTag] ? darken(color, 0.3) : COLORS.unassignedBorder;
            var isDisconnected = !nodes.some(function (cn) { return cn.resRef === n.resRef; });

            elements.push({
                group: 'nodes',
                data: {
                    id: n.resRef,
                    label: n.name || n.resRef,
                    resRef: n.resRef,
                    region: n.region || '',
                    hasSpawnProfile: n.hasSpawnProfile ? 'yes' : '',
                    spawnProfileName: n.spawnProfileName || '',
                    nodeColor: color,
                    nodeBorder: borderColor,
                    isDisconnected: isDisconnected ? 'yes' : ''
                }
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
            style: [
                {
                    selector: 'node',
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
                {
                    selector: 'node[isDisconnected = "yes"]',
                    style: {
                        'shape': 'diamond',
                        'width': 28,
                        'height': 28,
                        'font-size': '8px'
                    }
                },
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
                }
            ],
            layout: { name: 'preset' }
        });

        cy.startBatch();
        cy.add(elements);
        cy.endBatch();

        cy.layout({
            name: 'cose',
            animate: 'end',
            animationDuration: 600,
            nodeRepulsion: function () { return 8000; },
            idealEdgeLength: function () { return 100; },
            edgeElasticity: function () { return 100; },
            gravity: 0.3,
            numIter: 500,
            padding: 30,
            randomize: true
        }).run();

        // Node click
        cy.on('tap', 'node', function (evt) {
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
                    spawnProfileName: data.spawnProfileName
                }));
            }
        });

        // Background click
        cy.on('tap', function (evt) {
            if (evt.target === cy) {
                cy.nodes().removeClass('selected-node');
                if (dotNetRef) {
                    dotNetRef.invokeMethodAsync('OnGraphNodeDeselected');
                }
            }
        });
    }

    /**
     * Highlight all nodes belonging to a specific region.
     */
    function highlightRegion(regionName) {
        if (!cy) return;
        cy.elements().removeClass('highlighted dimmed region-highlight');

        if (!regionName) return;

        var lowerName = regionName.toLowerCase();
        var matched = cy.nodes().filter(function (n) {
            return (n.data('region') || '').toLowerCase() === lowerName;
        });

        if (matched.length === 0) return;

        var neighborhood = matched.closedNeighborhood();
        cy.elements().addClass('dimmed');
        neighborhood.removeClass('dimmed');
        matched.addClass('region-highlight');
        matched.connectedEdges().addClass('highlighted');

        cy.animate({
            fit: { eles: matched, padding: 80 },
            duration: 400
        });
    }

    /**
     * Highlight all nodes NOT assigned to any region (orphans).
     */
    function highlightOrphans() {
        if (!cy) return;
        cy.elements().removeClass('highlighted dimmed region-highlight');

        var orphans = cy.nodes().filter(function (n) {
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

    /**
     * Search by name or resref.
     */
    function highlightNode(query) {
        if (!cy) return false;
        cy.elements().removeClass('highlighted dimmed region-highlight');
        if (!query || query.trim() === '') return false;

        var lowerQuery = query.toLowerCase();
        var matched = cy.nodes().filter(function (n) {
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
        cy.elements().removeClass('highlighted dimmed region-highlight selected-node');
    }

    function fitView() {
        if (!cy) return;
        cy.animate({
            fit: { padding: 30 },
            duration: 300
        });
    }

    function destroy() {
        if (cy) {
            cy.destroy();
            cy = null;
        }
        dotNetRef = null;
        regionColorMap = {};
    }

    /**
     * Get the current region color map (regionTag -> hex color).
     */
    function getRegionColors() {
        return regionColorMap;
    }

    /**
     * Darken a hex color by a factor (0–1).
     */
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
        destroy: destroy,
        getRegionColors: getRegionColors
    };
})();
