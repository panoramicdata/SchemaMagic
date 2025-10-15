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

	// Get connection points for the specific property rows
	const fromInfo = getTableConnectionPoint(fromEntityName, propertyName, null);
	const toInfo = getTableConnectionPoint(toEntityName, 'Id', null);

	if (!fromInfo || !toInfo) {
		console.log(`❌ Could not get connection points for ${fromEntityName} -> ${toEntityName}`);
		return;
	}

	// Constants for routing
	const MARKER_OFFSET = 50;
	const MARKER_GAP = 20; // Gap between table edge and crow's foot
	const SELF_REF_LOOP_WIDTH = 200;

	let pathSegments = [];
	let fromX, fromY, toX, toY;
	let fromDirection, toDirection; // Track which direction the connection goes

	fromY = fromInfo.y;
	toY = toInfo.y;

	// Check if this is a self-referential relationship
	if (fromEntityName === toEntityName) {
		console.log(`🔄 Self-referential relationship detected: ${fromEntityName} -> ${fromEntityName}`);
		
		fromX = fromInfo.leftEdge;
		toX = toInfo.leftEdge;
		fromDirection = 'left';
		toDirection = 'left';
		
		const loopLeft = fromInfo.leftEdge - SELF_REF_LOOP_WIDTH;
		
		pathSegments = [
			`M ${fromX} ${fromY}`,
			`L ${loopLeft} ${fromY}`,
			`L ${loopLeft} ${toY}`,
			`L ${toX} ${toY}`
		];
		
		console.log(`🔄 Self-referential loop`);
	} else if (fromInfo.centerX < toInfo.centerX) {
		// LEFT->RIGHT
		fromX = fromInfo.rightEdge;
		toX = toInfo.leftEdge;
		fromDirection = 'right';
		toDirection = 'left';
		
		const verticalSeparation = Math.abs(fromY - toY);
		const horizontalSeparation = toX - fromX;
		
		if (verticalSeparation > 250) {
			const minHorizontalFirst = Math.max(MARKER_OFFSET, horizontalSeparation * 0.4);
			const midX = fromX + minHorizontalFirst;
			const entryX = toX - MARKER_OFFSET;
			
			pathSegments = [
				`M ${fromX} ${fromY}`,
				`L ${midX} ${fromY}`,
				`L ${midX} ${toY}`,
				`L ${entryX} ${toY}`,
				`L ${toX} ${toY}`
			];
		} else {
			const exitX = fromX + MARKER_OFFSET;
			const entryX = toX - MARKER_OFFSET;
			
			pathSegments = [
				`M ${fromX} ${fromY}`,
				`L ${exitX} ${fromY}`,
				`L ${exitX} ${toY}`,
				`L ${entryX} ${toY}`,
				`L ${toX} ${toY}`
			];
		}
	} else {
		// RIGHT->LEFT
		fromX = fromInfo.leftEdge;
		toX = toInfo.rightEdge;
		fromDirection = 'left';
		toDirection = 'right';
		
		const verticalSeparation = Math.abs(fromY - toY);
		const horizontalSeparation = fromX - toX;
		
		if (verticalSeparation > 250) {
			const minHorizontalFirst = Math.max(MARKER_OFFSET, horizontalSeparation * 0.4);
			const midX = fromX - minHorizontalFirst;
			const entryX = toX + MARKER_OFFSET;
			
			pathSegments = [
				`M ${fromX} ${fromY}`,
				`L ${midX} ${fromY}`,
				`L ${midX} ${toY}`,
				`L ${entryX} ${toY}`,
				`L ${toX} ${toY}`
			];
		} else {
			const exitX = fromX - MARKER_OFFSET;
			const entryX = toX + MARKER_OFFSET;
			
			pathSegments = [
				`M ${fromX} ${fromY}`,
				`L ${exitX} ${fromY}`,
				`L ${exitX} ${toY}`,
				`L ${entryX} ${toY}`,
				`L ${toX} ${toY}`
			];
		}
	}

	const pathData = pathSegments.join(' ');

	// Create the main line WITHOUT markers
	const line = document.createElementNS('http://www.w3.org/2000/svg', 'path');
	line.setAttribute('d', pathData);
	line.classList.add('relationship-line');
	line.setAttribute('data-from', fromEntityName);
	line.setAttribute('data-to', toEntityName);
	line.setAttribute('data-property', propertyName);
	line.setAttribute('data-relationship-type', relationship.type);
	line.setAttribute('data-self-referential', fromEntityName === toEntityName ? 'true' : 'false');

	// Insert line BEFORE any table groups to ensure it appears behind tables
	const firstTableGroup = svg.querySelector('.table-group');
	if (firstTableGroup) {
		svg.insertBefore(line, firstTableGroup);
	} else {
		svg.appendChild(line);
	}

	// Draw crow's foot notation as explicit SVG elements and track them
	const fromCrowsFoot = drawCrowsFootNotation(svg, fromX, fromY, fromDirection, relationship, true); // Start point (many side)
	const toCrowsFoot = drawCrowsFootNotation(svg, toX, toY, toDirection, relationship, false); // End point (one side)
	
	// Add data attributes to crow's feet to track which relationship they belong to
	fromCrowsFoot.setAttribute('data-from', fromEntityName);
	fromCrowsFoot.setAttribute('data-to', toEntityName);
	fromCrowsFoot.setAttribute('data-property', propertyName);
	
	toCrowsFoot.setAttribute('data-from', fromEntityName);
	toCrowsFoot.setAttribute('data-to', toEntityName);
	toCrowsFoot.setAttribute('data-property', propertyName);

	console.log(`🐾 Crow's Foot line created with explicit notation`);
	console.log(`   Type: ${relationship.type}, From: ${fromDirection}, To: ${toDirection}`);
}

