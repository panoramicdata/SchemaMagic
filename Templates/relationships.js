// Relationship lines generation and management with Crow's Foot notation
const MARKER_OFFSET = 50;
const SELF_REF_LOOP_WIDTH = 200;
const VERTICAL_DETOUR_THRESHOLD = 250;
const CROW_SIZE = 35; // Distance from table edge to the crow's foot tip
const CIRCLE_RADIUS = 20; // Radius of the optional indicator
const CIRCLE_GAP = 5;
const CIRCLE_OFFSET = CROW_SIZE + CIRCLE_RADIUS + CIRCLE_GAP;
// Must match the CSS .relationship-line stroke colour and width
const NOTATION_COLOR = "#2563eb";
const NOTATION_STROKE_WIDTH = 8;
const NOTATION_ANGLES = { right: 0, left: 180, up: -90, down: 90 };

function generateRelationships(svg) {
	if (!showRelationships) {
		return;
	}

	extractRelationships().forEach((relationship) => {
		createCrowsFootRelationshipLine(
			svg,
			relationship.from,
			relationship.to,
			relationship.property,
		);
	});
}

function findEntityByExactName(candidates) {
	return candidates.find((name) => entities[name]) || null;
}

function findFuzzyEntityMatch(foreignKeyName, candidate) {
	const fkLower = foreignKeyName.toLowerCase();
	const candidateLower = candidate.toLowerCase();
	return (
		Object.keys(entities).find(
			(entityName) =>
				fkLower.includes(entityName.toLowerCase()) ||
				entityName.toLowerCase().includes(candidateLower),
		) || null
	);
}

function findTargetEntity(foreignKeyName) {
	// Strategy 1: remove the Id suffix (DeviceId -> Device)
	const candidate = foreignKeyName.replace(/Id$/, "");

	// Strategy 2: common patterns, then fuzzy matching on names
	return (
		findEntityByExactName([
			candidate,
			foreignKeyName.replace(/Id$/, "Model"),
			foreignKeyName,
			foreignKeyName.replace(/Model$/, ""),
		]) || findFuzzyEntityMatch(foreignKeyName, candidate)
	);
}

function buildRoutePath(points) {
	return points.map(([x, y], index) => `${index === 0 ? "M" : "L"} ${x} ${y}`).join(" ");
}

// Route when the source table sits on the near side: direction is +1 (right) or -1 (left)
function buildSideToSideRoute(fromX, fromY, toX, toY, direction) {
	const verticalSeparation = Math.abs(fromY - toY);
	const horizontalSeparation = direction * (toX - fromX);
	const entryX = toX - direction * MARKER_OFFSET;

	const turnOffset =
		verticalSeparation > VERTICAL_DETOUR_THRESHOLD
			? Math.max(MARKER_OFFSET, horizontalSeparation * 0.4)
			: MARKER_OFFSET;
	const turnX = fromX + direction * turnOffset;

	return buildRoutePath([
		[fromX, fromY],
		[turnX, fromY],
		[turnX, toY],
		[entryX, toY],
		[toX, toY],
	]);
}

function computeRelationshipRoute(fromInfo, toInfo, isSelfReferential) {
	const fromY = fromInfo.y;
	const toY = toInfo.y;

	if (isSelfReferential) {
		const fromX = fromInfo.leftEdge;
		const toX = toInfo.leftEdge;
		const loopLeft = fromInfo.leftEdge - SELF_REF_LOOP_WIDTH;
		return {
			fromX,
			fromY,
			toX,
			toY,
			fromDirection: "left",
			toDirection: "left",
			pathData: buildRoutePath([
				[fromX, fromY],
				[loopLeft, fromY],
				[loopLeft, toY],
				[toX, toY],
			]),
		};
	}

	const leftToRight = fromInfo.centerX < toInfo.centerX;
	const fromX = leftToRight ? fromInfo.rightEdge : fromInfo.leftEdge;
	const toX = leftToRight ? toInfo.leftEdge : toInfo.rightEdge;

	return {
		fromX,
		fromY,
		toX,
		toY,
		fromDirection: leftToRight ? "right" : "left",
		toDirection: leftToRight ? "left" : "right",
		pathData: buildSideToSideRoute(fromX, fromY, toX, toY, leftToRight ? 1 : -1),
	};
}

function insertBehindTables(svg, element) {
	const firstTableGroup = svg.querySelector(".table-group");
	if (firstTableGroup) {
		svg.insertBefore(element, firstTableGroup);
	} else {
		svg.appendChild(element);
	}
}

function tagRelationshipElement(element, from, to, propertyName) {
	element.setAttribute("data-from", from);
	element.setAttribute("data-to", to);
	element.setAttribute("data-property", propertyName);
}

