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
    /// This class contains helper function to add (fixed) parts to a gcode writer.
    /// </summary>
    public static class GcodeTemplates
    {
        // Some GCODE explanations:
        // https://www.sainsmart.com/blogs/news/grbl-v1-1-quick-reference#Parameters
        // https://marlinfw.org/docs/gcode/G002-G003.html

        /// <summary>
        /// Add the header as it is writen in FreeCad -> GCODE -> CNC3018
        /// </summary>
        public static void AddHeaderLikeFreeCadGrbl(GcodeWriter writer)
        {
            writer.AddComment("Header structure taken from FreeCAD");
            writer.AddComment("Post Processor: grbl_G81_post");
            writer.AddComment("begin preamble");
            writer.AddGeneric("G17 G90", "Arcs in the XY plane. Absolute values.");
            writer.AddGeneric("G21", "Values in mm");
            writer.AddComment("Default Tool");
            writer.AddComment("begin toolchange");
            writer.AddGeneric("(M6 T1.0)", "Toolchange with n=1..128");
            writer.Add(new GcodeItemSpindleRpmM3(9999, "Turn on spindle with specific RPM"));
        }

        // <summary>
        /// Add the footer as it is writen in FreeCad -> GCODE -> CNC3018
        /// </summary>
        public static void AddFooterLikeFreeCadGrbl(GcodeWriter writer)
        {
            writer.AddComment("begin postamble");
            writer.AddGeneric("M5", "Spindle OFF");
            writer.AddGeneric("G17", "Draw Arcs in the XY plane, default.");
            writer.AddGeneric("G90", "All distances and positions are absolute values from the current origin.");
            writer.AddGeneric("M2", "Abort program without stopping spindle");
        }
    }
}
