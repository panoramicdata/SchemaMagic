// Toggle functions and UI controls

function updateButtonStates() {
	// Update all button states to reflect current settings
	const relationshipsBtn = document.getElementById('relationships-btn');
	const navPropsBtn = document.getElementById('nav-props-btn');
	const inheritedPropsBtn = document.getElementById('inherited-props-btn');
	const fullHeightBtn = document.getElementById('full-height-btn');
	const snapGridBtn = document.getElementById('snap-grid-btn');

	// Relationships button
	if (relationshipsBtn) {
		relationshipsBtn.textContent = showRelationships ? 'Hide Relations' : 'Show Relations';
		relationshipsBtn.classList.toggle('active', showRelationships);
	}

	// Navigation properties button
	if (navPropsBtn) {
		navPropsBtn.classList.toggle('active', showNavigationProperties);
	}

	// Inherited properties button
	if (inheritedPropsBtn) {
		inheritedPropsBtn.classList.toggle('active', showInheritedProperties);
	}

	// Full height button
	if (fullHeightBtn) {
		fullHeightBtn.classList.toggle('active', fullHeightMode);
	}

	// Snap to grid button
	if (snapGridBtn) {
		snapGridBtn.classList.toggle('active', snapToGrid);
		// Also update grid background visibility
		const gridBg = document.getElementById('grid-background');
		if (gridBg) {
			gridBg.style.display = snapToGrid ? 'block' : 'none';
		}
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

function downloadSchema() {
	// Generate a new GUID for the downloaded document
	const newDocumentGuid = generateGuid();

	// Get current state including all customizations
	const currentState = {
		// Capture current view state
		viewBox: { ...svgViewBox },
		zoom: currentZoom,

		// Capture all table positions
		tablePositions: {},

		// Capture all settings
		settings: {
			showRelationships,
			showNavigationProperties,
			showInheritedProperties,
			fullHeightMode,
			snapToGrid
		},

		// Capture selected table if any
		selectedTable,
		showOnlySelectedRelations
	};

	// Extract current table positions
	document.querySelectorAll('.table-group').forEach(table => {
		const entityName = table.getAttribute('data-entity');
		const rect = table.querySelector('.table-box');
		currentState.tablePositions[entityName] = {
			x: parseFloat(rect.getAttribute('x')),
			y: parseFloat(rect.getAttribute('y'))
		};
	});

	// Get the complete HTML document including DOCTYPE
	const doctype = document.doctype ?
		`<!DOCTYPE ${document.doctype.name}${document.doctype.publicId ? ` PUBLIC "${document.doctype.publicId}"` : ''}${document.doctype.systemId ? ` "${document.doctype.systemId}"` : ''}>` :
		'<!DOCTYPE html>';

	const currentHtml = doctype + '\n' + document.documentElement.outerHTML;

	// Create a modified version that embeds the current state with new GUID
	const modifiedHtml = embedCurrentStateIntoHtml(currentHtml, currentState, newDocumentGuid);

	// Create a blob and download link
	const blob = new Blob([modifiedHtml], { type: 'text/html' });
	const url = URL.createObjectURL(blob);

	// Create download link with descriptive filename
	const link = document.createElement('a');
	link.href = url;
	const timestamp = new Date().toISOString().slice(0, 19).replace(/:/g, '-');
	const zoomLevel = Math.round(currentZoom * 100);
	link.download = `schema-customized-${timestamp}-zoom${zoomLevel}%.html`;

	// Trigger download
	document.body.appendChild(link);
	link.click();
	document.body.removeChild(link);

	// Clean up
	URL.revokeObjectURL(url);

	console.log(`📥 Downloaded schema with new GUID: ${newDocumentGuid} (includes DOCTYPE for Standards Mode)`);
}

function generateGuid() {
	return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function(c) {
		const r = Math.random() * 16 | 0;
		const v = c == 'x' ? r : (r & 0x3 | 0x8);
		return v.toString(16);
	});
}

function embedCurrentStateIntoHtml(html, state, newDocumentGuid) {
	// Replace the old DOCUMENT_GUID with the new one
	const guidRegex = /const DOCUMENT_GUID = '[^']+';/;
	const newGuidDeclaration = `const DOCUMENT_GUID = '${newDocumentGuid}';`;
	html = html.replace(guidRegex, newGuidDeclaration);

	// Find the entities JSON in the HTML - improve the regex to be more robust
	const entitiesRegex = /const entities = (\{[\s\S]*?\});(?=\s*\/\/|\s*const|\s*let|\s*var|\s*function|\s*document|\s*$)/;
	const match = html.match(entitiesRegex);

	if (!match) {
		console.warn('Could not find entities JSON in HTML, downloading without entities - document may not be fully interactive');
		// Continue anyway - the state injection might still make it partially functional
	}

	// Use base64 encoding to completely avoid string escaping issues
	const stateJson = JSON.stringify(state);
	const stateBase64 = btoa(unescape(encodeURIComponent(stateJson)));

	// Create a localStorage implementation that starts with embedded data but allows full editing
	const stateInjectionScript =
		'// Fully editable downloaded document with embedded initial state\n' +
		'(function() {\n' +
		'	const embeddedStateBase64 = \'' + stateBase64 + '\';\n' +
		'	const newDocumentGuid = \'' + newDocumentGuid + '\';\n' +
		'	\n' +
		'	try {\n' +
		'		// Decode the embedded state\n' +
		'		const stateJson = decodeURIComponent(escape(atob(embeddedStateBase64)));\n' +
		'		const embeddedState = JSON.parse(stateJson);\n' +
		'		\n' +
		'		// Create the new document-specific localStorage keys\n' +
		'		const newStorageKeys = {\n' +
		'			tablePositions: `schemaMagic_${newDocumentGuid}_tablePositions`,\n' +
		'			viewSettings: `schemaMagic_${newDocumentGuid}_viewSettings`,\n' +
		'			viewBox: `schemaMagic_${newDocumentGuid}_viewBox`\n' +
		'		};\n' +
		'		\n' +
		'		// Initialize localStorage with embedded state for new document GUID\n' +
		'		// This creates a fully functional localStorage setup\n' +
		'		if (embeddedState.tablePositions) {\n' +
		'			localStorage.setItem(newStorageKeys.tablePositions, JSON.stringify(embeddedState.tablePositions));\n' +
		'		}\n' +
		'		if (embeddedState.settings) {\n' +
		'			localStorage.setItem(newStorageKeys.viewSettings, JSON.stringify(embeddedState.settings));\n' +
		'		}\n' +
		'		if (embeddedState.viewBox) {\n' +
		'			localStorage.setItem(newStorageKeys.viewBox, JSON.stringify(embeddedState.viewBox));\n' +
		'		}\n' +
		'		\n' +
		'		// No need to override localStorage - the new GUID will work with normal localStorage\n' +
		'		console.log(\'📦 Downloaded document initialized with embedded state as localStorage\');\n' +
		'		console.log(\'🆔 New document GUID:\', newDocumentGuid);\n' +
		'		console.log(\'✏️ Document is fully editable - changes will be saved to localStorage\');\n' +
		'		console.log(\'📊 Embedded state:\', {\n' +
		'			tableCount: Object.keys(embeddedState.tablePositions || {}).length,\n' +
		'			zoom: embeddedState.zoom,\n' +
		'			settings: embeddedState.settings\n' +
		'		});\n' +
		'		\n' +
		'	} catch (error) {\n' +
		'		console.error(\'❌ Failed to initialize embedded state:\', error);\n' +
		'		console.log(\'🔄 Document will start with default layout and be fully editable\');\n' +
		'	}\n' +
		'})();\n' +
		'\n' +
		'// Restore state and ensure normal functionality after DOM is loaded\n' +
		'document.addEventListener(\'DOMContentLoaded\', function() {\n' +
		'	setTimeout(function() {\n' +
		'		try {\n' +
		'			// Load settings normally - localStorage now contains the embedded data\n' +
		'			loadSettings();\n' +
		'			\n' +
		'			// Generate schema - this will use saved positions or auto-optimize if none exist\n' +
		'			generateSchema();\n' +
		'			\n' +
		'			// Update button states to reflect loaded settings\n' +
		'			updateButtonStates();\n' +
		'			\n' +
		'			// Restore view state if available\n' +
		'			const savedViewBox = localStorage.getItem(STORAGE_KEYS.viewBox);\n' +
		'			if (savedViewBox) {\n' +
		'				try {\n' +
		'					const viewBoxData = JSON.parse(savedViewBox);\n' +
		'					if (viewBoxData.zoom) {\n' +
		'						currentZoom = viewBoxData.zoom;\n' +
		'					}\n' +
		'					if (viewBoxData.viewBox) {\n' +
		'						svgViewBox = { ...viewBoxData.viewBox };\n' +
		'						updateViewBox();\n' +
		'					}\n' +
		'				} catch (e) {\n' +
		'					console.warn(\'Could not restore view state:\', e);\n' +
		'				}\n' +
		'			}\n' +
		'			\n' +
		'			// Ensure event handlers are properly set up\n' +
		'			setupEventListeners();\n' +
		'			\n' +
		'			console.log(\'✅ Downloaded document fully restored and ready for editing\');\n' +
		'			console.log(\'💾 All changes will be saved to localStorage with GUID:\', DOCUMENT_GUID);\n' +
		'			\n' +
		'		} catch (error) {\n' +
		'			console.error(\'❌ Failed to restore downloaded document state:\', error);\n' +
		'			console.log(\'🔄 Falling back to default state - document is still fully functional\');\n' +
		'			// Try to set up basic functionality even if restore fails\n' +
		'			try {\n' +
		'				setupEventListeners();\n' +
		'				generateSchema();\n' +
		'				updateButtonStates();\n' +
		'			} catch (fallbackError) {\n' +
		'				console.error(\'❌ Fallback initialization also failed:\', fallbackError);\n' +
		'			}\n' +
		'		}\n' +
		'	}, 100);\n' +
		'});';

	// ⚠️ CRITICAL: DO NOT CHANGE THE LINE BELOW! ⚠️
	// The scriptEndTag MUST be '/script>' NOT the full closing tag with < bracket
	// Having the full closing script tag as a string literal inside a script tag will
	// break HTML parsing because browsers process that pattern at the lexical level
	// before JavaScript parsing. This is a well-known HTML gotcha - the closing script
	// tag terminates the script block even when it appears inside quotes or comments.
	// Using '/script>' (without the < bracket) avoids this issue entirely.
	// 🚨 NEVER "fix" this to include the opening < bracket! 🚨
	const scriptEndTag = '/script>';
	const scriptEndIndex = html.lastIndexOf(scriptEndTag);

	if (scriptEndIndex === -1) {
		console.warn('Could not find script tag to inject state, downloading without state');
		return html;
	}

	// Insert the state initialization script before the closing script tag.
	const modifiedHtml =
		html.substring(0, scriptEndIndex - 1) +
		'\n\t\t' + stateInjectionScript + '\n\t' +
		html.substring(scriptEndIndex - 1);

	return modifiedHtml;
}