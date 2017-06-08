using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rhino.Geometry;

namespace TuringAndCorbusier
{
    public class KDGinfo
    {
        public List<Curve> surrbuildings = new List<Curve>();
        public Curve outrect = null;
        public Point3d center = Point3d.Unset;
        public Curve boundary = null;
        public Curve setbackBoundary = null;
        public double scalefactor = 0;

        public List<Guid> regacy = new List<Guid>();

        public double slopeRatio;
        public Brep ground;
        public Vector3d normDir;

        public List<Point3d> campos = new List<Point3d>();
       
       


        public KDGinfo(Curve boundary, double ScaleFactor, bool drawing)
        {
            //long mem1 = GC.GetTotalMemory(false);
            
            scalefactor = ScaleFactor;
            surrbuildings = KDG.getInstance().GetRectAndHideOtherObjects(boundary);
            //long mem2 = GC.GetTotalMemory(false);
            //GC.Collect();
            outrect = KDG.getInstance().OutRect(boundary);
            //long mem3 = GC.GetTotalMemory(false);
            //GC.Collect();
            center = AreaMassProperties.Compute(boundary).Centroid;
            //long mem4 = GC.GetTotalMemory(false);
            //GC.Collect();
            this.boundary = boundary.DuplicateCurve();
            ExtendKDG(ScaleFactor);
            //long mem5 = GC.GetTotalMemory(false);
            //GC.Collect();

            SetCampos();

            //split surface


            if (drawing)
            {
                var objectsToHide = Rhino.RhinoDoc.ActiveDoc.Objects.FindByObjectType(Rhino.DocObjects.ObjectType.AnyObject);

                foreach (var objectToHide in objectsToHide)
                {
                    Rhino.RhinoDoc.ActiveDoc.Objects.Hide(objectToHide.Id, true);
                }



                var regrec = Rhino.RhinoDoc.ActiveDoc.Objects.Add(outrect);
                regacy.Add(regrec);
                foreach (Curve curve in surrbuildings)
                {
                    var reg = Rhino.RhinoDoc.ActiveDoc.Objects.Add(curve);
                    regacy.Add(reg);
                }


            }
            //long mem6 = GC.GetTotalMemory(false);
            //Rhino.RhinoApp.WriteLine("Memory123: {0} , {1} , {2}", mem1,mem2,mem3);
            //Rhino.RhinoApp.WriteLine("Memory456: {0} , {1} , {2}", mem4, mem5, mem6);

        }

        private void ExtendKDG(double ScaleFactor)
        {
            surrbuildings = scaleCurves(ref surrbuildings, ScaleFactor);
            outrect = scaleCurve(outrect, ScaleFactor);
            boundary = scaleCurve(boundary, ScaleFactor);
        }

        private void SetCampos()
        {
            campos = outrect.DuplicateSegments().Select(n => n.PointAtStart + new Vector3d(center - n.PointAtStart) * 0.5).ToList();
        }


        private List<Curve> scaleCurves(ref List<Curve> input, double ScaleFactor)
        {
            for (int i = 0; i < input.Count; i++)
            {
                input[i].Transform(Transform.Scale(center, ScaleFactor));
            }
            return input;
        }

        private Curve scaleCurve(Curve input, double ScaleFactor)
        {

            input.Transform(Transform.Scale(center, ScaleFactor));

            return input;
        }

        public void Getdir(List<Point3d> points)
        {
            Polyline poly;
            boundary.TryGetPolyline(out poly);
            normDir = KDG.getInstance().normalDirection(points, poly, out slopeRatio, out ground);

        }

        public void HideRegacy()
        {
            var objectsToHide = Rhino.RhinoDoc.ActiveDoc.Objects.FindByObjectType(Rhino.DocObjects.ObjectType.Point);



            foreach (var reg in regacy)
            {
                Rhino.RhinoDoc.ActiveDoc.Objects.Hide(reg, true);
            }
            foreach (var reg in objectsToHide)
            {
                Rhino.RhinoDoc.ActiveDoc.Objects.Hide(reg, true);
            }

        }
    }


    public class KDG
    {

        private static KDG instance = new KDG();


        public List<Curve> temp = new List<Curve>();
        public Curve outrect = null;
        public Point3d center = Point3d.Unset;



        private KDG()
        {
            if (instance == null)
                instance = this;
        }
        public static KDG getInstance()
        { return instance; }



        /// <summary>
        /// 중점찾기
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>


