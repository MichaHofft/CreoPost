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
            writer.AddComment("Exported by FreeCAD");
            writer.AddComment("Post Processor: grbl_G81_post");
            writer.AddComment("Output Time: 2020 - 11 - 11 19:11:33.165853");
            writer.AddComment("begin preamble");
            writer.AddGeneric("G17", "Draw Arcs in the XY plane, default.");
            writer.AddGeneric("G90", "All distances and positions are absolute values from the current origin.");
            writer.AddGeneric("G21", "All distances and positions are in mm");
            writer.AddComment("begin operation: Default Tool");
            writer.AddComment("Path: Default Tool");
            writer.AddComment("Default Tool");
            writer.AddComment("begin toolchange");
            writer.AddGeneric("(M6 T1.0)", "Toolchange with n=1..128");
            writer.Add(new GcodeItemSpindleRpmM3(9999, "Turn on spindle with specific RPM"));
            writer.AddComment("finish operation: Default Tool");
            writer.AddComment("begin operation: Drilling");
            writer.AddComment("Path: Drilling");
            writer.AddComment("Drilling");
            writer.AddComment("Begin Drilling");
            writer.AddGeneric("(G0 Z2.000)", "A rapid positioning move at the Rapid Feed Rate.");
            writer.AddGeneric("G90", "All distances and positions are absolute values from the current origin.");
            writer.AddGeneric("(G98)", "Switch on negative soft stops??");
            writer.AddComment("Drilling command: G83 X10.000 Y2.500 Z - 11.000 F30.00 Q2.000 R2.000");
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
