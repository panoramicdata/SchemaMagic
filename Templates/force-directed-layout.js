// ========================================
// FORCE-DIRECTED LAYOUT ALGORITHM
// Auto-optimization only when no saved document state exists
// Respects user positioning when document has been previously customized
// ========================================

// Global variables for force-directed layout
let layoutOptimizationRunning = false;

// Force-directed layout configuration
const FORCE_CONFIG = {
	// Phase 1: Topology establishment (connection forces dominate)
	phase1: {
		connectionStrength: 1.0,          // Full strength attractive forces
		separationStrength: 0.04,         // Very weak repulsive forces
		idealEdgeLength: 700,             // Increased from 280 to 700 (2.5x larger)
		maxIterations: 80,                // Let topology settle quickly
		coolingRate: 0.96,                // Moderate cooling
		initialTemperature: 100,          // Moderate initial energy
		minTemperature: 1.0,              // Allow more movement
		damping: 0.85,                    // Higher damping for stability
		maxVelocity: 88,                  // Increased from 35 to 88 (2.5x larger)
		convergenceThreshold: 5.0,        // Increased from 2.0 to 5.0 (2.5x larger)
	},
	
	// Phase 2: Spacing refinement (gradual separation increase)
	phase2: {
		connectionStrength: 1.0,          // Maintain connection forces
		separationBaseStrength: 0.04,     // Starting separation strength (same as phase 1 end)
		separationRampRate: 1.08,         // Faster exponential increase (8% per iteration)
		separationMaxMultiplier: 40.0,    // Very high maximum separation strength
		idealEdgeLength: 700,             // Increased from 280 to 700 (2.5x larger)
		maxIterations: 200,               // More iterations for proper separation
		coolingRate: 0.98,                // Slower cooling to allow more movement
		initialTemperature: 80,           // Lower initial energy
		minTemperature: 0.2,              // Tight convergence
		damping: 0.65,                    // Lower damping for more movement
		maxVelocity: 175,                 // Increased from 70 to 175 (2.5x larger)
		convergenceThreshold: 0.5,        // Increased from 0.2 to 0.5 (2.5x larger)
	},
	
	// Phase 3: Aggressive separation enforcement (overlap elimination)
	phase3: {
		connectionStrength: 0.6,          // Reduce connection forces significantly
		separationStrength: 12.0,         // VERY strong separation forces
		maxIterations: 300,               // More iterations for thorough separation
		coolingRate: 0.99,                // Very slow cooling
		initialTemperature: 100,          // High energy for aggressive movement
		minTemperature: 0.05,             // Very tight convergence
		damping: 0.55,                    // Low damping for strong movement
		maxVelocity: 300,                 // Increased from 120 to 300 (2.5x larger)
		convergenceThreshold: 0.125,      // Increased from 0.05 to 0.125 (2.5x larger)
		overlapTolerance: 50,             // Increased from 20 to 50 (2.5x larger)
		temperatureResetFactor: 0.8,      // Factor for temperature reset
		maxTemperatureResets: 5,          // Maximum number of temperature resets
	},
	
	// Elliptical force field parameters
	ellipse: {
		minTableDistance: 200,            // Increased from 80 to 200 (2.5x larger)
		collisionMultiplier: 15.0,        // Extremely strong repulsion for overlapping tables
		forceRampDistance: 2.5,           // Larger distance multiplier for force ramp-up
		smoothingFactor: 0.1,             // Smoothing for ellipse approximation
		separationForceMultiplier: 3.0,   // Much stronger additional multiplier for separation phase
		phase3ForceMultiplier: 5.0,       // Extra multiplier specifically for phase 3
	},
	
	// Unconnected tables layout
	unconnectedRowHeight: 375,            // Increased from 150 to 375 (2.5x larger)
	unconnectedSpacing: 750,              // Increased from 300 to 750 (2.5x larger)
	unconnectedStartY: 200,               // Increased from 80 to 200 (2.5x larger)
	boundaryPadding: 300,                 // Increased from 120 to 300 (2.5x larger)
};

/**
 * Enhanced table position calculation with document-aware optimization
 * Only auto-optimizes if no saved document state exists
 */
function calculateTablePositionsWithForces() {
	const entityNames = Object.keys(entities);
	
	// Check if this specific document has saved state
	if (hasSavedDocumentState()) {
		console.log(`📄 Document ${DOCUMENT_GUID}: Using saved positions (no auto-layout)`);
		return convertSavedToStandardFormat(loadSavedDocumentPositions(), entityNames);
	}
	
	// Only auto-optimize for new/unsaved documents
	console.log(`🎯 Document ${DOCUMENT_GUID}: No saved state found, auto-optimizing layout...`);
	return optimizeLayoutWithThreePhaseForces(entityNames, null);
}

/**
 * Load saved positions for this specific document
 */
function loadSavedDocumentPositions() {
	try {
		const saved = localStorage.getItem(STORAGE_KEYS.tablePositions);
		if (saved) {
			return JSON.parse(saved);
		}
	} catch (e) {
		console.warn('Failed to load saved document positions:', e);
	}
	return null;
}

/**
 * Convert saved positions to standard format
 */
