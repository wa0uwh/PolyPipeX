// Authors: Grok and Eldon R. Brown - WA0UWH
// Rev: B26.10.28.03 - PolyPipeX B26.10.28.03 - (1) Intersect with "Merge with all" now intersects only the parts the
//                 pipe overlaps (other parts are left alone; before, every part in the Part Studio was intersected
//                 and parts the pipe did not touch vanished). Nothing overlapped = a clear FAILED note. (2) Keep pipe
//                 as a part: stays Remove / Intersect only (Add merges the pipe, so there is nothing to keep); help
//                 says so. (3) Tightest-bend check samples 64 points per edge and refines, so a sharp spot in a
//                 3D spline is found and the Sweep failed message gives the real bend radius (was 17 samples,
//                 could miss a hairpin and blame "a nearby part of the path"). (4) Duplicate path edges (a closed
//                 sketch region gives each edge twice: sketch wire + region boundary) are ignored, the wire edge
//                 is kept. (5) New Profile shape "Round over" (corner minus circle): with Remove on an outside
//                 part edge it rounds the edge like a fillet, the side is found from the part edge; "Round on the
//                 other side" flips it. New ids: shape value roundOver, roundOverFlipSide. (6) Add + hollow + Cut
//                 bore on a picked part edge: the path is copied first (the edge was gone after the merge, so
//                 "the bore could not be made").
// Rev: B26.10.28.02 - PolyPipeX B26.10.28.02 - Name written as one camel-case word "PolyPipeX" everywhere (menu /
//                 feature name, dialog, help, messages, comments, REVISION, file names). Ids unchanged (polyPipeX,
//                 PolyPipeXShape, ...). Shape tests (Circle, Arc, Polygon / Star, Slot, Hollow, Twist, Part of path).
// Rev: B26.10.28.01 - PolyPipeX B26.10.28.01 - experimental, derived from GrokPipe V3.10.28. Clean break from PolyPipe /
//                 GrokPipe: new feature function polyPipeX ("PolyPipeX"), every parameter id renamed to say what it does
//                 (see PARAM_MAP.md), compatibility-only parameters and code removed (Profile orientation / PolyPipe
//                 V3.08 world-axis rule, Swap Start / End values, old Place Sketch / Show Sketch, Twist None, left-most
//                 start rule, straight-in shorten cut, unused Any Profile and Offset type). GrokPipe behaviour is the only
//                 behaviour: start at the drawn start, square cut ends, Height along the sketch / face normal.
//                 Sign clean-ups: Offset values and Extend lengths are positive by default (the flip arrows now mean
//                 "negative" / "shorten"); GrokPipe had them the other way round. Not compatible with PolyPipe or
//                 GrokPipe features (it is a different feature). Not published, not versioned.
// Version scheme: B + YY.MM.DD + daily revision (01, 02, ...; back to 01 on each new day).
// History before B26.10.28.01: see GrokPipe_V3.10.28_20261027.fs (GrokPipe V3.10.00 - V3.10.28, PolyPipe V3.08 - V3.10).

/*
 * 
 * This is an adaption (remix) of the simple "Pipe" Feature Script (Thanks to the Author), 
 *     which has now been modified by: Eldon R. Brown - WA0UWH - 12/2023
 *     
 * This would be useful for a lot of my CAD Designs, as easy to add a "washer" for a bolt, 
 *     edge-trim for a shelf, an O-ring Grove, wire bundles, etc. It would be like the reverse of Filet and Chamfer.
 * 
 */

FeatureScript 2207;
import(path : "onshape/std/common.fs", version : "2207.0");
export import(path : "onshape/std/tool.fs", version : "2207.0"); // Makes NewBodyOperationType visible to Part Studios (per std docs)
import(path : "3021561771935e716a44c1b1/8beb18dbbd39acb4494a8b7b/bfd5c01cc3a5a91aaa35c238", version : "6461ded0564e9d8ab9ff7d92"); // constructPaths (path library, same as GrokPipe)

const REVISION = "PolyPipeX B26.10.28.03"; // ### REV NUMBER: B YY.MM.DD.NN ###################################

const FS_USAGE =
        "{<br><b>PolyPipeX - (FS: <i>" ~ REVISION ~ "</i>)</b>" ~
        "<br>Experimental. Sweeps a profile along selected Path(s): " ~
        "<b>Circle</b>, <b>Arc</b>, <b>Polygon / Star</b>, <b>Rectangle</b> or <b>Slot</b>, solid or hollow, " ~
        "or <b>Round over</b> (corner minus circle: use Remove on an outside edge to round it)." ~
        "<br><br><b>Result:</b> New, Add, Remove, Intersect, like Extrude. " ~
        "<i>Intersect</i> with <i>Merge with all</i> uses only the parts the pipe overlaps. " ~
        "<i>Keep pipe as a part</i> (Remove / Intersect only). <i>Cut bore through target</i> (Add, hollow)." ~
        "<br><br><b>Path:</b> starts where it was drawn from; <i>Reverse</i> flips it. " ~
        "<i>Use part of path</i>, <i>Extend / shorten ends</i>. Duplicate edges are ignored." ~
        "<br><br><b>Twist:</b> turns over the path, or pitch." ~
        "<br><br>Based on FeatureScript 'Pipe'<br>}";

const MAX_CORNERS = 64;

const PERCENT_START_BOUNDS = {
    (unitless) : [0.0, 0.0, 100.0]
} as RealBoundSpec;

const PERCENT_END_BOUNDS = {
    (unitless) : [0.0, 100.0, 100.0]
} as RealBoundSpec;

const POLYGON_CORNER_BOUNDS = {
    (unitless) : [3, 5, MAX_CORNERS]
} as IntegerBoundSpec;

const SIZE_FRACTION_BOUNDS = {
    (unitless) : [0.0, 0.0, 10.0]
} as RealBoundSpec;

const PATH_POSITION_BOUNDS = {
    (unitless) : [0.0, 0.5, 1.0]
} as RealBoundSpec;

const SIZE_BOUNDS = {
    (meter)      : [1e-5, 0.0025, 500],
    (centimeter) : 1,
    (millimeter) : 10.0,
    (inch)       : .5,
    (foot)       : 0.1,
    (yard)       : 0.0025
} as LengthBoundSpec;

const WIDTH_BOUNDS = {
    (meter)      : [1e-5, 0.005, 500],
    (centimeter) : 2,
    (millimeter) : 20.0,
    (inch)       : 1.0,
    (foot)       : 0.2,
    (yard)       : 0.005
} as LengthBoundSpec;

const LENGTH_ZERO_OK_BOUNDS = {
    (meter)      : [0.0, 0.01, 500],
    (centimeter) : 1.0,
    (millimeter) : 10.0,
    (inch)       : 0.5,
    (foot)       : 0.01,
    (yard)       : 0.001
} as LengthBoundSpec;

const SMALL_LENGTH_ZERO_OK_BOUNDS = {
    (meter)      : [0, 0.001, 500],
    (centimeter) : 0.1,
    (millimeter) : 1.0,
    (inch)       : 0.04,
    (foot)       : 0.003,
    (yard)       : 0.001
} as LengthBoundSpec;

const RADIUS_ZERO_OK_BOUNDS = {
    (meter)      : [0.0, 0.005, 500],
    (centimeter) : 0.5,
    (millimeter) : 5.0,
    (inch)       : 0.2,
    (foot)       : 0.016,
    (yard)       : 0.006
} as LengthBoundSpec;

const TWIST_TURNS_BOUNDS = {
    (unitless) : [0.0, 1.0, 1000.0]
} as RealBoundSpec;

const TWIST_MAX_TURNS = 1000;

const TWIST_PITCH_BOUNDS = {
    (meter)      : [1e-5, 0.02, 500],
    (centimeter) : 2.0,
    (millimeter) : 20.0,
    (inch)       : 1.0,
    (foot)       : 0.1,
    (yard)       : 0.025
} as LengthBoundSpec;

const ARC_ANGLE_BOUNDS = {
    (degree) : [1, 60, 360],
    (radian) : 1
} as AngleBoundSpec;

const STATION_COUNT_BOUNDS = {
    (unitless) : [1, 3, 100]
} as IntegerBoundSpec;

export enum PolyPipeXShape {
    annotation { "Name" : "Circle" }            circle,
    annotation { "Name" : "Arc" }               arc,
    annotation { "Name" : "Polygon / Star" }    polygon,
    annotation { "Name" : "Rectangle" }         rectangle,
    annotation { "Name" : "Slot" }              slot,
    annotation { "Name" : "Round over" }        roundOver,
}
export enum PolyPipeXTwistMode {
    annotation { "Name" : "Turns" }  turns,
    annotation { "Name" : "Pitch" }  pitch,
}
export enum PolyPipeXStations {
    annotation { "Name" : "One (preview)" }  one,
    annotation { "Name" : "Evenly spaced" }  evenly,
}

// ##########################################################################
// ##########################################################################
// Setup ####################################################################
annotation { "Feature Type Name" : "PolyPipeX", "Feature Type Description" :  FS_USAGE,
             "Filter Selector" : "allparts",
             "Editing Logic Function" : "polyPipeXEditLogic" }
