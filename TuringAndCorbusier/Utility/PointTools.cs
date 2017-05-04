using System;
using System.Collections.Generic;
using Rhino.Geometry;
using System.Linq;
using Rhino.Display;
using System.IO;

namespace TuringAndCorbusier.Utility
{
    class PointTools
    {
        public static Point3d ChangeCoordinate(Point3d basePt, Plane fromPln, Plane toPln)
        {
            Point3d changedPt = basePt;
            changedPt.Transform(Transform.ChangeBasis(fromPln, toPln));

            return changedPt;
        }
    }
}
