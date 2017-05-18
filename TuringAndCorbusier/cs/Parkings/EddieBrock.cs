using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rhino.Geometry;
using TuringAndCorbusier.Utility;
namespace TuringAndCorbusier
{
    //Eddie Brock - 1대 베놈
    //Peter Parker : Eddie Brock = Ground Parking : UnderGround Parking

    class UnderGroundParkingConsts
    {
        //from boundary
        public static double Clearance { get { return 800; } }

        //each floor
        public static double MinimumArea { get { return 500 * 1000 * 1000; } }

        // Area * this = count
        public static double ParkingPerArea { get { return (double)1 / (40 * 1000 * 1000); } }


        public static int InitialCount { get { return 10; } }


        // ramp 관련

        public static double OffsetFromRoad { get { return 1000; } }
        public static double OffsetFromOthers { get { return 800; } }
        public static double[] LinearRampWidth { get { return new double[] { 3300, 6000 }; } }
        public static double[] CurveRampWidth { get { return new double[] { 3600, 6500 }; } }
        public static double[] WithinRadius { get { return new double[] { 5000, 6000 }; } }
    }

    

    class ParkingDistributor
    {
        public static void Distribute(ref Apartment apartment /*ref ParkingLotOnEarth onEarth, ref ParkingLotUnderGround underGround*/)
        {

            int legalPark = apartment.GetLegalParkingLotofHousing();
            int tempPark = apartment.ParkingLotOnEarth.ParkingLines[0].Count();

            if (tempPark < legalPark)
            {
                int required = legalPark - tempPark;
                //최소 10대 규모
                if (required < 10)
                    required = 10;
                UnderGroundParkingModule upm = new UnderGroundParkingModule(apartment.Plot.Boundary, required);
                bool canUnderGroundParking = upm.CheckPrecondition();
                if (canUnderGroundParking)
                {
                    upm.Calculate();
                    ParkingLotUnderGround plug = new ParkingLotUnderGround((int)upm.EachFloorParkingCount * upm.Floors, upm.EachFloorArea * upm.Floors, upm.Floors);
                    apartment.ParkingLotUnderGround = plug;
                }
                else
                {
                    //beforeUnderGround;
                }
            }
            else
            {
                //beforeUnderGround;
            }
        }
    }

    class UnderGroundParkingModule
    {
        private Curve boundary;
        private Curve innerBoundary;
        private Curve isothetic;
        private int require;
        private int floors = 1;
        private double eachFloorParkingCount = 0;
        private double eachFloorArea = 0;


        public Curve Boundary { private get { return boundary; } set { boundary = value; } }
        public Curve InnerBoundary { private get { return innerBoundary; } set { innerBoundary = value; } }
        public Curve Isothetic { get { return isothetic; } set { isothetic = value; } }
        public int Require { private get { return require; } set { require = value; } }
        public int Floors { get { return floors; } set { floors = value; } }
        public double EachFloorParkingCount { get { return eachFloorParkingCount; } set { eachFloorParkingCount = value; } }
        public double EachFloorArea { get { return eachFloorArea; } set { eachFloorArea = value; } }

        private bool available = false;

        public UnderGroundParkingModule(Curve boundary, int require)
        {
            this.boundary = boundary;
            this.require = require;
            GetInnerBoundary();
            GetInnerIsothetic();

        }


        enum RampScale
        { Under50 = 0, Over50 = 1 }

        //ramp와 겹치는 지상 주차 제거

        //지상주차수 체크

        //목표주차 미달?

        //지하주차 추가

        // 지하주차 규모 체크

        // 지하주차 규모 초과?
        //새 램프
        // 지하주차 규모 충분
        //끝

        //목표주차 충분

        //끝