export const polyPipeX = defineFeature(
    function(context is Context, mainId is Id, definition is map)
    
    precondition
    {
        // ---- Result: New / Add / Remove / Intersect (same UI as Extrude) ----
        booleanStepTypePredicate(definition);
        
        // ---- Profile shape ----
        annotation { "Name" : "Profile shape", "UIHint" : UIHint.REMEMBER_PREVIOUS_VALUE }
        definition.shape is PolyPipeXShape;
        
        // ---- Path ----
        annotation { "Name" : "Paths (FS: " ~ REVISION ~ ")", "Filter" : EntityType.EDGE }
        definition.paths is Query;
        
            annotation { "Name" : "Reverse path direction", "UIHint" : UIHint.OPPOSITE_DIRECTION,
                         "Description" : "START is where the first picked edge was drawn from; this reverses it." }
            definition.reversePath is boolean;
        
        // ---- Profile size ----
        if (definition.shape == PolyPipeXShape.circle || definition.shape == PolyPipeXShape.arc || definition.shape == PolyPipeXShape.roundOver)
        {
            annotation { "Name" : "Radius", "UIHint" : UIHint.REMEMBER_PREVIOUS_VALUE }
            isLength(definition.radius, SIZE_BOUNDS);
            
            if (definition.shape == PolyPipeXShape.roundOver)
            {
                annotation { "Name" : "Round on the other side", "UIHint" : UIHint.OPPOSITE_DIRECTION,
                             "Description" : "Round over = the corner minus a circle, for Remove on an outside part edge (rounds it like a fillet). The side is found from the part edge (sketch edge: one side); this flips it. Rotate turns it further." }
                definition.roundOverFlipSide is boolean;
            }
            
            if (definition.shape == PolyPipeXShape.arc)
            {
                annotation { "Name" : "Arc angle (1 - 360)", "UIHint" : UIHint.REMEMBER_PREVIOUS_VALUE }
                isAngle(definition.arcAngle, ARC_ANGLE_BOUNDS);
            }
        }
        else if (definition.shape == PolyPipeXShape.polygon)
        {
            annotation { "Name" : "Corners", "UIHint" : UIHint.REMEMBER_PREVIOUS_VALUE }
            isInteger(definition.polygonCorners, POLYGON_CORNER_BOUNDS);
            
            annotation { "Name" : "Corner radius", "UIHint" : UIHint.REMEMBER_PREVIOUS_VALUE,
                         "Description" : "Center to each corner" }
            isLength(definition.polygonRadius, SIZE_BOUNDS);
            
            annotation { "Name" : "Side dent depth", "UIHint" : UIHint.REMEMBER_PREVIOUS_VALUE,
                         "Description" : "0 = plain polygon; more = star" }
            isLength(definition.polygonDentDepth, SMALL_LENGTH_ZERO_OK_BOUNDS);
            
                annotation { "Name" : "Bulge out instead", "UIHint" : UIHint.OPPOSITE_DIRECTION }
                definition.polygonDentOutward is boolean;
        }
        else if (definition.shape == PolyPipeXShape.rectangle)
        {
            annotation { "Name" : "Height", "UIHint" : UIHint.REMEMBER_PREVIOUS_VALUE,
                         "Description" : "Along the sketch / face normal (or the Height reference)" }
            isLength(definition.rectHeight, SIZE_BOUNDS);
            
            annotation { "Name" : "Height-end dent depth", "UIHint" : UIHint.REMEMBER_PREVIOUS_VALUE }
            isLength(definition.rectHeightDentDepth, SMALL_LENGTH_ZERO_OK_BOUNDS);
            
                annotation { "Name" : "Bulge out instead", "UIHint" : UIHint.OPPOSITE_DIRECTION }
                definition.rectHeightDentOutward is boolean;
            
            annotation { "Name" : "Width", "UIHint" : UIHint.REMEMBER_PREVIOUS_VALUE }
            isLength(definition.rectWidth, WIDTH_BOUNDS);
            
            annotation { "Name" : "Width-end dent depth", "UIHint" : UIHint.REMEMBER_PREVIOUS_VALUE }
            isLength(definition.rectWidthDentDepth, SMALL_LENGTH_ZERO_OK_BOUNDS);
            
                annotation { "Name" : "Bulge out instead", "UIHint" : UIHint.OPPOSITE_DIRECTION }
                definition.rectWidthDentOutward is boolean;
        }
        else if (definition.shape == PolyPipeXShape.slot)
        {
            annotation { "Name" : "End radius", "UIHint" : UIHint.REMEMBER_PREVIOUS_VALUE }
            isLength(definition.slotEndRadius, RADIUS_ZERO_OK_BOUNDS);
            
            annotation { "Name" : "Straight length", "UIHint" : UIHint.REMEMBER_PREVIOUS_VALUE,
                         "Description" : "Between the two end centers" }
            isLength(definition.slotStraightLength, LENGTH_ZERO_OK_BOUNDS);
            
            annotation { "Name" : "One flat end" }
            definition.slotOneFlatEnd is boolean;
            
            if (definition.slotOneFlatEnd)
            {
                annotation { "Name" : "Flat end at the other side", "UIHint" : UIHint.OPPOSITE_DIRECTION }
                definition.slotSwapFlatEnd is boolean;
            }
        }
        
        // ---- Hollow (not for Round over) ----
        if (definition.shape != PolyPipeXShape.roundOver)
        {
        annotation { "Name" : "Hollow" }
        definition.hollow is boolean;
        
        if (definition.hollow)
        {
            annotation { "Name" : ". . . Wall thickness", "UIHint" : UIHint.REMEMBER_PREVIOUS_VALUE }
            isLength(definition.wallThickness, SMALL_LENGTH_ZERO_OK_BOUNDS);
            
                annotation { "Name" : "Wall outside the profile", "UIHint" : UIHint.OPPOSITE_DIRECTION,
                             "Description" : "Off: wall inside the profile. On: the profile is the hole, the wall is added outside." }
                definition.wallOutside is boolean;
            
            if (definition.shape == PolyPipeXShape.arc)
            {
                annotation { "Name" : ". . . Curved band (no side walls)" }
                definition.arcCurvedBand is boolean;
            }
        }
        }
        
        // ---- Profile placement ----
        if (definition.shape != PolyPipeXShape.circle)
        {
            annotation { "Name" : "Rotate profile" }
            definition.rotateProfile is boolean;
            
            if (definition.rotateProfile)
            {
                annotation { "Name" : ". . . Angle", "UIHint" : UIHint.REMEMBER_PREVIOUS_VALUE }
                isAngle(definition.rotateAngle, ANGLE_360_ZERO_DEFAULT_BOUNDS);
                
                    annotation { "Name" : "Clockwise", "UIHint" : UIHint.OPPOSITE_DIRECTION_CIRCULAR }
                    definition.rotateClockwise is boolean;
            }
        }
        
        annotation { "Name" : "Offset profile from path" }
        definition.offsetProfile is boolean;
        
        if (definition.offsetProfile)
        {
            annotation { "Name" : ". . . Height offset", "UIHint" : UIHint.REMEMBER_PREVIOUS_VALUE }
            isLength(definition.offsetHeight, NONNEGATIVE_ZERO_DEFAULT_LENGTH_BOUNDS);
            
                annotation { "Name" : "Negative", "UIHint" : UIHint.OPPOSITE_DIRECTION }
                definition.offsetHeightNegative is boolean;
            
            annotation { "Name" : ". . . Width offset", "UIHint" : UIHint.REMEMBER_PREVIOUS_VALUE }
            isLength(definition.offsetWidth, NONNEGATIVE_ZERO_DEFAULT_LENGTH_BOUNDS);
            
                annotation { "Name" : "Negative", "UIHint" : UIHint.OPPOSITE_DIRECTION }
                definition.offsetWidthNegative is boolean;
            
            annotation { "Name" : ". . . Height offset, x profile height", "UIHint" : UIHint.REMEMBER_PREVIOUS_VALUE }
            isReal(definition.offsetHeightFraction, SIZE_FRACTION_BOUNDS);
            
                annotation { "Name" : "Negative", "UIHint" : UIHint.OPPOSITE_DIRECTION }
                definition.offsetHeightFractionNegative is boolean;
            
            annotation { "Name" : ". . . Width offset, x profile width", "UIHint" : UIHint.REMEMBER_PREVIOUS_VALUE }
            isReal(definition.offsetWidthFraction, SIZE_FRACTION_BOUNDS);
            
                annotation { "Name" : "Negative", "UIHint" : UIHint.OPPOSITE_DIRECTION }
                definition.offsetWidthFractionNegative is boolean;
        }
        
        annotation { "Name" : "Height reference" }
        definition.useHeightReference is boolean;
        
        if (definition.useHeightReference)
        {
            annotation { "Name" : ". . . Height direction", "Filter" : QueryFilterCompound.ALLOWS_DIRECTION, "MaxNumberOfPicks" : 1,
                         "Description" : "Line, axis or plane normal; default is the sketch / face normal" }
            definition.heightDirection is Query;
        }
        
        // ---- Twist ----
        annotation { "Name" : "Twist" }
        definition.twist is boolean;
        
        if (definition.twist)
        {
            annotation { "Name" : ". . . Twist by", "Default" : PolyPipeXTwistMode.turns, "UIHint" : [UIHint.SHOW_LABEL, UIHint.REMEMBER_PREVIOUS_VALUE] }
            definition.twistMode is PolyPipeXTwistMode;
            
            if (definition.twistMode == PolyPipeXTwistMode.turns)
            {
                annotation { "Name" : ". . . Turns over the path", "UIHint" : UIHint.REMEMBER_PREVIOUS_VALUE }
                isReal(definition.twistTurns, TWIST_TURNS_BOUNDS);
            }
            else
            {
                annotation { "Name" : ". . . Path length per turn", "UIHint" : UIHint.REMEMBER_PREVIOUS_VALUE }
                isLength(definition.twistPitch, TWIST_PITCH_BOUNDS);
            }
            
                annotation { "Name" : "Clockwise", "UIHint" : UIHint.OPPOSITE_DIRECTION_CIRCULAR }
                definition.twistClockwise is boolean;
        }
        
        // ---- Path extent ----
        annotation { "Name" : "Use part of path" }
        definition.usePartOfPath is boolean;
        
        if (definition.usePartOfPath)
        {
            annotation { "Name" : ". . . From %", "UIHint" : UIHint.REMEMBER_PREVIOUS_VALUE }
            isReal(definition.pathStartPercent, PERCENT_START_BOUNDS);
            
            annotation { "Name" : ". . . To %", "UIHint" : UIHint.REMEMBER_PREVIOUS_VALUE }
            isReal(definition.pathEndPercent, PERCENT_END_BOUNDS);
        }
        
        annotation { "Name" : "Extend / shorten ends" }
        definition.changeEnds is boolean;
        
        if (definition.changeEnds)
        {
            annotation { "Name" : ". . . START end (green)", "UIHint" : UIHint.REMEMBER_PREVIOUS_VALUE }
            isLength(definition.startEndLength, SMALL_LENGTH_ZERO_OK_BOUNDS);
            
                annotation { "Name" : "Shorten", "UIHint" : UIHint.OPPOSITE_DIRECTION, "Description" : "Off = extend, on = shorten (cut square)" }
                definition.shortenStartEnd is boolean;
            
            annotation { "Name" : ". . . END end (red)", "UIHint" : UIHint.REMEMBER_PREVIOUS_VALUE }
            isLength(definition.endEndLength, SMALL_LENGTH_ZERO_OK_BOUNDS);
            
                annotation { "Name" : "Shorten", "UIHint" : UIHint.OPPOSITE_DIRECTION, "Description" : "Off = extend, on = shorten (cut square)" }
                definition.shortenEndEnd is boolean;
        }
        
        // ---- Merge (only shown for non-New, same UI as Extrude) ----
        booleanStepScopePredicate(definition);
        
        if (definition.operationType == NewBodyOperationType.REMOVE || definition.operationType == NewBodyOperationType.INTERSECT)
        {
            annotation { "Name" : "Keep pipe as a part",
                         "Description" : "Remove / Intersect only: also keep the pipe as its own part, like Boolean Keep tools (e.g. an O-ring that cuts its own groove). Not offered for Add, where the pipe becomes part of the target." }
            definition.keepPipeAsPart is boolean;
        }
        
        if (definition.operationType == NewBodyOperationType.ADD && definition.hollow && definition.shape != PolyPipeXShape.roundOver)
        {
            annotation { "Name" : "Cut bore through target", "Default" : true }
            definition.cutBoreThroughTarget is boolean;
        }
        
        // ---- Show (debug, dialog) ----
        annotation { "Name" : "Show profiles" }
        definition.showProfiles is boolean;
        
        if (definition.showProfiles)
        {
            annotation { "Name" : ". . . Where", "UIHint" : [UIHint.HORIZONTAL_ENUM, UIHint.REMEMBER_PREVIOUS_VALUE] }
            definition.profileStations is PolyPipeXStations;
            
            if (definition.profileStations == PolyPipeXStations.evenly)
            {
                annotation { "Name" : ". . . How many", "UIHint" : UIHint.REMEMBER_PREVIOUS_VALUE }
                isInteger(definition.stationCount, STATION_COUNT_BOUNDS);
            }
            else
            {
                annotation { "Name" : ". . . At (0.0 - 1.0 of the pipe)", "UIHint" : UIHint.REMEMBER_PREVIOUS_VALUE }
                isReal(definition.previewAt, PATH_POSITION_BOUNDS);
            }
            
            annotation { "Name" : ". . . Dialog only (leave no sketches)", "UIHint" : UIHint.REMEMBER_PREVIOUS_VALUE }
            definition.profilesDialogOnly is boolean;
            
            annotation { "Name" : ". . . Profile start points", "UIHint" : UIHint.REMEMBER_PREVIOUS_VALUE }
            definition.showProfileStartPoints is boolean;
            
            annotation { "Name" : ". . . Path direction arrows", "UIHint" : UIHint.REMEMBER_PREVIOUS_VALUE }
            definition.showPathArrows is boolean;
            
            annotation { "Name" : ". . . List stations (info line)", "UIHint" : UIHint.REMEMBER_PREVIOUS_VALUE }
            definition.listStations is boolean;
        }
        
        annotation { "Name" : "Hide START / END marks",
                     "Description" : "Marks show only while this dialog is open: green = START, red = END." }
        definition.hideEndMarks is boolean;
    }
    
    // ##############################
    // Execution/Main ###############
    //     An expected failure (ppxNote) is reported as a yellow "FAILED: ..." warning, bodies made so far are removed.
    {
    try
    {
        if (isQueryEmpty(context, definition.paths))
            throw regenError("PolyPipeX: select one or more path edges.", ["paths"]);
        definition.paths = uniquePathEdges(context, definition.paths);   // closed sketch regions give each edge twice
        var paths = constructPaths(context, definition.paths);
        definition.arcCurvedBand = (definition.shape == PolyPipeXShape.arc) ? definition.arcCurvedBand == true : false;
        
        if (definition.usePartOfPath == true && definition.pathStartPercent >= definition.pathEndPercent)
            throw ppxNote("PolyPipeX: From % must be less than To %.");
        
        // Hollow = bore swept from an inner profile (no opShell)
        const hollowOn = definition.hollow == true && definition.shape != PolyPipeXShape.roundOver && definition.wallThickness > 0;
        // Add + hollow + Cut bore: merge the solid pipes, then subtract the bore sweeps from the result
        const cutBore = hollowOn && definition.operationType == NewBodyOperationType.ADD && definition.cutBoreThroughTarget == true;
        const mode    = cutBore ? "solid" : "pipe";
        
        // Prepare each path once: drawn start, Reverse, reference normal, part of path, square cut ends, frame
        var pPaths = [];
        var counter = 0;
        for (var path in paths)
        {
            counter += 1;
            const prepId = mainId + "path" + unstableIdComponent(counter);
            setExternalDisambiguation(context, prepId, path.edges[0]);
            pPaths = append(pPaths, preparePath(context, prepId, path, definition));
        }
        
        // Info line
        var infoMsg = "";
        if (definition.showProfiles == true && definition.listStations == true)
        {
            infoMsg = reportTwist(context, mainId, pPaths, definition);
            const si = stationInfo(context, pPaths, definition);
            if (si != "")
                infoMsg = (infoMsg == "" ? "" : infoMsg ~ "  |  ") ~ si;
        }
        else
            infoMsg = shortInfo(context, pPaths, definition);
        var tight = "";
        try silent
        {
            tight = tightBendNote(context, pPaths, definition);
        }
        if (tight != "")
            infoMsg = (infoMsg == "" ? "" : infoMsg ~ " | ") ~ tight;
        if (infoMsg != "")
            reportFeatureInfo(context, mainId, infoMsg);
        
        // Make all the pipe bodies, all are created under mainId
        makePipes(context, mainId, pPaths, definition, mode);
        
        if (definition.showProfiles == true)
            makeStationSketches(context, mainId + "stations", pPaths, definition);
        
        // START / END marks (debug only, dialog only), after the pipe so they are on top
        if (definition.hideEndMarks != true)
        {
            try silent
            {
                showEndMarks(context, pPaths, definition);
            }
        }
        
        // Merge: New / Add / Remove / Intersect, exactly like Extrude.
        //     Intersect + Merge with all: only the parts the pipe overlaps (else every other part would vanish)
        definition = intersectScope(context, mainId, definition);
        const reconstructOp = function(id)
            {
                makePipes(context, id, pPaths, definition, mode);
            };
        const keepOn = definition.keepPipeAsPart == true &&
            (definition.operationType == NewBodyOperationType.REMOVE || definition.operationType == NewBodyOperationType.INTERSECT);
        var kept = false;
        if (keepOn)
        {
            try silent
            {
                keepToolMerge(context, mainId, definition);
                kept = true;
            }
        }
        if (!kept)
            processNewBodyIfNeeded(context, mainId, definition, reconstructOp);
        if (keepOn && !kept)
            reportFeatureWarning(context, mainId, "FAILED: Keep pipe as a part: the Merge could not keep the pipe, so it was done without it (the target is cut, no pipe part kept).");
        
        if (cutBore)
            cutBoresThroughTargets(context, mainId, pPaths, definition);
        
        // Remove the copied (trimmed) path wires
        const wires = qCreatedBy(mainId + "path", EntityType.BODY);
        if (!isQueryEmpty(context, wires))
            opDeleteBodies(context, mainId + "deleteWires", { "entities" : wires });
    }
    catch (problem)
    {
        if (problem is map && problem.ppxNote == true)
        {
            const made = qCreatedBy(mainId, EntityType.BODY);
            if (!isQueryEmpty(context, made))
                opDeleteBodies(context, mainId + "noteCleanup", { "entities" : made });
            reportFeatureWarning(context, mainId, "FAILED: " ~ problem.message);
            return;
        }
        throw problem;
    }
    }, { "operationType" : NewBodyOperationType.NEW });

