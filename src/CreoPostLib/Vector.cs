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
    /// This class contains simple vector mathematics.
    /// </summary>
    public class Vector
    {
        public struct Pos3
        {
            public double X = 0.0;
            public double Y = 0.0;
            public double Z = 0.0;

            public Pos3()
            {
            }

            public Pos3(double x, double y, double z)
            {
                X = x;
                Y = y;
                Z = z;
            }

            public Pos3 Add(double? x, double? y, double? z)
            {
                if (x.HasValue)
                    X += x.Value;
                if (y.HasValue)
                    Y += y.Value;
                if (z.HasValue)
                    Z += z.Value;
                return this;
            }
        }
    }
}
