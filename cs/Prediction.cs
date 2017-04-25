using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhino.Geometry;

namespace TuringAndCorbusier
{
    class Prediction
    {


        #region Angle_Prediction
        //일정 거리 떨어진 예비 선들을 뽑아 값 비교
        public static double AngleMax_Simple(Curve regCurve, double floor, double width, double angleRadian, out List<Curve> results, out List<Line> parkingBase)
        {
            double height = regCurve.PointAtStart.Z;
            double aptDistance = (height - Consts.PilotiHeight) * 0.8;
            Curve regulationCurve = regCurve.DuplicateCurve();
            Curve regZero = regCurve.DuplicateCurve();


            List<Curve> output = new List<Curve>();
            //List<Curve> wholeLines = new List<Curve>();
            List<Curve> outputUp = new List<Curve>();
            List<Curve> outputDown = new List<Curve>();
            List<Curve> outputRegions = new List<Curve>();

            regZero.Translate(Vector3d.ZAxis * -regZero.PointAt(0).Z);

            Point3d rotatecenter = AreaMassProperties.Compute(regCurve).Centroid;
            regulationCurve.Rotate(-angleRadian, Vector3d.ZAxis, rotatecenter);
            var boundingbox = regulationCurve.GetBoundingBox(false);

            //y범위
            double ygap = boundingbox.Max.Y - boundingbox.Min.Y;


            List<double> param = new List<double>();

            double linecount = 1;
            double unitlength = width;
            double z = width;
            double y = aptDistance;

            //y채우기
            param.Add(0);

            while (unitlength < ygap)
            {
                linecount++;
                unitlength = z * linecount + y * (linecount - 1);
                if (unitlength < ygap)
                    param.Add(unitlength);
            }

            if (unitlength > ygap)
                unitlength -= z + y;

            param[0] += z;

            //채우고 남은 y
            double lengthremain = ygap - unitlength;

            List<double> wholeLengths = new List<double>();
            double MaxLength = double.MinValue;

            List<Line> result = new List<Line>();
            List<Line> parkingBaseline = new List<Line>();
            //남은 길이의 0.1단위마다,,? ===> 2라인,3라인 이 1,2보다 안나오는 경우 있음. 모든 y 검토

            double step = 1;

            for (int k = 0; k < ygap / step; k++)
            {
                List<Line> tempResult = new List<Line>();

                double[] tempParam = param.ToArray();

                //parameter[i] + 단위길이 - width/2 위치에 라인 생성
                for (int i = 0; i < param.Count; i++)
                {
                    tempParam[i] = param[i] + k * step - z / 2;
                    Line templine = new Line(new Point3d(boundingbox.Min.X, boundingbox.Min.Y + tempParam[i], 0), new Point3d(boundingbox.Max.X, boundingbox.Min.Y + tempParam[i], 0));
                    tempResult.Add(templine);
                }


                //생성된 라인을 역회전 하여 기존 상태로 되돌림.
                for (int i = 0; i < tempResult.Count; i++)
                {
                    Line tempr = tempResult[i];
                    tempr.Transform(Transform.Rotation(angleRadian, Vector3d.ZAxis, rotatecenter));
                    tempResult[i] = tempr;
                }

                List<Curve> aptlines = new List<Curve>();

                //offset한 라인마다 충돌체크,길이조정
                for (int i = 0; i < tempResult.Count; i++)
                {
                    Curve[] inner = InnerRegion(regZero, tempResult[i].ToNurbsCurve(), z);
                    aptlines.AddRange(inner);
                }

                //결과 값의 길이 확인
                var AvailableLength = GetAvailableLength(aptlines);
                //wholeLines.AddRange(aptlines);
                wholeLengths.Add(AvailableLength);
                if (MaxLength < AvailableLength)
                {
                    MaxLength = AvailableLength;
                    output = aptlines;
                    parkingBaseline = tempResult;
                }

            }

            if (output.Count == 0)
            {
                results = new List<Curve>();
                parkingBase = new List<Line>();
                return 0;
            }

            //outputUp = output.Select(n => n.DuplicateCurve()).Where(n => n.GetLength() >= 8).ToList();
            //outputDown = output.Select(n => n.DuplicateCurve()).Where(n => n.GetLength() >= 8).ToList();

            //Vector3d tv = output[0].TangentAtStart;
            //tv.Rotate(Math.PI / 2, Vector3d.ZAxis);
            //tv.Unitize();

            //for (int i = 0; i < outputUp.Count; i++)
            //{
            //    outputUp[i].Transform(Transform.Translation(tv * z / 2));
            //    outputDown[i].Transform(Transform.Translation(-tv * z / 2));

            //    Polyline p = new Polyline(new Point3d[] { outputUp[i].PointAtStart, outputUp[i].PointAtEnd, outputDown[i].PointAtEnd, outputDown[i].PointAtStart, outputUp[i].PointAtStart });
            //    outputRegions.Add(p.ToNurbsCurve());
            //}
            results = output;
            parkingBase = parkingBaseline;
            return MaxLength;

        }

