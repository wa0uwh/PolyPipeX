# PolyPipeX B26.10.28.03 — parameter map (experimental)

**B26.10.28.03 changes:** new Profile shape value `roundOver` ("Round over": corner minus circle, for Remove on an outside
part edge; side found from the part edge) and new boolean `roundOverFlipSide` ("Round on the other side"). `radius` is also
used by Round over. Hollow / Cut bore are hidden for Round over. `keepPipeAsPart` stays Remove / Intersect only (hidden for
Add). Intersect + `defaultScope` (Merge with all) now uses only the parts the pipe overlaps. Duplicate path edges are ignored.
All other ids unchanged from .02 (66 dialog ids incl. the two new ones; the feature spec lists 65 parameters).


Derived from GrokPipe V3.10.28 (B26.10.28.01); B26.10.28.02 only writes the name as "PolyPipeX". Parameter ids unchanged. Feature `grokPipe` ("GrokPipe") -> `polyPipeX` ("PolyPipeX"); Editing Logic
`grokPipeEditLogic` -> `polyPipeXEditLogic` (kept only for the Extrude-style auto New/Add switching).
Enums: `GrokPipeType` -> `PolyPipeXShape` (circular->circle, arc, polygonDent->polygon, rectDent->rectangle, slot);
`GrokTwistType` -> `PolyPipeXTwistMode` (count->turns, pitch); `GrokPlaceMode` -> `PolyPipeXStations` (preview->one, count->evenly).
Not compatible with PolyPipe / GrokPipe features (it is a new feature type). Dialog order: Result, Shape, Paths, size,
Hollow, Rotate / Offset / Height reference, Twist, Part of path / Extend-shorten, Merge, Show.

