// ========================================
// SMART LAYERED SCHEMA LAYOUT
// Deterministic Sugiyama-style layout:
//   1. Split the schema into connected components
//   2. Assign each table to a column (BFS layering from a pseudo-peripheral root)
//   3. Reduce relationship-line crossings (barycenter ordering sweeps)
//   4. Vertically align tables with their relationship partners
//      (least-squares 1D placement that preserves order and spacing)
//   5. Pack components (and unconnected tables) into a compact block
// Auto-layout only runs when no saved document state exists;
// user positioning is always respected once a document has been customized.
// ========================================

const LAYOUT_CONFIG = {
	columnGap: 550,          // Horizontal corridor between columns (room for orthogonal edge routing + crow's feet)
	rowGap: 130,             // Vertical space between stacked tables in a column
	componentGap: 700,       // Space between separate connected components
	singletonGap: 150,       // Grid spacing between unconnected tables
	orderingSweeps: 8,       // Barycenter crossing-reduction passes
	alignmentPasses: 12,     // Vertical neighbor-alignment relaxation passes
	minColumnHeight: 3000,   // Never split a column shorter than this
	maxColumnHeight: 13000,  // Hard cap: split taller columns into side-by-side banks
	gridSize: 50,            // Snap-to-grid size
};

/**
 * Entry point used by schema-generation.js.
 * Uses saved positions when the document has been customized,
 * otherwise computes a fresh smart layout.
 */
function calculateSmartTablePositions() {
	const entityNames = Object.keys(entities);

	if (hasSavedDocumentState()) {
		console.log(`📄 Document ${DOCUMENT_GUID}: Using saved positions (no auto-layout)`);
		return convertSavedToStandardFormat(loadSavedDocumentPositions(), entityNames);
	}

	console.log(`🎯 Document ${DOCUMENT_GUID}: No saved state found, computing smart layout...`);
	return computeSmartLayout(entityNames);
}

// Backwards-compatible alias (older callers/templates)
function calculateTablePositionsWithForces() {
	return calculateSmartTablePositions();
}

/**
 * Load saved positions for this specific document
 */
function loadSavedDocumentPositions() {
	try {
		const saved = localStorage.getItem(STORAGE_KEYS.tablePositions);
		if (saved) {
			return JSON.parse(saved);
		}
	} catch (e) {
		console.warn('Failed to load saved document positions:', e);
	}
	return null;
}

/**
 * Convert saved positions to standard format
 */
function convertSavedToStandardFormat(savedPositions, entityNames) {
	const positions = {};

	entityNames.forEach(entityName => {
		let x, y;
		if (savedPositions && savedPositions[entityName]) {
			x = savedPositions[entityName].x;
			y = savedPositions[entityName].y;
		} else {
			// Default position for new entities
			x = 500;
			y = 500;
		}

		if (snapToGrid) {
			x = Math.round(x / LAYOUT_CONFIG.gridSize) * LAYOUT_CONFIG.gridSize;
			y = Math.round(y / LAYOUT_CONFIG.gridSize) * LAYOUT_CONFIG.gridSize;
		}

		positions[entityName] = { x, y };
	});

	return positions;
}

/**
 * Compute a full smart layout for the given entities.
 * Returns { entityName: { x, y } } with top-left coordinates.
 */
function computeSmartLayout(entityNames) {
	if (entityNames.length === 0) return {};

	const relationships = extractRelationships();
	const graph = buildLayoutGraph(entityNames, relationships);
	const dims = {};
	entityNames.forEach(name => { dims[name] = getTableDimensions(name); });

	// Split into connected components (deterministic order)
	const components = findConnectedComponents(entityNames, graph);

	const blocks = [];
	const singletons = [];

	components.forEach(component => {
		if (component.length === 1) {
			singletons.push(component[0]);
		} else {
			blocks.push(layoutComponent(component, graph, dims));
		}
	});

	// Unconnected tables become one compact alphabetical grid block
	if (singletons.length > 0) {
		singletons.sort();
		blocks.push(layoutSingletonGrid(singletons, dims));
	}

	console.log(`🧩 Smart layout: ${components.length} components (${singletons.length} unconnected tables)`);

	// Pack component blocks into a compact overall arrangement
	const packed = packBlocks(blocks);

	return finalizePositions(packed);
}

