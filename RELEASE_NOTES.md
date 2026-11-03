# PolyPipeX V1.00 (B26.11.02.04) - Release Notes
Released 2026-11-02. Authors: Grok and Eldon R. Brown - WA0UWH.

## What it does
PolyPipeX is an Onshape custom feature that sweeps a profile along any edge you select. Think of it as the reverse of Fillet and Chamfer: washers, edge trim, O-ring grooves, wire bundles, rounded edges and more. Remix of the simple "Pipe" FeatureScript.

## Any edge can be a path
- Sketch lines, arcs and splines, including construction lines.
- Part edges: pick edges right on an existing part. New to most users, and great for rounding, trimming or grooving an edge in place.
- Intersection curves and 3D curves (helix, bridging curve, projected or composite curves).
Pick several edges at once; connected edges make one continuous pipe.

## Profile shapes
Circle, Arc (1-360 deg), Polygon / Star, Rectangle (corner radius), Slot, Round over (corner minus circle; with Remove on an outside edge it rounds the edge like a fillet, "Round on the other side" flips it). Any shape can be Hollow (wall thickness, wall inside or outside), with dent / flat-end / bulge options.

## Options
- Result: New, Add, Remove, Intersect (like Extrude); Intersect with Merge with all uses only overlapped parts.
- Keep pipe as a part (Remove / Intersect), Cut bore through target (Add + Hollow).
- Path: Reverse direction, Use part of path (From % / To %), Extend / shorten ends; duplicate edges ignored.
- Offset profile from path (width / height), Rotate profile, Height reference.
- Twist: turns over the path, or pitch.
- Show profiles (one / evenly spaced), START/END marks, path direction arrows.
- Clear failure messages (e.g. profile too wide for the tightest bend, with a suggested size).

## How to add it to your document
1. In a Part Studio, open the Custom features (puzzle-piece) toolbar menu > Add custom features.
2. Search "PolyPipeX" (or paste the link to the versioned PolyPipeX_V1.00 doc) and pick the PolyPipeX feature studio version.
3. PolyPipeX appears in your toolbar. See the PolyPipeX_V1.00_Demo doc for examples of every shape, option, operation and use.

## Versioning
V major.minor (V1.00, V1.01, ...), plus build code B YY.MM.DD.NN shown in the dialog. Not compatible with older PolyPipe / GrokPipe features (different feature type).
