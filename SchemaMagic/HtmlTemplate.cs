namespace SchemaMagic.Tool;

public static class HtmlTemplate
{
	public static string Generate(string entitiesJson)
	{
		return $@"<!DOCTYPE html>
<html lang=""en"">
<head>
	<meta charset=""UTF-8"">
	<meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
	<title>Entity Framework Schema Visualization - SchemaMagic</title>
	<style>
		* {{
			margin: 0;
			padding: 0;
			box-sizing: border-box;
		}}

		body {{
			font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
			background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
			height: 100vh;
			overflow: hidden;
		}}

		.container {{
			width: 100vw;
			height: 100vh;
			display: flex;
			flex-direction: column;
		}}

		.toolbar {{
			background: rgba(255, 255, 255, 0.95);
			padding: 15px 20px;
			display: flex;
			align-items: center;
			gap: 15px;
			box-shadow: 0 2px 10px rgba(0,0,0,0.1);
			z-index: 1000;
			border-bottom: 1px solid #ddd;
			flex-shrink: 0;
		}}

		.toolbar h1 {{
			color: #333;
			font-size: 24px;
			margin-right: 20px;
			background: linear-gradient(45deg, #667eea, #764ba2);
			-webkit-background-clip: text;
			-webkit-text-fill-color: transparent;
			background-clip: text;
		}}

		.toolbar button {{
			background: #667eea;
			color: white;
			border: none;
			padding: 10px 16px;
			border-radius: 8px;
			cursor: pointer;
			font-size: 14px;
			font-weight: 500;
			transition: all 0.3s ease;
			box-shadow: 0 2px 4px rgba(0,0,0,0.1);
		}}

		.toolbar button:hover {{
			background: #5a67d8;
			transform: translateY(-2px);
			box-shadow: 0 4px 8px rgba(0,0,0,0.15);
		}}

		.toolbar button.active {{
			background: #764ba2;
			box-shadow: inset 0 2px 4px rgba(0,0,0,0.1);
		}}

		.toolbar button.download {{
			background: #059669;
		}}

		.toolbar button.download:hover {{
			background: #047857;
		}}

		.legend {{
			background: rgba(255, 255, 255, 0.9);
			padding: 15px;
			border-radius: 8px;
			font-size: 12px;
			line-height: 1.6;
			box-shadow: 0 4px 12px rgba(0,0,0,0.15);
			margin-left: auto;
		}}

		.schema-container {{
			flex: 1;
			position: relative;
			overflow: hidden;
			cursor: grab;
			background: #f8fafc;
			user-select: none;
		}}

		.schema-container:active {{
			cursor: grabbing;
		}}

		#schema-svg {{
			width: 100%;
			height: 100%;
			position: absolute;
			top: 0;
			left: 0;
		}}

		.table-box {{
			fill: white;
			stroke: #e2e8f0;
			stroke-width: 2;
			filter: drop-shadow(0 4px 6px rgba(0, 0, 0, 0.1));
			cursor: move;
			transition: all 0.3s ease;
		}}

		.table-box:hover {{
			stroke: #667eea;
			stroke-width: 3;
			fill: #fafbfc;
		}}

		.table-box.selected {{
			stroke: #764ba2;
			stroke-width: 4;
			fill: #f7fafc;
		}}

		.table-box.inherited {{
			fill: #f0f9ff;
			stroke: #0ea5e9;
		}}

		.table-box.inherited:hover {{
			fill: #e0f2fe;
			stroke: #0284c7;
		}}

		.table-header {{
			fill: #667eea;
		}}

		.table-header.inherited {{
			fill: #0ea5e9;
		}}

		.table-title {{
			fill: white;
			font-weight: bold;
			font-size: 16px;
			font-family: 'Segoe UI', sans-serif;
		}}

		.inheritance-text {{
			fill: #64748b;
			font-size: 11px;
			font-style: italic;
			font-family: 'Segoe UI', sans-serif;
		}}

		.property-text {{
			font-size: 13px;
			font-family: 'Consolas', 'Monaco', monospace;
			fill: #2d3748;
		}}

		.property-key {{
			fill: #d69e2e;
			font-weight: bold;
		}}

		.property-foreign-key {{
			fill: #9f7aea;
			font-weight: bold;
		}}

		.property-navigation {{
			fill: #059669;
			cursor: pointer;
			text-decoration: underline;
		}}

		.property-navigation:hover {{
			fill: #047857;
		}}

		.property-inherited {{
			fill: #0369a1;
			font-style: italic;
		}}

		.property-type {{
			fill: #718096;
			text-anchor: end;
			font-size: 12px;
		}}

		.property-icon {{
			fill: #9ca3af;
			font-size: 14px;
		}}

		.property-group {{
			fill: #f1f5f9;
			stroke: #cbd5e1;
			stroke-width: 1;
			stroke-dasharray: 2,2;
		}}

		.inherited-section {{
			fill: #e0f2fe;
			stroke: #0284c7;
			stroke-width: 1;
		}}

		.section-divider {{
			stroke: #cbd5e1;
			stroke-width: 1;
		}}

