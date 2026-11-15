// Table generation and rendering
const TABLE_METRICS = {
	rowHeight: 55,
	headerHeight: 88,
	inheritanceHeight: 50,
	padding: 30,
	minWidth: 1000,
	iconWidth: 60,
	typeIconSpace: 50,
	spacingBuffer: 100,
	tableIconSpace: 40,
	collapsedRowLimit: 15,
	maxTitleFontSize: 80,
	minTitleFontSize: 32,
};

function createTextElement(attributes, className, text) {
	const element = createSvgElement("text", attributes, className);
	if (text !== undefined) {
		element.textContent = text;
	}
	return element;
}

function appendTitleTooltip(element, comment) {
	if (comment) {
		const title = createSvgElement("title", {});
		title.textContent = comment;
		element.appendChild(title);
	}
}

function collectGroupedProperties(entity) {
	const inheritedProps = showInheritedProperties
		? entity.inheritedProperties || []
		: [];
	const allProperties = [
		...inheritedProps.map((p) => ({ ...p, isInherited: true })),
		...(entity.properties || []).map((p) => ({ ...p, isInherited: false })),
	];

	const sortedProperties = sortProperties(allProperties);
	const visibleProperties = showNavigationProperties
		? sortedProperties
		: sortedProperties.filter((p) => !isNavigationProperty(p));

	return groupPropertiesWithNavigation(visibleProperties, sortedProperties);
}

function flattenGroupedProperties(groupedProperties) {
	return groupedProperties.flatMap((item) =>
		item.type === "group" ? item.properties : [item.property],
	);
}

function computeTableWidth(maxNameLength, maxTypeLength) {
	const m = TABLE_METRICS;
	const nameColumnWidth = Math.max(200, maxNameLength * 18);
	const typeColumnWidth = Math.max(150, maxTypeLength * 15);

	return Math.max(
		m.minWidth,
		m.iconWidth +
			nameColumnWidth +
			typeColumnWidth +
			m.typeIconSpace +
			m.spacingBuffer +
			m.padding * 2,
	);
}

function calculateTableWidth(entity, displayedProperties) {
	const maxNameLength = Math.max(
		entity.type.length,
		...displayedProperties.map((p) => p.name.length),
	);
	// Includes any "(maxLength)" suffix
	const maxTypeLength = Math.max(
		8,
		...displayedProperties.map((p) => getDisplayType(p).length),
	);
	return computeTableWidth(maxNameLength, maxTypeLength);
}

function calculateTableLayout(entity) {
	const m = TABLE_METRICS;
	const groupedProperties = collectGroupedProperties(entity);
	const displayedProperties = flattenGroupedProperties(groupedProperties);
	const totalDisplayRows = displayedProperties.length;

	const inheritanceHeight =
		entity.baseType && showInheritedProperties ? m.inheritanceHeight : 0;
	const maxVisibleRows = fullHeightMode
		? totalDisplayRows
		: Math.min(totalDisplayRows, m.collapsedRowLimit);

	return {
		matchingRule: getMatchingRule(entity.type),
		totalDisplayRows,
		maxVisibleRows,
		inheritanceHeight,
		tableWidth: calculateTableWidth(entity, displayedProperties),
		tableHeight:
			m.headerHeight +
			inheritanceHeight +
			maxVisibleRows * m.rowHeight +
			m.padding,
		propertiesToShow: getPropertiesToShow(groupedProperties, maxVisibleRows),
	};
}

function appendSectionDivider(tableGroup, x, currentY, tableWidth) {
	tableGroup.appendChild(
		createSvgElement(
			"line",
			{
				x1: x + 10,
				y1: currentY - 5,
				x2: x + tableWidth - 10,
				y2: currentY - 5,
			},
			"section-divider",
		),
	);
}

function appendGroupBackground(tableGroup, item, x, currentY, tableWidth) {
	const rowHeight = TABLE_METRICS.rowHeight;
	const groupRect = createSvgElement(
		"rect",
		{
			x: x + 5,
			y: currentY - 10,
			width: tableWidth - 10,
			height: item.properties.length * rowHeight + 10,
		},
		"property-group",
	);
	if (item.properties[0].isInherited) {
		groupRect.classList.add("inherited-section");
	}
	tableGroup.appendChild(groupRect);

	const isForeignKeyGroup =
		item.properties.length === 2 &&
		item.properties[0].isForeignKey &&
		isNavigationProperty(item.properties[1]);
	if (isForeignKeyGroup) {
		tableGroup.appendChild(
			createSvgElement(
				"line",
				{
					x1: x + 25,
					y1: currentY + 10,
					x2: x + 25,
					y2: currentY + rowHeight + 10,
				},
				"fk-nav-connector",
			),
		);
	}
}

