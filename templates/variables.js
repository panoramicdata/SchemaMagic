// Global variables and constants
const entities = ENTITIES_JSON_PLACEHOLDER;

// Document identifier for localStorage - injected at generation time
const DOCUMENT_GUID = 'DOCUMENT_GUID_PLACEHOLDER';

let selectedTable = null;
let showOnlySelectedRelations = false;
let showRelationships = true;
let showNavigationProperties = true; // Changed from false to true - enabled by default
let showInheritedProperties = true; // ? ON by default
let fullHeightMode = true; // Changed from false to true - enabled by default
let snapToGrid = true; // Changed from false to true - enabled by default
let currentZoom = 0.35; // Reduced from 1 to 0.35 - start much more zoomed out
let isPanning = false;
let panStart = { x: 0, y: 0 };
let svgViewBox = { x: 0, y: 0, width: 20000, height: 15000 }; // Increased from 8000x6000 to 20000x15000
const CANVAS_WIDTH = 20000; // Increased from 8000 to 20000 (2.5x larger)
const CANVAS_HEIGHT = 15000; // Increased from 6000 to 15000 (2.5x larger)
const BOUNDARY_MARGIN = 250; // Increased from 100 to 250 (2.5x larger)
const MAX_ZOOM = 15;

// Document-specific local storage keys using GUID
const STORAGE_KEYS = {
	tablePositions: `schemaMagic_${DOCUMENT_GUID}_tablePositions`,
	viewSettings: `schemaMagic_${DOCUMENT_GUID}_viewSettings`,
	viewBox: `schemaMagic_${DOCUMENT_GUID}_viewBox`
};

// Helper function to check if this document has saved state
function hasSavedDocumentState() {
	try {
		const savedPositions = localStorage.getItem(STORAGE_KEYS.tablePositions);
		return savedPositions !== null && savedPositions !== '';
	} catch (e) {
		console.warn('Failed to check saved document state:', e);
		return false;
	}
}

// Helper function to get document-specific storage key
function getDocumentStorageKey(keyType) {
	return STORAGE_KEYS[keyType] || `schemaMagic_${DOCUMENT_GUID}_${keyType}`;
}

