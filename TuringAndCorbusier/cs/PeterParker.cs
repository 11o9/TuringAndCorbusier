using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhino.Geometry;

namespace TuringAndCorbusier
{
    public class ParkingMaster
    {
        Curve Boundary;
        List<Curve> aptLines;
        double TotalLength;
        public List<Curve> parkingCells;
        double CoreDepth;
        public ParkingMaster(Curve boundary, List<Curve> aptLines, double length, double coreDepth)
        {
            Boundary = InnerLoop(boundary);
            this.aptLines = aptLines;
            TotalLength = length;
            CoreDepth = coreDepth;
            //ParkingResult result = ParkingPrediction.Calculate()
        }

        private Curve InnerLoop(Curve boundary)
        {
            CurveOrientation ot = boundary.ClosedCurveOrientation(Plane.WorldXY);
            double offsetDistance = 3500;
            var segments = boundary.DuplicateSegments();
            

            for (int i = 0; i < segments.Length; i++)
            {
                Vector3d v = segments[i].TangentAtStart;
                v.Rotate(-Math.PI / 2, Vector3d.ZAxis);
                Curve temp = segments[i].DuplicateCurve();
                temp.Translate(v * offsetDistance);
                segments[i] = temp;
            }

            List<Point3d> toPoly = new List<Point3d>();

            for (int i = 0; i < segments.Length; i++)
            {
                int j = (i + 1) % segments.Length;
                Line l1 = new Line(segments[i].PointAtStart, segments[i].PointAtEnd);
                Line l2 = new Line(segments[j].PointAtStart, segments[j].PointAtEnd);
                double p1;
                double p2;
                Rhino.Geometry.Intersect.Intersection.LineLine(l1, l2, out p1, out p2);
                toPoly.Add(l1.PointAt(p1));
            }

            toPoly.Add(toPoly[0]);

            Curve merged = new Polyline(toPoly).ToNurbsCurve();

            var intersection = Rhino.Geometry.Intersect.Intersection.CurveSelf(merged, 0);

            var parameters = intersection.Select(n => n.ParameterA).ToList();
            parameters.AddRange(intersection.Select(n => n.ParameterB));

            var spltd = merged.Split(parameters);
            var joined = CommonFunc.NewJoin(spltd);

            return joined.Where(n => n.ClosedCurveOrientation(Plane.WorldXY) == ot).ToList()[0];


        }

        public int CalculateParkingScore()
        {
            if (aptLines.Count == 0)
                return 0;
            Vector3d dir0 = aptLines[0].TangentAtStart;
            List<Curve> origins = new List<Curve>(aptLines);
            dir0.Rotate(-Math.PI / 2, Vector3d.ZAxis);
            //List<Point3d> starts = x.Select(n => n.PointAtStart).ToList();
            if (origins.Count > 0)
            {
                Curve temp = origins[0].DuplicateCurve();
                temp.Translate(dir0 * TotalLength);
                origins.Insert(0, temp);

            }

            var parkingResult = ParkingPrediction.Calculate(TotalLength,CoreDepth);
            List<Curve> result = new List<Curve>();
            List<Curve> partitions = new List<Curve>();
            for (int i = 0; i < origins.Count; i++)
            {
                result.AddRange(parkingResult.GetCurves(origins[i]));
                partitions.AddRange(parkingResult.GetParkingLines(origins[i]));
            }

            var widths = parkingResult.widths();
            var heights = parkingResult.heights();

            var innerParkings = partitions.Where(n => IsInside(Boundary, n)).ToList();

            string log = innerParkings.Count.ToString() + " / " + partitions.Count.ToString()
              + " ( " + (double)innerParkings.Count / partitions.Count * 100 + " %)";

            //to debug
            parkingCells = innerParkings;

            return innerParkings.Count;
        }

        private bool IsInside(Curve c, Curve d)
        {
            var endPoints = d.DuplicateSegments().Select(n => n.PointAtStart).ToList();

            int outCount = 0;
            for (int i = 0; i < endPoints.Count; i++)
            {
                if (c.Contains(endPoints[i]) == PointContainment.Outside)
                    outCount++;
            }

            if (outCount > 0)
                return false;
            else
                return true;

        }
    }
    //단위 거리 분할 결과.
    public class ParkingResult
    {
        PeterParkerCollection resultCollection;
        public ParkingResult(PeterParkerCollection result)
        {
            resultCollection = result;
        }