		.relationship-line {{
			stroke: #a0aec0;
			stroke-width: 2;
			fill: none;
			marker-end: url(#arrowhead);
			transition: all 0.3s ease;
		}}

		.relationship-line.highlighted {{
			stroke: #667eea;
			stroke-width: 4;
		}}

		.relationship-line.dimmed {{
			stroke: #e2e8f0;
			stroke-width: 1;
			opacity: 0.3;
		}}

		.relationship-line.hidden {{
			display: none;
		}}

		.grid-pattern {{
			fill: none;
			stroke: #e2e8f0;
			stroke-width: 1;
			opacity: 0.3;
		}}

		.boundary-indicator {{
			fill: none;
			stroke: #ef4444;
			stroke-width: 3;
			stroke-dasharray: 10,5;
			opacity: 0.7;
		}}

		.table-group {{
			cursor: move;
		}}

		.table-group.dragging .table-box {{
			transition: none !important;
		}}

		.table-group.dragging * {{
			transition: none !important;
		}}
	</style>
</head>
<body>
	<div class=""container"">
		<div class=""toolbar"">
			<h1>SchemaMagic</h1>
			<button onclick=""toggleRelationships()"" id=""relationships-btn"">Hide Relations</button>
			<button onclick=""toggleNavigationProperties()"" id=""nav-props-btn"">Navigation Properties</button>
			<button onclick=""toggleInheritedProperties()"" id=""inherited-props-btn"" class=""active"">Show Inherited</button>
			<button onclick=""toggleFullHeight()"" id=""full-height-btn"">Full Height</button>
			<button onclick=""toggleSnapToGrid()"" id=""snap-grid-btn"">Snap to Grid</button>
			<button onclick=""zoomIn()"">Zoom In</button>
			<button onclick=""zoomOut()"">Zoom Out</button>
			<button onclick=""resetZoom()"">Reset Zoom</button>
			<button onclick=""downloadSchema()"" class=""download"">Download</button>

			<div class=""legend"">
				<strong>Schema Legend:</strong><br>
				PK: Primary Key | FK: Foreign Key | NAV: Navigation | INH: Inherited | Drag tables | Drag background to pan | Click to focus
			</div>
		</div>

		<div class=""schema-container"" id=""schema-container"">
			<svg id=""schema-svg"" viewBox=""0 0 8000 6000"">
				<defs>
					<marker id=""arrowhead"" markerWidth=""12"" markerHeight=""8""
					 refX=""11"" refY=""4"" orient=""auto"">
						<polygon points=""0 0, 12 4, 0 8"" fill=""#a0aec0"" />
					</marker>
					<marker id=""arrowhead-highlighted"" markerWidth=""12"" markerHeight=""8""
					 refX=""11"" refY=""4"" orient=""auto"">
						<polygon points=""0 0, 12 4, 0 8"" fill=""#667eea"" />
					</marker>
					<pattern id=""grid"" width=""20"" height=""20"" patternUnits=""userSpaceOnUse"">
						<circle cx=""10"" cy=""10"" r=""1"" class=""grid-pattern""/>
					</pattern>
				</defs>

				<rect id=""grid-background"" width=""100%"" height=""100%"" fill=""url(#grid)"" style=""display: none;""/>

				<!-- Boundary indicators -->
				<rect class=""boundary-indicator"" x=""50"" y=""50"" width=""7900"" height=""5900"" style=""display: none;"" id=""boundary-rect""/>

				<rect id=""background-pan-area"" width=""100%"" height=""100%"" fill=""transparent"" style=""cursor: grab;""/>
			</svg>
		</div>
	</div>

	<script>
		const entities = {entitiesJson};

		let selectedTable = null;
		let showOnlySelectedRelations = false;
		let showRelationships = true;
		let showNavigationProperties = false;
		let showInheritedProperties = true;
		let fullHeightMode = false;
		let snapToGrid = false;
		let currentZoom = 1;
		let isPanning = false;
		let panStart = {{ x: 0, y: 0 }};
		let svgViewBox = {{ x: 0, y: 0, width: 8000, height: 6000 }};
		const CANVAS_WIDTH = 8000;
		const CANVAS_HEIGHT = 6000;
		const BOUNDARY_MARGIN = 100;
		const MAX_ZOOM = 15; // Increased from 3 to 15 for 5x more zoom

		// Local storage keys
		const STORAGE_KEYS = {{
			tablePositions: 'schemaMagic_tablePositions',
			viewSettings: 'schemaMagic_viewSettings'
		}};

		document.addEventListener('DOMContentLoaded', function() {{
			loadSettings();
			generateSchema();
			setupEventListeners();
		}});

		function setupEventListeners() {{
			const container = document.getElementById('schema-container');
			const svg = document.getElementById('schema-svg');
			const backgroundArea = document.getElementById('background-pan-area');

			backgroundArea.addEventListener('mousedown', startBackgroundPan);
			container.addEventListener('mousemove', handlePan);
			container.addEventListener('mouseup', endPan);
			container.addEventListener('mouseleave', endPan);

			svg.addEventListener('wheel', handleWheel);
			svg.addEventListener('contextmenu', e => e.preventDefault());
		}}

		function startBackgroundPan(e) {{
			if (e.target.closest('.table-group')) return;

			isPanning = true;
			panStart = {{
				x: e.clientX,
				y: e.clientY
			}};
			document.getElementById('schema-container').style.cursor = 'grabbing';
			e.preventDefault();
		}}

		function handlePan(e) {{
			if (!isPanning) return;

			const deltaX = (e.clientX - panStart.x) / currentZoom;
			const deltaY = (e.clientY - panStart.y) / currentZoom;

			svgViewBox.x -= deltaX;
			svgViewBox.y -= deltaY;

			panStart.x = e.clientX;
			panStart.y = e.clientY;

			updateViewBox();
			e.preventDefault();
		}}

		function endPan(e) {{
			if (!isPanning) return;
			isPanning = false;
			document.getElementById('schema-container').style.cursor = 'grab';
		}}

		function handleWheel(e) {{
			e.preventDefault();
			const rect = e.currentTarget.getBoundingClientRect();
			const mouseX = e.clientX - rect.left;
			const mouseY = e.clientY - rect.top;

			const svgX = (mouseX / rect.width) * svgViewBox.width + svgViewBox.x;
			const svgY = (mouseY / rect.height) * svgViewBox.height + svgViewBox.y;

			const delta = e.deltaY > 0 ? 0.9 : 1.1;
			const newZoom = Math.max(0.1, Math.min(MAX_ZOOM, currentZoom * delta));

			const zoomFactor = newZoom / currentZoom;
			const newWidth = svgViewBox.width / zoomFactor;
			const newHeight = svgViewBox.height / zoomFactor;

			svgViewBox.x = svgX - (svgX - svgViewBox.x) / zoomFactor;
			svgViewBox.y = svgY - (svgY - svgViewBox.y) / zoomFactor;
			svgViewBox.width = newWidth;
			svgViewBox.height = newHeight;

			currentZoom = newZoom;
			updateViewBox();
		}}

		function updateViewBox() {{
			const svg = document.getElementById('schema-svg');
			svg.setAttribute('viewBox', `${{svgViewBox.x}} ${{svgViewBox.y}} ${{svgViewBox.width}} ${{svgViewBox.height}}`);
		}}

		function zoomIn() {{
			const centerX = svgViewBox.x + svgViewBox.width / 2;
			const centerY = svgViewBox.y + svgViewBox.height / 2;

			currentZoom = Math.min(MAX_ZOOM, currentZoom * 1.2);
			svgViewBox.width = CANVAS_WIDTH / currentZoom;
			svgViewBox.height = CANVAS_HEIGHT / currentZoom;
			svgViewBox.x = centerX - svgViewBox.width / 2;
			svgViewBox.y = centerY - svgViewBox.height / 2;

			updateViewBox();
		}}

		function zoomOut() {{
			const centerX = svgViewBox.x + svgViewBox.width / 2;
			const centerY = svgViewBox.y + svgViewBox.height / 2;

			currentZoom = Math.max(0.1, currentZoom * 0.8);
			svgViewBox.width = CANVAS_WIDTH / currentZoom;
			svgViewBox.height = CANVAS_HEIGHT / currentZoom;
			svgViewBox.x = centerX - svgViewBox.width / 2;
			svgViewBox.y = centerY - svgViewBox.height / 2;

			updateViewBox();
		}}

		function resetZoom() {{
			currentZoom = 1;
			svgViewBox = {{ x: 0, y: 0, width: CANVAS_WIDTH, height: CANVAS_HEIGHT }};
			updateViewBox();
		}}

		function saveSettings() {{
			// Save table positions
			const positions = {{}};
			document.querySelectorAll('.table-group').forEach(table => {{
				const entityName = table.getAttribute('data-entity');
				const rect = table.querySelector('.table-box');
				positions[entityName] = {{
					x: parseFloat(rect.getAttribute('x')),
					y: parseFloat(rect.getAttribute('y'))
				}};
			}});
			localStorage.setItem(STORAGE_KEYS.tablePositions, JSON.stringify(positions));

			// Save view settings
			const settings = {{
				showRelationships,
				showNavigationProperties,
				showInheritedProperties,
				fullHeightMode,
				snapToGrid
			}};
			localStorage.setItem(STORAGE_KEYS.viewSettings, JSON.stringify(settings));
		}}

		function loadSettings() {{
			// Load view settings
			try {{
				const savedSettings = localStorage.getItem(STORAGE_KEYS.viewSettings);
				if (savedSettings) {{
					const settings = JSON.parse(savedSettings);
					showRelationships = settings.showRelationships ?? true;
					showNavigationProperties = settings.showNavigationProperties ?? false;
					showInheritedProperties = settings.showInheritedProperties ?? true;
					fullHeightMode = settings.fullHeightMode ?? false;
					snapToGrid = settings.snapToGrid ?? false;
				}}
			}} catch (e) {{
				console.warn('Failed to load settings:', e);
			}}
		}}

		function updateButtonStates() {{
			// Update button active states based on current settings
			const relationshipsBtn = document.getElementById('relationships-btn');
			relationshipsBtn.classList.toggle('active', !showRelationships);
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
		}}

		function generateSchema() {{
			const svg = document.getElementById('schema-svg');

			const existingContent = svg.querySelectorAll('.table-group, .relationship-line');
			existingContent.forEach(el => el.remove());

			const tablePositions = calculateTablePositions();

			Object.keys(entities).forEach((entityName) => {{
				const entity = entities[entityName];
				const position = tablePositions[entityName];
				generateTable(svg, entity, position.x, position.y);
			}});

			if (showRelationships) {{
				generateRelationships(svg);
			}}

			updateButtonStates();
		}}

		function calculateTablePositions() {{
			// Try to load saved positions first
			let savedPositions = null;
			try {{
				const saved = localStorage.getItem(STORAGE_KEYS.tablePositions);
				if (saved) {{
					savedPositions = JSON.parse(saved);
				}}
			}} catch (e) {{
				console.warn('Failed to load saved positions:', e);
			}}

			const positions = {{}};
			const entityNames = Object.keys(entities);

			if (savedPositions) {{
				// Use saved positions if available
				entityNames.forEach(entityName => {{
					if (savedPositions[entityName]) {{
						positions[entityName] = savedPositions[entityName];
					}} else {{
						// Default position for new entities
						positions[entityName] = {{ x: 200, y: 200 }};
					}}
				}});
			}} else {{
				// Calculate centered grid layout
				const cols = Math.ceil(Math.sqrt(entityNames.length));
				const baseSpacing = 500;
				const totalWidth = cols * baseSpacing;
				const totalHeight = Math.ceil(entityNames.length / cols) * baseSpacing;

				// Center the grid in the canvas
				const startX = (CANVAS_WIDTH - totalWidth) / 2;
				const startY = (CANVAS_HEIGHT - totalHeight) / 2;

				entityNames.forEach((entityName, index) => {{
					const row = Math.floor(index / cols);
					const col = index % cols;

					positions[entityName] = {{
						x: startX + col * baseSpacing + (row % 2) * 250,
						y: startY + row * baseSpacing
					}};
				}});
			}}

			return positions;
		}}

		function getPropertyIcon(property) {{
			if (property.isInherited) return 'INH';
			if (property.isKey) return 'PK';
			if (property.isForeignKey) return 'FK';
			if (isNavigationProperty(property)) return 'NAV';
			return '';
		}}

		function sortProperties(properties) {{
			return properties.sort((a, b) => {{
				// Primary sort: inherited properties first
				if (a.isInherited && !b.isInherited) return -1;
				if (!a.isInherited && b.isInherited) return 1;

				// Secondary sort: alphabetical within each group
				return a.name.localeCompare(b.name);
			}});
		}}

		function generateTable(svg, entity, x, y) {{
			const properties = entity.properties || [];
			const inheritedProps = showInheritedProperties ? (entity.inheritedProperties || []) : [];

			// Combine and mark inherited properties
			const allProperties = [
				...inheritedProps.map(p => ({{ ...p, isInherited: true }})),
				...properties.map(p => ({{ ...p, isInherited: false }}))
			];

			// Sort properties: inherited first (alphabetically), then regular (alphabetically)
			const sortedProperties = sortProperties(allProperties);

			// Filter visible properties based on navigation toggle
			const visibleProperties = showNavigationProperties ?
				sortedProperties :
				sortedProperties.filter(p => !isNavigationProperty(p));

			// Group properties with their related navigation properties
			const groupedProperties = groupPropertiesWithNavigation(visibleProperties, sortedProperties);

			const rowHeight = 22;
			const headerHeight = 35;
			const inheritanceHeight = (entity.baseType && showInheritedProperties) ? 20 : 0;
			const padding = 12;
			const minWidth = 350;

			// Calculate dimensions based on ALL visible content (including navigation properties if shown)
			const contentForSizing = showNavigationProperties ? sortedProperties : visibleProperties;

			const maxNameLength = Math.max(
				entity.type.length,
				...contentForSizing.map(p => p.name.length + (getPropertyIcon(p).length > 0 ? 5 : 0))
			);

			const maxTypeLength = Math.max(
				0,
				...contentForSizing.map(p => p.type.length)
			);

			// Calculate proper widths with consistent spacing
			const nameWidth = Math.max(120, maxNameLength * 8);
			const typeWidth = Math.max(80, maxTypeLength * 7);
			const iconWidth = 50; // Space for icons like PK, FK, NAV, INH
			const tableWidth = Math.max(minWidth, nameWidth + typeWidth + iconWidth + padding * 4);

			// For full height mode, consider navigation properties toggle
			const maxVisibleRows = fullHeightMode ? groupedProperties.length : Math.min(groupedProperties.length, 15);
			const calculatedHeight = headerHeight + inheritanceHeight + (maxVisibleRows * rowHeight) + padding;
			const tableHeight = calculatedHeight;

			const hasInheritedProperties = inheritedProps.length > 0 && showInheritedProperties;

			const tableGroup = document.createElementNS('http://www.w3.org/2000/svg', 'g');
			tableGroup.classList.add('table-group');
			tableGroup.setAttribute('data-entity', entity.type);

			const tableRect = document.createElementNS('http://www.w3.org/2000/svg', 'rect');
			tableRect.setAttribute('x', x);
			tableRect.setAttribute('y', y);
			tableRect.setAttribute('width', tableWidth);
			tableRect.setAttribute('height', tableHeight);
			tableRect.classList.add('table-box');
			if (hasInheritedProperties) tableRect.classList.add('inherited');
			tableGroup.appendChild(tableRect);

			const headerRect = document.createElementNS('http://www.w3.org/2000/svg', 'rect');
			headerRect.setAttribute('x', x);
			headerRect.setAttribute('y', y);
			headerRect.setAttribute('width', tableWidth);
			headerRect.setAttribute('height', headerHeight);
			headerRect.classList.add('table-header');
			if (hasInheritedProperties) headerRect.classList.add('inherited');
			tableGroup.appendChild(headerRect);

			const titleText = document.createElementNS('http://www.w3.org/2000/svg', 'text');
			titleText.setAttribute('x', x + tableWidth / 2);
			titleText.setAttribute('y', y + headerHeight / 2 + 6);
			titleText.setAttribute('text-anchor', 'middle');
			titleText.classList.add('table-title');
			titleText.textContent = entity.type;
			tableGroup.appendChild(titleText);

			// Add inheritance indicator (only if showing inherited properties)
			if (entity.baseType && showInheritedProperties) {{
				const inheritanceText = document.createElementNS('http://www.w3.org/2000/svg', 'text');
				inheritanceText.setAttribute('x', x + tableWidth / 2);
				inheritanceText.setAttribute('y', y + headerHeight + 14);
				inheritanceText.setAttribute('text-anchor', 'middle');
				inheritanceText.classList.add('inheritance-text');
				inheritanceText.textContent = 'inherits from ' + entity.baseType;
				tableGroup.appendChild(inheritanceText);
			}}

			// Render grouped properties
			const propertiesToShow = groupedProperties.slice(0, maxVisibleRows);
			let currentY = y + headerHeight + inheritanceHeight;
			let lastPropertyWasInherited = false;

			propertiesToShow.forEach((item, index) => {{
				if (item.type === 'group') {{
					// Check if we need a section divider
					const firstProp = item.properties[0];
					if (lastPropertyWasInherited && !firstProp.isInherited) {{
						// Add section divider line
						const divider = document.createElementNS('http://www.w3.org/2000/svg', 'line');
						divider.setAttribute('x1', x + 4);
						divider.setAttribute('y1', currentY - 4);
						divider.setAttribute('x2', x + tableWidth - 4);
						divider.setAttribute('y2', currentY - 4);
						divider.classList.add('section-divider');
						tableGroup.appendChild(divider);
					}}

					// Draw group background
					const groupRect = document.createElementNS('http://www.w3.org/2000/svg', 'rect');
					groupRect.setAttribute('x', x + 4);
					groupRect.setAttribute('y', currentY - 8);
					groupRect.setAttribute('width', tableWidth - 8);
					groupRect.setAttribute('height', item.properties.length * rowHeight + 8);
					groupRect.classList.add('property-group');
					if (firstProp.isInherited) {{
						groupRect.classList.add('inherited-section');
					}}
					tableGroup.appendChild(groupRect);

					// Render each property in the group
					item.properties.forEach((property, propIndex) => {{
						const propY = currentY + (propIndex * rowHeight) + 16;
						renderProperty(tableGroup, property, x, propY, tableWidth, padding, entity.type);
					}});

					currentY += item.properties.length * rowHeight;
					lastPropertyWasInherited = item.properties[item.properties.length - 1].isInherited;
				}} else {{
					// Check if we need a section divider
					if (lastPropertyWasInherited && !item.property.isInherited) {{
						// Add section divider line
						const divider = document.createElementNS('http://www.w3.org/2000/svg', 'line');
						divider.setAttribute('x1', x + 4);
						divider.setAttribute('y1', currentY - 4);
						divider.setAttribute('x2', x + tableWidth - 4);
						divider.setAttribute('y2', currentY - 4);
						divider.classList.add('section-divider');
						tableGroup.appendChild(divider);
					}}

					const propY = currentY + 16;
					renderProperty(tableGroup, item.property, x, propY, tableWidth, padding, entity.type);
					currentY += rowHeight;
					lastPropertyWasInherited = item.property.isInherited;
				}}
			}});

			// Add ellipsis if there are more properties
			if (!fullHeightMode && groupedProperties.length > maxVisibleRows) {{
				const ellipsisY = currentY + 6;
				const ellipsisText = document.createElementNS('http://www.w3.org/2000/svg', 'text');
				ellipsisText.setAttribute('x', x + tableWidth / 2);
				ellipsisText.setAttribute('y', ellipsisY);
				ellipsisText.setAttribute('text-anchor', 'middle');
				ellipsisText.classList.add('property-text');
				ellipsisText.textContent = '... (+' + (groupedProperties.length - maxVisibleRows) + ' more)';
				ellipsisText.style.fontSize = '11px';
				ellipsisText.style.fill = '#9ca3af';
				tableGroup.appendChild(ellipsisText);
			}}

			setupTableInteraction(tableGroup, entity.type);
			svg.appendChild(tableGroup);
		}}

		function groupPropertiesWithNavigation(visibleProperties, allProperties) {{
			const grouped = [];
			const processed = new Set();

			visibleProperties.forEach(prop => {{
				if (processed.has(prop.name)) return;

				if (prop.isForeignKey && showNavigationProperties) {{
					// Find corresponding navigation property only when showing nav properties
					const navPropName = prop.name.replace('Id', '');
					const navProp = allProperties.find(p => p.name === navPropName && isNavigationProperty(p));

					if (navProp) {{
						grouped.push({{
							type: 'group',
							properties: [prop, navProp]
						}});
						processed.add(prop.name);
						processed.add(navProp.name);
					}} else {{
						grouped.push({{
							type: 'single',
							property: prop
						}});
						processed.add(prop.name);
					}}
				}} else {{
					grouped.push({{
						type: 'single',
						property: prop
					}});
					processed.add(prop.name);
				}}
			}});

			return grouped;
		}}

		function renderProperty(tableGroup, property, x, propY, tableWidth, padding, entityName) {{
			const propText = document.createElementNS('http://www.w3.org/2000/svg', 'text');
			propText.setAttribute('x', x + padding);
			propText.setAttribute('y', propY);
			propText.classList.add('property-text');

			if (property.isInherited) {{
				propText.classList.add('property-inherited');
			}} else if (property.isKey) {{
				propText.classList.add('property-key');
			}} else if (property.isForeignKey) {{
				propText.classList.add('property-foreign-key');
			}} else if (isNavigationProperty(property)) {{
				propText.classList.add('property-navigation');
				propText.style.cursor = 'pointer';
				propText.addEventListener('click', () => navigateToEntity(property.type));
			}}

			propText.textContent = property.name;
			tableGroup.appendChild(propText);

			// Add icon with consistent positioning
			const icon = getPropertyIcon(property);
			if (icon) {{
				const iconText = document.createElementNS('http://www.w3.org/2000/svg', 'text');
				iconText.setAttribute('x', x + tableWidth - padding - 80); // More consistent icon positioning
				iconText.setAttribute('y', propY);
				iconText.classList.add('property-icon');
				iconText.textContent = icon;
				tableGroup.appendChild(iconText);
			}}

			// Property type (right-aligned with consistent spacing)
			const typeText = document.createElementNS('http://www.w3.org/2000/svg', 'text');
			typeText.setAttribute('x', x + tableWidth - padding);
			typeText.setAttribute('y', propY);
			typeText.classList.add('property-text', 'property-type');
			typeText.textContent = property.type;
			tableGroup.appendChild(typeText);
		}}

		function navigateToEntity(typeName) {{
			// Clean up type name (remove generic brackets, nullability, etc.)
			const cleanTypeName = typeName.replace(/\\?$/, '').replace(/ICollection<(.+)>/, '$1').replace(/List<(.+)>/, '$1');

			const targetTable = document.querySelector('[data-entity=""' + cleanTypeName + '""]');
			if (targetTable) {{
				selectTable(cleanTypeName);

				// Center the target table in view
				const rect = targetTable.querySelector('.table-box');
				const targetX = parseFloat(rect.getAttribute('x'));
				const targetY = parseFloat(rect.getAttribute('y'));
				const targetWidth = parseFloat(rect.getAttribute('width'));
				const targetHeight = parseFloat(rect.getAttribute('height'));

				svgViewBox.x = targetX - svgViewBox.width / 2 + targetWidth / 2;
				svgViewBox.y = targetY - svgViewBox.height / 2 + targetHeight / 2;
				updateViewBox();
			}}
		}}

		function isNavigationProperty(property) {{
			const navPatterns = [
				/^ICollection</,
				/^List</,
				/^IList</,
				/^HashSet</,
				/^ISet</,
				/Model$/,
				/Entity$/
			];

			return navPatterns.some(pattern => pattern.test(property.type));
		}}

		function setupTableInteraction(tableGroup, entityName) {{
			let dragData = null;

			tableGroup.addEventListener('mousedown', function(e) {{
				e.stopPropagation();
				selectTable(entityName);

				const rect = tableGroup.querySelector('.table-box');

				const svg = document.getElementById('schema-svg');
				const svgRect = svg.getBoundingClientRect();
				const svgX = (e.clientX - svgRect.left) * svgViewBox.width / svgRect.width + svgViewBox.x;
				const svgY = (e.clientY - svgRect.top) * svgViewBox.height / svgRect.height + svgViewBox.y;

				dragData = {{
					startX: svgX,
					startY: svgY,
					elementX: parseFloat(rect.getAttribute('x')),
					elementY: parseFloat(rect.getAttribute('y'))
				}};

				tableGroup.classList.add('dragging');

				document.addEventListener('mousemove', handleTableDrag);
				document.addEventListener('mouseup', handleTableDragEnd);
			}});

			function handleTableDrag(e) {{
				if (!dragData) return;

				const svg = document.getElementById('schema-svg');
				const svgRect = svg.getBoundingClientRect();
				const svgX = (e.clientX - svgRect.left) * svgViewBox.width / svgRect.width + svgViewBox.x;
				const svgY = (e.clientY - svgRect.top) * svgViewBox.height / svgRect.height + svgViewBox.y;

				const deltaX = svgX - dragData.startX;
				const deltaY = svgY - dragData.startY;

				let newX = dragData.elementX + deltaX;
				let newY = dragData.elementY + deltaY;

				// Check boundaries and show indicator
				const rect = tableGroup.querySelector('.table-box');
				const tableWidth = parseFloat(rect.getAttribute('width'));
				const tableHeight = parseFloat(rect.getAttribute('height'));

				const boundaryRect = document.getElementById('boundary-rect');
				if (newX < BOUNDARY_MARGIN || newY < BOUNDARY_MARGIN ||
					newX + tableWidth > CANVAS_WIDTH - BOUNDARY_MARGIN ||
					newY + tableHeight > CANVAS_HEIGHT - BOUNDARY_MARGIN) {{
					boundaryRect.style.display = 'block';
				}} else {{
					boundaryRect.style.display = 'none';
				}}

				if (snapToGrid) {{
					newX = Math.round(newX / 20) * 20;
					newY = Math.round(newY / 20) * 20;
				}}

				moveTable(tableGroup, newX, newY);
				updateRelationships();
			}}

			function handleTableDragEnd() {{
				if (dragData) {{
					tableGroup.classList.remove('dragging');
					document.getElementById('boundary-rect').style.display = 'none';
					// Save positions after drag
					saveSettings();
				}}
				dragData = null;
				document.removeEventListener('mousemove', handleTableDrag);
				document.removeEventListener('mouseup', handleTableDragEnd);
			}}
		}}

		function moveTable(tableGroup, x, y) {{
			const elements = tableGroup.querySelectorAll('rect, text, line');
			const oldRect = tableGroup.querySelector('.table-box');
			const oldX = parseFloat(oldRect.getAttribute('x'));
			const oldY = parseFloat(oldRect.getAttribute('y'));
			const deltaX = x - oldX;
			const deltaY = y - oldY;

			elements.forEach(element => {{
				if (element.tagName === 'line') {{
					// Handle line elements differently
					const x1 = parseFloat(element.getAttribute('x1'));
					const y1 = parseFloat(element.getAttribute('y1'));
					const x2 = parseFloat(element.getAttribute('x2'));
					const y2 = parseFloat(element.getAttribute('y2'));
					element.setAttribute('x1', x1 + deltaX);
					element.setAttribute('y1', y1 + deltaY);
					element.setAttribute('x2', x2 + deltaX);
					element.setAttribute('y2', y2 + deltaY);
				}} else {{
					// Handle rect and text elements
					const currentX = parseFloat(element.getAttribute('x'));
					const currentY = parseFloat(element.getAttribute('y'));
					element.setAttribute('x', currentX + deltaX);
					element.setAttribute('y', currentY + deltaY);
				}}
			}});
		}}

		function selectTable(entityName) {{
			document.querySelectorAll('.table-box.selected').forEach(box => {{
				box.classList.remove('selected');
			}});

			const tableGroup = document.querySelector('[data-entity=""' + entityName + '""]');
			if (tableGroup) {{
				tableGroup.querySelector('.table-box').classList.add('selected');
				selectedTable = entityName;

				if (showOnlySelectedRelations) {{
					updateRelationships();
				}}
			}}
		}}

		function generateRelationships(svg) {{
			if (!showRelationships) return;

			Object.values(entities).forEach(entity => {{
				const fromEntityName = entity.type;
				entity.properties.forEach(prop => {{
					if (prop.isForeignKey) {{
						const targetEntityName = prop.name.replace('Id', '');
						if (entities[targetEntityName]) {{
							createRelationshipLine(svg, fromEntityName, targetEntityName, prop.name);
						}}
					}}
				}});
			}});
		}}

		function createRelationshipLine(svg, fromEntityName, toEntityName, propertyName) {{
			const fromInfo = getTableConnectionPoint(fromEntityName, propertyName, 'out');
			const toInfo = getTableConnectionPoint(toEntityName, 'Id', 'in');

			if (!fromInfo || !toInfo) return;

			const line = document.createElementNS('http://www.w3.org/2000/svg', 'path');

			// Create angled connector
			const midX = (fromInfo.x + toInfo.x) / 2;
			const pathData = 'M ' + fromInfo.x + ' ' + fromInfo.y + ' L ' + midX + ' ' + fromInfo.y + ' L ' + midX + ' ' + toInfo.y + ' L ' + toInfo.x + ' ' + toInfo.y;

			line.setAttribute('d', pathData);
			line.classList.add('relationship-line');
			line.setAttribute('data-from', fromEntityName);
			line.setAttribute('data-to', toEntityName);
			line.setAttribute('data-property', propertyName);

			svg.appendChild(line);
		}}

		function getTableConnectionPoint(entityName, propertyName, direction) {{
			const tableGroup = document.querySelector('[data-entity=""' + entityName + '""]');
			if (!tableGroup) return null;

			const rect = tableGroup.querySelector('.table-box');
			const x = parseFloat(rect.getAttribute('x'));
			const y = parseFloat(rect.getAttribute('y'));
			const width = parseFloat(rect.getAttribute('width'));
			const height = parseFloat(rect.getAttribute('height'));

			// For now, use center points of edges
			if (direction === 'out') {{
				return {{ x: x + width, y: y + height / 2 }}; // Right edge
			}} else {{
				return {{ x: x, y: y + height / 2 }}; // Left edge
			}}
		}}

		function updateRelationships() {{
			const lines = document.querySelectorAll('.relationship-line');

			lines.forEach(line => {{
				const from = line.getAttribute('data-from');
				const to = line.getAttribute('data-to');
				const propertyName = line.getAttribute('data-property');

				const fromInfo = getTableConnectionPoint(from, propertyName, 'out');
				const toInfo = getTableConnectionPoint(to, 'Id', 'in');

				if (fromInfo && toInfo) {{
					const midX = (fromInfo.x + toInfo.x) / 2;
					const pathData = 'M ' + fromInfo.x + ' ' + fromInfo.y + ' L ' + midX + ' ' + fromInfo.y + ' L ' + midX + ' ' + toInfo.y + ' L ' + toInfo.x + ' ' + toInfo.y;
					line.setAttribute('d', pathData);
				}}

				line.classList.remove('highlighted', 'dimmed', 'hidden');

				if (!showRelationships) {{
					line.style.display = 'none';
				}} else {{
					line.style.display = 'block';
					if (showOnlySelectedRelations && selectedTable) {{
						if (from === selectedTable || to === selectedTable) {{
							line.classList.add('highlighted');
							line.setAttribute('marker-end', 'url(#arrowhead-highlighted)');
						}} else {{
							line.classList.add('dimmed');
						}}
					}} else {{
						line.setAttribute('marker-end', 'url(#arrowhead)');
					}}
				}}
			}});
		}}

		function toggleRelationships() {{
			showRelationships = !showRelationships;
			updateRelationships();
			updateButtonStates();
			saveSettings();
		}}

		function toggleNavigationProperties() {{
			showNavigationProperties = !showNavigationProperties;
			generateSchema();
			saveSettings();
		}}

		function toggleInheritedProperties() {{
			showInheritedProperties = !showInheritedProperties;
			generateSchema();
			saveSettings();
		}}

		function toggleFullHeight() {{
			fullHeightMode = !fullHeightMode;
			generateSchema();
			saveSettings();
		}}

		function toggleSnapToGrid() {{
			snapToGrid = !snapToGrid;
			updateButtonStates();
			saveSettings();
		}}

		function downloadSchema() {{
			// Get current state
			const currentHtml = document.documentElement.outerHTML;

			// Create a blob and download link
			const blob = new Blob([currentHtml], {{ type: 'text/html' }});
			const url = URL.createObjectURL(blob);

			// Create download link
			const link = document.createElement('a');
			link.href = url;
			link.download = 'schema-visualization-' + new Date().toISOString().slice(0, 19).replace(/:/g, '-') + '.html';

			// Trigger download
			document.body.appendChild(link);
			link.click();
			document.body.removeChild(link);

			// Clean up
			URL.revokeObjectURL(url);
		}}
	</script>
</body>
</html>";
	}

	private static string GenerateJavaScript(string entitiesJson)
	{
		return $@"		const entities = {entitiesJson};

		let selectedTable = null;
		let showOnlySelectedRelations = false;
		let showRelationships = true;
		let showNavigationProperties = false;
		let showInheritedProperties = true;
		let fullHeightMode = false;
		let snapToGrid = false;
		let currentZoom = 1;
		let isPanning = false;
		let panStart = {{ x: 0, y: 0 }};
		let svgViewBox = {{ x: 0, y: 0, width: 8000, height: 6000 }};
		const CANVAS_WIDTH = 8000;
		const CANVAS_HEIGHT = 6000;
		const BOUNDARY_MARGIN = 100;
		const MAX_ZOOM = 15; // Increased from 3 to 15 for 5x more zoom

		// Local storage keys
		const STORAGE_KEYS = {{
			tablePositions: 'schemaMagic_tablePositions',
			viewSettings: 'schemaMagic_viewSettings'
		}};

		document.addEventListener('DOMContentLoaded', function() {{
			loadSettings();
			generateSchema();
			setupEventListeners();
		}});

		function setupEventListeners() {{
			const container = document.getElementById('schema-container');
			const svg = document.getElementById('schema-svg');
			const backgroundArea = document.getElementById('background-pan-area');

			backgroundArea.addEventListener('mousedown', startBackgroundPan);
			container.addEventListener('mousemove', handlePan);
			container.addEventListener('mouseup', endPan);
			container.addEventListener('mouseleave', endPan);

			svg.addEventListener('wheel', handleWheel);
			svg.addEventListener('contextmenu', e => e.preventDefault());
		}}

		function startBackgroundPan(e) {{
			if (e.target.closest('.table-group')) return;

			isPanning = true;
			panStart = {{
				x: e.clientX,
				y: e.clientY
			}};
			document.getElementById('schema-container').style.cursor = 'grabbing';
			e.preventDefault();
		}}

		function handlePan(e) {{
			if (!isPanning) return;

			const deltaX = (e.clientX - panStart.x) / currentZoom;
			const deltaY = (e.clientY - panStart.y) / currentZoom;

			svgViewBox.x -= deltaX;
			svgViewBox.y -= deltaY;

			panStart.x = e.clientX;
			panStart.y = e.clientY;

			updateViewBox();
			e.preventDefault();
		}}

		function endPan(e) {{
			if (!isPanning) return;
			isPanning = false;
			document.getElementById('schema-container').style.cursor = 'grab';
		}}

		function handleWheel(e) {{
			e.preventDefault();
			const rect = e.currentTarget.getBoundingClientRect();
			const mouseX = e.clientX - rect.left;
			const mouseY = e.clientY - rect.top;

			const svgX = (mouseX / rect.width) * svgViewBox.width + svgViewBox.x;
			const svgY = (mouseY / rect.height) * svgViewBox.height + svgViewBox.y;

			const delta = e.deltaY > 0 ? 0.9 : 1.1;
			const newZoom = Math.max(0.1, Math.min(MAX_ZOOM, currentZoom * delta));

			const zoomFactor = newZoom / currentZoom;
			const newWidth = svgViewBox.width / zoomFactor;
			const newHeight = svgViewBox.height / zoomFactor;

			svgViewBox.x = svgX - (svgX - svgViewBox.x) / zoomFactor;
			svgViewBox.y = svgY - (svgY - svgViewBox.y) / zoomFactor;
			svgViewBox.width = newWidth;
			svgViewBox.height = newHeight;

			currentZoom = newZoom;
			updateViewBox();
		}}

		function updateViewBox() {{
			const svg = document.getElementById('schema-svg');
			svg.setAttribute('viewBox', `${{svgViewBox.x}} ${{svgViewBox.y}} ${{svgViewBox.width}} ${{svgViewBox.height}}`);
		}}

		function zoomIn() {{
			const centerX = svgViewBox.x + svgViewBox.width / 2;
			const centerY = svgViewBox.y + svgViewBox.height / 2;

			currentZoom = Math.min(MAX_ZOOM, currentZoom * 1.2);
			svgViewBox.width = CANVAS_WIDTH / currentZoom;
			svgViewBox.height = CANVAS_HEIGHT / currentZoom;
			svgViewBox.x = centerX - svgViewBox.width / 2;
			svgViewBox.y = centerY - svgViewBox.height / 2;

			updateViewBox();
		}}

		function zoomOut() {{
			const centerX = svgViewBox.x + svgViewBox.width / 2;
			const centerY = svgViewBox.y + svgViewBox.height / 2;

			currentZoom = Math.max(0.1, currentZoom * 0.8);
			svgViewBox.width = CANVAS_WIDTH / currentZoom;
			svgViewBox.height = CANVAS_HEIGHT / currentZoom;
			svgViewBox.x = centerX - svgViewBox.width / 2;
			svgViewBox.y = centerY - svgViewBox.height / 2;

			updateViewBox();
		}}

		function resetZoom() {{
			currentZoom = 1;
			svgViewBox = {{ x: 0, y: 0, width: CANVAS_WIDTH, height: CANVAS_HEIGHT }};
			updateViewBox();
		}}

		function saveSettings() {{
			// Save table positions
			const positions = {{}};
			document.querySelectorAll('.table-group').forEach(table => {{
				const entityName = table.getAttribute('data-entity');
				const rect = table.querySelector('.table-box');
				positions[entityName] = {{
					x: parseFloat(rect.getAttribute('x')),
					y: parseFloat(rect.getAttribute('y'))
				}};
			}});
			localStorage.setItem(STORAGE_KEYS.tablePositions, JSON.stringify(positions));

			// Save view settings
			const settings = {{
				showRelationships,
				showNavigationProperties,
				showInheritedProperties,
				fullHeightMode,
				snapToGrid
			}};
			localStorage.setItem(STORAGE_KEYS.viewSettings, JSON.stringify(settings));
		}}

		function loadSettings() {{
			// Load view settings
			try {{
				const savedSettings = localStorage.getItem(STORAGE_KEYS.viewSettings);
				if (savedSettings) {{
					const settings = JSON.parse(savedSettings);
					showRelationships = settings.showRelationships ?? true;
					showNavigationProperties = settings.showNavigationProperties ?? false;
					showInheritedProperties = settings.showInheritedProperties ?? true;
					fullHeightMode = settings.fullHeightMode ?? false;
					snapToGrid = settings.snapToGrid ?? false;
				}}
			}} catch (e) {{
				console.warn('Failed to load settings:', e);
			}}
		}}

		function updateButtonStates() {{
			// Update button active states based on current settings
			const relationshipsBtn = document.getElementById('relationships-btn');
			relationshipsBtn.classList.toggle('active', !showRelationships);
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
		}}

		function generateSchema() {{
			const svg = document.getElementById('schema-svg');

			const existingContent = svg.querySelectorAll('.table-group, .relationship-line');
			existingContent.forEach(el => el.remove());

			const tablePositions = calculateTablePositions();

			Object.keys(entities).forEach((entityName) => {{
				const entity = entities[entityName];
				const position = tablePositions[entityName];
				generateTable(svg, entity, position.x, position.y);
			}});

			if (showRelationships) {{
				generateRelationships(svg);
			}}

			updateButtonStates();
		}}

		function calculateTablePositions() {{
			// Try to load saved positions first
			let savedPositions = null;
			try {{
				const saved = localStorage.getItem(STORAGE_KEYS.tablePositions);
				if (saved) {{
					savedPositions = JSON.parse(saved);
				}}
			}} catch (e) {{
				console.warn('Failed to load saved positions:', e);
			}}

			const positions = {{}};
			const entityNames = Object.keys(entities);
			
			if (savedPositions) {{
				// Use saved positions if available
				entityNames.forEach(entityName => {{
					if (savedPositions[entityName]) {{
						positions[entityName] = savedPositions[entityName];
					}} else {{
						// Default position for new entities
						positions[entityName] = {{ x: 200, y: 200 }};
					}}
				}});
			}} else {{
				// Calculate centered grid layout
				const cols = Math.ceil(Math.sqrt(entityNames.length));
				const baseSpacing = 500;
				const totalWidth = cols * baseSpacing;
				const totalHeight = Math.ceil(entityNames.length / cols) * baseSpacing;
				
				// Center the grid in the canvas
				const startX = (CANVAS_WIDTH - totalWidth) / 2;
				const startY = (CANVAS_HEIGHT - totalHeight) / 2;

				entityNames.forEach((entityName, index) => {{
					const row = Math.floor(index / cols);
					const col = index % cols;

					positions[entityName] = {{
						x: startX + col * baseSpacing + (row % 2) * 250,
						y: startY + row * baseSpacing
					}};
				}});
			}}

			return positions;
		}}

		function getPropertyIcon(property) {{
			if (property.isInherited) return 'INH';
			if (property.isKey) return 'PK';
			if (property.isForeignKey) return 'FK';
			if (isNavigationProperty(property)) return 'NAV';
			return '';
		}}

		function sortProperties(properties) {{
			return properties.sort((a, b) => {{
				// Primary sort: inherited properties first
				if (a.isInherited && !b.isInherited) return -1;
				if (!a.isInherited && b.isInherited) return 1;

				// Secondary sort: alphabetical within each group
				return a.name.localeCompare(b.name);
			}});
		}}

		function generateTable(svg, entity, x, y) {{
			const properties = entity.properties || [];
			const inheritedProps = showInheritedProperties ? (entity.inheritedProperties || []) : [];
			
			// Combine and mark inherited properties
			const allProperties = [
				...inheritedProps.map(p => ({{ ...p, isInherited: true }})),
				...properties.map(p => ({{ ...p, isInherited: false }}))
			];

			// Sort properties: inherited first (alphabetically), then regular (alphabetically)
			const sortedProperties = sortProperties(allProperties);

			// Filter visible properties based on navigation toggle
			const visibleProperties = showNavigationProperties ?
				sortedProperties :
				sortedProperties.filter(p => !isNavigationProperty(p));

			// Group properties with their related navigation properties
			const groupedProperties = groupPropertiesWithNavigation(visibleProperties, sortedProperties);

			const rowHeight = 22;
			const headerHeight = 35;
			const inheritanceHeight = (entity.baseType && showInheritedProperties) ? 20 : 0;
			const padding = 12;
			const minWidth = 350;

			// Calculate dimensions based on ALL visible content (including navigation properties if shown)
			const contentForSizing = showNavigationProperties ? sortedProperties : visibleProperties;
			
			const maxNameLength = Math.max(
				entity.type.length,
				...contentForSizing.map(p => p.name.length + (getPropertyIcon(p).length > 0 ? 5 : 0))
			);
			
			const maxTypeLength = Math.max(
				0,
				...contentForSizing.map(p => p.type.length)
			);

			// Calculate proper widths with consistent spacing
			const nameWidth = Math.max(120, maxNameLength * 8);
			const typeWidth = Math.max(80, maxTypeLength * 7);
			const iconWidth = 50; // Space for icons like PK, FK, NAV, INH
			const tableWidth = Math.max(minWidth, nameWidth + typeWidth + iconWidth + padding * 4);

			// For full height mode, consider navigation properties toggle
			const maxVisibleRows = fullHeightMode ? groupedProperties.length : Math.min(groupedProperties.length, 15);
			const calculatedHeight = headerHeight + inheritanceHeight + (maxVisibleRows * rowHeight) + padding;
			const tableHeight = calculatedHeight;

			const hasInheritedProperties = inheritedProps.length > 0 && showInheritedProperties;

			const tableGroup = document.createElementNS('http://www.w3.org/2000/svg', 'g');
			tableGroup.classList.add('table-group');
			tableGroup.setAttribute('data-entity', entity.type);

			const tableRect = document.createElementNS('http://www.w3.org/2000/svg', 'rect');
			tableRect.setAttribute('x', x);
			tableRect.setAttribute('y', y);
			tableRect.setAttribute('width', tableWidth);
			tableRect.setAttribute('height', tableHeight);
			tableRect.classList.add('table-box');
			if (hasInheritedProperties) tableRect.classList.add('inherited');
			tableGroup.appendChild(tableRect);

			const headerRect = document.createElementNS('http://www.w3.org/2000/svg', 'rect');
			headerRect.setAttribute('x', x);
			headerRect.setAttribute('y', y);
			headerRect.setAttribute('width', tableWidth);
			headerRect.setAttribute('height', headerHeight);
			headerRect.classList.add('table-header');
			if (hasInheritedProperties) headerRect.classList.add('inherited');
			tableGroup.appendChild(headerRect);

			const titleText = document.createElementNS('http://www.w3.org/2000/svg', 'text');
			titleText.setAttribute('x', x + tableWidth / 2);
			titleText.setAttribute('y', y + headerHeight / 2 + 6);
			titleText.setAttribute('text-anchor', 'middle');
			titleText.classList.add('table-title');
			titleText.textContent = entity.type;
			tableGroup.appendChild(titleText);

			// Add inheritance indicator (only if showing inherited properties)
			if (entity.baseType && showInheritedProperties) {{
				const inheritanceText = document.createElementNS('http://www.w3.org/2000/svg', 'text');
				inheritanceText.setAttribute('x', x + tableWidth / 2);
				inheritanceText.setAttribute('y', y + headerHeight + 14);
				inheritanceText.setAttribute('text-anchor', 'middle');
				inheritanceText.classList.add('inheritance-text');
				inheritanceText.textContent = 'inherits from ' + entity.baseType;
				tableGroup.appendChild(inheritanceText);
			}}

			// Render grouped properties
			const propertiesToShow = groupedProperties.slice(0, maxVisibleRows);
			let currentY = y + headerHeight + inheritanceHeight;
			let lastPropertyWasInherited = false;
			
			propertiesToShow.forEach((item, index) => {{
				if (item.type === 'group') {{
					// Check if we need a section divider
					const firstProp = item.properties[0];
					if (lastPropertyWasInherited && !firstProp.isInherited) {{
						// Add section divider line
						const divider = document.createElementNS('http://www.w3.org/2000/svg', 'line');
						divider.setAttribute('x1', x + 4);
						divider.setAttribute('y1', currentY - 4);
						divider.setAttribute('x2', x + tableWidth - 4);
						divider.setAttribute('y2', currentY - 4);
						divider.classList.add('section-divider');
						tableGroup.appendChild(divider);
					}}

					// Draw group background
					const groupRect = document.createElementNS('http://www.w3.org/2000/svg', 'rect');
					groupRect.setAttribute('x', x + 4);
					groupRect.setAttribute('y', currentY - 8);
					groupRect.setAttribute('width', tableWidth - 8);
					groupRect.setAttribute('height', item.properties.length * rowHeight + 8);
					groupRect.classList.add('property-group');
					if (firstProp.isInherited) {{
						groupRect.classList.add('inherited-section');
					}}
					tableGroup.appendChild(groupRect);

					// Render each property in the group
					item.properties.forEach((property, propIndex) => {{
						const propY = currentY + (propIndex * rowHeight) + 16;
						renderProperty(tableGroup, property, x, propY, tableWidth, padding, entity.type);
					}});
					
					currentY += item.properties.length * rowHeight;
					lastPropertyWasInherited = item.properties[item.properties.length - 1].isInherited;
				}} else {{
					// Check if we need a section divider
					if (lastPropertyWasInherited && !item.property.isInherited) {{
						// Add section divider line
						const divider = document.createElementNS('http://www.w3.org/2000/svg', 'line');
						divider.setAttribute('x1', x + 4);
						divider.setAttribute('y1', currentY - 4);
						divider.setAttribute('x2', x + tableWidth - 4);
						divider.setAttribute('y2', currentY - 4);
						divider.classList.add('section-divider');
						tableGroup.appendChild(divider);
					}}

					const propY = currentY + 16;
					renderProperty(tableGroup, item.property, x, propY, tableWidth, padding, entity.type);
					currentY += rowHeight;
					lastPropertyWasInherited = item.property.isInherited;
				}}
			}});

			// Add ellipsis if there are more properties
			if (!fullHeightMode && groupedProperties.length > maxVisibleRows) {{
				const ellipsisY = currentY + 6;
				const ellipsisText = document.createElementNS('http://www.w3.org/2000/svg', 'text');
				ellipsisText.setAttribute('x', x + tableWidth / 2);
				ellipsisText.setAttribute('y', ellipsisY);
				ellipsisText.setAttribute('text-anchor', 'middle');
				ellipsisText.classList.add('property-text');
				ellipsisText.textContent = '... (+' + (groupedProperties.length - maxVisibleRows) + ' more)';
				ellipsisText.style.fontSize = '11px';
				ellipsisText.style.fill = '#9ca3af';
				tableGroup.appendChild(ellipsisText);
			}}

			setupTableInteraction(tableGroup, entity.type);
			svg.appendChild(tableGroup);
		}}

		function groupPropertiesWithNavigation(visibleProperties, allProperties) {{
			const grouped = [];
			const processed = new Set();

			visibleProperties.forEach(prop => {{
				if (processed.has(prop.name)) return;

				if (prop.isForeignKey && showNavigationProperties) {{
					// Find corresponding navigation property only when showing nav properties
					const navPropName = prop.name.replace('Id', '');
					const navProp = allProperties.find(p => p.name === navPropName && isNavigationProperty(p));

					if (navProp) {{
						grouped.push({{
							type: 'group',
							properties: [prop, navProp]
						}});
						processed.add(prop.name);
						processed.add(navProp.name);
					}} else {{
						grouped.push({{
							type: 'single',
							property: prop
						}});
						processed.add(prop.name);
					}}
				}} else {{
					grouped.push({{
						type: 'single',
						property: prop
					}});
					processed.add(prop.name);
				}}
			}});

			return grouped;
		}}

		function renderProperty(tableGroup, property, x, propY, tableWidth, padding, entityName) {{
			const propText = document.createElementNS('http://www.w3.org/2000/svg', 'text');
			propText.setAttribute('x', x + padding);
			propText.setAttribute('y', propY);
			propText.classList.add('property-text');

			if (property.isInherited) {{
				propText.classList.add('property-inherited');
			}} else if (property.isKey) {{
				propText.classList.add('property-key');
			}} else if (property.isForeignKey) {{
				propText.classList.add('property-foreign-key');
			}} else if (isNavigationProperty(property)) {{
				propText.classList.add('property-navigation');
				propText.style.cursor = 'pointer';
				propText.addEventListener('click', () => navigateToEntity(property.type));
			}}

			propText.textContent = property.name;
			tableGroup.appendChild(propText);

			// Add icon with consistent positioning
			const icon = getPropertyIcon(property);
			if (icon) {{
				const iconText = document.createElementNS('http://www.w3.org/2000/svg', 'text');
				iconText.setAttribute('x', x + tableWidth - padding - 80); // More consistent icon positioning
				iconText.setAttribute('y', propY);
				iconText.classList.add('property-icon');
				iconText.textContent = icon;
				tableGroup.appendChild(iconText);
			}}

			// Property type (right-aligned with consistent spacing)
			const typeText = document.createElementNS('http://www.w3.org/2000/svg', 'text');
			typeText.setAttribute('x', x + tableWidth - padding);
			typeText.setAttribute('y', propY);
			typeText.classList.add('property-text', 'property-type');
			typeText.textContent = property.type;
			tableGroup.appendChild(typeText);
		}}

		function navigateToEntity(typeName) {{
			// Clean up type name (remove generic brackets, nullability, etc.)
			const cleanTypeName = typeName.replace(/\\?$/, '').replace(/ICollection<(.+)>/, '$1').replace(/List<(.+)>/, '$1');
			
			const targetTable = document.querySelector('[data-entity=""' + cleanTypeName + '""]');
			if (targetTable) {{
				selectTable(cleanTypeName);

				// Center the target table in view
				const rect = targetTable.querySelector('.table-box');
				const targetX = parseFloat(rect.getAttribute('x'));
				const targetY = parseFloat(rect.getAttribute('y'));
				const targetWidth = parseFloat(rect.getAttribute('width'));
				const targetHeight = parseFloat(rect.getAttribute('height'));

				svgViewBox.x = targetX - svgViewBox.width / 2 + targetWidth / 2;
				svgViewBox.y = targetY - svgViewBox.height / 2 + targetHeight / 2;
				updateViewBox();
			}}
		}}

		function isNavigationProperty(property) {{
			const navPatterns = [
				/^ICollection</,
				/^List</,
				/^IList</,
				/^HashSet</,
				/^ISet</,
				/Model$/,
				/Entity$/
			];

			return navPatterns.some(pattern => pattern.test(property.type));
		}}

		function setupTableInteraction(tableGroup, entityName) {{
			let dragData = null;

			tableGroup.addEventListener('mousedown', function(e) {{
				e.stopPropagation();
				selectTable(entityName);

				const rect = tableGroup.querySelector('.table-box');

				const svg = document.getElementById('schema-svg');
				const svgRect = svg.getBoundingClientRect();
				const svgX = (e.clientX - svgRect.left) * svgViewBox.width / svgRect.width + svgViewBox.x;
				const svgY = (e.clientY - svgRect.top) * svgViewBox.height / svgRect.height + svgViewBox.y;

				dragData = {{
					startX: svgX,
					startY: svgY,
					elementX: parseFloat(rect.getAttribute('x')),
					elementY: parseFloat(rect.getAttribute('y'))
				}};

				tableGroup.classList.add('dragging');

				document.addEventListener('mousemove', handleTableDrag);
				document.addEventListener('mouseup', handleTableDragEnd);
			}});

			function handleTableDrag(e) {{
				if (!dragData) return;

				const svg = document.getElementById('schema-svg');
				const svgRect = svg.getBoundingClientRect();
				const svgX = (e.clientX - svgRect.left) * svgViewBox.width / svgRect.width + svgViewBox.x;
				const svgY = (e.clientY - svgRect.top) * svgViewBox.height / svgRect.height + svgViewBox.y;

				const deltaX = svgX - dragData.startX;
				const deltaY = svgY - dragData.startY;

				let newX = dragData.elementX + deltaX;
				let newY = dragData.elementY + deltaY;

				// Check boundaries and show indicator
				const rect = tableGroup.querySelector('.table-box');
				const tableWidth = parseFloat(rect.getAttribute('width'));
				const tableHeight = parseFloat(rect.getAttribute('height'));

				const boundaryRect = document.getElementById('boundary-rect');
				if (newX < BOUNDARY_MARGIN || newY < BOUNDARY_MARGIN ||
					newX + tableWidth > CANVAS_WIDTH - BOUNDARY_MARGIN ||
					newY + tableHeight > CANVAS_HEIGHT - BOUNDARY_MARGIN) {{
					boundaryRect.style.display = 'block';
				}} else {{
					boundaryRect.style.display = 'none';
				}}

				if (snapToGrid) {{
					newX = Math.round(newX / 20) * 20;
					newY = Math.round(newY / 20) * 20;
				}}

				moveTable(tableGroup, newX, newY);
				updateRelationships();
			}}

			function handleTableDragEnd() {{
				if (dragData) {{
					tableGroup.classList.remove('dragging');
					document.getElementById('boundary-rect').style.display = 'none';
					// Save positions after drag
					saveSettings();
				}}
				dragData = null;
				document.removeEventListener('mousemove', handleTableDrag);
				document.removeEventListener('mouseup', handleTableDragEnd);
			}}
		}}

		function moveTable(tableGroup, x, y) {{
			const elements = tableGroup.querySelectorAll('rect, text, line');
			const oldRect = tableGroup.querySelector('.table-box');
			const oldX = parseFloat(oldRect.getAttribute('x'));
			const oldY = parseFloat(oldRect.getAttribute('y'));
			const deltaX = x - oldX;
			const deltaY = y - oldY;

			elements.forEach(element => {{
				if (element.tagName === 'line') {{
					// Handle line elements differently
					const x1 = parseFloat(element.getAttribute('x1'));
					const y1 = parseFloat(element.getAttribute('y1'));
					const x2 = parseFloat(element.getAttribute('x2'));
					const y2 = parseFloat(element.getAttribute('y2'));
					element.setAttribute('x1', x1 + deltaX);
					element.setAttribute('y1', y1 + deltaY);
					element.setAttribute('x2', x2 + deltaX);
					element.setAttribute('y2', y2 + deltaY);
				}} else {{
					// Handle rect and text elements
					const currentX = parseFloat(element.getAttribute('x'));
					const currentY = parseFloat(element.getAttribute('y'));
					element.setAttribute('x', currentX + deltaX);
					element.setAttribute('y', currentY + deltaY);
				}}
			}});
		}}

		function selectTable(entityName) {{
			document.querySelectorAll('.table-box.selected').forEach(box => {{
				box.classList.remove('selected');
			}});

			const tableGroup = document.querySelector('[data-entity=""' + entityName + '""]');
			if (tableGroup) {{
				tableGroup.querySelector('.table-box').classList.add('selected');
				selectedTable = entityName;

				if (showOnlySelectedRelations) {{
					updateRelationships();
				}}
			}}
		}}

		function generateRelationships(svg) {{
			if (!showRelationships) return;

			Object.values(entities).forEach(entity => {{
				const fromEntityName = entity.type;
				entity.properties.forEach(prop => {{
					if (prop.isForeignKey) {{
						const targetEntityName = prop.name.replace('Id', '');
						if (entities[targetEntityName]) {{
							createRelationshipLine(svg, fromEntityName, targetEntityName, prop.name);
						}}
					}}
				}});
			}});
		}}

		function createRelationshipLine(svg, fromEntityName, toEntityName, propertyName) {{
			const fromInfo = getTableConnectionPoint(fromEntityName, propertyName, 'out');
			const toInfo = getTableConnectionPoint(toEntityName, 'Id', 'in');

			if (!fromInfo || !toInfo) return;

			const line = document.createElementNS('http://www.w3.org/2000/svg', 'path');

			// Create angled connector
			const midX = (fromInfo.x + toInfo.x) / 2;
			const pathData = 'M ' + fromInfo.x + ' ' + fromInfo.y + ' L ' + midX + ' ' + fromInfo.y + ' L ' + midX + ' ' + toInfo.y + ' L ' + toInfo.x + ' ' + toInfo.y;

			line.setAttribute('d', pathData);
			line.classList.add('relationship-line');
			line.setAttribute('data-from', fromEntityName);
			line.setAttribute('data-to', toEntityName);
			line.setAttribute('data-property', propertyName);

			svg.appendChild(line);
		}}

		function getTableConnectionPoint(entityName, propertyName, direction) {{
			const tableGroup = document.querySelector('[data-entity=""' + entityName + '""]');
			if (!tableGroup) return null;

			const rect = tableGroup.querySelector('.table-box');
			const x = parseFloat(rect.getAttribute('x'));
			const y = parseFloat(rect.getAttribute('y'));
			const width = parseFloat(rect.getAttribute('width'));
			const height = parseFloat(rect.getAttribute('height'));

			// For now, use center points of edges
			if (direction === 'out') {{
				return {{ x: x + width, y: y + height / 2 }}; // Right edge
			}} else {{
				return {{ x: x, y: y + height / 2 }}; // Left edge
			}}
		}}

		function updateRelationships() {{
			const lines = document.querySelectorAll('.relationship-line');

			lines.forEach(line => {{
				const from = line.getAttribute('data-from');
				const to = line.getAttribute('data-to');
				const propertyName = line.getAttribute('data-property');

				const fromInfo = getTableConnectionPoint(from, propertyName, 'out');
				const toInfo = getTableConnectionPoint(to, 'Id', 'in');

				if (fromInfo && toInfo) {{
					const midX = (fromInfo.x + toInfo.x) / 2;
					const pathData = 'M ' + fromInfo.x + ' ' + fromInfo.y + ' L ' + midX + ' ' + fromInfo.y + ' L ' + midX + ' ' + toInfo.y + ' L ' + toInfo.x + ' ' + toInfo.y;
					line.setAttribute('d', pathData);
				}}

				line.classList.remove('highlighted', 'dimmed', 'hidden');

				if (!showRelationships) {{
					line.style.display = 'none';
				}} else {{
					line.style.display = 'block';
					if (showOnlySelectedRelations && selectedTable) {{
						if (from === selectedTable || to === selectedTable) {{
							line.classList.add('highlighted');
							line.setAttribute('marker-end', 'url(#arrowhead-highlighted)');
						}} else {{
							line.classList.add('dimmed');
						}}
					}} else {{
						line.setAttribute('marker-end', 'url(#arrowhead)');
					}}
				}}
			}});
		}}

		function toggleRelationships() {{
			showRelationships = !showRelationships;
			updateRelationships();
			updateButtonStates();
			saveSettings();
		}}

		function toggleNavigationProperties() {{
			showNavigationProperties = !showNavigationProperties;
			generateSchema();
			saveSettings();
		}}

		function toggleInheritedProperties() {{
			showInheritedProperties = !showInheritedProperties;
			generateSchema();
			saveSettings();
		}}

		function toggleFullHeight() {{
			fullHeightMode = !fullHeightMode;
			generateSchema();
			saveSettings();
		}}

		function toggleSnapToGrid() {{
			snapToGrid = !snapToGrid;
			updateButtonStates();
			saveSettings();
		}}

		function downloadSchema() {{
			// Get current state
			const currentHtml = document.documentElement.outerHTML;

			// Create a blob and download link
			const blob = new Blob([currentHtml], {{ type: 'text/html' }});
			const url = URL.createObjectURL(blob);

			// Create download link
			const link = document.createElement('a');
			link.href = url;
			link.download = 'schema-visualization-' + new Date().toISOString().slice(0, 19).replace(/:/g, '-') + '.html';

			// Trigger download
			document.body.appendChild(link);
			link.click();
			document.body.removeChild(link);

			// Clean up
			URL.revokeObjectURL(url);
		}}
	";
	}
}