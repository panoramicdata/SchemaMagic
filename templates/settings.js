// Local storage and settings management

function saveSettings() {
	// Save table positions
	const positions = {};
	document.querySelectorAll('.table-group').forEach(table => {
		const entityName = table.getAttribute('data-entity');
		const rect = table.querySelector('.table-box');
		positions[entityName] = {
			x: parseFloat(rect.getAttribute('x')),
			y: parseFloat(rect.getAttribute('y'))
		};
	});
	localStorage.setItem(STORAGE_KEYS.tablePositions, JSON.stringify(positions));

	// Save view settings
	const settings = {
		showRelationships,
		showNavigationProperties,
		showInheritedProperties,
		fullHeightMode,
		snapToGrid,
		legendVisible
	};
	localStorage.setItem(STORAGE_KEYS.viewSettings, JSON.stringify(settings));
}

function saveViewState() {
	// Save current zoom and view box state
	const viewState = {
		zoom: currentZoom,
		viewBox: { ...svgViewBox }
	};
	localStorage.setItem(STORAGE_KEYS.viewBox, JSON.stringify(viewState));
}

function loadSettings() {
	// Load view settings
	try {
		const savedSettings = localStorage.getItem(STORAGE_KEYS.viewSettings);
		if (savedSettings) {
			const settings = JSON.parse(savedSettings);
			showRelationships = settings.showRelationships ?? true;
			showNavigationProperties = settings.showNavigationProperties ?? false;
			showInheritedProperties = settings.showInheritedProperties ?? true;
			fullHeightMode = settings.fullHeightMode ?? false;
			snapToGrid = settings.snapToGrid ?? true;
			legendVisible = settings.legendVisible ?? true;

			// Apply legend visibility
			const legend = document.getElementById('legend');
			if (legend) {
				legend.style.display = legendVisible ? 'block' : 'none';
			}
		}
	} catch (e) {
		console.warn('Failed to load settings:', e);
	}

	// Load view state
	try {
		const savedViewState = localStorage.getItem(STORAGE_KEYS.viewBox);
		if (savedViewState) {
			const viewState = JSON.parse(savedViewState);
			if (viewState.zoom) {
				currentZoom = viewState.zoom;
			}
			if (viewState.viewBox) {
				svgViewBox = { ...viewState.viewBox };
			}
		}
	} catch (e) {
		console.warn('Failed to load view state:', e);
	}
}

function updateButtonStates() {
	// Update button active states based on current settings
	const relationshipsBtn = document.getElementById('relationships-btn');
	relationshipsBtn.classList.toggle('active', showRelationships); // Fixed: was inverted
	relationshipsBtn.textContent = showRelationships ? 'Hide Relations' : 'Show Relations';

	const navPropsBtn = document.getElementById('nav-props-btn');
	navPropsBtn.classList.toggle('active', showNavigationProperties);

	const inheritedBtn = document.getElementById('inherited-props-btn');
	inheritedBtn.classList.toggle('active', showInheritedProperties);
	inheritedBtn.textContent = showInheritedProperties ? 'Hide Inherited' : 'Show Inherited';

	const fullHeightBtn = document.getElementById('full-height-btn');
	fullHeightBtn.classList.toggle('active', fullHeightMode);

	const snapGridBtn = document.getElementById('snap-grid-btn');
	snapGridBtn.classList.toggle('active', snapToGrid);
	document.getElementById('grid-background').style.display = snapToGrid ? 'block' : 'none';
}