/**
 * Build a weighted undirected adjacency map from FK relationships.
 * Parallel FKs between the same pair increase the edge weight.
 * Self-referential FKs are ignored for layout purposes.
 */
function buildLayoutGraph(entityNames, relationships) {
	const adjacency = {};
	entityNames.forEach(name => { adjacency[name] = new Map(); });

	relationships.forEach(rel => {
		if (rel.from === rel.to) return; // self-loops don't influence placement
		if (!adjacency[rel.from] || !adjacency[rel.to]) return;
		adjacency[rel.from].set(rel.to, (adjacency[rel.from].get(rel.to) || 0) + 1);
		adjacency[rel.to].set(rel.from, (adjacency[rel.to].get(rel.from) || 0) + 1);
	});

	return adjacency;
}

/**
 * Find connected components, largest first (ties broken alphabetically)
 */
function findConnectedComponents(entityNames, graph) {
	const visited = new Set();
	const components = [];
	const sortedNames = [...entityNames].sort();

	sortedNames.forEach(start => {
		if (visited.has(start)) return;

		const component = [];
		const queue = [start];
		visited.add(start);

		while (queue.length > 0) {
			const node = queue.shift();
			component.push(node);
			const neighbors = [...graph[node].keys()].sort();
			neighbors.forEach(neighbor => {
				if (!visited.has(neighbor)) {
					visited.add(neighbor);
					queue.push(neighbor);
				}
			});
		}

		components.push(component);
	});

	components.sort((a, b) => b.length - a.length || a[0].localeCompare(b[0]));
	return components;
}

/**
 * Lay out one connected component using a layered approach.
 * Returns { positions: { name: {x, y} }, width, height } with
 * positions normalized so the component's bounding box starts at (0, 0).
 */
function layoutComponent(nodes, graph, dims) {
	// --- 1. Layer assignment (columns) ---
	const layerOf = assignLayers(nodes, graph);
	let layerCount = 0;
	nodes.forEach(n => { layerCount = Math.max(layerCount, layerOf[n] + 1); });

	const layers = Array.from({ length: layerCount }, () => []);
	[...nodes].sort().forEach(n => layers[layerOf[n]].push(n));

	// --- 2. Crossing reduction (barycenter ordering sweeps) ---
	orderLayers(layers, graph);

	// --- 3. Split over-tall layers into side-by-side physical columns ---
	const columns = splitTallLayers(layers, dims);

	// --- 4. Coordinate assignment ---
	// Column x positions from cumulative column widths
	const columnX = [];
	let cumX = 0;
	columns.forEach((column, index) => {
		const colWidth = Math.max(...column.map(n => dims[n].width));
		columnX[index] = { x: cumX, width: colWidth };
		cumX += colWidth + LAYOUT_CONFIG.columnGap;
	});

	// Initial vertical stacking (centered per column)
	const pos = {}; // name -> { x, y } (top-left)
	const columnOf = {};
	columns.forEach((column, colIndex) => {
		const stackHeight = columnStackHeight(column, dims);
		let y = -stackHeight / 2;
		column.forEach(name => {
			const d = dims[name];
			pos[name] = {
				x: columnX[colIndex].x + (columnX[colIndex].width - d.width) / 2,
				y: y
			};
			columnOf[name] = colIndex;
			y += d.height + LAYOUT_CONFIG.rowGap;
		});
	});

	// Vertical alignment: pull tables toward the average of their
	// relationship partners. Early passes may reorder tables within their
	// column (fixing partners separated by column banking); later passes
	// only fine-tune positions so the layout settles.
	for (let pass = 0; pass < LAYOUT_CONFIG.alignmentPasses; pass++) {
		const forward = pass % 2 === 0;
		const allowReorder = pass < LAYOUT_CONFIG.alignmentPasses / 2;
		const indices = forward ?
			columns.map((_, i) => i) :
			columns.map((_, i) => columns.length - 1 - i);

		indices.forEach(colIndex => {
			relaxColumn(columns[colIndex], graph, dims, pos, allowReorder);
		});
	}

	// --- 5. Normalize to (0, 0) and measure ---
	let minX = Infinity, minY = Infinity, maxX = -Infinity, maxY = -Infinity;
	nodes.forEach(name => {
		const d = dims[name];
		minX = Math.min(minX, pos[name].x);
		minY = Math.min(minY, pos[name].y);
		maxX = Math.max(maxX, pos[name].x + d.width);
		maxY = Math.max(maxY, pos[name].y + d.height);
	});

	const positions = {};
	nodes.forEach(name => {
		positions[name] = {
			x: pos[name].x - minX,
			y: pos[name].y - minY
		};
	});

	return { positions, width: maxX - minX, height: maxY - minY };
}

