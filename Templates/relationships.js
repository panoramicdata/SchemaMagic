// Relationship lines generation and management with Crow's Foot notation
function generateRelationships(svg) {
	if (!showRelationships) {
		console.log('🔗 Relationships disabled - skipping generation');
		return;
	}

	console.log('🔗 Starting relationship generation with Crow\'s Foot notation...');
	let relationshipCount = 0;
	let attemptedCount = 0;

	Object.values(entities).forEach(entity => {
		const fromEntityName = entity.type;
		const foreignKeys = entity.properties.filter(p => p.isForeignKey);

		if (foreignKeys.length > 0) {
			console.log(`🔗 Entity ${fromEntityName} has ${foreignKeys.length} foreign keys:`,
				foreignKeys.map(fk => fk.name));
		}

		entity.properties.forEach(prop => {
			if (prop.isForeignKey) {
				attemptedCount++;
				console.log(`🔗 Processing FK: ${fromEntityName}.${prop.name}`);

				// Try multiple strategies to find the target entity
				const targetEntityName = findTargetEntity(prop.name);

				if (targetEntityName && entities[targetEntityName]) {
					console.log(`✅ Found target: ${prop.name} -> ${targetEntityName}`);
					createCrowsFootRelationshipLine(svg, fromEntityName, targetEntityName, prop.name);
					relationshipCount++;
				} else {
					console.log(`❌ Could not find target entity for FK ${fromEntityName}.${prop.name}`);
					console.log(`   Available entities:`, Object.keys(entities).slice(0, 5).join(', ') + '...');
				}
			}
		});
	});

	console.log(`🔗 Relationship generation complete: ${relationshipCount}/${attemptedCount} relationships created`);

	// Debug: check if any lines were actually added to the SVG
	const relationshipLines = svg.querySelectorAll('.relationship-line');
	console.log(`🔗 Total relationship lines in SVG: ${relationshipLines.length}`);
}

function findTargetEntity(foreignKeyName) {
	console.log(`🔍 Finding target for FK: ${foreignKeyName}`);

	// Strategy 1: Remove 'Id' suffix (e.g., DeviceId -> Device)
	let candidate = foreignKeyName.replace(/Id$/, '');
	console.log(`   Strategy 1 - Remove Id: ${candidate}`);
	if (entities[candidate]) {
		console.log(`   ✅ Found: ${candidate}`);
		return candidate;
	}

	// Strategy 2: Try common patterns
	const patterns = [
		foreignKeyName.replace(/Id$/, 'Model'), // DeviceId -> DeviceModel
		foreignKeyName, // Sometimes the FK name matches exactly
		foreignKeyName.replace(/Model$/, ''), // DeviceModelId -> Device
	];

	console.log(`   Strategy 2 - Common patterns:`, patterns);
	for (const pattern of patterns) {
		if (entities[pattern]) {
			console.log(`   ✅ Found: ${pattern}`);
			return pattern;
		}
	}

	// Strategy 3: Look for entities with similar names
	const entityNames = Object.keys(entities);
	console.log(`   Strategy 3 - Fuzzy matching against ${entityNames.length} entities`);
	for (const entityName of entityNames) {
		// Check if the entity name is contained in the FK name
		if (foreignKeyName.toLowerCase().includes(entityName.toLowerCase()) ||
			entityName.toLowerCase().includes(candidate.toLowerCase())) {
			console.log(`   ✅ Fuzzy match found: ${entityName}`);
			return entityName;
		}
	}

	console.log(`   ❌ No target found for ${foreignKeyName}`);
	return null;
}

