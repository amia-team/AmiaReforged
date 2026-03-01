/**
 * Area Graph — Cytoscape.js interop for Blazor.
 * Renders the area connectivity graph with force-directed layout,
 * color-coded edges (door vs trigger), and search/highlight support.
 */
window.areaGraph = (function () {
    /** @type {import('cytoscape').Core | null} */
    let cy = null;

    const COLORS = {
        nodeBg: '#c9a84c',
        nodeBorder: '#8a6d1b',
        nodeText: '#d0ccc2',
        edgeDoor: '#5ba4b5',
        edgeTrigger: '#4ecca3',
        highlightNode: '#ffc107',
        highlightEdge: '#ffc107',
        dimmed: 'rgba(80,75,65,0.25)'
    };

    /**
     * Initialize the Cytoscape graph inside a container element.
     * @param {string} containerId DOM id of the container div
     * @param {string} nodesJson JSON array of {resRef, name, region}
     * @param {string} edgesJson JSON array of {sourceResRef, targetResRef, transitionType, transitionTag}
     */
    function init(containerId, nodesJson, edgesJson) {
        destroy(); // clean up any previous instance

        const container = document.getElementById(containerId);
        if (!container) {
            console.error('[areaGraph] Container not found:', containerId);
            return;
        }

        const nodes = JSON.parse(nodesJson);
        const edges = JSON.parse(edgesJson);

        const elements = [];

        // Add nodes
        nodes.forEach(function (n) {
            elements.push({
                group: 'nodes',
                data: {
                    id: n.resRef,
                    label: n.name || n.resRef,
                    resRef: n.resRef,
                    region: n.region || ''
                }
            });
        });

        // Add edges — use a composite id to allow multiple edges between same pair
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
            elements: elements,
            minZoom: 0.1,
            maxZoom: 3,
            wheelSensitivity: 0.2,
            style: [
                {
                    selector: 'node',
                    style: {
                        'background-color': COLORS.nodeBg,
                        'border-color': COLORS.nodeBorder,
                        'border-width': 2,
                        'label': 'data(label)',
                        'font-size': '10px',
                        'color': COLORS.nodeText,
                        'text-valign': 'center',
                        'text-halign': 'center',
                        'text-wrap': 'wrap',
                        'text-max-width': '100px',
                        'width': 40,
                        'height': 40,
                        'text-background-color': '#26241f',
                        'text-background-opacity': 0.85,
                        'text-background-padding': '2px',
                        'text-background-shape': 'roundrectangle'
                    }
                },
                {
                    selector: 'edge',
                    style: {
                        'width': 1.5,
                        'curve-style': 'bezier',
                        'target-arrow-shape': 'triangle',
                        'target-arrow-color': COLORS.edgeDoor,
                        'line-color': COLORS.edgeDoor,
                        'arrow-scale': 0.8,
                        'opacity': 0.7
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
                        'width': 55,
                        'height': 55,
                        'z-index': 999,
                        'font-size': '13px',
                        'font-weight': 'bold'
                    }
                },
                {
                    selector: 'edge.highlighted',
                    style: {
                        'line-color': COLORS.highlightEdge,
                        'target-arrow-color': COLORS.highlightEdge,
                        'width': 3,
                        'opacity': 1,
                        'z-index': 998
                    }
                },
                {
                    selector: '.dimmed',
                    style: {
                        'opacity': 0.15
                    }
                }
            ],
            layout: {
                name: 'cose',
                animate: true,
                animationDuration: 800,
                nodeRepulsion: function () { return 8000; },
                idealEdgeLength: function () { return 120; },
                edgeElasticity: function () { return 100; },
                gravity: 0.25,
                numIter: 1000,
                padding: 40,
                randomize: true
            }
        });

        // Tooltip on tap
        cy.on('tap', 'node', function (evt) {
            var node = evt.target;
            var data = node.data();
            var region = data.region ? '\nRegion: ' + data.region : '';
            console.log('[areaGraph] Node: ' + data.label + ' (' + data.resRef + ')' + region);
        });

        cy.on('tap', 'edge', function (evt) {
            var edge = evt.target;
            var data = edge.data();
            console.log('[areaGraph] Edge: ' + data.source + ' → ' + data.target +
                ' [' + data.transitionType + ': ' + data.transitionTag + ']');
        });
    }

    /**
     * Highlight a node by resref (or partial name match) and its direct neighbors.
     * Dims all other elements.
     * @param {string} query ResRef or name substring to match (case-insensitive)
     */
    function highlightNode(query) {
        if (!cy) return false;

        // Clear previous highlights
        cy.elements().removeClass('highlighted dimmed');

        if (!query || query.trim() === '') return false;

        var lowerQuery = query.toLowerCase();
        var matched = cy.nodes().filter(function (n) {
            return n.data('resRef').toLowerCase().indexOf(lowerQuery) !== -1 ||
                   n.data('label').toLowerCase().indexOf(lowerQuery) !== -1;
        });

        if (matched.length === 0) return false;

        // Get neighborhood
        var neighborhood = matched.closedNeighborhood();

        // Dim everything, then un-dim matches and neighbors
        cy.elements().addClass('dimmed');
        neighborhood.removeClass('dimmed');
        matched.addClass('highlighted');
        matched.connectedEdges().addClass('highlighted');

        // Center on matched nodes
        cy.animate({
            fit: { eles: neighborhood, padding: 60 },
            duration: 400
        });

        return true;
    }

    /**
     * Clear all highlights and show full graph.
     */
    function clearHighlight() {
        if (!cy) return;
        cy.elements().removeClass('highlighted dimmed');
    }

    /**
     * Reset zoom to fit all elements.
     */
    function fitView() {
        if (!cy) return;
        cy.animate({
            fit: { padding: 40 },
            duration: 300
        });
    }

    /**
     * Destroy the Cytoscape instance and free resources.
     */
    function destroy() {
        if (cy) {
            cy.destroy();
            cy = null;
        }
    }

    /**
     * Get the number of nodes currently in the graph.
     */
    function getNodeCount() {
        return cy ? cy.nodes().length : 0;
    }

    return {
        init: init,
        highlightNode: highlightNode,
        clearHighlight: clearHighlight,
        fitView: fitView,
        destroy: destroy,
        getNodeCount: getNodeCount
    };
})();