function drawCrowsFootNotation(svg, x, y, direction, relationship, isFromSide) {
	const CROW_SIZE = 35; // Size of crow's foot
	const ONE_SIDE_OFFSET = 20; // Offset from table edge for "one" side notation
	const CIRCLE_RADIUS = 10; // Radius for optional indicator
	const CIRCLE_OFFSET = -20; // CHANGED: Negative offset to place circle BEFORE the crow's foot (closer to table)
	
	// Use the same blue color as relationship lines for consistency
	const color = '#2563eb'; // Blue color matching relationship lines
	
	// Determine what notation to draw based on relationship type and side
	let notationType;
	if (isFromSide) {
		// From side (FK table) = many side
		notationType = relationship.fromOptional ? 'many-optional' : 'many';
	} else {
		// To side (PK table) = one side
		notationType = relationship.toOptional ? 'one-optional' : 'one';
	}
	
	console.log(`🐾 Drawing crow's foot: ${notationType} at (${x}, ${y}) facing ${direction}`);
	
	// Calculate rotation angle based on direction
	let angle = 0;
	switch (direction) {
		case 'right':
			angle = 0;
			break;
		case 'left':
			angle = 180;
			break;
		case 'up':
			angle = -90;
			break;
		case 'down':
			angle = 90;
			break;
	}
	
	// Create a group for the notation
	const group = document.createElementNS('http://www.w3.org/2000/svg', 'g');
	group.setAttribute('transform', `translate(${x}, ${y}) rotate(${angle})`);
	group.classList.add('crows-foot-notation');
	
	// Draw notation based on type
	if (notationType.includes('many')) {
		// Draw circle for optional relationships FIRST (positioned between table and crow's foot)
		if (notationType.includes('optional')) {
			const circle = document.createElementNS('http://www.w3.org/2000/svg', 'circle');
			circle.setAttribute('cx', CIRCLE_OFFSET);
			circle.setAttribute('cy', '0');
			circle.setAttribute('r', CIRCLE_RADIUS);
			circle.setAttribute('stroke', color);
			circle.setAttribute('stroke-width', '5');
			circle.setAttribute('fill', 'none'); // FIXED: Transparent fill so circle is visible
			group.appendChild(circle);
			console.log(`⭕ Drew optional circle for many side at cx=${CIRCLE_OFFSET}`);
		}
		
		// Draw crow's foot as a path with two segments forming a > shape
		// The path should point AWAY from the table (to the right in unrotated state)
		// So: toe at x=0, legs extend to the right and up/down
		const crowPath = document.createElementNS('http://www.w3.org/2000/svg', 'path');
		const pathData = `M 0,${-CROW_SIZE} L ${CROW_SIZE},0 L 0,${CROW_SIZE}`;
		crowPath.setAttribute('d', pathData);
		crowPath.setAttribute('stroke', color);
		crowPath.setAttribute('stroke-width', '8');
		crowPath.setAttribute('fill', 'none');
		crowPath.setAttribute('stroke-linejoin', 'miter');
		group.appendChild(crowPath);
	} else {
		// Draw circle for optional relationships FIRST
		if (notationType.includes('optional')) {
			const circle = document.createElementNS('http://www.w3.org/2000/svg', 'circle');
			circle.setAttribute('cx', 0);
			circle.setAttribute('cy', '0');
			circle.setAttribute('r', CIRCLE_RADIUS);
			circle.setAttribute('stroke', color);
			circle.setAttribute('stroke-width', '5');
			circle.setAttribute('fill', 'none'); // FIXED: Transparent fill so circle is visible
			group.appendChild(circle);
			console.log(`⭕ Drew optional circle for one side at cx=0`);
		}
		
		// One side - vertical line with offset from table edge (drawn AFTER circle so it appears in front)
		const verticalLine = document.createElementNS('http://www.w3.org/2000/svg', 'line');
		verticalLine.setAttribute('x1', ONE_SIDE_OFFSET);
		verticalLine.setAttribute('y1', -CROW_SIZE);
		verticalLine.setAttribute('x2', ONE_SIDE_OFFSET);
		verticalLine.setAttribute('y2', CROW_SIZE);
		verticalLine.setAttribute('stroke', color);
		verticalLine.setAttribute('stroke-width', '8');
		group.appendChild(verticalLine);
	}
	
	// Insert crow's foot BEFORE tables (at the beginning of SVG) to ensure proper z-indexing
	const firstTableGroup = svg.querySelector('.table-group');
	if (firstTableGroup) {
		svg.insertBefore(group, firstTableGroup);
	} else {
		svg.appendChild(group);
	}
	
	return group; // Return the group so we can track it with the relationship line
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

	// Check if FK is nullable (optional relationship on the "many" side)
	// FIXED: Only check type string for '?' - PropertyInfo doesn't have a nullable property
	const isNullable = fkProp && fkProp.type.includes('?');

	// Look for navigation properties to determine relationship type
	const allToProps = [...(toEntity.properties || []), ...(toEntity.inheritedProperties || [])];

	// Check if target entity has a collection navigation property back to source
	const navPropName = fromEntityName.replace(/Model$/, ''); // Remove 'Model' suffix for collection name
	const collectionNavProp = allToProps.find(p =>
		p.type.includes(`ICollection<${fromEntityName}>`) ||
		p.type.includes(`List<${fromEntityName}>`) ||
		p.name.toLowerCase().includes(navPropName.toLowerCase() + 's') // Plural form
	);

	// Check if source entity has a navigation property to target
	const sourceNavPropName = toEntityName.replace(/Model$/, '');
	const sourceNavProp = allFromProps.find(p =>
		p.type === toEntityName ||
		p.type === `${toEntityName}?` || // Nullable navigation property
		p.name === sourceNavPropName
	);

	// Check if the navigation property on the source is nullable (indicates optional on "one" side)
	// FIXED: Only check type string for '?' - PropertyInfo doesn't have a nullable property
	const sourceNavPropIsNullable = sourceNavProp && sourceNavProp.type.includes('?');

	// Determine relationship type based on analysis
	let relationshipType;
	let toOptional = false; // Whether the "one" side is optional (0..1)

	if (collectionNavProp && sourceNavProp) {
		// Both sides have navigation properties - this is 1:N (most common)
		relationshipType = '1:N';
		// The "one" side is optional if EITHER the FK is nullable OR the navigation property is nullable
		toOptional = isNullable || sourceNavPropIsNullable;
	} else if (sourceNavProp && !collectionNavProp) {
		// Only source has nav prop, no collection on target - could be 1:1 or 1:0..1
		relationshipType = (isNullable || sourceNavPropIsNullable) ? '1:0..1' : '1:1';
		toOptional = isNullable || sourceNavPropIsNullable; // The "one" side is optional if either is nullable
	} else if (collectionNavProp) {
		// Target has collection - definitely 1:N
		relationshipType = '1:N';
		toOptional = isNullable; // The "one" side is optional if FK is nullable
	} else {
		// No navigation properties found - assume 1:N (most common for FK relationships)
		relationshipType = '1:N';
		toOptional = isNullable; // The "one" side is optional if FK is nullable
	}

	console.log(`📊 Relationship analysis for ${fromEntityName}.${propertyName} -> ${toEntityName}:`);
	console.log(`   FK nullable: ${isNullable}`);
	console.log(`   Collection nav prop: ${collectionNavProp ? collectionNavProp.name : 'none'}`);
	console.log(`   Source nav prop: ${sourceNavProp ? sourceNavProp.name : 'none'}`);
	console.log(`   Source nav prop nullable: ${sourceNavPropIsNullable}`);
	console.log(`   Determined type: ${relationshipType}`);
	console.log(`   From optional (many side): ${isNullable}`);
	console.log(`   To optional (one side): ${toOptional}`);

	return {
		type: relationshipType,
		fromOptional: isNullable, // FK side optionality (many side)
		toOptional: toOptional // PK side optionality (one side)
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
			// One-to-zero-or-one: FK side has circle+line (optional), PK side has line
			fromSide = relationship.fromOptional ? 'url(#one-optional-side)' : 'url(#one-side)';
			toSide = 'url(#one-side)';
			break;

		case '1:N':
		default:
			// For One-to-Many relationships:
			// - FROM side (FK table) = "many" side = needs crow's foot
			// - TO side (PK table) = "one" side = needs single line (optionally with circle if nullable)
			fromSide = relationship.fromOptional ? 'url(#many-optional-side)' : 'url(#many-side)';
			toSide = 'url(#one-side)';
			break;
	}

	return { fromSide, toSide };
}

