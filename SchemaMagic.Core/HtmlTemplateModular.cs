using System.Reflection;
using System.Text;

namespace SchemaMagic.Core;

public static class ModularHtmlTemplate
{
    public static string Generate(string entitiesJson, string documentGuid, string? customCssPath)
    {
        // Read custom CSS if provided
        var customCss = string.Empty;
        if (!string.IsNullOrEmpty(customCssPath) && File.Exists(customCssPath))
        {
            customCss = File.ReadAllText(customCssPath);
        }

        return GenerateWithCustomCss(entitiesJson, documentGuid, customCss);
    }

    public static string GenerateWithCustomCss(string entitiesJson, string documentGuid, string? customCss)
    {
        var cssContent = !string.IsNullOrEmpty(customCss) ? customCss : GetDefaultCss();
        
        return $@"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Schema Visualization - SchemaMagic</title>
    <style>
{cssContent}
    </style>
</head>
<body>
    <div id=""schema-container"">
        <div id=""toolbar"">
            <h1>📊 Interactive Schema Diagram</h1>
            <div class=""controls"">
                <button id=""reset-zoom"" title=""Reset zoom and center"">🔍 Reset View</button>
                <button id=""toggle-inheritance"" title=""Toggle inheritance relationships"">🔗 Inheritance</button>
                <button id=""toggle-navigation"" title=""Toggle navigation properties"">🧭 Navigation</button>
                <button id=""auto-layout"" title=""Auto-arrange tables"">📐 Auto Layout</button>
            </div>
        </div>
        <svg id=""schema-svg"" width=""100%"" height=""100%"">
            <defs>
                <marker id=""arrowhead"" markerWidth=""10"" markerHeight=""7"" refX=""9"" refY=""3.5"" orient=""auto"">
                    <polygon points=""0 0, 10 3.5, 0 7"" fill=""#666"" />
                </marker>
            </defs>
            <g id=""relationships""></g>
            <g id=""tables""></g>
        </svg>
    </div>

    <script>
        // Document GUID for localStorage isolation
        const DOCUMENT_GUID = '{documentGuid}';
        const STORAGE_KEY = `schemamagic_layout_${{DOCUMENT_GUID}}`;
        
        // Entity data
        const entities = {entitiesJson};

        // Global state
        let selectedTable = null;
        let showInheritance = true;
        let showNavigation = true;
        let scale = 1;
        let translateX = 0;
        let translateY = 0;
        let isDragging = false;
        let dragTarget = null;
        let dragOffset = {{ x: 0, y: 0 }};

        // Initialize the schema visualization
        function initializeSchema() {{
            console.log('🚀 SchemaMagic: Initializing interactive schema...');
            console.log(`📊 Loaded ${{Object.keys(entities).length}} entities`);
            console.log(`🆔 Document GUID: ${{DOCUMENT_GUID}}`);

            const svg = document.getElementById('schema-svg');
            
            // Load saved layout or generate new one
            const savedLayout = loadLayout();
            const tablePositions = savedLayout || generateAutoLayout();

            // Render tables and relationships
            renderTables(tablePositions);
            renderRelationships();

            // Set up event listeners
            setupEventListeners();

            // Apply saved view state
            if (savedLayout && savedLayout.viewState) {{
                scale = savedLayout.viewState.scale || 1;
                translateX = savedLayout.viewState.translateX || 0;
                translateY = savedLayout.viewState.translateY || 0;
                updateViewTransform();
            }} else {{
                // Auto-fit on first load
                setTimeout(resetView, 100);
            }}

            console.log('✅ Schema visualization ready!');
        }}

        // Generate automatic layout using force-directed algorithm
        function generateAutoLayout() {{
            const entities_array = Object.entries(entities);
            const width = window.innerWidth - 40;
            const height = window.innerHeight - 120;
            
            // Simple grid layout as fallback
            const cols = Math.ceil(Math.sqrt(entities_array.length));
            const tableWidth = 280;
            const tableHeight = 200;
            
            const positions = {{}};
            entities_array.forEach(([name], index) => {{
                const row = Math.floor(index / cols);
                const col = index % cols;
                positions[name] = {{
                    x: col * (tableWidth + 40) + 20,
                    y: row * (tableHeight + 40) + 20
                }};
            }});

            return {{ tables: positions }};
        }}

        // Render tables
        function renderTables(layout) {{
            const tablesGroup = document.getElementById('tables');
            tablesGroup.innerHTML = '';

            Object.entries(entities).forEach(([entityName, entity]) => {{
                const position = layout.tables[entityName] || {{ x: 100, y: 100 }};
                const tableElement = createTableElement(entityName, entity, position);
                tablesGroup.appendChild(tableElement);
            }});
        }}

        // Create table SVG element
        function createTableElement(entityName, entity, position) {{
            const g = document.createElementNS('http://www.w3.org/2000/svg', 'g');
            g.setAttribute('class', 'table');
            g.setAttribute('data-entity', entityName);
            g.setAttribute('transform', `translate(${{position.x}}, ${{position.y}})`);

            // Calculate table dimensions
            const properties = [...(entity.properties || []), ...(entity.inheritedProperties || [])];
            const tableWidth = 280;
            const headerHeight = 40;
            const rowHeight = 24;
            const tableHeight = headerHeight + (properties.length * rowHeight) + 10;

            // Table background
            const rect = document.createElementNS('http://www.w3.org/2000/svg', 'rect');
            rect.setAttribute('width', tableWidth);
            rect.setAttribute('height', tableHeight);
            rect.setAttribute('rx', '8');
            rect.setAttribute('class', 'table-bg');
            g.appendChild(rect);

            // Table header
            const headerRect = document.createElementNS('http://www.w3.org/2000/svg', 'rect');
            headerRect.setAttribute('width', tableWidth);
            headerRect.setAttribute('height', headerHeight);
            headerRect.setAttribute('rx', '8');
            headerRect.setAttribute('class', 'table-header');
            g.appendChild(headerRect);

            // Entity name
            const title = document.createElementNS('http://www.w3.org/2000/svg', 'text');
            title.setAttribute('x', tableWidth / 2);
            title.setAttribute('y', 26);
            title.setAttribute('class', 'table-title');
            title.setAttribute('text-anchor', 'middle');
            title.textContent = entityName;
            g.appendChild(title);

            // Properties
            properties.forEach((prop, index) => {{
                const y = headerHeight + (index * rowHeight) + 16;
                
                // Property text
                const propText = document.createElementNS('http://www.w3.org/2000/svg', 'text');
                propText.setAttribute('x', 12);
                propText.setAttribute('y', y);
                propText.setAttribute('class', getPropertyClass(prop));
                propText.textContent = `${{getPropertyIcon(prop)}} ${{prop.name}}: ${{prop.type}}`;
                g.appendChild(propText);
            }});

            // Make draggable
            g.style.cursor = 'move';
            
            return g;
        }}

        // Get property CSS class
        function getPropertyClass(prop) {{
            const classes = ['property'];
            if (prop.isKey) classes.push('property-key');
            if (prop.isForeignKey) classes.push('property-fk');
            return classes.join(' ');
        }}

        // Get property icon
        function getPropertyIcon(prop) {{
            if (prop.isKey) return '🔑';
            if (prop.isForeignKey) return '🔗';
            return '📄';
        }}

        // Render relationships
        function renderRelationships() {{
            const relationshipsGroup = document.getElementById('relationships');
            relationshipsGroup.innerHTML = '';

            // Simple relationship rendering - can be enhanced
            console.log('🔗 Rendering relationships...');
        }}

        // Event listeners
        function setupEventListeners() {{
            const svg = document.getElementById('schema-svg');
            
            // Mouse events for pan/zoom
            svg.addEventListener('wheel', handleWheel);
            svg.addEventListener('mousedown', handleMouseDown);
            svg.addEventListener('mousemove', handleMouseMove);
            svg.addEventListener('mouseup', handleMouseUp);

            // Button events
            document.getElementById('reset-zoom').addEventListener('click', resetView);
            document.getElementById('toggle-inheritance').addEventListener('click', toggleInheritance);
            document.getElementById('toggle-navigation').addEventListener('click', toggleNavigation);
            document.getElementById('auto-layout').addEventListener('click', autoLayout);

            // Table click events
            svg.addEventListener('click', handleTableClick);
        }}

        // Event handlers
        function handleWheel(event) {{
            event.preventDefault();
            const delta = event.deltaY > 0 ? 0.9 : 1.1;
            scale *= delta;
            scale = Math.max(0.1, Math.min(3, scale));
            updateViewTransform();
            saveLayout();
        }}

        function handleMouseDown(event) {{
            const target = event.target.closest('.table');
            if (target) {{
                isDragging = true;
                dragTarget = target;
                const rect = target.getBoundingClientRect();
                const svgRect = document.getElementById('schema-svg').getBoundingClientRect();
                dragOffset = {{
                    x: (event.clientX - svgRect.left) / scale - translateX,
                    y: (event.clientY - svgRect.top) / scale - translateY
                }};
                
                // Get current transform
                const transform = target.getAttribute('transform');
                const match = transform.match(/translate\\(([^,]+),\\s*([^)]+)\\)/);
                if (match) {{
                    dragOffset.x -= parseFloat(match[1]);
                    dragOffset.y -= parseFloat(match[2]);
                }}
            }}
        }}

