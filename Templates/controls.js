// Toggle functions and UI controls

// Track toolbar and legend visibility
let toolbarVisible = true;
let legendVisible = true;

function updateToggleIndicator(button, isActive) {
	if (!button) return;

	const indicator = button.querySelector(".toggle-indicator");
	if (indicator) {
		indicator.style.opacity = isActive ? "1" : "0";
	}
	button.classList.toggle("active", isActive);
}

// Update all toggle buttons to reflect current settings
function updateButtonStates() {
	const toggles = [
		["relationships-btn", showRelationships],
		["nav-props-btn", showNavigationProperties],
		["inherited-props-btn", showInheritedProperties],
		["full-height-btn", fullHeightMode],
		["snap-grid-btn", snapToGrid],
		["legend-toggle-btn", legendVisible],
	];
	toggles.forEach(([id, isActive]) => {
		updateToggleIndicator(document.getElementById(id), isActive);
	});

	const gridBg = document.getElementById("grid-background");
	if (gridBg) {
		gridBg.style.display = snapToGrid ? "block" : "none";
	}
}

function toggleRelationships() {
	showRelationships = !showRelationships;
	updateRelationships();
	updateButtonStates();
	saveSettings();
}

function toggleNavigationProperties() {
	showNavigationProperties = !showNavigationProperties;
	generateSchema();
	saveSettings();
}

function toggleInheritedProperties() {
	showInheritedProperties = !showInheritedProperties;
	generateSchema();
	saveSettings();
}

function toggleFullHeight() {
	fullHeightMode = !fullHeightMode;
	generateSchema();
	saveSettings();
}

function toggleSnapToGrid() {
	snapToGrid = !snapToGrid;
	updateButtonStates();
	saveSettings();
}

function toggleLegend() {
	legendVisible = !legendVisible;
	const legend = document.getElementById("legend");
	if (legend) {
		legend.style.display = legendVisible ? "block" : "none";
	}
	updateButtonStates();
	saveSettings();
}

function toggleToolbar() {
	toolbarVisible = !toolbarVisible;
	const toolbar = document.getElementById("toolbar");
	if (toolbar) {
		toolbar.style.display = toolbarVisible ? "flex" : "none";
	}
	// The toolbar state is not saved: it always starts visible
}

// Recompute the smart layout from scratch (ignores saved positions) and persist it
function autoLayoutTables() {
	const positions = computeSmartLayout(Object.keys(entities));
	applyPositionsToTables(positions);
	saveSettings();
}

function collectCurrentState() {
	return {
		viewBox: { ...svgViewBox },
		zoom: currentZoom,
		tablePositions: getTablePositions(),
		settings: {
			showRelationships,
			showNavigationProperties,
			showInheritedProperties,
			fullHeightMode,
			snapToGrid,
			legendVisible,
		},
		selectedTable,
		showOnlySelectedRelations,
	};
}

function buildDoctype() {
	const doctype = document.doctype;
	if (!doctype) {
		return "<!DOCTYPE html>";
	}
	const publicId = doctype.publicId ? ` PUBLIC "${doctype.publicId}"` : "";
	const systemId = doctype.systemId ? ` "${doctype.systemId}"` : "";
	return `<!DOCTYPE ${doctype.name}${publicId}${systemId}>`;
}

function triggerHtmlDownload(html) {
	const blob = new Blob([html], { type: "text/html" });
	const url = URL.createObjectURL(blob);

	const link = document.createElement("a");
	link.href = url;
	const timestamp = new Date().toISOString().slice(0, 19).replace(/:/g, "-");
	const zoomLevel = Math.round(currentZoom * 100);
	link.download = `schema-customized-${timestamp}-zoom${zoomLevel}%.html`;

	document.body.appendChild(link);
	link.click();
	document.body.removeChild(link);
	URL.revokeObjectURL(url);
}

function downloadSchema() {
	// The downloaded document gets its own GUID so its saved state is separate
	const newDocumentGuid = generateGuid();
	const currentHtml = buildDoctype() + "\n" + document.documentElement.outerHTML;

	triggerHtmlDownload(
		embedCurrentStateIntoHtml(currentHtml, collectCurrentState(), newDocumentGuid),
	);
}

function generateGuid() {
	return "xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx".replace(/[xy]/g, function (c) {
		const r = crypto.getRandomValues(new Uint8Array(1))[0] & 15;
		const v = c == "x" ? r : (r & 0x3) | 0x8;
		return v.toString(16);
	});
}

