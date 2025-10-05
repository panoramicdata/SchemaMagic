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
	const elements = tableGroup.querySelectorAll('rect, text, line, circle');
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