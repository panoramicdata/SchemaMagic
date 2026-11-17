// Schema generation and table positioning
function generateTables(svg, tablePositions) {
	let tablesGenerated = 0;
	Object.keys(entities).forEach((entityName) => {
		try {
			const position = tablePositions[entityName];
			if (!position) {
				console.warn("No position found for entity:", entityName);
				return;
			}

			generateTable(svg, entities[entityName], position.x, position.y);
			tablesGenerated++;
		} catch (e) {
			console.error("Failed to generate table for", entityName, e);
		}
	});
	return tablesGenerated;
}

function generateSchema() {
	const svg = document.getElementById("schema-svg");
	if (!svg) {
		console.error("SVG element not found!");
		return;
	}
	if (typeof entities === "undefined") {
		console.error("Entities not defined!");
		return;
	}

	const currentSelection = selectedTable;

	// Selection overlays are part of the table groups, so they are removed with them
	svg
		.querySelectorAll(".table-group, .relationship-line")
		.forEach((el) => el.remove());

	const tablesGenerated = generateTables(svg, calculateTablePositions());
	console.log("Generated tables:", tablesGenerated);

	if (showRelationships) {
		try {
			generateRelationships(svg);
		} catch (e) {
			console.error("Failed to generate relationships:", e);
		}
	}

	if (currentSelection) {
		selectTable(currentSelection);
	}

	updateButtonStates();
}

function calculateTablePositions() {
	// The smart layered layout is the default; saved positions are honoured by it
	if (typeof calculateSmartTablePositions === "function") {
		return calculateSmartTablePositions();
	}
	return calculateBasicGridLayout();
}

function snapPoint(x, y) {
	if (!snapToGrid) {
		return { x, y };
	}
	return { x: Math.round(x / 20) * 20, y: Math.round(y / 20) * 20 };
}

function loadSavedPositions() {
	try {
		const saved = localStorage.getItem(STORAGE_KEYS.tablePositions);
		return saved ? JSON.parse(saved) : null;
	} catch (e) {
		console.warn("Failed to load saved positions:", e);
		return null;
	}
}

function layoutFromSavedPositions(entityNames, savedPositions) {
	const positions = {};
	entityNames.forEach((entityName) => {
		const saved = savedPositions[entityName];
		// New entities get a default position
		positions[entityName] = saved ? snapPoint(saved.x, saved.y) : snapPoint(200, 200);
	});
	return positions;
}

function layoutCenteredGrid(entityNames) {
	const positions = {};
	const cols = Math.ceil(Math.sqrt(entityNames.length));
	const baseSpacing = 500;
	const totalWidth = cols * baseSpacing;
	const totalHeight = Math.ceil(entityNames.length / cols) * baseSpacing;
	const startX = (CANVAS_WIDTH - totalWidth) / 2;
	const startY = (CANVAS_HEIGHT - totalHeight) / 2;

	entityNames.forEach((entityName, index) => {
		const row = Math.floor(index / cols);
		const col = index % cols;
		positions[entityName] = snapPoint(
			startX + col * baseSpacing + (row % 2) * 250,
			startY + row * baseSpacing,
		);
	});
	return positions;
}

function calculateBasicGridLayout() {
	const savedPositions = loadSavedPositions();
	const entityNames = Object.keys(entities);

	return savedPositions
		? layoutFromSavedPositions(entityNames, savedPositions)
		: layoutCenteredGrid(entityNames);
}