function convertSavedToStandardFormat(savedPositions, entityNames) {
	const positions = {};
	
	entityNames.forEach(entityName => {
		if (savedPositions && savedPositions[entityName]) {
			let x = savedPositions[entityName].x;
			let y = savedPositions[entityName].y;
			
			// Snap to grid if enabled
			if (snapToGrid) {
				x = Math.round(x / 50) * 50; // Increased from 20 to 50 (2.5x larger)
				y = Math.round(y / 50) * 50; // Increased from 20 to 50 (2.5x larger)
			}
			
			positions[entityName] = { x, y };
		} else {
			// Default position for new entities
			let x = 500; // Increased from 200 to 500 (2.5x larger)
			let y = 500; // Increased from 200 to 500 (2.5x larger)
			if (snapToGrid) {
				x = Math.round(x / 50) * 50; // Increased from 20 to 50 (2.5x larger)
				y = Math.round(y / 50) * 50; // Increased from 20 to 50 (2.5x larger)
			}
			positions[entityName] = { x, y };
		}
	});
	
	return positions;
}

/**
 * Main three-phase force-directed layout optimization function
 * Only called for documents without saved state
 */
function optimizeLayoutWithThreePhaseForces(entityNames, savedPositions) {
	const relationships = extractRelationships();
	
	// Separate connected and unconnected entities
	const { connectedEntities, unconnectedEntities } = separateConnectedEntities(entityNames, relationships);
	
	console.log(`🚀 Auto-optimizing new document: ${connectedEntities.length} connected, ${unconnectedEntities.length} unconnected entities`);
	
	// Step 1: Layout unconnected entities at the top
	const unconnectedPositions = layoutUnconnectedEntities(unconnectedEntities);
	
	// Step 2: Initialize positions for connected entities
	const initialPositions = generateSmartInitialLayout(connectedEntities, relationships);
	
	// Step 3: Phase 1 - Establish optimal topology with weak separation
	console.log('📍 Phase 1: Establishing topology with connection-dominated forces...');
	const phase1Positions = applyPhase1Optimization(initialPositions, relationships);
	
	// Brief cooling period between phases
	console.log('❄️ Brief cooling period between phases...');
	coolDownPositions(phase1Positions);
	
	// Step 4: Phase 2 - Gradual separation increase while preserving topology
	console.log('📏 Phase 2: Gradually increasing separation while preserving topology...');
	const phase2Positions = applyPhase2Optimization(phase1Positions, relationships);
	
	// Brief cooling period between phases
	console.log('❄️ Brief cooling period before aggressive separation enforcement...');
	coolDownPositions(phase2Positions);
	
	// Step 5: Phase 3 - Aggressively enforce separation until no overlaps remain
	console.log('🚫 Phase 3: AGGRESSIVELY enforcing separation to eliminate ALL table overlaps...');
	const finalPositions = applyPhase3AggressiveSeparationEnforcement(phase2Positions, relationships);
	
	// Step 6: Combine all positions
	const allPositions = { ...unconnectedPositions, ...finalPositions };
	
	// Step 7: Convert back to the expected format
	return convertToStandardFormat(allPositions);
}

/**
 * Separate entities into connected and unconnected groups
 */
function separateConnectedEntities(entityNames, relationships) {
	const connectedSet = new Set();
	
	// Add all entities that appear in relationships
	relationships.forEach(rel => {
		connectedSet.add(rel.from);
		connectedSet.add(rel.to);
	});
	
	const connectedEntities = entityNames.filter(name => connectedSet.has(name));
	const unconnectedEntities = entityNames.filter(name => !connectedSet.has(name));
	
	// Sort unconnected entities alphabetically
	unconnectedEntities.sort();
	
	return { connectedEntities, unconnectedEntities };
}

/**
 * Layout unconnected entities in a row at the top, alphabetically
 */
function layoutUnconnectedEntities(unconnectedEntities) {
	const positions = {};
	
	if (unconnectedEntities.length === 0) return positions;
	
	// Calculate starting X position to center the row
	const totalWidth = (unconnectedEntities.length - 1) * FORCE_CONFIG.unconnectedSpacing;
	const startX = Math.max(FORCE_CONFIG.boundaryPadding, (CANVAS_WIDTH - totalWidth) / 2);
	
	unconnectedEntities.forEach((entityName, index) => {
		positions[entityName] = {
			x: startX + (index * FORCE_CONFIG.unconnectedSpacing),
			y: FORCE_CONFIG.unconnectedStartY,
			vx: 0,
			vy: 0
		};
	});
	
	console.log(`📋 Positioned ${unconnectedEntities.length} unconnected entities at top in alphabetical order`);
	return positions;
}

/**
 * Generate smart initial layout based on relationship analysis
 * Places highly connected nodes near center, isolated nodes at periphery
 */