        public Point3d CenterPoint(Curve c)
        {
            Polyline d;
            c.TryGetPolyline(out d);

            return d.CenterPoint();

            //regacy

            //var components = c.DuplicateSegments();

            //double X = 0;
            //double Y = 0;
            //double Z = 0;
            //foreach (Curve d in components)
            //{
            //    X += d.PointAtStart.X;
            //    Y += d.PointAtStart.Y;
            //    Z += d.PointAtStart.Z;
            //}

            //X /= components.Length;
            //Y /= components.Length;
            //Z /= components.Length;

            //Point3d center = new Point3d(X, Y, Z);

            ////Rhino.RhinoDoc.ActiveDoc.Objects.AddPoint(center);
            //////

            //////

            //return center;

        }



        /// <summary>
        /// 외곽 사각형 찾기
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>

        public PolylineCurve OutRect(Curve c)
        {



            Point3d Center = CenterPoint(c);
            center = Center;
            //Rhino.RhinoDoc.ActiveDoc.Objects.AddPoint(Center);

            double MX = 0;
            double MY = 0;
            Curve[] components = c.DuplicateSegments();
            foreach (Curve d in components)
            {
                double CY = d.PointAtStart.Y - Center.Y;
                double CX = d.PointAtStart.X - Center.X;

                if (Math.Abs(CY) > MY)
                    MY = Math.Abs(CY);
                if (Math.Abs(CX) > MX)
                    MX = Math.Abs(CX);
            }
            //Rhino.RhinoApp.WriteLine("MX = " + MX.ToString() + "MY = " + MY.ToString());
            if (MX > 1.625f * MY)
            {
                //Rhino.RhinoApp.WriteLine("Y확장");
                MY = MX / 1.625f;

            }
            else if (MX < 1.625f * MY)
            {
                MX = MY * 1.625;
                //Rhino.RhinoApp.WriteLine("X확장");
            }

            //Rhino.RhinoApp.WriteLine("MX = " + MX.ToString() + "MY = " + MY.ToString());
            List<Point3d> rectAP = new List<Point3d>();

            for (int i = 0; i < 4; i++)
            {

                double scale = 1.3;

                double a = scale;
                double b = scale;


                if (i < 2)
                    a = -scale;
                if (i % 3 == 0)
                    b = -scale;


                Point3d tempPoint = new Point3d(Center.X + (a * MX), Center.Y + (b * MY), Center.Z);
                //Rhino.RhinoDoc.ActiveDoc.Objects.AddPoint(tempPoint);
                rectAP.Add(tempPoint);
            }

            rectAP.Add(rectAP[0]);

            PolylineCurve rect = new PolylineCurve(rectAP);

            //Rhino.RhinoDoc.ActiveDoc.Objects.AddPoints(rectAP);

            //Rhino.RhinoDoc.ActiveDoc.Objects.AddCurve(rect);
            return rect;


        }

        /// <summary>
        /// 외곽사각형 외부의 오브젝트 숨김
        /// </summary>
        /// <param name="c"></param>

