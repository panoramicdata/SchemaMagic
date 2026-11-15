// Table interaction and dragging
const GRID_SIZE = 20;

// Looks the table up by comparing the attribute, so entity names never reach a selector
function findTableGroup(entityName) {
	return (
		Array.from(document.querySelectorAll("[data-entity]")).find(
			(element) => element.getAttribute("data-entity") === entityName,
		) || null
	);
}

function getSvgPointFromMouse(e) {
	const svgRect = document.getElementById("schema-svg").getBoundingClientRect();
	return {
		x:
			((e.clientX - svgRect.left) * svgViewBox.width) / svgRect.width +
			svgViewBox.x,
		y:
			((e.clientY - svgRect.top) * svgViewBox.height) / svgRect.height +
			svgViewBox.y,
	};
}

function isOutsideBoundary(x, y, width, height) {
	return (
		x < BOUNDARY_MARGIN ||
		y < BOUNDARY_MARGIN ||
		x + width > CANVAS_WIDTH - BOUNDARY_MARGIN ||
		y + height > CANVAS_HEIGHT - BOUNDARY_MARGIN
	);
}

function setupTableInteraction(tableGroup, entityName) {
	let dragData = null;

	function handleTableDrag(e) {
		if (!dragData) return;

		const point = getSvgPointFromMouse(e);
		let newX = dragData.elementX + point.x - dragData.startX;
		let newY = dragData.elementY + point.y - dragData.startY;

		const rect = tableGroup.querySelector(".table-box");
		const tableWidth = parseFloat(rect.getAttribute("width"));
		const tableHeight = parseFloat(rect.getAttribute("height"));

		document.getElementById("boundary-rect").style.display = isOutsideBoundary(
			newX,
			newY,
			tableWidth,
			tableHeight,
		)
			? "block"
			: "none";

		if (snapToGrid) {
			newX = Math.round(newX / GRID_SIZE) * GRID_SIZE;
			newY = Math.round(newY / GRID_SIZE) * GRID_SIZE;
		}

		moveTable(tableGroup, newX, newY);
		updateRelationships();
	}

	function handleTableDragEnd() {
		if (dragData) {
			tableGroup.classList.remove("dragging");
			document.getElementById("boundary-rect").style.display = "none";
			saveSettings();
		}
		dragData = null;
		document.removeEventListener("mousemove", handleTableDrag);
		document.removeEventListener("mouseup", handleTableDragEnd);
	}

	tableGroup.addEventListener("mousedown", function (e) {
		e.stopPropagation();
		selectTable(entityName);
		bringTableToFront(tableGroup);

		const rect = tableGroup.querySelector(".table-box");
		const point = getSvgPointFromMouse(e);
		dragData = {
			startX: point.x,
			startY: point.y,
			elementX: parseFloat(rect.getAttribute("x")),
			elementY: parseFloat(rect.getAttribute("y")),
		};

		tableGroup.classList.add("dragging");
		document.addEventListener("mousemove", handleTableDrag);
		document.addEventListener("mouseup", handleTableDragEnd);
	});
}

// Re-appending puts the table last, which is on top in z-order
function bringTableToFront(tableGroup) {
	const svg = document.getElementById("schema-svg");
	tableGroup.remove();
	svg.appendChild(tableGroup);
}

function shiftAttributes(element, names, delta) {
	names.forEach((name) => {
		const value = parseFloat(element.getAttribute(name));
		element.setAttribute(name, value + (name.startsWith("x") || name === "cx" ? delta.x : delta.y));
	});
}

function shiftPolygon(element, delta) {
	const pointsAttr = element.getAttribute("points");
	if (!pointsAttr) return;

	const points = pointsAttr
		.trim()
		.split(/\s+/)
		.map((pair) => {
			const [px, py] = pair.split(",").map(parseFloat);
			return `${px + delta.x},${py + delta.y}`;
		});
	element.setAttribute("points", points.join(" "));
}

function shiftGroup(element, delta) {
	const currentTransform = element.getAttribute("transform") || "";
	const translateMatch = currentTransform.match(/translate\(([^,]+),([^)]+)\)/);

	if (translateMatch) {
		const currentX = parseFloat(translateMatch[1]);
		const currentY = parseFloat(translateMatch[2]);
		element.setAttribute(
			"transform",
			currentTransform.replace(
				/translate\([^)]+\)/,
				`translate(${currentX + delta.x},${currentY + delta.y})`,
			),
		);
	} else {
		element.setAttribute(
			"transform",
			`${currentTransform} translate(${delta.x},${delta.y})`.trim(),
		);
	}
}

const ELEMENT_SHIFTERS = {
	line: (element, delta) => shiftAttributes(element, ["x1", "y1", "x2", "y2"], delta),
	circle: (element, delta) => shiftAttributes(element, ["cx", "cy"], delta),
	polygon: shiftPolygon,
	g: shiftGroup,
	rect: (element, delta) => shiftAttributes(element, ["x", "y"], delta),
	text: (element, delta) => shiftAttributes(element, ["x", "y"], delta),
};

function moveTable(tableGroup, x, y) {
	const elements = tableGroup.querySelectorAll(
		"rect, text, line, circle, polygon, g",
	);
	const oldRect = tableGroup.querySelector(".table-box");
	const delta = {
		x: x - parseFloat(oldRect.getAttribute("x")),
		y: y - parseFloat(oldRect.getAttribute("y")),
	};

	elements.forEach((element) => {
		const shift = ELEMENT_SHIFTERS[element.tagName];
		if (shift) {
			shift(element, delta);
		}
	});
}

function clearSelectionVisuals() {
	document.querySelectorAll(".selection-overlay").forEach((overlay) => {
		overlay.style.display = "none";
	});
	document.querySelectorAll(".table-box.selected").forEach((box) => {
		box.classList.remove("selected");
	});
}

function selectTable(entityName) {
	clearSelectionVisuals();

	const tableGroup = findTableGroup(entityName);
	if (!tableGroup) return;

	tableGroup.querySelector(".table-box").classList.add("selected");
	selectedTable = entityName;

	const selectionOverlay = tableGroup.querySelector(".selection-overlay");
	if (selectionOverlay) {
		selectionOverlay.style.display = "block";
	}

	showOnlySelectedRelations = true;
	updateRelationships();
}

function clearSelection() {
	clearSelectionVisuals();
	selectedTable = null;
	showOnlySelectedRelations = false;
	updateRelationships();
}
