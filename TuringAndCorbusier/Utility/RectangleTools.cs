using System;
using System.Collections.Generic;
using Rhino.Geometry;
using System.Linq;
using Rhino.Display;
using System.IO;

namespace TuringAndCorbusier.Utility
{
    class RectangleTools
    {
        public static Rectangle3d DrawP2PRect(Point3d pointStart, Point3d pointEnd, double thickness)
        {
            Rectangle3d p2pRect = new Rectangle3d();

            Vector3d alignP2P = new Line(pointStart, pointEnd).UnitTangent;
            Vector3d perpP2P = VectorTools.RotateVectorXY(alignP2P, Math.PI / 2.0);

            Point3d corner1 = pointStart - alignP2P * thickness / 2.0 + perpP2P * thickness / 2.0;
            Point3d corner2 = pointEnd + alignP2P * thickness / 2.0 - perpP2P * thickness / 2.0;
            Plane p2pPlane = new Plane(pointStart, alignP2P, perpP2P);

            p2pRect = new Rectangle3d(p2pPlane, corner1, corner2);

            return p2pRect;
        }

        public static Rectangle3d DrawP2PRect(Point3d pointStart, Point3d pointEnd, double perpThickness, double alignThickness)
        {
            Rectangle3d p2pRect = new Rectangle3d();

            Vector3d alignP2P = new Line(pointStart, pointEnd).UnitTangent;
            Vector3d perpP2P = VectorTools.RotateVectorXY(alignP2P, Math.PI / 2.0);

            Point3d corner1 = pointStart - alignP2P * alignThickness / 2.0 + perpP2P * perpThickness / 2.0;
            Point3d corner2 = pointEnd + alignP2P * alignThickness / 2.0 - perpP2P * perpThickness / 2.0;
            Plane p2pPlane = new Plane(pointStart, alignP2P, perpP2P);

            p2pRect = new Rectangle3d(p2pPlane, corner1, corner2);

            return p2pRect;
        }
    }
}