function createCrowsFootRelationshipLine(svg, fromEntityName, toEntityName, propertyName) {
	console.log(`🐾 Creating Crow's Foot relationship: ${fromEntityName}.${propertyName} -> ${toEntityName}`);

	// Analyze the relationship to determine cardinality
	const relationship = analyzeRelationshipCardinality(fromEntityName, toEntityName, propertyName);
	console.log(`📊 Relationship analysis:`, relationship);

	// Get connection points
	const fromInfo = getTableConnectionPoint(fromEntityName, propertyName, null);
	const toInfo = getTableConnectionPoint(toEntityName, 'Id', null);

	if (!fromInfo || !toInfo) {
		console.log(`❌ Could not get connection points for ${fromEntityName} -> ${toEntityName}`);
		return;
	}

	// Determine smart positioning based on table locations
	let fromX, fromY, toX, toY;

	fromY = fromInfo.y;
	toY = toInfo.y;

	if (fromInfo.centerX < toInfo.centerX) {
		// Source table is to the LEFT of target table
		fromX = fromInfo.rightEdge;
		toX = toInfo.leftEdge;
		console.log(`🔄 LEFT->RIGHT: Source right edge(${fromX},${fromY}) -> Target left edge(${toX},${toY})`);
	} else {
		// Source table is to the RIGHT of target table
		fromX = fromInfo.leftEdge;
		toX = toInfo.rightEdge;
		console.log(`🔄 RIGHT->LEFT: Source left edge(${fromX},${fromY}) -> Target right edge(${toX},${toY})`);
	}

	const line = document.createElementNS('http://www.w3.org/2000/svg', 'path');

	// Create angled connector with proper coordinates
	const midX = (fromX + toX) / 2;
	const pathData = `M ${fromX} ${fromY} L ${midX} ${fromY} L ${midX} ${toY} L ${toX} ${toY}`;

	line.setAttribute('d', pathData);
	line.classList.add('relationship-line');
	line.setAttribute('data-from', fromEntityName);
	line.setAttribute('data-to', toEntityName);
	line.setAttribute('data-property', propertyName);
	line.setAttribute('data-relationship-type', relationship.type);

	// Set appropriate crow's foot markers based on relationship analysis
	const markers = getCrowsFootMarkers(relationship);

	// For foreign key relationships:
	// - FROM entity has FK = "many" side = needs crow's foot marker
	// - TO entity has PK = "one" side = needs single line marker
	// In SVG, marker-start is at the path start, marker-end is at path end
	line.setAttribute('marker-start', markers.fromSide); // Many side (FK table start)
	line.setAttribute('marker-end', markers.toSide);     // One side (PK table end)

	console.log(`🐾 Crow's Foot line: ${fromEntityName}(${fromX},${fromY}) -> ${toEntityName}(${toX},${toY})`);
	console.log(`   Type: ${relationship.type}, Markers: ${markers.fromSide} -> ${markers.toSide}`);
	console.log(`   Path: ${pathData}`);

	// Insert line BEFORE any table groups to ensure it appears behind tables
	const firstTableGroup = svg.querySelector('.table-group');
	if (firstTableGroup) {
		svg.insertBefore(line, firstTableGroup);
	} else {
		svg.appendChild(line);
	}
}