        public static double AngleMax(Curve regCurve, double floor, double width, double angleRadian, out List<Curve> results)
        {
            double height = regCurve.PointAtStart.Z;
            double aptDistance = (height - Consts.PilotiHeight) * 0.8;
            Curve regulationCurve = regCurve.DuplicateCurve();
            Curve regZero = regCurve.DuplicateCurve();
            //double[] parameters = { a, b, c, d, f };
            //double storiesHigh = Math.Max((int)parameters[0], (int)parameters[1]);
            //double storiesLow = Math.Min((int)parameters[0], (int)parameters[1]);
            //double width = parameters[2];
            //double angleRadian = -parameters[3];

            List<Curve> output = new List<Curve>();
            //List<Curve> wholeLines = new List<Curve>();
            List<Curve> outputUp = new List<Curve>();
            List<Curve> outputDown = new List<Curve>();
            List<Curve> outputRegions = new List<Curve>();

            regZero.Translate(Vector3d.ZAxis * -regZero.PointAt(0).Z);

            Point3d rotatecenter = AreaMassProperties.Compute(regCurve).Centroid;
            regulationCurve.Rotate(-angleRadian, Vector3d.ZAxis, rotatecenter);
            var boundingbox = regulationCurve.GetBoundingBox(false);

            //y범위
            double ygap = boundingbox.Max.Y - boundingbox.Min.Y;


            List<double> param = new List<double>();

            double linecount = 1;
            double unitlength = width;
            double z = width;
            double y = aptDistance;

            //y채우기
            param.Add(0);

            while (unitlength < ygap)
            {
                linecount++;
                unitlength = z * linecount + y * (linecount - 1);
                if (unitlength < ygap)
                    param.Add(unitlength);
            }

            if (unitlength > ygap)
                unitlength -= z + y;

            param[0] += z;

            //채우고 남은 y
            double lengthremain = ygap - unitlength;

            List<double> wholeLengths = new List<double>();
            double MaxLength = double.MinValue;

            List<Line> result = new List<Line>();
            //남은 길이의 0.1단위마다,,? ===> 2라인,3라인 이 1,2보다 안나오는 경우 있음. 모든 y 검토

            double step = 1;

            for (int k = 0; k < ygap / step; k++)
            {
                List<Line> tempResult = new List<Line>();

                double[] tempParam = param.ToArray();

                //parameter[i] + 단위길이 - width/2 위치에 라인 생성
                for (int i = 0; i < param.Count; i++)
                {
                    tempParam[i] = param[i] + k * step - z / 2;
                    Line templine = new Line(new Point3d(boundingbox.Min.X, boundingbox.Min.Y + tempParam[i], 0), new Point3d(boundingbox.Max.X, boundingbox.Min.Y + tempParam[i], 0));
                    tempResult.Add(templine);
                }


                //생성된 라인을 역회전 하여 기존 상태로 되돌림.
                for (int i = 0; i < tempResult.Count; i++)
                {
                    Line tempr = tempResult[i];
                    tempr.Transform(Transform.Rotation(angleRadian, Vector3d.ZAxis, rotatecenter));
                    tempResult[i] = tempr;
                }

                List<Curve> aptlines = new List<Curve>();

                //offset한 라인마다 충돌체크,길이조정
                for (int i = 0; i < tempResult.Count; i++)
                {
                    Curve[] inner = InnerRegion(regZero, tempResult[i].ToNurbsCurve(), z);
                    aptlines.AddRange(inner);
                }

                //결과 값의 길이 확인
                var AvailableLength = GetAvailableLength(aptlines);
                //wholeLines.AddRange(aptlines);
                wholeLengths.Add(AvailableLength);
                if (MaxLength < AvailableLength)
                {
                    MaxLength = AvailableLength;
                    output = aptlines;
                }

            }

            if (output.Count == 0)
            {
                results = new List<Curve>();
                return 0;
            }

            outputUp = output.Select(n => n.DuplicateCurve()).Where(n => n.GetLength() >= 8).ToList();
            outputDown = output.Select(n => n.DuplicateCurve()).Where(n => n.GetLength() >= 8).ToList();

            Vector3d tv = output[0].TangentAtStart;
            tv.Rotate(Math.PI / 2, Vector3d.ZAxis);
            tv.Unitize();

            for (int i = 0; i < outputUp.Count; i++)
            {
                outputUp[i].Transform(Transform.Translation(tv * z / 2));
                outputDown[i].Transform(Transform.Translation(-tv * z / 2));

                Polyline p = new Polyline(new Point3d[] { outputUp[i].PointAtStart, outputUp[i].PointAtEnd, outputDown[i].PointAtEnd, outputDown[i].PointAtStart, outputUp[i].PointAtStart });
                outputRegions.Add(p.ToNurbsCurve());
            }
            results = outputRegions;
            return MaxLength;
        }
        public static double GetAvailableLength(IEnumerable<Curve> curves)
        {
            double minlength = 8000;
            var list = curves.ToList();
            double result = 0;
            for (int i = 0; i < list.Count; i++)
            {
                var d = list[i].GetLength() >= minlength ? list[i].GetLength() : 0;
                result += d;
            }

            return result;
        }
        public static Curve[] InnerRegion(Curve outside, Curve baseCurve, double regionWidth)
        {
            //check upper, lower bound
            int underzero = 5;

            Curve up = baseCurve.DuplicateCurve();
            Curve down = baseCurve.DuplicateCurve();

            Vector3d vu = up.TangentAtStart * regionWidth / 2;
            vu.Rotate(Math.PI / 2, Vector3d.ZAxis);
            Vector3d vd = -vu;

            up.Translate(vu);
            down.Translate(vd);

            var iu = Rhino.Geometry.Intersect.Intersection.CurveCurve(up, outside, 0, 0);
            var id = Rhino.Geometry.Intersect.Intersection.CurveCurve(down, outside, 0, 0);

            List<double> parameters = new List<double>();
            parameters.AddRange(iu.Select(n => Math.Round(n.ParameterA, underzero)));
            parameters.AddRange(id.Select(n => Math.Round(n.ParameterA, underzero)));

            var su = up.Split(parameters);
            var sd = down.Split(parameters);

            bool[] inu = su.Select(n => outside.Contains(n.PointAtNormalizedLength(0.5)) == PointContainment.Inside).ToArray();
            bool[] ind = sd.Select(n => outside.Contains(n.PointAtNormalizedLength(0.5)) == PointContainment.Inside).ToArray();

            if (inu.Length != ind.Length)
            //why?
            {
                return new Curve[0];
            }

            var sb = baseCurve.Split(parameters);
            List<Curve> result = new List<Curve>();
            List<Curve> boxes = new List<Curve>();
            for (int i = 0; i < inu.Length; i++)
            {
                if (inu[i] && ind[i])
                {
                    //1번,3번 segments 가 사이드선
                    boxes.Add(new Polyline(
                        new Point3d[] { su[i].PointAtStart, su[i].PointAtEnd, sd[i].PointAtEnd, sd[i].PointAtStart, su[i].PointAtStart }
                        ).ToNurbsCurve());

                    result.Add(sb[i]);
                }
            }


            // check side
            Vector3d testv = -baseCurve.TangentAtStart;
            //makebox
            for (int i = 0; i < boxes.Count; i++)
            {
                //외곽 기준선의 vertex들
                var outps = outside.DuplicateSegments().Select(n => n.PointAtStart).ToList();

                List<double> lefts = new List<double>();
                List<double> rights = new List<double>();

                for (int j = 0; j < outps.Count; j++)
                {
                    //박스 i 가 점 j 를 포함하면?
                    if (boxes[i].Contains(outps[j]) == PointContainment.Inside)
                    {
                        var boxsegments = boxes[i].DuplicateSegments();
                        Point3d testleft = outps[j] - testv;

                        double param;
                        result[i].ClosestPoint(outps[j], out param);
                        //외곽 기준선이 testpoint 를 포함하면?
                        if (outside.Contains(testleft) == PointContainment.Inside)
                        {
                            //왼쪽 선 수정
                            lefts.Add(param);
                        }
                        else
                        {
                            //오른쪽 선 수정
                            rights.Add(param);
                        }

                    }
                }

                //lefts 중 max, rights 중 min 으로 선 조정....
                var newstart = lefts.Count > 0 ? result[i].PointAt(lefts.Max()) : result[i].PointAtStart;
                var newend = rights.Count > 0 ? result[i].PointAt(rights.Min()) : result[i].PointAtEnd;
                result[i] = new LineCurve(newstart, newend);

            }

            return result.ToArray();

        }


