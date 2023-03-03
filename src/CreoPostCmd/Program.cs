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
            // var ncl = new NclReader(@"L:\Creo-8\CNC3018\profilfräsen_2.ncl.2");
            // var ncl = new NclReader(@"L:\Creo-8\CNC3018\schruppen_1.ncl.1");
            var ncl = new NclReader(@"L:\Creo-8\CNC3018\bohren_1.ncl.8");

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
                gcode.WriteLinesToFile("out.nc");
            }
        }
    }
}