function generateSmartInitialLayout(entityNames, relationships) {
	const positions = {};
	
	if (entityNames.length === 0) return positions;
	
	// Calculate node connectivity (centrality measure)
	const connectivity = calculateNodeConnectivity(entityNames, relationships);
	
	// Sort entities by connectivity (most connected first)
	const sortedEntities = entityNames.sort((a, b) => connectivity[b] - connectivity[a]);
	
	// Place nodes in expanding rings based on connectivity
	// Start below the unconnected entities row
	const centerY = FORCE_CONFIG.unconnectedRowHeight + 750; // Increased from 300 to 750 (2.5x larger)
	
	sortedEntities.forEach((entityName, index) => {
		const angle = (index * Math.PI * 2) / entityNames.length;
		const radius = Math.min(500 + (index * 75), Math.min(CANVAS_WIDTH, CANVAS_HEIGHT) / 4); // Increased from 200+30 to 500+75 (2.5x larger)
		
		positions[entityName] = {
			x: CANVAS_WIDTH / 2 + Math.cos(angle) * radius + (Math.random() - 0.5) * 200, // Increased from 80 to 200 (2.5x larger)
			y: centerY + Math.sin(angle) * radius + (Math.random() - 0.5) * 200, // Increased from 80 to 200 (2.5x larger)
			vx: 0,
			vy: 0
		};
	});
	
	console.log(`📊 Initial layout: ${sortedEntities.length} connected entities arranged by connectivity`);
	return positions;
}

/**
 * Calculate node connectivity for centrality-based initial placement
 */
function calculateNodeConnectivity(entityNames, relationships) {
	const connectivity = {};
	
	// Initialize connectivity counts
	entityNames.forEach(name => connectivity[name] = 0);
	
	// Count incoming and outgoing relationships
	relationships.forEach(rel => {
		connectivity[rel.from] = (connectivity[rel.from] || 0) + 1;
		connectivity[rel.to] = (connectivity[rel.to] || 0) + 1;
	});
	
	return connectivity;
}

/**
 * Extract relationships from entity data for force calculations
 */
function extractRelationships() {
	const relationships = [];
	
	Object.values(entities).forEach(entity => {
		const fromEntityName = entity.type;
		entity.properties.forEach(prop => {
			if (prop.isForeignKey) {
				// Smart target entity detection with multiple fallback strategies
				let targetEntityName = findTargetEntity(prop.name);
				
				if (targetEntityName && entities[targetEntityName]) {
					relationships.push({
						from: fromEntityName,
						to: targetEntityName,
						property: prop.name,
						strength: 1.0,
						type: 'FK'
					});
				}
			}
		});
	});
	
	console.log(`🔗 Extracted ${relationships.length} relationships from ${Object.keys(entities).length} entities`);
	return relationships;
}

/**
 * Enhanced target entity detection with multiple strategies
 */
function findTargetEntity(foreignKeyName) {
	// Strategy 1: Remove 'Id' suffix and try exact match
	let targetName = foreignKeyName.replace(/Id$/, '');
	if (entities[targetName]) return targetName;
	
	// Strategy 2: Try with 'Model' suffix
	if (entities[targetName + 'Model']) return targetName + 'Model';
	
	// Strategy 3: Try with 'Entity' suffix  
	if (entities[targetName + 'Entity']) return targetName + 'Entity';
	
	// Strategy 4: Search for partial matches
	const entityNames = Object.keys(entities);
	const partialMatch = entityNames.find(name => 
		name.toLowerCase().includes(targetName.toLowerCase()) ||
		targetName.toLowerCase().includes(name.toLowerCase().replace(/model|entity$/i, ''))
	);
	
	return partialMatch || null;
}

/**
 * Phase 1: Establish topology with connection-dominated forces
 * Weak separation allows connection forces to dominate and establish optimal grouping
 */
function applyPhase1Optimization(positions, relationships) {
	let temperature = FORCE_CONFIG.phase1.initialTemperature;
	const entityNames = Object.keys(positions);
	const phaseConfig = FORCE_CONFIG.phase1;
	
	if (entityNames.length === 0) return positions;
	
	console.log('🎯 Phase 1: Connection forces dominate to establish topology');
	
	for (let iteration = 0; iteration < phaseConfig.maxIterations; iteration++) {
		// Calculate all forces for this iteration (Phase 1 configuration)
		const forces = calculatePhase1Forces(positions, relationships, entityNames, phaseConfig);
		
		// Apply forces with temperature scaling and velocity damping
		const totalMovement = applyForcesWithDamping(positions, forces, temperature, entityNames, phaseConfig);
		
		// Cool the system (simulated annealing)
		temperature *= phaseConfig.coolingRate;
		
		// Progress logging
		if (iteration % 25 === 0) {
			console.log(`📈 Phase 1 - Iteration ${iteration}: temp=${temperature.toFixed(2)}, movement=${totalMovement.toFixed(2)}`);
		}
		
		// Early termination conditions
		if (totalMovement < phaseConfig.convergenceThreshold || temperature < phaseConfig.minTemperature) {
			console.log(`✅ Phase 1 converged after ${iteration} iterations (movement: ${totalMovement.toFixed(2)})`);
			break;
		}
	}
	
	console.log('🏁 Phase 1 complete: Topology established with optimal connection grouping');
	return positions;
}

/**
 * Brief cooling period between phases to settle velocities
 */