// Script injected into downloaded documents; __STATE_BASE64__ and __NEW_GUID__ are substituted
const STATE_INJECTION_LINES = [
	"// Fully editable downloaded document with embedded initial state",
	"(function() {",
	"\tconst embeddedStateBase64 = '__STATE_BASE64__';",
	"\tconst newDocumentGuid = '__NEW_GUID__';",
	"\t",
	"\ttry {",
	"\t\t// Decode the embedded state",
	"\t\tconst stateJson = decodeURIComponent(escape(atob(embeddedStateBase64)));",
	"\t\tconst embeddedState = JSON.parse(stateJson);",
	"\t\t",
	"\t\t// Create the new document-specific localStorage keys",
	"\t\tconst newStorageKeys = {",
	"\t\t\ttablePositions: `schemaMagic_${newDocumentGuid}_tablePositions`,",
	"\t\t\tviewSettings: `schemaMagic_${newDocumentGuid}_viewSettings`,",
	"\t\t\tviewBox: `schemaMagic_${newDocumentGuid}_viewBox`",
	"\t\t};",
	"\t\t",
	"\t\t// Initialize localStorage with embedded state for new document GUID",
	"\t\t// This creates a fully functional localStorage setup",
	"\t\tif (embeddedState.tablePositions) {",
	"\t\t\tlocalStorage.setItem(newStorageKeys.tablePositions, JSON.stringify(embeddedState.tablePositions));",
	"\t\t}",
	"\t\tif (embeddedState.settings) {",
	"\t\t\tlocalStorage.setItem(newStorageKeys.viewSettings, JSON.stringify(embeddedState.settings));",
	"\t\t}",
	"\t\tif (embeddedState.viewBox) {",
	"\t\t\tlocalStorage.setItem(newStorageKeys.viewBox, JSON.stringify(embeddedState.viewBox));",
	"\t\t}",
	"\t\t",
	"\t\t// No need to override localStorage - the new GUID will work with normal localStorage",
	"\t\tconsole.log('📦 Downloaded document initialized with embedded state as localStorage');",
	"\t\tconsole.log('🆔 New document GUID:', newDocumentGuid);",
	"\t\tconsole.log('✏️ Document is fully editable - changes will be saved to localStorage');",
	"\t\tconsole.log('📊 Embedded state:', {",
	"\t\t\ttableCount: Object.keys(embeddedState.tablePositions || {}).length,",
	"\t\t\tzoom: embeddedState.zoom,",
	"\t\t\tsettings: embeddedState.settings",
	"\t\t});",
	"\t\t",
	"\t} catch (error) {",
	"\t\tconsole.error('❌ Failed to initialize embedded state:', error);",
	"\t\tconsole.log('🔄 Document will start with default layout and be fully editable');",
	"\t}",
	"})();",
	"",
	"// Restore state and ensure normal functionality after DOM is loaded",
	"document.addEventListener('DOMContentLoaded', function() {",
	"\tsetTimeout(function() {",
	"\t\ttry {",
	"\t\t\t// Load settings normally - localStorage now contains the embedded data",
	"\t\t\tloadSettings();",
	"\t\t\t",
	"\t\t\t// Generate schema - this will use saved positions or auto-optimize if none exist",
	"\t\t\tgenerateSchema();",
	"\t\t\t",
	"\t\t\t// Update button states to reflect loaded settings",
	"\t\t\tupdateButtonStates();",
	"\t\t\t",
	"\t\t\t// Restore view state if available",
	"\t\t\tconst savedViewBox = localStorage.getItem(STORAGE_KEYS.viewBox);",
	"\t\t\tif (savedViewBox) {",
	"\t\t\t\ttry {",
	"\t\t\t\t\tconst viewBoxData = JSON.parse(savedViewBox);",
	"\t\t\t\t\tif (viewBoxData.zoom) {",
	"\t\t\t\t\t\tcurrentZoom = viewBoxData.zoom;",
	"\t\t\t\t\t}",
	"\t\t\t\t\tif (viewBoxData.viewBox) {",
	"\t\t\t\t\t\tsvgViewBox = { ...viewBoxData.viewBox };",
	"\t\t\t\t\t\tupdateViewBox();",
	"\t\t\t\t\t}",
	"\t\t\t\t} catch (e) {",
	"\t\t\t\t\tconsole.warn('Could not restore view state:', e);",
	"\t\t\t\t}",
	"\t\t\t}",
	"\t\t\t",
	"\t\t\t// Ensure event handlers are properly set up",
	"\t\t\tsetupEventListeners();",
	"\t\t\t",
	"\t\t\tconsole.log('✅ Downloaded document fully restored and ready for editing');",
	"\t\t\tconsole.log('💾 All changes will be saved to localStorage with GUID:', DOCUMENT_GUID);",
	"\t\t\t",
	"\t\t} catch (error) {",
	"\t\t\tconsole.error('❌ Failed to restore downloaded document state:', error);",
	"\t\t\tconsole.log('🔄 Falling back to default state - document is still fully functional');",
	"\t\t\t// Try to set up basic functionality even if restore fails",
	"\t\t\ttry {",
	"\t\t\t\tsetupEventListeners();",
	"\t\t\t\tgenerateSchema();",
	"\t\t\t\tupdateButtonStates();",
	"\t\t\t} catch (fallbackError) {",
	"\t\t\t\tconsole.error('❌ Fallback initialization also failed:', fallbackError);",
	"\t\t\t}",
	"\t\t}",
	"\t}, 100);",
	"});",
];

function buildStateInjectionScript(stateBase64, newDocumentGuid) {
	return STATE_INJECTION_LINES.join("\n")
		.replace("__STATE_BASE64__", () => stateBase64)
		.replace("__NEW_GUID__", () => newDocumentGuid);
}

function embedCurrentStateIntoHtml(html, state, newDocumentGuid) {
	const guidRegex = /const DOCUMENT_GUID = '[^']+';/;
	html = html.replace(guidRegex, `const DOCUMENT_GUID = '${newDocumentGuid}';`);

	// Base64 avoids any string escaping issues
	const stateBase64 = btoa(unescape(encodeURIComponent(JSON.stringify(state))));
	const stateInjectionScript = buildStateInjectionScript(
		stateBase64,
		newDocumentGuid,
	);

	// The end tag is written without its "<": a literal closing script tag inside a
	// script block terminates the block at the HTML parsing level.
	const scriptEndTag = "/script>";
	const scriptEndIndex = html.lastIndexOf(scriptEndTag);

	if (scriptEndIndex === -1) {
		console.warn("Could not find script tag to inject state, downloading without state");
		return html;
	}

	return (
		html.substring(0, scriptEndIndex - 1) +
		"\n\t\t" +
		stateInjectionScript +
		"\n\t" +
		html.substring(scriptEndIndex - 1)
	);
}