/**
 * Area Graph — Cytoscape.js interop for Blazor.
 * Renders the area connectivity graph with force-directed layout,
 * color-coded edges (door vs trigger), search/highlight, and node selection.
 */
window.areaGraph = (function () {
    /** @type {import('cytoscape').Core | null} */
    let cy = null;
    /** @type {DotNet.DotNetObject | null} */
    let dotNetRef = null;

    const COLORS = {
        nodeBg: '#c9a84c',
        nodeBorder: '#8a6d1b',
        nodeText: '#d0ccc2',
        nodeSpawn: '#4ecca3',
        edgeDoor: '#5ba4b5',
        edgeTrigger: '#4ecca3',
        highlightNode: '#ffc107',
        highlightEdge: '#ffc107',
        selectedNode: '#ff9800',
        dimmed: 'rgba(80,75,65,0.25)'
    };

    /**
     * Initialize the Cytoscape graph inside a container element.
     * @param {string} containerId DOM id of the container div
     * @param {string} nodesJson JSON array of node data
     * @param {string} edgesJson JSON array of edge data
     * @param {DotNet.DotNetObject} blazorRef .NET object ref for callbacks
     */
    function init(containerId, nodesJson, edgesJson, blazorRef) {
        destroy(); // clean up any previous instance

        dotNetRef = blazorRef || null;

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
                    region: n.region || '',
                    hasSpawnProfile: n.hasSpawnProfile ? 'yes' : '',
                    spawnProfileName: n.spawnProfileName || ''
                }
            });
        });

        // Add edges
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
                // Nodes with spawn profiles get a distinct border
                {
                    selector: 'node[hasSpawnProfile = "yes"]',
                    style: {
                        'border-color': COLORS.nodeSpawn,
                        'border-width': 3
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

        // Node click — send data back to Blazor
        cy.on('tap', 'node', function (evt) {
            var node = evt.target;
            var data = node.data();

            // Visual selection
            cy.nodes().removeClass('selected-node');
            node.addClass('selected-node');

            // Gather connections
            var incomingEdges = node.incomers('edge');
            var outgoingEdges = node.outgoers('edge');

            var connections = [];
            outgoingEdges.forEach(function (edge) {
                var targetNode = cy.getElementById(edge.data('target'));
                connections.push({
                    direction: 'outgoing',
                    areaResRef: edge.data('target'),
                    areaName: targetNode.data('label') || edge.data('target'),
                    transitionType: edge.data('transitionType'),
                    transitionTag: edge.data('transitionTag')
                });
            });
            incomingEdges.forEach(function (edge) {
                var sourceNode = cy.getElementById(edge.data('source'));
                connections.push({
                    direction: 'incoming',
                    areaResRef: edge.data('source'),
                    areaName: sourceNode.data('label') || edge.data('source'),
                    transitionType: edge.data('transitionType'),
                    transitionTag: edge.data('transitionTag')
                });
            });

            // Invoke Blazor callback
            if (dotNetRef) {
                dotNetRef.invokeMethodAsync('OnNodeSelected', JSON.stringify({
                    resRef: data.resRef,
                    name: data.label,
                    region: data.region,
                    hasSpawnProfile: data.hasSpawnProfile === 'yes',
                    spawnProfileName: data.spawnProfileName,
                    connections: connections
                }));
            }
        });

        // Click on background deselects
        cy.on('tap', function (evt) {
            if (evt.target === cy) {
                cy.nodes().removeClass('selected-node');
                if (dotNetRef) {
                    dotNetRef.invokeMethodAsync('OnNodeDeselected');
                }
            }
        });
    }

    /**
     * Highlight a node by resref (or partial name match) and its direct neighbors.
     */
    function highlightNode(query) {
        if (!cy) return false;

        cy.elements().removeClass('highlighted dimmed');

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
        cy.elements().removeClass('highlighted dimmed');
    }

    function fitView() {
        if (!cy) return;
        cy.animate({
            fit: { padding: 40 },
            duration: 300
        });
    }

    function destroy() {
        if (cy) {
            cy.destroy();
            cy = null;
        }
        dotNetRef = null;
    }

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