function coolDownPositions(positions) {
	// Reset velocities to zero for clean phase start
	Object.values(positions).forEach(pos => {
		pos.vx = pos.vx * 0.1; // Reduce velocity by 90%
		pos.vy = pos.vy * 0.1;
	});
}

/**
 * Phase 2: Gradually increase separation while preserving topology
 * Exponentially ramp up separation forces while maintaining connection forces
 */
function applyPhase2Optimization(positions, relationships) {
	let temperature = FORCE_CONFIG.phase2.initialTemperature;
	let separationMultiplier = 1.0; // Start at same strength as phase 1 end
	const entityNames = Object.keys(positions);
	const phaseConfig = FORCE_CONFIG.phase2;
	
	if (entityNames.length === 0) return positions;
	
	console.log('🎯 Phase 2: Gradually increasing separation while preserving topology');
	
	for (let iteration = 0; iteration < phaseConfig.maxIterations; iteration++) {
		// Gradually ramp up separation strength
		separationMultiplier = Math.min(
			separationMultiplier * phaseConfig.separationRampRate,
			phaseConfig.separationMaxMultiplier
		);
		
		// Calculate all forces for this iteration (Phase 2 configuration)
		const forces = calculatePhase2Forces(positions, relationships, entityNames, phaseConfig, separationMultiplier);
		
		// Apply forces with temperature scaling and velocity damping
		const totalMovement = applyForcesWithDamping(positions, forces, temperature, entityNames, phaseConfig);
		
		// Cool the system (simulated annealing)
		temperature *= phaseConfig.coolingRate;
		
		// Progress logging
		if (iteration % 30 === 0) {
			console.log(`📈 Phase 2 - Iteration ${iteration}: temp=${temperature.toFixed(2)}, separation×${separationMultiplier.toFixed(2)}, movement=${totalMovement.toFixed(2)}`);
		}
		
		// Early termination conditions
		if (totalMovement < phaseConfig.convergenceThreshold || temperature < phaseConfig.minTemperature) {
			console.log(`✅ Phase 2 converged after ${iteration} iterations (movement: ${totalMovement.toFixed(2)}, final separation×${separationMultiplier.toFixed(2)})`);
			break;
		}
	}
	
	console.log('🏁 Phase 2 complete: Separation forces gradually increased while preserving topology');
	return positions;
}

/**
 * Phase 3: AGGRESSIVE separation enforcement - eliminate ALL table overlaps
 * Will not stop until zero overlaps remain, with temperature resets as needed
 */
function applyPhase3AggressiveSeparationEnforcement(positions, relationships) {
	let temperature = FORCE_CONFIG.phase3.initialTemperature;
	let temperatureResets = 0;
	const entityNames = Object.keys(positions);
	const phaseConfig = FORCE_CONFIG.phase3;
	
	if (entityNames.length === 0) return positions;
	
	console.log('🎯 Phase 3: AGGRESSIVELY enforcing separation to eliminate ALL overlaps');
	
	for (let iteration = 0; iteration < phaseConfig.maxIterations; iteration++) {
		// Check for overlaps before calculating forces
		const overlapCount = countTableOverlaps(positions, entityNames);
		
		// Calculate forces focused on AGGRESSIVE separation
		const forces = calculatePhase3AggressiveForces(positions, relationships, entityNames, phaseConfig);
		
		// Apply forces with temperature scaling and velocity damping
		const totalMovement = applyForcesWithDamping(positions, forces, temperature, entityNames, phaseConfig);
		
		// Cool the system (simulated annealing)
		temperature *= phaseConfig.coolingRate;
		
		// Progress logging
		if (iteration % 20 === 0) {
			console.log(`📈 Phase 3 - Iteration ${iteration}: temp=${temperature.toFixed(2)}, overlaps=${overlapCount}, movement=${totalMovement.toFixed(2)}`);
		}
		
		// Check if we've eliminated all overlaps
		if (overlapCount === 0 && totalMovement < phaseConfig.convergenceThreshold) {
			console.log(`✅ Phase 3 SUCCESS: No overlaps remaining after ${iteration} iterations (movement: ${totalMovement.toFixed(2)})`);
			break;
		}
		
		// AGGRESSIVE: Continue if overlaps remain, even if movement is low
		if (overlapCount > 0 && temperature < phaseConfig.minTemperature && temperatureResets < phaseConfig.maxTemperatureResets) {
			// Reset temperature more aggressively to continue separation
			temperature = phaseConfig.initialTemperature * phaseConfig.temperatureResetFactor;
			temperatureResets++;
			console.log(`🔥 AGGRESSIVE RESET: ${overlapCount} overlaps remain, resetting temperature (reset #${temperatureResets})...`);
		}
		
		// Last resort: If we've used all resets and still have overlaps, force separation
		if (overlapCount > 0 && iteration > phaseConfig.maxIterations * 0.8) {
			console.log(`💥 FORCING FINAL SEPARATION: ${overlapCount} overlaps, applying maximum forces...`);
			// Apply emergency separation by directly moving overlapping tables apart
			forceEmergencySeparation(positions, entityNames);
		}
	}
	
	// Final overlap check with detailed reporting
	const finalOverlaps = countTableOverlaps(positions, entityNames);
	if (finalOverlaps > 0) {
		console.error(`❌ Phase 3 FAILED: ${finalOverlaps} overlaps remaining after ${phaseConfig.maxIterations} iterations and ${temperatureResets} resets`);
		// Log which tables are still overlapping for debugging
		logOverlappingTables(positions, entityNames);
		// Apply one final emergency separation
		forceEmergencySeparation(positions, entityNames);
	} else {
		console.log(`🎉 Phase 3 COMPLETE SUCCESS: ALL table overlaps eliminated! (${temperatureResets} temperature resets used)`);
	}
	
	// Final boundary enforcement and cleanup
	return enforceBoundaryConstraints(positions);
}