/**
 * Assign tables to layers (columns) via BFS.
 * Root selection uses a double-BFS pseudo-diameter heuristic so the
 * component spreads across several columns with hubs in the middle,
 * instead of one giant column fanning out from the hub.
 */
function assignLayers(nodes, graph) {
	const sorted = [...nodes].sort();

	// Deterministic starting point: highest weighted degree
	const weightedDegree = name => {
		let sum = 0;
		graph[name].forEach(weight => { sum += weight; });
		return sum;
	};
	let start = sorted[0];
	sorted.forEach(n => {
		if (weightedDegree(n) > weightedDegree(start)) start = n;
	});

	// Double BFS: farthest node from the hub is a pseudo-peripheral root
	const firstPass = bfsDistances(start, graph);
	let root = start;
	sorted.forEach(n => {
		if (firstPass[n] !== undefined && firstPass[n] > (firstPass[root] || 0)) root = n;
	});

	return bfsDistances(root, graph);
}

/**
 * BFS distances from a root; only reaches the root's component
 */
function bfsDistances(root, graph) {
	const dist = { [root]: 0 };
	const queue = [root];

	while (queue.length > 0) {
		const node = queue.shift();
		const neighbors = [...graph[node].keys()].sort();
		neighbors.forEach(neighbor => {
			if (dist[neighbor] === undefined) {
				dist[neighbor] = dist[node] + 1;
				queue.push(neighbor);
			}
		});
	}

	return dist;
}

/**
 * Barycenter crossing reduction: repeatedly reorder each layer by the
 * average position of each table's relationship partners in the
 * neighboring layer, sweeping left-to-right then right-to-left.
 */
function orderLayers(layers, graph) {
	// Current index of each node within its layer
	const indexOf = {};
	const refreshIndices = layer => layer.forEach((n, i) => { indexOf[n] = i; });
	layers.forEach(refreshIndices);

	const sortByBarycenter = (layer, referenceLayer) => {
		const referenceSet = new Set(referenceLayer);
		const keyed = layer.map(name => {
			let weightSum = 0;
			let weightedIndexSum = 0;
			graph[name].forEach((weight, neighbor) => {
				if (referenceSet.has(neighbor)) {
					weightSum += weight;
					weightedIndexSum += weight * indexOf[neighbor];
				}
			});
			// Tables with no partners in the reference layer keep their position
			const barycenter = weightSum > 0 ? weightedIndexSum / weightSum : indexOf[name];
			return { name, barycenter };
		});

		keyed.sort((a, b) => a.barycenter - b.barycenter || a.name.localeCompare(b.name));
		const reordered = keyed.map(k => k.name);
		layer.length = 0;
		layer.push(...reordered);
		refreshIndices(layer);
	};

	for (let sweep = 0; sweep < LAYOUT_CONFIG.orderingSweeps; sweep++) {
		if (sweep % 2 === 0) {
			// Left-to-right: order each layer by partners in the previous layer
			for (let k = 1; k < layers.length; k++) {
				sortByBarycenter(layers[k], layers[k - 1]);
			}
		} else {
			// Right-to-left: order each layer by partners in the next layer
			for (let k = layers.length - 2; k >= 0; k--) {
				sortByBarycenter(layers[k], layers[k + 1]);
			}
		}
	}
}

/**
 * Total stacked height of a column including row gaps
 */
function columnStackHeight(column, dims) {
	let height = 0;
	column.forEach((name, i) => {
		height += dims[name].height;
		if (i > 0) height += LAYOUT_CONFIG.rowGap;
	});
	return height;
}

/**
 * Split any layer whose stacked height is excessive into several
 * side-by-side physical columns, keeping the crossing-reduced order.
 */
