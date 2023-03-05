using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CreoPost
{
    /// <summary>
    /// This class contains a post processor for Creo -> G-Code.
    /// </summary>
    public class PostProcNclToGcode
    {
        public Log.LogDelegate Log = CreoPost.Log.LogToConsole;

        public const double FeedRateRapid = 888.0;

        public Vector.Pos3 CurrPos;
        public double CurrFeedRate = 0.0;
        public bool RapidMove = true;

        public bool SpindleOn = false;
        public double SpindleRpm = 0.0;

        public bool NctToGcode(NclReader ncl, GcodeWriter gcode)
        {
            // over all NCL instructions
            for (int nclIndex=0; nclIndex<ncl.Lines.Count; nclIndex++)
            {
                // access and look-ahead
                var ncli = ncl.Lines[nclIndex];
                NclItemBase ncliNext = null;
                if (nclIndex < ncl.Lines.Count - 1)
                    ncliNext = ncl.Lines[nclIndex + 1];

                // what item?
                if (ncli is NclItemSpindleRpm sprpm)
                {
                    SpindleOn = true;
                    SpindleRpm = sprpm.Rpm;
                    gcode.Add(new GcodeItemSpindleRpmM3(SpindleRpm));
                }

                if (ncli is NclItemSpindleOff spoff)
                {
                    SpindleOn = false;
                    SpindleRpm = 0.0;
                    gcode.Add(new GcodeItemSpindleOffM5());
                }

                if (ncli is NclItemRapid rapid)
                {
                    RapidMove = true;
                    CurrFeedRate = FeedRateRapid;
                }

                if (ncli is NclItemFeedRate feed)
                {
                    RapidMove = false;
                    CurrFeedRate = feed.FeedRate;
                }

                if (ncli is NclItemGoto go)
                {
                    CurrPos = (new Vector.Pos3()).Add(go.X, go.Y, go.Z);

                    // need to disting between G0 and G1
                    if (RapidMove)
                    {
                        // G0
                        gcode.Add(new GcodeItemMoveRapidG0(go.X, go.Y, go.Z));
                    }
                    else
                    {
                        // G1
                        gcode.Add(new GcodeItemMoveLinearG1(go.X, go.Y, go.Z, feedRate: CurrFeedRate));
                    }
                }

                if (ncli is NclItemCircle circle)
                {
                    // the spec (?) demands, that CIRCLE is followed by a GOTO
                    var go2 = ncliNext as NclItemGoto;
                    if (go2 == null)
                    {
                        Log?.Invoke(LogLevel.Error, $"Line ({circle.LineNo}) : CIRCLE command is required to be followed by GOTO. Aborting prossing!");
                        return false;
                    }

                    // ok
                    bool? clw = null;

                    if (circle.I == 0.0 && circle.J == 0.0 && circle.K == -1.0)
                        clw = true;
                    if (circle.I == 0.0 && circle.J == 0.0 && circle.K == +1.0)
                        clw = false;

                    if (!clw.HasValue)
                    {
                        Log?.Invoke(LogLevel.Error, $"Line ({circle.LineNo}) : CIRCLE command has normal vector different to XY plane. Aborting prossing!");
                        return false;
                    }

                    // update target
                    CurrPos = (new Vector.Pos3()).Add(circle.X, circle.Y, circle.Z);

                    if (false)
                    {
                        // use the R variant
                        if (clw.Value)
                        {
                            gcode.Add(new GcodeItemArcClockwise()
                            {
                                X = go2.X,
                                Y = go2.Y,
                                Z = go2.Z,
                                R = circle.R,
                                Feedrate = CurrFeedRate
                            });
                        }
                        else
                        {
                            gcode.Add(new GcodeItemArcAntiClockwise()
                            {
                                X = go2.X,
                                Y = go2.Y,
                                Z = go2.Z,
                                R = circle.R,
                                Feedrate = CurrFeedRate
                            });
                        }
                    }
                    else
                    {
                        // use the IJ variant
                        if (clw.Value)
                        {
                            gcode.Add(new GcodeItemArcClockwise()
                            {
                                X = go2.X,
                                Y = go2.Y,
                                Z = go2.Z,
                                I = circle.X - go2.X,
                                J = circle.Y - go2.Y,
                                Feedrate = CurrFeedRate
                            });
                        }
                        else
                        {
                            gcode.Add(new GcodeItemArcAntiClockwise()
                            {
                                X = go2.X,
                                Y = go2.Y,
                                Z = go2.Z,
                                I = circle.X - go2.X,
                                J = circle.Y - go2.Y,
                                Feedrate = CurrFeedRate
                            });
                        }
                    }

                    // skip GOTO
                    nclIndex++;
                }

                if (ncli is NclItemCycleBase cycleBase)
                {
                    // first check, if assumptions concerning command sequence are met ..
                    var gotoSeq = new List<NclItemGoto>();
                    int offIndex = -1;
                    bool err2 = false;
                    int j = nclIndex + 1;
                    while (j < ncl.Lines.Count)
                    {
                        if (ncl.Lines[j] is NclItemGoto gs)
                            gotoSeq.Add(gs);
                        else if (ncl.Lines[j] is NclItemCycleOff)
                        {
                            offIndex = j;
                            break;
                        }
                        else
                            err2 = true;
                        j++;
                    }

                    // still ok?
                    if (offIndex == -1 || err2)
                    {
                        Log?.Invoke(LogLevel.Error, $"Line ({cycleBase.LineNo}) : CYCLE / * command parsed incorrect. Aborting prossing!");
                        return false;
                    }

                    // advance index
                    nclIndex = offIndex + 1;

                    // now differentiate
                    var res = false;

                    if (cycleBase is NclItemCycleDrill cycleDrill)
                        res = SubCommandCycleDrill(ncl, gcode, cycleDrill, gotoSeq);

                    if (cycleBase is NclItemCycleDeep cycleDeep)
                        res = SubCommandCycleDeep(ncl, gcode, cycleDeep, gotoSeq);

                    if (!res)
                    {
                        Log?.Invoke(LogLevel.Error, $"Line ({cycleBase.LineNo}) : CYCLE / * command was not executed corrently. Aborting prossing!");
                        return false;
                    }
                }
            }

            // looks good
            return true;
        }

        protected bool SubCommandCycleDrill(
            NclReader ncl,
            GcodeWriter gcode,
            NclItemCycleDrill cycleDeep,
            List<NclItemGoto> gotoSeq)
        {
            // access
            if (cycleDeep == null)
                return false;

            // get some argument values
            var deepDepth = cycleDeep.Args?.GetNamedValue("DEPTH");
            var deepMmpm = cycleDeep.Args?.GetNamedValue("MMPM");
            var deepClear = cycleDeep.Args?.GetNamedValue("CLEAR");
            if (deepDepth == null || deepMmpm == null || deepClear == null)
            {
                Log?.Invoke(LogLevel.Error, $"Line ({cycleDeep.LineNo}) : CYCLE / DRILL command missing some arguments DEPTH, MMPM, CLEAR. Aborting prossing!");
                return false;
            }

            // now perform process for each goto command
            foreach (var goPos in gotoSeq)
            {
                // have the arguments of cycleDeep and goPos
                // see: http://bdml.stanford.edu/twiki/pub/Manufacturing/HaasReferenceInfo/V61_GPost_CD_Manual.pdf
                // pp. 98
                // for GRBL operation, also this might be interesting for setting G0 speed
                // see: https://diymachining.com/grbl-feed-rate/

                // go deep. Deepening vector direction (Z) is going negative!!
                var workZ = goPos.Z - deepDepth.Value;

                // some info in gcode
                gcode.AddComment($"Software-based drilling in one pass");
                gcode.AddComment(FormattableString.Invariant($"Target position: {goPos.X:0.000}, {goPos.Y:0.000}, {workZ:0.000}"));

                // go rapid to safe position
                gcode.Add(new GcodeItemMoveRapidG0(
                    x: goPos.X, y: goPos.Y, z: deepClear.Value, comment: "new cycle deep: go to clearance depth"));

                // feed SLOWLY to this depth
                gcode.Add(new GcodeItemMoveLinearG1(
                    x: goPos.X, y: goPos.Y, z: workZ,
                    feedRate: deepMmpm.Value,
                    comment: "go slowly in one pass to drill depth"));

                // retract FAST completely
                gcode.Add(new GcodeItemMoveRapidG0(
                    z: deepClear,
                    comment: "retract"));
            }

            // ok
            return true;
        }

        protected bool SubCommandCycleDeep(
            NclReader ncl,
            GcodeWriter gcode,
            NclItemCycleDeep cycleDeep,
            List<NclItemGoto> gotoSeq)
        {
            // access
            if (cycleDeep == null)
                return false;

            // get some argument values
            var deepDepth = cycleDeep.Args?.GetNamedValue("DEPTH");
            var deepStep = cycleDeep.Args?.GetNamedValue("STEP");
            var deepMmpm = cycleDeep.Args?.GetNamedValue("MMPM");
            var deepClear = cycleDeep.Args?.GetNamedValue("CLEAR");
            if (deepDepth == null || deepStep == null || deepMmpm == null || deepClear == null)
            {
                Log?.Invoke(LogLevel.Error, $"Line ({cycleDeep.LineNo}) : CYCLE / DEEP command missing some arguments DEPTH, STEP, MMPM, CLEAR. Aborting prossing!");
                return false;
            }

            // now perform process for each goto command
            foreach (var goPos in gotoSeq)
            {
                // have the arguments of cycleDeep and goPos
                // see: http://bdml.stanford.edu/twiki/pub/Manufacturing/HaasReferenceInfo/V61_GPost_CD_Manual.pdf
                // pp. 95
                // for GRBL operation, also this might be interesting for setting G0 speed
                // see: https://diymachining.com/grbl-feed-rate/

                // Deep drilling. Deepening vector direction (Z) is going negative!!
                var deepZ = goPos.Z - deepStep.Value;

                // some info in gcode
                gcode.AddComment($"Software-based deep drilling");
                gcode.AddComment(FormattableString.Invariant($"Target position: {goPos.X:0.000}, {goPos.Y:0.000}, {deepZ:0.000}"));

                // go rapid to safe position
                gcode.Add(new GcodeItemMoveRapidG0(
                    x: goPos.X, y: goPos.Y, z: deepClear.Value, comment: "new cycle deep: go to clearance depth"));

                // loop
                while (deepZ > goPos.Z - (deepDepth.Value + deepStep.Value))
                {
                    // avoid deepZ to be LESS than desired depth
                    var workZ = Math.Max(deepZ, goPos.Z - deepDepth.Value);

                    // feed SLOWLY to this depth
                    gcode.Add(new GcodeItemMoveLinearG1(
                        x: goPos.X, y: goPos.Y, z: workZ,
                        feedRate: deepMmpm.Value,
                        comment: "go slowly to step depth"));

                    // retract FAST completely
                    gcode.Add(new GcodeItemMoveRapidG0(
                        z: deepClear,
                        comment: "retract"));

                    // now take a step
                    deepZ -= deepStep.Value;
                }
            }

            // ok
            return true;
        }
    }
}