// ##########################################################################
// ##########################################################################   
// User Functions ###########################################################

// Editing Logic: same boolean heuristics as Extrude and Sweep (a pipe that hits exactly one part switches to Add
//     with that part as the Merge scope, until the user touches Result / Merge with all / Merge scope).
export function polyPipeXEditLogic(context is Context, id is Id, oldDefinition is map, definition is map,
    isCreating is boolean, specifiedParameters is map, hiddenBodies is Query) returns map
{
    return booleanStepEditLogic(context, id, oldDefinition, definition, specifiedParameters, hiddenBodies, polyPipeX);
}

// ##########################################################################
// Paths ####################################################################

const PATH_TOL = 1e-6;            // Fraction of path, and unit-vector tolerance
const LEX_TOL  = 1e-6 * meter;    // Point compare tolerance

// Prepare one Path:
//     1. Start and direction: drawn start (drawnStartPath), then Reverse path direction
//     2. Reference normal from the source edge at the start (sketch plane normal, or adjacent face normal)
//     3. Start % / End % trim (trimPath), on a copy of the path edges
//     4. Sketch frame at the start of the (trimmed) path (frameAt)
function preparePath(context is Context, id is Id, path, definition is map) returns map
{
    path = drawnStartPath(context, path, evaluateQuery(context, definition.paths), definition.reversePath == true);
    
    var s = 0;
    var e = 1;
    if (definition.usePartOfPath == true) {
        s = definition.pathStartPercent / 100;
        e = definition.pathEndPercent / 100;
    }
    
    const startHere = evPathTangentLines(context, path, [s]);
    const srcNormal = edgeReferenceNormal(context, path.edges[startHere.edgeIndices[0]], startHere.tangentLines[0].origin);
    
    var sweepPath = path;
    if (s > PATH_TOL || e < 1 - PATH_TOL) {
        sweepPath = trimPath(context, id, path, s, e);
    }
    else if (definition.operationType == NewBodyOperationType.ADD && definition.hollow == true && definition.cutBoreThroughTarget == true)
    {
        // Cut bore sweeps again after the Add: work on a copy, a picked part edge is gone once the pipe is merged
        sweepPath = trimPath(context, id + "copy", path, 0, 1);
    }
    
    // Square cut ends: shorten amounts trim the path, so the end faces are square to it
    var ext = { "near" : 0 * meter, "far" : 0 * meter, "inPath" : false, "cutStart" : 0 * meter, "cutEnd" : 0 * meter };
    const squareCut = definition.changeEnds == true && !sweepPath.closed;
    if (squareCut)
    {
        const amt = extendAmounts(definition);
        const cS  = max(0 * meter, -amt.start);
        const cE  = max(0 * meter, -amt.end);
        if (cS > LEX_TOL || cE > LEX_TOL)
        {
            const Lp = evPathLength(context, sweepPath);
            if (cS + cE >= Lp - LEX_TOL)
                throw ppxNote("PolyPipeX: Extend / shorten ends: the shorten amounts remove the whole path: make START end / END end smaller than the path length.");
            const ends0 = evPathTangentLines(context, sweepPath, [0, 1]).tangentLines;
            ext.origStart = ends0[0];
            ext.origEnd   = ends0[1];
            sweepPath = trimPath(context, id + "cut", sweepPath, cS / Lp, 1 - cE / Lp);
            ext.cutStart = cS;
            ext.cutEnd   = cE;
        }
    }
    
    // Twist: Extend / shorten ends become part of the path (straight tangent lines), so the twist carries on
    const twistOn = twistIsOn(definition) && twistVisible(definition);
    if (twistOn && definition.changeEnds == true && !sweepPath.closed)
    {
        const ends = extendAmounts(definition);   // each value with its own flip
        ext.near   = squareCut ? max(0 * meter, ends.start) : ends.start;   // minus already trimmed
        ext.far    = squareCut ? max(0 * meter, ends.end)   : ends.end;
        ext.inPath = true;
        sweepPath  = extendPath(context, id + "ext", sweepPath, ext.near, ext.far);
    }
    const shorten = min(ext.near, 0 * meter) + min(ext.far, 0 * meter);
    const twist   = twistFor(context, sweepPath, definition, shorten);
    
    if (twistOn)
    {
        const corner = pathCorner(context, sweepPath);
        if (corner != undefined)
        {
            try silent
            {
                addDebugEntities(context, qUnion(sweepPath.edges), DebugColor.MAGENTA);   // the built path
            }
            if (showSketchOn(definition))
            {
                try silent
                {
                    showCorner(context, { "path" : sweepPath, "srcNormal" : srcNormal, "twist" : twist,
                                          "frame" : frameAt(context, evPathTangentLines(context, sweepPath, [0]).tangentLines[0], srcNormal, definition) },
                               corner, definition);
                }
            }
            throw regenError("PolyPipeX: Twist needs a smooth path: fillet the sharp corners or uncheck Twist. (First sharp corner: " ~
                             roundToPrecision(corner.angle / degree, 1) ~ " deg at " ~ fmtPoint(corner.point) ~ " mm.)" ~ problemHint(definition), ["paths"]);   // the path only (a sharp corner)
        }
    }
    
    // A closed loop is swept in two halves, the second half starts from the end face of the first
    var halves = undefined;
    if (abs(twist.angle) > 1e-9 && sweepPath.closed)
        halves = [trimPath(context, id + "half0", sweepPath, 0, 0.5), trimPath(context, id + "half1", sweepPath, 0.5, 1)];
    
    const startLine = evPathTangentLines(context, sweepPath, [0]).tangentLines[0];
    // Round over: which side of the picked part edge the part is on (sketch edge: the -y side)
    var roundSide = undefined;
    if (definition.shape == PolyPipeXShape.roundOver)
        roundSide = roundSideFor(context, path.edges[startHere.edgeIndices[0]], startHere.tangentLines[0].origin,
                                 frameAt(context, startHere.tangentLines[0], srcNormal, definition));
    return {
        "roundSide" : roundSide,
        "path"      : sweepPath,
        "srcNormal" : srcNormal,
        "frame"     : frameAt(context, startLine, srcNormal, definition),
        "disambig"  : path.edges[0],
        "twist"     : twist,
        "ext"       : ext,
        "halves"    : halves
    };
}

// Extend / shorten ends: signed amounts at the path START and END (+ extend, - shorten)
function extendAmounts(definition is map) returns map
{
    return { "start" : (definition.shortenStartEnd == true ? -1 : 1) * definition.startEndLength,
             "end"   : (definition.shortenEndEnd   == true ? -1 : 1) * definition.endEndLength };
}

// Twist + Extend / shorten ends: add straight lines, tangent to the path, at the ends (positive amounts only)
function extendPath(context is Context, id is Id, path, near is ValueWithUnits, far is ValueWithUnits)
{
    const ends = evPathTangentLines(context, path, [0, 1]).tangentLines;
    var extra = [];
    var start = ends[0].origin;
    if (near > LEX_TOL)
    {
        start = ends[0].origin - normalize(ends[0].direction) * near;
        opFitSpline(context, id + "near", { "points" : [start, ends[0].origin] });
        extra = append(extra, qCreatedBy(id + "near", EntityType.EDGE));
    }
    if (far > LEX_TOL)
    {
        opFitSpline(context, id + "far", { "points" : [ends[1].origin, ends[1].origin + normalize(ends[1].direction) * far] });
        extra = append(extra, qCreatedBy(id + "far", EntityType.EDGE));
    }
    if (size(extra) == 0)
        return path;
    var out = constructPath(context, qUnion(concatenateArrays([path.edges, extra])), {}).path;
    const first = evPathTangentLines(context, out, [0]).tangentLines[0].origin;
    if (norm(first - start) > LEX_TOL)
        out = reverse(out);
    return out;
}

// First sharp corner between edges (more than 0.5 deg): { point, angle }, or undefined
function pathCorner(context is Context, path)
{
    const n = size(path.edges);
    const last = path.closed ? n : n - 1;
    for (var i = 0; i < last; i += 1)
    {
        const j = (i + 1) % n;
        const a = evEdgeTangentLine(context, { "edge" : path.edges[i], "parameter" : path.flipped[i] ? 0 : 1 }).direction * (path.flipped[i] ? -1 : 1);
        const tb = evEdgeTangentLine(context, { "edge" : path.edges[j], "parameter" : path.flipped[j] ? 1 : 0 });
        const b  = tb.direction * (path.flipped[j] ? -1 : 1);
        const c  = dot(normalize(a), normalize(b));
        if (c < cos(0.5 * degree))
            return { "point" : tb.origin, "angle" : acos(max(-1, min(1, c))), "inDir" : normalize(a), "outDir" : normalize(b), "i" : i, "j" : j };
    }
    return undefined;
}

// "x, y, z" in mm, 2 decimals
function fmtPoint(p is Vector) returns string
{
    return roundToPrecision(p[0] / millimeter, 2) ~ ", " ~ roundToPrecision(p[1] / millimeter, 2) ~ ", " ~ roundToPrecision(p[2] / millimeter, 2);
}

// Largest distance of a profile loop from the path (the sketch origin), including Center Offset
function profileExtent(loop is array) returns ValueWithUnits
{
    var r = 0 * meter;
    for (var seg in loop)
    {
        if (seg.kind == "circle")
            r = max(r, norm(seg.c) + seg.r);
        else
        {
            r = max(r, max(norm(seg.a), norm(seg.b)));
            if (seg.kind == "arc")
                r = max(r, norm(seg.m));
        }
    }
    return r;
}

// too-wide failure text: reach vs tightest bend, ~% along the path, a size to aim for (0.9 x R)
function tooWideMessage(context is Context, pp is map, bend is map, X is ValueWithUnits, twisted is boolean, definition is map) returns string
{
    const R = bend.radius;
    var where = "";
    try silent
    {
        var cum = 0 * meter;
        var at  = 0 * meter;
        for (var i = 0; i < size(pp.path.edges); i += 1)
        {
            const len = evLength(context, { "entities" : pp.path.edges[i] });
            if (i == bend.edge)
                at = cum + len * (pp.path.flipped[i] ? 1 - bend.param : bend.param);
            cum += len;
        }
        where = " at ~" ~ round(100 * at / cum) ~ "% along the path";
    }
    const f   = 0.9 * R / X;     // scale that gives a reach of 0.9 x R
    var aim = "the profile below ~" ~ roundToPrecision(0.9 * R / millimeter, 1) ~ " mm reach (~" ~ round(100 * f) ~ "% of its size)";
    if (definition.shape == PolyPipeXShape.rectangle)
        aim = "Width / Height below ~" ~ roundToPrecision(definition.rectWidth * f / millimeter, 1) ~ " / " ~
               roundToPrecision(definition.rectHeight * f / millimeter, 1) ~ " mm";
    return "PolyPipeX: " ~ (twisted ? "Twisted profile intersects itself" : "Sweep failed, the profile intersects itself") ~
           ": profile reach " ~ roundToPrecision(X / millimeter, 1) ~ " mm vs tightest bend R " ~ roundToPrecision(R / millimeter, 2) ~ " mm" ~ where ~
           ": reduce " ~ aim ~ " or ease the bend" ~ (twisted ? "; with Twist, try other turn counts." : ".");
}

// "tight bend" note for the info line: reach >= 0.9 x the tightest bend radius, else ""
function tightBendNote(context is Context, pPaths is array, definition is map) returns string
{
    const X = profileExtent(makeProfiles(definition).outer);
    var worst = undefined;
    for (var pp in pPaths)
    {
        const b = pathMinBend(context, pp.path);
        if (b != undefined && (worst == undefined || b.radius < worst))
            worst = b.radius;
    }
    if (worst == undefined || X < 0.9 * worst)
        return "";
    return "tight bend: profile " ~ roundToPrecision(X / millimeter, 1) ~ " / R " ~ roundToPrecision(worst / millimeter, 1) ~ " mm";
}

const BEND_SAMPLES = 64;   // curvature samples per edge (then refined around the sharpest one)

// Tightest bend: { radius, point }, undefined if straight
function pathMinBend(context is Context, path)
{
    var params = [];
    for (var i = 0; i <= BEND_SAMPLES; i += 1)
        params = append(params, i / BEND_SAMPLES);
    var kMax = 0 / meter;
    var at = undefined;
    var atEdge = 0;
    var atParam = 0;
    for (var ei = 0; ei < size(path.edges); ei += 1)
    {
        const edge = path.edges[ei];
        if (try silent(evCurveDefinition(context, { "edge" : edge })) is Line)
            continue;      // a straight line has no bend
        const cs = try silent(evEdgeCurvatures(context, { "edge" : edge, "parameters" : params }));
        if (cs != undefined)
            for (var ci = 0; ci < size(cs); ci += 1)
                if (abs(cs[ci].curvature) > kMax) {
                    kMax = abs(cs[ci].curvature);
                    at = cs[ci].frame.origin;
                    atEdge = ei;
                    atParam = params[ci];
                }
    }
    if (kMax * meter < 1e-6)
        return undefined;
    // Refine around the sharpest sample (a hairpin can fall between samples)
    var fine = [];
    for (var i = -16; i <= 16; i += 1)
        fine = append(fine, max(0, min(1, atParam + i / (16 * BEND_SAMPLES))));
    const cf = try silent(evEdgeCurvatures(context, { "edge" : path.edges[atEdge], "parameters" : fine }));
    if (cf != undefined)
        for (var ci = 0; ci < size(cf); ci += 1)
            if (abs(cf[ci].curvature) > kMax) {
                kMax = abs(cf[ci].curvature);
                at = cf[ci].frame.origin;
                atParam = fine[ci];
            }
    return { "radius" : 1 / kMax, "point" : at, "edge" : atEdge, "param" : atParam };
}