function analyzeRelationshipCardinality(fromEntityName, toEntityName, propertyName) {
	const fromEntity = entities[fromEntityName];
	const toEntity = entities[toEntityName];

	if (!fromEntity || !toEntity) {
		return { type: '1:N', fromOptional: false, toOptional: false };
	}

	// Find the foreign key property details
	const fkProperty = fromEntity.properties.find(p => p.name === propertyName);
	const allFromProps = [...(fromEntity.properties || []), ...(fromEntity.inheritedProperties || [])];
	const fkProp = allFromProps.find(p => p.name === propertyName);

	// Check if FK is nullable (optional relationship)
	const isNullable = fkProp && (fkProp.type.includes('?') || fkProp.nullable === true);

	// Look for navigation properties to determine relationship type
	const allToProps = [...(toEntity.properties || []), ...(toEntity.inheritedProperties || [])];

	// Check if target entity has a collection navigation property back to source
	const navPropName = fromEntityName.replace(/Model$/, ''); // Remove 'Model' suffix for collection name
	const collectionNavProp = allToProps.find(p =>
		p.type.includes(`ICollection<${fromEntityName}>`) ||
		p.type.includes(`List<${fromEntityName}>`) ||
		p.name.toLowerCase().includes(navPropName.toLowerCase() + 's') // Plural form
	);

	// Check if source entity has a navigation property to target (indicating potential 1:1)
	const sourceNavPropName = toEntityName.replace(/Model$/, '');
	const sourceNavProp = allFromProps.find(p =>
		p.type === toEntityName ||
		p.name === sourceNavPropName
	);

	// Determine relationship type based on analysis
	let relationshipType;

	if (collectionNavProp && sourceNavProp) {
		// Both sides have navigation properties - this is 1:N (most common)
		relationshipType = '1:N';
	} else if (sourceNavProp && !collectionNavProp) {
		// Only source has nav prop, no collection on target - could be 1:1 or 1:0..1
		relationshipType = isNullable ? '1:0..1' : '1:1';
	} else if (collectionNavProp) {
		// Target has collection - definitely 1:N
		relationshipType = '1:N';
	} else {
		// No navigation properties found - assume 1:N (most common for FK relationships)
		relationshipType = '1:N';
	}

	console.log(`📊 Relationship analysis for ${fromEntityName}.${propertyName} -> ${toEntityName}:`);
	console.log(`   FK nullable: ${isNullable}`);
	console.log(`   Collection nav prop: ${collectionNavProp ? collectionNavProp.name : 'none'}`);
	console.log(`   Source nav prop: ${sourceNavProp ? sourceNavProp.name : 'none'}`);
	console.log(`   Determined type: ${relationshipType}`);

	return {
		type: relationshipType,
		fromOptional: isNullable, // FK side optionality
		toOptional: false // PK side is typically required
	};
}

function getCrowsFootMarkers(relationship) {
	// Base markers (normal blue)
	let fromSide, toSide;

	switch (relationship.type) {
		case '1:1':
			// One-to-one: both sides have single line
			fromSide = 'url(#one-side)';
			toSide = 'url(#one-side)';
			break;

		case '1:0..1':
			// One-to-zero-or-one: FK side has circle (optional), PK side has line
			fromSide = relationship.fromOptional ? 'url(#one-optional-side)' : 'url(#one-side)';
			toSide = 'url(#one-side)';
			break;

		case '1:N':
		default:
			// CORRECTED: For One-to-Many relationships:
			// - FROM side (FK table) = "many" side = needs crow's foot
			// - TO side (PK table) = "one" side = needs single line
			fromSide = relationship.fromOptional ? 'url(#many-optional-side)' : 'url(#many-side)';
			toSide = 'url(#one-side)';
			break;
	}

	return { fromSide, toSide };
}

function getCrowsFootMarkersHighlighted(relationship) {
	// Highlighted markers (red) - same logic but with highlighted versions
	let fromSide, toSide;

	switch (relationship.type) {
		case '1:1':
			fromSide = 'url(#one-side-highlighted)';
			toSide = 'url(#one-side-highlighted)';
			break;

		case '1:0..1':
			fromSide = relationship.fromOptional ? 'url(#one-optional-side-highlighted)' : 'url(#one-side-highlighted)';
			toSide = 'url(#one-side-highlighted)';
			break;

		case '1:N':
		default:
			// CORRECTED: For One-to-Many relationships:
			// - FROM side (FK table) = "many" side = needs crow's foot
			// - TO side (PK table) = "one" side = needs single line
			fromSide = relationship.fromOptional ? 'url(#many-optional-side-highlighted)' : 'url(#many-side-highlighted)';
			toSide = 'url(#one-side-highlighted)';
			break;
	}

	return { fromSide, toSide };
}

function getTableConnectionPoint(entityName, propertyName, direction) {
	const tableGroup = document.querySelector('[data-entity="' + entityName + '"]');
	if (!tableGroup) {
		console.log(`❌ Could not find table group for entity: ${entityName}`);
		return null;
	}

	const rect = tableGroup.querySelector('.table-box');
	if (!rect) {
		console.log(`❌ Could not find table-box for entity: ${entityName}`);
		return null;
	}

	const x = parseFloat(rect.getAttribute('x'));
	const y = parseFloat(rect.getAttribute('y'));
	const width = parseFloat(rect.getAttribute('width'));
	const height = parseFloat(rect.getAttribute('height'));

	// Get the center point of the table for direction calculation
	const centerX = x + width / 2;
	const centerY = y + height / 2;

	// Find the specific property connection point
	const propertyY = findPropertyConnectionPoint(tableGroup, propertyName, centerY);

	// Return connection point info without determining side yet
	return {
		x: x,
		y: propertyY,
		width: width,
		centerX: centerX,
		centerY: centerY,
		leftEdge: x,
		rightEdge: x + width
	};
}