            //something changed = true
        public bool OverlapCheck(ref Apartment apt)
        {
            ParkingLotOnEarth gp = apt.ParkingLotOnEarth;
            ParkingLotUnderGround ugp = apt.ParkingLotUnderGround;

            if (gp.ParkingLines.Count <= 0)
                return false;

            Curve ramp = ugp.Ramp;
            if (ramp == null)
                return false;

            Stack<int> removeIndex = new Stack<int>();
            for (int i = 0; i < gp.ParkingLines[0].Count; i++)
            {
                var pccr = Curve.PlanarClosedCurveRelationship(ramp, gp.ParkingLines[0][i].Boundary.ToNurbsCurve(), Plane.WorldXY, 0);
                if (pccr != RegionContainment.Disjoint)
                {
                    removeIndex.Push(i);
                }
            }

            int overlapCount = removeIndex.Count;

            while (removeIndex.Count > 0)
            {
                int index = removeIndex.Pop();
                apt.ParkingLotOnEarth.ParkingLines[0].RemoveAt(index);
            }


            if (ugp.Count + overlapCount > 50 && ugp.Count <= 50)
            {
                //지하 50대 미만이었다가 초과하게되면? 다시생성
                this.require = ugp.Count + overlapCount;
                Calculate();
                apt.ParkingLotUnderGround = new ParkingLotUnderGround((int)eachFloorParkingCount * floors, eachFloorArea * floors, floors);
                return true;
            }

            else
            {
                //그밖의경우
                this.require = ugp.Count + overlapCount;
                Calculate();
                apt.ParkingLotUnderGround = new ParkingLotUnderGround((int)eachFloorParkingCount * floors, eachFloorArea * floors, floors);
                apt.ParkingLotUnderGround.Ramp = ramp;
                return false;
            }

        }

        public Curve DrawRamp(Plot plot, Vector3d lineAxis, List<Curve> obstacles)
        {
            //규모 판별 - 50 이상? 미만? 너비 3.3 or 6.5 , 내반경 5 or 6
            //innerboundary 기준 x (도로판별이힘듦)plot boundary 기준으로, '도로'에 '수직? 이어야 하나?' 인 '27m' 이상 길이 확보 가능? -> 직선 램프
            //불가능 -> 곡선램프 -> 시작 선, 도로 선, 장애물

            RampScale scale = RampScale.Under50;
            if (require > 50)
            {
                scale = RampScale.Over50;
            }

            //도로.
            Curve[] boundaries = plot.Boundary.DuplicateSegments();
            List<Curve> roads = new List<Curve>();
            for (int i = 0; i < boundaries.Length; i++)
            {
                if (plot.Surroundings[i] != 0)
                    roads.Add(boundaries[i]);
            }

            for (int i = 0; i < roads.Count; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    for (int l = 0; l < 100; l++)
                    {
                        double offsetTick = 100;//roads[i].GetLength() / 10;
                        if (l * offsetTick > roads[i].GetLength())
                            break;
                        Curve tempRamp = DrawLineRamp(roads[i], lineAxis, j, scale,l*offsetTick);
                        if (tempRamp == null)
                            continue;
                        bool collCheck = CollisionCheck(tempRamp, obstacles);
                        RegionContainment isInside = Curve.PlanarClosedCurveRelationship(tempRamp, innerBoundary, Plane.WorldXY, 0);
                        if (collCheck && isInside == RegionContainment.AInsideB)
                        {
                            return tempRamp;
                        }
                    }
                
                }
            }

            
            //for curvedramp
            for (int i = 0; i < roads.Count; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    for (int k = 0; k < 10; k++)
                    {
                        for (int l = 0; l < 100; l++)
                        {
                            double offsetTick = 100;//roads[i].GetLength() / 10;
                            if (l * offsetTick > roads[i].GetLength())
                                break;
                            double beforeRamp = 10000 - k * 1000;
                            Curve tempRamp = DrawCurvedRamp(roads[i], lineAxis, j, scale, beforeRamp, l*offsetTick);
                            if (tempRamp == null)
                                continue;
                            bool collCheck = CollisionCheck(tempRamp, obstacles);
                            RegionContainment isInside = Curve.PlanarClosedCurveRelationship(tempRamp, innerBoundary, Plane.WorldXY, 0);
                            if (collCheck && isInside == RegionContainment.AInsideB)
                            {
                                return tempRamp;
                            }
                        }
                    }
                }
            }