function getCrowsFootMarkersHighlighted(relationship) {
	// Highlighted markers (purple) - same logic but with highlighted versions
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
	const crowsFeet = document.querySelectorAll('.crows-foot-notation');
	
	console.log(`🔄 Updating ${lines.length} relationship lines and ${crowsFeet.length} crow's feet`);

	// Remove all existing crow's feet
	crowsFeet.forEach(cf => cf.remove());

	lines.forEach(line => {
		const from = line.getAttribute('data-from');
		const to = line.getAttribute('data-to');
		const propertyName = line.getAttribute('data-property');
		const relationshipType = line.getAttribute('data-relationship-type') || '1:N';
		const isSelfReferential = line.getAttribute('data-self-referential') === 'true';

		// Re-analyze the relationship to get proper optional indicators
		const relationship = analyzeRelationshipCardinality(from, to, propertyName);

		// Get updated connection points
		const fromInfo = getTableConnectionPoint(from, propertyName, null);
		const toInfo = getTableConnectionPoint(to, 'Id', null);

		if (fromInfo && toInfo) {
			// Recreate the routing with same logic as creation
			const MARKER_OFFSET = 50;
			const SELF_REF_LOOP_WIDTH = 200;
			
			let fromX, fromY, toX, toY;
			let fromDirection, toDirection;
			let pathSegments = [];

			fromY = fromInfo.y;
			toY = toInfo.y;

			if (isSelfReferential || from === to) {
				// Self-referential
				fromX = fromInfo.leftEdge;
				toX = toInfo.leftEdge;
				fromDirection = 'left';
				toDirection = 'left';
				
				const loopLeft = fromInfo.leftEdge - SELF_REF_LOOP_WIDTH;
				
				pathSegments = [
					`M ${fromX} ${fromY}`,
					`L ${loopLeft} ${fromY}`,
					`L ${loopLeft} ${toY}`,
					`L ${toX} ${toY}`
				];
			} else if (fromInfo.centerX < toInfo.centerX) {
				// LEFT->RIGHT
				fromX = fromInfo.rightEdge;
				toX = toInfo.leftEdge;
				fromDirection = 'right';
				toDirection = 'left';
				
				const verticalSeparation = Math.abs(fromY - toY);
				const horizontalSeparation = toX - fromX;
				
				if (verticalSeparation > 250) {
					const minHorizontalFirst = Math.max(MARKER_OFFSET, horizontalSeparation * 0.4);
					const midX = fromX + minHorizontalFirst;
					const entryX = toX - MARKER_OFFSET;
					
					pathSegments = [
						`M ${fromX} ${fromY}`,
						`L ${midX} ${fromY}`,
						`L ${midX} ${toY}`,
						`L ${entryX} ${toY}`,
						`L ${toX} ${toY}`
					];
				} else {
					const exitX = fromX + MARKER_OFFSET;
					const entryX = toX - MARKER_OFFSET;
					
					pathSegments = [
						`M ${fromX} ${fromY}`,
						`L ${exitX} ${fromY}`,
						`L ${exitX} ${toY}`,
						`L ${entryX} ${toY}`,
						`L ${toX} ${toY}`
					];
				}
			} else {
				// RIGHT->LEFT
				fromX = fromInfo.leftEdge;
				toX = toInfo.rightEdge;
				fromDirection = 'left';
				toDirection = 'right';
				
				const verticalSeparation = Math.abs(fromY - toY);
				const horizontalSeparation = fromX - toX;
				
				if (verticalSeparation > 250) {
					const minHorizontalFirst = Math.max(MARKER_OFFSET, horizontalSeparation * 0.4);
					const midX = fromX - minHorizontalFirst;
					const entryX = toX + MARKER_OFFSET;
					
					pathSegments = [
						`M ${fromX} ${fromY}`,
						`L ${midX} ${fromY}`,
						`L ${midX} ${toY}`,
						`L ${entryX} ${toY}`,
						`L ${toX} ${toY}`
					];
				} else {
					const exitX = fromX - MARKER_OFFSET;
					const entryX = toX + MARKER_OFFSET;
					
					pathSegments = [
						`M ${fromX} ${fromY}`,
						`L ${exitX} ${fromY}`,
						`L ${exitX} ${toY}`,
						`L ${entryX} ${toY}`,
						`L ${toX} ${toY}`
					];
				}
			}

			const pathData = pathSegments.join(' ');
			line.setAttribute('d', pathData);
			
			// Recreate crow's feet at new positions with proper relationship analysis
			const svg = document.getElementById('schema-svg');
			const fromCrowsFoot = drawCrowsFootNotation(svg, fromX, fromY, fromDirection, relationship, true);
			const toCrowsFoot = drawCrowsFootNotation(svg, toX, toY, toDirection, relationship, false);
			
			// Add data attributes to track crow's feet
			fromCrowsFoot.setAttribute('data-from', from);
			fromCrowsFoot.setAttribute('data-to', to);
			fromCrowsFoot.setAttribute('data-property', propertyName);
			
			toCrowsFoot.setAttribute('data-from', from);
			toCrowsFoot.setAttribute('data-to', to);
			toCrowsFoot.setAttribute('data-property', propertyName);
			
			// Apply same styling to crow's feet as the relationship line
			const lineClasses = Array.from(line.classList);
			if (lineClasses.includes('highlighted')) {
				fromCrowsFoot.classList.add('highlighted');
				toCrowsFoot.classList.add('highlighted');
			} else if (lineClasses.includes('dimmed')) {
				fromCrowsFoot.classList.add('dimmed');
				toCrowsFoot.classList.add('dimmed');
			}
		}

		line.classList.remove('highlighted', 'dimmed', 'hidden');

		if (!showRelationships) {
			line.style.display = 'none';
		} else {
			line.style.display = 'block';

			if (showOnlySelectedRelations && selectedTable) {
				if (from === selectedTable || to === selectedTable) {
					line.classList.add('highlighted');
				} else {
					line.classList.add('dimmed');
				}
			}
		}
	});
	
	// Update crow's feet visibility to match relationship lines
	document.querySelectorAll('.crows-foot-notation').forEach(cf => {
		const from = cf.getAttribute('data-from');
		const to = cf.getAttribute('data-to');
		
		if (!showRelationships) {
			cf.style.display = 'none';
		} else {
			cf.style.display = 'block';
			
			// Apply highlighting/dimming based on selected table
			if (showOnlySelectedRelations && selectedTable) {
				if (from === selectedTable || to === selectedTable) {
					cf.classList.add('highlighted');
					cf.classList.remove('dimmed');
				} else {
					cf.classList.add('dimmed');
					cf.classList.remove('highlighted');
				}
			} else {
				cf.classList.remove('highlighted', 'dimmed');
			}
		}
	});
}