using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rhino;
using Rhino.Geometry;

namespace TuringAndCorbusier.Utility
{
    public class Isothetic
    {
        public Polyline Outline { get; set; }
        public Point3d BasePt { get; set; }
        public Line WidthLine { get; set; }
        public Line HeightLine { get; set; }

        public Isothetic()
        {
            this.Outline = new Polyline();
            this.BasePt = new Point3d();
            this.WidthLine = new Line();
            this.HeightLine = new Line();
        }

        public Isothetic(Isothetic otherIso)
        {
            this.Outline = new Polyline(otherIso.Outline);
            this.BasePt = otherIso.BasePt;
            this.WidthLine = otherIso.WidthLine;
            this.HeightLine = otherIso.HeightLine;
        }

        public Isothetic(Polyline outline, Point3d basePt, Line widthLine, Line heightLine)
        {
            this.Outline = outline;
            this.BasePt = basePt;
            this.WidthLine = widthLine;
            this.HeightLine = heightLine;
        }
    }

    partial class InnerIsoDrawer
    {
        private class InitialPt
        {
            public Vector3d AlignGuide { get; set; }
            public Vector3d PerpGuide { get; set; }
            public Point3d Pt { get; set; }
            public Line MainAxis { get; set; }
            public Line SubAxis { get; set; }
            public double PotentialMax
            {
                get { return MainAxis.Length * SubAxis.Length; }
                private set { }
            } 

            public InitialPt(Point3d basePt, Vector3d alignVec, Vector3d perpVec)
            {
                this.Pt = basePt;

                this.AlignGuide = alignVec;
                this.AlignGuide.Unitize();

                this.PerpGuide = perpVec;
                this.PerpGuide.Unitize();
            }

            //main
            public void SetMainAxis(Polyline boundary)
            {
                Line alignMain = SetEachMainAxis(AlignGuide, boundary);
                Line perpMain = PCXTools.PCXLongest(Pt, boundary, PerpGuide);

                if (alignMain.Length > perpMain.Length)
                {
                    MainAxis = alignMain;
                    SubAxis = perpMain;
                }

                else
                {
                    MainAxis = perpMain;
                    SubAxis = alignMain;
                }
            }

            public void SetMainAxisForStrict(Polyline boundary)
            {
                Curve boundCrv = boundary.ToNurbsCurve();

                Line alignMain = SetEachMainAxis(AlignGuide, boundary);
                Line perpMain = SetEachMainAxis(PerpGuide, boundary);

                if (alignMain.Length > perpMain.Length)
                {
                    MainAxis = alignMain;
                    SubAxis = perpMain;
                }

                else
                {
                    MainAxis = perpMain;
                    SubAxis = alignMain;
                }
            }

            //sub
            private Line SetEachMainAxis(Vector3d guide, Polyline bound)
            {
                Line axis1 = PCXTools.PCXLongest(Pt, bound, guide);
                Line axis2 = PCXTools.PCXLongest(Pt, bound, -guide);

                bool isAxis1Longer = axis1.Length >= axis2.Length;
                bool isAxis1Inside = IsLineInside(axis1, bound);
                bool isAxis2Inside = IsLineInside(axis2, bound);

                if (isAxis1Inside)
                {
                    if (!isAxis2Inside)
                        return axis1;

                    if (isAxis1Longer)
                        return axis1;

                    return axis2;
                }

                if (isAxis2Inside)
                    return axis2;

                return new Line(Pt,Pt);
            }

            private bool IsLineInside(Line testLine, Polyline bound)
            {
                Point3d testPt = testLine.From + testLine.UnitTangent;
                if (bound.ToNurbsCurve().Contains(testPt) == PointContainment.Outside)
                    return false;

                if (testLine.Length == 0)
                    return false;

                return true;
            }
        }

        private class IndexTagSegment
        {
            public Line Line { get; private set; }
            public int FromIndex { get; private set; }
            public int ToIndex { get; private set; }

            public IndexTagSegment(Line segment, int index)
            {
                this.Line = segment;
                this.FromIndex = index;
                this.ToIndex = index + 1;
            }
        }