function splitTallLayers(layers, dims) {
	// Aim for a roughly 4:3 component; cap column height accordingly
	let totalArea = 0;
	layers.forEach(layer => layer.forEach(name => {
		totalArea += (dims[name].width + LAYOUT_CONFIG.columnGap) * (dims[name].height + LAYOUT_CONFIG.rowGap);
	}));
	const idealHeight = Math.sqrt(totalArea * 0.75); // height of a 4:3 rectangle with this area
	const maxColumnHeight = Math.min(
		Math.max(LAYOUT_CONFIG.minColumnHeight, idealHeight),
		LAYOUT_CONFIG.maxColumnHeight
	);

	const columns = [];
	layers.forEach(layer => {
		const stackHeight = columnStackHeight(layer, dims);
		if (stackHeight <= maxColumnHeight || layer.length <= 1) {
			columns.push([...layer]);
			return;
		}

		// Split into the fewest chunks that each fit the height budget
		const chunkCount = Math.ceil(stackHeight / maxColumnHeight);
		const targetPerChunk = Math.ceil(layer.length / chunkCount);
		for (let i = 0; i < layer.length; i += targetPerChunk) {
			columns.push(layer.slice(i, i + targetPerChunk));
		}
	});

	return columns.filter(c => c.length > 0);
}

/**
 * One vertical relaxation step for a column: each table wants to be
 * centered on the weighted average of its relationship partners.
 * When allowReorder is set, tables may swap places within the column to
 * get closer to their partners; spacing is always preserved via
 * least-squares block placement.
 */
function relaxColumn(column, graph, dims, pos, allowReorder) {
	const entries = column.map(name => {
		const d = dims[name];
		let weightSum = 0;
		let weightedCenterSum = 0;

		graph[name].forEach((weight, neighbor) => {
			if (pos[neighbor] === undefined) return;
			const neighborCenter = pos[neighbor].y + dims[neighbor].height / 2;
			weightSum += weight;
			weightedCenterSum += weight * neighborCenter;
		});

		const currentCenter = pos[name].y + d.height / 2;
		const desiredCenter = weightSum > 0 ? weightedCenterSum / weightSum : currentCenter;

		return {
			name,
			desiredTop: desiredCenter - d.height / 2,
			size: d.height + LAYOUT_CONFIG.rowGap,
			weight: Math.max(weightSum, 0.1)
		};
	});

	if (allowReorder) {
		entries.sort((a, b) => a.desiredTop - b.desiredTop || a.name.localeCompare(b.name));
		column.length = 0;
		column.push(...entries.map(e => e.name));
	}

	const tops = placeSequence1D(entries);
	column.forEach((name, i) => { pos[name].y = tops[i]; });
}

/**
 * Least-squares 1D placement: given items in fixed order with desired top
 * coordinates, minimum sizes and weights, find tops that respect order and
 * spacing while minimizing weighted squared deviation from desired positions.
 * Classic block-merging algorithm (as used in Brandes-Köpf style layouts).
 */
function placeSequence1D(entries) {
	const blocks = [];

	entries.forEach(entry => {
		let block = {
			desiredTop: entry.desiredTop,
			weight: entry.weight,
			size: entry.size,
			entries: [entry]
		};

		// Merge with previous blocks while they would overlap
		while (blocks.length > 0) {
			const last = blocks[blocks.length - 1];
			if (last.desiredTop + last.size <= block.desiredTop) break;

			blocks.pop();
			const combinedWeight = last.weight + block.weight;
			const combinedDesired =
				(last.desiredTop * last.weight + (block.desiredTop - last.size) * block.weight) / combinedWeight;
			block = {
				desiredTop: combinedDesired,
				weight: combinedWeight,
				size: last.size + block.size,
				entries: [...last.entries, ...block.entries]
			};
		}

		blocks.push(block);
	});

	const tops = [];
	blocks.forEach(block => {
		let y = block.desiredTop;
		block.entries.forEach(entry => {
			tops.push(y);
			y += entry.size;
		});
	});

	return tops;
}

/**
 * Arrange unconnected tables into a compact alphabetical grid block
 */