/**
 * Emergency function to force separation of overlapping tables
 */
function forceEmergencySeparation(positions, entityNames) {
	console.log('🚨 Applying EMERGENCY separation to force apart overlapping tables...');
	
	for (let i = 0; i < entityNames.length; i++) {
		for (let j = i + 1; j < entityNames.length; j++) {
			const nodeA = positions[entityNames[i]];
			const nodeB = positions[entityNames[j]];
			const entityNameA = entityNames[i];
			const entityNameB = entityNames[j];
			
			if (areTablesOverlapping(nodeA, nodeB, entityNameA, entityNameB)) {
				// Calculate required separation distance
				const tableA = getTableDimensions(entityNameA);
				const tableB = getTableDimensions(entityNameB);
				const requiredDistance = (tableA.width + tableB.width) / 2 + FORCE_CONFIG.ellipse.minTableDistance;
				
				// Calculate direction vector
				const dx = nodeB.x - nodeA.x;
				const dy = nodeB.y - nodeA.y;
				const currentDistance = Math.sqrt(dx * dx + dy * dy);
				
				if (currentDistance < requiredDistance) {
					// Move them apart by the required distance
					const separationNeeded = requiredDistance - currentDistance + 10; // Extra padding
					const directionX = dx / Math.max(currentDistance, 1);
					const directionY = dy / Math.max(currentDistance, 1);
					
					// Move both tables away from each other
					nodeA.x -= directionX * separationNeeded * 0.5;
					nodeA.y -= directionY * separationNeeded * 0.5;
					nodeB.x += directionX * separationNeeded * 0.5;
					nodeB.y += directionY * separationNeeded * 0.5;
					
					console.log(`💥 EMERGENCY: Separated ${entityNameA} and ${entityNameB} by ${separationNeeded.toFixed(1)}px`);
				}
			}
		}
	}
}

/**
 * Log which tables are still overlapping for debugging
 */
function logOverlappingTables(positions, entityNames) {
	console.log('🔍 DEBUGGING: Overlapping tables:');
	for (let i = 0; i < entityNames.length; i++) {
		for (let j = i + 1; j < entityNames.length; j++) {
			const nodeA = positions[entityNames[i]];
			const nodeB = positions[entityNames[j]];
			const entityNameA = entityNames[i];
			const entityNameB = entityNames[j];
			
			if (areTablesOverlapping(nodeA, nodeB, entityNameA, entityNameB)) {
				const distance = Math.sqrt((nodeB.x - nodeA.x) ** 2 + (nodeB.y - nodeA.y) ** 2);
				console.log(`   - ${entityNameA} <-> ${entityNameB} (distance: ${distance.toFixed(1)}px)`);
			}
		}
	}
}

/**
 * Count the number of overlapping table pairs
 */
function countTableOverlaps(positions, entityNames) {
	let overlapCount = 0;
	
	for (let i = 0; i < entityNames.length; i++) {
		for (let j = i + 1; j < entityNames.length; j++) {
			const nodeA = positions[entityNames[i]];
			const nodeB = positions[entityNames[j]];
			const entityNameA = entityNames[i];
			const entityNameB = entityNames[j];
			
			if (areTablesOverlapping(nodeA, nodeB, entityNameA, entityNameB)) {
				overlapCount++;
			}
		}
	}
	
	return overlapCount;
}

/**
 * Check if two tables are overlapping with increased tolerance
 */
function areTablesOverlapping(nodeA, nodeB, entityNameA, entityNameB) {
	const tableA = getTableDimensions(entityNameA);
	const tableB = getTableDimensions(entityNameB);
	
	// Calculate bounding boxes
	const boxA = {
		left: nodeA.x - tableA.width / 2,
		right: nodeA.x + tableA.width / 2,
		top: nodeA.y - tableA.height / 2,
		bottom: nodeA.y + tableA.height / 2
	};
	
	const boxB = {
		left: nodeB.x - tableB.width / 2,
		right: nodeB.x + tableB.width / 2,
		top: nodeB.y - tableB.height / 2,
		bottom: nodeB.y + tableB.height / 2
	};
	
	// Add tolerance for minimum distance
	const tolerance = FORCE_CONFIG.phase3.overlapTolerance;
	
	// Check for overlap with tolerance
	const overlapping = !(
		boxA.right + tolerance < boxB.left ||
		boxB.right + tolerance < boxA.left ||
		boxA.bottom + tolerance < boxB.top ||
		boxB.bottom + tolerance < boxA.top
	);
	
	return overlapping;
}