// Property group backgrounds and section dividers sit below the text
function appendPropertyBackgrounds(tableGroup, layout, x, y) {
	const m = TABLE_METRICS;
	let currentY = y + m.headerHeight + layout.inheritanceHeight;
	let lastPropertyWasInherited = false;

	layout.propertiesToShow.forEach((item) => {
		const properties = item.type === "group" ? item.properties : [item.property];

		if (lastPropertyWasInherited && !properties[0].isInherited) {
			appendSectionDivider(tableGroup, x, currentY, layout.tableWidth);
		}
		if (item.type === "group") {
			appendGroupBackground(tableGroup, item, x, currentY, layout.tableWidth);
		}

		currentY += properties.length * m.rowHeight;
		lastPropertyWasInherited = properties[properties.length - 1].isInherited;
	});
}

// Estimated width is roughly 0.6 * fontSize * characters for Segoe UI Bold
function calculateTitleFontSize(characterCount, availableWidth) {
	let fontSize = TABLE_METRICS.maxTitleFontSize;
	while (
		fontSize > TABLE_METRICS.minTitleFontSize &&
		0.6 * fontSize * characterCount > availableWidth
	) {
		fontSize -= 2;
	}
	return fontSize;
}

function appendHeader(tableGroup, entity, layout, x, y) {
	const m = TABLE_METRICS;
	const rule = layout.matchingRule;

	const headerRect = createSvgElement(
		"rect",
		{ x, y, width: layout.tableWidth, height: m.headerHeight },
		"table-header",
	);
	if (rule) {
		headerRect.style.fill = rule.color;
	}
	tableGroup.appendChild(headerRect);

	const titleText = createTextElement(
		{
			x: x + layout.tableWidth / 2,
			y: y + m.headerHeight / 2 + 5,
			"text-anchor": "middle",
			"dominant-baseline": "middle",
		},
		"table-title",
		entity.type,
	);
	appendTitleTooltip(titleText, entity.comment);

	const availableWidth =
		layout.tableWidth - m.padding * 2 - (rule && rule.icon ? m.tableIconSpace : 0);
	const fontSize = calculateTitleFontSize(entity.type.length, availableWidth);
	titleText.style.fontSize = `${fontSize}px`;
	tableGroup.appendChild(titleText);

	if (entity.baseType && showInheritedProperties) {
		tableGroup.appendChild(
			createTextElement(
				{
					x: x + layout.tableWidth / 2,
					y: y + m.headerHeight + 35,
					"text-anchor": "middle",
				},
				"inheritance-text",
				"inherits from " + entity.baseType,
			),
		);
	}
}

// Property text is added last so it appears on top of the backgrounds
function appendPropertyRows(tableGroup, layout, x, y, entityType) {
	const m = TABLE_METRICS;
	let currentY = y + m.headerHeight + layout.inheritanceHeight;

	layout.propertiesToShow.forEach((item) => {
		const properties = item.type === "group" ? item.properties : [item.property];
		const context = {
			x,
			tableWidth: layout.tableWidth,
			entityName: entityType,
		};

		properties.forEach((property, index) => {
			renderProperty(tableGroup, property, {
				...context,
				propY: currentY + index * m.rowHeight + 40,
			});
		});
		currentY += properties.length * m.rowHeight;
	});

	return currentY;
}

function appendEllipsis(tableGroup, layout, x, currentY) {
	if (fullHeightMode || layout.totalDisplayRows <= layout.maxVisibleRows) return;

	const ellipsisText = createTextElement(
		{ x: x + layout.tableWidth / 2, y: currentY + 15, "text-anchor": "middle" },
		"property-text",
		"... (+" + (layout.totalDisplayRows - layout.maxVisibleRows) + " more)",
	);
	ellipsisText.style.fontSize = "28px";
	ellipsisText.style.fill = "#9ca3af";
	tableGroup.appendChild(ellipsisText);
}

// The selection overlay is created last (hidden) so it renders on top
function appendSelectionOverlay(tableGroup, entity, layout, x, y) {
	const overlay = createSvgElement(
		"rect",
		{
			x,
			y,
			width: layout.tableWidth,
			height: layout.tableHeight,
			"data-entity": entity.type,
		},
		"selection-overlay",
	);
	overlay.style.display = "none";
	tableGroup.appendChild(overlay);
}

function createTableGroup(entity, layout, x, y) {
	const tableGroup = createSvgElement(
		"g",
		{ "data-entity": entity.type },
		"table-group",
	);

	const tableRect = createSvgElement(
		"rect",
		{ x, y, width: layout.tableWidth, height: layout.tableHeight },
		"table-box",
	);
	if (layout.matchingRule) {
		// The "10" suffix gives the background 10% opacity
		tableRect.style.fill = layout.matchingRule.color + "10";
		tableRect.style.stroke = layout.matchingRule.color;
	}
	tableGroup.appendChild(tableRect);
	return tableGroup;
}

function generateTable(svg, entity, x, y) {
	const layout = calculateTableLayout(entity);
	const tableGroup = createTableGroup(entity, layout, x, y);

	appendPropertyBackgrounds(tableGroup, layout, x, y);
	appendHeader(tableGroup, entity, layout, x, y);
	const endY = appendPropertyRows(tableGroup, layout, x, y, entity.type);
	appendEllipsis(tableGroup, layout, x, endY);
	appendSelectionOverlay(tableGroup, entity, layout, x, y);

	setupTableInteraction(tableGroup, entity.type);
	svg.appendChild(tableGroup);
}