        public List<Curve> GetCurves(Curve origin)
        {
            List<double> offsets = resultCollection.collection.Last().OffsetDistances();
            List<Curve> results = new List<Curve>();
            for (int i = 0; i < offsets.Count; i++)
            {
                Curve temp = origin.DuplicateCurve();
                Vector3d v = temp.TangentAtStart;
                v.Rotate(Math.PI / 2, Vector3d.ZAxis);
                temp.Translate(v * offsets[i]);
                results.Add(temp);
            }

            return results;
        }

        public List<Curve> GetParkingLines(Curve origin)
        {
            List<double> offsets = resultCollection.collection.Last().OffsetDistances();
            List<double[]> widths = resultCollection.collection.Last().GetWidths();
            List<double> heights = resultCollection.collection.Last().GetHeights();
            List<Curve> results = new List<Curve>();

            double originCurveRotation = Vector3d.VectorAngle(Vector3d.XAxis, origin.TangentAtEnd, Plane.WorldXY);

            for (int i = 0; i < widths.Count; i++)
            {
                Parking parking = resultCollection.collection.Last().parkings[i];

                //도로면 패스
                if (widths[i][0] == 0)
                    continue;

                Curve temp = origin.DuplicateCurve();
                Vector3d v = origin.TangentAtStart;
                v.Rotate(Math.PI / 2, Vector3d.ZAxis);
                temp.Translate(v * offsets[i]);
                var divided = origin.DivideByLength(widths[i][1], true);

                //하나도 안나오면 패스
                if (divided == null)
                    continue;

                var partitions = divided.Select(n => new LineCurve(temp.PointAt(n), temp.PointAt(n) - v * heights[i])).ToList();

                List<Curve> pLines = new List<Curve>();

                for (int j = 0; j < partitions.Count; j++)
                {
                    List<Curve> lines = parking.DrawLine(partitions[j].PointAtStart).ToList();
                    lines.ForEach(n => n.Transform(Transform.Rotation(originCurveRotation, Vector3d.ZAxis, partitions[j].PointAtStart)));
                    pLines.AddRange(lines);
                }

                pLines.ForEach(n => results.Add(n));


            }


            return results;
        }


        public bool IsInside(Curve c, Curve d)
        {
            var endPoints = d.DuplicateSegments().Select(n => n.PointAtStart).ToList();

            int outCount = 0;
            for (int i = 0; i < endPoints.Count; i++)
            {
                if (c.Contains(endPoints[i]) == PointContainment.Outside)
                    outCount++;
            }

            if (outCount > 0)
                return false;
            else
                return true;

        }


        //test//
        public List<string> widths()
        {
            List<string> result = new List<string>();
            List<double[]> widths = resultCollection.collection.Last().GetWidths();
            for (int i = 0; i < widths.Count; i++)
            {
                result.Add(widths[i][0].ToString() + " , " + widths[i][1].ToString());
            }
            return result;

        }

        public List<string> heights()
        {
            List<string> result = new List<string>();
            List<double> heights = resultCollection.collection.Last().GetHeights();
            for (int i = 0; i < heights.Count; i++)
            {
                result.Add(heights[i].ToString());
            }
            return result;

        }
    }

    public class ParkingPrediction
    {
        public static ParkingResult Calculate(double initialLength,double coreDepth)
        {
            PeterParker origin = new PeterParker(initialLength/*, initialCurve*/);
            Queue<PeterParker> wait = new Queue<PeterParker>();
            PeterParkerCollection fit = new PeterParkerCollection();
            wait.Enqueue(origin);

            double leftLengthLimit = 5000;
            //bool isFirst = true;
            while (wait.Count > 0)
            {
                PeterParker current = wait.Dequeue();
                for (int i = 0; i < (int)ParkingType.Max; i++)
                {
                    if ((current.LeftLength() == current.totalLength) && coreDepth > new Parking((ParkingType)i).height)
                        continue;
                    PeterParker temp = new PeterParker(current, (ParkingType)i);
                    if (temp.LeftLength() < 0)
                    {
                        //cull
                    }
                    else if (temp.LeftLength() > 0)
                    {
                        if (temp.LeftLength() < leftLengthLimit)
                        {
                            //fit
                            fit.Add(temp);
                        }
                        else
                        {
                            //re enqueue
                            wait.Enqueue(temp);
                        }
                    }

                }

                //isFirst = false;
            }

            //

            fit.collection.ForEach(n => n.FillRoads());
            var result = new ParkingResult(fit);

            return result;
        }
    }

    public class PeterParkerCollection
    {
        public List<PeterParker> collection;
        public int Count { get { return collection.Count; } }
        public PeterParkerCollection()
        {
            collection = new List<PeterParker>();
        }

