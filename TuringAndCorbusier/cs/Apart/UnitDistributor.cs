using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rhino.Geometry;

namespace TuringAndCorbusier
{
    class UnitDistributor
    {
        public List<ApartLine> aptLines;
        List<Unit> units;
        double[] unitrates;
        double[] added;

        public UnitDistributor(List<double> aptLineLengths, List<Unit> units, List<double> unitRates)
        {
            aptLines = aptLineLengths.Select(n => new ApartLine(n)).ToList();
            this.units = units;
            added = units.Select(n => 0.0).ToArray();
            unitrates = unitRates.ToArray();
        }

        public void DistributeByRate()
        {
            for (int k = 0; k < aptLines.Count; k++)
            {
                while (true)
                {
                    double minValue = double.MaxValue;
                    int minIndex = -1;
                    for (int i = 0; i < unitrates.Length; i++)
                    {
                        double[] tempPrediction = new double[unitrates.Length];

                        for (int j = 0; j < tempPrediction.Length; j++)
                        {
                            //선택한비율 index와 예측비율값 칸 index 같으면 +1 값 아니면 그대로
                            tempPrediction[j] = (added[j] + ((j == i) ? 1 : 0)) / (added.Sum() + 1);
                        }
                        double[] different = new double[tempPrediction.Length];
                        for (int j = 0; j < different.Length; j++)
                        {
                            different[j] = Math.Abs(tempPrediction[j] - unitrates[j]);
                        }

                        if (minValue > different.Sum())
                        {
                            minValue = different.Sum();
                            minIndex = i;
                        }
                    }


                    //유닛 넣기
                    //성공 - 다음
                    //실패 - 1. 작은면적 들어갈 공간 있음 - 넣음
                    //       2. 작은면적 들어갈 공간 없음 - 끝
                    if (minIndex == -1)
                        minIndex = 0;
                    var result = aptLines[k].Add(units[minIndex]);
                    if (result)
                    {
                        added[minIndex]++;
                        //RhinoApp.WriteLine("{0} index added(by rate), result : {1},{2}", minIndex, added[0], added[1]);
                    }

                    else
                    {

                        bool nothingAdded = true;
                        for (int i = units.Count - 1; i >= 0; i--)
                        {
                            var addResult = aptLines[k].Add(units[i]);
                            if (addResult)
                            {
                                added[i]++;
                                nothingAdded = false;
                                break;
                            }
                        }

                        if (nothingAdded)
                        {
                            //홀수 tower 체크
                            int towerCount = aptLines[k].Units.GetTypes().Where(n => n == UnitType.Tower).Count();

                            //타워가 홀수개 존재하면
                            if (towerCount % 2 != 0)
                            {
                                int towerindex = aptLines[k].Units.Contains.FindIndex(n => n.Type == UnitType.Tower);
                                aptLines[k].Units.Contains.RemoveAt(towerindex);


                                for (int i = units.Count - 1; i >= 0; i--)
                                {
                                    if (units[i].Type == UnitType.Tower)
                                        continue;

                                    while (true)
                                    {
                                        var addResult = aptLines[k].Add(units[i]);
                                        if (addResult)
                                        {
                                            added[i]++;
                                            nothingAdded = false;
                                        }
                                        else
                                            break;
                                    }
                                }

                            }
                            //RhinoApp.WriteLine("Nothing Added, Finish at {0}", k);
                            if (nothingAdded)
                                break;
                        }
                    }
                }
            }
        }

    }

    public enum UnitType
    {
        Corridor = 0,
        Tower,
        Clearance
    }

    class UnitCollection
    {
        public List<Unit> Contains { get; set; }
        public double Length { get { return GetLengthSum(); } }
        bool sorted = false;
        public UnitCollection()
        {
            Contains = new List<Unit>();
        }

        public List<UnitType> GetTypes()
        {
            //SandwichClearanceAppropriately();
            return Contains.Select(n => n.Type).ToList();
        }

        double GetLengthSum()
        {
            return Contains.Sum(n => n.Length);
        }

        public void Add(Unit unit)
        {
            int insertIndex = Contains.Count - 1;

            if (insertIndex == -1)
            {
                Contains.Add(unit);
                return;
            }

            for (int i = 0; i < Contains.Count; i++)
            {
                if (Contains[i].Type == unit.Type)
                {
                    insertIndex = i;
                    break;
                }

                if (i == Contains.Count - 1)
                {
                    Contains.Add(unit);
                    return;
                }
            }
            Contains.Insert(insertIndex, unit);
        }