// Table Grouping Rules - Comprehensive Default Set
const DEFAULT_TABLE_GROUPING_RULES = [
	// Core Business Entities
	{ name: "Organizations", pattern: "Company|Organization|Business|Enterprise|Corporation|Firm", icon: "fa-building", color: "#3b82f6", enabled: true },
	{ name: "Departments", pattern: "Department|Division|Unit|Team|Group", icon: "fa-sitemap", color: "#06b6d4", enabled: true },
	{ name: "Locations", pattern: "Location|Address|Site|Office|Branch|Facility", icon: "fa-location-dot", color: "#8b5cf6", enabled: true },
	
	// People & Users
	{ name: "Users", pattern: "User|Account|Profile|Identity", icon: "fa-user", color: "#ec4899", enabled: true },
	{ name: "Employees", pattern: "Employee|Staff|Personnel|Worker", icon: "fa-id-badge", color: "#f59e0b", enabled: true },
	{ name: "Customers", pattern: "Customer|Client|Contact|Lead|Prospect", icon: "fa-user-tie", color: "#10b981", enabled: true },
	{ name: "Members", pattern: "Member|Subscriber|Participant", icon: "fa-users", color: "#6366f1", enabled: true },
	
	// Relationships & Associations
	{ name: "Junctions", pattern: ".*To.*|.*Member.*|.*Assignment.*|.*Mapping.*", icon: "fa-link", color: "#64748b", enabled: true },
	{ name: "Roles & Permissions", pattern: "Role|Permission|Access|Authorization|Privilege", icon: "fa-shield-halved", color: "#dc2626", enabled: true },
	
	// Financial
	{ name: "Financial", pattern: "Invoice|Payment|Transaction|Order|Purchase|Sale|Price|Cost|Revenue|Billing", icon: "fa-money-bill-wave", color: "#16a34a", enabled: true },
	{ name: "Products", pattern: "Product|Item|SKU|Inventory|Stock|Catalog", icon: "fa-box", color: "#ea580c", enabled: true },
	
	// Content & Media
	{ name: "Documents", pattern: "Document|File|Attachment|Upload|Asset|Media", icon: "fa-file-lines", color: "#0ea5e9", enabled: true },
	{ name: "Posts & Articles", pattern: "Post|Article|Blog|Content|Page|News", icon: "fa-newspaper", color: "#8b5cf6", enabled: true },
	{ name: "Comments", pattern: "Comment|Reply|Feedback|Review|Rating", icon: "fa-comment", color: "#a855f7", enabled: true },
	{ name: "Tags", pattern: "Tag|Category|Label|Classification", icon: "fa-tags", color: "#14b8a6", enabled: true },
	
	// Communication
	{ name: "Messages", pattern: "Message|Email|Notification|Alert|SMS|Chat", icon: "fa-envelope", color: "#6366f1", enabled: true },
	{ name: "Conversations", pattern: "Conversation|Thread|Discussion|Ticket", icon: "fa-comments", color: "#8b5cf6", enabled: true },
	
	// Projects & Tasks
	{ name: "Projects", pattern: "Project|Initiative|Program|Campaign", icon: "fa-diagram-project", color: "#0891b2", enabled: true },
	{ name: "Tasks", pattern: "Task|Todo|Activity|Action|Assignment", icon: "fa-list-check", color: "#f97316", enabled: true },
	{ name: "Milestones", pattern: "Milestone|Phase|Sprint|Iteration|Release", icon: "fa-flag-checkered", color: "#7c3aed", enabled: true },
	
	// Scheduling & Time
	{ name: "Events", pattern: "Event|Meeting|Appointment|Booking|Reservation", icon: "fa-calendar-days", color: "#db2777", enabled: true },
	{ name: "Schedule", pattern: "Schedule|Shift|Roster|Timetable|Availability", icon: "fa-calendar-check", color: "#059669", enabled: true },
	
	// Technical & System
	{ name: "Logs & Audits", pattern: "Log|Audit|History|Track|Change|Event", icon: "fa-clipboard-list", color: "#475569", enabled: true },
	{ name: "Configuration", pattern: "Config|Setting|Preference|Option|Parameter", icon: "fa-gear", color: "#71717a", enabled: true },
	{ name: "Integration", pattern: "Integration|API|Webhook|Sync|Import|Export", icon: "fa-plug", color: "#6b7280", enabled: true },
	
	// Monitoring & Analytics
	{ name: "Metrics", pattern: "Metric|Statistic|Analytics|Report|Dashboard|KPI", icon: "fa-chart-line", color: "#2563eb", enabled: true },
	{ name: "Alerts", pattern: "Alert|Warning|Error|Exception|Issue", icon: "fa-triangle-exclamation", color: "#ef4444", enabled: true },
	
	// Devices & Network
	{ name: "Devices", pattern: "Device|Equipment|Asset|Hardware|Machine|Sensor", icon: "fa-microchip", color: "#0284c7", enabled: true },
	{ name: "Network", pattern: "Network|Connection|Link|Node|Interface", icon: "fa-network-wired", color: "#0d9488", enabled: true },
	
	// Workflow & Status
	{ name: "Workflow", pattern: "Workflow|Process|Pipeline|Stage|Status", icon: "fa-route", color: "#7c2d12", enabled: true },
	{ name: "Queue", pattern: "Queue|Job|Batch|Background|Worker", icon: "fa-list-ol", color: "#92400e", enabled: true },
	
	// Security
	{ name: "Security", pattern: "Security|Token|Key|Certificate|Credential|Secret", icon: "fa-lock", color: "#991b1b", enabled: true },
	{ name: "Sessions", pattern: "Session|Login|Authentication|OAuth|SSO", icon: "fa-right-to-bracket", color: "#be123c", enabled: true },
	
	// Default fallback (matches everything) - should be last
	{ name: "Default", pattern: ".*", icon: "fa-database", color: "#0ea5e9", enabled: true }
];