        function handleMouseMove(event) {{
            if (isDragging && dragTarget) {{
                const svgRect = document.getElementById('schema-svg').getBoundingClientRect();
                const x = (event.clientX - svgRect.left) / scale - translateX - dragOffset.x;
                const y = (event.clientY - svgRect.top) / scale - translateY - dragOffset.y;
                
                dragTarget.setAttribute('transform', `translate(${{x}}, ${{y}})`);
                renderRelationships();
            }}
        }}

        function handleMouseUp() {{
            if (isDragging) {{
                isDragging = false;
                dragTarget = null;
                saveLayout();
            }}
        }}

        function handleTableClick(event) {{
            const table = event.target.closest('.table');
            if (table) {{
                const entityName = table.getAttribute('data-entity');
                if (selectedTable === entityName) {{
                    // Deselect
                    selectedTable = null;
                    document.querySelectorAll('.table').forEach(t => t.classList.remove('selected'));
                }} else {{
                    // Select
                    selectedTable = entityName;
                    document.querySelectorAll('.table').forEach(t => t.classList.remove('selected'));
                    table.classList.add('selected');
                }}
                renderRelationships();
            }}
        }}

        function resetView() {{
            scale = 1;
            translateX = 0;
            translateY = 0;
            updateViewTransform();
            saveLayout();
        }}