// ##########################################################################
// Show the problem area: debug lines only, shown in the dialog, nothing left in the model

function showSketchOn(definition is map) returns boolean
{
    return definition.showProfiles == true;
}

function problemHint(definition is map) returns string
{
    return showSketchOn(definition) ? " (Show profiles: problem area shown in red)" : " Check Show profiles to see the problem area.";
}

// Fraction (0..1) along the path of a point on (or near) the path
function pathFractionOf(context is Context, path, point is Vector) returns number
{
    const L = evPathLength(context, path);
    var cum  = [0 * meter];
    var lens = [];
    for (var edge in path.edges)
    {
        const l = evLength(context, { "entities" : edge });
        lens = append(lens, l);
        cum  = append(cum, cum[size(cum) - 1] + l);
    }
    return max(0, min(1, pathParameterAt(context, path, cum, lens, L, point)));
}

// Profile frame at a fraction of the path, turned by the twist so far (same as Show profiles)
function frameAtFraction(context is Context, pp is map, f is number, definition is map) returns Plane
{
    const at = evPathTangentLines(context, pp.path, [f]).tangentLines[0];
    var frame = frameAt(context, at, pp.srcNormal, definition);
    if (pp.twist != undefined && abs(pp.twist.angle) > 1e-9)
    {
        const a = pp.twist.angle * f * radian;
        frame = plane(frame.origin, frame.normal, frame.x * cos(a) + yAxis(frame) * sin(a));
    }
    return frame;
}

// Draw a profile loop with debug lines in a frame
function debugLoop(context is Context, frame is Plane, loop is array, color is DebugColor)
{
    const y = yAxis(frame);
    const w = function(p) { return frame.origin + frame.x * p[0] + y * p[1]; };
    for (var seg in loop)
    {
        var pts = [];
        if (seg.kind == "line")
            pts = [seg.a, seg.b];
        else if (seg.kind == "circle")
        {
            for (var i = 0; i <= 24; i += 1)
                pts = append(pts, seg.c + vector(cos(i * 15 * degree), sin(i * 15 * degree)) * seg.r);
        }
        else
            pts = arcPoints(seg.a, seg.m, seg.b, 12);
        for (var i = 0; i + 1 < size(pts); i += 1)
            addDebugLine(context, w(pts[i]), w(pts[i + 1]), color);
    }
}

// Points along the arc through a, m, b (2D, sketch units)
function arcPoints(a is Vector, m is Vector, b is Vector, n is number) returns array
{
    const A = a / meter;
    const M = m / meter;
    const B = b / meter;
    const d = 2 * (A[0] * (M[1] - B[1]) + M[0] * (B[1] - A[1]) + B[0] * (A[1] - M[1]));
    if (abs(d) < 1e-14)
        return [a, m, b];
    const ux = (squaredNorm(A) * (M[1] - B[1]) + squaredNorm(M) * (B[1] - A[1]) + squaredNorm(B) * (A[1] - M[1])) / d;
    const uy = (squaredNorm(A) * (B[0] - M[0]) + squaredNorm(M) * (A[0] - B[0]) + squaredNorm(B) * (M[0] - A[0])) / d;
    const C  = vector(ux, uy);
    const r  = norm(A - C);
    const t0 = atan2(A[1] - C[1], A[0] - C[0]) / radian;
    const tm = atan2(M[1] - C[1], M[0] - C[0]) / radian;
    const t1 = atan2(B[1] - C[1], B[0] - C[0]) / radian;
    var d1 = t1 - t0;
    while (d1 <= 0)
        d1 += 2 * PI;
    var dm = tm - t0;
    while (dm < 0)
        dm += 2 * PI;
    const sweep = (dm > d1) ? d1 - 2 * PI : d1;
    var pts = [];
    for (var i = 0; i <= n; i += 1)
    {
        const t = (t0 + sweep * i / n) * radian;
        pts = append(pts, (C + vector(cos(t), sin(t)) * r) * meter);
    }
    return pts;
}

// Closest approach of the path to itself, away from the local neighbourhood (e.g. the next helix turn).
//     Returns { "f0", "f1", "p0", "p1", "dist" } or undefined. 96 samples, cheap.
function findClash(context is Context, path, X is ValueWithUnits)
{
    const N = 96;
    const L = evPathLength(context, path);
    var params = [];
    for (var i = 0; i <= N; i += 1)
        params = append(params, i / N);
    const pts = evPathTangentLines(context, path, params).tangentLines;
    const minGap = max(3 * X, 3 * L / N);
    var best = undefined;
    for (var i = 0; i <= N; i += 1)
    {
        for (var j = i + 1; j <= N; j += 1)
        {
            var gap = (j - i) / N * L;
            if (path.closed)
                gap = min(gap, L - gap);
            if (gap < minGap)
                continue;
            const dist = norm(pts[j].origin - pts[i].origin);
            if (dist < 0.7 * gap && (best == undefined || dist < best.dist))
                best = { "f0" : i / N, "f1" : j / N, "p0" : pts[i].origin, "p1" : pts[j].origin, "dist" : dist };
        }
    }
    if (best == undefined || best.dist > 2.2 * X)
        return undefined;
    return best;
}

// Sweep failed: profile at the start (green), and the problem area (red)
function showSweepProblem(context is Context, pp is map, loop is array, definition is map)
{
    debugLoop(context, pp.frame, loop, DebugColor.GREEN);
    const X = profileExtent(loop);
    const bend = pathMinBend(context, pp.path);
    if (bend != undefined && X >= bend.radius * 0.98)
    {
        // Too wide for the bend: the profile at the tightest bend
        addDebugPoint(context, bend.point, DebugColor.RED);
        debugLoop(context, frameAtFraction(context, pp, pathFractionOf(context, pp.path, bend.point), definition), loop, DebugColor.RED);
        return;
    }
    const clash = findClash(context, pp.path, X);
    if (clash != undefined)
    {
        // Hits a nearby part of the path: the profile at both places, and a line between them
        debugLoop(context, frameAtFraction(context, pp, clash.f0, definition), loop, DebugColor.RED);
        debugLoop(context, frameAtFraction(context, pp, clash.f1, definition), loop, DebugColor.RED);
        addDebugPoint(context, clash.p0, DebugColor.RED);
        addDebugPoint(context, clash.p1, DebugColor.RED);
        addDebugLine(context, clash.p0, clash.p1, DebugColor.RED);
        return;
    }
    // Not found: the profile at the tightest bend and at the end of the path
    if (bend != undefined)
    {
        addDebugPoint(context, bend.point, DebugColor.RED);
        debugLoop(context, frameAtFraction(context, pp, pathFractionOf(context, pp.path, bend.point), definition), loop, DebugColor.RED);
    }
    debugLoop(context, frameAtFraction(context, pp, 1, definition), loop, DebugColor.RED);
}

// Twist + sharp corner: the corner point, its two edges, and the profile at the corner (red)
function showCorner(context is Context, pp is map, corner is map, definition is map)
{
    const loop = makeProfiles(definition).outer;
    debugLoop(context, pp.frame, loop, DebugColor.GREEN);
    addDebugPoint(context, corner.point, DebugColor.RED);
    addDebugEntities(context, qUnion([pp.path.edges[corner.i], pp.path.edges[corner.j]]), DebugColor.RED);
    const X = max(profileExtent(loop), 1 * millimeter);
    addDebugLine(context, corner.point - corner.inDir * 3 * X, corner.point, DebugColor.RED);
    addDebugLine(context, corner.point, corner.point + corner.outDir * 3 * X, DebugColor.RED);
    // Profile square to the incoming edge, at the corner
    var frame = frameAt(context, line(corner.point, corner.inDir), pp.srcNormal, definition);
    if (pp.twist != undefined && abs(pp.twist.angle) > 1e-9)
    {
        const a = pp.twist.angle * pathFractionOf(context, pp.path, corner.point) * radian;
        frame = plane(frame.origin, frame.normal, frame.x * cos(a) + yAxis(frame) * sin(a));
    }
    debugLoop(context, frame, loop, DebugColor.RED);
}

// Helpful message when a sweep fails: profile extent vs the path's tightest bend
function sweepFailMessage(context is Context, pp is map, loop is array, twisted is boolean, definition is map) returns string
{
    const X  = profileExtent(loop);
    const bend = pathMinBend(context, pp.path);
    const R  = (bend == undefined) ? undefined : bend.radius;
    const xs = roundToPrecision(X / millimeter, 2) ~ " mm";
    const rs = (R == undefined) ? "straight" : roundToPrecision(R / millimeter, 2) ~ " mm";
    const tooWide = R != undefined && X >= R * 0.98;
    if (tooWide)
    {
        const m = try silent(tooWideMessage(context, pp, bend, X, twisted, definition));   // numbers, where, a size to aim for
        if (m != undefined)
            return m;
        return "PolyPipeX: " ~ (twisted ? "Twisted profile intersects itself" : "Sweep failed, the profile intersects itself") ~
               ": the profile (max extent " ~ xs ~ ") is too wide for the path curvature (min bend radius " ~ rs ~ "). Reduce the profile width or increase the bend radius.";
    }
    if (twisted)
    {
        return "PolyPipeX: Twisted profile intersects itself: the profile (max extent " ~ xs ~ ") fits the path curvature (min bend radius " ~ rs ~
               "), so it hits a nearby part of the path (e.g. the next helix turn, keep them more than " ~ roundToPrecision(2 * X / millimeter, 2) ~
               " mm apart) or the twist is too tight. Reduce the profile width, space the path out, or use fewer Turns / a longer Pitch.";
    }
    return "PolyPipeX: Sweep failed: the profile (max extent " ~ xs ~ ", min bend radius " ~ rs ~ ") may hit a nearby part of the path " ~
           "(e.g. the next helix turn, keep them more than " ~ roundToPrecision(2 * X / millimeter, 2) ~ " mm apart), or the path has a gap or a sharp corner the profile cannot follow.";
}

// ##########################################################################
// Twist #########################################################
//     The native Sweep twist turns the profile about the path by "angle" (radians), spread evenly
//     along the path length. Positive = counter-clockwise looking back from the end of the path
//     (right hand rule about the path direction, same sense as Rotate). Twist Clockwise = clockwise.
//     Count: turns over the swept path. Pitch: swept path length / pitch.
//     Closed loop (not trimmed): rounded to whole turns, else the ends would not meet.
//     A centered Circular profile looks the same at any angle, so it is not twisted.

// The Twist check box
function twistIsOn(definition is map) returns boolean
{
    return definition.twist == true;
}

// shorten (<= 0): Extend / shorten ends minus amounts, cut off after the sweep. Turns are for the length that is left.
function twistFor(context is Context, sweepPath, definition is map, shorten is ValueWithUnits) returns map
{
    const Lswept = evPathLength(context, sweepPath);
    const L = Lswept + shorten;
    var out = { "turns" : 0, "asked" : 0, "length" : L, "rounded" : false, "angle" : 0 };
    if (!twistIsOn(definition))
        return out;
    if (L <= LEX_TOL)
        throw ppxNote("PolyPipeX: Extend / shorten ends: the shorten amounts remove the whole path: make START end / END end smaller than the path length.");
    
    var turns = 0;
    if (definition.twistMode != PolyPipeXTwistMode.pitch)
        turns = definition.twistTurns;
    else
        turns = L / definition.twistPitch;
    out.asked = turns;
    
    if (turns > TWIST_MAX_TURNS)
        throw ppxNote("PolyPipeX: Twist is " ~ roundToPrecision(turns, 1) ~ " turns, the limit is " ~ TWIST_MAX_TURNS ~
                         ": use fewer Turns or a longer Pitch.");
    
    if (sweepPath.closed && abs(turns - round(turns)) > 1e-6)
    {
        turns = max(round(turns), 1);
        out.rounded = true;
    }
    out.turns = turns;
    
    if (!twistVisible(definition))
        return out;
    // The sweep runs over the whole swept path, the shortened ends are cut off after
    out.angle = (definition.twistClockwise == true ? -1 : 1) * turns * (Lswept / L) * 2 * PI;
    return out;
}

// A centered full circle does not change when it turns
function twistVisible(definition is map) returns boolean
{
    const isFullCircle = definition.shape == PolyPipeXShape.circle ||
                        (definition.shape == PolyPipeXShape.arc && definition.arcAngle >= 360 * degree);
    return !isFullCircle || definition.offsetProfile == true;
}

