// Event listeners and initialization
function runInitStep(name, step) {
	try {
		step();
	} catch (e) {
		console.error("Initialization step failed:", name, e);
	}
}

document.addEventListener("DOMContentLoaded", function () {
	runInitStep("loadSettings", loadSettings);
	runInitStep("loadTableGroupingRules", loadTableGroupingRules);
	runInitStep("generateSchema", generateSchema);
	// Load view state AFTER the schema is generated (so svgViewBox is initialized)
	runInitStep("loadViewState", loadViewState);
	runInitStep("setupEventListeners", setupEventListeners);
});

function handleKeyDown(e) {
	if (e.key === "Escape" && selectedTable) {
		clearSelection();
		e.preventDefault();
	}
	if (e.ctrlKey && e.key === "h") {
		toggleToolbar();
		e.preventDefault();
	}
}

function setupEventListeners() {
	const container = document.getElementById("schema-container");
	const svg = document.getElementById("schema-svg");
	const backgroundArea = document.getElementById("background-pan-area");

	backgroundArea.addEventListener("mousedown", startBackgroundPan);
	container.addEventListener("mousemove", handlePan);
	container.addEventListener("mouseup", endPan);
	container.addEventListener("mouseleave", endPan);

	svg.addEventListener("wheel", handleWheel);
	svg.addEventListener("contextmenu", (e) => e.preventDefault());

	// Clicking anywhere shows a hidden toolbar
	container.addEventListener("click", function () {
		if (!toolbarVisible) {
			toggleToolbar();
		}
	});

	document.addEventListener("keydown", handleKeyDown);
}
