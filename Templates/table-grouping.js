// Table Grouping Rules Management

function createElement(tag, className, text) {
	const element = document.createElement(tag);
	if (className) {
		element.className = className;
	}
	if (text !== undefined) {
		element.textContent = text;
	}
	return element;
}

function createIconButton(className, title, iconClass, label, onClick) {
	const button = createElement("button", className);
	button.title = title;
	button.appendChild(createElement("i", iconClass));
	if (label) {
		button.appendChild(document.createTextNode(` ${label}`));
	}
	button.addEventListener("click", onClick);
	return button;
}

function setModalVisibility(ids, display) {
	ids.forEach((id) => {
		document.getElementById(id).style.display = display;
	});
}

const GROUPING_MODAL_IDS = ["grouping-modal-overlay", "grouping-modal"];
const ICON_PICKER_IDS = ["icon-picker-overlay", "icon-picker-modal"];

// Open the table grouping dialog
function openTableGroupingDialog() {
	autoRemoveUnusedRules();
	populateRulesUI();
	analyzeUnusedRules();
	setModalVisibility(GROUPING_MODAL_IDS, "block");
}

function closeTableGroupingDialog() {
	setModalVisibility(GROUPING_MODAL_IDS, "none");
}

// Returns the entity names matched by a rule, or null when its pattern is invalid
function findRuleMatches(rule, entityNames) {
	try {
		const regex = compileRulePattern(rule.pattern);
		return entityNames.filter((name) => regex.test(name));
	} catch (e) {
		return null;
	}
}

// Automatically remove unused rules without confirmation
function autoRemoveUnusedRules() {
	const entityNames = Object.keys(entities);
	const rulesToKeep = tableGroupingRules.filter((rule) => {
		if (rule.pattern === ".*") {
			return true;
		}
		const matches = findRuleMatches(rule, entityNames);
		return matches !== null && matches.length > 0;
	});

	const removedCount = tableGroupingRules.length - rulesToKeep.length;
	if (removedCount > 0) {
		tableGroupingRules = rulesToKeep;
		saveTableGroupingRulesToStorage();
	}
	console.log("Auto-removed unused rules:", removedCount);
}

// Analyze which rules don't match any tables (for informational purposes only)
function analyzeUnusedRules() {
	const entityNames = Object.keys(entities);
	const unusedRules = [];

	tableGroupingRules.forEach((rule, index) => {
		if (rule.pattern === ".*") {
			return;
		}
		const matches = findRuleMatches(rule, entityNames);
		if (matches !== null && matches.length === 0) {
			unusedRules.push({
				index: index,
				rule: rule,
				name: rule.name,
				pattern: rule.pattern,
			});
		}
	});

	if (unusedRules.length > 0) {
		showUnusedRulesNotification(unusedRules);
	} else {
		closeUnusedRulesNotification();
	}

	return unusedRules;
}

function getOrCreateUnusedRulesNotification() {
	let notification = document.getElementById("unused-rules-notification");
	if (!notification) {
		notification = createElement("div", "unused-rules-notification");
		notification.id = "unused-rules-notification";
		const modalBody = document.querySelector("#grouping-modal .modal-body");
		const description = modalBody.querySelector(".modal-description");
		description.insertAdjacentElement("afterend", notification);
	}
	return notification;
}

function createUnusedRuleItem(item) {
	const li = createElement("li");
	li.appendChild(createElement("strong", "", item.name));
	li.appendChild(document.createTextNode(" "));
	li.appendChild(createElement("span", "rule-pattern-badge", item.pattern));
	li.appendChild(
		createIconButton(
			"btn-delete-unused",
			"Delete this rule",
			"fas fa-trash",
			"Delete",
			() => deleteRule(item.index),
		),
	);
	return li;
}

function createNotificationHeader(count) {
	const header = createElement("div", "notification-header");
	header.appendChild(createElement("i", "fas fa-info-circle"));
	header.appendChild(
		createElement("strong", "", `Unused Rules Detected (${count})`),
	);
	header.appendChild(
		createIconButton(
			"btn-close-notification",
			"",
			"fas fa-times",
			"",
			closeUnusedRulesNotification,
		),
	);
	return header;
}

function createNotificationBody(unusedRules) {
	const body = createElement("div", "notification-body");
	body.appendChild(
		createElement(
			"p",
			"",
			"These rules don't match any tables. They were kept because they might be for future use:",
		),
	);
	const list = createElement("ul", "unused-rules-list");
	unusedRules.forEach((item) => list.appendChild(createUnusedRuleItem(item)));
	body.appendChild(list);
	body.appendChild(
		createIconButton(
			"btn-remove-all-unused",
			"",
			"fas fa-trash-alt",
			"Remove All Unused Rules",
			removeAllUnusedRules,
		),
	);
	return body;
}

