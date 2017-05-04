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
        //fields & properties
        private List<InitialPt> initialPoints = new List<InitialPt>();
        private Polyline boundary;
        private Curve boundCrv;
        private double minLength = 0;
        private List<double> aspectRatio = new List<double>();
        private int concaveCount = 0;
        private double maxAspectRatio = 3;
        private double ratioDividingCount = 9;
        private int candidateCount = 5;

        public int ConcaveCount { get { return concaveCount; } set { concaveCount = value; } }
        public double MaxAspectRatio{ get { return maxAspectRatio; } set { maxAspectRatio = value; } }
        public double RatioDividingCount { get { return ratioDividingCount; } set { ratioDividingCount = value; } }
        public int CandidateCount { get { return candidateCount; } set { candidateCount = value; } }
        

        //constructor
        public InnerIsoDrawer(Polyline boundary, double minLength, int concaveCount)
        {
            this.boundary = new Polyline(boundary);
            RefineBoundary();
            this.boundCrv = this.boundary.ToNurbsCurve();
            this.minLength = minLength;
            this.concaveCount = concaveCount;
            SetAspectRatio();
        }


        //main
        /// <summary>
        /// 기준점과 축을 찾아 최대내접 다각형을 그립니다.
        /// </summary>
        public Isothetic Draw()
        {
            SearchInitialPts();
            List<IsoBlock> maxBlocks = SearchGlobalMaxIso();

            if (maxBlocks.Count == 0)
                return null;

            Isothetic defaultIso = BakeIso(maxBlocks);
            return defaultIso;
        }

        /// <summary>
        /// 지정한 벡터를 좌표 평면 기준으로 해서 가장긴 PCX라인들을 축으로 잡습니다.
        /// </summary>
        public Isothetic DrawStrict(Point3d basePt, Vector3d axis, Vector3d perpAxis)
        {
            InitialPt strictInit = new InitialPt(basePt, axis, perpAxis);
            strictInit.SetMainAxisForStrict(boundary);

            List<IsoBlock> maxBlocks = SearchLocalMaxIso(strictInit, ConcaveCount, true);

            if (maxBlocks.Count == 0)
                return null;

            Isothetic strictIso = BakeIso(maxBlocks);
            return strictIso;
        }

        /// <summary>
        /// 지정한 라인을 축으로 기준점애서 최대내접 다각형을 그립니다.
        /// </summary>
        public Isothetic DrawStrict(Point3d basePt, Line longAxis, Line shortAxis)
        {
            InitialPt superInit = new InitialPt(basePt, longAxis.UnitTangent, shortAxis.UnitTangent);
            superInit.MainAxis = longAxis;
            superInit.SubAxis = shortAxis;

            List<IsoBlock> maxBlocks = SearchLocalMaxIso(superInit, ConcaveCount, true);

            if (maxBlocks.Count == 0)
                return null;

            Isothetic strictIso = BakeIso(maxBlocks);
            return strictIso;
        }


        //sub
        private void RefineBoundary()
        {
            PolylineTools.AlignCCW(boundary);
            PolylineTools.RemoveOnStraightPt(boundary, 0.005);
        }

        private void SetAspectRatio()
        {
            List<double> result = new List<double>();

            for (int i = 0; i < RatioDividingCount + 1; i++)
                result.Add((Math.Pow(MaxAspectRatio, i / RatioDividingCount)));

            aspectRatio = result;
        }

        private void SearchInitialPts()
        {
            InitialPointFinder ipf = new InitialPointFinder(boundary, minLength);
            initialPoints = ipf.Search();

            foreach (InitialPt i in initialPoints)
                i.SetMainAxis(boundary);

            initialPoints.Sort((a, b) => -a.PotentialMax.CompareTo(b.PotentialMax));
            initialPoints = initialPoints.Take(CandidateCount).ToList();
            
        }

        private List<IsoBlock> SearchGlobalMaxIso()
        {
            //output
            List<IsoBlock> bestBlocksG = new List<IsoBlock>();
            double bestAreaG = 0;

            if (initialPoints.Count == 0)
                return bestBlocksG;

            foreach (InitialPt i in initialPoints)
            {
                if ( i.PotentialMax <= bestAreaG)
                    continue;

                List<IsoBlock> bestBlockL = SearchLocalMaxIso(i, ConcaveCount, true);
                double bestAreaL = 0;

                if (bestBlockL.Count != 0)
                    bestBlockL.ForEach(n => bestAreaL += n.Area);

                if (bestAreaL > bestAreaG)
                {
                    bestAreaG = bestAreaL;
                    bestBlocksG = bestBlockL;
                }
            }

            return bestBlocksG;
        }

        private List<IsoBlock> SearchLocalMaxIso(InitialPt initial, int concaveCount, bool isInitialPhase)
        {

            List<IsoBlock> bestBlocksL = new List<IsoBlock>();
            double bestAreaL = 0;

            foreach(double ratio in aspectRatio)
            {
                List<IsoBlock> tempBlocks = new List<IsoBlock>();

                IsoBlock tempBlock = new IsoBlock(initial, ratio);
                tempBlock.Inflate(boundary, boundCrv, minLength, !isInitialPhase);

                if (tempBlock.Area <= 0)
                    continue;

                if (concaveCount == 0)
                    tempBlocks.Add(tempBlock);

                else
                {
                    tempBlocks.Add(tempBlock);
                    tempBlocks.AddRange(SearchLocalMaxIso(tempBlock.NextInitialPt(), concaveCount - 1,false));
                }

                double tempArea = 0;
                tempBlocks.ForEach(n => tempArea += n.Area);

                if (tempArea > bestAreaL)
                {
                    bestBlocksL = tempBlocks;
                    bestAreaL = tempArea;
                }
            }

            return bestBlocksL;
        }

        private Isothetic BakeIso(List<IsoBlock> blocks)
        {
            List<Point3d> isoVertex = new List<Point3d>();
            IsoBlock firstBlock = blocks.First();

            for (int i = 0; i < blocks.Count; i++)
            {
                IsoBlock tempBlock = blocks[i];
                Point3d basePt= tempBlock.BasePt;
                Vector3d widthVec = tempBlock.WidthLine.UnitTangent;
                Vector3d heightVec = tempBlock.HeightLine.UnitTangent;

                if(i == 0)
                    isoVertex.Add(basePt); //add corner1

                Point3d corner2 = basePt + widthVec * tempBlock.Width;
                Point3d corner3 = corner2 + heightVec * tempBlock.Height;
                isoVertex.Add(corner2);
                isoVertex.Add(corner3);

                if (i == blocks.Count - 1)
                {
                    isoVertex.Add(basePt + heightVec * tempBlock.Height); //add corner4
                    isoVertex.Add(isoVertex.First());
                }
            }

          
            Polyline bakedOutline =  new Polyline(isoVertex);
            PolylineTools.AlignCCW(bakedOutline);

            return new Isothetic(bakedOutline, firstBlock.BasePt, firstBlock.WidthLine, firstBlock.HeightLine);

        }
    }
}
