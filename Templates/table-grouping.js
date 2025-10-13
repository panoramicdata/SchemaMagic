// Table Grouping Rules Management

// Open the table grouping dialog
function openTableGroupingDialog() {
	populateRulesUI();
	document.getElementById('grouping-modal-overlay').style.display = 'block';
	document.getElementById('grouping-modal').style.display = 'block';
}

// Close the table grouping dialog
function closeTableGroupingDialog() {
	document.getElementById('grouping-modal-overlay').style.display = 'none';
	document.getElementById('grouping-modal').style.display = 'none';
}

// Populate the rules UI
function populateRulesUI() {
	const container = document.getElementById('rules-container');
	container.innerHTML = '';
	
	tableGroupingRules.forEach((rule, index) => {
		const ruleElement = createRuleElement(rule, index);
		container.appendChild(ruleElement);
	});
	
	// Initialize sortable for drag-and-drop reordering
	initializeSortable();
}

// Create a single rule element
function createRuleElement(rule, index) {
	const div = document.createElement('div');
	div.className = 'rule-item';
	div.dataset.index = index;
	
	div.innerHTML = `
		<div class="rule-drag-handle" title="Drag to reorder">
			<i class="fas fa-grip-vertical"></i>
		</div>
		<div class="rule-content">
			<div class="rule-row">
				<input type="checkbox" 
					   id="rule-enabled-${index}" 
					   ${rule.enabled ? 'checked' : ''} 
					   onchange="toggleRuleEnabled(${index})"
					   class="rule-checkbox">
				<input type="text" 
					   value="${rule.name}" 
					   placeholder="Rule Name" 
					   onchange="updateRuleName(${index}, this.value)"
					   class="rule-name-input">
				<button class="btn-icon-picker" onclick="openIconPicker(${index})" title="Select Icon">
					<i class="${rule.icon}"></i>
				</button>
				<input type="color" 
					   value="${rule.color}" 
					   onchange="updateRuleColor(${index}, this.value)"
					   class="rule-color-input"
					   title="Choose Color">
				<button class="btn-delete-rule" onclick="deleteRule(${index})" title="Delete Rule">
					<i class="fas fa-trash"></i>
				</button>
			</div>
			<div class="rule-row">
				<input type="text" 
					   value="${rule.pattern}" 
					   placeholder="Regex Pattern (e.g., User|Customer|Person)" 
					   onchange="updateRulePattern(${index}, this.value)"
					   class="rule-pattern-input">
				<button class="btn-test-pattern" onclick="testPattern(${index})" title="Test Pattern">
					<i class="fas fa-flask"></i> Test
				</button>
			</div>
			<div class="rule-matches" id="rule-matches-${index}"></div>
		</div>
	`;
	
	return div;
}

// Initialize drag-and-drop sorting
function initializeSortable() {
	const container = document.getElementById('rules-container');
	let draggedElement = null;
	
	container.querySelectorAll('.rule-item').forEach(item => {
		const handle = item.querySelector('.rule-drag-handle');
		
		handle.addEventListener('mousedown', (e) => {
			draggedElement = item;
			item.classList.add('dragging');
			e.preventDefault();
		});
	});
	
	document.addEventListener('mouseup', () => {
		if (draggedElement) {
			draggedElement.classList.remove('dragging');
			draggedElement = null;
		}
	});
	
	container.addEventListener('dragover', (e) => {
		e.preventDefault();
		if (!draggedElement) return;
		
		const afterElement = getDragAfterElement(container, e.clientY);
		if (afterElement == null) {
			container.appendChild(draggedElement);
		} else {
			container.insertBefore(draggedElement, afterElement);
		}
	});
}

function getDragAfterElement(container, y) {
	const draggableElements = [...container.querySelectorAll('.rule-item:not(.dragging)')];
	
	return draggableElements.reduce((closest, child) => {
		const box = child.getBoundingClientRect();
		const offset = y - box.top - box.height / 2;
		
		if (offset < 0 && offset > closest.offset) {
			return { offset: offset, element: child };
		} else {
			return closest;
		}
	}, { offset: Number.NEGATIVE_INFINITY }).element;
}

