// Event listeners and initialization
document.addEventListener('DOMContentLoaded', function () {
	console.log('?? SchemaMagic: DOM loaded, initializing...');
	console.log('?? Entities found:', typeof entities !== 'undefined' ? Object.keys(entities).length : 'UNDEFINED');
	console.log('?? Document GUID:', typeof DOCUMENT_GUID !== 'undefined' ? DOCUMENT_GUID : 'UNDEFINED');

	try {
		loadSettings();
		console.log('?? Settings loaded successfully');
	} catch (e) {
		console.error('? Failed to load settings:', e);
	}

	try {
		loadTableGroupingRules();
		console.log('?? Table grouping rules loaded successfully');
	} catch (e) {
		console.error('? Failed to load table grouping rules:', e);
	}

	try {
		generateSchema();
		console.log('?? Schema generated successfully');
	} catch (e) {
		console.error('? Failed to generate schema:', e);
	}

	// Load view state AFTER schema has been generated (so svgViewBox is initialized)
	try {
		const loaded = loadViewState();
		if (loaded) {
			console.log('?? Zoom/pan state restored from previous session');
		} else {
			console.log('?? No saved zoom/pan state found - using defaults');
		}
	} catch (e) {
		console.error('? Failed to load view state:', e);
	}

	try {
		setupEventListeners();
		console.log('?? Event listeners set up successfully');
	} catch (e) {
		console.error('? Failed to set up event listeners:', e);
	}
});

function setupEventListeners() {
	const container = document.getElementById('schema-container');
	const svg = document.getElementById('schema-svg');
	const backgroundArea = document.getElementById('background-pan-area');

	backgroundArea.addEventListener('mousedown', startBackgroundPan);
	container.addEventListener('mousemove', handlePan);
	container.addEventListener('mouseup', endPan);
	container.addEventListener('mouseleave', endPan);

	svg.addEventListener('wheel', handleWheel);
	svg.addEventListener('contextmenu', e => e.preventDefault());

	// Add click anywhere to show toolbar
	container.addEventListener('click', function (e) {
		// If toolbar is hidden and user clicks anywhere, show it
		if (!toolbarVisible) {
			toggleToolbar();
		}
	});

	// Add keyboard shortcuts
	document.addEventListener('keydown', function (e) {
		if (e.key === 'Escape' && selectedTable) {
			clearSelection();
			e.preventDefault();
		}
		// Add Ctrl+H to toggle toolbar
		if (e.ctrlKey && e.key === 'h') {
			toggleToolbar();
			e.preventDefault();
		}
	});
}

function clearSelection() {
	// Use the new deselectTable function for consistency
	deselectTable();
}

// Enhanced background pan handling with selection clearing
function startBackgroundPan(e) {
	// Only pan if clicking on background, not on table elements
	if (e.target.closest('.table-group')) return;

	// Clear selection when clicking on background
	if (selectedTable) {
		clearSelection();
	}

	isPanning = true;
	panStart = {
		x: e.clientX - svgViewBox.x,
		y: e.clientY - svgViewBox.y
	};
	document.getElementById('schema-container').style.cursor = 'grabbing';
	e.preventDefault();
}