        function toggleInheritance() {{
            showInheritance = !showInheritance;
            renderRelationships();
        }}

        function toggleNavigation() {{
            showNavigation = !showNavigation;
            renderRelationships();
        }}

        function autoLayout() {{
            const newLayout = generateAutoLayout();
            renderTables(newLayout);
            renderRelationships();
            saveLayout();
        }}

        function updateViewTransform() {{
            const tablesGroup = document.getElementById('tables');
            const relationshipsGroup = document.getElementById('relationships');
            const transform = `translate(${{translateX}}, ${{translateY}}) scale(${{scale}})`;
            tablesGroup.setAttribute('transform', transform);
            relationshipsGroup.setAttribute('transform', transform);
        }}

        // Layout persistence
        function saveLayout() {{
            const tables = {{}};
            document.querySelectorAll('.table').forEach(table => {{
                const entityName = table.getAttribute('data-entity');
                const transform = table.getAttribute('transform');
                const match = transform.match(/translate\\(([^,]+),\\s*([^)]+)\\)/);
                if (match) {{
                    tables[entityName] = {{
                        x: parseFloat(match[1]),
                        y: parseFloat(match[2])
                    }};
                }}
            }});

            const layout = {{
                tables,
                viewState: {{
                    scale,
                    translateX,
                    translateY
                }}
            }};

            try {{
                localStorage.setItem(STORAGE_KEY, JSON.stringify(layout));
            }} catch (e) {{
                console.warn('Could not save layout to localStorage:', e);
            }}
        }}

        function loadLayout() {{
            try {{
                const saved = localStorage.getItem(STORAGE_KEY);
                return saved ? JSON.parse(saved) : null;
            }} catch (e) {{
                console.warn('Could not load layout from localStorage:', e);
                return null;
            }}
        }}

        // Initialize when DOM is ready
        if (document.readyState === 'loading') {{
            document.addEventListener('DOMContentLoaded', initializeSchema);
        }} else {{
            initializeSchema();
        }}
    </script>
