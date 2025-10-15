// Table Grouping Rules Management

// Open the table grouping dialog
function openTableGroupingDialog() {
	// Automatically remove unused rules before populating UI
	autoRemoveUnusedRules();
	
	populateRulesUI();
	analyzeUnusedRules(); // Analyze again to show any remaining issues
	document.getElementById('grouping-modal-overlay').style.display = 'block';
	document.getElementById('grouping-modal').style.display = 'block';
}

// Close the table grouping dialog
function closeTableGroupingDialog() {
	document.getElementById('grouping-modal-overlay').style.display = 'none';
	document.getElementById('grouping-modal').style.display = 'none';
}

// Automatically remove unused rules without confirmation
function autoRemoveUnusedRules() {
	const entityNames = Object.keys(entities);
	const rulesToKeep = [];
	let removedCount = 0;
	
	tableGroupingRules.forEach((rule, index) => {
		// Always keep the default catch-all rule (usually last)
		if (rule.pattern === '.*') {
			rulesToKeep.push(rule);
			return;
		}
		
		try {
			const regex = new RegExp(rule.pattern, 'i');
			const matches = entityNames.filter(name => regex.test(name));
			
			if (matches.length === 0) {
				// Rule doesn't match anything - skip it (don't add to rulesToKeep)
				removedCount++;
				console.log(`??? Auto-removed unused rule: "${rule.name}" (pattern: ${rule.pattern})`);
			} else {
				// Rule matches at least one table - keep it
				rulesToKeep.push(rule);
			}
		} catch (e) {
			// Invalid regex - remove it
			removedCount++;
			console.warn(`??? Auto-removed invalid rule "${rule.name}": ${e.message}`);
		}
	});
	
	if (removedCount > 0) {
		tableGroupingRules = rulesToKeep;
		saveTableGroupingRulesToStorage();
		console.log(`? Auto-removed ${removedCount} unused rule(s). ${tableGroupingRules.length} rules remain.`);
	} else {
		console.log(`? All ${tableGroupingRules.length} rules are in use - no cleanup needed`);
	}
}

// Analyze which rules don't match any tables (for informational purposes only)
function analyzeUnusedRules() {
	const entityNames = Object.keys(entities);
	const unusedRules = [];
	
	tableGroupingRules.forEach((rule, index) => {
		// Skip the default catch-all rule (usually last)
		if (rule.pattern === '.*') {
			return;
		}
		
		try {
			const regex = new RegExp(rule.pattern, 'i');
			const matches = entityNames.filter(name => regex.test(name));
			
			if (matches.length === 0) {
				unusedRules.push({
					index: index,
					rule: rule,
					name: rule.name,
					pattern: rule.pattern
				});
			}
		} catch (e) {
			console.warn(`Invalid regex pattern in rule "${rule.name}": ${rule.pattern}`, e);
		}
	});
	
	// Only show notification if there are STILL unused rules after auto-cleanup
	// (This should rarely happen, but good to have for edge cases)
	if (unusedRules.length > 0) {
		console.log(`?? Found ${unusedRules.length} unused rules after auto-cleanup (edge case):`);
		unusedRules.forEach(item => {
			console.log(`   - "${item.name}" (pattern: ${item.pattern})`);
		});
		showUnusedRulesNotification(unusedRules);
	} else {
		console.log('? All rules match at least one table');
		closeUnusedRulesNotification();
	}
	
	return unusedRules;
}

// Show notification about unused rules (only for edge cases now)
function showUnusedRulesNotification(unusedRules) {
	// Check if notification element already exists
	let notification = document.getElementById('unused-rules-notification');
	if (!notification) {
		notification = document.createElement('div');
		notification.id = 'unused-rules-notification';
		notification.className = 'unused-rules-notification';
		
		// Insert after modal description
		const modalBody = document.querySelector('#grouping-modal .modal-body');
		const description = modalBody.querySelector('.modal-description');
		description.insertAdjacentElement('afterend', notification);
	}
	
	// Build notification content
	const rulesList = unusedRules.map(item => 
		`<li>
			<strong>${item.name}</strong> 
			<span class="rule-pattern-badge">${item.pattern}</span>
			<button class="btn-delete-unused" onclick="deleteRule(${item.index})" title="Delete this rule">
				<i class="fas fa-trash"></i> Delete
			</button>
		</li>`
	).join('');
	
	notification.innerHTML = `
		<div class="notification-header">
			<i class="fas fa-info-circle"></i>
			<strong>Unused Rules Detected (${unusedRules.length})</strong>
			<button class="btn-close-notification" onclick="closeUnusedRulesNotification()">
				<i class="fas fa-times"></i>
			</button>
		</div>
		<div class="notification-body">
			<p>These rules don't match any tables. They were kept because they might be for future use:</p>
			<ul class="unused-rules-list">
				${rulesList}
			</ul>
			<button class="btn-remove-all-unused" onclick="removeAllUnusedRules()">
				<i class="fas fa-trash-alt"></i> Remove All Unused Rules
			</button>
		</div>
	`;
	
	notification.style.display = 'block';
}