        public List<Curve> GetRectAndHideOtherObjects(Curve c)
        {
            var rect = OutRect(c);

            outrect = rect;
            //long mem1 = GC.GetTotalMemory(false);
            //GC.Collect();
            var objects = Rhino.RhinoDoc.ActiveDoc.Objects.Where(n => n.Geometry.ObjectType == Rhino.DocObjects.ObjectType.Curve);
            //long mem2 = GC.GetTotalMemory(false);
            //GC.Collect();
            var texts = Rhino.RhinoDoc.ActiveDoc.Objects.FindByObjectType(Rhino.DocObjects.ObjectType.Annotation);
            //long mem3 = GC.GetTotalMemory(false);
            //GC.Collect();

            List<Rhino.DocObjects.RhinoObject> inside = new List<Rhino.DocObjects.RhinoObject>();

            List<Rhino.DocObjects.RhinoObject> crossed = new List<Rhino.DocObjects.RhinoObject>();


            List<Rhino.DocObjects.CurveObject> curves = new List<Rhino.DocObjects.CurveObject>();



            /// 교차확인

            foreach (var obj in objects)
            {
                Curve objc = (obj as Rhino.DocObjects.CurveObject).CurveGeometry;
                var intersect = Rhino.Geometry.Intersect.Intersection.CurveCurve(objc, rect, 0, 0);
                Point3d temps = objc.PointAtStart;
                Point3d tempe = objc.PointAtEnd;


                if (intersect.Count <= 0)
                {
                    if ((rect.Contains(temps) == PointContainment.Outside) && (rect.Contains(tempe) == PointContainment.Outside))
                        continue;
                    //Rhino.RhinoDoc.ActiveDoc.Objects.Hide(obj, true);
                    //의심포인트1
                    else if (objc != c)
                        inside.Add(obj);

                }
                else if (intersect.Count >= 1)
                {
                    List<double> intersectionpoints = new List<double>();

                    intersectionpoints.AddRange(intersect.Select(n => n.ParameterA));

                    Curve copy = objc.Duplicate() as Curve;

                    Rhino.RhinoDoc.ActiveDoc.Objects.Delete(obj, true);

                    var splitrect = copy.Split(intersectionpoints);

                    Rhino.RhinoDoc.ActiveDoc.Layers.SetCurrentLayerIndex(Rhino.RhinoDoc.ActiveDoc.Layers.Find("ZC304", true), true);

                    var innerpiece = splitrect.Where(n => rect.Contains(n.PointAt(n.Domain.Mid)) == PointContainment.Inside);

                    var cobjs = innerpiece.Select(n => Rhino.RhinoDoc.ActiveDoc.Objects.Add(n)).Select(m => Rhino.RhinoDoc.ActiveDoc.Objects.Find(m) as Rhino.DocObjects.CurveObject);

                    curves.AddRange(cobjs);

                    Rhino.RhinoDoc.ActiveDoc.Layers.SetCurrentLayerIndex(Rhino.RhinoDoc.ActiveDoc.Layers.Find("Default", true), true);
                }
            }

            //long mem4 = GC.GetTotalMemory(false);
            //GC.Collect();
            foreach (var curveobject in inside)
            {
                curves.Add(curveobject as Rhino.DocObjects.CurveObject);
            }

            foreach (var text in texts)
            {
                if (rect.Contains(text.Geometry.GetBoundingBox(true).Center) == PointContainment.Outside)
                    Rhino.RhinoDoc.ActiveDoc.Objects.Hide(text, true);
            }
    


            List<Curve> result = new List<Curve>();
            List<Guid> guids = new List<Guid>();

            foreach (var curve in curves)
            {
                if (curve.CurveGeometry.ClosedCurveOrientation(Plane.WorldXY) == CurveOrientation.CounterClockwise)
                    curve.CurveGeometry.Reverse();
                result.Add(curve.CurveGeometry as Curve);

            }
            Rhino.RhinoDoc.ActiveDoc.Views.Redraw();

            temp = result;
            return result;
        }