// Available FontAwesome 6 Free Icons (Solid)
const AVAILABLE_ICONS = [
	"fa-database", "fa-table", "fa-building", "fa-sitemap", "fa-location-dot",
	"fa-user", "fa-users", "fa-id-badge", "fa-user-tie", "fa-link", "fa-shield-halved",
	"fa-money-bill-wave", "fa-box", "fa-file-lines", "fa-newspaper", "fa-comment",
	"fa-tags", "fa-envelope", "fa-comments", "fa-diagram-project", "fa-list-check",
	"fa-flag-checkered", "fa-calendar-days", "fa-calendar-check", "fa-clipboard-list",
	"fa-gear", "fa-plug", "fa-chart-line", "fa-triangle-exclamation", "fa-microchip",
	"fa-network-wired", "fa-route", "fa-list-ol", "fa-lock", "fa-right-to-bracket",
	"fa-circle", "fa-square", "fa-heart", "fa-star", "fa-bookmark", "fa-bell",
	"fa-flag", "fa-cloud", "fa-truck", "fa-shopping-cart", "fa-credit-card",
	"fa-certificate", "fa-trophy", "fa-gift", "fa-phone", "fa-mobile",
	"fa-laptop", "fa-desktop", "fa-globe", "fa-map", "fa-compass",
	"fa-wrench", "fa-cog", "fa-code", "fa-bug", "fa-terminal",
	"fa-folder", "fa-folder-open", "fa-file", "fa-file-pdf", "fa-file-word",
	"fa-image", "fa-video", "fa-music", "fa-camera", "fa-print",
	"fa-search", "fa-filter", "fa-sort", "fa-bars", "fa-th",
	"fa-home", "fa-briefcase", "fa-graduation-cap", "fa-hospital", "fa-medkit",
	"fa-chart-bar", "fa-chart-pie", "fa-chart-area", "fa-bullseye", "fa-crosshairs",
	"fa-server", "fa-hdd", "fa-wifi", "fa-signal", "fa-battery-full"
];

// Color palette options for rules
const COLOR_PALETTE = [
	"#ef4444", "#f97316", "#f59e0b", "#eab308", "#84cc16", "#22c55e", "#10b981", "#14b8a6",
	"#06b6d4", "#0ea5e9", "#3b82f6", "#6366f1", "#8b5cf6", "#a855f7", "#d946ef", "#ec4899",
	"#f43f5e", "#dc2626", "#ea580c", "#d97706", "#ca8a04", "#65a30d", "#16a34a", "#059669",
	"#0d9488", "#0891b2", "#0284c7", "#2563eb", "#4f46e5", "#7c3aed", "#9333ea", "#c026d3",
	"#db2777", "#be123c", "#991b1b", "#9a3412", "#92400e", "#713f12", "#365314", "#14532d",
	"#064e3b", "#134e4a", "#164e63", "#1e3a8a", "#312e81", "#4c1d95", "#581c87", "#701a75"
];

// Current active rule for icon picker
let currentEditingRuleIndex = null;

// Load or initialize table grouping rules
let tableGroupingRules = [];

function loadTableGroupingRules() {
	const storedRules = localStorage.getItem(STORAGE_KEYS.tableGroupingRules);
	if (storedRules) {
		try {
			tableGroupingRules = JSON.parse(storedRules);
			console.log(`?? Loaded ${tableGroupingRules.length} table grouping rules`);
		} catch (e) {
			console.warn('Failed to parse stored rules, using defaults', e);
			tableGroupingRules = [...DEFAULT_TABLE_GROUPING_RULES];
		}
	} else {
		tableGroupingRules = [...DEFAULT_TABLE_GROUPING_RULES];
		console.log(`?? Initialized with ${tableGroupingRules.length} default rules`);
	}
}

function saveTableGroupingRulesToStorage() {
	localStorage.setItem(STORAGE_KEYS.tableGroupingRules, JSON.stringify(tableGroupingRules));
	console.log(`?? Saved ${tableGroupingRules.length} table grouping rules`);
}

// Get the matching rule for a table name
function getMatchingRule(tableName) {
	for (const rule of tableGroupingRules) {
		if (!rule.enabled) continue;
		
		try {
			const regex = new RegExp(rule.pattern, 'i'); // Case-insensitive
			if (regex.test(tableName)) {
				return rule;
			}
		} catch (e) {
			console.warn(`Invalid regex pattern: ${rule.pattern}`, e);
		}
	}
	return null; // No match found
}