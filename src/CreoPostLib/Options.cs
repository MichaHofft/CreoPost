using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft;
using Newtonsoft.Json;

namespace CreoPost
{
    /// <summary>
    /// Some minimal constants and options
    /// </summary>
    public class Options
    {
        /// <summary>
        /// Constant to have in all UIs the same info.
        /// </summary>
        public const string PrgVersionAndCredits = "CreoPost v.0.1 (c) by Michael Hoffmeister.";

        /// <summary>
        /// When a file is currently loaded and is updated by a file access outside, update the
        /// contents and may be trigger new transformation.
        /// </summary>
        public bool AutoUpdateFile = true;

        /// <summary>
        /// When a file is currently loaded and a file in the __SAME__ directory is created/ updated,
        /// automatically load this new file and filename, update the contents and may be trigger a 
        /// new transformation.
        /// </summary>
        public bool AutoLoadFromSameDir = false;

        /// <summary>
        /// When a input file was successfully loaded, directly trigger a transformation (this is, the
        /// post processing (A|B)CL -> GCode, to the output content.
        /// </summary>
        public bool AutoTransform = false;

        /// <summary>
        /// When a input file was successfully loaded, set the output filename according to the input
        /// filename. If not set, leave it constant.
        /// </summary>
        public bool AutoAdaptFilename = true;

        /// <summary>
        /// Then a transformation (post processing) was successfully done, save the output content with 
        /// the current output filename.
        /// </summary>
        public bool AutoSave = false;

        /// <summary>
        /// Then a transformation (post processing) was successfully done, save the output content with 
        /// the current output filename.
        /// </summary>
        public bool AutoPasteBin = false;

        /// <summary>
        /// Id for accessing the PasteBin. Typically 8 characters.
        /// Will form a raw url: https://pastebin.com/raw/{id}
        /// </summary>
        public string PasteBinId = "";

        //
        // Functionality
        //

        public static string GetDefaultOptionsFn()
        {
            var assy = Assembly.GetExecutingAssembly();
            var optfn = System.IO.Path.Combine(
                        System.IO.Path.GetDirectoryName(assy.Location) ?? "",
                        "CreoPost.options.json");
            return optfn;
        }

        public static Options? LoadFromFile(string path)
        {
            var json = System.IO.File.ReadAllText(path);
            var res = JsonConvert.DeserializeObject<Options>(json);
            return res;
        }

        public static void SaveFile(string path, Options options)
        {
            var json = JsonConvert.SerializeObject(options, Formatting.Indented);
            System.IO.File.WriteAllText(path, json);
        }
    }
}