        public Curve GetBound(Curve c, Point3d origin)
        {
            List<Curve> contact = new List<Curve>();
            List<Curve> final = new List<Curve>();
            foreach (Curve d in temp)
            {
                if (c.PointAtEnd == d.PointAtStart)
                    if (d.PointAtEnd == origin)
                    {
                        final.Add(d);
                        return d;
                    }
                    else
                        contact.Add(d);
            }

            if (contact.Count == 1)
            {
                final.Add(GetBound(contact[0], origin));
                Curve[] result = Curve.JoinCurves(final);
                return result[0];
            }
            else if (contact.Count == 0)
            {
                return c;
            }
            else
            {
                float maxdgree = 0;
                Curve temp = null;
                foreach (Curve d in contact)
                {
                    if (maxdgree < d.Degree - c.Degree)
                    {
                        maxdgree = d.Degree - c.Degree;
                        temp = d;
                    }

                    //Rhino.RhinoApp.WriteLine("maxdgree = " + maxdgree + " d.dgree = " + d.Degree + " c.dgree = " + c.Degree);

                }

                final.Add(GetBound(temp, origin));
                Curve[] result = Curve.JoinCurves(final);
                return result[0];

            }



        }
        public List<Brep> KDGmaker(List<Curve> segs, Curve boundary, List<Point3d> roadPoints, double scalefactor)
        {
            //split surface
            Rhino.RhinoDoc.ActiveDoc.Layers.SetCurrentLayerIndex(Rhino.RhinoDoc.ActiveDoc.Layers.Find("ZC304", true), true);
            List<Point3d> bboxPts = new List<Point3d>();
            foreach (Curve c in segs)
            {
                bboxPts.Add(c.PointAtStart);
                bboxPts.Add(c.PointAtEnd);
            }
            var bbox = new BoundingBox(bboxPts);
            Rectangle3d rect = new Rectangle3d(Plane.WorldXY, bbox.Min, bbox.Max);
            List<Curve> crvs = new List<Curve>();
            crvs.Add(rect.ToNurbsCurve());
            Brep brep = Brep.CreatePlanarBreps(rect.ToNurbsCurve())[0];
            var temp1 = brep.Faces[0].Split(segs, 0.1).Faces;
            List<Brep> temp2 = new List<Brep>();
            foreach (BrepFace bf in temp1)
            {
                temp2.Add(bf.DuplicateFace(false));
            }
            

            //get edge polylines
            List <Polyline> edges = new List<Polyline>();
            List<Rectangle3d> rects = new List<Rectangle3d>();
            foreach (Brep b in temp2)
            {
                Polyline polyTemp;
                Curve.JoinCurves(b.DuplicateEdgeCurves().ToList(), 1)[0].TryGetPolyline(out polyTemp);

                bool isRoad = false;
                for (int i = 0; i < roadPoints.Count; i++)
                {
                    if (polyTemp.ToNurbsCurve().Contains(roadPoints[i]) != Rhino.Geometry.PointContainment.Outside)
                    {

                        isRoad = true;
                        break;
                    }
                }

                if (!isRoad && boundary.Contains(polyTemp.CenterPoint()) == Rhino.Geometry.PointContainment.Outside)
                {
                    edges.Add(polyTemp);
                }
            }

            //create building mass
            Random rand = new Random(segs.Count);
            List<Brep> buildings = new List<Brep>();
            double pil = 3300;
            double flr = 2800;

            foreach (Polyline c in edges)
            {
                Curve cTemp = c.ToNurbsCurve();
                Curve[] temp = cTemp.Offset(Plane.WorldXY, -5000, 1, CurveOffsetCornerStyle.Sharp);
                if (temp != null && temp.Length != 0)
                {


                    cTemp = temp[0];
                    double _scalefactor = 1.7 * scalefactor;
                    Curve[] temptemp = cTemp.Offset(Plane.WorldXY, _scalefactor, 1, CurveOffsetCornerStyle.Sharp);





                    //try
                    //{
                    //    temptemp[0] = Rhino.RhinoDoc.ActiveDoc.Objects.ElementAt(0).Geometry as Curve;
                    //}
                    //catch (NullReferenceException e)
                    //{
                    //    Rhino.RhinoApp.WriteLine(e.Message);
                    //    return null;
                    //}



                    if (temptemp.Length != 0)
                    {
                        cTemp = temptemp[0];
                        if (cTemp.IsClosed == true)
                            buildings.Add(Surface.CreateExtrusion(cTemp, Vector3d.ZAxis * ((scalefactor / 1700) * pil + flr * rand.Next(3, 5))).ToBrep().CapPlanarHoles(1));
                    }

                }
            }



            Rhino.RhinoDoc.ActiveDoc.Layers.SetCurrentLayerIndex(Rhino.RhinoDoc.ActiveDoc.Layers.Find("Default", true), true);
            return buildings;
        }
        public List<Brep> KDGmaker(List<Curve> segs, Curve boundary)
        {
            //split surface
            List<Point3d> bboxPts = new List<Point3d>();
            foreach (Curve c in segs)
            {
                bboxPts.Add(c.PointAtStart);
                bboxPts.Add(c.PointAtEnd);
            }
            var bbox = new BoundingBox(bboxPts);
            Rectangle3d rect = new Rectangle3d(Plane.WorldXY, bbox.Min, bbox.Max);
            List<Curve> crvs = new List<Curve>();
            crvs.Add(rect.ToNurbsCurve());
            Brep brep = Brep.CreatePlanarBreps(rect.ToNurbsCurve())[0];
            var temp1 = brep.Faces[0].Split(segs, 0.1).Faces;
            List<Brep> temp2 = new List<Brep>();
            foreach (BrepFace bf in temp1)
            {
                temp2.Add(bf.DuplicateFace(false));
            }

            //get edge polylines
            List<Polyline> edges = new List<Polyline>();
            List<Rectangle3d> rects = new List<Rectangle3d>();
            foreach (Brep b in temp2)
            {
                Polyline polyTemp;
                Curve.JoinCurves(b.DuplicateEdgeCurves().ToList(), 1)[0].TryGetPolyline(out polyTemp);

                Rectangle3d rectTemp = outerRectFinder(polyTemp);

                double ratio = rectTemp.Height / rectTemp.Width;
                if (ratio < 1)
                    ratio = 1 / ratio;

                if (rectTemp.Area * 0.51 < getArea(polyTemp) && ratio < 1.5 && boundary.Contains(polyTemp.CenterPoint()) == Rhino.Geometry.PointContainment.Outside)
                {
                    edges.Add(polyTemp);
                    rects.Add(rectTemp);
                }

            }


            //create building mass
            List<Brep> buildings = new List<Brep>();

            foreach (Polyline c in edges)
            {
                Curve cTemp = c.ToNurbsCurve();
                cTemp.Transform(Transform.Translation(-new Vector3d(c.CenterPoint())));
                cTemp.Scale(0.8);
                cTemp.Transform(Transform.Translation(new Vector3d(c.CenterPoint())));
                buildings.Add(Surface.CreateExtrusion(cTemp, Vector3d.ZAxis * 20000).ToBrep().CapPlanarHoles(1));
            }
            //Rhino.RhinoApp.WriteLine(" temp2 = " + temp2.Count + "edges = " + edges.Count + " buildings = " + buildings.Count);
            return buildings;
        }
        // use
        public List<Brep> KDGmaker(List<Curve> segs, Curve boundary, List<Point3d> roadPoints, Brep ground)
        {
            double depth = 3000;
           
            var groundMesh = Mesh.CreateFromBrep(ground);


            //long mem1 = GC.GetTotalMemory(false);
            //Rhino.RhinoApp.WriteLine("init : {0}", mem1);
            //split surface
            List<Point3d> bboxPts = new List<Point3d>();
            foreach (Curve c in segs)
            {
                bboxPts.Add(c.PointAtStart);
                bboxPts.Add(c.PointAtEnd);
            }
            var bbox = new BoundingBox(bboxPts);
            Rectangle3d rect = new Rectangle3d(Plane.WorldXY, bbox.Min, bbox.Max);
            List<Curve> crvs = new List<Curve>();
            crvs.Add(rect.ToNurbsCurve());

            Brep brep = Brep.CreatePlanarBreps(rect.ToNurbsCurve())[0];


           // mem1 = GC.GetTotalMemory(false);
            //Rhino.RhinoApp.WriteLine("BeforeSplit : {0}", mem1);


            var temp1 = brep.Faces[0].Split(segs, 0.1).Faces;
            //mem1 = GC.GetTotalMemory(false);
            //Rhino.RhinoApp.WriteLine("AfterSplit : {0}", mem1);
            List<Brep> temp2 = new List<Brep>();
            foreach (BrepFace bf in temp1)
            {
                temp2.Add(bf.DuplicateFace(false));
            }


            List<Curve> groundsplitcurve = new List<Curve>();

            //get edge polylines
            List<Polyline> edges = new List<Polyline>();
            List<Polyline> edgesRoad = new List<Polyline>();
            List<Polyline> edgesPlot = new List<Polyline>();
            List<Rectangle3d> rects = new List<Rectangle3d>();

            //mem1 = GC.GetTotalMemory(false);
            //Rhino.RhinoApp.WriteLine("BeforeD : {0}", mem1);
            foreach (Brep b in temp2)
            {
                Polyline polyTemp;
                Curve.JoinCurves(b.DuplicateEdgeCurves().ToList(), 1)[0].TryGetPolyline(out polyTemp);
               
                bool isRoad = false;
                for (int i = 0; i < roadPoints.Count; i++)
                {
                    if (polyTemp.ToNurbsCurve().Contains(roadPoints[i]) != Rhino.Geometry.PointContainment.Outside)
                    {
                        isRoad = true;
                        break;
                    }

                    
                }

                if (!isRoad && boundary.Contains(polyTemp.CenterPoint()) == Rhino.Geometry.PointContainment.Outside)
                    edges.Add(polyTemp);
                else if (isRoad)
                {
                    edgesRoad.Add(polyTemp);
                   
                }
                else
                {
                    edgesPlot.Add(polyTemp);
                   
                }

            }
           // mem1 = GC.GetTotalMemory(false);
            //Rhino.RhinoApp.WriteLine("AfterD : {0}", mem1);


            //create plot ground
            List<double> groundHeight = new List<double>();
            List<Brep> grounds = new List<Brep>();
            foreach (Polyline c in edgesPlot)
            {
                List<Point3d> ptl = new List<Point3d>();
                List<Brep> bl = new List<Brep>();
                ptl.Add(c.CenterPoint());
                bl.Add(ground);

                //LoadManager.getInstance().DrawObjectWithSpecificLayer(bl, LoadManager.NamedLayer.Guide);
                //Rhino.RhinoDoc.ActiveDoc.Views.ActiveView.Redraw();

                Point3d projectedPoint = Rhino.Geometry.Intersect.Intersection.ProjectPointsToMeshes(groundMesh, ptl, Vector3d.ZAxis, 0)[0];
                groundHeight.Add(projectedPoint.Z);

                //Brep groundPiece = Brep.CreatePlanarBreps(c.ToNurbsCurve())[0];
                Brep groundPiece = Surface.CreateExtrusion(c.ToNurbsCurve(), Vector3d.ZAxis * (-depth)).ToBrep().CapPlanarHoles(1);
                grounds.Add(groundPiece);
            }
            double averageHeight = groundHeight.Sum() / groundHeight.Count;

            //create building mass and ground
            Random rand = new Random(segs.Count);
            List<Brep> buildings = new List<Brep>();

            double pil = Consts.PilotiHeight;
            double flr = Consts.FloorHeight;



            //mem1 = GC.GetTotalMemory(false);
            //Rhino.RhinoApp.WriteLine("BeforeEdge : {0}", mem1);
            foreach (Polyline c in edges)
            {
                if(getArea(c)>1)
                { 
                    List<Point3d> ptl = new List<Point3d>();
                    List<Brep> bl = new List<Brep>();
                    ptl.Add(c.CenterPoint());
                    bl.Add(ground);
                    Point3d projectedPoint;

                    //LoadManager.getInstance().DrawObjectWithSpecificLayer(bl, LoadManager.NamedLayer.Guide);
                    //Rhino.RhinoDoc.ActiveDoc.Views.ActiveView.Redraw();

                    //dtsasdfasdf
                    
                        projectedPoint = Rhino.Geometry.Intersect.Intersection.ProjectPointsToMeshes(groundMesh, ptl, Vector3d.ZAxis, 0)[0];

                    //~dtsasdfasdf
                    //Brep groundPiece = Brep.CreatePlanarBreps(c.ToNurbsCurve())[0];
                    Brep groundPiece = Surface.CreateExtrusion(c.ToNurbsCurve(), Vector3d.ZAxis * (-depth)).ToBrep().CapPlanarHoles(1);
                    if (groundPiece == null)
                        continue;
                    groundPiece.Translate(Vector3d.ZAxis * (projectedPoint.Z - averageHeight));
                    grounds.Add(groundPiece);

                    Curve cTemp = c.ToNurbsCurve();
                    Curve[] temp = cTemp.Offset(Plane.WorldXY, -3002, 1, CurveOffsetCornerStyle.Sharp);
                    if (temp != null && temp.Length != 0)
                    {
                        cTemp = temp[0];
                        Curve[] temptemp = cTemp.Offset(Plane.WorldXY, 2, 1, CurveOffsetCornerStyle.Sharp);
                        if (temptemp.Length != 0)
                        {
                            cTemp = temptemp[0];
                            Polyline pTemp;
                            cTemp.TryGetPolyline(out pTemp);

                            if (getArea(pTemp) < getArea(c) * 0.6)
                            {
                                Curve[] temptemptemp = cTemp.Offset(Plane.WorldXY, 1000, 1, CurveOffsetCornerStyle.Sharp);
                                if (temptemptemp != null)
                                    cTemp = temptemptemp[0];
                            }
                            if (cTemp.IsClosed == true && cTemp.GetBoundingBox(false).Diagonal.Length < bbox.Diagonal.Length)
                            {
                                Brep tempBrep = Surface.CreateExtrusion(cTemp, Vector3d.ZAxis * (pil + flr * rand.Next(3, 5))).ToBrep().CapPlanarHoles(1);
                                tempBrep.Translate(Vector3d.ZAxis * (projectedPoint.Z - averageHeight));
                                buildings.Add(tempBrep);
                            }

                        }

                    }

                }
            }

            //mem1 = GC.GetTotalMemory(false);
           // Rhino.RhinoApp.WriteLine("AfterEdge : {0}", mem1);

            List<Curve> projectedRoads = new List<Curve>();

            //mem1 = GC.GetTotalMemory(false);
            //Rhino.RhinoApp.WriteLine("BeforeRoadEdge : {0}", mem1);

            foreach (Polyline c in edgesRoad)
            {
                Curve road1 = Curve.ProjectToMesh(c.ToNurbsCurve(), groundMesh, Vector3d.ZAxis, 0.01)[0];
                List<Curve> roadl = new List<Curve>();
                roadl.Add(road1);

                //LoadManager.getInstance().DrawObjectWithSpecificLayer(ground, LoadManager.NamedLayer.Model);
                
                //Curve road2 = Curve.ProjectToMesh(c.ToNurbsCurve(), Mesh.CreateFromBrep(ground)[0], Vector3d.ZAxis, 0)[0];
                road1.Translate(Vector3d.ZAxis * (-averageHeight));
                Brep groundPiece = Surface.CreateExtrusion(road1.ToNurbsCurve(), Vector3d.ZAxis * (-depth)).ToBrep().CapPlanarHoles(1);
                Guid guid = LoadManager.getInstance().DrawObjectWithSpecificLayer(road1, LoadManager.NamedLayer.ETC);
                
                //Rhino.RhinoDoc.ActiveDoc.Objects.Select(guid,true,true);
                //Rhino.RhinoApp.RunScript("_-Patch '_Enter", true);

                //Rhino.RhinoDoc.ActiveDoc.Objects.Delete(guid, true);
                
                //LoadManager.getInstance().DrawObjectWithSpecificLayer(temp, LoadManager.NamedLayer.Guide);
                grounds.Add(groundPiece);

                projectedRoads.Add(road1);
            }

            //mem1 = GC.GetTotalMemory(false);
            //Rhino.RhinoApp.WriteLine("AfterRoadEdge : {0}", mem1);


            List<Mesh> projectedRoadMesh = GetRoadPatch(projectedRoads, groundMesh[0]);
            
            for (int i = 0; i < projectedRoadMesh.Count; i++)
            {
                if (projectedRoadMesh[i] != null)
                    LoadManager.getInstance().DrawObjectWithSpecificLayer(projectedRoadMesh[i], LoadManager.NamedLayer.ETC);
               
            }
            

            Point3d test = new Point3d(1, 1, 1);

            
            
            //test
            buildings.AddRange(grounds);

            return buildings;
        }