</body>
</html>";
    }

    public static string GetDefaultCss()
    {
        return @"
* {
    margin: 0;
    padding: 0;
    box-sizing: border-box;
}

body {
    font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
    background: linear-gradient(135deg, #f5f7fa 0%, #c3cfe2 100%);
    height: 100vh;
    overflow: hidden;
}

#schema-container {
    width: 100%;
    height: 100vh;
    display: flex;
    flex-direction: column;
}

#toolbar {
    background: rgba(255, 255, 255, 0.95);
    backdrop-filter: blur(10px);
    border-bottom: 1px solid #e0e0e0;
    padding: 12px 20px;
    display: flex;
    justify-content: space-between;
    align-items: center;
    box-shadow: 0 2px 10px rgba(0, 0, 0, 0.1);
}

#toolbar h1 {
    font-size: 1.5em;
    color: #333;
    margin: 0;
}

.controls {
    display: flex;
    gap: 10px;
}

.controls button {
    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
    color: white;
    border: none;
    padding: 8px 16px;
    border-radius: 6px;
    cursor: pointer;
    font-size: 0.9em;
    transition: all 0.3s ease;
}

.controls button:hover {
    transform: translateY(-2px);
    box-shadow: 0 4px 12px rgba(102, 126, 234, 0.4);
}

#schema-svg {
    flex: 1;
    background: linear-gradient(45deg, #f8f9fa 25%, transparent 25%),
                linear-gradient(-45deg, #f8f9fa 25%, transparent 25%),
                linear-gradient(45deg, transparent 75%, #f8f9fa 75%),
                linear-gradient(-45deg, transparent 75%, #f8f9fa 75%);
    background-size: 20px 20px;
    background-position: 0 0, 0 10px, 10px -10px, -10px 0px;
}

.table {
    transition: all 0.3s ease;
}

.table:hover {
    filter: drop-shadow(0 8px 16px rgba(0, 0, 0, 0.15));
}

.table.selected {
    filter: drop-shadow(0 8px 20px rgba(102, 126, 234, 0.4));
}

.table-bg {
    fill: white;
    stroke: #ddd;
    stroke-width: 1;
    filter: drop-shadow(0 4px 8px rgba(0, 0, 0, 0.1));
}

.table-header {
    fill: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
    stroke: none;
}

.table-title {
    fill: white;
    font-size: 14px;
    font-weight: 600;
}

.property {
    fill: #444;
    font-size: 12px;
    font-family: 'Monaco', 'Consolas', monospace;
}

.property-key {
    fill: #f39c12;
    font-weight: 600;
}

.property-fk {
    fill: #9b59b6;
    font-weight: 500;
}

.relationship-line {
    stroke: #666;
    stroke-width: 2;
    fill: none;
    marker-end: url(#arrowhead);
}

.relationship-line.inheritance {
    stroke: #3498db;
    stroke-dasharray: 5,5;
}

.relationship-line.navigation {
    stroke: #27ae60;
}

@media (max-width: 768px) {
    #toolbar {
        flex-direction: column;
        gap: 10px;
        padding: 10px;
    }
    
    .controls {
        flex-wrap: wrap;
        justify-content: center;
    }
    
    .controls button {
        font-size: 0.8em;
        padding: 6px 12px;
    }
}";
    }
}