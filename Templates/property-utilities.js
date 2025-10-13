// Property utilities and grouping
function getPropertyIcon(property) {
	// This function now only handles property-level icons (PK, FK, N, INH)
	return getPropertyLevelIcon(property);
}

function getPropertyLevelIcon(property) {
	if (property.isInherited) return 'INH';
	if (property.isKey) return 'PK';
	if (property.isForeignKey) return 'FK';
	if (isNavigationProperty(property)) return 'N';
	return null; // No property-level icon for regular properties
}

function getTypeIcon(type) {
	// Normalize the type (handle nullables)
	const cleanType = type.replace(/\?$/, ''); // Remove trailing ?
	const isNullable = type.endsWith('?');
	
	// Return type information for visual icon creation
	
	// Basic C# types
	if (cleanType === 'string') return { iconType: 'string', isNullable };
	if (['int', 'long', 'short', 'byte'].includes(cleanType)) return { iconType: 'int', isNullable };
	if (['double', 'float', 'decimal'].includes(cleanType)) return { iconType: 'double', isNullable };
	if (cleanType === 'bool') return { iconType: 'bool', isNullable };
	
	// .NET Framework types
	if (cleanType === 'Guid') return { iconType: 'Guid', isNullable };
	if (['DateTime', 'DateTimeOffset', 'TimeSpan'].includes(cleanType)) return { iconType: 'DateTime', isNullable };
	
	// Collection types
	if (cleanType.startsWith('ICollection') || cleanType.startsWith('List') || cleanType.startsWith('IList')) {
		return { iconType: 'Collection', isNullable };
	}
	
	// JSON types
	if (cleanType === 'JsonDocument') {
		return { iconType: 'JsonDocument', isNullable };
	}
	
	// Navigation properties (Model/Entity types) - show arrow icon for these
	if (cleanType.includes('Model') || cleanType.includes('Entity')) {
		return { iconType: 'Navigation', isNullable };
	}
	
	// Enum types (usually end with certain patterns or are single words without Model suffix)
	if (!cleanType.includes('<') && !cleanType.includes('Model') && !cleanType.includes('Entity') && 
		!['string', 'int', 'long', 'bool', 'double', 'float', 'decimal', 'byte', 'short', 'Guid', 'DateTime', 'DateTimeOffset', 'TimeSpan', 'JsonDocument'].includes(cleanType)) {
		return { iconType: 'Enum', isNullable };
	}
	
	// Default for unknown types
	return { iconType: 'unknown', isNullable };
}

function getTypeColorClass(type) {
	// Get the color class for property type text
	const cleanType = type.replace(/\?$/, ''); // Remove trailing ?
	
	// Basic C# types
	if (cleanType === 'string') return 'property-type-string';
	if (['int', 'long', 'short', 'byte'].includes(cleanType)) return 'property-type-int';
	if (['double', 'float', 'decimal'].includes(cleanType)) return 'property-type-double';
	if (cleanType === 'bool') return 'property-type-bool';
	
	// .NET Framework types
	if (cleanType === 'Guid') return 'property-type-guid';
	if (['DateTime', 'DateTimeOffset', 'TimeSpan'].includes(cleanType)) return 'property-type-datetime';
	
	// Collection types
	if (cleanType.startsWith('ICollection') || cleanType.startsWith('List') || cleanType.startsWith('IList')) {
		return 'property-type-collection';
	}
	
	// JSON types
	if (cleanType === 'JsonDocument') {
		return 'property-type-jsondocument';
	}
	
	// Navigation properties (Model/Entity types)
	if (cleanType.includes('Model') || cleanType.includes('Entity')) {
		return 'property-type-navigation';
	}
	
	// Enum types
	if (!cleanType.includes('<') && !cleanType.includes('Model') && !cleanType.includes('Entity') && 
		!['string', 'int', 'long', 'bool', 'double', 'float', 'decimal', 'byte', 'short', 'Guid', 'DateTime', 'DateTimeOffset', 'TimeSpan', 'JsonDocument'].includes(cleanType)) {
		return 'property-type-enum';
	}
	
	// Default
	return 'property-type-unknown';
}

