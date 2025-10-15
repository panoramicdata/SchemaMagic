# Visual Guide: Relationship Routing Improvements

## ?? Self-Referential Relationships

### Before (? Weird Tail)
```
                FK: ParentCategoryId
                    |
    [Category] ??????
         Id         |
                    |
                    ? (midpoint calculation)
         ????????????
         |
         ???? weird tail effect
```

### After (? Clean Loop)
```
    [Category]
    ???????????????????????????
    ? Id                  int ????
    ? Name             string ?  ?
    ? ParentCategoryId?   int ????
    ? ParentCategory  Category?  ?
    ? SubCategories Collection?  ?
    ???????????????????????????  ?
          ?                      ?
          ?                      ?
          ????????????????????????
           Clean rectangular loop
```

## ?? Vertically Stacked Tables

### Before (? Long Horizontal Lines)
```
[OrderItem]
    ProductId ???????????????????????????????
                                            ?
                                            ? Long horizontal segment
                                            ? looks unnatural
                                            ?
                                            ?
                                      [Product]
                                          Id
```

### After (? Angled Routing)
```
[OrderItem]
    ProductId ??????????
                       ? 40% horizontal
                       ?
                       ? Angle down
                       ?
                       ?
                       ?????????????? [Product]
                                         Id

Exit right ? Move 40% ? Angle down ? Continue ? Enter left
```

## ?? 0..1 Relationship Examples

### Optional Foreign Keys (Nullable)
```
[Order]
?? CustomerId         int     ? Customer (required, 1:N)
?? BillingAddressId   int?    ? Address (optional, 0..1) ?
?? ShippingAddressId  int?    ? Address (optional, 0..1) ?
?? ApprovedById       int?    ? Customer (optional, 0..1) ?

Legend:
? = Standard relationship
? = Optional (nullable) relationship with circle marker
```

## ?? Routing Decision Flow

```
Is same entity?
    ?? YES ? Self-referential loop (left side)
    ?? NO
        ?
        Source left of target?
        ?? YES (LEFT?RIGHT)
        ?   ?
        ?   Vertical separation > 100px?
        ?   ?? YES ? Angled routing (40% mid-point)
        ?   ?? NO  ? Standard horizontal routing
        ?
        ?? NO (RIGHT?LEFT)
            ?
            Vertical separation > 100px?
            ?? YES ? Angled routing (40% mid-point)
            ?? NO  ? Standard horizontal routing
```

## ?? Path Patterns

### Standard Horizontal (Side-by-Side)
```
[Source]                    [Target]
    FK ?????? ?????????? ?????? Id
         ^         ^       ^
         |         |       |
         exit   vertical  entry
         offset  adjust   offset
```

### Angled (Vertically Separated)
```
[Source]
    FK ??????
            ? 40%
            ?
            ?
            ?
            ??????? [Target]
                       Id
```

### Self-Referential Loop
```
    [Entity]
    ?????????????
    ?           ?
    ? Id        ?
    ?           ?
    ? ParentId ??
    ?           
    ???????????
              ? Loop width: 200px
              ?
    ???????????
```

## ?? Key Measurements

| Metric | Value | Purpose |
|--------|-------|---------|
| MARKER_OFFSET | 150px | Space for crow's foot markers |
| SELF_REF_LOOP_WIDTH | 200px | Width of self-referential loop |
| Vertical separation threshold | 100px | Trigger for angled routing |
| Horizontal separation threshold | 100px | Trigger for angled routing |
| Angled mid-point ratio | 40% | Where to angle vertically |

## ?? Visual Markers

### Relationship Cardinality Markers

```
1:1 (One-to-One)
Source ?????? ?????? Target
       one  one

1:0..1 (One-to-Zero-or-One)
Source ?????? ?????? Target
       one  optional one

1:N (One-to-Many)
Source ?????? ?????? Target
       one  many

0..1:N (Optional-to-Many)
Source ?????? ?????? Target
     optional many

Legend:
? = One (single line)
? = Optional (circle)
? = Many (crow's foot)
```

## ?? Geometric Calculations

### Angled Path (LEFT?RIGHT)
```javascript
horizontalDistance = toX - fromX
verticalDistance = toY - fromY

midPoint = {
    x: fromX + (horizontalDistance * 0.4),
    y: fromY  // Same level until angle
}

path = [
    Start(fromX, fromY),
    Line(midX, fromY),      // Horizontal
    Line(midX, toY),        // Angle
    Line(entryX, toY),      // Horizontal
    End(toX, toY)
]
```

## ?? Color Coding (from styles.css)

```css
/* Relationship Lines */
.relationship-line {
    stroke: #3b82f6;           /* Blue */
    stroke-width: 2;
}

.relationship-line.highlighted {
    stroke: #8b5cf6;           /* Purple */
    stroke-width: 3;
}

.relationship-line.dimmed {
    stroke: #6b7280;           /* Gray */
    opacity: 0.3;
}

/* Markers */
marker {
    fill: #3b82f6;             /* Blue for standard */
}

marker.highlighted {
    fill: #8b5cf6;             /* Purple for selected */
}
```

## ?? Test Scenarios

### Scenario 1: Self-Referential
```
? PASS: Clean rectangular loop on left side
? PASS: No extra path segments
? PASS: FK and Id clearly connected
? FAIL: Loop extends too far left (adjust SELF_REF_LOOP_WIDTH)
? FAIL: Tail or extra segments visible
```

### Scenario 2: Vertical Stacking
```
? PASS: Angled path when separation > 100px
? PASS: 40% horizontal before angle
? PASS: Clean entry into target
? FAIL: Still using horizontal routing (check thresholds)
? FAIL: Angle point looks wrong (adjust 40% ratio)
```

### Scenario 3: Optional Relationships
```
? PASS: Circle marker visible on nullable FKs
? PASS: Crow's foot on many side
? PASS: Single line on one side
? FAIL: No circle on nullable FK (check marker ID)
? FAIL: Wrong marker orientation
```

## ?? Future Enhancements

### 1. Configurable Routing
```javascript
const routingConfig = {
    style: 'angled' | 'orthogonal' | 'curved',
    angleRatio: 0.4,  // 40% default
    verticalThreshold: 100,
    horizontalThreshold: 100,
    loopWidth: 200
};
```

### 2. Automatic Layout Optimization
- Detect crowded areas
- Suggest table repositioning
- Minimize line crossings

### 3. Multi-Point Paths
For very complex relationships:
```
[A] ??
     ??? Avoid [B]
     ??? Route around obstacles
          ???? [C]
```

### 4. Smart Anchoring
- Auto-select best anchor point based on target direction
- Use top/bottom edges for vertical relationships
- Use left/right edges for horizontal relationships

## ?? Additional Resources

- **EF Core Documentation**: [Relationships in EF Core](https://learn.microsoft.com/en-us/ef/core/modeling/relationships)
- **Crow's Foot Notation**: [Database Design Visual Guide](https://www.lucidchart.com/pages/ER-diagram-symbols-and-meaning)
- **SVG Path Commands**: [MDN SVG Path Reference](https://developer.mozilla.org/en-US/docs/Web/SVG/Tutorial/Paths)