// Show notification about unused rules (only for edge cases now)
function showUnusedRulesNotification(unusedRules) {
	const notification = getOrCreateUnusedRulesNotification();
	notification.replaceChildren(
		createNotificationHeader(unusedRules.length),
		createNotificationBody(unusedRules),
	);
	notification.style.display = "block";
}

function closeUnusedRulesNotification() {
	const notification = document.getElementById("unused-rules-notification");
	if (notification) {
		notification.style.display = "none";
	}
}

// Remove all unused rules at once (still available for manual use)
function removeAllUnusedRules() {
	const unusedRules = analyzeUnusedRules();

	if (unusedRules.length === 0) {
		alert("No unused rules to remove!");
		return;
	}

	// Descending order avoids index shifting while splicing
	const indicesToRemove = unusedRules
		.map((item) => item.index)
		.sort((a, b) => b - a);

	indicesToRemove.forEach((index) => {
		tableGroupingRules.splice(index, 1);
	});

	populateRulesUI();
	closeUnusedRulesNotification();

	alert(`Successfully removed ${indicesToRemove.length} unused rule(s)!`);
}

function populateRulesUI() {
	const container = document.getElementById("rules-container");
	container.replaceChildren();

	tableGroupingRules.forEach((rule, index) => {
		container.appendChild(createRuleElement(rule, index));
	});

	initializeSortable();
}

function createRuleInput(type, className, value, onChange) {
	const input = createElement("input", className);
	input.type = type;
	input.value = value;
	input.addEventListener("change", () => onChange(input.value));
	return input;
}

function createRuleHeaderRow(rule, index) {
	const row = createElement("div", "rule-row");

	const checkbox = createElement("input", "rule-checkbox");
	checkbox.type = "checkbox";
	checkbox.id = `rule-enabled-${index}`;
	checkbox.checked = Boolean(rule.enabled);
	checkbox.addEventListener("change", () => toggleRuleEnabled(index));
	row.appendChild(checkbox);

	const nameInput = createRuleInput("text", "rule-name-input", rule.name, (v) =>
		updateRuleName(index, v),
	);
	nameInput.placeholder = "Rule Name";
	row.appendChild(nameInput);

	row.appendChild(
		createIconButton(
			"btn-icon-picker",
			"Select Icon",
			String(rule.icon),
			"",
			() => openIconPicker(index),
		),
	);

	const colorInput = createRuleInput("color", "rule-color-input", rule.color, (v) =>
		updateRuleColor(index, v),
	);
	colorInput.title = "Choose Color";
	row.appendChild(colorInput);

	row.appendChild(
		createIconButton(
			"btn-delete-rule",
			"Delete Rule",
			"fas fa-trash",
			"",
			() => deleteRule(index),
		),
	);
	return row;
}

function createRulePatternRow(rule, index) {
	const row = createElement("div", "rule-row");
	const patternInput = createRuleInput(
		"text",
		"rule-pattern-input",
		rule.pattern,
		(v) => updateRulePattern(index, v),
	);
	patternInput.placeholder = "Regex Pattern (e.g., User|Customer|Person)";
	row.appendChild(patternInput);
	row.appendChild(
		createIconButton(
			"btn-test-pattern",
			"Test Pattern",
			"fas fa-flask",
			"Test",
			() => testPattern(index),
		),
	);
	return row;
}

function createRuleElement(rule, index) {
	const div = createElement("div", "rule-item");
	div.dataset.index = index;
	div.setAttribute("draggable", "true");

	const handle = createElement("div", "rule-drag-handle");
	handle.title = "Drag to reorder";
	handle.appendChild(createElement("i", "fas fa-grip-vertical"));
	div.appendChild(handle);

	const content = createElement("div", "rule-content");
	content.appendChild(createRuleHeaderRow(rule, index));
	content.appendChild(createRulePatternRow(rule, index));
	const matches = createElement("div", "rule-matches");
	matches.id = `rule-matches-${index}`;
	content.appendChild(matches);
	div.appendChild(content);

	return div;
}

function moveDraggedElement(draggedElement, item, event) {
	const bounding = item.getBoundingClientRect();
	const offset = event.clientY - bounding.top;
	const before = offset > bounding.height / 2 ? item.nextSibling : item;
	item.parentNode.insertBefore(draggedElement, before);
}