function sortProperties(properties) {
	return properties.sort((a, b) => {
		// Primary sort: inherited properties first
		if (a.isInherited && !b.isInherited) return -1;
		if (!a.isInherited && b.isInherited) return 1;

		// Secondary sort: alphabetical within each group
		return a.name.localeCompare(b.name);
	});
}

function groupPropertiesWithNavigation(visibleProperties, allProperties) {
	const grouped = [];
	const processed = new Set();

	visibleProperties.forEach(prop => {
		if (processed.has(prop.name)) return;

		// Only group when both FK and NAV are visible and we're showing navigation properties
		if (prop.isForeignKey && showNavigationProperties) {
			// Find corresponding navigation property - be more specific about matching
			const navPropName = prop.name.replace(/Id$/, ''); // Remove 'Id' suffix
			const navProp = visibleProperties.find(p => 
				p.name === navPropName &&
				isNavigationProperty(p) &&
				!processed.has(p.name) // Make sure it hasn't been processed yet
			);

			if (navProp) {
				// Group FK and NAV together
				grouped.push({
					type: 'group',
					properties: [prop, navProp]
				});
				processed.add(prop.name);
				processed.add(navProp.name);
			} else {
				// Just add the FK property alone
				grouped.push({
					type: 'single',
					property: prop
				});
				processed.add(prop.name);
			}
		} else {
			// Single property (not part of FK-NAV group)
			grouped.push({
				type: 'single',
				property: prop
			});
			processed.add(prop.name);
		}
	});

	return grouped;
}

function isNavigationProperty(property) {
	const navPatterns = [
		/^ICollection</,
		/^List</,
		/^IList</,
		/^HashSet</,
		/^ISet</,
		/Model$/,
		/Entity$/
	];

	// First check: Standard collection and naming patterns
	if (navPatterns.some(pattern => pattern.test(property.type))) {
		return true;
	}

	// ENHANCED: Second check - detect entity references by checking if the type exists in our entities object
	// This catches many-to-one relationships like "Tag", "Task", "User", etc.
	const cleanType = property.type.replace(/\?$/, ''); // Remove nullable marker
	
	// Check if this type name matches an existing entity
	if (typeof entities !== 'undefined' && entities[cleanType]) {
		console.log(`🔍 Detected navigation property: ${property.name} (${property.type}) - matches entity ${cleanType}`);
		return true;
	}

	return false;
}

function navigateToEntity(typeName) {
	// Clean up type name (remove generic brackets, nullability, etc.)
	const cleanTypeName = typeName.replace(/\?$/, '').replace(/ICollection<(.+)>/, '$1').replace(/List<(.+)>/, '$1');

	const targetTable = document.querySelector('[data-entity="' + cleanTypeName + '"]');
	if (targetTable) {
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
	}
}

