using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GISData.DataStruct;
using Rhino.Geometry;
using Rhino;
using Rhino.DocObjects;
using Rhino.Display;
namespace GISData.Extract
{
    public class PiljiDrawer
    {
        List<PiljiObj> drawn = new List<PiljiObj>();

        public void Draw(List<Pilji> piljis)
        {
            Delete();

            for (int i = 0; i < piljis.Count; i++)
            {
                PiljiObj obj = new PiljiObj(piljis[i]);
                obj.Draw();
                drawn.Add(obj);
            }
        }

        public void Unlock()
        {
            for (int i = 0; i < drawn.Count; i++)
            {
                drawn[i].UnLock();
            }
        }

        private void Delete()
        {
            foreach (var d in drawn)
                d.Delete();
            drawn.Clear();

            var objs = RhinoDoc.ActiveDoc.Objects.FindByObjectType(ObjectType.AnyObject);
            RhinoDoc.ActiveDoc.Objects.Delete(objs.Select(n=>n.Id).ToList(), true);
        }

        public Pilji Find(Guid guid)
        {
            for (int i = 0; i < drawn.Count; i++)
            {
                Pilji temp = drawn[i].GuidCheck(guid);
                if (temp == null)
                    continue;
                return temp;
            }

            return null;
        }

        public Pilji Find(string jibun)
        {
            for (int i = 0; i < drawn.Count; i++)
            {
                if (drawn[i].piljiData.Name != jibun)
                    continue;
                return drawn[i].piljiData;
            }
            //올일은 없겠지만.
            return null;
        }

        public PiljiObj FindDrawn(string jibun)
        {
            for (int i = 0; i < drawn.Count; i++)
            {
                if (drawn[i].piljiData.Name != jibun)
                    continue;
                return drawn[i];
            }
            //올일은 없겠지만.
            return null;
        }

        public void Hide()
        {
            drawn.ForEach(n => n.Delete());
            drawn.Clear();
        }

    }

    public class PiljiObj
    {
        public List<Guid> drawnObj = new List<Guid>();
        public Pilji piljiData;
        

        public PiljiObj(Pilji pilji)
        {
            piljiData = pilji;
        }

        public void Draw()
        {
            ObjectAttributes att = new ObjectAttributes() { ColorSource = ObjectColorSource.ColorFromObject, ObjectColor = System.Drawing.Color.Black, Mode = ObjectMode.Locked };

            if (piljiData.Jimok == "도")
                att.ObjectColor = System.Drawing.Color.Red;

            var outbounds = piljiData.Outbound;
            for (int j = 0; j < outbounds.Count; j++)
            {
                drawnObj.Add(RhinoDoc.ActiveDoc.Objects.Add(outbounds[j], att));
            }

            List<Point3d> points = new List<Point3d>();
            outbounds.ForEach(n => points.AddRange(n.DuplicateSegments().Select(m => m.PointAtStart)));
            BoundingBox bb = new BoundingBox(points);
            Point3d center = bb.Center;

            Text3d name = new Text3d(piljiData.Name,new Plane(center,Vector3d.XAxis,Vector3d.YAxis),1);
            att.Mode = ObjectMode.Normal;
            drawnObj.Add(RhinoDoc.ActiveDoc.Objects.AddText(name, att));
        }

        public void Delete()
        {
            foreach (var obj in drawnObj)
            {
                var robj = RhinoDoc.ActiveDoc.Objects.Find(obj);
                if (robj == null)
                    continue;
                robj.Attributes.Mode = ObjectMode.Normal;
                robj.CommitChanges();
                RhinoDoc.ActiveDoc.Objects.Delete(obj, true);
            }
        }

        public Pilji GuidCheck(Guid g)
        {
            if (drawnObj.Contains(g))
                return piljiData;
            else
                return null;
        }

        public void Hide()
        {
            ObjectAttributes att = new ObjectAttributes();
            att.Mode = ObjectMode.Normal;
            drawnObj.ForEach(n => RhinoDoc.ActiveDoc.Objects.ModifyAttributes(n, att, true));
            drawnObj.ForEach(n => RhinoDoc.ActiveDoc.Objects.Hide(n, true));
        }

        public void UnLock()
        {
            ObjectAttributes att = new ObjectAttributes();
            att.Mode = ObjectMode.Normal;
            drawnObj.ForEach(n => RhinoDoc.ActiveDoc.Objects.ModifyAttributes(n, att, true));
        }

    }
}
