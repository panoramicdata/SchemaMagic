// Property utilities and grouping
const SVG_NAMESPACE = "http://www.w3.org/2000/svg";

const INT_TYPES = ["int", "long", "short", "byte"];
const DOUBLE_TYPES = ["double", "float", "decimal"];
const DATETIME_TYPES = ["DateTime", "DateTimeOffset", "TimeSpan"];
const COLLECTION_PREFIXES = ["ICollection", "List", "IList"];
const KNOWN_SCALAR_TYPES = [
	"string",
	"bool",
	"Guid",
	"JsonDocument",
	...INT_TYPES,
	...DOUBLE_TYPES,
	...DATETIME_TYPES,
];

// Ordered: the first matching rule decides the kind of a (non-nullable) type name
const TYPE_RULES = [
	["string", (t) => t === "string"],
	["int", (t) => INT_TYPES.includes(t)],
	["double", (t) => DOUBLE_TYPES.includes(t)],
	["bool", (t) => t === "bool"],
	["Guid", (t) => t === "Guid"],
	["DateTime", (t) => DATETIME_TYPES.includes(t)],
	["Collection", (t) => COLLECTION_PREFIXES.some((p) => t.startsWith(p))],
	["JsonDocument", (t) => t === "JsonDocument"],
	["Navigation", (t) => t.includes("Model") || t.includes("Entity")],
	["Enum", (t) => !t.includes("<") && !KNOWN_SCALAR_TYPES.includes(t)],
];

const TYPE_COLOR_CLASSES = {
	string: "property-type-string",
	int: "property-type-int",
	double: "property-type-double",
	bool: "property-type-bool",
	Guid: "property-type-guid",
	DateTime: "property-type-datetime",
	Collection: "property-type-collection",
	JsonDocument: "property-type-jsondocument",
	Navigation: "property-type-navigation",
	Enum: "property-type-enum",
	unknown: "property-type-unknown",
};

const TYPE_SYMBOLS = {
	string: "a",
	int: "#",
	double: ".",
	bool: "✓",
	JsonDocument: "{}",
	Collection: "[]",
	unknown: "?",
};

const PROPERTY_LEVEL_ICON_CLASSES = {
	PK: "icon-pk",
	FK: "icon-fk",
	N: "icon-nav",
	INH: "icon-inh",
};

const NAVIGATION_PATTERNS = [
	/^ICollection</,
	/^List</,
	/^IList</,
	/^HashSet</,
	/^ISet</,
	/Model$/,
	/Entity$/,
];

function getPropertyIcon(property) {
	return getPropertyLevelIcon(property);
}

function getPropertyLevelIcon(property) {
	// Primary keys ALWAYS show PK, even if inherited
	if (property.isKey) return "PK";
	if (property.isInherited) return "INH";
	if (property.isForeignKey) return "FK";
	if (isNavigationProperty(property)) return "N";
	return null;
}

function classifyType(type) {
	const cleanType = type.replace(/\?$/, "");
	const rule = TYPE_RULES.find(([, matches]) => matches(cleanType));
	return rule ? rule[0] : "unknown";
}

function getTypeIcon(type) {
	return { iconType: classifyType(type), isNullable: type.endsWith("?") };
}

function getTypeColorClass(type) {
	return TYPE_COLOR_CLASSES[classifyType(type)];
}

function compareProperties(a, b) {
	// Id first, then inherited before regular, then alphabetical
	const aIsId = a.name.toLowerCase() === "id";
	const bIsId = b.name.toLowerCase() === "id";
	if (aIsId !== bIsId) return aIsId ? -1 : 1;
	if (Boolean(a.isInherited) !== Boolean(b.isInherited)) {
		return a.isInherited ? -1 : 1;
	}
	return a.name.localeCompare(b.name);
}

function sortProperties(properties) {
	return properties.sort(compareProperties);
}

function findNavigationPartner(prop, visibleProperties, processed) {
	if (!prop.isForeignKey || !showNavigationProperties) return null;
	const navPropName = prop.name.replace(/Id$/, "");
	return (
		visibleProperties.find(
			(p) =>
				p.name === navPropName &&
				isNavigationProperty(p) &&
				!processed.has(p.name),
		) || null
	);
}

function groupPropertiesWithNavigation(visibleProperties, allProperties) {
	const grouped = [];
	const processed = new Set();

	visibleProperties.forEach((prop) => {
		if (processed.has(prop.name)) return;

		const navProp = findNavigationPartner(prop, visibleProperties, processed);
		if (navProp) {
			grouped.push({ type: "group", properties: [prop, navProp] });
			processed.add(navProp.name);
		} else {
			grouped.push({ type: "single", property: prop });
		}
		processed.add(prop.name);
	});

	return grouped;
}

// Build the type text shown in the type column. Navigation collection wrappers are stripped,
// and scalar columns with a max length get a SQL-style "(n)" suffix, e.g. "string(255)".
function getDisplayType(property) {
	const displayType = property.type;

	if (isNavigationProperty(property)) {
		return displayType
			.replace(/^ICollection<(.+)>$/, "$1")
			.replace(/^List<(.+)>$/, "$1");
	}

	if (property.maxLength && property.maxLength > 0) {
		// The length goes before any nullable marker: "string?" -> "string(255)?"
		const isNullable = displayType.endsWith("?");
		const base = isNullable ? displayType.slice(0, -1) : displayType;
		return `${base}(${property.maxLength})${isNullable ? "?" : ""}`;
	}

	return displayType;
}