/**
 * Calculate Phase 1 forces: Strong connections, weak separation
 */
function calculatePhase1Forces(positions, relationships, entityNames, phaseConfig) {
	const forces = {};
	
	// Initialize force vectors
	entityNames.forEach(name => {
		forces[name] = { x: 0, y: 0 };
	});
	
	// 1. WEAK ELLIPTICAL REPULSIVE FORCES (just enough to prevent overlap)
	for (let i = 0; i < entityNames.length; i++) {
		for (let j = i + 1; j < entityNames.length; j++) {
			const nodeA = entityNames[i];
			const nodeB = entityNames[j];
			const repulsiveForce = calculateEllipticalRepulsiveForce(
				positions[nodeA], 
				positions[nodeB], 
				nodeA, 
				nodeB, 
				phaseConfig.separationStrength,
				1.0 // Normal force multiplier for phase 1
			);
			
			forces[nodeA].x -= repulsiveForce.x;
			forces[nodeA].y -= repulsiveForce.y;
			forces[nodeB].x += repulsiveForce.x;
			forces[nodeB].y += repulsiveForce.y;
		}
	}
	
	// 2. STRONG ATTRACTIVE FORCES (dominate to establish topology)
	relationships.forEach(edge => {
		const attractiveForce = calculateAttractiveForce(
			positions[edge.from], 
			positions[edge.to],
			phaseConfig.connectionStrength,
			phaseConfig.idealEdgeLength
		);
		
		forces[edge.from].x += attractiveForce.x;
		forces[edge.from].y += attractiveForce.y;
		forces[edge.to].x -= attractiveForce.x;
		forces[edge.to].y -= attractiveForce.y;
	});
	
	return forces;
}

/**
 * Calculate Phase 2 forces: Maintain connections, gradually increase separation
 */
function calculatePhase2Forces(positions, relationships, entityNames, phaseConfig, separationMultiplier) {
	const forces = {};
	
	// Initialize force vectors
	entityNames.forEach(name => {
		forces[name] = { x: 0, y: 0 };
	});
	
	// 1. GRADUALLY INCREASING ELLIPTICAL REPULSIVE FORCES
	const currentSeparationStrength = phaseConfig.separationBaseStrength * separationMultiplier;
	
	for (let i = 0; i < entityNames.length; i++) {
		for (let j = i + 1; j < entityNames.length; j++) {
			const nodeA = entityNames[i];
			const nodeB = entityNames[j];
			const repulsiveForce = calculateEllipticalRepulsiveForce(
				positions[nodeA], 
				positions[nodeB], 
				nodeA, 
				nodeB, 
				currentSeparationStrength,
				FORCE_CONFIG.ellipse.separationForceMultiplier // Enhanced force multiplier for phase 2
			);
			
			forces[nodeA].x -= repulsiveForce.x;
			forces[nodeA].y -= repulsiveForce.y;
			forces[nodeB].x += repulsiveForce.x;
			forces[nodeB].y += repulsiveForce.y;
		}
	}
	
	// 2. MAINTAINED ATTRACTIVE FORCES (preserve topology)
	relationships.forEach(edge => {
		const attractiveForce = calculateAttractiveForce(
			positions[edge.from], 
			positions[edge.to],
			phaseConfig.connectionStrength,
			phaseConfig.idealEdgeLength
		);
		
		forces[edge.from].x += attractiveForce.x;
		forces[edge.from].y += attractiveForce.y;
		forces[edge.to].x -= attractiveForce.x;
		forces[edge.to].y -= attractiveForce.y;
	});
	
	return forces;
}

/**
 * Calculate Phase 3 forces: AGGRESSIVE separation enforcement
 */
function calculatePhase3AggressiveForces(positions, relationships, entityNames, phaseConfig) {
	const forces = {};
	
	// Initialize force vectors
	entityNames.forEach(name => {
		forces[name] = { x: 0, y: 0 };
	});
	
	// 1. EXTREMELY STRONG SEPARATION FORCES - eliminate all overlaps aggressively
	for (let i = 0; i < entityNames.length; i++) {
		for (let j = i + 1; j < entityNames.length; j++) {
			const nodeA = entityNames[i];
			const nodeB = entityNames[j];
			const repulsiveForce = calculateEllipticalRepulsiveForce(
				positions[nodeA], 
				positions[nodeB], 
				nodeA, 
				nodeB, 
				phaseConfig.separationStrength,
				FORCE_CONFIG.ellipse.phase3ForceMultiplier // AGGRESSIVE force multiplier for phase 3
			);
			
			forces[nodeA].x -= repulsiveForce.x;
			forces[nodeA].y -= repulsiveForce.y;
			forces[nodeB].x += repulsiveForce.x;
			forces[nodeB].y += repulsiveForce.y;
		}
	}
	
	// 2. REDUCED ATTRACTIVE FORCES (preserve some topology but prioritize separation)
	relationships.forEach(edge => {
		const attractiveForce = calculateAttractiveForce(
			positions[edge.from], 
			positions[edge.to],
			phaseConfig.connectionStrength,
			FORCE_CONFIG.phase2.idealEdgeLength
		);
		
		forces[edge.from].x += attractiveForce.x;
		forces[edge.from].y += attractiveForce.y;
		forces[edge.to].x -= attractiveForce.x;
		forces[edge.to].y -= attractiveForce.y;
	});
	
	return forces;
}

