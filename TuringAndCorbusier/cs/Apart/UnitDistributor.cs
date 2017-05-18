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
        //double[] unitrates;
        double[] added;

        public UnitDistributor(List<double> aptLineLengths, List<Unit> units)
        {
            aptLines = aptLineLengths.Select(n => new ApartLine(n)).ToList();
            this.units = units;
            //added = units.Select(n => 0.0).ToArray();
        }

        public void DistributeUnit()
        {

            int suppliedSum = 0;
            //각 아파트 라인마다
            for (int i = 0; i < aptLines.Count; i++)
            {

                while (true)
                {
                    //int selectedUnit = -1;
                    List<double> expectedValues = new List<double>();

                    //목표 비율 = unit.rate
                    //현재 비율 = unit.supplied / supplied.Sum
                    List<double> targetRate = units.Select(n => n.Rate).ToList();
                    List<double> tempRate = suppliedSum != 0 ?  //공급 0이 아니면
                        units.Select(n => (double)n.Supplied / suppliedSum).ToList() //유닛별 공급 / 총 공급 으로 비율 계산 
                        : targetRate.Select(n => (double)0).ToList(); //다 0

                    for (int j = 0; j < units.Count; j++)
                    {
                        //[j] 골랐을때 예상 비율 = j 유닛 : supplied + 1 / supplied.Sum +1 , 선택 안한 유닛 : supplied / supplied.Sum + 1
                        //현재 비율 - 예상 비율 의 절대값의 합이 가장 작은 경우 선택.
                        List<double> expectedRate = units.Select(n => (double)n.Supplied).ToList();
                        expectedRate[j] += 1;

                        expectedRate = expectedRate.Select(n => n / (suppliedSum + 1)).ToList();

                        for (int k = 0; k < expectedRate.Count; k++)
                        {
                            expectedRate[k] = Math.Abs(expectedRate[k] - targetRate[k]);
                        }

                        double distance = expectedRate.Sum();
                        expectedValues.Add(distance);
                        //예상값에 추가
                    }

                    //필수 유닛 - unit.required > unit.supplied 인 세대 우선 배분한다.
                    //필수 유닛 분배 완료 - 모든? 유형에 기회 제공 (면적 가변 유닛으로)


                    List<bool> satisfyReq = units.Select(n => n.Required <= n.Supplied ? true : false).ToList();
                    int minIndex = 0;
                    double minValue = double.MaxValue;
                    //필요는 모두 만족
                    Unit unitToAdd;
                    var trueOnly = satisfyReq.Where(n => n).ToList();
                    if (trueOnly.Count == satisfyReq.Count)
                    {
                        for (int j = 0; j < expectedValues.Count; j++)
                        {
                            //순차적으로 최소값 인덱스 탐색
                            if (expectedValues[j] < minValue)
                            {
                                minIndex = j;
                                minValue = expectedValues[j];
                            }
                        }

                        //가변유닛
                        unitToAdd = units[minIndex].GetFixed(false);
                    }
                    else
                    //아님
                    {
                        for (int j = 0; j < expectedValues.Count; j++)
                        {
                            //순차적으로 최소값 & 불충족 인덱스 탐색
                            if (expectedValues[j] < minValue && !satisfyReq[j])
                            {
                                minIndex = j;
                                minValue = expectedValues[j];
                            }
                        }

                        //고정유닛
                        unitToAdd = units[minIndex].GetFixed(true);
                    }

                    //units[minIndex] 추가.


                    //더이상 공간이 안남을때까지 반복,
                    //

                    //유닛 넣기
                    //성공 - 다음
                    //실패 - 1. 작은면적 들어갈 공간 있음 - 넣음
                    //       2. 작은면적 들어갈 공간 없음 - 끝
                    //if (minIndex == -1)
                    //    minIndex = 0;

                    var result = aptLines[i].Add(unitToAdd);
                    if (result)
                    {
                        units[minIndex].Supplied++;
                        suppliedSum++;
                        //added[minIndex]++;
                        //RhinoApp.WriteLine("{0} index added(by rate), result : {1},{2}", minIndex, added[0], added[1]);
                    }

                    else
                    {

                        bool nothingAdded = true;
                        for (int j = units.Count - 1; j >= 0; j--)
                        {
                            unitToAdd = units[j].GetFixed(false);
                            var addResult = aptLines[i].Add(unitToAdd);
                            if (addResult)
                            {
                                units[j].Supplied++;
                                suppliedSum++;
                                nothingAdded = false;
                                break;
                            }
                        }

                        if (nothingAdded)
                        {
                            //홀수 tower 체크
                            int towerCount = aptLines[i].Container.GetTypes().Where(n => n == UnitType.Tower).Count();

                            //타워가 홀수개 존재하면
                            if (towerCount % 2 != 0)
                            {
                                int towerindex = aptLines[i].Container.Units.FindIndex(n => n.Type == UnitType.Tower);
                                aptLines[i].Container.Units.RemoveAt(towerindex);


                                for (int j = units.Count - 1; j >= 0; j--)
                                {
                                    if (units[j].Type == UnitType.Tower)
                                        continue;

                                    while (true)
                                    {
                                        unitToAdd = units[j].GetFixed(false);
                                        var addResult = aptLines[i].Add(unitToAdd);
                                        if (addResult)
                                        {
                                            units[j].Supplied++;
                                            suppliedSum++;
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

            aptLines = aptLines;
        }

        //not used
        public void DistributeByRate()
        {
            //for (int k = 0; k < aptLines.Count; k++)
            //{
            //    while (true)
            //    {
            //        double minValue = double.MaxValue;
            //        int minIndex = -1;
            //        for (int i = 0; i < units.Count; i++)
            //        {
            //            double[] tempPrediction = units.Select(n => n.Rate).ToArray();

            //            for (int j = 0; j < tempPrediction.Length; j++)
            //            {
            //                //선택한비율 index와 예측비율값 칸 index 같으면 +1 값 아니면 그대로
            //                tempPrediction[j] = (added[j] + ((j == i) ? 1 : 0)) / (added.Sum() + 1);
            //            }
            //            double[] different = new double[tempPrediction.Length];
            //            for (int j = 0; j < different.Length; j++)
            //            {
            //                different[j] = Math.Abs(tempPrediction[j] - units[j].Rate);
            //            }

            //            if (minValue > different.Sum())
            //            {
            //                minValue = different.Sum();
            //                minIndex = i;
            //            }
            //        }


            //        //유닛 넣기
            //        //성공 - 다음
            //        //실패 - 1. 작은면적 들어갈 공간 있음 - 넣음
            //        //       2. 작은면적 들어갈 공간 없음 - 끝
            //        if (minIndex == -1)
            //            minIndex = 0;
            //        var result = aptLines[k].Add(units[minIndex]);
            //        if (result)
            //        {
            //            added[minIndex]++;
            //            //RhinoApp.WriteLine("{0} index added(by rate), result : {1},{2}", minIndex, added[0], added[1]);
            //        }

            //        else
            //        {

            //            bool nothingAdded = true;
            //            for (int i = units.Count - 1; i >= 0; i--)
            //            {
            //                var addResult = aptLines[k].Add(units[i]);
            //                if (addResult)
            //                {
            //                    added[i]++;
            //                    nothingAdded = false;
            //                    break;
            //                }
            //            }

            //            if (nothingAdded)
            //            {
            //                //홀수 tower 체크
            //                int towerCount = aptLines[k].Container.GetTypes().Where(n => n == UnitType.Tower).Count();

            //                //타워가 홀수개 존재하면
            //                if (towerCount % 2 != 0)
            //                {
            //                    int towerindex = aptLines[k].Container.Units.FindIndex(n => n.Type == UnitType.Tower);
            //                    aptLines[k].Container.Units.RemoveAt(towerindex);


            //                    for (int i = units.Count - 1; i >= 0; i--)
            //                    {
            //                        if (units[i].Type == UnitType.Tower)
            //                            continue;

            //                        while (true)
            //                        {
            //                            var addResult = aptLines[k].Add(units[i]);
            //                            if (addResult)
            //                            {
            //                                added[i]++;
            //                                nothingAdded = false;
            //                            }
            //                            else
            //                                break;
            //                        }
            //                    }

            //                }
            //                //RhinoApp.WriteLine("Nothing Added, Finish at {0}", k);
            //                if (nothingAdded)
            //                    break;
            //            }
            //        }
            //    }
            //}
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
        public List<Unit> Units { get; set; }
        public double Length { get { return GetLengthSum(); } }
        bool sorted = false;
        public UnitCollection()
        {
            Units = new List<Unit>();
        }

        public List<UnitType> GetTypes()
        {
            //SandwichClearanceAppropriately();
            return Units.Select(n => n.Type).ToList();
        }

        double GetLengthSum()
        {
            return Units.Sum(n => n.Length);
        }

        public void Add(Unit unit)
        {
            int insertIndex = Units.Count - 1;

            if (insertIndex == -1)
            {
                Units.Add(unit);
                return;
            }

            for (int i = 0; i < Units.Count; i++)
            {
                if (Units[i].Type == unit.Type)
                {
                    insertIndex = i;
                    break;
                }

                if (i == Units.Count - 1)
                {
                    Units.Add(unit);
                    return;
                }
            }
            Units.Insert(insertIndex, unit);
        }

        public void Remove(Unit unit)
        {
            Units.Remove(unit);
        }

        public List<double> Positions()
        {
            SandwichClearanceAppropriately();
            List<double> positions = new List<double>();
            positions.Add(0);
            for (int i = 0; i < Units.Count; i++)
            {
                double position = Units.Take(i + 1).Sum(n => n.Length);
                positions.Add(position);
            }
            //positions.Add(Length);
            return positions;
        }

        public List<bool> Clearances()
        {
            SandwichClearanceAppropriately();
            return Units.Select(n => n.Type == UnitType.Clearance ? true : false).ToList();
        }

        void SandwichClearanceAppropriately()
        {
            if (sorted)
                return;

            sorted = true;

            Units.Sort((Unit a, Unit b) => ((int)a.Type).CompareTo((int)b.Type));

            //패티 숫자를 구한다. add 하면서 자동으로 넣어준 패티들.
            int pattyCount = Units.Where(n => n.Type == UnitType.Clearance).Count();

            //번 길이!
            double corridorBunLength = Units.Where(n => n.Type == UnitType.Corridor).Sum(n => n.Length);
            double towerBunLength = Units.Where(n => n.Type == UnitType.Tower).Sum(n => n.Length);

            //각 번의 요소 count, 나중에 끼워넣을 위치 구하기 위함
            int corridorCount = Units.Where(n => n.Type == UnitType.Corridor).Count();
            int towerCount = Units.Where(n => n.Type == UnitType.Tower).Count();

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
            //        if (Units.Last().Type == UnitType.Clearance)
            //            Units.RemoveAt(Units.Count - 1);

            //    }
            //}

            //1.번이 두종류면 사이에 패티 위치시킴 

            if (corridorCount != 0 && towerCount != 0 && Length >= 70000)
            {
                int[] corr = GetStartEnd(UnitType.Corridor);
                Units.Insert(corr[1] + 1, new Unit(UnitType.Clearance));
                Units.RemoveAt(Units.Count - 1);
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
                Units.Insert(index, new Unit(UnitType.Clearance));
                Units.RemoveAt(Units.Count - 1);

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

                if (index == Units.Count)
                    break;

                Units.Insert(index, new Unit(UnitType.Clearance));
                Units.RemoveAt(Units.Count - 1);
            }



        }

        int[] GetStartEnd(UnitType type)
        {
            int[] indexes = new int[2] { -1, -1 };

            for (int i = 0; i < Units.Count; i++)
            {
                if (Units[i].Type == type)
                {
                    if (indexes[0] == -1)
                        indexes[0] = i;
                    indexes[1] = i;
                }
            }

            return indexes;
        }

        public List<Unit> ExpandableUnits()
        {
            return Units.Where(n => !n.AreaFixed && n.Maximum - n.Area > 0).ToList();
        }

        public List<Unit> ContractableUnits()
        {
            return Units.Where(n => !n.AreaFixed && n.Area - n.Minimum > 0).ToList();
        }
    }

    class ApartLine
    {
        public double TotalLength { get; set; }
        public UnitCollection Container { get; set; }
        public double LeftLength { get { return GetLeftLength(); } }
        double tempClearancePredict = 0;
        public ApartLine(double length)
        {
            TotalLength = length;
            Container = new UnitCollection();
        }

        double GetLeftLength()
        {
            return
              TotalLength - Container.Length;
        }

        public bool Add(Unit unit)
        {

            if (LeftLength >= unit.Length)
            {
                Container.Add(unit);

                if (ClearancePredict() != tempClearancePredict && unit.Type != UnitType.Clearance)
                {
                    tempClearancePredict = ClearancePredict();
                    Unit clearance = new Unit(UnitType.Clearance);
                    Container.Add(clearance);

                    if (LeftLength < 0)
                    {
                        Container.Remove(unit);
                        Container.Remove(clearance);
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

        public void ExpandUnits()
        {
            double tolerance = 5;
            double tempLeftLength = LeftLength;
            while (true)
            {
                List<Unit> expandables = Container.ExpandableUnits();

                if (expandables.Count == 0)
                    break;
                if (tempLeftLength <= tolerance)
                    break;

                double avr = tempLeftLength / expandables.Count;

                for (int i = 0; i < expandables.Count; i++)
                {
                    //평균값만큼 확장 후 남은값 받음
                    double left = expandables[i].Expand(avr);
                    //tempLeftLength 에서 확장한 값 만큼 
                    tempLeftLength -= (avr - left);
                }
            }
        }

        public int CoreCount()
        {
            int towers = Container.Units.Where(n => n.Type == UnitType.Tower).Count();
            int corridors = Container.Units.Where(n => n.Type == UnitType.Corridor).Count();

            return towers / 2 + (int)Math.Ceiling((double)corridors / 8);
        }

        public double SupplyAreaSum()
        {
            return Container.Units.Sum(n => n.Area);
        }

        public double FloorAreaSum()
        {
            return Container.Units.Sum(n => n.FloorArea);
        }

        public double ContractUnit(double length)
        {
            double tolerance = 5;
            double tempLeftLength = length;
            while (true)
            {
                List<Unit> contractable = Container.ContractableUnits();

                if (contractable.Count == 0)
                    break;
                if (tempLeftLength <= tolerance)
                    break;

                double avr = tempLeftLength / contractable.Count;

                for (int i = 0; i < contractable.Count; i++)
                {
                    //평균값만큼 축소 후 남은값 받음
                    double left = contractable[i].Contract(avr);
                    //tempLeftLength 에서 축소한 값 만큼 
                    tempLeftLength -= (avr - left);
                }
            }

            return tempLeftLength;
        }

    }

    public class Unit
    {
        public double FloorArea { get { return GetFloorArea(); } }
        public double Length { get; set; }
        public double Area { get; set; }
        public double Rate { get; set; }
        public UnitType Type { get; set; }
        public double Maximum { get; set; }
        public double Minimum { get; set; }
        public bool AreaFixed { get; set; }
        public int Required { get; set; }
        public int Supplied { get; set; }
        public double CoreArea { get ; set; }

        public Unit(UnitType type)
        {
            Length = 5000;
            Area = 0;
            Type = type;
            AreaFixed = true;
        }

        public Unit()
        {
           
        }

        public void Initialize()
        {
            if (Area < Consts.AreaLimit)
                Type = UnitType.Corridor;
            else
                Type = UnitType.Tower;

            Supplied = 0; 
        }

        public Unit GetFixed(bool areaLock)
        {
            Unit fixedUnit = new Unit();
            fixedUnit.Area = this.Area;
            fixedUnit.Maximum = this.Maximum;
            fixedUnit.Minimum = this.Minimum;
            fixedUnit.Length = this.Length;
            fixedUnit.AreaFixed = areaLock;
            fixedUnit.Type = this.Type;
            fixedUnit.CoreArea = this.CoreArea;

            if (areaLock)
            {
                fixedUnit.Maximum = fixedUnit.Area;
                fixedUnit.Minimum = fixedUnit.Area;
            }

            return fixedUnit;
            //아마 rate,required,supplied 는 필요없을듯
        }

        //d 만큼 expand. expand 하고 남은 길이 만큼 return
        public double Expand(double length)
        {
            //고정 유닛이라면 그대로 리턴
            if (AreaFixed)
                return length;

            double width = Area / Length;
            double expandable = (Maximum - Area)/width;
            if (expandable <= length)
            {
                //면적변경
                Area = Maximum;
                //길이변경
                Length += expandable;

                return length - expandable;
            }
            else
            {
                //면적변경
                Area = Area + length * width;
                //길이변경
                Length += length;
                return 0;
            }



        }

        public double Contract(double length)
        {
            //고정 유닛이라면 그대로 리턴
            if (AreaFixed)
                return length;

            double width = Area / Length;
            double contractable = (Area - Minimum)/width;
            if (contractable <= length)
            {
                Area = Minimum;
                //길이변경
                Length -= contractable;
                return length - contractable;
            }
            else
            {
                Area = Area - length * width;
                //길이변경
                Length -= length;
                return 0;
            }


        }

        private double GetFloorArea()
        {
            double area = Area;
            area -= CoreArea;
            if (Type == UnitType.Tower)
                area /= Consts.balconyRate_Tower;
            else if (Type == UnitType.Corridor)
                area /= Consts.balconyRate_Corridor;

            area += CoreArea;

            return area;
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
