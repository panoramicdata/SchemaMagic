// Table generation and rendering
function generateTable(svg, entity, x, y) {
	const properties = entity.properties || [];
	const inheritedProps = showInheritedProperties ? (entity.inheritedProperties || []) : [];

	// Combine and mark inherited properties
	const allProperties = [
		...inheritedProps.map(p => ({ ...p, isInherited: true })),
		...properties.map(p => ({ ...p, isInherited: false }))
	];

	// Sort properties: inherited first (alphabetically), then regular (alphabetically)
	const sortedProperties = sortProperties(allProperties);

	// Filter visible properties based on navigation toggle
	const visibleProperties = showNavigationProperties ?
		sortedProperties :
		sortedProperties.filter(p => !isNavigationProperty(p));

	// Group properties with their related navigation properties
	const groupedProperties = groupPropertiesWithNavigation(visibleProperties, sortedProperties);

	const rowHeight = 55; // Increased from 22 to 55 (2.5x larger)
	const headerHeight = 88; // Increased from 35 to 88 (2.5x larger)
	const inheritanceHeight = (entity.baseType && showInheritedProperties) ? 50 : 0; // Increased from 20 to 50 (2.5x larger)
	const padding = 30; // Increased from 12 to 30 (2.5x larger)
	const minWidth = 1000; // Increased from 400 to 1000 (2.5x larger)
	const iconWidth = 60; // Increased from 24 to 60 (2.5x larger)

	// Calculate actual displayed rows for proper height calculation
	let totalDisplayRows = 0;
	groupedProperties.forEach(item => {
		if (item.type === 'group') {
			totalDisplayRows += item.properties.length;
		} else {
			totalDisplayRows += 1;
		}
	});

	// Calculate width based on ACTUAL content that will be displayed
	const allDisplayedProperties = [];
	groupedProperties.forEach(item => {
		if (item.type === 'group') {
			allDisplayedProperties.push(...item.properties);
		} else {
			allDisplayedProperties.push(item.property);
		}
	});

	// Calculate the maximum lengths for proper width allocation
	const maxNameLength = Math.max(
		entity.type.length,
		...allDisplayedProperties.map(p => p.name.length)
	);

	const maxTypeLength = Math.max(
		8, // minimum for "string"
		...allDisplayedProperties.map(p => {
			// Clean up type names for accurate length calculation
			let typeText = p.type;
			if (isNavigationProperty(p)) {
				// For navigation properties, show clean type names
				typeText = typeText.replace(/^ICollection<(.+)>$/, '$1').replace(/^List<(.+)>$/, '$1');
			}
			return typeText.length;
		})
	);

	// Calculate table width with optimized spacing and space for type icons
	const nameColumnWidth = Math.max(200, maxNameLength * 18);    // Increased from 80 and 7 to 200 and 18 (2.5x larger)
	const typeColumnWidth = Math.max(150, maxTypeLength * 15);     // Increased from 60 and 6 to 150 and 15 (2.5x larger)
	const typeIconSpace = 50; // Increased from 20 to 50 (2.5x larger)
	const spacingBuffer = 100;                                    // Increased from 40 to 100 (2.5x larger)

	const tableWidth = Math.max(
		minWidth,
		iconWidth + nameColumnWidth + typeColumnWidth + typeIconSpace + spacingBuffer + (padding * 2)
	);

	// For full height mode, use actual total rows, otherwise limit to 15
	const maxVisibleRows = fullHeightMode ? totalDisplayRows : Math.min(totalDisplayRows, 15);
	const calculatedHeight = headerHeight + inheritanceHeight + (maxVisibleRows * rowHeight) + padding;
	const tableHeight = calculatedHeight;

	const tableGroup = document.createElementNS('http://www.w3.org/2000/svg', 'g');
	tableGroup.classList.add('table-group');
	tableGroup.setAttribute('data-entity', entity.type);

	// FIRST: Draw the main table box (background) - all tables use same style now
	const tableRect = document.createElementNS('http://www.w3.org/2000/svg', 'rect');
	tableRect.setAttribute('x', x);
	tableRect.setAttribute('y', y);
	tableRect.setAttribute('width', tableWidth);
	tableRect.setAttribute('height', tableHeight);
	tableRect.classList.add('table-box');
	// Removed inherited class logic - all tables use same style
	tableGroup.appendChild(tableRect);

	// SECOND: Render property group backgrounds and section dividers
	const propertiesToShow = getPropertiesToShow(groupedProperties, maxVisibleRows);
	let currentY = y + headerHeight + inheritanceHeight;
	let lastPropertyWasInherited = false;

	propertiesToShow.forEach((item, index) => {
		if (item.type === 'group') {
			// Check if we need a section divider
			const firstProp = item.properties[0];
			if (lastPropertyWasInherited && !firstProp.isInherited) {
				// Add section divider line
				const divider = document.createElementNS('http://www.w3.org/2000/svg', 'line');
				divider.setAttribute('x1', x + 10); // Increased from 4 to 10 (2.5x larger)
				divider.setAttribute('y1', currentY - 10); // Increased from 4 to 10 (2.5x larger)
				divider.setAttribute('x2', x + tableWidth - 10); // Increased from 4 to 10 (2.5x larger)
				divider.setAttribute('y2', currentY - 10); // Increased from 4 to 10 (2.5x larger)
				divider.classList.add('section-divider');
				tableGroup.appendChild(divider);
			}

			// Draw group background with proper positioning
			const groupRect = document.createElementNS('http://www.w3.org/2000/svg', 'rect');
			groupRect.setAttribute('x', x + 5); // Increased from 2 to 5 (2.5x larger)
			groupRect.setAttribute('y', currentY - 10); // Increased from 4 to 10 (2.5x larger)
			groupRect.setAttribute('width', tableWidth - 10); // Increased from 4 to 10 (2.5x larger)
			groupRect.setAttribute('height', item.properties.length * rowHeight + 10); // Increased from 4 to 10 (2.5x larger)
			groupRect.classList.add('property-group');
			if (firstProp.isInherited) {
				groupRect.classList.add('inherited-section');
			}
			tableGroup.appendChild(groupRect);

			// Add connecting line for FK-NAV groups
			if (item.properties.length === 2 &&
				item.properties[0].isForeignKey &&
				isNavigationProperty(item.properties[1])) {
				const connectLine = document.createElementNS('http://www.w3.org/2000/svg', 'line');
				connectLine.setAttribute('x1', x + 25); // Increased from 10 to 25 (2.5x larger)
				connectLine.setAttribute('y1', currentY + 10); // Increased from 4 to 10 (2.5x larger)
				connectLine.setAttribute('x2', x + 25); // Increased from 10 to 25 (2.5x larger)
				connectLine.setAttribute('y2', currentY + rowHeight + 10); // Increased from 4 to 10 (2.5x larger)
				connectLine.classList.add('fk-nav-connector');
				tableGroup.appendChild(connectLine);
			}

			currentY += item.properties.length * rowHeight;
			lastPropertyWasInherited = item.properties[item.properties.length - 1].isInherited;
		} else {
			// Check if we need a section divider
			if (lastPropertyWasInherited && !item.property.isInherited) {
				// Add section divider line
				const divider = document.createElementNS('http://www.w3.org/2000/svg', 'line');
				divider.setAttribute('x1', x + 10); // Increased from 4 to 10 (2.5x larger)
				divider.setAttribute('y1', currentY - 10); // Increased from 4 to 10 (2.5x larger)
				divider.setAttribute('x2', x + tableWidth - 10); // Increased from 4 to 10 (2.5x larger)
				divider.setAttribute('y2', currentY - 10); // Increased from 4 to 10 (2.5x larger)
				divider.classList.add('section-divider');
				tableGroup.appendChild(divider);
			}

			currentY += rowHeight;
			lastPropertyWasInherited = item.property.isInherited;
		}
	});

	// THIRD: Draw the header and title (these will appear ON TOP of the property groups) - all tables use same style now
	const headerRect = document.createElementNS('http://www.w3.org/2000/svg', 'rect');
	headerRect.setAttribute('x', x);
	headerRect.setAttribute('y', y);
	headerRect.setAttribute('width', tableWidth);
	headerRect.setAttribute('height', headerHeight);
	headerRect.classList.add('table-header');
	// Removed inherited class logic - all headers use same style
	tableGroup.appendChild(headerRect);

	// Calculate optimal font size for the title based on table width
	const titleText = document.createElementNS('http://www.w3.org/2000/svg', 'text');
	titleText.setAttribute('x', x + tableWidth / 2);
	titleText.setAttribute('y', y + headerHeight / 2 + 18);
	titleText.setAttribute('text-anchor', 'middle');
	titleText.classList.add('table-title');
	titleText.textContent = entity.type;

	// Dynamic font sizing: start with double the normal size and scale down if needed
	const baseFontSize = 40; // Current base font size
	const maxFontSize = 80; // Double the base for better readability when zoomed out
	const minFontSize = 32; // Minimum readable size
	const availableWidth = tableWidth - (padding * 2); // Leave some padding on sides

	// Estimate text width: approximately 0.6 * fontSize * characterCount for Segoe UI Bold
	let optimalFontSize = maxFontSize;
	const characterCount = entity.type.length;

	while (optimalFontSize > minFontSize) {
		const estimatedTextWidth = 0.6 * optimalFontSize * characterCount;
		if (estimatedTextWidth <= availableWidth) {
			break;
		}
		optimalFontSize -= 2; // Reduce by 2px increments for fine control
	}

	titleText.style.fontSize = `${optimalFontSize}px`;
	tableGroup.appendChild(titleText);

	console.log(`📝 Title "${entity.type}": ${characterCount} chars, optimal font: ${optimalFontSize}px, available width: ${availableWidth}px`);

	// Add inheritance indicator (only if showing inherited properties)
	if (entity.baseType && showInheritedProperties) {
		const inheritanceText = document.createElementNS('http://www.w3.org/2000/svg', 'text');
		inheritanceText.setAttribute('x', x + tableWidth / 2);
		inheritanceText.setAttribute('y', y + headerHeight + 35); // Increased from 14 to 35 (2.5x larger)
		inheritanceText.setAttribute('text-anchor', 'middle');
		inheritanceText.classList.add('inheritance-text');
		inheritanceText.textContent = 'inherits from ' + entity.baseType;
		tableGroup.appendChild(inheritanceText);
	}

	// FOURTH: Render all property text (these will appear on top)
	currentY = y + headerHeight + inheritanceHeight;
	let rowCount = 0;

	propertiesToShow.forEach((item, index) => {
		if (item.type === 'group') {
			// Render each property in the group
			item.properties.forEach((property, propIndex) => {
				const propY = currentY + (propIndex * rowHeight) + 40; // Increased from 16 to 40 (2.5x larger)
				renderProperty(tableGroup, property, x, propY, tableWidth, padding, iconWidth, entity.type);
			});

			currentY += item.properties.length * rowHeight;
			rowCount += item.properties.length;
		} else {
			const propY = currentY + 40; // Increased from 16 to 40 (2.5x larger)
			renderProperty(tableGroup, item.property, x, propY, tableWidth, padding, iconWidth, entity.type);
			currentY += rowHeight;
			rowCount += 1;
		}
	});

	// Add ellipsis if there are more properties
	if (!fullHeightMode && totalDisplayRows > maxVisibleRows) {
		const ellipsisY = currentY + 15; // Increased from 6 to 15 (2.5x larger)
		const ellipsisText = document.createElementNS('http://www.w3.org/2000/svg', 'text');
		ellipsisText.setAttribute('x', x + tableWidth / 2);
		ellipsisText.setAttribute('y', ellipsisY);
		ellipsisText.setAttribute('text-anchor', 'middle');
		ellipsisText.classList.add('property-text');
		ellipsisText.textContent = '... (+' + (totalDisplayRows - maxVisibleRows) + ' more)';
		ellipsisText.style.fontSize = '28px'; // Increased from 11px to 28px (2.5x larger)
		ellipsisText.style.fill = '#9ca3af';
		tableGroup.appendChild(ellipsisText);
	}

	// FIFTH: Create selection overlay LAST (always present but hidden) - this ensures it renders on top
	const selectionOverlay = document.createElementNS('http://www.w3.org/2000/svg', 'rect');
	selectionOverlay.setAttribute('x', x);
	selectionOverlay.setAttribute('y', y);
	selectionOverlay.setAttribute('width', tableWidth);
	selectionOverlay.setAttribute('height', tableHeight);
	selectionOverlay.classList.add('selection-overlay');
	selectionOverlay.setAttribute('data-entity', entity.type);
	selectionOverlay.style.display = 'none'; // Hidden by default
	tableGroup.appendChild(selectionOverlay);

	setupTableInteraction(tableGroup, entity.type);
	svg.appendChild(tableGroup);
}