        public void Add(PeterParker parker)
        {
            if (collection.Count < 10)
            {
                collection.Add(parker);
                Sort();
            }

            else
            {
                if (collection[0].ParkingUnitCount().Sum() < parker.ParkingUnitCount().Sum())
                {
                    collection.RemoveAt(0);
                    collection.Add(parker);
                    Sort();
                }
            }
        }

        void Sort()
        {
            PeterParkerComparer comp = new PeterParkerComparer();
            collection.Sort(comp);
        }
    }

    public enum ParkingType
    {
        P0Single,
        P0Double,
        P45Single,
        P45Double,
        P60Single,
        P60Double,
        P90Single,
        P90Double,
        Max,
        Road
    }

    public class Parking
    {
        public double height;
        public double width;
        public double widthOffset;
        public double necessaryRoad;
        public bool isDouble;
        public ParkingType type;
        public Parking(double distance)
        {
            height = distance;
            necessaryRoad = 0;
            width = 0;
            widthOffset = 0;
            isDouble = false;
            type = ParkingType.Road;
        }
        public Parking(ParkingType type)
        {
            this.type = type;
            switch (type)
            {
                case ParkingType.P0Single:
                    {
                        height = 2000;
                        width = 6000;
                        widthOffset = 6000;
                        necessaryRoad = 3500;
                        isDouble = false;
                        break;
                    }

                case ParkingType.P0Double:
                    {
                        height = 4000;
                        width = 6000;
                        widthOffset = 6000;
                        necessaryRoad = 3500;
                        isDouble = true;
                        break;
                    }

                case ParkingType.P45Single:
                    {
                        height = 5162;
                        width = 5162;
                        widthOffset = 3253;
                        necessaryRoad = 3500;
                        isDouble = false;
                        break;
                    }

                case ParkingType.P45Double:
                    {
                        height = 8697;
                        width = 5162;
                        widthOffset = 3253;
                        necessaryRoad = 3500;
                        isDouble = true;
                        break;
                    }

                case ParkingType.P60Single:
                    {
                        height = 5480;
                        width = 4492;
                        widthOffset = 2656;
                        necessaryRoad = 4500;
                        isDouble = false;
                        break;
                    }

                case ParkingType.P60Double:
                    {
                        height = 9810;
                        width = 4492;
                        widthOffset = 2656;
                        necessaryRoad = 4500;
                        isDouble = true;
                        break;
                    }

                case ParkingType.P90Single:
                    {
                        height = 5000;
                        width = 2300;
                        widthOffset = 2300;
                        necessaryRoad = 6000;
                        isDouble = false;
                        break;
                    }

                case ParkingType.P90Double:
                    {
                        height = 10000;
                        width = 2300;
                        widthOffset = 2300;
                        necessaryRoad = 6000;
                        isDouble = true;
                        break;
                    }
            }
        }
        public List<Curve> DrawLine(Point3d origin)
        {
            switch (type)
            {
                case ParkingType.P0Single:
                    return DrawRect(origin, -1, false);


                case ParkingType.P0Double:
                    return DrawRect(origin, -1, true);


                case ParkingType.P45Single:
                    return DrawRect(origin, Math.PI / 2 - Math.PI / 4, false);


                case ParkingType.P45Double:
                    return DrawRect(origin, Math.PI / 2 - Math.PI / 4, true);


                case ParkingType.P60Single:
                    return DrawRect(origin, Math.PI / 2 - Math.PI / 3, false);


                case ParkingType.P60Double:
                    return DrawRect(origin, Math.PI / 2 - Math.PI / 3, true);


                case ParkingType.P90Single:
                    return DrawRect(origin, Math.PI / 2 - Math.PI / 2, false);


                case ParkingType.P90Double:
                    return DrawRect(origin, Math.PI / 2 - Math.PI / 2, true);
            }
            return null;
        }
        public List<Curve> DrawRect(Point3d origin, double rad, bool isdouble)
        {
            List<Curve> result = new List<Curve>();
            //평행주차 시, [6,2]
            if (rad == -1)
            {
                Polyline p = new Polyline(new Point3d[]{
          origin,
          origin - Vector3d.YAxis * 2000,
          origin - Vector3d.YAxis * 2000 + Vector3d.XAxis * 6000,
          origin + Vector3d.XAxis * 6000,
          origin});

                result.Add(p.ToNurbsCurve());

                if (isdouble)
                {
                    Polyline p2 = new Polyline(new Point3d[]{
            p[1],
            p[1] - Vector3d.YAxis * 2000,
            p[1] - Vector3d.YAxis * 2000 + Vector3d.XAxis * 6000,
            p[1] + Vector3d.XAxis * 6000,
            p[1]});

                    result.Add(p2.ToNurbsCurve());
                }
            }
            //나머지 [2.3,5]
            else
            {
                Polyline p = new Polyline(new Point3d[]{
          origin,
          origin - Vector3d.YAxis * 5000,
          origin - Vector3d.YAxis * 5000 + Vector3d.XAxis * 2300,
          origin + Vector3d.XAxis * 2300,
          origin});

                if (isdouble)
                {
                    Polyline p2 = new Polyline(new Point3d[]{
            p[1],
            p[1] - Vector3d.YAxis * 5000,
            p[1] - Vector3d.YAxis * 5000 + Vector3d.XAxis * 2300,
            p[1] + Vector3d.XAxis * 2300,
            p[1]});

                    p2.Transform(Transform.Rotation(-rad, origin));
                    result.Add(p2.ToNurbsCurve());
                }
                p.Transform(Transform.Rotation(-rad, origin));
                result.Add(p.ToNurbsCurve());

            }

            return result;
        }
    }