// Rule CRUD operations
function addNewRule() {
	const newRule = {
		name: 'New Rule',
		pattern: '.*',
		icon: 'fa-database',
		color: '#3b82f6',
		enabled: true
	};
	
	tableGroupingRules.push(newRule);
	populateRulesUI();
}

function deleteRule(index) {
	if (confirm(`Delete rule "${tableGroupingRules[index].name}"?`)) {
		tableGroupingRules.splice(index, 1);
		populateRulesUI();
	}
}

function toggleRuleEnabled(index) {
	tableGroupingRules[index].enabled = !tableGroupingRules[index].enabled;
}

function updateRuleName(index, value) {
	tableGroupingRules[index].name = value;
}

function updateRulePattern(index, value) {
	tableGroupingRules[index].pattern = value;
}

function updateRuleColor(index, value) {
	tableGroupingRules[index].color = value;
}

function testPattern(index) {
	const rule = tableGroupingRules[index];
	const matchesContainer = document.getElementById(`rule-matches-${index}`);
	
	try {
		const regex = new RegExp(rule.pattern, 'i');
		const matches = Object.keys(entities).filter(name => regex.test(name));
		
		if (matches.length === 0) {
			matchesContainer.innerHTML = '<div class="test-result-none">No tables match this pattern</div>';
		} else {
			matchesContainer.innerHTML = `
				<div class="test-result-success">
					<strong>Matches ${matches.length} table(s):</strong>
					${matches.slice(0, 10).join(', ')}${matches.length > 10 ? '...' : ''}
				</div>
			`;
		}
	} catch (e) {
		matchesContainer.innerHTML = `<div class="test-result-error">Invalid regex: ${e.message}</div>`;
	}
}

function resetToDefaultRules() {
	if (confirm('Reset all rules to default settings? This will overwrite your current rules.')) {
		tableGroupingRules = [...DEFAULT_TABLE_GROUPING_RULES];
		populateRulesUI();
	}
}

function saveTableGroupingRules() {
	// Reorder rules based on current DOM order
	const container = document.getElementById('rules-container');
	const orderedRules = [];
	
	container.querySelectorAll('.rule-item').forEach(item => {
		const index = parseInt(item.dataset.index);
		orderedRules.push(tableGroupingRules[index]);
	});
	
	tableGroupingRules = orderedRules;
	saveTableGroupingRulesToStorage();
	
	// Regenerate schema with new rules
	generateSchema();
	
	closeTableGroupingDialog();
	
	alert('Table grouping rules saved successfully!');
}

// Icon Picker Functions
function openIconPicker(ruleIndex) {
	currentEditingRuleIndex = ruleIndex;
	populateIconPicker();
	document.getElementById('icon-picker-overlay').style.display = 'block';
	document.getElementById('icon-picker-modal').style.display = 'block';
}

function closeIconPicker() {
	currentEditingRuleIndex = null;
	document.getElementById('icon-picker-overlay').style.display = 'none';
	document.getElementById('icon-picker-modal').style.display = 'none';
}

function populateIconPicker() {
	const grid = document.getElementById('icon-grid');
	grid.innerHTML = '';
	
	AVAILABLE_ICONS.forEach(icon => {
		const iconElement = document.createElement('div');
		iconElement.className = 'icon-item';
		iconElement.innerHTML = `<i class="${icon}"></i>`;
		iconElement.onclick = () => selectIcon(icon);
		grid.appendChild(iconElement);
	});
}

function filterIcons(searchTerm) {
	const grid = document.getElementById('icon-grid');
	const items = grid.querySelectorAll('.icon-item');
	const term = searchTerm.toLowerCase();
	
	items.forEach(item => {
		const iconClass = item.querySelector('i').className;
		if (iconClass.toLowerCase().includes(term)) {
			item.style.display = 'block';
		} else {
			item.style.display = 'none';
		}
	});
}

function selectIcon(iconClass) {
	if (currentEditingRuleIndex !== null) {
		tableGroupingRules[currentEditingRuleIndex].icon = iconClass;
		populateRulesUI();
	}
	closeIconPicker();
}
