using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Runtime.Intrinsics.Arm;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CreoPost
{
    public class GcodeItemBase
    {
        public int LineNo;
        public string RawLine = "";
        public string Comment = "";

        public void ComposeRaw(string cmd, string cmt, bool indentCmt = true)
        {
            RawLine = cmd;
            if (cmt != null && cmt.Length > 0)
            {
                if (RawLine.Length > 0)
                    RawLine += " ";

                while (indentCmt && RawLine.Length < 40)
                    RawLine += " ";

                RawLine += "(" + cmt + ")";
            }
        }

        public void AddParam(ref string parameters, string name, double? value)
        {
            // value
            if (!value.HasValue || parameters == null)
                return;

            // comma?
            if (parameters.Length > 0)
                parameters += " ";

            parameters += name;

            parameters += FormattableString.Invariant($"{value:0.0000}");
        }

        public virtual void SetRaw()
        {
            ComposeRaw("", Comment);
        }
    }

    public class GcodeItemComment : GcodeItemBase
    {
        public override void SetRaw()
        {
            ComposeRaw("", Comment, indentCmt: false);
        }

        public GcodeItemComment(string comment = "")
        {
            Comment = comment;
        }
    }

    public class GcodeItemGeneric : GcodeItemBase
    {
        public string Cmd = "";

        public override void SetRaw()
        {
            ComposeRaw(Cmd, Comment);
        }

        public GcodeItemGeneric(string cmd, string comment = "")
        {
            Cmd = cmd;
            Comment = comment;
        }
    }

    public class GcodeItemSpindleRpmM3 : GcodeItemBase
    {
        public double Rpm = 0.0;

        public override void SetRaw()
        {
            ComposeRaw(FormattableString.Invariant($"M3 S{Rpm:0.000}"), Comment);
        }

        public GcodeItemSpindleRpmM3(double rpm, string comment = "")
        {
            Rpm = rpm;
            Comment = comment;
        }
    }

    public class GcodeItemSpindleOffM5 : GcodeItemBase
    {
        public override void SetRaw()
        {
            ComposeRaw("M5", Comment);
        }

        public GcodeItemSpindleOffM5(string comment = "")
        {
            Comment = comment;
        }
    }

    public abstract class GcodeItemMoveAbstract : GcodeItemBase
    {
        public double? X;
        public double? Y;
        public double? Z;
    }

    public class GcodeItemMoveRapidG0 : GcodeItemMoveAbstract
    {
        public GcodeItemMoveRapidG0(double? x = null, double? y = null, double? z = null, string comment = null)
        {
            Comment = comment;
            X = x;
            Y = y;
            Z = z;
        }

        public override void SetRaw()
        {
            var cmd = "G0";
            AddParam(ref cmd, "X", X);
            AddParam(ref cmd, "Y", Y);
            AddParam(ref cmd, "Z", Z);
            ComposeRaw(cmd, Comment);
        }
    }

    public class GcodeItemMoveLinearG1 : GcodeItemMoveAbstract
    {
        public double? Feedrate;

        public GcodeItemMoveLinearG1(double? x = null, double? y = null, double? z = null, double? feedRate = null, string comment = null)
        {
            Comment = comment;
            X = x;
            Y = y;
            Z = z;
            Feedrate = feedRate;
        }

        public override void SetRaw()
        {
            var cmd = "G1";
            AddParam(ref cmd, "X", X);
            AddParam(ref cmd, "Y", Y);
            AddParam(ref cmd, "Z", Z);
            AddParam(ref cmd, "F", Feedrate);
            ComposeRaw(cmd, Comment);
        }
    }

    public class GcodeItemArcAbstract : GcodeItemMoveAbstract
    {
        // see: https://marlinfw.org/docs/gcode/G002-G003.html
        // either I,J or R shall be specified

        public double? Feedrate;

        public double? I;
        public double? J;
        public double? R;
    }

    public class GcodeItemArcClockwise : GcodeItemArcAbstract
    { 
        public override void SetRaw()
        {
            var cmd = "G2 ";
            AddParam(ref cmd, "X", X);
            AddParam(ref cmd, "Y", Y);
            AddParam(ref cmd, "Z", Z);
            AddParam(ref cmd, "I", I);
            AddParam(ref cmd, "J", J);
            AddParam(ref cmd, "R", R);
            AddParam(ref cmd, "F", Feedrate);
            ComposeRaw(cmd, Comment);
        }
    }

    public class GcodeItemArcAntiClockwise : GcodeItemArcAbstract
    {
        public override void SetRaw()
        {
            var cmd = "G3";
            AddParam(ref cmd, "X", X);
            AddParam(ref cmd, "Y", Y);
            AddParam(ref cmd, "Z", Z);
            AddParam(ref cmd, "I", I);
            AddParam(ref cmd, "J", J);
            AddParam(ref cmd, "R", R);
            AddParam(ref cmd, "F", Feedrate);
            ComposeRaw(cmd, Comment);
        }
    }

    public class GcodeWriter
    {
        public List<GcodeItemBase> Lines = new List<GcodeItemBase>();
        
        public void AddComment(string comment)
        {
            Lines.Add(new GcodeItemComment(comment));
        }

        public void AddGeneric(string cmd, string comment = "")
        {
            Lines.Add(new GcodeItemGeneric(cmd, comment));
        }

        public void Add(GcodeItemBase item)
        {
            Lines.Add(item);
        }

        public void WriteLines(string path)
        {
            // prepare all lines
            var lineno = 1;
            foreach (var ln in Lines)
            {
                ln.LineNo = lineno++;
                ln.SetRaw();
            }

            // write
            System.IO.File.WriteAllLines(path, Lines.Select((ln) => ln.RawLine));
        }
    }
}
