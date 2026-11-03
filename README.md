<!-- Authors: Grok and Eldon R. Brown - WA0UWH, Rev 2026-11-03 -->
# PolyPipeX

**PolyPipeX** is an Onshape FeatureScript custom feature that sweeps a profile along **any edge** you select.
Think of it as the reverse of Fillet and Chamfer.

Current release: **V1.00, build B26.11.02.04** (released 2026-11-02).

## Any edge can be a path
- Sketch lines, arcs and splines, including **construction lines**
- **Part edges** - pick edges right on an existing part to round, trim or groove an edge in place
- **Intersection curves** and 3D curves (helix, bridging, projected or composite curves)

Pick several edges at once; connected edges make one continuous pipe.

## Profile shapes
Circle, Arc (1-360 deg), Polygon / Star, Rectangle (corner radius), Slot, and Round over.
Any shape can be solid or **Hollow** (wall thickness, wall inside or outside).

## Result
New, Add, Remove, Intersect (like Extrude).

## Uses
Washers, edge trim, O-ring grooves, cables and wire bundles, reverse fillets, round-overs.

## Path and other options
- Reverse direction, use part of path (From % / To %), extend / shorten ends
- Twist by turns over the path, or by pitch
- Offset and rotate the profile, show profiles, START/END marks, path direction arrows
- Clear failure messages (for example, profile too wide for the tightest bend)

See [RELEASE_NOTES.md](RELEASE_NOTES.md) for the full list.

## How to use
1. Open the public PolyPipeX script doc:
   https://cad.onshape.com/documents/e1d7954fe30ceed624c58ca8/v/e1e5b11796a2374ccb7bcce5
   - In a Part Studio, use Custom features (puzzle piece) > Add custom features, search "PolyPipeX" or paste the link, and pick the PolyPipeX feature studio version.
2. Or create your own Feature Studio and paste in [FeatureScript/PolyPipeX_V1.00_B26.11.02.04.fs](FeatureScript/PolyPipeX_V1.00_B26.11.02.04.fs).

Demo document with examples of every shape, option, operation and use:
https://cad.onshape.com/documents/bdedb1c31094967e66258764/v/7a0555a7372ca5b0a802fdbb
(see [Demo/Part_Edge_Demo.md](Demo/Part_Edge_Demo.md)).

## Repository layout
- `FeatureScript/` - the V1.00 release script
- `RELEASE_NOTES.md` - V1.00 release notes
- `Docs/PARAM_MAP.md` - parameter map (written for build B26.10.28.03)
- `Demo/` - notes on the demo document tabs
- `History/` - earlier PolyPipeX builds

## Background
Based on Onshape's FeatureScript "Pipe" example; grew out of the earlier GrokPipe / PolyPipe work.
PolyPipeX is a new feature type and is not compatible with older PolyPipe / GrokPipe features.

Versioning: V major.minor (V1.00, V1.01, ...) plus a build code B YY.MM.DD.NN shown in the dialog.

## License
TBD (not chosen yet).

## Authors
Grok and Eldon R. Brown - WA0UWH
<!-- End -->