function attachDragHandlers(item, getDragged, setDragged) {
	item.addEventListener("dragstart", (e) => {
		setDragged(item);
		item.classList.add("dragging");
		e.dataTransfer.effectAllowed = "move";
		e.dataTransfer.setData("text/plain", item.dataset.index);
	});

	item.addEventListener("dragend", () => {
		item.classList.remove("dragging");
	});

	item.addEventListener("dragover", (e) => {
		e.preventDefault();
		e.dataTransfer.dropEffect = "move";
		const dragged = getDragged();
		if (dragged && dragged !== item) {
			moveDraggedElement(dragged, item, e);
		}
	});

	item.addEventListener("drop", (e) => {
		e.preventDefault();
		e.stopPropagation();
	});
}

// Drag-and-drop sorting using the HTML5 Drag and Drop API
function initializeSortable() {
	const container = document.getElementById("rules-container");
	let draggedElement = null;

	container.querySelectorAll(".rule-item").forEach((item) => {
		attachDragHandlers(
			item,
			() => draggedElement,
			(element) => {
				draggedElement = element;
			},
		);
	});

	container.addEventListener("dragover", (e) => {
		e.preventDefault();
		e.dataTransfer.dropEffect = "move";
	});
}

function addNewRule() {
	tableGroupingRules.push({
		name: "New Rule",
		pattern: ".*",
		icon: "fa-database",
		color: "#3b82f6",
		enabled: true,
	});
	populateRulesUI();
	analyzeUnusedRules();
}

function deleteRule(index) {
	if (confirm(`Delete rule "${tableGroupingRules[index].name}"?`)) {
		tableGroupingRules.splice(index, 1);
		populateRulesUI();
		analyzeUnusedRules();
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

function showTestResult(container, className, text) {
	container.replaceChildren(createElement("div", className, text));
}

function describeMatches(matches) {
	const shown = matches.slice(0, 10).join(", ");
	const extra =
		matches.length > 10 ? ` ... and ${matches.length - 10} more` : "";
	return `Matches ${matches.length} table(s): ${shown}${extra}`;
}

function testPattern(index) {
	const rule = tableGroupingRules[index];
	const matchesContainer = document.getElementById(`rule-matches-${index}`);
	const matches = findRuleMatches(rule, Object.keys(entities));

	if (matches === null) {
		showTestResult(matchesContainer, "test-result-error", "Invalid regex");
	} else if (matches.length === 0) {
		showTestResult(
			matchesContainer,
			"test-result-none",
			"No tables match this pattern",
		);
	} else {
		showTestResult(
			matchesContainer,
			"test-result-success",
			describeMatches(matches),
		);
	}
}

function resetToDefaultRules() {
	if (
		confirm(
			"Reset all rules to default settings? This will overwrite your current rules.",
		)
	) {
		tableGroupingRules = [...DEFAULT_TABLE_GROUPING_RULES];
		saveTableGroupingRulesToStorage();
		autoRemoveUnusedRules();
		populateRulesUI();
		analyzeUnusedRules();
	}
}

function saveTableGroupingRules() {
	const container = document.getElementById("rules-container");
	const orderedRules = [];

	container.querySelectorAll(".rule-item").forEach((item) => {
		const index = parseInt(item.dataset.index, 10);
		orderedRules.push(tableGroupingRules[index]);
	});

	tableGroupingRules = orderedRules;
	saveTableGroupingRulesToStorage();
	generateSchema();
	closeTableGroupingDialog();
	alert("Table grouping rules saved successfully!");
}

// Icon Picker Functions
function openIconPicker(ruleIndex) {
	currentEditingRuleIndex = ruleIndex;
	populateIconPicker();
	setModalVisibility(ICON_PICKER_IDS, "block");
}

function closeIconPicker() {
	currentEditingRuleIndex = null;
	setModalVisibility(ICON_PICKER_IDS, "none");
}

function populateIconPicker() {
	const grid = document.getElementById("icon-grid");
	grid.replaceChildren();

	AVAILABLE_ICONS.forEach((icon) => {
		const iconElement = createElement("div", "icon-item");
		iconElement.appendChild(createElement("i", String(icon)));
		iconElement.onclick = () => selectIcon(icon);
		grid.appendChild(iconElement);
	});
}

function filterIcons(searchTerm) {
	const grid = document.getElementById("icon-grid");
	const term = searchTerm.toLowerCase();

	grid.querySelectorAll(".icon-item").forEach((item) => {
		const iconClass = item.querySelector("i").className;
		item.style.display = iconClass.toLowerCase().includes(term)
			? "block"
			: "none";
	});
}

function selectIcon(iconClass) {
	if (currentEditingRuleIndex !== null) {
		tableGroupingRules[currentEditingRuleIndex].icon = iconClass;
		populateRulesUI();
		analyzeUnusedRules();
	}
	closeIconPicker();
}
