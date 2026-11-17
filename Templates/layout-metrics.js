// Measurements and DOM helpers shared by the layout code

// FK relationships, including inherited ones so base-class links influence the layout
function extractRelationships() {
	const relationships = [];

	Object.values(entities).forEach((entity) => {
		const seenPropNames = new Set();
		const allProps = [
			...(entity.properties || []),
			...(entity.inheritedProperties || []),
		].filter((p) => !seenPropNames.has(p.name) && seenPropNames.add(p.name));

		allProps
			.filter((prop) => prop.isForeignKey)
			.forEach((prop) => {
				const targetEntityName = findTargetEntity(prop.name);
				if (targetEntityName && entities[targetEntityName]) {
					relationships.push({
						from: entity.type,
						to: targetEntityName,
						property: prop.name,
					});
				}
			});
	});

	return relationships;
}

function getLayoutVisibleProperties(entity) {
	const inheritedProps = showInheritedProperties
		? entity.inheritedProperties || []
		: [];
	const allProperties = [...inheritedProps, ...(entity.properties || [])];

	return showNavigationProperties
		? allProperties
		: allProperties.filter((p) => !isNavigationProperty(p));
}

function getLayoutTypeLength(property) {
	if (!isNavigationProperty(property)) {
		return property.type.length;
	}
	return property.type
		.replace(/^ICollection<(.+)>$/, "$1")
		.replace(/^List<(.+)>$/, "$1").length;
}

// Estimates table size BEFORE rendering, using the same metrics as generateTable
function getTableDimensions(entityName) {
	const entity = entities[entityName];
	if (!entity) return { width: 1000, height: 500 };

	const m = TABLE_METRICS;
	const visibleProperties = getLayoutVisibleProperties(entity);
	const inheritanceHeight =
		entity.baseType && showInheritedProperties ? m.inheritanceHeight : 0;

	const maxNameLength = Math.max(
		entity.type.length,
		...visibleProperties.map((p) => p.name.length),
		0,
	);
	const maxTypeLength = Math.max(
		8,
		...visibleProperties.map(getLayoutTypeLength),
	);

	const visibleRows = fullHeightMode
		? visibleProperties.length
		: Math.min(visibleProperties.length, m.collapsedRowLimit);
	const height =
		m.headerHeight + inheritanceHeight + visibleRows * m.rowHeight + m.padding;

	return {
		width: computeTableWidth(maxNameLength, maxTypeLength),
		height: Math.max(height, 200),
	};
}

function getCurrentTablePositions() {
	const positions = {};

	document.querySelectorAll(".table-group").forEach((tableGroup) => {
		const entityName = tableGroup.getAttribute("data-entity");
		const rect = tableGroup.querySelector(".table-box");

		if (rect && entityName) {
			positions[entityName] = {
				x: parseFloat(rect.getAttribute("x")),
				y: parseFloat(rect.getAttribute("y")),
			};
		}
	});

	return positions;
}

// Moves existing tables to computed positions and refreshes relationship lines
function applyPositionsToTables(positions) {
	Object.keys(positions).forEach((entityName) => {
		const tableGroup = findTableGroup(entityName);
		if (tableGroup && positions[entityName]) {
			const newPos = positions[entityName];
			moveTable(tableGroup, newPos.x, newPos.y);
		}
	});

	if (showRelationships) {
		updateRelationships();
	}
}