function findPropertyConnectionPoint(tableGroup, propertyName, fallbackY) {
	// Try to find the specific property text element first
	const propertyTexts = tableGroup.querySelectorAll('.property-text');

	for (const propText of propertyTexts) {
		if (propText.textContent === propertyName) {
			const y = parseFloat(propText.getAttribute('y'));
			console.log(`🎯 Found property ${propertyName} at y=${y}`);
			return y;
		}
	}

	// If not found in visible properties, check if it's 'Id' and likely inherited
	if (propertyName === 'Id') {
		console.log(`🎯 Property 'Id' not found in visible properties - assuming inherited, using header center`);

		const headerRect = tableGroup.querySelector('.table-header');
		if (headerRect) {
			const headerY = parseFloat(headerRect.getAttribute('y'));
			const headerHeight = parseFloat(headerRect.getAttribute('height'));
			const headerCenterY = headerY + headerHeight / 2;

			console.log(`🎯 Using header center: headerY=${headerY}, height=${headerHeight}, center=${headerCenterY}`);
			return headerCenterY;
		}
	}

	// Last resort fallback
	console.log(`🎯 Using fallback center Y=${fallbackY} for property ${propertyName}`);
	return fallbackY;
}

function updateRelationships() {
	const lines = document.querySelectorAll('.relationship-line');
	console.log(`🔄 Updating ${lines.length} relationship lines`);

	lines.forEach(line => {
		const from = line.getAttribute('data-from');
		const to = line.getAttribute('data-to');
		const propertyName = line.getAttribute('data-property');
		const relationshipType = line.getAttribute('data-relationship-type') || '1:N';

		// Get updated connection points
		const fromInfo = getTableConnectionPoint(from, propertyName, null);
		const toInfo = getTableConnectionPoint(to, 'Id', null);

		if (fromInfo && toInfo) {
			// Determine smart positioning based on current table locations
			let fromX, fromY, toX, toY;

			fromY = fromInfo.y;
			toY = toInfo.y;

			if (fromInfo.centerX < toInfo.centerX) {
				// From table is to the LEFT of target table - connect from RIGHT edge to LEFT edge
				fromX = fromInfo.rightEdge; // Connect from right edge of source
				toX = toInfo.leftEdge;      // Connect to left edge of destination
			} else {
				// From table is to the RIGHT of target table - connect from LEFT edge to RIGHT edge
				fromX = fromInfo.leftEdge;  // Connect from left edge of source
				toX = toInfo.rightEdge;     // Connect to right edge of destination
			}

			const midX = (fromX + toX) / 2;
			const pathData = `M ${fromX} ${fromY} L ${midX} ${fromY} L ${midX} ${toY} L ${toX} ${toY}`;
			line.setAttribute('d', pathData);
		}

		line.classList.remove('highlighted', 'dimmed', 'hidden');

		if (!showRelationships) {
			line.style.display = 'none';
		} else {
			line.style.display = 'block';

			// Reconstruct relationship analysis for marker updates
			const relationship = { type: relationshipType, fromOptional: false, toOptional: false };

			if (showOnlySelectedRelations && selectedTable) {
				if (from === selectedTable || to === selectedTable) {
					line.classList.add('highlighted');
					// Use highlighted crow's foot markers
					const markers = getCrowsFootMarkersHighlighted(relationship);
					line.setAttribute('marker-start', markers.fromSide);
					line.setAttribute('marker-end', markers.toSide);
				} else {
					line.classList.add('dimmed');
				}
			} else {
				// Use normal crow's foot markers
				const markers = getCrowsFootMarkers(relationship);
				line.setAttribute('marker-start', markers.fromSide);
				line.setAttribute('marker-end', markers.toSide);
			}
		}
	});
}