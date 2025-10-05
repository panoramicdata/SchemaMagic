// Global variables and constants
const entities = ENTITIES_JSON_PLACEHOLDER;

// Document identifier for localStorage - injected at generation time
const DOCUMENT_GUID = 'DOCUMENT_GUID_PLACEHOLDER';

let selectedTable = null;
let showOnlySelectedRelations = false;
let showRelationships = true;
let showNavigationProperties = false;
let showInheritedProperties = true; // Show inherited by default
let fullHeightMode = false;
let snapToGrid = true; // Changed from false to true - enabled by default
let currentZoom = 0.35; // Reduced from 1 to 0.35 - start much more zoomed out
let isPanning = false;
let panStart = { x: 0, y: 0 };
let svgViewBox = { x: 0, y: 0, width: 20000, height: 15000 }; // Increased from 8000x6000 to 20000x15000
const CANVAS_WIDTH = 20000; // Increased from 8000 to 20000 (2.5x larger)
const CANVAS_HEIGHT = 15000; // Increased from 6000 to 15000 (2.5x larger)
const BOUNDARY_MARGIN = 250; // Increased from 100 to 250 (2.5x larger)
const MAX_ZOOM = 20; // Increased from 5 to 20 for extreme zoom capability

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