function getPropertiesToShow(groupedProperties, maxVisibleRows) {
	const result = [];
	let currentRowCount = 0;

	for (const item of groupedProperties) {
		if (item.type === 'group') {
			const remainingRows = maxVisibleRows - currentRowCount;
			if (remainingRows <= 0) break;

			if (item.properties.length <= remainingRows) {
				// Include entire group
				result.push(item);
				currentRowCount += item.properties.length;
			} else {
				// Partially include group
				result.push({
					type: 'group',
					properties: item.properties.slice(0, remainingRows)
				});
				currentRowCount = maxVisibleRows;
				break;
			}
		} else {
			if (currentRowCount >= maxVisibleRows) break;
			result.push(item);
			currentRowCount += 1;
		}
	}

	return result;
}

function renderProperty(tableGroup, property, x, propY, tableWidth, padding, iconWidth, entityName) {
	// Fixed left position for all property names (consistent alignment)
	const propertyNameX = x + iconWidth + 10; // Increased from 4 to 10 (2.5x larger)

	// Only show property-level icons (PK, FK, N, INH) to the left of name
	const propertyIcon = getPropertyLevelIcon(property);

	if (propertyIcon) {
		const iconElements = createPropertyIcon(propertyIcon, x + 10, propY); // Increased from 4 to 10 (2.5x larger)
		iconElements.forEach(element => {
			tableGroup.appendChild(element);

			// Add click handler for FK icons - navigate to related entity
			if (propertyIcon === 'FK' && property.isForeignKey) {
				element.style.cursor = 'pointer';
				element.addEventListener('click', () => {
					const targetType = getNavigationTargetType(property.name, entityName);
					if (targetType) {
						navigateToEntity(targetType);
					}
				});
			}

			// Add click handler for Navigation icons - navigate to related entity
			if (propertyIcon === 'N' && isNavigationProperty(property)) {
				element.style.cursor = 'pointer';
				element.addEventListener('click', () => navigateToEntity(property.type));
			}
		});
	}

	// Property name positioned at consistent left position
	const propText = document.createElementNS('http://www.w3.org/2000/svg', 'text');
	propText.setAttribute('x', propertyNameX);
	propText.setAttribute('y', propY);
	propText.classList.add('property-text');

	if (property.isInherited) {
		propText.classList.add('property-inherited');
	} else if (property.isKey) {
		propText.classList.add('property-key');
	} else if (property.isForeignKey) {
		propText.classList.add('property-foreign-key');
		// Add click handler for FK property names too
		propText.style.cursor = 'pointer';
		propText.addEventListener('click', () => {
			const targetType = getNavigationTargetType(property.name, entityName);
			if (targetType) {
				navigateToEntity(targetType);
			}
		});
	} else if (isNavigationProperty(property)) {
		propText.classList.add('property-navigation');
		propText.style.cursor = 'pointer';
		propText.addEventListener('click', () => navigateToEntity(property.type));
	}

	propText.textContent = property.name;
	tableGroup.appendChild(propText);

	// Add underline for navigation properties since SVG doesn't support text-decoration properly
	if (isNavigationProperty(property)) {
		// Estimate text width based on character count and font size
		const estimatedTextWidth = property.name.length * 19; // Increased from 7.5 to 19 (2.5x larger)
		const underline = document.createElementNS('http://www.w3.org/2000/svg', 'line');
		underline.setAttribute('x1', propertyNameX);
		underline.setAttribute('y1', propY + 5); // Increased from 2 to 5 (2.5x larger)
		underline.setAttribute('x2', propertyNameX + estimatedTextWidth);
		underline.setAttribute('y2', propY + 5); // Increased from 2 to 5 (2.5x larger)
		underline.setAttribute('stroke', '#059669');
		underline.setAttribute('stroke-width', '2.5'); // Increased from 1 to 2.5 (2.5x larger)
		underline.classList.add('navigation-underline');
		underline.style.cursor = 'pointer';
		underline.addEventListener('click', () => navigateToEntity(property.type));
		tableGroup.appendChild(underline);
	}

	// Property type (right-aligned with proper spacing and color coding)
	const typeText = document.createElementNS('http://www.w3.org/2000/svg', 'text');

	// Calculate position for type text and icon
	const typeIcon = getTypeIcon(property.type);
	const typeTextX = typeIcon ? (x + tableWidth - padding - 50) : (x + tableWidth - padding); // Increased from 20 to 50 (2.5x larger)

	typeText.setAttribute('x', typeTextX);
	typeText.setAttribute('y', propY);
	typeText.setAttribute('text-anchor', 'end'); // Right-align the type text
	typeText.classList.add('property-text', 'property-type');

	// Add type-specific color class
	const typeColorClass = getTypeColorClass(property.type);
	typeText.classList.add(typeColorClass);

	// Clean up navigation property type names for display and preserve nullables
	let displayType = property.type;
	if (isNavigationProperty(property)) {
		displayType = displayType.replace(/^ICollection<(.+)>$/, '$1').replace(/^List<(.+)>$/, '$1');
	}

	typeText.textContent = displayType;
	tableGroup.appendChild(typeText);

	// Add type icon to the RIGHT of the type text - with corrected Y positioning
	if (typeIcon) {
		const typeIconElements = createPropertyIcon(typeIcon, x + tableWidth - padding - 40, propY); // Increased from 16 to 40 (2.5x larger)
		typeIconElements.forEach(element => {
			tableGroup.appendChild(element);

			// Add click handler for navigation property type icons
			if (isNavigationProperty(property)) {
				element.style.cursor = 'pointer';
				element.addEventListener('click', () => navigateToEntity(property.type));
			}
		});
	}
}