function layoutSingletonGrid(singletons, dims) {
	const count = singletons.length;
	const columnsCount = Math.max(1, Math.ceil(Math.sqrt(count * 1.6)));

	const positions = {};
	let y = 0;
	let width = 0;

	for (let rowStart = 0; rowStart < count; rowStart += columnsCount) {
		const row = singletons.slice(rowStart, rowStart + columnsCount);
		let x = 0;
		let rowHeight = 0;
		row.forEach(name => {
			positions[name] = { x, y };
			x += dims[name].width + LAYOUT_CONFIG.singletonGap;
			rowHeight = Math.max(rowHeight, dims[name].height);
		});
		width = Math.max(width, x - LAYOUT_CONFIG.singletonGap);
		y += rowHeight + LAYOUT_CONFIG.singletonGap;
	}

	return { positions, width, height: y - LAYOUT_CONFIG.singletonGap };
}

/**
 * Pack component blocks onto shelves, aiming for the canvas aspect ratio.
 * Blocks are placed largest-first, left-to-right, wrapping to a new shelf.
 */
function packBlocks(blocks) {
	if (blocks.length === 0) return { positions: {}, width: 0, height: 0 };

	const sorted = [...blocks].sort((a, b) => b.height - a.height || b.width - a.width);

	const totalArea = sorted.reduce(
		(sum, b) => sum + (b.width + LAYOUT_CONFIG.componentGap) * (b.height + LAYOUT_CONFIG.componentGap), 0);
	const canvasAspect = CANVAS_WIDTH / CANVAS_HEIGHT;
	const maxBlockWidth = Math.max(...sorted.map(b => b.width));
	const targetWidth = Math.max(maxBlockWidth, Math.sqrt(totalArea * canvasAspect));

	const positions = {};
	let shelfX = 0;
	let shelfY = 0;
	let shelfHeight = 0;
	let totalWidth = 0;
	let totalHeight = 0;

	sorted.forEach(block => {
		if (shelfX > 0 && shelfX + block.width > targetWidth) {
			// Wrap to next shelf
			shelfY += shelfHeight + LAYOUT_CONFIG.componentGap;
			shelfX = 0;
			shelfHeight = 0;
		}

		Object.keys(block.positions).forEach(name => {
			positions[name] = {
				x: block.positions[name].x + shelfX,
				y: block.positions[name].y + shelfY
			};
		});

		shelfX += block.width + LAYOUT_CONFIG.componentGap;
		shelfHeight = Math.max(shelfHeight, block.height);
		totalWidth = Math.max(totalWidth, shelfX - LAYOUT_CONFIG.componentGap);
		totalHeight = Math.max(totalHeight, shelfY + shelfHeight);
	});

	return { positions, width: totalWidth, height: totalHeight };
}

/**
 * Center the packed layout on the canvas, snap to grid and round.
 * Very large schemas expand the canvas so every table stays inside
 * the draggable boundary.
 */
function finalizePositions(packed) {
	const neededWidth = packed.width + BOUNDARY_MARGIN * 2;
	const neededHeight = packed.height + BOUNDARY_MARGIN * 2;
	if (neededWidth > CANVAS_WIDTH || neededHeight > CANVAS_HEIGHT) {
		CANVAS_WIDTH = Math.max(CANVAS_WIDTH, Math.ceil(neededWidth / 1000) * 1000);
		CANVAS_HEIGHT = Math.max(CANVAS_HEIGHT, Math.ceil(neededHeight / 1000) * 1000);
		console.log(`🖼️ Expanded canvas to ${CANVAS_WIDTH}×${CANVAS_HEIGHT} to fit the layout`);
	}

	const offsetX = Math.max(BOUNDARY_MARGIN, (CANVAS_WIDTH - packed.width) / 2);
	const offsetY = Math.max(BOUNDARY_MARGIN, (CANVAS_HEIGHT - packed.height) / 2);

	const result = {};
	Object.keys(packed.positions).forEach(name => {
		let x = packed.positions[name].x + offsetX;
		let y = packed.positions[name].y + offsetY;

		if (snapToGrid) {
			x = Math.round(x / LAYOUT_CONFIG.gridSize) * LAYOUT_CONFIG.gridSize;
			y = Math.round(y / LAYOUT_CONFIG.gridSize) * LAYOUT_CONFIG.gridSize;
		}

		result[name] = { x: Math.round(x), y: Math.round(y) };
	});

	console.log(`📐 Smart layout complete: ${Object.keys(result).length} tables in ${Math.round(packed.width)}×${Math.round(packed.height)}px`);
	return result;
}