        private List<Mesh> GetRoadPatch(List<Curve> projectedRoads, Mesh groundMesh)
        {
            List<Mesh> result = new List<Mesh>();
            foreach (var c in projectedRoads)
            {
                Polyline p = null;
                c.TryGetPolyline(out p);
                Mesh tempResult = Mesh.CreateFromClosedPolyline(p);
                result.Add(tempResult);
            }

            return result;
        }

        private Rectangle3d outerRectFinder(Polyline convexHull)
        {
            double solAngle = 0;
            double solArea = double.MaxValue;
            Line[] edges = convexHull.GetSegments();
            for (int i = 0; i < edges.Length; i++)
            {
                double tempAngle = Vector3d.VectorAngle(edges[i].UnitTangent, Vector3d.XAxis) * Math.Sign(Vector3d.CrossProduct(edges[i].UnitTangent, Vector3d.XAxis).Z);
                Polyline polylineClone = new Polyline(convexHull);
                polylineClone.Transform(Transform.Rotation(tempAngle, Point3d.Origin));
                BoundingBox tempBox = polylineClone.BoundingBox;
                double tempArea = tempBox.Diagonal.X * tempBox.Diagonal.Y;
                if (tempArea < solArea)
                {
                    solArea = tempArea;
                    solAngle = tempAngle;
                }
            }

            Polyline polyClone = new Polyline(convexHull);
            polyClone.Transform(Transform.Rotation(solAngle, Point3d.Origin));
            BoundingBox solBox = polyClone.BoundingBox;
            Rectangle3d solRect = new Rectangle3d(new Plane(solBox.Min, Vector3d.ZAxis), solBox.Max.X - solBox.Min.X, solBox.Max.Y - solBox.Min.Y);
            solRect.Transform(Transform.Rotation(-solAngle, Point3d.Origin));

            return solRect;
        }

