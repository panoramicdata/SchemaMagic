// Table interaction and dragging
function setupTableInteraction(tableGroup, entityName) {
	let dragData = null;

	tableGroup.addEventListener('mousedown', function(e) {
		e.stopPropagation();
		selectTable(entityName);
		
		// Bring table to front when starting to drag
		bringTableToFront(tableGroup);

		const rect = tableGroup.querySelector('.table-box');

		const svg = document.getElementById('schema-svg');
		const svgRect = svg.getBoundingClientRect();
		const svgX = (e.clientX - svgRect.left) * svgViewBox.width / svgRect.width + svgViewBox.x;
		const svgY = (e.clientY - svgRect.top) * svgViewBox.height / svgRect.height + svgViewBox.y;

		dragData = {
			startX: svgX,
			startY: svgY,
			elementX: parseFloat(rect.getAttribute('x')),
			elementY: parseFloat(rect.getAttribute('y'))
		};

		tableGroup.classList.add('dragging');

		document.addEventListener('mousemove', handleTableDrag);
		document.addEventListener('mouseup', handleTableDragEnd);
	});

	function handleTableDrag(e) {
		if (!dragData) return;

		const svg = document.getElementById('schema-svg');
		const svgRect = svg.getBoundingClientRect();
		const svgX = (e.clientX - svgRect.left) * svgViewBox.width / svgRect.width + svgViewBox.x;
		const svgY = (e.clientY - svgRect.top) * svgViewBox.height / svgRect.height + svgViewBox.y;

		const deltaX = svgX - dragData.startX;
		const deltaY = svgY - dragData.startY;

		let newX = dragData.elementX + deltaX;
		let newY = dragData.elementY + deltaY;

		// Check boundaries and show indicator
		const rect = tableGroup.querySelector('.table-box');
		const tableWidth = parseFloat(rect.getAttribute('width'));
		const tableHeight = parseFloat(rect.getAttribute('height'));

		const boundaryRect = document.getElementById('boundary-rect');
		if (newX < BOUNDARY_MARGIN || newY < BOUNDARY_MARGIN ||
			newX + tableWidth > CANVAS_WIDTH - BOUNDARY_MARGIN ||
			newY + tableHeight > CANVAS_HEIGHT - BOUNDARY_MARGIN) {
			boundaryRect.style.display = 'block';
		} else {
			boundaryRect.style.display = 'none';
		}

		if (snapToGrid) {
			newX = Math.round(newX / 20) * 20;
			newY = Math.round(newY / 20) * 20;
		}

		moveTable(tableGroup, newX, newY);
		updateRelationships();
	}

	function handleTableDragEnd() {
		if (dragData) {
			tableGroup.classList.remove('dragging');
			document.getElementById('boundary-rect').style.display = 'none';
			// Save positions after drag
			saveSettings();
		}
		dragData = null;
		document.removeEventListener('mousemove', handleTableDrag);
		document.removeEventListener('mouseup', handleTableDragEnd);
	}
}

function bringTableToFront(tableGroup) {
	const svg = document.getElementById('schema-svg');
	
	// Remove the table group from its current position
	tableGroup.remove();
	
	// Re-append it to the end (this puts it on top in z-order)
	svg.appendChild(tableGroup);
}

function moveTable(tableGroup, x, y) {
	// FIXED: Include polygon and g elements (for type icons like diamonds and navigation arrows)
	const elements = tableGroup.querySelectorAll('rect, text, line, circle, polygon, g');
	const oldRect = tableGroup.querySelector('.table-box');
	const oldX = parseFloat(oldRect.getAttribute('x'));
	const oldY = parseFloat(oldRect.getAttribute('y'));
	const deltaX = x - oldX;
	const deltaY = y - oldY;

	elements.forEach(element => {
		if (element.tagName === 'line') {
			// Handle line elements
			const x1 = parseFloat(element.getAttribute('x1'));
			const y1 = parseFloat(element.getAttribute('y1'));
			const x2 = parseFloat(element.getAttribute('x2'));
			const y2 = parseFloat(element.getAttribute('y2'));
			element.setAttribute('x1', x1 + deltaX);
			element.setAttribute('y1', y1 + deltaY);
			element.setAttribute('x2', x2 + deltaX);
			element.setAttribute('y2', y2 + deltaY);
		} else if (element.tagName === 'circle') {
			// Handle circle elements (icon backgrounds)
			const cx = parseFloat(element.getAttribute('cx'));
			const cy = parseFloat(element.getAttribute('cy'));
			element.setAttribute('cx', cx + deltaX);
			element.setAttribute('cy', cy + deltaY);
		} else if (element.tagName === 'polygon') {
			// FIXED: Handle polygon elements (enum diamonds, navigation arrows)
			const pointsAttr = element.getAttribute('points');
			if (pointsAttr) {
				// Parse points string: "x1,y1 x2,y2 x3,y3 ..."
				const points = pointsAttr.trim().split(/\s+/).map(pair => {
					const [x, y] = pair.split(',').map(parseFloat);
					return `${x + deltaX},${y + deltaY}`;
				});
				element.setAttribute('points', points.join(' '));
			}
		} else if (element.tagName === 'g') {
			// FIXED: Handle g (group) elements by transforming them
			const currentTransform = element.getAttribute('transform') || '';
			const translateMatch = currentTransform.match(/translate\(([^,]+),([^)]+)\)/);
			
			if (translateMatch) {
				// Update existing translate
				const currentX = parseFloat(translateMatch[1]);
				const currentY = parseFloat(translateMatch[2]);
				const newTransform = currentTransform.replace(
					/translate\([^)]+\)/,
					`translate(${currentX + deltaX},${currentY + deltaY})`
				);
				element.setAttribute('transform', newTransform);
			} else {
				// Add new translate
				const newTransform = currentTransform + ` translate(${deltaX},${deltaY})`;
				element.setAttribute('transform', newTransform.trim());
			}
		} else if (element.tagName === 'rect' || element.tagName === 'text') {
			// Handle rect and text elements
			const currentX = parseFloat(element.getAttribute('x'));
			const currentY = parseFloat(element.getAttribute('y'));
			element.setAttribute('x', currentX + deltaX);
			element.setAttribute('y', currentY + deltaY);
		}
	});
}

function selectTable(entityName) {
	// Hide all selection overlays
	document.querySelectorAll('.selection-overlay').forEach(overlay => {
		overlay.style.display = 'none';
	});

	// Clear selected class from all table boxes
	document.querySelectorAll('.table-box.selected').forEach(box => {
		box.classList.remove('selected');
	});

	const tableGroup = document.querySelector('[data-entity="' + entityName + '"]');
	if (tableGroup) {
		const tableBox = tableGroup.querySelector('.table-box');
		tableBox.classList.add('selected');
		selectedTable = entityName;

		// Show the selection overlay for this table
		const selectionOverlay = tableGroup.querySelector('.selection-overlay');
		if (selectionOverlay) {
			selectionOverlay.style.display = 'block';
		}

		// Enable "show only selected relations" mode when a table is selected
		showOnlySelectedRelations = true;
		updateRelationships();
		
		console.log(`?? Selected table: ${entityName} - highlighting related connections`);
	}
}

function clearSelection() {
	// Hide all selection overlays
	document.querySelectorAll('.selection-overlay').forEach(overlay => {
		overlay.style.display = 'none';
	});

	// Clear selected class from all table boxes
	document.querySelectorAll('.table-box.selected').forEach(box => {
		box.classList.remove('selected');
	});

	selectedTable = null;
	showOnlySelectedRelations = false;
	updateRelationships();
	
	console.log('?? Selection cleared - showing all relationships normally');
}