function getPropertiesToShow(groupedProperties, maxVisibleRows) {
	const result = [];
	let currentRowCount = 0;

	for (const item of groupedProperties) {
		const size = item.type === "group" ? item.properties.length : 1;
		const remainingRows = maxVisibleRows - currentRowCount;
		if (remainingRows <= 0) break;

		if (size <= remainingRows) {
			result.push(item);
			currentRowCount += size;
		} else {
			// Only a group can be partly shown
			result.push({
				type: "group",
				properties: item.properties.slice(0, remainingRows),
			});
			break;
		}
	}

	return result;
}

function makeClickable(element, resolveTarget) {
	element.style.cursor = "pointer";
	element.addEventListener("click", () => {
		const target = resolveTarget();
		if (target) {
			navigateToEntity(target);
		}
	});
}

function renderLevelIcons(tableGroup, property, context, propertyIcon) {
	createPropertyIcon(propertyIcon, context.x + 10, context.propY).forEach(
		(element) => {
			tableGroup.appendChild(element);

			if (propertyIcon === "FK" && property.isForeignKey) {
				makeClickable(element, () =>
					getNavigationTargetType(property.name, context.entityName),
				);
			}
			if (propertyIcon === "N" && isNavigationProperty(property)) {
				makeClickable(element, () => property.type);
			}
		},
	);
}

function applyPropertyNameStyle(propText, property, entityName) {
	if (property.isInherited) {
		propText.classList.add("property-inherited");
	} else if (property.isKey) {
		propText.classList.add("property-key");
	} else if (property.isForeignKey) {
		propText.classList.add("property-foreign-key");
		makeClickable(propText, () =>
			getNavigationTargetType(property.name, entityName),
		);
	} else if (isNavigationProperty(property)) {
		propText.classList.add("property-navigation");
		makeClickable(propText, () => property.type);
	}
}

// SVG does not support text-decoration properly, so navigation names get a drawn underline
function appendNavigationUnderline(tableGroup, property, nameX, propY) {
	const underline = createSvgElement(
		"line",
		{
			x1: nameX,
			y1: propY + 5,
			x2: nameX + property.name.length * 19,
			y2: propY + 5,
			stroke: "#059669",
			"stroke-width": "2.5",
		},
		"navigation-underline",
	);
	makeClickable(underline, () => property.type);
	tableGroup.appendChild(underline);
}

function renderPropertyName(tableGroup, property, context) {
	const nameX = context.x + TABLE_METRICS.iconWidth + 10;
	const propText = createTextElement(
		{ x: nameX, y: context.propY },
		"property-text",
		property.name,
	);
	applyPropertyNameStyle(propText, property, context.entityName);
	appendTitleTooltip(propText, property.comment);
	tableGroup.appendChild(propText);

	if (isNavigationProperty(property)) {
		appendNavigationUnderline(tableGroup, property, nameX, context.propY);
	}
}

function renderPropertyType(tableGroup, property, context) {
	const padding = TABLE_METRICS.padding;
	const isNavigation = isNavigationProperty(property);
	// Navigation properties always show the navigation arrow icon
	const typeIcon = isNavigation
		? { iconType: "Navigation", isNullable: property.type.endsWith("?") }
		: getTypeIcon(property.type);

	const typeText = createTextElement(
		{
			x: context.x + context.tableWidth - padding - 50,
			y: context.propY,
			"text-anchor": "end",
		},
		"property-text",
		getDisplayType(property),
	);
	typeText.classList.add("property-type", getTypeColorClass(property.type));

	if (property.maxLength && property.maxLength > 0 && !isNavigation) {
		appendTitleTooltip(typeText, `Maximum length: ${property.maxLength}`);
	}
	tableGroup.appendChild(typeText);

	createPropertyIcon(
		typeIcon,
		context.x + context.tableWidth - padding - 40,
		context.propY,
	).forEach((element) => {
		tableGroup.appendChild(element);
		if (isNavigation) {
			makeClickable(element, () => property.type);
		}
	});
}

function renderProperty(tableGroup, property, context) {
	const propertyIcon = getPropertyLevelIcon(property);
	if (propertyIcon) {
		renderLevelIcons(tableGroup, property, context, propertyIcon);
	}

	renderPropertyName(tableGroup, property, context);
	renderPropertyType(tableGroup, property, context);
}

// Navigation target type for a foreign key property
function getNavigationTargetType(fkPropertyName, entityName) {
	const navPropName = fkPropertyName.replace(/Id$/, "");

	const entityData = entities[entityName];
	if (entityData) {
		const allProps = [
			...(entityData.properties || []),
			...(entityData.inheritedProperties || []),
		];
		const navProp = allProps.find(
			(p) => p.name === navPropName && isNavigationProperty(p),
		);
		if (navProp) {
			return navProp.type;
		}
	}

	// Assume the naming convention when no navigation property is found
	return navPropName + "Model";
}