// Helper function to get navigation target type from FK property name
function getNavigationTargetType(fkPropertyName, entityName) {
	// Remove 'Id' suffix to get the expected navigation property name
	const navPropName = fkPropertyName.replace(/Id$/, '');

	// Look up the entity data from our global entities array
	const entityData = entities.find(e => e.type === entityName);
	if (entityData) {
		// Look for a navigation property with the expected name
		const allProps = [...(entityData.properties || []), ...(entityData.inheritedProperties || [])];
		const navProp = allProps.find(p =>
			p.name === navPropName &&
			isNavigationProperty(p)
		);

		if (navProp) {
			return navProp.type;
		}
	}

	// Fallback: assume it follows naming convention
	return navPropName + 'Model';
}

// Table interaction setup - handles selection and deselection
function setupTableInteraction(tableGroup, entityType) {
	// Add click handler to the entire table group
	tableGroup.addEventListener('click', function(e) {
		e.stopPropagation(); // Prevent event bubbling to background
		
		// Check if user clicked on an interactive element (navigation links, icons, etc.)
		const clickedElement = e.target;
		
		// More specific check for interactive elements that should not trigger table selection
		const isInteractiveElement = 
			clickedElement.style.cursor === 'pointer' && (
				// FK or Navigation property links
				clickedElement.classList.contains('property-foreign-key') ||
				clickedElement.classList.contains('property-navigation') ||
				clickedElement.classList.contains('navigation-underline') ||
				// Property icons that have navigation functionality
				(clickedElement.tagName === 'circle' || clickedElement.tagName === 'rect' || clickedElement.tagName === 'text') &&
				clickedElement.parentElement && clickedElement.parentElement.style.cursor === 'pointer'
			);
		
		// If clicked on interactive element, let it handle its own action
		if (isInteractiveElement) {
			return;
		}
		
		// Handle table selection/deselection
		if (selectedTable === entityType) {
			// If this table is already selected, deselect it
			deselectTable();
		} else {
			// Otherwise, select this table
			selectTable(entityType);
		}
	});

	// Add hover effect for better user experience
	tableGroup.addEventListener('mouseenter', function() {
		if (selectedTable !== entityType) {
			tableGroup.style.cursor = 'pointer';
		}
	});

	tableGroup.addEventListener('mouseleave', function() {
		tableGroup.style.cursor = 'default';
	});
}