function drawRouteNotations(svg, route, relationship, from, to, propertyName) {
	const fromCrowsFoot = drawCrowsFootNotation(
		svg,
		route.fromX,
		route.fromY,
		route.fromDirection,
		relationship,
		true,
	);
	const toCrowsFoot = drawCrowsFootNotation(
		svg,
		route.toX,
		route.toY,
		route.toDirection,
		relationship,
		false,
	);
	tagRelationshipElement(fromCrowsFoot, from, to, propertyName);
	tagRelationshipElement(toCrowsFoot, from, to, propertyName);
	return [fromCrowsFoot, toCrowsFoot];
}

function createCrowsFootRelationshipLine(svg, fromEntityName, toEntityName, propertyName) {
	const relationship = analyzeRelationshipCardinality(
		fromEntityName,
		toEntityName,
		propertyName,
	);

	const fromInfo = getTableConnectionPoint(fromEntityName, propertyName);
	const toInfo = getTableConnectionPoint(toEntityName, "Id");
	if (!fromInfo || !toInfo) {
		return;
	}

	const isSelfReferential = fromEntityName === toEntityName;
	const route = computeRelationshipRoute(fromInfo, toInfo, isSelfReferential);

	const line = createSvgElement(
		"path",
		{
			d: route.pathData,
			"data-relationship-type": relationship.type,
			"data-self-referential": isSelfReferential ? "true" : "false",
		},
		"relationship-line",
	);
	tagRelationshipElement(line, fromEntityName, toEntityName, propertyName);
	insertBehindTables(svg, line);

	drawRouteNotations(svg, route, relationship, fromEntityName, toEntityName, propertyName);
}

function createOptionalCircle() {
	return createSvgElement("circle", {
		cx: CIRCLE_OFFSET,
		cy: "0",
		r: CIRCLE_RADIUS,
		stroke: NOTATION_COLOR,
		"stroke-width": NOTATION_STROKE_WIDTH,
		fill: "white",
	});
}

function createManySymbol() {
	// A ">" shape pointing away from the table
	return createSvgElement("path", {
		d: `M 0,${-CROW_SIZE} L ${CROW_SIZE},0 L 0,${CROW_SIZE}`,
		stroke: NOTATION_COLOR,
		"stroke-width": NOTATION_STROKE_WIDTH,
		fill: "none",
		"stroke-linejoin": "miter",
	});
}

function createOneSymbol() {
	return createSvgElement("line", {
		x1: CROW_SIZE,
		y1: -CROW_SIZE,
		x2: CROW_SIZE,
		y2: CROW_SIZE,
		stroke: NOTATION_COLOR,
		"stroke-width": NOTATION_STROKE_WIDTH,
	});
}

// The from side (FK table) is the many side; the to side (PK table) is the one side
function drawCrowsFootNotation(svg, x, y, direction, relationship, isFromSide) {
	const isOptional = isFromSide ? relationship.fromOptional : relationship.toOptional;
	const angle = NOTATION_ANGLES[direction] ?? 0;

	const group = createSvgElement(
		"g",
		{ transform: `translate(${x}, ${y}) rotate(${angle})` },
		"crows-foot-notation",
	);

	if (isOptional) {
		group.appendChild(createOptionalCircle());
	}
	group.appendChild(isFromSide ? createManySymbol() : createOneSymbol());

	insertBehindTables(svg, group);
	return group;
}

function collectEntityProperties(entity) {
	return [...(entity.properties || []), ...(entity.inheritedProperties || [])];
}

function findCollectionNavProperty(toProps, fromEntityName) {
	const pluralName = fromEntityName.replace(/Model$/, "").toLowerCase() + "s";
	return toProps.find(
		(p) =>
			p.type.includes(`ICollection<${fromEntityName}>`) ||
			p.type.includes(`List<${fromEntityName}>`) ||
			p.name.toLowerCase().includes(pluralName),
	);
}

function findSourceNavProperty(fromProps, toEntityName) {
	const navName = toEntityName.replace(/Model$/, "");
	return fromProps.find(
		(p) =>
			p.type === toEntityName ||
			p.type === `${toEntityName}?` ||
			p.name === navName,
	);
}

// PropertyInfo has no nullable flag, so nullability is read from the type string
function isNullableProperty(property) {
	return Boolean(property && property.type.includes("?"));
}