/**
 * Calculate repulsive force using elliptical force fields with AGGRESSIVE parameters
 * Smoother than rectangular collision detection, more natural physics simulation
 */
function calculateEllipticalRepulsiveForce(nodeA, nodeB, entityNameA, entityNameB, separationStrengthMultiplier, forceMultiplier = 1.0) {
	// Get table dimensions for ellipse parameters
	const tableA = getTableDimensions(entityNameA);
	const tableB = getTableDimensions(entityNameB);
	
	// Create ellipse parameters (semi-axes)
	const ellipseA = {
		centerX: nodeA.x,
		centerY: nodeA.y,
		semiMajorAxis: tableA.width / 2,
		semiMinorAxis: tableA.height / 2
	};
	
	const ellipseB = {
		centerX: nodeB.x,
		centerY: nodeB.y,
		semiMajorAxis: tableB.width / 2,
		semiMinorAxis: tableB.height / 2
	};
	
	// Calculate center-to-center distance
	const dx = nodeB.x - nodeA.x;
	const dy = nodeB.y - nodeA.y;
	const centerDistance = Math.sqrt(dx * dx + dy * dy);
	
	if (centerDistance < 1) return { x: 0, y: 0 }; // Avoid division by zero
	
	// Calculate minimum separation distance (ellipse edges + padding)
	const minSeparationDistance = ellipseA.semiMajorAxis + ellipseB.semiMajorAxis + FORCE_CONFIG.ellipse.minTableDistance;
	
	// Calculate distance from ellipse surfaces (approximation)
	const surfaceDistance = Math.max(0.1, centerDistance - (ellipseA.semiMajorAxis + ellipseB.semiMajorAxis));
	
	// AGGRESSIVE base repulsive force using elliptical field approximation
	let baseForce = (150000 * separationStrengthMultiplier * forceMultiplier) / (surfaceDistance * surfaceDistance);
	
	// Apply EXTREME repulsion for overlapping or too-close ellipses
	if (centerDistance < minSeparationDistance) {
		const overlapFactor = minSeparationDistance / Math.max(centerDistance, 1);
		baseForce *= FORCE_CONFIG.ellipse.collisionMultiplier * Math.pow(overlapFactor, 2); // Squared overlap factor for more aggressive separation
	}
	
	// Apply smooth ramping for force transition with increased range
	const rampDistance = minSeparationDistance * FORCE_CONFIG.ellipse.forceRampDistance;
	if (centerDistance < rampDistance) {
		const rampFactor = 1.0 + Math.pow((rampDistance - centerDistance) / rampDistance, 2); // Squared for more aggressive ramping
		baseForce *= rampFactor;
	}
	
	return {
		x: (dx / centerDistance) * baseForce,
		y: (dy / centerDistance) * baseForce
	};
}

/**
 * Estimate table dimensions based on entity content
 */
function getTableDimensions(entityName) {
	const entity = entities[entityName];
	if (!entity) return { width: 625, height: 500 }; // Increased from 250x200 to 625x500 (2.5x larger)
	
	// Calculate dimensions based on content
	const properties = entity.properties || [];
	const visibleProperties = showNavigationProperties ? 
		properties : 
		properties.filter(p => !isNavigationProperty(p));
	
	const headerHeight = 88; // Increased from 35 to 88 (2.5x larger)
	const rowHeight = 55; // Increased from 22 to 55 (2.5x larger)
	const padding = 30; // Increased from 12 to 30 (2.5x larger)
	const minWidth = 625; // Increased from 250 to 625 (2.5x larger)
	
	// Estimate width based on longest text
	const maxNameLength = Math.max(
		entity.type.length,
		...visibleProperties.map(p => p.name.length)
	);
	const maxTypeLength = Math.max(
		0,
		...visibleProperties.map(p => p.type.length)
	);
	
	const estimatedWidth = Math.max(minWidth, (maxNameLength * 20) + (maxTypeLength * 18) + (padding * 8)); // Increased multipliers from 8,7,3 to 20,18,8
	const estimatedHeight = headerHeight + (visibleProperties.length * rowHeight) + padding;
	
	return {
		width: estimatedWidth,
		height: Math.max(estimatedHeight, 250) // Increased minimum height from 100 to 250 (2.5x larger)
	};
}

/**
 * Calculate attractive force along an edge (Hooke's law)
 */
function calculateAttractiveForce(nodeA, nodeB, connectionStrength, idealEdgeLength) {
	const dx = nodeB.x - nodeA.x;
	const dy = nodeB.y - nodeA.y;
	const distance = Math.sqrt(dx * dx + dy * dy);
	
	if (distance < 1) return { x: 0, y: 0 };
	
	// Spring force: F = k * (current_length - ideal_length)
	const lengthDifference = distance - idealEdgeLength;
	const force = (0.8 * connectionStrength) * lengthDifference;
		
	return {
		x: (dx / distance) * force,
		y: (dy / distance) * force
	};
}