        private class AngleTagVertex
        {
            public Point3d Pt { get; private set; }

            public Line ToPre { get; private set; }
            public Line ToPost { get; private set; }
            public Vector3d PreVec { get { return ToPre.UnitTangent; } private set { } }
            public Vector3d PostVec { get { return ToPost.UnitTangent; } private set { } }

            public AngleType AngleType { get; private set; }

            //constructor
            public AngleTagVertex(Point3d basePt, Line preline, Line postLine)
            {
                this.Pt = basePt;
                this.ToPre = preline;
                this.ToPost = postLine;
                this.AngleType = VectorTools.CheckAngleType(PreVec, PostVec, 0.0005);
            }
        }

        private class IsoBlock
        {
            //innate
            public Point3d BasePt { get; private set; }
            public Line WidthLine { get; private set; }
            public Line HeightLine { get; private set; }
            private Vector3d widthVec { get; set; }
            private Vector3d heightVec { get; set; }
            private double aspectRatio;
            private double width = 0;
            private double height = 0;

            //extrinsic
            public double Width { get { return width; } set { width = value; } }
            public double Height { get { return height; } set { height = value; } }
            public double Area { get { return Width * Height; } private set { } }


            public IsoBlock(InitialPt ip, double ar)
            {
                this.BasePt = ip.Pt;
                this.WidthLine = ip.MainAxis;
                this.widthVec = WidthLine.UnitTangent;
                this.HeightLine = ip.SubAxis;
                this.heightVec = HeightLine.UnitTangent;
                this.aspectRatio = ar;
            }

            //main
            public void Inflate(Polyline boundary, Curve boundCrv, double minLength, bool applyMinToUpper)
            {
                bool isInsideBound = false;
                double tempLoBound = minLength;
                double tempUpBound = WidthLine.Length;

                if (applyMinToUpper)
                    tempUpBound -= minLength;

                if (HeightLine.Length < 0.5 || WidthLine.Length < 0.5) //start seive: zeroLine
                    return;
                if (tempUpBound < minLength) //start seive: initial width < minLength
                    return;

                double loopBreaker = 0;

                while (tempLoBound <= tempUpBound)
                {
                    if (loopBreaker == 10)
                        break;

                    loopBreaker++;

                    double tempWidth = (tempLoBound + tempUpBound) / 2;
                    double tempHeight = tempWidth / aspectRatio;

                    if (tempWidth < minLength) //mid seive
                    {
                        tempLoBound = minLength;
                        continue;
                    }

                    //counter-clockwise
                    Point3d p1 = BasePt;
                    Point3d p2 = BasePt + widthVec * tempWidth;
                    Point3d p3 = BasePt + widthVec * tempWidth + heightVec * tempHeight;
                    Point3d p4 = BasePt + heightVec * tempHeight;

                    Polyline tempBlock = new Polyline { p1, p2, p3, p4, p1 };

                    if (boundCrv.Contains(p3) == PointContainment.Outside)
                    {
                        tempUpBound = tempWidth;
                        continue;
                    }

                    if (CCXTools.IsIntersectCrv(tempBlock, boundary, false))
                        tempUpBound = tempWidth;

                    else
                    {
                        isInsideBound = true;
                        tempLoBound = tempWidth;
                    }
                }

                if (!isInsideBound) //end seive: intersection
                    return;

                if (tempLoBound / aspectRatio < minLength) //end seive: height
                    return;

                Width = tempLoBound;
                Height = tempLoBound / aspectRatio;
            }

            public InitialPt NextInitialPt()
            {
                Point3d nextBase = BasePt + heightVec * Height;
                InitialPt nextInit = new InitialPt(nextBase, widthVec, heightVec);

                nextInit.MainAxis = new Line(nextBase, nextBase + WidthLine.UnitTangent * Width);
                nextInit.SubAxis = new Line(nextBase, HeightLine.To);
                Line subCandidate = nextInit.SubAxis;

                if (subCandidate.Length !=0 && subCandidate.UnitTangent == -heightVec)
                    nextInit.SubAxis = new Line(nextBase, nextBase);

                return nextInit;
            }
        }
    }
}
