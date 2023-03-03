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
    // Some ACL explanations
    // see: http://bdml.stanford.edu/twiki/pub/Manufacturing/HaasReferenceInfo/V61_GPost_CD_Manual.pdf

    public class NclItemBase
    {
        public int LineNo;
        public string RawLine;
        public string Comment;
    }

    public class NclItemUnknown : NclItemBase
    {
    }

    public class NclItemGlobal : NclItemBase
    {
    }

    public class NclItemBlank : NclItemBase
    {
    }

    public class NclItemLoadTool : NclItemBase
    {
        public int ToolNo;
    }

    public class NclItemSpindleRpm : NclItemBase
    {
        public double Rpm;
    }

    public class NclItemSpindleOff : NclItemBase
    {
    }

    public class NclItemFeedRate : NclItemBase
    {
        public double FeedRate;
        public NclUnit? Unit;
    }

    public class NclItemRapid : NclItemBase
    {
    }

    public class NclItemFini : NclItemBase
    {
    }

    public class NclItemGoto : NclItemBase
    {
        public double X, Y, Z;
    }

    public class NclItemCycleDeep : NclItemBase
    {
        public NclArgValueList Args = new NclArgValueList();
    }

    public class NclItemCycleOff : NclItemBase
    {
    }

    public class NclItemCircle : NclItemBase
    {
        // see: http://bdml.stanford.edu/twiki/pub/Manufacturing/HaasReferenceInfo/V61_GPost_CD_Manual.pdf
        // Clause 4.8, pp. 66
        // I/J/K are vector of circular normal
        // R is radius

        public double X, Y, Z, I, J, K, R;
    }

    /// <summary>
    /// Single named argument of a command.
    /// </summary>
    public class NclArgValue
    {
        public string Arg = "";
        public double Value = 0.0;
    }

    /// <summary>
    /// Some commands have named arguments in (arbitrary??) sequence.
    /// This is a help class.
    /// </summary>
    public class NclArgValueList : Dictionary<string, NclArgValue>
    {
        public NclArgValueList Parse(string st)
        {
            var its = st.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            for (int i=0; i<its.Length - 1; i+=2)
            {
                var astr = its[i].ToUpper();
                var vstr = its[i + 1];

                if (double.TryParse(vstr, System.Globalization.NumberStyles.Float, CultureInfo.InvariantCulture, out var f))
                    this.Add(astr, new NclArgValue() { 
                        Arg = astr, Value = f 
                    });
            }
            return this;
        }

        public double? GetNamedValue(string argName)
        {
            if (!this.ContainsKey(argName))
                return null;
            return this[argName]?.Value;
        }
    }

    public enum NclUnit { 
        MM, Inch, 
        InchPerMin, InchPerRev, SurfacefeetPerMin,
        MmPerMin, MmPerRev, SurfaceMeterPerMin
    };

    public class NclGlobals
    {
        public string PartNo;
        public string PostProc;
        public string MachineNo;
        public NclUnit Units = NclUnit.MM;
    }

    public class NclReader
    {
        public List<NclItemBase> Lines = new List<NclItemBase>();
        
        public NclGlobals Globals = new NclGlobals();

        public int ErrorNum = 0;

        public NclReader()
        {
        }

        public NclReader(string fn)
        {
            ReadNclLines(fn);
        }

        public static List<double>? ParseDoubles(string ln)
        {
            // res?
            var res = new List<double>();

            // into items
            var items = ln.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            // loop
            foreach (var it in items)
                if (double.TryParse(it, System.Globalization.NumberStyles.Float, CultureInfo.InvariantCulture, out var f))
                    res.Add(f);
                else
                    return null;

            // ok!
            return res;
        }

        public static NclUnit? ParseUnit(string st)
        {
            foreach (var x in (NclUnit[])Enum.GetValues(typeof(NclUnit)))
                if (x.ToString().Equals(st, StringComparison.InvariantCultureIgnoreCase))
                    return x;
            return null;
        }

        public bool ParseLine(int lineno, string raw)
        {
            // cmt?
            var ln = raw;
            var cmt = "";
            var i = ln.IndexOf("$$");
            if (i >= 0)
            {
                cmt = ln.Substring(i + 2);
                ln = ln.Substring(0, i);
            }

            // trimmed
            ln = ln.Trim();

            // error?
            var error = false;
            var found = false;

            // recognize
            NclItemBase item = null;

            // some globals
            var m = Regex.Match(ln, @"^PARTNO\s*/\s*(\w.*)");
            if (m.Success)
            {
                item = new NclItemGlobal();
                Globals.PartNo = m.Groups[1].ToString().Trim();
                found = true;
            }

            m = Regex.Match(ln, @"^MACHIN\s*/\s*([^,]+),\s*(\w.*)");
            if (!found && m.Success)
            {
                item = new NclItemGlobal();
                Globals.PostProc = m.Groups[1].ToString().Trim();
                Globals.MachineNo = m.Groups[2].ToString().Trim();
                found = true;
            }

            m = Regex.Match(ln, @"^UNITS\s*/\s*(\w.*)");
            if (!found && m.Success)
            {
                item = new NclItemGlobal();
                var un = m.Groups[1].ToString().Trim().ToLower();
                if (un == "mm")
                    Globals.Units = NclUnit.MM;
                else if (un == "inch")
                    Globals.Units = NclUnit.Inch;
                else
                {
                    error = true;
                }
                found = true;
            }

            m = Regex.Match(ln, @"^LOADTL\s*/\s*(\d+)$");
            if (!found && m.Success)
            {
                item = new NclItemLoadTool()
                {
                    ToolNo = Convert.ToInt32(m.Groups[1].ToString().Trim())
                };
                found = true;
            }

            m = Regex.Match(ln, @"^SPINDL\s*/\s*RPM,\s*([-0-9+.]+)");
            if (!found && m.Success)
                if (double.TryParse(m.Groups[1].ToString().Trim(), NumberStyles.Float, 
                    CultureInfo.InvariantCulture, out var f))
                {
                    item = new NclItemSpindleRpm()
                    {
                        Rpm = f
                    };
                    found = true;
                }

            m = Regex.Match(ln, @"^SPINDL\s*/\s*OFF$");
            if (!found && m.Success)
            {
                item = new NclItemSpindleOff()
                {
                };
                found = true;
            }

            m = Regex.Match(ln, @"^RAPID$");
            if (!found && m.Success)
            {
                item = new NclItemRapid()
                {
                };
                found = true;
            }

            m = Regex.Match(ln, @"^FINI");
            if (!found && m.Success)
            {
                item = new NclItemFini()
                {
                };
                found = true;
            }

            m = Regex.Match(ln, @"^GOTO\s*/\s*([-0-9+., ]+)");
            if (!found && m.Success)
                if (ParseDoubles(m.Groups[1].ToString().Trim()) is List<double> dbls
                    && dbls.Count == 3)
                {
                    item = new NclItemGoto()
                    {
                        X = dbls[0],
                        Y = dbls[1],
                        Z = dbls[2]
                    };
                    found = true;
                }

            m = Regex.Match(ln, @"^CIRCLE\s*/\s*([-0-9+., ]+)");
            if (!found && m.Success)
                if (ParseDoubles(m.Groups[1].ToString().Trim()) is List<double> dbls
                    && dbls.Count == 7)
                {
                    item = new NclItemCircle()
                    {
                        X = dbls[0],
                        Y = dbls[1],
                        Z = dbls[2],
                        I = dbls[3],
                        J = dbls[4],
                        K = dbls[5],
                        R = dbls[6]
                    };
                    found = true;
                }

            m = Regex.Match(ln, @"^FEDRAT\s*/\s*([-0-9+.]+),\s*(\w+)");
            if (!found && m.Success)
                if (double.TryParse(m.Groups[1].ToString().Trim(), System.Globalization.NumberStyles.Float,
                    CultureInfo.InvariantCulture, out var f))
                {
                    item = new NclItemFeedRate()
                    {
                        FeedRate = f,
                        Unit = ParseUnit(m.Groups[2].ToString().Trim())
                    };
                    found = true;
                }

            m = Regex.Match(ln, @"^CYCLE\s*/\s*DEEP\s*,\s*(.+)");
            if (!found && m.Success)
                if ((new NclArgValueList().Parse(m.Groups[1].ToString())) is NclArgValueList alist)
                {
                    item = new NclItemCycleDeep()
                    {
                        Args = alist
                    };
                    found = true;
                }

            m = Regex.Match(ln, @"^CYCLE\s*/\s*OFF\s*$");
            if (!found && m.Success)
            {
                item = new NclItemCycleOff();
                found = true;
            }

            // empty
            if (!found && ln.Trim() == "")
            {
                item = new NclItemBlank()
                {
                };
                found = true;
            }

            // finalize item
            if (error || !found)
            {
                item = new NclItemUnknown()
                {
                    RawLine = raw,
                    LineNo = lineno
                };
                Lines.Add(item);
                return false;
            }

            if (item != null)
            {
                // enrich
                item.RawLine = raw;
                item.LineNo = lineno;
                item.Comment = cmt;
                Lines.Add(item);
                return true;
            }

            // hmm?
            return false;
        }

        public void ReadNclLines(string fn)
        {
            // first read line by line
            var lines = System.IO.File.ReadAllLines(fn);

            // now join some lines
            var joined = new List<string>();
            for (int li=0; li < lines.Length; li++)
            {
                var sum = lines[li].TrimEnd('$', ' ');
                while (li < lines.Length && lines[li].TrimEnd().EndsWith("$"))
                {
                    li++;
                    sum += " " + lines[li].TrimStart().TrimEnd('$', ' ');
                }
                joined.Add(sum);
            }

            // parse
            int lineno = 1;
            foreach (var ln in joined)
                ParseLine(lineno++, ln);

            // check for errors
            foreach (var l in Lines)
                if (l is NclItemUnknown niu)
                {
                    ErrorNum++;
                    System.Console.WriteLine($"Unknown line ({niu.LineNo}): {niu.RawLine}");
                }

            // ok
            ;
        }
    }
}