        public void Remove(Unit unit)
        {
            Contains.Remove(unit);
        }

        public List<double> Positions()
        {
            SandwichClearanceAppropriately();
            List<double> positions = new List<double>();
            positions.Add(0);
            for (int i = 0; i < Contains.Count; i++)
            {
                double position = Contains.Take(i + 1).Sum(n => n.Length);
                positions.Add(position);
            }
            //positions.Add(Length);
            return positions;
        }

        public List<bool> Clearances()
        {
            SandwichClearanceAppropriately();
            return Contains.Select(n => n.Type == UnitType.Clearance ? true : false).ToList();
        }

        void SandwichClearanceAppropriately()
        {
            if (sorted)
                return;

            sorted = true;

            Contains.Sort((Unit a, Unit b) => ((int)a.Type).CompareTo((int)b.Type));

            //패티 숫자를 구한다. add 하면서 자동으로 넣어준 패티들.
            int pattyCount = Contains.Where(n => n.Type == UnitType.Clearance).Count();

            //번 길이!
            double corridorBunLength = Contains.Where(n => n.Type == UnitType.Corridor).Sum(n => n.Length);
            double towerBunLength = Contains.Where(n => n.Type == UnitType.Tower).Sum(n => n.Length);

            //각 번의 요소 count, 나중에 끼워넣을 위치 구하기 위함
            int corridorCount = Contains.Where(n => n.Type == UnitType.Corridor).Count();
            int towerCount = Contains.Where(n => n.Type == UnitType.Tower).Count();

            //번 별로 필요한 패티 숫자
            int corridorBurgerPattyCount = (int)Math.Floor((corridorBunLength / 70000));
            int towerBurgerPattyCount = (int)Math.Floor((towerBunLength / 70000));



            //필요 패티와 전체 패티 숫자 비교, 맞춤
            //if (corridorBurgerPattyCount + towerBurgerPattyCount != pattyCount)
            //{
            //    while (corridorBurgerPattyCount + towerBurgerPattyCount > pattyCount)
            //    {
            //        pattyCount++;
            //        Add(new Unit(0, 5000, UnitType.Clearance));
            //    }

            //    while (corridorBurgerPattyCount + towerBurgerPattyCount < pattyCount)
            //    {
            //        pattyCount--;
            //        if (Contains.Last().Type == UnitType.Clearance)
            //            Contains.RemoveAt(Contains.Count - 1);

            //    }
            //}

            //1.번이 두종류면 사이에 패티 위치시킴 

            if (corridorCount != 0 && towerCount != 0 && Length >= 70000)
            {
                int[] corr = GetStartEnd(UnitType.Corridor);
                Contains.Insert(corr[1] + 1, new Unit(0, 5000, UnitType.Clearance));
                Contains.RemoveAt(Contains.Count - 1);
            }

            //corridor 번의 시작/끝
            int[] corridorIndex = GetStartEnd(UnitType.Corridor);

            //번이 들어갈 간격
            double offset = (double)corridorCount / (double)(corridorBurgerPattyCount + 1);

            //패티 개수만큼 돌며 패티 삽입
            for (int i = 0; i < corridorBurgerPattyCount; i++)
            {
                //시작 인덱스 + offset + 누적추가분
                int index = corridorIndex[0] + (int)Math.Ceiling(offset) * (i + 1) + i;
                //끝 인덱스 + 누적 추가분
                if (index >= corridorIndex[1] + 1 + i)
                    index = corridorIndex[1] + 1 + i;
                Contains.Insert(index, new Unit(0, 5000, UnitType.Clearance));
                Contains.RemoveAt(Contains.Count - 1);

            }

            int[] towerIndex = GetStartEnd(UnitType.Tower);
            offset = (double)towerCount / (double)(towerBurgerPattyCount + 1);
            offset = Math.Ceiling(offset);

            if (offset % 2 != 0)
                offset++;
            for (int i = 0; i < towerBurgerPattyCount; i++)
            {
                //시작 인덱스 + offset + 누적추가분
                int index = towerIndex[0] + (int)offset * (i + 1) + i;
                //끝 인덱스 + 1 + 누적 추가분
                if (index >= towerIndex[1] + 1 + i)
                    index = towerIndex[1] + 1 + i;

                if (index == Contains.Count)
                    break;

                Contains.Insert(index, new Unit(0, 5000, UnitType.Clearance));
                Contains.RemoveAt(Contains.Count - 1);
            }



        }