function isNavigationProperty(property) {
	if (NAVIGATION_PATTERNS.some((pattern) => pattern.test(property.type))) {
		return true;
	}

	// Many-to-one references: the type names an entity we know about
	const cleanType = property.type.replace(/\?$/, "");
	if (typeof entities !== "undefined" && entities[cleanType]) {
		return true;
	}

	return false;
}

function navigateToEntity(typeName) {
	const cleanTypeName = typeName
		.replace(/\?$/, "")
		.replace(/ICollection<(.+)>/, "$1")
		.replace(/List<(.+)>/, "$1");

	const targetTable = Array.from(
		document.querySelectorAll("[data-entity]"),
	).find((element) => element.getAttribute("data-entity") === cleanTypeName);
	if (!targetTable) return;

	selectTable(cleanTypeName);

	const rect = targetTable.querySelector(".table-box");
	const targetX = parseFloat(rect.getAttribute("x"));
	const targetY = parseFloat(rect.getAttribute("y"));
	const targetWidth = parseFloat(rect.getAttribute("width"));
	const targetHeight = parseFloat(rect.getAttribute("height"));

	svgViewBox.x = targetX - svgViewBox.width / 2 + targetWidth / 2;
	svgViewBox.y = targetY - svgViewBox.height / 2 + targetHeight / 2;
	updateViewBox();
}

function createSvgElement(name, attributes, className) {
	const element = document.createElementNS(SVG_NAMESPACE, name);
	Object.entries(attributes).forEach(([key, value]) => {
		element.setAttribute(key, value);
	});
	if (className) {
		element.classList.add(className);
	}
	return element;
}

function createCenteredText(centerX, textY, fillColor, text) {
	const element = createSvgElement(
		"text",
		{
			x: centerX,
			y: textY,
			"text-anchor": "middle",
			"dominant-baseline": "central",
			fill: fillColor,
		},
		"type-icon-symbol-large",
	);
	element.textContent = text;
	return element;
}

// Property-level icons (PK, FK, N, INH) are drawn on a coloured circle
function createLevelIcon(iconType, centerX, textY, radius) {
	const circle = createSvgElement(
		"circle",
		{ cx: centerX, cy: textY, r: radius },
		"property-icon-circle",
	);
	circle.classList.add(PROPERTY_LEVEL_ICON_CLASSES[iconType]);

	const text = createSvgElement(
		"text",
		{
			x: centerX,
			y: textY,
			"text-anchor": "middle",
			"dominant-baseline": "central",
		},
		"property-icon-text",
	);
	text.textContent = iconType;
	return [circle, text];
}

function createDateTimeIcon(centerX, textY, strokeColor) {
	const calendar = createSvgElement(
		"rect",
		{
			x: centerX - 10,
			y: textY - 8,
			width: "20",
			height: "16",
			fill: "none",
			stroke: strokeColor,
			"stroke-width": "2",
		},
		"type-icon-shape-large",
	);
	const line = createSvgElement(
		"line",
		{
			x1: centerX - 8,
			y1: textY - 2,
			x2: centerX + 8,
			y2: textY - 2,
			stroke: strokeColor,
			"stroke-width": "2",
		},
		"type-icon-shape-large",
	);
	return [calendar, line];
}

function createPolygonIcon(points, attributes) {
	return createSvgElement(
		"polygon",
		{ points: points, ...attributes },
		"type-icon-shape-large",
	);
}

function createShapeIcon(type, centerX, textY, fillColor, strokeColor) {
	switch (type) {
		case "Guid":
			return [
				createSvgElement(
					"rect",
					{
						x: centerX - 8,
						y: textY - 8,
						width: "16",
						height: "16",
						fill: fillColor,
						stroke: strokeColor,
					},
					"type-icon-shape-large",
				),
			];
		case "DateTime":
			return createDateTimeIcon(centerX, textY, strokeColor);
		case "Enum":
			return [
				createPolygonIcon(
					`${centerX},${textY - 8} ${centerX + 8},${textY} ${centerX},${textY + 8} ${centerX - 8},${textY}`,
					{ fill: fillColor, stroke: strokeColor },
				),
			];
		default:
			// Navigation arrow
			return [
				createPolygonIcon(
					`${centerX - 8},${textY - 6} ${centerX + 6},${textY} ${centerX - 8},${textY + 6} ${centerX - 5},${textY + 3} ${centerX + 3},${textY} ${centerX - 5},${textY - 3}`,
					{ fill: "#059669" },
				),
			];
	}
}

// Type icons have no circle and use a larger size
function createTypeIcon(iconInfo, centerX, textY) {
	const { iconType: type, isNullable } = iconInfo;
	const color = isNullable ? "#9ca3af" : "#4b5563";

	if (["Guid", "DateTime", "Enum", "Navigation"].includes(type)) {
		return createShapeIcon(type, centerX, textY, color, color);
	}
	return [createCenteredText(centerX, textY, color, TYPE_SYMBOLS[type] ?? "?")];
}

function createPropertyIcon(iconType, x, y) {
	const iconSize = 30;
	const centerX = x + iconSize / 2 + 10;
	const textY = y - 12;

	if (PROPERTY_LEVEL_ICON_CLASSES[iconType]) {
		return createLevelIcon(iconType, centerX, textY, iconSize / 2);
	}
	if (typeof iconType === "object" && iconType && iconType.iconType) {
		return createTypeIcon(iconType, centerX, textY);
	}
	return [];
}