    public class PeterParkerComparer : IComparer<PeterParker>
    {
        public int Compare(PeterParker a, PeterParker b)
        {

            return a.ParkingUnitCount().Sum().CompareTo(b.ParkingUnitCount().Sum());
        }

    }

    public class PeterParker
    {
        public List<Parking> parkings;
        public double totalLength;
        //public Curve baseCurve;

        public PeterParker(double length/*, Curve baseCurve*/)
        {
            parkings = new List<Parking>();
            totalLength = length;
            //this.baseCurve = baseCurve.DuplicateCurve();
        }

        public PeterParker(PeterParker parent, ParkingType type)
        {
            parkings = new List<Parking>(parent.parkings);
            parkings.Add(new Parking(type));
            totalLength = parent.totalLength;
            //baseCurve = parent.baseCurve;
        }

        //call when fit
        public void FillRoads()
        {

            List<int> inserts = new List<int>();
            List<Parking> roads = new List<Parking>();


            for (int i = 0; i < parkings.Count; i++)
            {
                int j = (i + 1) % parkings.Count;
                double roadi = parkings[i].necessaryRoad;
                double roadj = parkings[j].necessaryRoad;
                double roadLonger = roadi > roadj ? roadi : roadj;

                inserts.Add(i);
                roads.Add(new Parking(roadLonger));
            }

            //음.... +1 해줘야 맞음
            for (int i = inserts.Count - 1; i >= 0; i--)
            {
                parkings.Insert(inserts[i] + 1, roads[i]);
            }
        }
        //zero - back
        public List<double> OffsetDistances()
        {
            List<double> Lengths = new List<double>();

            double sum = 0;

            for (int i = 0; i < parkings.Count; i++)
            {
                sum += parkings[i].height;
                Lengths.Add(sum);
            }

            return Lengths;
        }

        //front - back
        public List<double> GetHeights()
        {
            List<double> Heights = new List<double>();
            Heights = parkings.Select(n => n.height).ToList();
            return Heights;
        }
        //left - right
        public List<double[]> GetWidths()
        {
            List<double[]> Widths = new List<double[]>();

            //double sum = 0;

            for (int i = 0; i < parkings.Count; i++)
            {
                Widths.Add(new double[] { parkings[i].width, parkings[i].widthOffset });
            }

            return Widths;
        }

        public double LeftLength()
        {

            List<double> Lengths = new List<double>();
            for (int i = 0; i < parkings.Count; i++)
            {
                int j = (i + 1) % parkings.Count;
                double roadi = parkings[i].necessaryRoad;
                double roadj = parkings[j].necessaryRoad;
                double roadLonger = roadi > roadj ? roadi : roadj;
                Lengths.Add(parkings[i].height);
                Lengths.Add(roadLonger);
            }

            return totalLength - Lengths.Sum();
        }

        //카운트를 여기서?...
        public List<int> ParkingUnitCount()
        {
            List<int> Counts = new List<int>();
            for (int i = 0; i < parkings.Count; i++)
            {
                double totalWidth = 100000;
                int count = 0;
                while (true)
                {
                    double offset = i == 0 ? parkings[i].width : parkings[i].widthOffset;
                    totalWidth -= offset;
                    if (totalWidth > 0)
                    {
                        if (parkings[i].isDouble)
                            count += 2;
                        else
                            count++;
                    }
                    else
                    {
                        Counts.Add(count);
                        break;
                    }
                }
            }

            return Counts;
        }
    }
}