        int[] GetStartEnd(UnitType type)
        {
            int[] indexes = new int[2] { -1, -1 };

            for (int i = 0; i < Contains.Count; i++)
            {
                if (Contains[i].Type == type)
                {
                    if (indexes[0] == -1)
                        indexes[0] = i;
                    indexes[1] = i;
                }
            }

            return indexes;
        }
    }

    class ApartLine
    {
        public double TotalLength { get; set; }
        public UnitCollection Units { get; set; }
        public double LeftLength { get { return GetLeftLength(); } }
        double tempClearancePredict = 0;
        public ApartLine(double length)
        {
            TotalLength = length;
            Units = new UnitCollection();
        }

        double GetLeftLength()
        {
            return
              TotalLength - Units.Length;
        }

        public bool Add(Unit unit)
        {

            if (LeftLength >= unit.Length)
            {
                Units.Add(unit);

                if (ClearancePredict() != tempClearancePredict && unit.Type != UnitType.Clearance)
                {
                    tempClearancePredict = ClearancePredict();
                    Unit clearance = new Unit(0, 5000, UnitType.Clearance);
                    Units.Add(clearance);

                    if (LeftLength < 0)
                    {
                        Units.Remove(unit);
                        Units.Remove(clearance);
                        return false;
                    }
                }


                return true;
            }
            else
            {
                return false;
            }
        }

        double ClearancePredict()
        {
            double tempLength = TotalLength - LeftLength;
            double div = tempLength / 70000;
            return Math.Floor(div);
        }
    }

    class Unit
    {
        public double Length { get; set; }
        public double Area { get; set; }
        public UnitType Type { get; set; }

        public Unit(double area, double length, UnitType type)
        {
            Length = length;
            Area = area;
            Type = type;
        }
    }




    class HouseholdGenerator
    {

        Vector3d XDirection;
        Vector3d YDirection;
        double Width;
        double Length;
        int householdSizetype = 0;
        Curve baseCurve;
        double coreWidth;
        double coreDepth;



        public void Initialize(Curve baseCurve)
        {
            this.baseCurve = baseCurve;
            XDirection = -baseCurve.TangentAtStart;
            YDirection = new Vector3d(XDirection);
            YDirection.Rotate(Math.PI / 2, Vector3d.ZAxis);
            Length = baseCurve.GetLength();
        }

        public HouseholdGenerator(double width, double coreWidth, double coreDepth)
        {
            Width = width;
            this.coreWidth = coreWidth;
            this.coreDepth = coreDepth;
        }

        public Household Generate(UnitType type, int buildingnum, int houseindex)
        {
            Household temp = new Household();
            temp.XDirection = XDirection;
            temp.YDirection = YDirection;
            temp.XLengthA = Length;

            switch (type)
            {
                case UnitType.Corridor:
                    temp.isCorridorType = true;
                    temp.YLengthA = Width - Consts.corridorWidth;
                    temp.Origin = baseCurve.PointAtEnd + temp.YDirection * (Width / 2 - Consts.corridorWidth);
                    temp.WallFactor = new List<double>(new double[] { 1, 0.5, 1, 0.5 });
                    temp.EntrancePoint = baseCurve.PointAtNormalizedLength(0.5) - temp.YDirection * Consts.corridorWidth;
                    temp.CorridorArea = Consts.corridorWidth * Length;
                    temp.indexer = new int[] { buildingnum, houseindex };
                    break;
                case UnitType.Tower:
                    temp.isCorridorType = false;
                    temp.XLengthB = coreWidth / 2;
                    temp.YLengthA = Width;
                    temp.YLengthB = coreDepth;
                    temp.WallFactor = new List<double>(new double[] { 1, 1, 0.5, 1, 0.5, 1 });
                    //짝수인경우 : 홀수인경우
                    var p = (houseindex % 2 == 0) ? baseCurve.PointAtEnd : baseCurve.PointAtStart;
                    temp.XDirection *= (houseindex % 2 == 0) ? 1 : -1;
                    p += temp.YDirection * Width / 2;
                    p -= temp.YDirection * temp.YLengthB;
                    p += temp.XDirection * coreWidth / 2;
                    temp.Origin = p;
                    temp.CorridorArea = 0;
                    temp.EntrancePoint = temp.Origin - temp.YDirection * temp.YLengthB / 2;
                    temp.indexer = new int[] { buildingnum, houseindex };

                    break;

                default:
                    temp.XLengthA = 0;
                    break;
            }

            return temp;
        }
    }

}