// Feature info: turns and length used for each path (first 3 paths)
// Short info line, per path: "1 turn | 125.66 mm | START -20, END +5"; "" when nothing is noteworthy.
//     Turns and length when twisted; end changes (signed, mm) with Show profiles on, a 0 end is left out.
function shortInfo(context is Context, pPaths is array, definition is map) returns string
{
    const twistOn = twistIsOn(definition);
    if (twistOn && !twistVisible(definition))
        return "Twist skipped: centered circle";
    const showEnds = definition.showProfiles == true;
    var out = "";
    var n = 0;
    for (var pp in pPaths)
    {
        n += 1;
        if (n > 2) {
            if (out != "")
                out ~= " ...";
            break;
        }
        var parts = [];
        var ends = "";
        var built = undefined;
        if (showEnds)
        {
            const span = builtSpan(context, pp, definition);
            built = span.a1 - span.a0;
            if (abs(span.start) > LEX_TOL)
                ends = "START " ~ signedNum(span.start);
            if (abs(span.end) > LEX_TOL)
                ends ~= (ends == "" ? "" : ", ") ~ "END " ~ signedNum(span.end);
        }
        if (twistOn)
        {
            if (pp.twist.rounded)
                parts = append(parts, "Twist rounded " ~ roundToPrecision(pp.twist.asked, 2) ~ " -> " ~ roundToPrecision(pp.twist.turns, 3) ~ " turns (closed)");
            else
                parts = append(parts, roundToPrecision(pp.twist.turns, 3) ~ (abs(pp.twist.turns - 1) < 1e-9 ? " turn" : " turns"));
            parts = append(parts, roundToPrecision(pp.twist.length / millimeter, 2) ~ " mm");
        }
        else if (ends != "")
            parts = append(parts, roundToPrecision(built / millimeter, 2) ~ " mm");
        if (ends != "")
            parts = append(parts, ends);
        var line = "";
        for (var p in parts)
            line ~= (line == "" ? "" : " | ") ~ p;
        if (line != "")
            out ~= (out == "" ? "" : "; ") ~ line;
    }
    return out;
}

function signedNum(v is ValueWithUnits) returns string
{
    const x = roundToPrecision(v / millimeter, 2);
    return (x > 0 ? "+" : "") ~ x;
}

function reportTwist(context is Context, id is Id, pPaths is array, definition is map) returns string
{
    if (!twistIsOn(definition))
        return "";
    var msg = "Twist:";
    var n = 0;
    for (var pp in pPaths)
    {
        n += 1;
        if (n > 3) {
            msg ~= " ...";
            break;
        }
        msg ~= (n > 1 ? ";" : "") ~ " " ~ roundToPrecision(pp.twist.turns, 3) ~ (abs(pp.twist.turns - 1) < 1e-9 ? " turn" : " turns") ~ " over " ~
               roundToPrecision(pp.twist.length / millimeter, 2) ~ " mm" ~
               (pp.twist.rounded ? " (closed loop: rounded from " ~ roundToPrecision(pp.twist.asked, 3) ~ " to whole turns so the ends meet)" : "");
    }
    if (!twistVisible(definition))
        msg = "Twist has no effect on a centered circle (it looks the same at every angle), so it is not twisted.";
    return msg;
}

// Drawn start: the first picked edge of this path runs in its own direction (its parameter 0 -> 1);
//     a closed loop starts at the start of that edge. Reverse path direction reverses the result.
function drawnStartPath(context is Context, path, selected is array, flipStart is boolean)
{
    var first = undefined;
    for (var sel in selected)
    {
        for (var i = 0; i < size(path.edges); i += 1)
        {
            if (!isQueryEmpty(context, qIntersection([path.edges[i], sel])))
            {
                first = sel;
                break;
            }
        }
        if (first != undefined)
            break;
    }
    if (first != undefined)
    {
        var i = indexOfEdge(context, path, first);
        if (path.flipped[i])
        {
            path = reverse(path);
            i = indexOfEdge(context, path, first);
        }
        if (path.closed && i > 0)
        {
            const n = size(path.edges);
            path.edges   = concatenateArrays([subArray(path.edges,   i, n), subArray(path.edges,   0, i)]);
            path.flipped = concatenateArrays([subArray(path.flipped, i, n), subArray(path.flipped, 0, i)]);
        }
    }
    if (flipStart)
        path = reverse(path);
    return path;
}

// Path edges without duplicates: a closed sketch region gives each edge twice (sketch wire + region boundary).
//     Same midpoint and length = the same edge; the wire edge is kept, the picking order is kept.
function uniquePathEdges(context is Context, paths is Query) returns Query
{
    const picked = evaluateQuery(context, paths);
    if (size(picked) < 2)
        return paths;
    var kept = [];
    var seen = [];
    for (var e in picked)
    {
        const p = evEdgeTangentLine(context, { "edge" : e, "parameter" : 0.5 }).origin;
        const L = evLength(context, { "entities" : e });
        const isWire = !isQueryEmpty(context, qBodyType(e, BodyType.WIRE));
        var dup = -1;
        for (var i = 0; i < size(seen); i += 1)
            if (norm(seen[i].p - p) < LEX_TOL && abs(seen[i].L - L) < LEX_TOL)
                dup = i;
        if (dup == -1)
        {
            kept = append(kept, e);
            seen = append(seen, { "p" : p, "L" : L, "wire" : isWire });
        }
        else if (isWire && !seen[dup].wire)
        {
            kept[dup] = e;
            var sd = seen[dup];
            sd.wire = true;
            seen[dup] = sd;
        }
    }
    if (size(kept) == size(picked))
        return paths;
    return qUnion(kept);
}

// Intersect + Merge with all: Merge scope = only the parts the pipe overlaps (touching faces do not count).
//     Other parts are left alone. Nothing overlapped = FAILED note (Intersect would leave nothing).
function intersectScope(context is Context, mainId is Id, definition is map) returns map
{
    if (definition.operationType != NewBodyOperationType.INTERSECT || definition.defaultScope == false)
        return definition;
    const pipes = qBodyType(qCreatedBy(mainId, EntityType.BODY), BodyType.SOLID);
    const others = qSubtraction(qAllModifiableSolidBodiesNoMesh(), pipes);
    var hit = [];
    if (!isQueryEmpty(context, pipes) && !isQueryEmpty(context, others))
    {
        const clashes = try silent(evCollision(context, { "tools" : pipes, "targets" : others }));
        if (clashes != undefined)
            for (var c in clashes)
                if (!match(c["type"] ~ "", ".*ABUT_(NO_CLASS|TOOL_OUT_TARGET).*").hasMatch)
                    hit = append(hit, c.targetBody);
    }
    if (size(hit) == 0)
        throw ppxNote("PolyPipeX: Intersect: the pipe does not overlap any part, so nothing would be left. Move the path onto a part, or use New.");
    definition.defaultScope = false;
    definition.booleanScope = qUnion(hit);
    return definition;
}

// Round over on a part edge: the side face is the adjacent face most square to the Height (frame x);
//     the part is behind its outward normal, so the corner region goes to that side of y. Sketch edge: -1.
function roundSideFor(context is Context, edge is Query, point is Vector, frame is Plane) returns number
{
    const y = yAxis(frame);
    var best = undefined;
    var bestScore = 2;
    for (var face in evaluateQuery(context, qAdjacent(edge, AdjacencyType.EDGE, EntityType.FACE)))
    {
        const hit = try silent(evDistance(context, { "side0" : face, "side1" : point }));
        if (hit != undefined)
        {
            const tp = try silent(evFaceTangentPlane(context, { "face" : face, "parameter" : hit.sides[0].parameter }));
            if (tp != undefined && abs(dot(tp.normal, frame.x)) < bestScore)
            {
                bestScore = abs(dot(tp.normal, frame.x));
                best = tp.normal;
            }
        }
    }
    if (best == undefined || abs(dot(best, y)) < PATH_TOL)
        return -1;
    return dot(best, y) > 0 ? -1 : 1;
}

function indexOfEdge(context is Context, path, edge) returns number
{
    for (var i = 0; i < size(path.edges); i += 1)
    {
        if (!isQueryEmpty(context, qIntersection([path.edges[i], edge])))
            return i;
    }
    return 0;
}

// Reference normal for the profile x-axis, from the source edge:
//     sketch edge -> the sketch plane normal
//     solid edge  -> an adjacent face normal at the point (the most World-Z face, then most World-X)
//     otherwise   -> undefined (frameAt then uses World Z, then World X)
function edgeReferenceNormal(context is Context, edge is Query, point is Vector)
{
    const sketchPlane = try silent(evOwnerSketchPlane(context, { "entity" : edge }));
    if (sketchPlane != undefined)
        return sketchPlane.normal;
    
    var best = undefined;
    for (var face in evaluateQuery(context, qAdjacent(edge, AdjacencyType.EDGE, EntityType.FACE)))
    {
        const hit = try silent(evDistance(context, { "side0" : face, "side1" : point }));
        if (hit != undefined)
        {
            const tangentPlane = try silent(evFaceTangentPlane(context, { "face" : face, "parameter" : hit.sides[0].parameter }));
            if (tangentPlane != undefined && (best == undefined || isBetterReference(tangentPlane.normal, best)))
                best = tangentPlane.normal;
        }
    }
    return best;
}

function isBetterReference(a is Vector, b is Vector) returns boolean
{
    if (abs(a[2]) > abs(b[2]) + PATH_TOL)
        return true;
    if (abs(a[2]) < abs(b[2]) - PATH_TOL)
        return false;
    return abs(a[0]) > abs(b[0]) + PATH_TOL;
}

// Sketch frame: normal = path tangent, x = first usable reference projected onto the sketch plane:
//     user Height direction, then source edge normal, then World Z, then World X.
function frameAt(context is Context, tangentLine is Line, srcNormal, definition is map) returns Plane
{
    const t = normalize(tangentLine.direction);
    var refs = [];
    if (definition.useHeightReference == true)
    {
        const userDir = extractDirection(context, definition.heightDirection);
        if (userDir == undefined)
            throw regenError("PolyPipeX: pick a line, axis or plane for the Height direction.", ["heightDirection"]);
        refs = append(refs, userDir);
    }
    if (srcNormal != undefined)
        refs = append(refs, srcNormal);
    refs = append(refs, vector(0, 0, 1));
    refs = append(refs, vector(1, 0, 0));
    
    for (var r in refs)
    {
        const x = r - t * dot(r, t);
        if (norm(x) > PATH_TOL)
        {
            return plane(tangentLine.origin, t, normalize(x));
        }
    }
    return plane(tangentLine.origin, t); // Not reached, Z and X can not both be parallel to t
}

// Start % / End %: copy the path edges (opExtractWires), split the copies (opSplitEdges) at s and e,
//     keep the pieces whose middle is between s and e, and make a new Path from them.
//     The user's edges are not modified. Works for multi-edge, curved, 3D and closed paths.
function trimPath(context is Context, id is Id, path, s is number, e is number)
{
    const L = evPathLength(context, path);
    var cum  = [0 * meter];
    var lens = [];
    for (var edge in path.edges)
    {
        const l = evLength(context, { "entities" : edge });
        lens = append(lens, l);
        cum  = append(cum, cum[size(cum) - 1] + l);
    }
    
    const wireId = id + "wires";
    opExtractWires(context, wireId, { "edges" : qUnion(path.edges) });
    const wireEdgesQ = qOwnedByBody(qCreatedBy(wireId, EntityType.BODY), EntityType.EDGE);
    const wireEdges  = evaluateQuery(context, wireEdgesQ);
    
    // Split points on the copies
    const ends = evPathTangentLines(context, path, [s, e]).tangentLines;
    var splitIdx    = [];
    var splitParams = [];
    for (var k = 0; k < 2; k += 1)
    {
        const u = (k == 0) ? s : e;
        if (u > PATH_TOL && u < 1 - PATH_TOL)
        {
            const hit = evDistance(context, { "side0" : qUnion(wireEdges), "side1" : ends[k].origin, "arcLengthParameterization" : true });
            const tEdge = hit.sides[0].parameter;
            if (tEdge > PATH_TOL && tEdge < 1 - PATH_TOL)
            {
                const idx = hit.sides[0].index;
                var found = -1;
                for (var j = 0; j < size(splitIdx); j += 1)
                {
                    if (splitIdx[j] == idx)
                        found = j;
                }
                if (found == -1) {
                    splitIdx    = append(splitIdx, idx);
                    splitParams = append(splitParams, [tEdge]);
                }
                else if (tEdge < splitParams[found][0]) {
                    splitParams[found] = [tEdge, splitParams[found][0]];
                }
                else {
                    splitParams[found] = [splitParams[found][0], tEdge];
                }
            }
        }
    }
    // A closed edge (e.g. a sketch circle) can not be split at only one point: when Start = 0 or End = 100
    //     its seam is the other cut, so add a temporary second split in the longer piece;
    //     the seam itself is cut in the second pass below
    for (var j = 0; j < size(splitIdx); j += 1)
    {
        if (size(splitParams[j]) == 1 && isClosedEdge(context, wireEdges[splitIdx[j]]))
        {
            const t = splitParams[j][0];
            splitParams[j] = (t > 0.5) ? [t / 2, t] : [t, (1 + t) / 2];
        }
    }
    if (size(splitIdx) > 0)
    {
        var toSplit = [];
        for (var idx in splitIdx)
            toSplit = append(toSplit, wireEdges[idx]);
        opSplitEdges(context, id + "split", {
                "edges"                     : qUnion(toSplit),
                "parameters"                : splitParams,
                "arcLengthParameterization" : true
        });
    }
    
    // Second pass: splitting a closed edge drops its seam vertex, so cut again at any
    //     Start / End point that is not yet a vertex of the pieces (e.g. Start 0 or End 100 on a circle)
    if (path.closed)
    {
        for (var k = 0; k < 2; k += 1)
        {
            const hit = evDistance(context, { "side0" : wireEdgesQ, "side1" : ends[k].origin, "arcLengthParameterization" : true });
            const t = hit.sides[0].parameter;
            if (t > PATH_TOL && t < 1 - PATH_TOL)
            {
                const pieces = evaluateQuery(context, wireEdgesQ);
                opSplitEdges(context, id + ("seamSplit" ~ k), {
                        "edges"                     : pieces[hit.sides[0].index],
                        "parameters"                : [[t]],
                        "arcLengthParameterization" : true
                });
            }
        }
    }
    
    // Keep the pieces between s and e
    var kept = [];
    for (var edge in evaluateQuery(context, wireEdgesQ))
    {
        const mid = evEdgeTangentLine(context, { "edge" : edge, "parameter" : 0.5 }).origin;
        const g = pathParameterAt(context, path, cum, lens, L, mid);
        if (g > s + PATH_TOL && g < e - PATH_TOL)
            kept = append(kept, edge);
    }
    if (size(kept) == 0)
        throw ppxNote("PolyPipeX: From % and To % leave nothing of the path.");
    
    // New Path, same direction as the original (starts at s)
    var trimmed = constructPath(context, qUnion(kept), {}).path;
    const first = evPathTangentLines(context, trimmed, [0]).tangentLines[0].origin;
    if (norm(first - ends[0].origin) > LEX_TOL)
        trimmed = reverse(trimmed);
    return trimmed;
}