function createPropertyIcon(iconType, x, y) {
	const iconSize = 30; // Increased from 12 to 30 (2.5x larger)
	const elements = [];
	
	// Handle property-level icons (PK, FK, N, INH) - these get circles
	if (['PK', 'FK', 'N', 'INH'].includes(iconType)) {
		// Create background circle - aligned with text middle
		const bgCircle = document.createElementNS('http://www.w3.org/2000/svg', 'circle');
		bgCircle.setAttribute('cx', x + iconSize/2 + 10); // Increased from 4 to 10 (2.5x larger)
		bgCircle.setAttribute('cy', y - 12); // Increased from -5 to -12 (2.5x larger)
		bgCircle.setAttribute('r', iconSize/2);
		bgCircle.classList.add('property-icon-circle');
		
		// Create text - aligned with text middle
		const iconText = document.createElementNS('http://www.w3.org/2000/svg', 'text');
		iconText.setAttribute('x', x + iconSize/2 + 10); // Increased from 4 to 10 (2.5x larger)
		iconText.setAttribute('y', y - 12); // Increased from -5 to -12 (2.5x larger)
		iconText.setAttribute('text-anchor', 'middle');
		iconText.setAttribute('dominant-baseline', 'central');
		iconText.classList.add('property-icon-text');
		iconText.textContent = iconType;
		
		// Style based on icon type
		switch(iconType) {
			case 'PK':
				bgCircle.classList.add('icon-pk');
				break;
			case 'FK':
				bgCircle.classList.add('icon-fk');
				break;
			case 'N':
				bgCircle.classList.add('icon-nav');
				break;
			case 'INH':
				bgCircle.classList.add('icon-inh');
				break;
		}
		
		elements.push(bgCircle);
		elements.push(iconText);
	}
	// Handle type icons - NO circles, larger size like capital M
	else if (typeof iconType === 'object' && iconType.iconType) {
		const { iconType: type, isNullable } = iconType;
		
		let iconContent = null;
		const textY = y - 12; // Increased from -5 to -12 (2.5x larger)
		const centerX = x + iconSize/2 + 10; // Increased from 4 to 10 (2.5x larger)
		const strokeColor = isNullable ? '#9ca3af' : '#4b5563';
		const fillColor = isNullable ? '#9ca3af' : '#4b5563';
		
		switch(type) {
			case 'string':
				// Letter "a" for string - larger size
				iconContent = document.createElementNS('http://www.w3.org/2000/svg', 'text');
				iconContent.setAttribute('x', centerX);
				iconContent.setAttribute('y', textY);
				iconContent.setAttribute('text-anchor', 'middle');
				iconContent.setAttribute('dominant-baseline', 'central');
				iconContent.setAttribute('fill', fillColor);
				iconContent.classList.add('type-icon-symbol-large');
				iconContent.textContent = 'a';
				break;
				
			case 'int':
			case 'long':
			case 'short':
			case 'byte':
				// Hash symbol for integers - larger size
				iconContent = document.createElementNS('http://www.w3.org/2000/svg', 'text');
				iconContent.setAttribute('x', centerX);
				iconContent.setAttribute('y', textY);
				iconContent.setAttribute('text-anchor', 'middle');
				iconContent.setAttribute('dominant-baseline', 'central');
				iconContent.setAttribute('fill', fillColor);
				iconContent.classList.add('type-icon-symbol-large');
				iconContent.textContent = '#';
				break;
				
			case 'double':
			case 'float':
			case 'decimal':
				// Decimal point for floating point numbers - larger size
				iconContent = document.createElementNS('http://www.w3.org/2000/svg', 'text');
				iconContent.setAttribute('x', centerX);
				iconContent.setAttribute('y', textY);
				iconContent.setAttribute('text-anchor', 'middle');
				iconContent.setAttribute('dominant-baseline', 'central');
				iconContent.setAttribute('fill', fillColor);
				iconContent.classList.add('type-icon-symbol-large');
				iconContent.textContent = '.';
				break;
				
			case 'bool':
				// Checkmark for boolean - larger size
				iconContent = document.createElementNS('http://www.w3.org/2000/svg', 'text');
				iconContent.setAttribute('x', centerX);
				iconContent.setAttribute('y', textY);
				iconContent.setAttribute('text-anchor', 'middle');
				iconContent.setAttribute('dominant-baseline', 'central');
				iconContent.setAttribute('fill', fillColor);
				iconContent.classList.add('type-icon-symbol-large');
				iconContent.textContent = '✓';
				break;
				
			case 'Guid':
				// Simple square for Guid (unique identifier) - larger size
				iconContent = document.createElementNS('http://www.w3.org/2000/svg', 'rect');
				iconContent.setAttribute('x', centerX - 8); // Increased from 3 to 8 (2.5x larger)
				iconContent.setAttribute('y', textY - 8); // Increased from 3 to 8 (2.5x larger)
				iconContent.setAttribute('width', '16'); // Increased from 6 to 16 (2.5x larger)
				iconContent.setAttribute('height', '16'); // Increased from 6 to 16 (2.5x larger)
				iconContent.setAttribute('fill', fillColor);
				iconContent.setAttribute('stroke', strokeColor);
				iconContent.classList.add('type-icon-shape-large');
				break;
				
			case 'DateTime':
			case 'DateTimeOffset':
			case 'TimeSpan':
				// Simple calendar icon (rectangle with line) - larger size - create as individual elements instead of group
				const dateRect = document.createElementNS('http://www.w3.org/2000/svg', 'rect');
				dateRect.setAttribute('x', centerX - 10); // Increased from 4 to 10 (2.5x larger)
				dateRect.setAttribute('y', textY - 8); // Increased from 3 to 8 (2.5x larger)
				dateRect.setAttribute('width', '20'); // Increased from 8 to 20 (2.5x larger)
				dateRect.setAttribute('height', '16'); // Increased from 6 to 16 (2.5x larger)
				dateRect.setAttribute('fill', 'none');
				dateRect.setAttribute('stroke', strokeColor);
				dateRect.setAttribute('stroke-width', '2'); // Increased from 0.8 to 2 (2.5x larger)
				dateRect.classList.add('type-icon-shape-large');
				elements.push(dateRect);
				
				const dateLine = document.createElementNS('http://www.w3.org/2000/svg', 'line');
				dateLine.setAttribute('x1', centerX - 8); // Increased from 3 to 8 (2.5x larger)
				dateLine.setAttribute('y1', textY - 2); // Increased from 1 to 2 (2.5x larger)
				dateLine.setAttribute('x2', centerX + 8); // Increased from 3 to 8 (2.5x larger)
				dateLine.setAttribute('y2', textY - 2); // Increased from 1 to 2 (2.5x larger)
				dateLine.setAttribute('stroke', strokeColor);
				dateLine.setAttribute('stroke-width', '2'); // Increased from 0.8 to 2 (2.5x larger)
				dateLine.classList.add('type-icon-shape-large');
				elements.push(dateLine);
				
				// Return early for DateTime since we already added both elements
				return elements;
				
			case 'JsonDocument':
				// Curly braces for JSON - larger size
				iconContent = document.createElementNS('http://www.w3.org/2000/svg', 'text');
				iconContent.setAttribute('x', centerX);
				iconContent.setAttribute('y', textY);
				iconContent.setAttribute('text-anchor', 'middle');
				iconContent.setAttribute('dominant-baseline', 'central');
				iconContent.setAttribute('fill', fillColor);
				iconContent.classList.add('type-icon-symbol-large');
				iconContent.textContent = '{}';
				break;
				
			case 'Collection':
				// Square brackets for collections - larger size
				iconContent = document.createElementNS('http://www.w3.org/2000/svg', 'text');
				iconContent.setAttribute('x', centerX);
				iconContent.setAttribute('y', textY);
				iconContent.setAttribute('text-anchor', 'middle');
				iconContent.setAttribute('dominant-baseline', 'central');
				iconContent.setAttribute('fill', fillColor);
				iconContent.classList.add('type-icon-symbol-large');
				iconContent.textContent = '[]';
				break;
				
			case 'Enum':
				// Simple diamond for enums - larger size
				iconContent = document.createElementNS('http://www.w3.org/2000/svg', 'polygon');
				iconContent.setAttribute('points', `${centerX},${textY - 8} ${centerX + 8},${textY} ${centerX},${textY + 8} ${centerX - 8},${textY}`); // Increased from 3 to 8 (2.5x larger)
				iconContent.setAttribute('fill', fillColor);
				iconContent.setAttribute('stroke', strokeColor);
				iconContent.classList.add('type-icon-shape-large');
				break;
				
			case 'Navigation':
				// Arrow for navigation properties - larger size
				iconContent = document.createElementNS('http://www.w3.org/2000/svg', 'polygon');
				iconContent.setAttribute('points', `${centerX - 8},${textY - 6} ${centerX + 6},${textY} ${centerX - 8},${textY + 6} ${centerX - 5},${textY + 3} ${centerX + 3},${textY} ${centerX - 5},${textY - 3}`);
				iconContent.classList.add('type-icon-shape-large');
				iconContent.setAttribute('fill', '#059669'); // Green color for navigation
				break;
				
			default:
				// Question mark for unknown types - larger size
				iconContent = document.createElementNS('http://www.w3.org/2000/svg', 'text');
				iconContent.setAttribute('x', centerX);
				iconContent.setAttribute('y', textY);
				iconContent.setAttribute('text-anchor', 'middle');
				iconContent.setAttribute('dominant-baseline', 'central');
				iconContent.setAttribute('fill', fillColor);
				iconContent.classList.add('type-icon-symbol-large');
				iconContent.textContent = '?';
		}
		
		if (iconContent) {
			elements.push(iconContent);
		}
	}
	
	return elements;
}