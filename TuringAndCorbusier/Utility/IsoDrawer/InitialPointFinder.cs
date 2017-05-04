using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rhino;
using Rhino.Geometry;


namespace TuringAndCorbusier.Utility
{
    partial class InnerIsoDrawer
    {
        private class InitialPointFinder
        {
            //field
            private List<InitialPt> initialPts = new List<InitialPt>();
            private List<AngleTagVertex> atVertices = new List<AngleTagVertex>();
            List<IndexTagSegment> itSegments = new List<IndexTagSegment>();
            private Polyline boundary = new Polyline();
            private Curve boundCrv;
            private double minLength = 0;

            //constructor
            public InitialPointFinder(Polyline boundary, double minLength)
            {
                this.boundary = boundary;
                this.boundCrv = boundary.ToNurbsCurve();
                this.minLength = minLength;
                SetSegmentByLength();
                TagAngleTypeOnVertices();
            }

            //main
            public List<InitialPt> Search()
            {             
                AddMidPtWhenTriangle();
                AddLongObtuseEdgePt();
                AddFootOfPerp();

                return initialPts;
            }


            //initial setting
            private void SetSegmentByLength()
            {
                Line[] pureSegment = boundary.GetSegments();              
                for (int i = 0; i < pureSegment.Count(); i++)
                    itSegments.Add(new IndexTagSegment(pureSegment[i], i));

                itSegments.Sort((a, b) => -a.Line.Length.CompareTo(b.Line.Length));
            }

            private void TagAngleTypeOnVertices()
            {
                List<Point3d> vertices = new List<Point3d>(boundary);
                if (boundary.IsClosed)
                    vertices.RemoveAt(vertices.Count - 1);

                for (int i = 0; i < vertices.Count; i++)
                {
                    Line toPre = new Line(vertices[i], vertices[(vertices.Count + i - 1) % vertices.Count]);
                    Line toPost = new Line(vertices[i], vertices[(vertices.Count + i + 1) % vertices.Count]);
                    this.atVertices.Add(new AngleTagVertex(vertices[i], toPre, toPost));
                }
            }


            //point adding
            private void AddMidPtWhenTriangle()
            {
                if (itSegments.Count != 3)
                    return;

                List<InitialPt> triangleInit = new List<InitialPt>();

                for (int i = 0; i < itSegments.Count; i++)
                {
                    Vector3d perpFromSegment = itSegments[i].Line.UnitTangent;
                    perpFromSegment.Rotate(Math.PI / 2, Vector3d.ZAxis);

                    for(int j=0; j< itSegments.Count; j++)
                    {
                        if (i == j)
                            continue;

                        Line pcxLine = PCXTools.PCXStrict(itSegments[j].Line.PointAt(0.5), boundary, -perpFromSegment);
                        Point3d pointToSeg = pcxLine.To;

                        if (pcxLine.Length == 0 || !PCXTools.IsPtOnLine(pointToSeg, itSegments[i].Line, 0.5))
                            continue;

                        Vector3d perpVec = pcxLine.From - pcxLine.To;
                        initialPts.Add(new InitialPt(pointToSeg, itSegments[i].Line.UnitTangent, perpVec));
                       
                    }
                }
            }

            private void AddLongObtuseEdgePt()
            {
                List<InitialPt> obtuseInit = new List<InitialPt>();

                int loopBreaker = 0;

                for (int i = 0; i < itSegments.Count(); i++)
                {
                    if (loopBreaker > 3)
                        break;

                    IndexTagSegment current = itSegments[i];
                    AngleTagVertex fromVrtx = atVertices[current.FromIndex];
                    AngleTagVertex toVrtx = atVertices[current.ToIndex% atVertices.Count];

                    if (fromVrtx.AngleType == AngleType.Obtuse)
                    {
                        Vector3d maxAlignVec = fromVrtx.PostVec;
                        Vector3d maxPerpVec = maxAlignVec;
                        maxPerpVec.Rotate(Math.PI / 2, Vector3d.ZAxis);

                        initialPts.Add(new InitialPt(fromVrtx.Pt, maxAlignVec, maxPerpVec));
                    }

                    if (toVrtx.AngleType == AngleType.Obtuse)
                    {
                        Vector3d maxAlignVec = toVrtx.PreVec;
                        Vector3d maxPerpVec = maxAlignVec;
                        maxPerpVec.Rotate(-Math.PI / 2, Vector3d.ZAxis);

                        initialPts.Add(new InitialPt(toVrtx.Pt, maxAlignVec, maxPerpVec));
                    }

                    loopBreaker++;
                }

                initialPts.AddRange(obtuseInit);
            }

            private void AddFootOfPerp()
            {   
                int addedBaseSegCount = 0;

                for (int i = 0; i < itSegments.Count(); i++)
                {
                    List<InitialPt> fopInit = new List<InitialPt>();

                    //set
                    Line baseSeg = itSegments[i].Line;
                    Vector3d perpFromSegment = baseSeg.UnitTangent;
                    perpFromSegment.Rotate(Math.PI / 2, Vector3d.ZAxis);

                    //seive: count
                    if (addedBaseSegCount > 5)
                        break;

                    //seive: minimum length
                    if (baseSeg.Length < minLength)
                        continue;

              
                    for (int j = 0; j < atVertices.Count(); j++)
                    {
                        AngleTagVertex currentV = atVertices[j];                        

                        Vector3d testVec = currentV.Pt - baseSeg.PointAt(0);
                        bool isDexter = perpFromSegment * testVec > 0;
                        bool isUndecidable = Math.Abs(perpFromSegment * testVec) < 0.005;
                        if (isDexter && !isUndecidable)
                            initialPts.AddRange(FindFootOfPerp(baseSeg, -perpFromSegment, currentV, false));

                        else if(!isUndecidable)
                            initialPts.AddRange(FindFootOfPerp(baseSeg, perpFromSegment, currentV, true));
                    }

                    addedBaseSegCount++;
                }
            }

            private List<InitialPt> FindFootOfPerp(Line perpBase, Vector3d perpDirection, AngleTagVertex perpStart, bool perpInvert)
            {
                List<InitialPt> footOfPerps = new List<InitialPt>();

                if (!VectorTools.IsBetweenVector(perpStart.PreVec, perpStart.PostVec, perpDirection))
                    return footOfPerps;

                Line crossLay = new Line(perpStart.Pt, perpStart.Pt + perpDirection);
                Point3d crossOnBase = CCXTools.GetCrossPt(perpBase, crossLay);
                if (crossOnBase == Point3d.Unset || boundCrv.Contains(crossOnBase) == PointContainment.Outside)
                    return footOfPerps;

                Polyline perpLinePoly = new Polyline { crossOnBase, perpStart.Pt };

                Vector3d perpVec = perpStart.Pt - crossOnBase;
                if (perpInvert)
                    perpVec = -perpVec;

                if (!CCXTools.IsIntersectCrv(perpLinePoly, boundary, false))
                    footOfPerps.Add(new InitialPt(crossOnBase, perpBase.UnitTangent, perpVec));

                return footOfPerps;
            }

        }
    }
}
