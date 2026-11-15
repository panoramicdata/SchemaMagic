// Pan and zoom functionality
let isDragging = false;
let dragStart = { x: 0, y: 0 };

const MIN_ZOOM = 0.05; // 5% - very zoomed out
// MAX_ZOOM is defined in variables.js - don't redeclare it here
const DEFAULT_ZOOM = 0.35;

// Convert a mouse event position to SVG coordinates
function getSvgPoint(e) {
	const svg = document.getElementById("schema-svg");
	const pt = svg.createSVGPoint();
	pt.x = e.clientX;
	pt.y = e.clientY;
	return pt.matrixTransform(svg.getScreenCTM().inverse());
}

function setContainerCursor(cursor) {
	document.getElementById("schema-container").style.cursor = cursor;
}

function startBackgroundPan(e) {
	if (e.target.closest(".table-group")) return;

	if (selectedTable) {
		clearSelection();
	}

	const svgP = getSvgPoint(e);
	isDragging = true;
	dragStart = { x: svgP.x, y: svgP.y };

	setContainerCursor("grabbing");
	e.preventDefault();
}

function handlePan(e) {
	if (!isDragging) return;

	const svgP = getSvgPoint(e);
	svgViewBox.x -= svgP.x - dragStart.x;
	svgViewBox.y -= svgP.y - dragStart.y;

	updateViewBox();
	e.preventDefault();
}

function endPan() {
	if (!isDragging) return;
	isDragging = false;
	setContainerCursor("grab");
	saveViewState();
}

// Resize the viewBox for a new zoom level, keeping the focus point at the given ratios
function applyZoom(newZoom, focusX, focusY, ratioX, ratioY) {
	const newWidth = CANVAS_WIDTH / newZoom;
	const newHeight = CANVAS_HEIGHT / newZoom;

	svgViewBox.x = focusX - ratioX * newWidth;
	svgViewBox.y = focusY - ratioY * newHeight;
	svgViewBox.width = newWidth;
	svgViewBox.height = newHeight;

	currentZoom = newZoom;
	updateViewBox();
	saveViewState();
}

function clampZoom(zoom) {
	return Math.max(MIN_ZOOM, Math.min(MAX_ZOOM, zoom));
}

function handleWheel(e) {
	e.preventDefault();

	const svgP = getSvgPoint(e);
	const newZoom = clampZoom(currentZoom * (e.deltaY > 0 ? 0.9 : 1.1));
	if (newZoom === currentZoom) return;

	const ratioX = (svgP.x - svgViewBox.x) / svgViewBox.width;
	const ratioY = (svgP.y - svgViewBox.y) / svgViewBox.height;
	applyZoom(newZoom, svgP.x, svgP.y, ratioX, ratioY);
}

function updateViewBox() {
	const svg = document.getElementById("schema-svg");
	svg.setAttribute(
		"viewBox",
		`${svgViewBox.x} ${svgViewBox.y} ${svgViewBox.width} ${svgViewBox.height}`,
	);
}

// Zoom about the centre of the current view
function zoomAboutCenter(factor) {
	const newZoom = clampZoom(currentZoom * factor);
	if (newZoom === currentZoom) return;

	const centerX = svgViewBox.x + svgViewBox.width / 2;
	const centerY = svgViewBox.y + svgViewBox.height / 2;
	applyZoom(newZoom, centerX, centerY, 0.5, 0.5);
}

function zoomIn() {
	zoomAboutCenter(1.2);
}

function zoomOut() {
	zoomAboutCenter(0.8);
}

function resetZoom() {
	currentZoom = DEFAULT_ZOOM;
	svgViewBox = {
		x: 0,
		y: 0,
		width: CANVAS_WIDTH / currentZoom,
		height: CANVAS_HEIGHT / currentZoom,
	};
	updateViewBox();
	saveViewState();
}

// Load the saved view state; call AFTER generateSchema() has initialised svgViewBox
function loadViewState() {
	try {
		const savedState = localStorage.getItem(
			getDocumentStorageKey("viewState"),
		);
		if (savedState) {
			const viewState = JSON.parse(savedState);
			applyViewState(viewState);
			return true;
		}
	} catch (e) {
		console.warn("Failed to load view state:", e);
	}
	return false;
}

function applyViewState(viewState) {
	currentZoom = viewState.zoom;
	const newWidth = CANVAS_WIDTH / currentZoom;
	const newHeight = CANVAS_HEIGHT / currentZoom;

	svgViewBox.x = viewState.centerX - newWidth / 2;
	svgViewBox.y = viewState.centerY - newHeight / 2;
	svgViewBox.width = newWidth;
	svgViewBox.height = newHeight;

	updateViewBox();
}
