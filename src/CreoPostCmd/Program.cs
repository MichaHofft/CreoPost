using CreoPost;

namespace CreoPost
{
    /// <summary>
    /// Debug main program.
    /// TODO: use command line arguments
    /// </summary>
    internal class Program
    {
        static void Main(string[] args)
        {
            // strictly require 2 parameters
            if (args.Length != 2)
            {
                System.Console.WriteLine(Options.PrgVersionAndCredits);
                System.Console.WriteLine("Usage: CreoPostCmd <input .ncl file> <output .nc file>");
                System.Console.WriteLine("Parameters missing. Aborting!");
                Environment.Exit(-1);
            }

            var inFn = args[0];
            var outFn = args[1];

            // L:\Creo-8\CNC3018\bohren_2.ncl.1

            var ncl = new NclReader(inFn);

            if (ncl.ErrorNum > 0)
            {
                System.Console.WriteLine($"There are {ncl.ErrorNum} errors. Aborting!");
            }

            var gcode = new GcodeWriter();

            GcodeTemplates.AddHeaderLikeFreeCadGrbl(gcode);

            var post = new PostProcNclToGcode();
            var res = post.NctToGcode(ncl, gcode);
            if (!res)
            {
                System.Console.WriteLine($"There were errors. Aborting!");
            }
            else
            {
                GcodeTemplates.AddFooterLikeFreeCadGrbl(gcode);
                gcode.WriteLinesToFile(outFn);
            }
        }
    }
}