## Renamed (old GrokPipe id -> new PolyPipeX id: meaning)
| GrokPipe | PolyPipeX | What it does |
|---|---|---|
| operationType | operationType | New / Add / Remove / Intersect (Onshape std id, unchanged) |
| PipeType | shape | Profile shape: Circle, Arc, Polygon / Star, Rectangle, Slot |
| path | paths | Path edges (one pipe per connected path) |
| flipStart | reversePath | Reverse the path direction (START <-> END) |
| outerRadius | radius | Circle / Arc radius |
| arcAngle | arcAngle | Arc opening angle (1-360 deg) |
| polygonDentSides | polygonCorners | Number of polygon corners (3-64) |
| polygonDentRadius | polygonRadius | Center-to-corner radius |
| polygonDentRadiusOffset | polygonDentDepth | Pushes side middles in (0 = plain polygon, more = star) |
| polygonDentFlipDent | polygonDentOutward | Bulge the side middles out instead |
| rectDentHeight | rectHeight | Rectangle height (along sketch / face normal) |
| rectDentHeightOffset | rectHeightDentDepth | Dent at the middle of the height ends |
| rectDentFlipHeight | rectHeightDentOutward | Bulge out instead |
| rectDentWidth | rectWidth | Rectangle width |
| rectDentWidthOffset | rectWidthDentDepth | Dent at the middle of the width ends |
| rectDentFlipWidth | rectWidthDentOutward | Bulge out instead |
| endcapRadius | slotEndRadius | Slot end radius (half the slot width) |
| centerLength | slotStraightLength | Distance between the two end centers |
| slotRmEnd | slotOneFlatEnd | Make one slot end flat |
| flipSlotEnd | slotSwapFlatEnd | Put the flat end at the other side |
| nShell | hollow | Make the pipe hollow |
| nWall | wallThickness | Wall thickness |
| nFlipWall | wallOutside | Wall outside the profile (profile = hole) instead of inside |
| nShellAlt | arcCurvedBand | Arc hollow as a curved band, no side walls |
| rotate | rotateProfile | Rotate the profile about the path |
| rotateAngle | rotateAngle | Rotation angle |
| rotateAngleFlip | rotateClockwise | Rotate clockwise |
| centerOffset | offsetProfile | Offset the profile from the path |
| centerOffsetX | offsetHeight | Offset along height, length (**sign fixed: + by default**) |
| centerOffsetXFlip | offsetHeightNegative | Negative direction |
| centerOffsetY | offsetWidth | Offset along width, length (**sign fixed**) |
| centerOffsetYFlip | offsetWidthNegative | Negative direction |
| pCenterOffsetX | offsetHeightFraction | Offset along height, x profile height (**sign fixed**) |
| pCenterOffsetXFlip | offsetHeightFractionNegative | Negative direction |
| pCenterOffsetY | offsetWidthFraction | Offset along width, x profile width (**sign fixed**) |
| pCenterOffsetYFlip | offsetWidthFractionNegative | Negative direction |
| useRefDirection | useHeightReference | Use a picked direction for the profile Height |
| refDirection | heightDirection | Line, axis or plane giving the Height direction |
| twist | twist | Twist the profile along the path |
| twistType | twistMode | Turns (total) or Pitch (length per turn) |
| twistTurns | twistTurns | Turns over the path |
| twistPitch | twistPitch | Path length per turn |
| twistFlip | twistClockwise | Twist clockwise |
| pathRange | usePartOfPath | Build on part of the path only |
| startPct | pathStartPercent | From % of the path |
| endPct | pathEndPercent | To % of the path |
| extEnds | changeEnds | Extend / shorten the pipe ends |
| extNearEnd | startEndLength | Amount at the START end (green) |
| extFlipNear | shortenStartEnd | Shorten instead of extend (**sign fixed: off = extend**) |
| extFarEnd | endEndLength | Amount at the END end (red) |
| extFlipFar | shortenEndEnd | Shorten instead of extend (**sign fixed**) |
| defaultScope / booleanScope | defaultScope / booleanScope | Merge with all / Merge scope (Onshape std ids, unchanged) |
| keepTools | keepPipeAsPart | Remove / Intersect also keeps the pipe as a part |
| cutBore | cutBoreThroughTarget | Add + hollow: cut the bore through the target (default on) |
| sketchView | showProfiles | Show profile sketches along the pipe |
| placeMode | profileStations | One (preview) or Evenly spaced |
| placeCount | stationCount | How many evenly spaced profiles |
| placeSketchPercent | previewAt | Where the single preview sits (0-1 of the built pipe) |
| sketchDialogOnly | profilesDialogOnly | Show in dialog only, leave no sketches |
| sketchStartMarks | showProfileStartPoints | Mark the first point of each profile |
| sketchPathArrows | showPathArrows | Blue path direction arrows |
| sketchListStations | listStations | List station positions in the info line |
| hideEndMarks | hideEndMarks | Hide the START / END marks |

## Dropped (and why)
| GrokPipe id | Why dropped |
|---|---|
| profileOrientation (+ enum GrokOrientation, older/grokpipe/polypipe) | PolyPipe V3.08 world-axis compat only; Height is always the sketch / face normal (or Height reference) |
| startAtDrawn | Compat only; START is always where the path was drawn from (left-most rule removed) |
| squareCutEnds | Compat only; shortened ends are always cut square |
| extFlipEnds, unswapEnds | Legacy Swap Start / End values; each end has its own value |
| placeSketch, showSketch | Old (V3.10.06) Place / Show Sketch check boxes |
| twistType value "none" | Old V3.10.03 off state; the Twist box is the switch |
| PipeType "any", pickedProfile | Hidden, never implemented |
| GrokOffsetType enum | Never used |
| (Version A/B ids rectDentHight*, optionalDents, polygonDentExchSides) | Were not in GrokPipe V3.10.28; nothing carried over |

## Added in B26.10.28.03
| PolyPipeX | What it does |
|---|---|
| shape = roundOver | Round over profile: square corner minus a quarter circle of `radius`; Remove along an outside part edge = a fillet-like round |
| roundOverFlipSide | Put the round on the other side (the side is found automatically from the part edge; sketch edges use one side) |

