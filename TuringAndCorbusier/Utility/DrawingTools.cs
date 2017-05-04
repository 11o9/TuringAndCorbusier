using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rhino;
using Rhino.Geometry;
using System.Drawing;

namespace TuringAndCorbusier.Utility
{
    class DrawingTools
    {
        public static void Print(IEnumerable<Curve> lc, Color color, RhinoDoc doc)
        {
            foreach (var r in lc)
            {
                if (r == null)
                    continue;
                Guid temp = doc.Objects.Add(r);
                var obj = doc.Objects.Find(temp);
                obj.Attributes.ColorSource = Rhino.DocObjects.ObjectColorSource.ColorFromObject;
                obj.Attributes.ObjectColor = color;
                obj.CommitChanges();
            }
        }

        public static void Print(IEnumerable<Polyline> lc, Color color, RhinoDoc doc)
        {
            foreach (var r in lc)
            {
                if (r.Count == 0)
                    continue;
                Guid temp = doc.Objects.AddPolyline(r);
                var obj = doc.Objects.Find(temp);
                obj.Attributes.ColorSource = Rhino.DocObjects.ObjectColorSource.ColorFromObject;
                obj.Attributes.ObjectColor = color;
                obj.CommitChanges();
            }
        }

        public static void PrintPLtoBrep(IEnumerable<Polyline> lc, Color color, RhinoDoc doc)
        {

            var breps = Brep.CreatePlanarBreps(lc.Select(n => n.ToNurbsCurve()));

            foreach (var r in breps)
            {

                Guid temp = doc.Objects.Add(r);
                var obj = doc.Objects.Find(temp);
                obj.Attributes.ColorSource = Rhino.DocObjects.ObjectColorSource.ColorFromObject;
                obj.Attributes.ObjectColor = color;
                obj.CommitChanges();
            }
        }

        public static void PrintCRVtoBrep(IEnumerable<Curve> lc, Color color, RhinoDoc doc)
        {

            var breps = Brep.CreatePlanarBreps(lc);

            foreach (var r in breps)
            {

                Guid temp = doc.Objects.Add(r);
                var obj = doc.Objects.Find(temp);
                obj.Attributes.ColorSource = Rhino.DocObjects.ObjectColorSource.ColorFromObject;
                obj.Attributes.ObjectColor = color;
                obj.CommitChanges();
            }
        }
    }
}
