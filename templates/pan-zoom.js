// Pan and zoom functionality - COMPLETELY REWRITTEN
let isDragging = false;
let dragStart = { x: 0, y: 0 };

// Zoom limits - using constants from variables.js
const MIN_ZOOM = 0.05;  // 5% - very zoomed out
// MAX_ZOOM is defined in variables.js - don't redeclare it here

function startBackgroundPan(e) {
	if (e.target.closest('.table-group')) return;

	// Clear selection when clicking background
	if (selectedTable) {
		clearSelection();
	}

	const svg = document.getElementById('schema-svg');
	const pt = svg.createSVGPoint();
	pt.x = e.clientX;
	pt.y = e.clientY;
	
	// Convert screen coordinates to SVG coordinates
	const svgP = pt.matrixTransform(svg.getScreenCTM().inverse());
	
	isDragging = true;
	dragStart = { x: svgP.x, y: svgP.y };
	
	document.getElementById('schema-container').style.cursor = 'grabbing';
	e.preventDefault();
}

function handlePan(e) {
	if (!isDragging) return;

	const svg = document.getElementById('schema-svg');
	const pt = svg.createSVGPoint();
	pt.x = e.clientX;
	pt.y = e.clientY;
	
	// Convert screen coordinates to SVG coordinates
	const svgP = pt.matrixTransform(svg.getScreenCTM().inverse());
	
	// Calculate how much we've moved in SVG space
	const dx = svgP.x - dragStart.x;
	const dy = svgP.y - dragStart.y;
	
	// Simply move the viewBox by the delta
	svgViewBox.x -= dx;
	svgViewBox.y -= dy;
	
	updateViewBox();
	e.preventDefault();
	
	// Debug logging
	console.log(`Pan: screen(${e.clientX},${e.clientY}) -> svg(${svgP.x.toFixed(1)},${svgP.y.toFixed(1)}) delta(${dx.toFixed(1)},${dy.toFixed(1)})`);
}

function endPan(e) {
	if (!isDragging) return;
	isDragging = false;
	document.getElementById('schema-container').style.cursor = 'grab';
	
	// Save view state when panning ends
	saveViewState();
}

function handleWheel(e) {
	e.preventDefault();
	
	const svg = document.getElementById('schema-svg');
	const pt = svg.createSVGPoint();
	pt.x = e.clientX;
	pt.y = e.clientY;
	
	// Get the mouse position in SVG coordinates BEFORE zoom
	const svgP = pt.matrixTransform(svg.getScreenCTM().inverse());
	
	// Determine zoom direction and factor
	const zoomFactor = e.deltaY > 0 ? 0.9 : 1.1;
	const newZoom = Math.max(MIN_ZOOM, Math.min(MAX_ZOOM, currentZoom * zoomFactor));
	
	if (newZoom === currentZoom) return; // Hit zoom limits
	
	// Calculate new viewBox size
	const newWidth = CANVAS_WIDTH / newZoom;
	const newHeight = CANVAS_HEIGHT / newZoom;
	
	// Keep the mouse position fixed during zoom
	const mouseXRatio = (svgP.x - svgViewBox.x) / svgViewBox.width;
	const mouseYRatio = (svgP.y - svgViewBox.y) / svgViewBox.height;
	
	// Calculate new viewBox position to keep mouse position fixed
	svgViewBox.x = svgP.x - (mouseXRatio * newWidth);
	svgViewBox.y = svgP.y - (mouseYRatio * newHeight);
	svgViewBox.width = newWidth;
	svgViewBox.height = newHeight;
	
	currentZoom = newZoom;
	updateViewBox();
	
	// Save view state when zooming
	saveViewState();
	
	console.log(`Zoom: ${(currentZoom * 100).toFixed(0)}% at (${svgP.x.toFixed(1)},${svgP.y.toFixed(1)})`);
}

function updateViewBox() {
	const svg = document.getElementById('schema-svg');
	svg.setAttribute('viewBox', `${svgViewBox.x} ${svgViewBox.y} ${svgViewBox.width} ${svgViewBox.height}`);
}

function zoomIn() {
	// Zoom into the center of the current view
	const centerX = svgViewBox.x + svgViewBox.width / 2;
	const centerY = svgViewBox.y + svgViewBox.height / 2;
	
	const newZoom = Math.min(MAX_ZOOM, currentZoom * 1.2);
	if (newZoom === currentZoom) return;
	
	const newWidth = CANVAS_WIDTH / newZoom;
	const newHeight = CANVAS_HEIGHT / newZoom;
	
	svgViewBox.x = centerX - newWidth / 2;
	svgViewBox.y = centerY - newHeight / 2;
	svgViewBox.width = newWidth;
	svgViewBox.height = newHeight;
	
	currentZoom = newZoom;
	updateViewBox();
	
	// Save view state when zooming via button
	saveViewState();
	
	console.log(`Button Zoom In: ${(currentZoom * 100).toFixed(0)}%`);
}

function zoomOut() {
	// Zoom out from the center of the current view
	const centerX = svgViewBox.x + svgViewBox.width / 2;
	const centerY = svgViewBox.y + svgViewBox.height / 2;
	
	const newZoom = Math.max(MIN_ZOOM, currentZoom * 0.8);
	if (newZoom === currentZoom) return;
	
	const newWidth = CANVAS_WIDTH / newZoom;
	const newHeight = CANVAS_HEIGHT / newZoom;
	
	svgViewBox.x = centerX - newWidth / 2;
	svgViewBox.y = centerY - newHeight / 2;
	svgViewBox.width = newWidth;
	svgViewBox.height = newHeight;
	
	currentZoom = newZoom;
	updateViewBox();
	
	// Save view state when zooming via button
	saveViewState();
	
	console.log(`Button Zoom Out: ${(currentZoom * 100).toFixed(0)}%`);
}

function resetZoom() {
	currentZoom = 0.35; // Changed from 1 to 0.35 to match initial zoom
	svgViewBox = { x: 0, y: 0, width: CANVAS_WIDTH, height: CANVAS_HEIGHT };
	updateViewBox();
	
	// Save view state when resetting zoom
	saveViewState();
	
	console.log('Zoom Reset: 35%'); // Updated log message
}

// Save the current view state (position and zoom level) to local storage
function saveViewState() {
	const viewState = {
		centerX: svgViewBox.x + svgViewBox.width / 2,
		centerY: svgViewBox.y + svgViewBox.height / 2,
		zoom: currentZoom
	};
	localStorage.setItem('schemaViewState', JSON.stringify(viewState));
	console.log('View state saved:', viewState);
}

// Load the view state from local storage
function loadViewState() {
	const viewState = JSON.parse(localStorage.getItem('schemaViewState'));
	if (viewState) {
		svgViewBox.x = viewState.centerX - svgViewBox.width / 2;
		svgViewBox.y = viewState.centerY - svgViewBox.height / 2;
		currentZoom = viewState.zoom;
		updateViewBox();
		console.log('View state loaded:', viewState);
	}
}

// Load view state on initial script run
loadViewState();