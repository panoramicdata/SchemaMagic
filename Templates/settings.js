// Settings management and local storage

function getTablePositions() {
	const positions = {};
	document.querySelectorAll(".table-group").forEach((table) => {
		const rect = table.querySelector(".table-box");
		positions[table.getAttribute("data-entity")] = {
			x: parseFloat(rect.getAttribute("x")),
			y: parseFloat(rect.getAttribute("y")),
		};
	});
	return positions;
}

function saveSettings() {
	localStorage.setItem(
		STORAGE_KEYS.tablePositions,
		JSON.stringify(getTablePositions()),
	);

	const settings = {
		showRelationships,
		showNavigationProperties,
		showInheritedProperties,
		fullHeightMode,
		snapToGrid,
		legendVisible,
	};
	localStorage.setItem(STORAGE_KEYS.viewSettings, JSON.stringify(settings));
}

function saveViewState() {
	const viewState = {
		zoom: currentZoom,
		viewBox: { ...svgViewBox },
	};
	localStorage.setItem(STORAGE_KEYS.viewBox, JSON.stringify(viewState));
}

const DEFAULT_VIEW_SETTINGS = {
	showRelationships: true,
	showNavigationProperties: false,
	showInheritedProperties: true,
	fullHeightMode: false,
	snapToGrid: true,
	legendVisible: true,
};

function applySavedViewSettings(settings) {
	const merged = { ...DEFAULT_VIEW_SETTINGS };
	Object.keys(merged).forEach((key) => {
		merged[key] = settings[key] ?? merged[key];
	});

	({
		showRelationships,
		showNavigationProperties,
		showInheritedProperties,
		fullHeightMode,
		snapToGrid,
		legendVisible,
	} = merged);

	const legend = document.getElementById("legend");
	if (legend) {
		legend.style.display = legendVisible ? "block" : "none";
	}
}

function applySavedViewBox(viewState) {
	if (viewState.zoom) {
		currentZoom = viewState.zoom;
	}
	if (viewState.viewBox) {
		svgViewBox = { ...viewState.viewBox };
	}
}

function loadStoredJson(key, apply, description) {
	try {
		const saved = localStorage.getItem(key);
		if (saved) {
			apply(JSON.parse(saved));
		}
	} catch (e) {
		console.warn("Failed to load stored value:", description, e);
	}
}

function loadSettings() {
	loadStoredJson(STORAGE_KEYS.viewSettings, applySavedViewSettings, "settings");
	loadStoredJson(STORAGE_KEYS.viewBox, applySavedViewBox, "view state");
}