/**
 * Extract FK relationships from entity data for layout calculations
 */
function extractRelationships() {
	const relationships = [];

	Object.values(entities).forEach(entity => {
		const fromEntityName = entity.type;
		// Include inherited FK properties so relationships declared on base classes
		// influence the layout (keeps base-linked tables near their target).
		const seenPropNames = new Set();
		const allProps = [...(entity.properties || []), ...(entity.inheritedProperties || [])]
			.filter(p => !seenPropNames.has(p.name) && seenPropNames.add(p.name));
		allProps.forEach(prop => {
			if (prop.isForeignKey) {
				const targetEntityName = findTargetEntity(prop.name);
				if (targetEntityName && entities[targetEntityName]) {
					relationships.push({
						from: fromEntityName,
						to: targetEntityName,
						property: prop.name
					});
				}
			}
		});
	});

	console.log(`🔗 Extracted ${relationships.length} relationships from ${Object.keys(entities).length} entities`);
	return relationships;
}

/**
 * Estimate table dimensions BEFORE rendering.
 * Mirrors the sizing math in table-generation.js (generateTable) exactly,
 * so layout spacing matches what actually gets drawn.
 */
function getTableDimensions(entityName) {
	const entity = entities[entityName];
	if (!entity) return { width: 1000, height: 500 };

	const properties = entity.properties || [];
	const inheritedProps = showInheritedProperties ? (entity.inheritedProperties || []) : [];
	const allProperties = [...inheritedProps, ...properties];

	const visibleProperties = showNavigationProperties ?
		allProperties :
		allProperties.filter(p => !isNavigationProperty(p));

	const rowHeight = 55;
	const headerHeight = 88;
	const inheritanceHeight = (entity.baseType && showInheritedProperties) ? 50 : 0;
	const padding = 30;
	const minWidth = 1000;
	const iconWidth = 60;

	const maxNameLength = Math.max(
		entity.type.length,
		...visibleProperties.map(p => p.name.length),
		0
	);

	const maxTypeLength = Math.max(
		8,
		...visibleProperties.map(p => {
			let typeText = p.type;
			if (isNavigationProperty(p)) {
				typeText = typeText.replace(/^ICollection<(.+)>$/, '$1').replace(/^List<(.+)>$/, '$1');
			}
			return typeText.length;
		})
	);

	const nameColumnWidth = Math.max(200, maxNameLength * 18);
	const typeColumnWidth = Math.max(150, maxTypeLength * 15);
	const typeIconSpace = 50;
	const spacingBuffer = 100;

	const width = Math.max(
		minWidth,
		iconWidth + nameColumnWidth + typeColumnWidth + typeIconSpace + spacingBuffer + (padding * 2)
	);

	const visibleRows = fullHeightMode ? visibleProperties.length : Math.min(visibleProperties.length, 15);
	const height = headerHeight + inheritanceHeight + (visibleRows * rowHeight) + padding;

	return { width, height: Math.max(height, 200) };
}

/**
 * Get current table positions from DOM
 */
function getCurrentTablePositions() {
	const positions = {};

	document.querySelectorAll('.table-group').forEach(tableGroup => {
		const entityName = tableGroup.getAttribute('data-entity');
		const rect = tableGroup.querySelector('.table-box');

		if (rect && entityName) {
			positions[entityName] = {
				x: parseFloat(rect.getAttribute('x')),
				y: parseFloat(rect.getAttribute('y'))
			};
		}
	});

	return positions;
}

/**
 * Apply computed positions to existing tables and refresh relationship lines
 */
function applyPositionsToTables(positions) {
	Object.keys(positions).forEach(entityName => {
		const tableGroup = document.querySelector(`[data-entity="${entityName}"]`);
		if (tableGroup && positions[entityName]) {
			const newPos = positions[entityName];
			moveTable(tableGroup, newPos.x, newPos.y);
		}
	});

	if (showRelationships) {
		updateRelationships();
	}
}