// true if the edge is a closed curve (start and end at the same point)
function isClosedEdge(context is Context, edge is Query) returns boolean
{
    const ends = evEdgeTangentLines(context, { "edge" : edge, "parameters" : [0, 1] });
    return norm(ends[0].origin - ends[1].origin) < LEX_TOL;
}

// Fraction (0..1) along the path of a point on the path
function pathParameterAt(context is Context, path, cum is array, lens is array, L is ValueWithUnits, point is Vector) returns number
{
    const hit = evDistance(context, { "side0" : qUnion(path.edges), "side1" : point, "arcLengthParameterization" : true });
    const i = hit.sides[0].index;
    const f = path.flipped[i] ? 1 - hit.sides[0].parameter : hit.sides[0].parameter;
    return (cum[i] + f * lens[i]) / L;
}

// ##########################################################################
// Pipes ####################################################################

// Make one Pipe for each prepared Path
//     mode "pipe"  = finished pipe (outer sweep - bore sweep)
//     mode "solid" = outer sweep only (Cut bore, the bore is cut after the merge)
//     mode "bore"  = bore sweep only
function makePipes(context is Context, id is Id, pPaths is array, definition is map, mode is string)
{
    var counter = 0;
    for (var pp in pPaths)
    {
        counter += 1;
        const pathId = id + unstableIdComponent(counter);
        try silent
        {
            setExternalDisambiguation(context, pathId, pp.disambig);   // a picked part edge may be gone after an Add (Cut bore)
        }
        buildPipe(context, pathId, pp, definition, mode);
    }
}

// Build one Pipe on one prepared Path
function buildPipe(context is Context, id is Id, pp is map, definition is map, mode is string)
{
    if (pp.roundSide != undefined)
        definition.roundOverSide = pp.roundSide;
    const prof = makeProfiles(definition);
    
    if (mode != "bore") {
        sweepProfile(context, id + "outer", pp, prof.outer, definition);
    }
    
    if (prof.bore != undefined && (mode == "bore" || (mode == "pipe" && prof.boreCutsPipe)))
    {
        sweepProfile(context, id + "bore", pp, prof.bore, definition);
        if (mode == "pipe")
        {
            try silent
            {
                opBoolean(context, id + "boreCut", {
                        "tools"         : qBodyType(qCreatedBy(id + "bore",  EntityType.BODY), BodyType.SOLID),
                        "targets"       : qBodyType(qCreatedBy(id + "outer", EntityType.BODY), BodyType.SOLID),
                        "operationType" : BooleanOperationType.SUBTRACTION
                });
            }
            catch
            {
                throw ppxNote("PolyPipeX: could not cut the bore, check Wall thickness.");
            }
        }
    }
}

// Sketch one profile loop in the path frame, Sweep it, delete the sketch, Extend / shorten ends
function sweepProfile(context is Context, id is Id, pp is map, loop is array, definition is map)
{
    const sketchId = id + "sketch1";
    var sketch = newSketchOnPlane(context, sketchId, { "sketchPlane" : pp.frame });
    drawLoop(sketch, loop, "p");
    skSolve(sketch);
    
    var sweepDef = {
                "profiles" : qCreatedBy(sketchId, EntityType.FACE),
                "path"     : qUnion(pp.path.edges)
            };
    if (pp.twist != undefined && abs(pp.twist.angle) > 1e-9)
    {
        sweepDef.hasTwist = true;           // Native Sweep twist
        sweepDef.angle    = pp.twist.angle;
    }
    try
    {
        if (pp.halves == undefined)
        {
            opSweep(context, id + "sweep1", sweepDef);
        }
        else
        {
            // Closed loop with Twist: first half from the sketch, second half from the end face of the first
            sweepDef.path  = qUnion(pp.halves[0].edges);
            sweepDef.angle = pp.twist.angle / 2;
            opSweep(context, id + "sweep1", sweepDef);
            sweepDef.profiles = qCapEntity(id + "sweep1", CapType.END, EntityType.FACE);
            sweepDef.path     = qUnion(pp.halves[1].edges);
            opSweep(context, id + "sweep2", sweepDef);
            opBoolean(context, id + "joinHalves", {
                    "tools"         : qUnion([qCreatedBy(id + "sweep1", EntityType.BODY), qCreatedBy(id + "sweep2", EntityType.BODY)]),
                    "operationType" : BooleanOperationType.UNION
            });
        }
    }
    catch
    {
        // say why, with numbers
        const twisted = sweepDef.hasTwist == true;
        // the built path (with extension lines) and the START / END marks, beneath the red problem marks
        try silent
        {
            showBuiltPath(context, pp, definition);
        }
        if (definition.hideEndMarks != true)
        {
            try silent
            {
                showEndMarks(context, [pp], definition);
            }
        }
        if (showSketchOn(definition))
        {
            try silent
            {
                showSweepProblem(context, pp, loop, definition);
            }
        }
        const msg = sweepFailMessage(context, pp, loop, twisted, definition) ~ problemHint(definition);
        if (twisted)
            throw ppxNote(msg);    // shown in the dialog banner, no red value boxes
        throw regenError(msg, ["paths"]);
    }
    
    opDeleteBodies(context, id + "deleteBodies1", {
        "entities" : qCreatedBy(sketchId, EntityType.BODY)
    });
    
    // Extend / shorten ends (open paths only, a closed loop has no ends).
    if (pp.ext != undefined && pp.ext.inPath) {
        // Twist: the extensions are already in the path, only cut the shortened (minus) ends
        if (pp.ext.near < -LEX_TOL) {
            opOffsetFace(context, id + "offsetFace1", {
                    "moveFaces" : qCapEntity( id + "sweep1", CapType.START, EntityType.FACE),
                    "offsetDistance" : pp.ext.near
            });
        }
        if (pp.ext.far < -LEX_TOL) {
            opOffsetFace(context, id + "offsetFace2", {
                    "moveFaces" : qCapEntity( id + "sweep1", CapType.END, EntityType.FACE),
                    "offsetDistance" : pp.ext.far
            });
        }
    }
    else if (definition.changeEnds == true && !pp.path.closed) {
        var ends = extendAmounts(definition);
        ends.start = max(0 * meter, ends.start);   // shorten amounts already trimmed the path (square cut)
        ends.end   = max(0 * meter, ends.end);
        if (abs(ends.start) > LEX_TOL) {
            opOffsetFace(context, id + "offsetFace1", {
                    "moveFaces" : qCapEntity( id + "sweep1", CapType.START, EntityType.FACE),
                    "offsetDistance" : ends.start
            });
        }
        if (abs(ends.end) > LEX_TOL) {
            opOffsetFace(context, id + "offsetFace2", {
                    "moveFaces" : qCapEntity( id + "sweep1", CapType.END, EntityType.FACE),
                    "offsetDistance" : ends.end
            });
        }
    }
}

// Stations: fractions of the built pipe for Show profiles
function stationFractions(definition is map, closed is boolean) returns array
{
    if (definition.profileStations != PolyPipeXStations.evenly)
        return [definition.previewAt];
    const n = max(1, definition.stationCount);
    if (n == 1)
        return [0.5];
    var out = [];
    const d = closed ? n : n - 1;   // a closed loop: evenly all round (the ends are the same point)
    for (var i = 0; i < n; i += 1)
        out = append(out, i / d);
    return out;
}

// Arrow with debug lines, from p along unit direction d, length len, head in the plane of n
function debugArrow(context is Context, p is Vector, d is Vector, n is Vector, len, color is DebugColor)
{
    const tip  = p + d * len;
    var side = cross(n, d);
    if (norm(side) < 1e-9)
        side = perpendicularVector(d);
    side = normalize(side);
    addDebugLine(context, p, tip, color);
    addDebugLine(context, tip, tip - d * len * 0.3 + side * len * 0.18, color);
    addDebugLine(context, tip, tip - d * len * 0.3 - side * len * 0.18, color);
}

// Start / end marks, debug only: START green dot + arrow, END red dot + small arrow on the built ends,
//     yellow dots at the original path ends when they differ, green dot at the profile start point with Twist / Rotate
function showEndMarks(context is Context, pPaths is array, definition is map)
{
    const prof = makeProfiles(definition);
    const ext  = profileExtent(prof.outer);
    for (var pp in pPaths)
    {
        const span = builtSpan(context, pp, definition);
        const dim  = ext * 2;                 // the profile's largest dimension (2 x extent)
        const len  = dim * 0.5;               // end arrows 0.5 x the largest dimension
        const ring = dim * 0.25;              // ring diameter 0.5 x the largest dimension
        const s0   = frameAtLength(context, pp, span.a0, span.L, definition);
        var s1 = undefined;
        if (!pp.path.closed)
            s1 = frameAtLength(context, pp, span.a1, span.L, definition);
        
        // Yellow first (beneath): the original path end, only at an end that moved
        if (!pp.path.closed)
        {
            try silent
            {
                const inPath = pp.ext != undefined && pp.ext.inPath;
                const moved  = 1e-4 * meter;
                if (cutOf(pp, "cutStart") > moved)        // trimmed away, at the original end
                    debugRing(context, frameAt(context, pp.ext.origStart, pp.srcNormal, definition), ring, DebugColor.YELLOW);
                else if (abs(span.start) > moved)
                    debugRing(context, frameAtLength(context, pp, inPath ? max(0 * meter, span.start) : 0 * meter, span.L, definition), ring, DebugColor.YELLOW);
                if (cutOf(pp, "cutEnd") > moved)
                    debugRing(context, frameAt(context, pp.ext.origEnd, pp.srcNormal, definition), ring, DebugColor.YELLOW);
                else if (abs(span.end) > moved)
                    debugRing(context, frameAtLength(context, pp, span.L - (inPath ? max(0 * meter, span.end) : 0 * meter), span.L, definition), ring, DebugColor.YELLOW);
            }
        }
        
        // START: green ring, green arrow from outside the start face pointing in to it
        debugRing(context, s0, ring, DebugColor.GREEN);
        const gap = ext * 0.3;
        debugArrow(context, s0.origin - s0.normal * (gap + len), s0.normal, s0.x, len, DebugColor.GREEN);
        // END: red ring, red arrow out of the end face
        if (s1 != undefined)
        {
            debugRing(context, s1, ring, DebugColor.RED);
            debugArrow(context, s1.origin, s1.normal, s0.x, len, DebugColor.RED);
        }
        if (definition.twist == true || definition.rotateProfile == true)
        {
            // Profile start point (0 deg) at START, as the Start point marks
            const seg = prof.outer[0];
            const a = seg.kind == "circle" ? seg.c + vector(seg.r, 0 * meter) : seg.a;
            addDebugPoint(context, s0.origin + s0.x * a[0] + yAxis(s0) * a[1], DebugColor.GREEN);
        }
    }
}

// Small ring with a cross (debug lines) in a frame plane, at its origin
function debugRing(context is Context, frame is Plane, r, color is DebugColor)
{
    const y = yAxis(frame);
    for (var i = 0; i < 12; i += 1)
    {
        const a0 = i * 30 * degree;
        const a1 = (i + 1) * 30 * degree;
        addDebugLine(context, frame.origin + (frame.x * cos(a0) + y * sin(a0)) * r,
                              frame.origin + (frame.x * cos(a1) + y * sin(a1)) * r, color);
    }
    addDebugLine(context, frame.origin - frame.x * r, frame.origin + frame.x * r, color);
    addDebugLine(context, frame.origin - y * r,       frame.origin + y * r,       color);
}

// Signed end changes of the built pipe: + extend, - shorten, same rules as sweepProfile
function endChanges(pp is map, definition is map) returns map
{
    if (pp.ext != undefined && pp.ext.inPath)
        return { "start" : pp.ext.near - cutOf(pp, "cutStart"), "end" : pp.ext.far - cutOf(pp, "cutEnd") };
    var out = { "start" : 0 * meter, "end" : 0 * meter };
    if (definition.changeEnds == true && !pp.path.closed)
    {
        const ends = extendAmounts(definition);
        out.start = ends.start;
        out.end   = ends.end;
    }
    return out;
}

// The built pipe on pp.path: from a0 to a1 (path length; below 0 / above L = straight beyond the ends)
function builtSpan(context is Context, pp is map, definition is map) returns map
{
    const L  = evPathLength(context, pp.path);
    const ch = endChanges(pp, definition);
    // a trimmed (square cut) minus amount is already out of the path
    const cS = cutOf(pp, "cutStart");
    const cE = cutOf(pp, "cutEnd");
    var a0 = -(ch.start + cS);
    var a1 = L + ch.end + cE;
    if (pp.ext != undefined && pp.ext.inPath)
    {
        // Twist: the plus amounts are already in the path, only the minus amounts are cut
        a0 = max(0 * meter, -(ch.start + cS));
        a1 = L - max(0 * meter, -(ch.end + cE));
    }
    return { "L" : L, "a0" : a0, "a1" : a1, "start" : ch.start, "end" : ch.end };
}