        //예비 선을 모두 뽑아 일정 거리 이상 의 선들을 조합.
        //느림. 정교화..?
        public static double AngleMax_Josh(Curve regCurve, double floor, double width, double angleRadian, out List<Curve> resultCurves)
        {
            double height = regCurve.PointAtStart.Z;
            double aptDistance = (height - Consts.PilotiHeight) * 0.8;
            Curve regulationCurve = regCurve.DuplicateCurve();
            Curve regZero = regCurve.DuplicateCurve();

            double minlength = 8000;

            List<Curve> output = new List<Curve>();
            List<Curve> wholeLines = new List<Curve>();
            List<Curve> outputUp = new List<Curve>();
            List<Curve> outputDown = new List<Curve>();
            List<Curve> outputRegions = new List<Curve>();

            regZero.Translate(Vector3d.ZAxis * -regZero.PointAt(0).Z);

            Point3d rotatecenter = AreaMassProperties.Compute(regCurve).Centroid;
            regulationCurve.Rotate(angleRadian, Vector3d.ZAxis, rotatecenter);
            regulationCurve.Translate(Vector3d.ZAxis * -regulationCurve.PointAt(0).Z);
            var boundingbox = regulationCurve.GetBoundingBox(false);

            //y범위
            double ygap = boundingbox.Max.Y - boundingbox.Min.Y;


            List<double> wholeLengths = new List<double>();

            //같은 라인에서 쪼개진녀석들 있을수 있으므로..
            List<List<Curve>> result = new List<List<Curve>>();

            double step = 0.5;

            for (int k = 0; k < ygap / step; k++)
            {
                Curve tempLine = new LineCurve(new Point3d(boundingbox.Min.X, boundingbox.Min.Y + k * step, 0), new Point3d(boundingbox.Max.X, boundingbox.Min.Y + k * step, 0));

                var survivor = InnerRegion(regulationCurve, tempLine, width);
                //result.Add(tempLine);
                result.Add(survivor.Where(n => n.GetLength() > minlength).ToList());

            }


            //var lengths = result.Select(n => n.GetLength()).ToList();
            var offsetindex = (int)Math.Round((aptDistance + width) / step);

            double maxlength = 0;
            List<int> indexes = new List<int>();

            Wornl(new List<int>(), 0, offsetindex, result, ref maxlength, ref indexes);

            #region asdf



            //for (int i = 0; i < result.Count; i++)
            //{
            //    List<int> baselist = new List<int>();
            //    baselist.Add(i);
            //    if (i + offsetindex >= result.Count)
            //    {
            //        List<int> cindexes = baselist;
            //        double tempmvalue = cindexes.Sum(n=>result[n].Sum(m=>m.GetLength()));
            //        if (maxlength < tempmvalue)
            //        {
            //            maxlength = tempmvalue;
            //            indexes = cindexes;
            //        }
            //    }
            //    else
            //    {
            //        for (int j = i + offsetindex; j < result.Count; j++)
            //        {
            //            if (j + offsetindex >= result.Count)
            //            {
            //                double tempmvalue = result[i].Sum(n => n.GetLength()) + result[j].Sum(n => n.GetLength());
            //                if (maxlength < tempmvalue)
            //                {
            //                    maxlength = tempmvalue;
            //                    indexes = new List<int>() { i , j};
            //                }
            //            }
            //            else
            //            {
            //                for (int k = j + offsetindex; k < result.Count; k++)
            //                {
            //                    if (k + offsetindex >= result.Count)
            //                    {
            //                        double tempmvalue = result[i].Sum(n => n.GetLength()) + result[j].Sum(n => n.GetLength()) + result[k].Sum(n => n.GetLength());
            //                        if (maxlength < tempmvalue)
            //                        {
            //                            maxlength = tempmvalue;
            //                            indexes = new List<int>() { i ,j ,k };
            //                        }
            //                    }

            //                    else
            //                    {
            //                        for (int l = k + offsetindex; l < result.Count; l++)
            //                        {
            //                            double tempmvalue = result[i].Sum(n => n.GetLength()) + result[j].Sum(n => n.GetLength()) + result[k].Sum(n => n.GetLength()) + result[l].Sum(n => n.GetLength());
            //                            if (maxlength < tempmvalue)
            //                            {
            //                                maxlength = tempmvalue;
            //                                indexes = new List<int>() { i , j , k , l};
            //                            }
            //                        }
            //                    }
            //                }
            //            }
            //        }
            //    }

            //next = i + offset
            //}
            #endregion

            for (int k = 0; k < result.Count; k++)
            {
                result[k].ForEach(n => n.Rotate(-angleRadian, Vector3d.ZAxis, rotatecenter));
            }


            List<Curve> outcurve = new List<Curve>();
            indexes.ForEach(n => outcurve.AddRange(result[n]));
            resultCurves = outcurve;
            return maxlength;
        }
        public static void Wornl(List<int> baselist, int start, int offsetindex, List<List<Curve>> result, ref double maxlength, ref List<int> indexes)
        {
            for (int i = start; i < result.Count; i++)
            {
                List<int> cindexes = new List<int>(baselist);
                cindexes.Add(i);
                if (i + offsetindex >= result.Count)
                {

                    double tempmvalue = cindexes.Sum(n => result[n].Sum(m => m.GetLength()));
                    if (maxlength < tempmvalue)
                    {
                        maxlength = tempmvalue;
                        indexes = cindexes;
                    }
                }
                else
                {
                    Wornl(cindexes, i + offsetindex, offsetindex, result, ref maxlength, ref indexes);
                }
            }
        }
        #endregion

        #region Parking_Prediction
        #endregion

    }

}

