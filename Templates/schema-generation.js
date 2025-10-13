// Schema generation and table positioning
function generateSchema() {
	console.log('??? Starting schema generation...');
	const svg = document.getElementById('schema-svg');
	
	if (!svg) {
		console.error('? SVG element not found!');
		return;
	}

	// Remember current selection
	const currentSelection = selectedTable;

	// Remove existing content but keep selection overlays (they're part of table groups)
	const existingContent = svg.querySelectorAll('.table-group, .relationship-line');
	console.log(`?? Removing ${existingContent.length} existing elements`);
	existingContent.forEach(el => el.remove());

	if (typeof entities === 'undefined') {
		console.error('? Entities not defined!');
		return;
	}

	const entityCount = Object.keys(entities).length;
	console.log(`?? Processing ${entityCount} entities:`, Object.keys(entities));

	const tablePositions = calculateTablePositions();
	console.log('?? Table positions calculated:', tablePositions);

	let tablesGenerated = 0;
	Object.keys(entities).forEach((entityName) => {
		try {
			const entity = entities[entityName];
			const position = tablePositions[entityName];
			
			if (!position) {
				console.warn(`?? No position found for entity: ${entityName}`);
				return;
			}
			
			generateTable(svg, entity, position.x, position.y);
			tablesGenerated++;
			console.log(`? Generated table for ${entityName} at (${position.x}, ${position.y})`);
		} catch (e) {
			console.error(`? Failed to generate table for ${entityName}:`, e);
		}
	});

	console.log(`?? Successfully generated ${tablesGenerated} tables`);

	if (showRelationships) {
		try {
			generateRelationships(svg);
			console.log('?? Relationships generated');
		} catch (e) {
			console.error('? Failed to generate relationships:', e);
		}
	}

	// Restore selection if it was previously set
	if (currentSelection) {
		selectTable(currentSelection);
	}

	updateButtonStates();
	console.log('? Schema generation completed');
}

function calculateTablePositions() {
	// Always use force-directed layout by default (auto-optimize unless saved positions exist)
	if (typeof calculateTablePositionsWithForces === 'function') {
		return calculateTablePositionsWithForces();
	}
	
	// Fallback to basic grid layout if force-directed not available
	return calculateBasicGridLayout();
}

function calculateBasicGridLayout() {
	// Try to load saved positions first
	let savedPositions = null;
	try {
		const saved = localStorage.getItem(STORAGE_KEYS.tablePositions);
		if (saved) {
			savedPositions = JSON.parse(saved);
		}
	} catch (e) {
		console.warn('Failed to load saved positions:', e);
	}

	const positions = {};
	const entityNames = Object.keys(entities);

	if (savedPositions) {
		// Use saved positions if available, but snap to grid if enabled
		entityNames.forEach(entityName => {
			if (savedPositions[entityName]) {
				let x = savedPositions[entityName].x;
				let y = savedPositions[entityName].y;
				
				// Snap to grid if enabled
				if (snapToGrid) {
					x = Math.round(x / 20) * 20;
					y = Math.round(y / 20) * 20;
				}
				
				positions[entityName] = { x, y };
			} else {
				// Default position for new entities, snapped to grid
				let x = 200;
				let y = 200;
				if (snapToGrid) {
					x = Math.round(x / 20) * 20;
					y = Math.round(y / 20) * 20;
				}
				positions[entityName] = { x, y };
			}
		});
	} else {
		// Calculate centered grid layout
		const cols = Math.ceil(Math.sqrt(entityNames.length));
		const baseSpacing = 500;
		const totalWidth = cols * baseSpacing;
		const totalHeight = Math.ceil(entityNames.length / cols) * baseSpacing;

		// Center the grid in the canvas
		const startX = (CANVAS_WIDTH - totalWidth) / 2;
		const startY = (CANVAS_HEIGHT - totalHeight) / 2;

		entityNames.forEach((entityName, index) => {
			const row = Math.floor(index / cols);
			const col = index % cols;

			let x = startX + col * baseSpacing + (row % 2) * 250;
			let y = startY + row * baseSpacing;

			// Snap to grid if enabled
			if (snapToGrid) {
				x = Math.round(x / 20) * 20;
				y = Math.round(y / 20) * 20;
			}

			positions[entityName] = { x, y };
		});
	}

	return positions;
}