/**
 * Apply forces with velocity damping and temperature scaling
 */
function applyForcesWithDamping(positions, forces, temperature, entityNames, phaseConfig) {
	let totalMovement = 0;
	
	entityNames.forEach(entityName => {
		const pos = positions[entityName];
		const force = forces[entityName];
		
		// Update velocity with damping
		pos.vx = pos.vx * phaseConfig.damping + force.x * 0.001;
		pos.vy = pos.vy * phaseConfig.damping + force.y * 0.001;
		
		// Limit maximum velocity
		const velocity = Math.sqrt(pos.vx * pos.vx + pos.vy * pos.vy);
		if (velocity > phaseConfig.maxVelocity) {
			pos.vx = (pos.vx / velocity) * phaseConfig.maxVelocity;
			pos.vy = (pos.vy / velocity) * phaseConfig.maxVelocity;
		}
		
		// Apply temperature scaling (simulated annealing)
		const temperatureFactor = Math.min(1, temperature / phaseConfig.initialTemperature);
		const displacement = {
			x: pos.vx * temperatureFactor,
			y: pos.vy * temperatureFactor
		};
		
		// Update position - keep connected entities below unconnected row
		pos.x += displacement.x;
		pos.y = Math.max(FORCE_CONFIG.unconnectedRowHeight + 50, pos.y + displacement.y);
		
		totalMovement += Math.sqrt(displacement.x * displacement.x + displacement.y * displacement.y);
	});
	
	return totalMovement;
}

/**
 * Enforce boundary constraints and normalize positions
 */
function enforceBoundaryConstraints(positions) {
	const result = {};
	
	if (Object.keys(positions).length === 0) return result;
	
	let minX = Infinity, maxX = -Infinity;
	let minY = Infinity, maxY = -Infinity;
	
	// Find current bounds (excluding unconnected entities at the top)
	Object.entries(positions).forEach(([entityName, pos]) => {
		if (pos.y > FORCE_CONFIG.unconnectedRowHeight) { // Only consider connected entities for bounds
			minX = Math.min(minX, pos.x);
			maxX = Math.max(maxX, pos.x);
			minY = Math.min(minY, pos.y);
			maxY = Math.max(maxY, pos.y);
		}
	});
	
	// Calculate scaling for connected entities only
	const currentWidth = maxX - minX;
	const currentHeight = maxY - minY;
	const targetWidth = CANVAS_WIDTH - (FORCE_CONFIG.boundaryPadding * 2);
	const targetHeight = CANVAS_HEIGHT - FORCE_CONFIG.unconnectedRowHeight - (FORCE_CONFIG.boundaryPadding * 2);
	
	const scaleX = currentWidth > 0 ? targetWidth / currentWidth : 1;
	const scaleY = currentHeight > 0 ? targetHeight / currentHeight : 1;
	const scale = Math.min(scaleX, scaleY, 1); // Don't scale up, only down
	
	// Apply scaling and centering
	Object.keys(positions).forEach(entityName => {
		const pos = positions[entityName];
		
		if (pos.y <= FORCE_CONFIG.unconnectedRowHeight) {
			// Keep unconnected entities at their fixed positions
			result[entityName] = {
				x: Math.round(pos.x),
				y: Math.round(pos.y)
			};
		} else {
			// Scale and center connected entities
			result[entityName] = {
				x: Math.round(FORCE_CONFIG.boundaryPadding + (pos.x - minX) * scale),
				y: Math.round(FORCE_CONFIG.unconnectedRowHeight + FORCE_CONFIG.boundaryPadding + (pos.y - minY) * scale)
			};
		}
	});
	
	console.log(`📐 Final layout: ${Object.keys(result).length} entities positioned (scale: ${scale.toFixed(2)})`);
	return result;
}

/**
 * Convert internal format back to standard format expected by the system
 */
function convertToStandardFormat(optimizedPositions) {
	const result = {};
	
	Object.keys(optimizedPositions).forEach(entityName => {
		const pos = optimizedPositions[entityName];
		result[entityName] = {
			x: Math.round(pos.x || 0),
			y: Math.round(pos.y || 0)
		};
	});
	
	return result;
}

/**
 * Get current table positions from DOM
 */
function getCurrentTablePositions() {
	const positions = {};
	
	document.querySelectorAll('.table-group').forEach(tableGroup => {
		const entityName = tableGroup.getAttribute('data-entity');
		const rect = tableGroup.querySelector('.table-box');
		
		if (rect && entityName) {
			positions[entityName] = {
				x: parseFloat(rect.getAttribute('x')),
				y: parseFloat(rect.getAttribute('y')),
				vx: 0,
				vy: 0
			};
		}
	});
	
	return positions;
}

/**
 * Apply optimized positions to existing tables
 */
function applyPositionsToTables(positions) {
	Object.keys(positions).forEach(entityName => {
		const tableGroup = document.querySelector(`[data-entity="${entityName}"]`);
		if (tableGroup && positions[entityName]) {
			const newPos = positions[entityName];
			moveTable(tableGroup, newPos.x, newPos.y);
		}
	});
	
	// Update relationships after moving tables
	if (showRelationships) {
		updateRelationships();
	}
}