// Close the unused rules notification
function closeUnusedRulesNotification() {
	const notification = document.getElementById('unused-rules-notification');
	if (notification) {
		notification.style.display = 'none';
	}
}

// Remove all unused rules at once (still available for manual use)
function removeAllUnusedRules() {
	const unusedRules = analyzeUnusedRules();
	
	if (unusedRules.length === 0) {
		alert('No unused rules to remove!');
		return;
	}
	
	// Sort indices in descending order to avoid index shifting issues
	const indicesToRemove = unusedRules.map(item => item.index).sort((a, b) => b - a);
	
	indicesToRemove.forEach(index => {
		tableGroupingRules.splice(index, 1);
	});
	
	console.log(`? Manually removed ${indicesToRemove.length} unused rules`);
	
	// Refresh the UI
	populateRulesUI();
	closeUnusedRulesNotification();
	
	alert(`Successfully removed ${indicesToRemove.length} unused rule(s)!`);
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
	div.setAttribute('draggable', 'true'); // Make the element draggable
	
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

// Initialize drag-and-drop sorting using HTML5 Drag and Drop API
function initializeSortable() {
	const container = document.getElementById('rules-container');
	let draggedElement = null;
	
	container.querySelectorAll('.rule-item').forEach(item => {
		// Drag start - store the dragged element
		item.addEventListener('dragstart', (e) => {
			draggedElement = item;
			item.classList.add('dragging');
			e.dataTransfer.effectAllowed = 'move';
			e.dataTransfer.setData('text/html', item.innerHTML);
		});
		
		// Drag end - clean up
		item.addEventListener('dragend', (e) => {
			item.classList.remove('dragging');
		});
		
		// Drag over - allow dropping
		item.addEventListener('dragover', (e) => {
			e.preventDefault();
			e.dataTransfer.dropEffect = 'move';
			
			if (draggedElement && draggedElement !== item) {
				const bounding = item.getBoundingClientRect();
				const offset = e.clientY - bounding.top;
				
				// Insert before or after based on mouse position
				if (offset > bounding.height / 2) {
					item.parentNode.insertBefore(draggedElement, item.nextSibling);
				} else {
					item.parentNode.insertBefore(draggedElement, item);
				}
			}
		});
		
		// Drop - finalize the drop
		item.addEventListener('drop', (e) => {
			e.preventDefault();
			e.stopPropagation();
		});
	});
	
	// Also handle drag over the container itself
	container.addEventListener('dragover', (e) => {
		e.preventDefault();
		e.dataTransfer.dropEffect = 'move';
	});
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
	analyzeUnusedRules(); // Re-analyze after adding
}

function deleteRule(index) {
	if (confirm(`Delete rule "${tableGroupingRules[index].name}"?`)) {
		tableGroupingRules.splice(index, 1);
		populateRulesUI();
		analyzeUnusedRules(); // Re-analyze after deleting
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
			matchesContainer.innerHTML = '<div class="test-result-none">?? No tables match this pattern</div>';
		} else {
			matchesContainer.innerHTML = `
				<div class="test-result-success">
					<strong>? Matches ${matches.length} table(s):</strong>
					${matches.slice(0, 10).join(', ')}${matches.length > 10 ? ` ... and ${matches.length - 10} more` : ''}
				</div>
			`;
		}
	} catch (e) {
		matchesContainer.innerHTML = `<div class="test-result-error">? Invalid regex: ${e.message}</div>`;
	}
}

function resetToDefaultRules() {
	if (confirm('Reset all rules to default settings? This will overwrite your current rules.')) {
		tableGroupingRules = [...DEFAULT_TABLE_GROUPING_RULES];
		saveTableGroupingRulesToStorage();
		
		// Auto-remove unused defaults immediately
		autoRemoveUnusedRules();
		
		populateRulesUI();
		analyzeUnusedRules(); // Re-analyze after reset
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
		analyzeUnusedRules(); // Re-analyze after icon change
	}
	closeIconPicker();
}