// draw the path as built (debug only, dialog only) when the sweep failed: the (trimmed) path, which with
//     Twist already holds the extension lines; without Twist the extensions (face offsets) are drawn as straight lines
function showBuiltPath(context is Context, pp is map, definition is map)
{
    addDebugEntities(context, qUnion(pp.path.edges), DebugColor.MAGENTA);
    if ((pp.ext == undefined || !pp.ext.inPath) && definition.changeEnds == true && !pp.path.closed)
    {
        const ends = extendAmounts(definition);
        const tl   = evPathTangentLines(context, pp.path, [0, 1]).tangentLines;
        if (ends.start > LEX_TOL)
            addDebugLine(context, tl[0].origin, tl[0].origin - tl[0].direction * ends.start, DebugColor.MAGENTA);
        if (ends.end > LEX_TOL)
            addDebugLine(context, tl[1].origin, tl[1].origin + tl[1].direction * ends.end, DebugColor.MAGENTA);
    }
}

// an expected failure with a help text: caught in the feature body and shown with reportFeatureError
//     (reportFeatureWarning, "FAILED: " + message)
function ppxNote(message is string) returns map
{
    return { "ppxNote" : true, "message" : message };
}

// Amount trimmed off a path end by Square cut ends, 0 if none
function cutOf(pp is map, key is string) returns ValueWithUnits
{
    if (pp.ext == undefined || pp.ext[key] == undefined)
        return 0 * meter;
    return pp.ext[key];
}

// Profile frame at path length a (may be beyond the ends: straight along the end tangent)
function frameAtLength(context is Context, pp is map, a is ValueWithUnits, L is ValueWithUnits, definition is map) returns Plane
{
    if (pp.path.closed || (a >= -LEX_TOL && a <= L + LEX_TOL))
        return frameAtFraction(context, pp, max(0, min(1, a / L)), definition);
    const f  = a < 0 * meter ? 0 : 1;
    const fr = frameAtFraction(context, pp, f, definition);
    const d  = a < 0 * meter ? a : a - L;
    return plane(fr.origin + fr.normal * d, fr.normal, fr.x);
}

// Station path lengths on the built pipe
function stationLengths(definition is map, pp is map, span is map) returns array
{
    var out = [];
    for (var u in stationFractions(definition, pp.path.closed))
        out = append(out, span.a0 + u * (span.a1 - span.a0));
    return out;
}

function signedMm(v is ValueWithUnits) returns string
{
    const x = roundToPrecision(v / millimeter, 2);
    return (x > 0 ? "+" : "") ~ x ~ " mm";
}

// Show profiles: profile at each station, kept as sketches (or dialog only), with optional marks.
function makeStationSketches(context is Context, id is Id, pPaths is array, definition is map)
{
    const prof = makeProfiles(definition);
    const ext  = profileExtent(prof.outer);
    var counter = 0;
    for (var pp in pPaths)
    {
        counter += 1;
        const span = builtSpan(context, pp, definition);
        const L    = span.a1 - span.a0;
        const at   = stationLengths(definition, pp, span);
        for (var k = 0; k < size(at); k += 1)
        {
            const frame = frameAtLength(context, pp, at[k], span.L, definition);   // on the built pipe
            if (definition.profilesDialogOnly == true)
            {
                debugLoop(context, frame, prof.outer, DebugColor.GREEN);
                if (prof.bore != undefined && prof.boreCutsPipe)
                    debugLoop(context, frame, prof.bore, DebugColor.GREEN);
            }
            else
            {
                const sketchId = id + unstableIdComponent(counter) + unstableIdComponent(k);
                setExternalDisambiguation(context, sketchId, pp.disambig);
                var sketch = newSketchOnPlane(context, sketchId, { "sketchPlane" : frame });
                drawLoop(sketch, prof.outer, "o");
                if (prof.bore != undefined && prof.boreCutsPipe)
                    drawLoop(sketch, prof.bore, "b");
                skSolve(sketch);
                debug(context, qCreatedBy(sketchId, EntityType.VERTEX ), DebugColor.GREEN);
                debug(context, qCreatedBy(sketchId, EntityType.EDGE   ), DebugColor.GREEN);
            }
            if (definition.showProfileStartPoints == true)
            {
                try silent
                {
                    // Dot at the first point of the outline, arrow along the drawing order
                    const seg = prof.outer[0];
                    var a; var b;
                    if (seg.kind == "circle") {
                        a = seg.c + vector(seg.r, 0 * meter);
                        b = seg.c + vector(seg.r, seg.r * 0.4);
                    } else {
                        a = seg.a;
                        b = seg.kind == "arc" ? seg.m : seg.b;
                    }
                    const y  = yAxis(frame);
                    const wa = frame.origin + frame.x * a[0] + y * a[1];
                    const wb = frame.origin + frame.x * b[0] + y * b[1];
                    addDebugPoint(context, wa, DebugColor.MAGENTA);
                    if (norm(wb - wa) > 1e-9 * meter)
                        debugArrow(context, wa, normalize(wb - wa), frame.normal, ext * 0.35, DebugColor.MAGENTA);
                }
            }
        }
        if (definition.showPathArrows == true)
        {
            try silent
            {
                // blue arrows along the path, beside the pipe, at the stations between the ends
                //     (or at mid-path); the end marks are drawn by showEndMarks
                const len  = ext * 3;
                const side = ext * 1.8;
                const tol  = 1e-4 * meter;
                var drawn = 0;
                for (var a in at)
                {
                    if (!pp.path.closed && (abs(a - span.a0) < tol || abs(a - span.a1) < tol))
                        continue;
                    const fs = frameAtLength(context, pp, a, span.L, definition);
                    debugArrow(context, fs.origin + fs.x * side - fs.normal * (len / 2), fs.normal, fs.x, len, DebugColor.BLUE);
                    drawn += 1;
                }
                if (drawn == 0)
                {
                    const fm = frameAtLength(context, pp, (span.a0 + span.a1) / 2, span.L, definition);
                    debugArrow(context, fm.origin + fm.x * side - fm.normal * (len / 2), fm.normal, fm.x, len, DebugColor.BLUE);
                }
            }
        }
    }
}

// Info line text: station positions along each path, in mm
//     built length and end changes (when Extend / shorten ends is used), stations measured from the built start cap
function stationInfo(context is Context, pPaths is array, definition is map) returns string
{
    var info = "";
    var n = 0;
    for (var pp in pPaths)
    {
        n += 1;
        if (n > 2) {
            info ~= " ...";
            break;
        }
        const span = builtSpan(context, pp, definition);
        const B    = span.a1 - span.a0;
        var part = "";
        if (abs(span.start) > LEX_TOL || abs(span.end) > LEX_TOL)
            part = "Built " ~ roundToPrecision(B / millimeter, 2) ~ " mm (START " ~ signedMm(span.start) ~ ", END " ~ signedMm(span.end) ~ ")";
        if (definition.listStations == true)
        {
            part ~= (part == "" ? "" : ", ") ~ "Stations at";
            const at = stationLengths(definition, pp, span);
            for (var k = 0; k < size(at); k += 1)
                part ~= (k > 0 ? "," : "") ~ " " ~ roundToPrecision((at[k] - span.a0) / millimeter, 2);
            part ~= " mm of " ~ roundToPrecision(B / millimeter, 2) ~ " mm";
        }
        if (part != "")
            info ~= (info == "" ? "" : "; ") ~ part;
    }
    return info;
}

// Keep tool Merge (Remove / Intersect): the same tools and targets as processNewBodyIfNeeded (Merge with all =
//     all other modifiable solids, else Merge scope), as one Boolean with "keep tools", so the pipe is left as its own part.
//     Throws if there is nothing to merge or the Boolean fails (the caller then runs the normal Merge).
function keepToolMerge(context is Context, mainId is Id, definition is map)
{
    const tools = qUnion(evaluateQuery(context, qModifiableEntityFilter(qBodyType(qCreatedBy(mainId, EntityType.BODY), BodyType.SOLID))));
    if (isQueryEmpty(context, tools))
        throw regenError(ErrorStringEnum.FEATURE_NO_SOLIDS);
    const scope   = (definition.defaultScope != false) ? qAllModifiableSolidBodies() : definition.booleanScope;
    const targets = qSubtraction(scope, tools);
    if (isQueryEmpty(context, targets))
        throw regenError(ErrorStringEnum.BOOLEAN_NEED_ONE_SOLID);
    opBoolean(context, mainId + "boolean", {
            "tools"                       : tools,
            "targets"                     : targets,
            "operationType"               : convertNewBodyOpToBoolOp(definition.operationType),
            "keepTools"                   : true,
            "targetsAndToolsNeedGrouping" : true
    });
}

// Cut Bore through target (Add + Shell): sweep the bores and subtract them from the merged result
function cutBoresThroughTargets(context is Context, mainId is Id, pPaths is array, definition is map)
{
    const boreId = mainId + "bore";
    try silent
    {
        makePipes(context, boreId, pPaths, definition, "bore");
    }
    
    const bores = qBodyType(qCreatedBy(boreId, EntityType.BODY), BodyType.SOLID);
    if (isQueryEmpty(context, bores)) {
        reportFeatureWarning(context, mainId, "Cut bore through target: the bore could not be made.");
        return;
    }
    
    // Same scope as the Merge: "Merge with all" or "Merge scope", plus any pipe that did not merge with anything
    const scope   = (definition.defaultScope != false) ? qAllModifiableSolidBodiesNoMesh() : definition.booleanScope;
    const targets = qSubtraction(qUnion([scope, qBodyType(qCreatedBy(mainId, EntityType.BODY), BodyType.SOLID)]),
                                 qCreatedBy(boreId, EntityType.BODY));
    try silent
    {
        opBoolean(context, boreId + "cut", {
                "tools"         : bores,
                "targets"       : targets,
                "operationType" : BooleanOperationType.SUBTRACTION
        });
    }
    catch
    {
        reportFeatureWarning(context, mainId, "Cut bore through target: the boolean failed, the bore was not cut.");
    }
    
    // Clean up anything left over
    const leftovers = qCreatedBy(boreId, EntityType.BODY);
    if (!isQueryEmpty(context, leftovers)) {
        opDeleteBodies(context, boreId + "cleanup", { "entities" : leftovers });
    }
}

// ##########################################################################
// Profiles #################################################################
//     Each profile is one closed loop of segments, in sketch (frame) coordinates:
//         { "kind" : "line",   "a" : start, "b" : end }
//         { "kind" : "arc",    "a" : start, "m" : mid, "b" : end }
//         { "kind" : "circle", "c" : center, "r" : radius }
//     outer = the pipe outline, bore = the hole. Both use the same Rotate and Center Offset,
//     so the bore always follows the outline. Wall outside the profile: off = bore inside the outline,
//     on = outline grows by the Wall thickness and the bore is the original outline.

function makeProfiles(definition is map) returns map
{
    const shellOn = definition.hollow == true && definition.shape != PolyPipeXShape.roundOver && definition.wallThickness > 0 * meter;
    const w       = shellOn ? definition.wallThickness : 0 * meter;   // wall thickness only counts when hollow
    const flip    = definition.wallOutside == true;
    const zero    = 0 * meter;
    const gOuter  = (shellOn && flip) ? w : zero;     // Outline growth
    const gBore   = flip ? zero : -w;                  // Bore growth (from the original outline)
    const T       = profileTransform(definition);
    
    var outer;
    var bore = undefined;
    var boreCutsPipe = true;
    
    if (definition.shape == PolyPipeXShape.arc && definition.arcAngle < 360 * degree)
    {
        const R = definition.radius;
        const h = definition.arcAngle / 2;
        if (shellOn && definition.arcCurvedBand == true)
        {
            // Alt Shell Form: a curved band (no side walls), drawn directly
            const rIn  = flip ? R : R - w;
            const rOut = flip ? R + w : R;
            if (rIn <= zero)
                throw ppxNote("PolyPipeX: Wall thickness is too large for this Arc.");
            outer = arcBand(rIn, rOut, h);
            bore  = arcSector(rIn, h, zero);   // Used only by Cut bore through target
            boreCutsPipe = false;              // The band is already hollow
        }
        else
        {
            outer = arcSector(R, h, gOuter);
            if (shellOn)
                bore = arcSector(R, h, gBore);
        }
    }
    else if (definition.shape == PolyPipeXShape.arc || definition.shape == PolyPipeXShape.circle)
    {
        const R = definition.radius;
        outer = [segCircle(R + gOuter)];
        if (shellOn)
            bore = [segCircle(R + gBore)];
    }
    else if (definition.shape == PolyPipeXShape.polygon)
    {
        const pts = polygonPoints(definition);
        outer = polyLoop(offsetPolygon(pts, gOuter));
        if (shellOn)
            bore = polyLoop(offsetPolygon(pts, gBore));
    }
    else if (definition.shape == PolyPipeXShape.rectangle)
    {
        const pts = rectanglePoints(definition);
        outer = polyLoop(offsetPolygon(pts, gOuter));
        if (shellOn)
            bore = polyLoop(offsetPolygon(pts, gBore));
    }
    else if (definition.shape == PolyPipeXShape.roundOver)
    {
        const side = (definition.roundOverSide == undefined) ? -1 : definition.roundOverSide;
        outer = roundOverLoop(definition.radius, (definition.roundOverFlipSide == true) ? -side : side);
    }
    else if (definition.shape == PolyPipeXShape.slot)
    {
        outer = slotLoop(definition, gOuter);
        if (shellOn)
            bore = slotLoop(definition, gBore);
    }
    else
    {
        throw regenError("PolyPipeX: unknown Profile shape.", ["shape"]);
    }
    
    const f = function(p) { return transformPoint(p, T); };
    return {
        "outer"        : mapLoop(outer, f),
        "bore"         : (bore == undefined) ? undefined : mapLoop(bore, f),
        "boreCutsPipe" : boreCutsPipe
    };
}