// Select a table and update visual state
function selectTable(entityType) {
	// Clear any previously selected table
	clearTableSelection();
	
	// Set new selected table
	selectedTable = entityType;
	showOnlySelectedRelations = true;
	
	// Find and highlight the selected table
	const tableGroup = document.querySelector(`[data-entity="${entityType}"]`);
	if (tableGroup) {
		// Add selected class to table group for CSS styling
		tableGroup.classList.add('selected');
		
		const selectionOverlay = tableGroup.querySelector('.selection-overlay');
		if (selectionOverlay) {
			selectionOverlay.style.display = 'block';
		}
		
		// Add selected class to table box for additional styling if needed
		const tableBox = tableGroup.querySelector('.table-box');
		if (tableBox) {
			tableBox.classList.add('selected');
		}
	}
	
	// Update relationships to show only selected table's relationships
	updateRelationships();
	
	console.log(`📋 Selected table: ${entityType}`);
}

// Deselect the currently selected table
function deselectTable() {
	if (selectedTable) {
		const entityType = selectedTable;
		
		// Clear selection state
		selectedTable = null;
		showOnlySelectedRelations = false;
		
		// Remove visual selection indicators
		const tableGroup = document.querySelector(`[data-entity="${entityType}"]`);
		if (tableGroup) {
			// Remove selected class from table group
			tableGroup.classList.remove('selected');
			
			const selectionOverlay = tableGroup.querySelector('.selection-overlay');
			if (selectionOverlay) {
				selectionOverlay.style.display = 'none';
			}
			
			// Remove selected class from table box
			const tableBox = tableGroup.querySelector('.table-box');
			if (tableBox) {
				tableBox.classList.remove('selected');
			}
		}
		
		// Update relationships to show all relationships again
		updateRelationships();
		
		console.log(`📋 Deselected table: ${entityType}`);
	}
}

// Clear all table selections (used by other functions)
function clearTableSelection() {
	if (selectedTable) {
		deselectTable();
	}
}