            //cant make ramp
            return null;
        }

        private bool CollisionCheck(Curve tempRamp, List<Curve> obstacles)
        {
            for (int i = 0; i < obstacles.Count; i++)
            {
                if (Curve.PlanarCurveCollision(tempRamp, obstacles[i], Plane.WorldXY, 0) 
                    || Curve.PlanarClosedCurveRelationship(obstacles[i],tempRamp,Plane.WorldXY,0) == RegionContainment.AInsideB)
                {
                    //has collision or contains
                    return false;
                }
            }
            //no collision 
            return true;
        }

        private Curve DrawLineRamp(Curve road,Vector3d axis, int onStart,RampScale scale , double offset)
        {
            Point3d start;
            Vector3d vx;
            Vector3d vy;

            if (axis == Vector3d.Unset)
                axis = Vector3d.XAxis;

            Vector3d axisPerp = new Vector3d(axis);
            axisPerp.Rotate(-Math.PI / 2, Vector3d.ZAxis);

            Vector3d roadv = road.TangentAtStart;
            double angleRoadAxis = Vector3d.VectorAngle(roadv, axis, Plane.WorldXY);
            if (angleRoadAxis < Math.PI * 0.25 || angleRoadAxis > Math.PI * 1.75 || (angleRoadAxis < Math.PI * 1.25 && angleRoadAxis > Math.PI * 0.75))
            {
                //selectperpvector
                vy = axisPerp;
            }
            else
            {
                //selectoriginvector
                vy = axis;
            }

            Point3d testPoint = road.PointAtNormalizedLength(0.5);
            Vector3d dirInside = new Vector3d(roadv);
            dirInside.Rotate(-Math.PI / 2, Vector3d.ZAxis);

            Point3d testPointInside = testPoint + dirInside;
            Point3d testPointY = testPoint + vy;

            Curve testLine = new LineCurve(testPointInside, testPointY);

            var testI = Rhino.Geometry.Intersect.Intersection.CurveCurve(testLine, road, 0, 0);
            if (testI.Count != 0)
            {
                vy.Reverse();
            }

            //true
            if (onStart == 0)
            {
                vx = new Vector3d(vy);
                vx.Rotate(Math.PI / 2, Vector3d.ZAxis);
                //vx = axis; // road.TangentAtStart;
                //if (vx == Vector3d.Unset)
                //    vx = road.TangentAtStart;
                //vy = new Vector3d(vx);
                //vy.Rotate(-Math.PI / 2, Vector3d.ZAxis);
                start = road.PointAtStart + road.TangentAtStart * (800 + offset);
                start = start + vy * 1000;
            }
            //false
            else
            {
                vx = new Vector3d(vy);
                vx.Rotate(-Math.PI / 2, Vector3d.ZAxis);
                //vx = -axis; //-road.TangentAtStart;
                //if (vx == -Vector3d.Unset)
                //    vx = -road.TangentAtStart;
                //vy = new Vector3d(vx);
                //vy.Rotate(Math.PI / 2, Vector3d.ZAxis);
                start = road.PointAtEnd + -road.TangentAtStart * (800 + offset);
                start = start + vy * 1000;
            }

            Point3d second = start + vx * UnderGroundParkingConsts.LinearRampWidth[(int)scale];
            Point3d third = second + vy * (20000+UnderGroundParkingConsts.WithinRadius[(int)scale]);
            Point3d forth = third - vx * UnderGroundParkingConsts.LinearRampWidth[(int)scale];

            PolylineCurve plc = new PolylineCurve(new Point3d[] { start, second, third, forth, start });
            return plc;
        }
        private Curve DrawCurvedRamp(Curve road,Vector3d axis, int onStart, RampScale scale, double beforeRamp, double offset)
        {
            Point3d start;
            Vector3d vx;
            Vector3d vy;


            if (axis == Vector3d.Unset)
                axis = Vector3d.XAxis;

            double afterRamp = 10000 - beforeRamp;


            Vector3d axisPerp = new Vector3d(axis);
            axisPerp.Rotate(-Math.PI / 2, Vector3d.ZAxis);

            Vector3d roadv = road.TangentAtStart;
            double angleRoadAxis = Vector3d.VectorAngle(roadv, axis, Plane.WorldXY);
            if (angleRoadAxis < Math.PI * 0.25 || angleRoadAxis > Math.PI * 1.75 || (angleRoadAxis < Math.PI * 1.25 && angleRoadAxis > Math.PI * 0.75))
            {
                //selectperpvector
                vy = axisPerp;
            }
            else
            {
                //selectoriginvector
                vy = axis;
            }

            Point3d testPoint = road.PointAtNormalizedLength(0.5);
            Vector3d dirInside = new Vector3d(roadv);
            dirInside.Rotate(-Math.PI / 2, Vector3d.ZAxis);

            Point3d testPointInside = testPoint + dirInside;
            Point3d testPointY = testPoint + vy;

            Curve testLine = new LineCurve(testPointInside, testPointY);

            var testI = Rhino.Geometry.Intersect.Intersection.CurveCurve(testLine, road, 0, 0);
            if (testI.Count != 0)
            {
                vy.Reverse();
            }
            //true
            if (onStart == 0)
            {
                vx = new Vector3d(vy);
                vx.Rotate(Math.PI / 2, Vector3d.ZAxis);
                //vx = axis;//road.TangentAtStart;
                //if (vx == Vector3d.Unset)
                //    vx = road.TangentAtStart;
                //vy = new Vector3d(vx);
                //vy.Rotate(-Math.PI / 2, Vector3d.ZAxis);
                start = road.PointAtStart + vx * (800 + offset);
                start = start + vy * 1000;

            }
            //false
            else
            {
                vx = new Vector3d(vy);
                vx.Rotate(-Math.PI / 2, Vector3d.ZAxis);
                //vx = -axis;// - road.TangentAtStart;
                //if (vx == -Vector3d.Unset)
                //    vx = -road.TangentAtStart;
                //vy = new Vector3d(vx);
                //vy.Rotate(Math.PI / 2, Vector3d.ZAxis);
                start = road.PointAtEnd + vx * (800 + offset);
                start = start + vy * 1000;
            }

            Point3d second = start + vx * UnderGroundParkingConsts.CurveRampWidth[(int)scale];

            Point3d third = second + vy * beforeRamp;//(20000 + UnderGroundParkingConsts.WithinRadius[(int)scale]);
            double innerArcR = UnderGroundParkingConsts.WithinRadius[(int)scale];
            double outerArcR = UnderGroundParkingConsts.WithinRadius[(int)scale] + UnderGroundParkingConsts.CurveRampWidth[(int)scale];
            Point3d forth = third + vy * innerArcR + vx * innerArcR; // third - vx * UnderGroundParkingConsts.LinearRampWidth[(int)scale];
            Point3d fifth = forth + vx * afterRamp;
            Point3d sixth = fifth + vy * UnderGroundParkingConsts.CurveRampWidth[(int)scale];
            Point3d seventh = sixth - vx * afterRamp;
            Point3d eighth = seventh - vx * outerArcR - vy * outerArcR;
            Point3d ninth = start;
            //polyline1
            PolylineCurve plc1 = new PolylineCurve(new Point3d[] { start, second, third});
            ArcCurve arc1 = new ArcCurve(new Arc(third, vy, forth));
            PolylineCurve plc2 = new PolylineCurve(new Point3d[] { forth,fifth, sixth, seventh });
            ArcCurve arc2 = new ArcCurve(new Arc(seventh, -vx, eighth));
            PolylineCurve plc3 = new PolylineCurve(new Point3d[] { eighth, ninth });
            Curve[] curvesToJoin = new Curve[] { plc1, arc1, plc2, arc2, plc3 };
            //Rhino.RhinoDoc.ActiveDoc.Objects.Add(Curve.JoinCurves(curvesToJoin)[0]);
            var joined = Curve.JoinCurves(curvesToJoin);
            if (joined.Length > 0)
                return joined[0];
            else
                return null;

            //return plc;
        }


        public bool Calculate()
        {
            if (!available)
            {
                return false;
            }
            
            double maximumCount = Math.Floor(isothetic.GetArea() * UnderGroundParkingConsts.ParkingPerArea);

            double requiredFloor = Math.Ceiling(require / maximumCount);

            floors = (int)requiredFloor;
            EachFloorParkingCount = (double)require / floors;
            eachFloorArea = EachFloorParkingCount / UnderGroundParkingConsts.ParkingPerArea;

            return true;
        }

        public bool CheckPrecondition()
        {
            if (boundary == null)
                return false;
            if (innerBoundary == null)
                return false;
            if (isothetic == null)
                return false;

            //대지면적의 80%가 500m2 이하
            if (boundary.GetArea() * 0.8 < UnderGroundParkingConsts.MinimumArea)
                return false;
            //내접하는 꺾인 직사각형의 면적이 500m2 이하
            if (isothetic.GetArea() < UnderGroundParkingConsts.MinimumArea)
                return false;

            available = true;
            return true;
        }

        private bool GetInnerBoundary()
        {
            //plot 의 boundary 와 roads 를 사용.
            var segments = boundary.DuplicateSegments();

            double offsetDistance = UnderGroundParkingConsts.Clearance;

            for (int i = 0; i < segments.Length; i++)
            {
                Curve temp = segments[i];
                var v = temp.TangentAtStart;
                v.Rotate(-Math.PI / 2, Vector3d.ZAxis);
                temp.Translate(v * offsetDistance);
                segments[i] = temp;
            }

            List<Point3d> topoly = new List<Point3d>();
            for (int k = 0; k < segments.Length; k++)
            {
                int j = (k + 1) % segments.Length;
                Line li = new Line(segments[k].PointAtStart, segments[k].PointAtEnd);
                Line lj = new Line(segments[j].PointAtStart, segments[j].PointAtEnd);

                double paramA;
                double paramB;
                var intersect = Rhino.Geometry.Intersect.Intersection.LineLine(li, lj, out paramA, out paramB, 0, false);

                // 교점이 A 선 위에 있음
                bool isparamAonA = paramA >= 0 && paramA <= 1 ? true : false;
                // 교점이 B 선 위에 있음
                bool isparamBonB = paramB >= 0 && paramB <= 1 ? true : false;

                bool isRightSided = paramA > 1 && paramB > 1 ? true : false;
                bool isLeftSided = paramA < 0 && paramB < 0 ? true : false;
                // A 나 B 둘중 한 선의 위에.
                if (isparamAonA && !isparamBonB || !isparamAonA && isparamBonB)
                {
                    topoly.Add(li.PointAt(paramA));
                    //topoly.Add(segments[k].PointAtEnd);
                    //topoly.Add(segments[j].PointAtStart);
                    //k의 endpoint 에서 j의 startpoint로 선 긋는다.
                }
                // 두 선 위의 교점에.
                else if (isparamAonA && isparamBonB)
                {
                    //교점을 더함
                    topoly.Add(li.PointAt(paramA));
                }
                //외부에
                else
                {
                    //오른쪽 치우침
                    if (isRightSided)
                    {
                        topoly.Add(segments[k].PointAtEnd);
                        topoly.Add(segments[j].PointAtStart);
                    }
                    //왼쪽 치우침
                    else if (isLeftSided)
                    {
                        topoly.Add(segments[k].PointAtEnd);
                        topoly.Add(segments[j].PointAtStart);
                    }
                    //일반
                    else
                        topoly.Add(li.PointAt(paramA));
                }
            }
            topoly.Add(topoly[0]);
            var tempcurve = new PolylineCurve(topoly);
            var rotation = tempcurve.ClosedCurveOrientation(Vector3d.ZAxis);
            var selfintersection = Rhino.Geometry.Intersect.Intersection.CurveSelf(tempcurve, 0);
            var parameters = selfintersection.Select(n => n.ParameterA).ToList();
            parameters.AddRange(selfintersection.Select(n => n.ParameterB));
            var spl = tempcurve.Split(parameters);
            var f = CommonFunc.NewJoin(spl);
            var merged = f.Where(n => n.ClosedCurveOrientation(Vector3d.ZAxis) == rotation).ToList();

            if (merged.Count == 0)
                return false;

            merged = merged.OrderByDescending(n => n.GetArea()).ToList();
            innerBoundary = merged[0];
            return true;
        }

        private bool GetInnerIsothetic()
        {
            Polyline poly = new Polyline();
            bool convertResult = boundary.TryGetPolyline(out poly);
            if (!convertResult)
                return false;

            InnerIsoDrawer noConcave = new InnerIsoDrawer(poly,2000,0);
            Isothetic iso1 = noConcave.Draw();

            InnerIsoDrawer oneConcave = new InnerIsoDrawer(poly, 2000, 1);
            Isothetic iso2 = oneConcave.Draw();

            isothetic = iso1.Outline.GetArea()>iso2.Outline.GetArea()? iso1.Outline.ToNurbsCurve() : iso2.Outline.ToNurbsCurve();
            return true;
        }

    }



    class EddieBrock
    {

    }
}