function analyzeRelationshipCardinality(fromEntityName, toEntityName, propertyName) {
	const fromEntity = entities[fromEntityName];
	const toEntity = entities[toEntityName];

	if (!fromEntity || !toEntity) {
		return { type: "1:N", fromOptional: false, toOptional: false };
	}

	const fromProps = collectEntityProperties(fromEntity);
	const fkNullable = isNullableProperty(fromProps.find((p) => p.name === propertyName));
	const collectionNavProp = findCollectionNavProperty(
		collectEntityProperties(toEntity),
		fromEntityName,
	);
	const sourceNavProp = findSourceNavProperty(fromProps, toEntityName);
	const eitherNullable = fkNullable || isNullableProperty(sourceNavProp);

	let type = "1:N";
	let toOptional = fkNullable;
	if (sourceNavProp && collectionNavProp) {
		toOptional = eitherNullable;
	} else if (sourceNavProp) {
		// No collection on the target: one-to-one or one-to-zero-or-one
		type = eitherNullable ? "1:0..1" : "1:1";
		toOptional = eitherNullable;
	}

	return { type, fromOptional: fkNullable, toOptional };
}

function getTableConnectionPoint(entityName, propertyName) {
	const tableGroup = findTableGroup(entityName);
	const rect = tableGroup && tableGroup.querySelector(".table-box");
	if (!rect) {
		return null;
	}

	const x = parseFloat(rect.getAttribute("x"));
	const y = parseFloat(rect.getAttribute("y"));
	const width = parseFloat(rect.getAttribute("width"));
	const height = parseFloat(rect.getAttribute("height"));
	const centerY = y + height / 2;

	return {
		x: x,
		y: findPropertyConnectionPoint(tableGroup, propertyName, centerY),
		width: width,
		centerX: x + width / 2,
		centerY: centerY,
		leftEdge: x,
		rightEdge: x + width,
	};
}

function findPropertyConnectionPoint(tableGroup, propertyName, fallbackY) {
	const propertyText = Array.from(
		tableGroup.querySelectorAll(".property-text"),
	).find((element) => element.textContent === propertyName);
	if (propertyText) {
		// Property icons are drawn at y-12, which is the centre of the row
		return parseFloat(propertyText.getAttribute("y")) - 12;
	}

	// An Id that is not shown is inherited, so use the header centre
	const headerRect = propertyName === "Id" && tableGroup.querySelector(".table-header");
	if (headerRect) {
		const headerY = parseFloat(headerRect.getAttribute("y"));
		return headerY + parseFloat(headerRect.getAttribute("height")) / 2;
	}

	return fallbackY;
}

function copyHighlightState(line, notations) {
	const stateClass = ["highlighted", "dimmed"].find((name) =>
		line.classList.contains(name),
	);
	if (stateClass) {
		notations.forEach((notation) => notation.classList.add(stateClass));
	}
}

function redrawRelationshipLine(line) {
	const from = line.getAttribute("data-from");
	const to = line.getAttribute("data-to");
	const propertyName = line.getAttribute("data-property");
	const isSelfReferential =
		line.getAttribute("data-self-referential") === "true" || from === to;

	const fromInfo = getTableConnectionPoint(from, propertyName);
	const toInfo = getTableConnectionPoint(to, "Id");
	if (!fromInfo || !toInfo) {
		return;
	}

	const relationship = analyzeRelationshipCardinality(from, to, propertyName);
	const route = computeRelationshipRoute(fromInfo, toInfo, isSelfReferential);
	line.setAttribute("d", route.pathData);

	const svg = document.getElementById("schema-svg");
	const notations = drawRouteNotations(svg, route, relationship, from, to, propertyName);
	copyHighlightState(line, notations);
}

function applyLineVisibility(line) {
	line.classList.remove("highlighted", "dimmed", "hidden");

	if (!showRelationships) {
		line.style.display = "none";
		return;
	}

	line.style.display = "block";
	if (showOnlySelectedRelations && selectedTable) {
		const involvesSelection =
			line.getAttribute("data-from") === selectedTable ||
			line.getAttribute("data-to") === selectedTable;
		line.classList.add(involvesSelection ? "highlighted" : "dimmed");
	}
}

function applyNotationVisibility(notation) {
	if (!showRelationships) {
		notation.style.display = "none";
		return;
	}

	notation.style.display = "block";
	notation.classList.remove("highlighted", "dimmed");
	if (showOnlySelectedRelations && selectedTable) {
		const involvesSelection =
			notation.getAttribute("data-from") === selectedTable ||
			notation.getAttribute("data-to") === selectedTable;
		notation.classList.add(involvesSelection ? "highlighted" : "dimmed");
	}
}

function updateRelationships() {
	document
		.querySelectorAll(".crows-foot-notation")
		.forEach((notation) => notation.remove());

	document.querySelectorAll(".relationship-line").forEach((line) => {
		redrawRelationshipLine(line);
		applyLineVisibility(line);
	});

	document
		.querySelectorAll(".crows-foot-notation")
		.forEach(applyNotationVisibility);
}