        private double getArea(Polyline plotPolyline)
        {
            List<Point3d> y = new List<Point3d>(plotPolyline);
            double area = 0;

            for (int i = 0; i < y.Count - 1; i++)
            {
                area += y[i].X * y[i + 1].Y;
                area -= y[i].Y * y[i + 1].X;
            }

            area /= 2;

            return (area < 0 ? -area : area);
        }

        /////////////////////////////////

        public Vector3d normalDirection(List<Point3d> groundPoints, Polyline outline, out double slopeRatio, out Brep ground)
        {
            //create patch

            double MCminZ = 0;

            
            var outrectsegs = TuringAndCorbusierPlugIn.InstanceClass.kdgInfoSet.outrect.DuplicateSegments();
            foreach (var seg in outrectsegs)
            {
                groundPoints.Add(seg.PointAtStart);
                groundPoints.Add(seg.PointAtEnd);
            }


            foreach (Point3d pointz in groundPoints)
            {
                if (pointz.Z < MCminZ)
                    MCminZ = pointz.Z;
            }

            BoundingBox bb = new BoundingBox(groundPoints);

            var dgnl = bb.Diagonal;
            var cp = bb.Center;
            Vector3d dgnl_shadow = new Vector3d(dgnl.X, dgnl.Y, 0);

            List<Point3d> external_points = new List<Point3d>();

            Point3d p1 = new Point3d(cp) + dgnl_shadow;
            Point3d p2 = new Point3d(cp) - dgnl_shadow;

            Vector3d dgnl_shadow_perp = new Vector3d();

            var zz = dgnl_shadow_perp.PerpendicularTo(dgnl_shadow);

            Point3d p3 = new Point3d(cp) - dgnl_shadow_perp;
            Point3d p4 = new Point3d(cp) + dgnl_shadow_perp;


            external_points.Add(p1);
            external_points.Add(p2);
            external_points.Add(p3);
            external_points.Add(p4);

            List<Rhino.Geometry.Point> pts = groundPoints.Select(n => new Rhino.Geometry.Point(n)).ToList();

            pts.AddRange(external_points.Select(n => new Rhino.Geometry.Point(n)).ToList());

            Brep patch = Brep.CreatePatch(pts, 30, 30, 1);

            Surface surface = patch.Surfaces[0];

            //Rhino.RhinoDoc.ActiveDoc.Objects.AddBrep(patch);

            //evaluate surface & extract x and y from average normal
            int resolution = 10;
            double xDir = 0;
            double yDir = 0;
            for (int i = 0; i < resolution + 1; i++)
            {
                for (int j = 0; j < resolution + 1; j++)
                {
                    Point3d point;
                    Vector3d[] derivatives;
                    surface.Evaluate((double)i / (double)resolution, (double)j / (double)resolution, 1, out point, out derivatives);

                    Vector3d normal = Vector3d.CrossProduct(derivatives[0], derivatives[1]);
                    normal.Unitize();
                    xDir += normal.X;
                    yDir += normal.Y;
                }
            }

            //unit vector of average normal direction
            Vector3d avrgNormal = new Vector3d(xDir, yDir, 0);
            avrgNormal.Unitize();

            ////create line along average normal vector from polyline center, and get longest intersecting segment
            //Point3d center = outline.CenterPoint();
            //Point3d lineEnd = new Point3d(center);
            //lineEnd.Transform(Transform.Translation(-outline.BoundingBox.Diagonal.Length * avrgNormal));



            //Line dirLine = new Line(lineEnd, outline.BoundingBox.Diagonal.Length * avrgNormal * 2);
            //Rhino.RhinoDoc.ActiveDoc.Objects.AddLine(dirLine);

            //Rhino.RhinoDoc.ActiveDoc.Objects.AddPolyline(outline);

            //var lxp = Rhino.Geometry.Intersect.Intersection.CurveCurve(dirLine.ToNurbsCurve(), outline.ToNurbsCurve(), 0, 0);

            ////get end points of segment and project onto the patch
            //Point3d pt1 = lxp[0].PointA;
            //Point3d pt2 = lxp[lxp.Count - 1].PointA;

            //double moveL = patch.GetBoundingBox(false).Diagonal.Length;
            //pt1.Transform(Transform.Translation(-moveL * Vector3d.ZAxis));
            //pt2.Transform(Transform.Translation(-moveL * Vector3d.ZAxis));

            //List<Brep> brep = new List<Brep>();
            //brep.Add(patch);
            //List<Point3d> twoPts = new List<Point3d>();
            //twoPts.Add(pt1);
            //twoPts.Add(pt2);

            //Point3d ptpt1 = Rhino.Geometry.Intersect.Intersection.ProjectPointsToBreps(brep, twoPts, Vector3d.ZAxis, 0)[0];
            //Point3d ptpt2 = Rhino.Geometry.Intersect.Intersection.ProjectPointsToBreps(brep, twoPts, Vector3d.ZAxis, 0)[1];

            slopeRatio = 0;

            //create line from projected points and calculate slope ratio
            //Line slopeLine = new Line(ptpt1, ptpt2);
            //slopeRatio = slopeLine.Direction.Z / Math.Pow(Math.Pow(slopeLine.Direction.X, 2) + Math.Pow(slopeLine.Direction.Y, 2), 0.5);

            ground = patch;
            return avrgNormal;

        }
    }
    }