// Rotate profile and Offset profile from path
function profileTransform(definition is map) returns map
{
    const isFullCircle = definition.shape == PolyPipeXShape.circle ||
                        (definition.shape == PolyPipeXShape.arc && definition.arcAngle >= 360 * degree);
    
    var A0 = 0 * degree;
    if (definition.rotateProfile == true && !isFullCircle) {
        A0 = (definition.rotateClockwise == true ? -1 : 1) * definition.rotateAngle;
    }
    const rotMat = [[cos(A0), -sin(A0)], [sin(A0), cos(A0)]] as Matrix;
    
    var Xo = 0 * meter;
    var Yo = 0 * meter;
    if (definition.offsetProfile == true)
    {
        Xo = (definition.offsetHeightNegative == true ? -1 : 1) * definition.offsetHeight;
        Yo = (definition.offsetWidthNegative  == true ? -1 : 1) * definition.offsetWidth;
        
        // Fraction offsets, of the nominal (un-shelled) size
        var dimX;
        var dimY;
        if (definition.shape == PolyPipeXShape.polygon) {
            dimX = definition.polygonRadius * 2;
            dimY = dimX;
        }
        else if (definition.shape == PolyPipeXShape.rectangle) {
            dimX = (definition.rectHeight/2 + (definition.rectHeightDentOutward?1:-1) * definition.rectHeightDentDepth) * 2;
            dimY = (definition.rectWidth/2  + (definition.rectWidthDentOutward?1:-1)  * definition.rectWidthDentDepth)  * 2;
        }
        else if (definition.shape == PolyPipeXShape.roundOver) {
            dimX = definition.radius;
            dimY = dimX;
        }
        else if (definition.shape == PolyPipeXShape.slot) {
            dimX = definition.slotEndRadius * 2;
            dimY = (definition.slotStraightLength/2 + definition.slotEndRadius) * 2;
        }
        else {
            dimX = definition.radius * 2;
            dimY = dimX;
        }
        Xo += (definition.offsetHeightFractionNegative == true ? -1 : 1) * definition.offsetHeightFraction * dimX;
        Yo += (definition.offsetWidthFractionNegative  == true ? -1 : 1) * definition.offsetWidthFraction  * dimY;
    }
    
    // As before: Arc (< 360) and Slot rotate their offset with the shape, the others do not
    const offRotates = !isFullCircle && (definition.shape == PolyPipeXShape.arc || definition.shape == PolyPipeXShape.slot);
    return { "rot" : rotMat, "off" : vector(Xo, Yo), "offRotates" : offRotates };
}

function transformPoint(p is Vector, T is map) returns Vector
{
    if (T.offRotates)
        return T.rot * (p + T.off);
    return T.rot * p + T.off;
}

function mapLoop(loop is array, f is function) returns array
{
    var out = [];
    for (var seg in loop)
    {
        if (seg.kind == "circle") {
            seg.c = f(seg.c);
        }
        else {
            seg.a = f(seg.a);
            seg.b = f(seg.b);
            if (seg.kind == "arc")
                seg.m = f(seg.m);
        }
        out = append(out, seg);
    }
    return out;
}

function drawLoop(sketch is Sketch, loop is array, prefix is string)
{
    for (var i = 0; i < size(loop); i += 1)
    {
        const seg  = loop[i];
        const name = prefix ~ i;
        if (seg.kind == "line")
            skLineSegment(sketch, name, { "start" : seg.a, "end" : seg.b });
        else if (seg.kind == "arc")
            skArc(sketch, name, { "start" : seg.a, "mid" : seg.m, "end" : seg.b });
        else
            skCircle(sketch, name, { "center" : seg.c, "radius" : seg.r });
    }
}

// Round over: the corner minus a circle of radius R, corner on the path. Height (x) points out of the top face,
//     the part is below it (x < 0) and on the s side of y (s = -1 or 1, found from the part edge by roundSideFor). The square overshoots both faces by 5%
//     so the Remove leaves no sliver. Swept with Remove along an outside edge it rounds the edge like a fillet.
function roundOverLoop(R is ValueWithUnits, s is number) returns array
{
    const e = 0.05 * R;
    const z = 0 * meter;
    const k = 1 / sqrt(2);
    const A = vector(z, s * R);
    const B = vector(e, s * R);
    const C = vector(e, -s * e);
    const D = vector(-R, -s * e);
    const E = vector(-R, z);
    const M = vector(-R + R * k, s * R - s * R * k);   // arc center (-R, s R), toward the corner
    return [segLine(A, B), segLine(B, C), segLine(C, D), segLine(D, E), segArc(E, M, A)];
}

function segLine(a is Vector, b is Vector) returns map
{
    return { "kind" : "line", "a" : a, "b" : b };
}

function segArc(a is Vector, m is Vector, b is Vector) returns map
{
    return { "kind" : "arc", "a" : a, "m" : m, "b" : b };
}

function segCircle(r is ValueWithUnits) returns map
{
    if (r <= 0 * meter)
        throw ppxNote("PolyPipeX: Wall thickness is too large.");
    return { "kind" : "circle", "c" : vector(0 * meter, 0 * meter), "r" : r };
}

// Arc (pie) sector, half angle h, apex at the center, grown outward by g (g < 0 = inward).
//     Each side moves by g (parallel), the arc radius becomes R + g.
function arcSector(R is ValueWithUnits, h is ValueWithUnits, g is ValueWithUnits) returns array
{
    const Rg = R + g;
    if (Rg <= abs(g))
        throw ppxNote("PolyPipeX: Wall thickness is too large for this Arc.");
    const d1   = vector(cos(h), sin(h));              // upper side direction
    const n1   = vector(sin(h), -cos(h));             // its inward normal
    const s    = sqrt(Rg * Rg - g * g);               // where the moved side meets the arc
    const sA   = -g * cos(h) / sin(h);                // where the moved side meets the other moved side
    const apex = vector(-g / sin(h), 0 * meter);
    if (s <= sA || abs(apex[0]) >= Rg)
        throw ppxNote("PolyPipeX: Wall thickness is too large for this Arc.");
    const e1 = d1 * s - n1 * g;
    const e2 = vector(e1[0], -e1[1]);
    return [ segLine(apex, e1), segArc(e1, vector(Rg, 0 * meter), e2), segLine(e2, apex) ];
}

// Alt Shell Form: curved band between rIn and rOut, half angle h
function arcBand(rIn is ValueWithUnits, rOut is ValueWithUnits, h is ValueWithUnits) returns array
{
    const d1 = vector(cos(h),  sin(h));
    const d2 = vector(cos(h), -sin(h));
    return [ segLine(d1 * rIn,  d1 * rOut),
             segArc(d1 * rOut, vector(rOut, 0 * meter), d2 * rOut),
             segLine(d2 * rOut, d2 * rIn),
             segArc(d2 * rIn,  vector(rIn,  0 * meter), d1 * rIn) ];
}

// Polygon / Star points
function polygonPoints(definition is map) returns array
{
    const N  = definition.polygonCorners;
    const R  = definition.polygonRadius;
    const Rd = cos(180/N*degree) * R + (definition.polygonDentOutward?1:-1) * definition.polygonDentDepth;
    if (Rd <= 0 * meter)
        throw ppxNote("PolyPipeX: Side dent depth is too large.");
    var pts = [];
    for (var i = 0; i < 2 * N; i += 1)
    {
        const A = i * 180 / N * degree;
        const r = (i % 2 == 0) ? R : Rd;
        pts = append(pts, vector(cos(A) * r, sin(A) * r));
    }
    return pts;
}

// Rectangle points (with optional dents at the side middles)
function rectanglePoints(definition is map) returns array
{
    const z  = 0 * meter;
    const X1 = definition.rectHeight/2;
    const Y1 = definition.rectWidth/2;
    const X2 = X1 + (definition.rectHeightDentOutward?1:-1) * definition.rectHeightDentDepth;
    const Y2 = Y1 + (definition.rectWidthDentOutward?1:-1) * definition.rectWidthDentDepth;
    return [ vector( X1, Y1), vector( z, Y2), vector(-X1, Y1), vector(-X2, z),
             vector(-X1,-Y1), vector( z,-Y2), vector( X1,-Y1), vector( X2, z) ];
}

// Slot (stadium), or with one round end removed, grown by g
function slotLoop(definition is map, g is ValueWithUnits) returns array
{
    const z  = 0 * meter;
    const r  = definition.slotEndRadius + g;
    const Y1 = definition.slotStraightLength/2;
    if (r <= z)
        throw ppxNote("PolyPipeX: Wall thickness is too large for this Slot.");
    
    if (!definition.slotOneFlatEnd)
    {
        if (Y1 < LEX_TOL)
            return [segCircle(r)];
        return [ segLine(vector( r,-Y1), vector( r, Y1)),
                 segArc(vector( r, Y1), vector( z, Y1 + r), vector(-r, Y1)),
                 segLine(vector(-r, Y1), vector(-r,-Y1)),
                 segArc(vector(-r,-Y1), vector( z,-Y1 - r), vector( r,-Y1)) ];
    }
    
    // Round end at +Y, flat end at -Y (Flat end at the other side mirrors it)
    const yb = -Y1 - g;
    var loop;
    if (yb < Y1 - LEX_TOL)
    {
        loop = [ segLine(vector( r, yb), vector( r, Y1)),
                 segArc(vector( r, Y1), vector( z, Y1 + r), vector(-r, Y1)),
                 segLine(vector(-r, Y1), vector(-r, yb)),
                 segLine(vector(-r, yb), vector( r, yb)) ];
    }
    else
    {
        // The flat end cuts into the round end
        const dy = yb - Y1;
        if (dy >= r - LEX_TOL)
            throw ppxNote("PolyPipeX: Wall thickness is too large for this Slot.");
        const x = sqrt(r * r - dy * dy);
        loop = [ segArc(vector( x, yb), vector( z, Y1 + r), vector(-x, yb)),
                 segLine(vector(-x, yb), vector( x, yb)) ];
    }
    if (definition.slotSwapFlatEnd)
        loop = mapLoop(loop, function(p) { return vector(p[0], -p[1]); });
    return loop;
}

// Closed polygon (point list) to line segments
function polyLoop(pts is array) returns array
{
    const n = size(pts);
    var loop = [];
    for (var i = 0; i < n; i += 1)
        loop = append(loop, segLine(pts[i], pts[(i + 1) % n]));
    return loop;
}

function cross2(u is Vector, v is Vector)
{
    return u[0] * v[1] - u[1] * v[0];
}

function dot2(u is Vector, v is Vector)
{
    return u[0] * v[0] + u[1] * v[1];
}

function signedArea(pts is array)
{
    const n = size(pts);
    var a = 0 * meter * meter;
    for (var i = 0; i < n; i += 1)
        a += cross2(pts[i], pts[(i + 1) % n]);
    return a / 2;
}

// Drop repeated and straight-through (collinear) points
function cleanPolygon(points is array) returns array
{
    var pts = [];
    for (var p in points)
    {
        if (size(pts) == 0 || norm(p - pts[size(pts) - 1]) > LEX_TOL)
            pts = append(pts, p);
    }
    if (size(pts) > 1 && norm(pts[0] - pts[size(pts) - 1]) <= LEX_TOL)
        pts = subArray(pts, 0, size(pts) - 1);
    
    const n = size(pts);
    var out = [];
    for (var i = 0; i < n; i += 1)
    {
        const u = pts[i] - pts[(i + n - 1) % n];
        const v = pts[(i + 1) % n] - pts[i];
        const straight = abs(cross2(u, v)) <= 1e-9 * norm(u) * norm(v) && dot2(u, v) > 0 * meter * meter;
        if (!straight)
            out = append(out, pts[i]);
    }
    return out;
}

// Offset a closed polygon by g (g > 0 outward, g < 0 inward): each side moves parallel by g,
//     corners are mitred (sharp). Throws if the offset turns the polygon inside out.
function offsetPolygon(points is array, g is ValueWithUnits) returns array
{
    const pts = cleanPolygon(points);
    const n = size(pts);
    if (n < 3)
        throw regenError("PolyPipeX: profile has less than 3 corners.", ["shape"]);
    if (abs(g) < LEX_TOL)
        return pts;
    
    const ccw = signedArea(pts) > 0 * meter * meter;
    var sides = [];
    for (var i = 0; i < n; i += 1)
    {
        const a = pts[i];
        const b = pts[(i + 1) % n];
        const d = (b - a) / norm(b - a);
        const nOut = ccw ? vector(d[1], -d[0]) : vector(-d[1], d[0]);
        sides = append(sides, { "p" : a + nOut * g, "d" : d });
    }
    
    var result = [];
    for (var j = 0; j < n; j += 1)
    {
        const s0 = sides[(j + n - 1) % n];
        const s1 = sides[j];
        const den = cross2(s0.d, s1.d);
        if (abs(den) < 1e-9)
            result = append(result, s1.p);
        else
            result = append(result, s0.p + s0.d * (cross2(s1.p - s0.p, s1.d) / den));
    }
    
    for (var i = 0; i < n; i += 1)
    {
        const nd = result[(i + 1) % n] - result[i];
        if (norm(nd) < LEX_TOL || dot2(nd, sides[i].d) <= 0 * meter)
            throw ppxNote("PolyPipeX: Wall thickness is too large for this shape.");
    }
    if ((signedArea(result) > 0 * meter * meter) != ccw)
        throw ppxNote("PolyPipeX: Wall thickness is too large for this shape.");
    return result;
}

// End
