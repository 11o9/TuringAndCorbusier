﻿using System;
using System.Collections.Generic;
using System.Linq;
using Rhino.Geometry;
using Rhino.Collections;

namespace TuringAndCorbusier
{
    class GiantAnteater
    {
        public static List<Apartment> giantAnteater(Plot plot, ApartmentGeneratorBase ag, Target target, bool previewOn)
        {
            double mutationProbability = ag.GAParameterSet[0];
            double elitismPercentage = ag.GAParameterSet[1];
            double initialBoost = ag.GAParameterSet[2];
            int population = (int)ag.GAParameterSet[3];
            int maxGen = (int)ag.GAParameterSet[4];
            double fitnessFactor = ag.GAParameterSet[5];
            double mutationFactor = ag.GAParameterSet[6];

            //Initialize Minimum and Maximum value





            double[] tempMaxInput = ag.MaxInput.Clone() as double[];
            double[] tempMinInput = ag.MinInput.Clone() as double[];


            tempMaxInput[0] = TuringAndCorbusierPlugIn.InstanceClass.page1Settings.MaxFloors;
            tempMinInput[0] = 3;
            tempMaxInput[1] = TuringAndCorbusierPlugIn.InstanceClass.page1Settings.MaxFloors;
            tempMinInput[1] = 3;

            //create initial genes
            Random myRandom = new Random((int)DateTime.Now.Ticks);
            double goodAngle = maxRectDirection(plot);

            //double goodAngle = Math.PI * 152 / 180;

            List<ParameterSet> offspringGenes = new List<ParameterSet>();

            //for (int i = 0; i < initialBoost; i++)
            //{
            //    CoreType tempCoreType = ag.GetRandomCoreType();

            //    double[] oneGene = new double[ag.MinInput.Length];

            //    //if (ag.IsCoreProtrude)
            //    //    tempMaxInput[2] = tempCoreType.GetDepth();

            //    for (int j = 0; j < ag.MinInput.Length; j++)
            //    {
            //        if (i % 2 == 0 && j == 3 && ag.GetType() == typeof(AG1))
            //        {
            //            oneGene[j] = goodAngle;
            //        }
            //        else
            //        {
            //            double parameterForGene = (tempMaxInput[j] - tempMinInput[j]) * myRandom.NextDouble() + tempMinInput[j];
            //            oneGene[j] = parameterForGene;
            //        }

            //        oneGene[0] = Math.Floor(oneGene[0]);
            //        oneGene[1] = Math.Floor(oneGene[1]);

            //    }

            //    ParameterSet a = new ParameterSet(oneGene, ag.GetType().ToString(), tempCoreType);
            //    offspringGenes.Add(a);
            //}

            double otherAngles = population * initialBoost;//(population - 1) * initialBoost;
            
            for (int i = 0; i < (int)otherAngles; i++)
            {
                CoreType tempCoreType = ag.GetRandomCoreType();

                double[] oneGene = new double[ag.MinInput.Length];

                //if (ag.IsCoreProtrude)
                //    tempMaxInput[2] = tempCoreType.GetDepth();

                for (int j = 0; j < ag.MinInput.Length; j++)
                {
                    if (/*i % 2 == 0 &&*/ j == 3 && ag.GetType() == typeof(AG1))
                    {
                        //double parameterForGene = ((goodAngle + Math.PI / 2 * (i % 4) / 2) % (Math.PI * 2) + Math.PI * ((int)(i / 4) % 2) % (Math.PI * 2));
                        //oneGene[j] = parameterForGene;
                        double mainAngle = Math.PI * i / otherAngles;
                        oneGene[j] = mainAngle;
                    }
                    else
                    {
                        double parameterForGene = (tempMaxInput[j] - tempMinInput[j]) * myRandom.NextDouble() + tempMinInput[j];

                        //width - 100씩
                        if (j == 2)
                            parameterForGene = Math.Round(parameterForGene / 100) * 100;
                        oneGene[j] = parameterForGene;
                    }

                    oneGene[0] = Math.Floor(oneGene[0]);
                    oneGene[1] = Math.Floor(oneGene[1]);

                }

                ParameterSet a = new ParameterSet(oneGene);
                offspringGenes.Add(a);
            }

            //initializing end condition
            bool endCondition = true;

            //start genetic algorithm
            int genCount = 0;
            ParameterSet bestGene = offspringGenes[0];

            while (endCondition)
            {
                //evaluate fitness`
                List<List<double>> evaluation = new List<List<double>>(evaluateFitness(plot, ag, target, offspringGenes, fitnessFactor, previewOn));
                List<double> fitnessValues = new List<double>(evaluation[0]);

                //sort genes and fitness values
                RhinoList<ParameterSet> myRhinoList = new RhinoList<ParameterSet>(offspringGenes);
                myRhinoList.Sort(fitnessValues.ToArray());
                myRhinoList.Reverse();
                fitnessValues.Sort();
                fitnessValues.Reverse();
                offspringGenes = myRhinoList.ToList();

                var radcheck = offspringGenes.Select(n => n.Parameters[3]);
                /*
                //write
                Rhino.RhinoApp.WriteLine(genCount.ToString());
                Rhino.RhinoApp.WriteLine(evaluation[1][0].ToString());
                Rhino.RhinoApp.WriteLine(evaluation[1][offspringGenes.Count-1].ToString());
                ParameterSet geneToShow = offspringGenes[0];

                for (int i = 0; i < geneToShow.Parameters.Length; i++)
                {
                    Rhino.RhinoApp.WriteLine(geneToShow.Parameters[i].ToString());
                }
                */

                //create new generation
                List<ParameterSet> tempGenes = new List<ParameterSet>();

                //Add elites to new generation
                int eliteNum = (int)(population * elitismPercentage);
                for (int i = 0; i < eliteNum; i++)
                {
                    tempGenes.Add(offspringGenes[i]);
                }

                //crossover & mutation
                for (int i = 0; i < population - eliteNum; i++)
                {
                    ParameterSet newOffspring = crossover(offspringGenes, fitnessValues, (int)myRandom.Next(0, int.MaxValue), ag.GetType().ToString());
                    if (myRandom.NextDouble() < mutationProbability)
                    {
                        newOffspring = mutation(newOffspring, ag, mutationFactor, (int)myRandom.Next(0, int.MaxValue));
                    }
                    tempGenes.Add(newOffspring);
                }
                offspringGenes = tempGenes;

                genCount += 1;
                if (genCount == maxGen)
                {
                    endCondition = false;
                }

                GC.Collect();
                //Rhino.RhinoApp.Wait();

                //finalize before end
                if (endCondition == false)
                {
                    //evaluate fitness
                    evaluation = new List<List<double>>(evaluateFitness(plot, ag, target, offspringGenes, fitnessFactor, previewOn));
                    fitnessValues = new List<double>(evaluation[0]);

                    //sort genes and fitness values
                    myRhinoList = new RhinoList<ParameterSet>(offspringGenes);
                    myRhinoList.Sort(fitnessValues.ToArray());
                    myRhinoList.Reverse();
                    fitnessValues.Sort();
                    fitnessValues.Reverse();
                    offspringGenes = myRhinoList.ToList();



                    bestGene = offspringGenes[0];
                }

                Rhino.RhinoApp.Wait();
            }

            //best 1
            Apartment bestOutput = ag.generator(plot, bestGene, target);
            return new Apartment[] { bestOutput }.ToList();

            //best 5
            //var uniqueGenes = offspringGenes.Distinct();
            Apartment[] bestOutputs = offspringGenes.Take(5).Select(n => ag.generator(plot, n, target)).ToArray();
            //return bestOutputs.ToList();


            //best 10
            //var uniqueGenes = offspringGenes.Distinct();
            //Apartment[] bestOutputs = offspringGenes.Take(10).Select(n=>ag.generator(plot, n, target)).ToArray();
            //return new Apartment[] { bestOutput }.ToList();
            //return bestOutputs.ToList();

            //all
            //Apartment[] bestOutputs = offspringGenes.Select(n=>ag.generator(plot, n, target)).ToArray();
            //return bestOutputs.ToList();

            if (bestOutput.ParameterSet == null)
                return FinalizeApartment.finalizeAGoutput(bestOutput, TuringAndCorbusierPlugIn.InstanceClass.page1Settings.MaxFloorAreaRatio, TuringAndCorbusierPlugIn.InstanceClass.page1Settings.MaxBuildingCoverage, false);

            if (bestOutput.ParameterSet.Parameters != null)
            {
                List<Apartment> output = FinalizeApartment.finalizeAGoutput(bestOutput, TuringAndCorbusierPlugIn.InstanceClass.page1Settings.MaxFloorAreaRatio, TuringAndCorbusierPlugIn.InstanceClass.page1Settings.MaxBuildingCoverage, false);

                bool IsSatisfyingLegalParking = false;

                List<Apartment> satisFyingLegalParkingOutput = new List<Apartment>();

                foreach (Apartment i in output)
                {
                    if (i.GetLegalParkingLotOfCommercial() + i.GetLegalParkingLotofHousing() < i.ParkingLotOnEarth.GetCount() + i.ParkingLotUnderGround.Count)
                    {
                        satisFyingLegalParkingOutput.Add(i);
                        IsSatisfyingLegalParking = true;
                    }
                }

                if (IsSatisfyingLegalParking == false)
                {
                    targetError tempErrorMessage = new targetError();
                    List<Apartment> tempNewOutput = tempErrorMessage.showDialogAndReturnValue(ag, plot, bestGene, target, output);

                    bool tempSatisfyingLegalParking = false;

                    List<Apartment> tempSatisFyingLegalParkingOutput = new List<Apartment>();

                    foreach (Apartment i in tempNewOutput)
                    {
                        if (i.GetLegalParkingLotOfCommercial() + i.GetLegalParkingLotofHousing() < i.ParkingLotOnEarth.GetCount() + i.ParkingLotUnderGround.Count)
                        {
                            tempSatisfyingLegalParking = true;
                            tempSatisFyingLegalParkingOutput.Add(i);
                        }
                    }

                    if (tempSatisfyingLegalParking == false)
                    {
                        System.Windows.MessageBox.Show("선택한 설계로 법정 주차대수를 만족하기 어려운 대지입니다.");

                        return output;
                    }
                    else
                    {
                        return tempSatisFyingLegalParkingOutput;
                    }
                }
                else
                {
                    return output;
                }
            }
            else
            {
                System.Windows.MessageBox.Show(CommonFunc.GetApartmentType(ag.GetAGType) + " 타입 설계에 적합하지 않은 대지입니다.");
            }

            return FinalizeApartment.finalizeAGoutput(bestOutput, TuringAndCorbusierPlugIn.InstanceClass.page1Settings.MaxFloorAreaRatio, TuringAndCorbusierPlugIn.InstanceClass.page1Settings.MaxBuildingCoverage, false);
        }

        public static List<List<List<Household>>> CloneHhp(List<List<List<Household>>> cloneBase)
        {
            List<List<List<Household>>> output = new List<List<List<Household>>>();

            for (int i = 0; i < cloneBase.Count(); i++)
            {
                List<List<Household>> tempOutput = new List<List<Household>>();

                for (int j = 0; j < cloneBase[i].Count(); j++)
                {
                    List<Household> tempTempOutput = new List<Household>();

                    for (int k = 0; k < cloneBase[i][j].Count(); k++)
                    {
                        tempTempOutput.Add(cloneBase[i][j][k]);
                    }

                    tempOutput.Add(tempTempOutput);
                }

                output.Add(tempOutput);
            }

            return output;
        }
        public static List<List<Core>> CloneCP(List<List<Core>> cloneBase)
        {
            List<List<Core>> output = new List<List<Core>>();

            for (int i = 0; i < cloneBase.Count(); i++)
            {
                List<Core> tempOutput = new List<Core>();

                for (int j = 0; j < cloneBase[i].Count(); j++)
                {
                    tempOutput.Add(new Core(cloneBase[i][j]));
                }

                output.Add(tempOutput);
            }

            return output;
        }


        private static double parametersetStoriesChange(double existingStory, double newStory)
        {
            if (existingStory < newStory)
                return existingStory;
            else
                return newStory;
        }

        private static List<List<double>> evaluateFitness(Plot plot, ApartmentGeneratorBase ag, Target target, List<ParameterSet> gene, double fitnessFactor, bool previewOn)
        {
            List<List<double>> result = new List<List<double>>();
            List<double> grossAreaRatio = new List<double>();
            List<double> parkinglotRatio = new List<double>();
            List<double> targetAccuracy = new List<double>();
            List<double> axisAccuracy = new List<double>();
            bool drawSomeThing = false;
            int i = 0;

            var timer = new System.Threading.Timer((e) =>
            {
                drawSomeThing = true;
            }
            , drawSomeThing, TimeSpan.Zero, TimeSpan.FromMilliseconds(1000));

            List<Apartment> agOutToDrawList = new List<Apartment>();
            List<double> agOutToDrawGrossAreaList = new List<double>();

            for (i = 0; i < gene.Count(); i++)
            {
                Apartment tempOutput = ag.generator(plot, gene[i], target);
                grossAreaRatio.Add(tempOutput.GetGrossAreaRatio());
                //targetAccuracy.Add(tempOutput.GetTargetAccuracy());
                parkinglotRatio.Add(tempOutput.GetParkingScore());
                axisAccuracy.Add(tempOutput.GetAxisAccuracy());
                TuringAndCorbusierPlugIn.InstanceClass.page3.currentProgressFactor += 1;
                TuringAndCorbusierPlugIn.InstanceClass.page3.updateProGressBar(TuringAndCorbusierPlugIn.InstanceClass.page3.currentProgressFactor.ToString() + "/" + TuringAndCorbusierPlugIn.InstanceClass.page3.currentWorkQuantity.ToString() + " 진행중");

                if (previewOn)
                {
                    agOutToDrawList.Add(tempOutput);
                    //agOutToDrawGrossAreaList.Add(tempOutput.GetGrossAreaRatio());

                    if (drawSomeThing)
                    {
                        TuringAndCorbusierPlugIn.InstanceClass.page3.preview(agOutToDrawList.Last());
                        Rhino.RhinoDoc.ActiveDoc.Views.Redraw();
                        Rhino.RhinoApp.Wait();

                        agOutToDrawList.Clear();
                        agOutToDrawGrossAreaList.Clear();
                        drawSomeThing = false;
                    }
                }

            }

            double MaxFAR = TuringAndCorbusierPlugIn.InstanceClass.page1Settings.MaxFloorAreaRatio;

            //법정 용적률에 근접한 유전자가 가장 높은 값 받음,
            // -0 ~ -5 최대값 받음
            //넘어가면 * 0.01 포인트 마이너스
            //낮으면 *0.1포인트 마이너스

            List<double> points = new List<double>();
            for (int r = 0; r < grossAreaRatio.Count; r++)
            {
                double point = 0;
                if (grossAreaRatio[r] < MaxFAR && grossAreaRatio[r] > MaxFAR - 5)
                    point = 1 + (5 - (MaxFAR - grossAreaRatio[r]))/MaxFAR ;
                else if (grossAreaRatio[r] > MaxFAR)
                    point = 1 - (grossAreaRatio[r] - MaxFAR) /MaxFAR /10;
                else
                    point = 1 - (MaxFAR - grossAreaRatio[r]) /MaxFAR;
                points.Add(point);
            }

            List<double> tempGAR = new List<double>(grossAreaRatio);
            RhinoList<double> FARList = new RhinoList<double>(tempGAR);

            //FARList.Sort(points.ToArray());
            //points.Sort();

            double Cworst = FARList.First;
            double Cbest = FARList.Last;
            double k = fitnessFactor;
            List<double> fitness = new List<double>();
            //double Cworst = tempGAR[0];
            //double Cbest = tempGAR[gene.Count - 1];
            //double k = fitnessFactor;
            //List<double> fitness = new List<double>();

            double parkingWeight = 0.3;
            double CworstR = parkinglotRatio.Min();
            double CbestR = parkinglotRatio.Max();


            double targetWeight = 0.4;
            //double CworstT = targetAccuracy.Min();
            //double CbestT = targetAccuracy.Max();

            //fitnessvalue rate
            //연면적 1 : 주차율(?) 0.3 : 유닛정확도 : 0.4


            //for test 
            string url = @"C://Users//user//Desktop//test";
            System.IO.DirectoryInfo dinfo = new System.IO.DirectoryInfo(url);

            string filename = "//garojutacklogV2" + (dinfo.GetFiles().Length + 1).ToString() + ".csv";
            System.IO.FileStream fs = new System.IO.FileStream(url + filename, System.IO.FileMode.CreateNew);
            fs.Close();
            fs.Dispose();
            System.IO.StreamWriter w = new System.IO.StreamWriter(fs.Name);
            string column = "FAR" + "," + "FARPoint" + "," + "ParkingPoint" + "," + "AxisPoint" + "," + "Sum" + "," + "Use1F" + ",UseSetback";
            w.WriteLine(column);
            for (int j = 0; j < gene.Count; j++)
            {
                double farfitnessVal = points[j]*10;
                double parkkingfitnessVal = parkinglotRatio[j];
                double axisfitnessVal = axisAccuracy[j];

                //double farfitnessVal = ((grossAreaRatio[j] - Cbest) * (k - 1) / k + (Cbest - Cworst) + 0.01) / (Cbest - Cworst + 0.01);
                //double parkingval = ((parkinglotRatio[j] - CbestR) * (k - 1) / k + (CbestR - CworstR) + 0.01) / (CbestR - CworstR + 0.01) * parkingWeight;
                //double targetval = ((targetAccuracy[j] - CbestT) * (k - 1) / k + (CbestT - CworstT) + 0.01) / (CbestT - CworstT + 0.01) * targetWeight;

                //setback test
                double setbackBonus = 0;
                //if (gene[j].setback)
                //    setbackBonus = 100;
                fitness.Add(farfitnessVal + parkkingfitnessVal + axisfitnessVal+ setbackBonus);
                //for test
                string format = grossAreaRatio[j].ToString() + "," + farfitnessVal + "," + parkkingfitnessVal + "," + axisfitnessVal + "," + (farfitnessVal+ parkkingfitnessVal + axisfitnessVal).ToString() + "," + gene[j].using1F.ToString() + "," + gene[j].setback.ToString();
                w.WriteLine(format);
            }
            //for test
            w.Close();
            w.Dispose();


            //tempGAR.Reverse();

            //fitness.Reverse();
            //FARList.Reverse();

            result.Add(fitness);
            result.Add(FARList.Select(n=>n).ToList());

            double maxfar = FARList.Max();
            double maxfitness = fitness.Max();

            TuringAndCorbusierPlugIn.InstanceClass.page3.updateProGressBar(TuringAndCorbusierPlugIn.InstanceClass.page3.currentProgressFactor.ToString() + "/" + TuringAndCorbusierPlugIn.InstanceClass.page3.currentWorkQuantity.ToString() + " 완료");


            return result;

            //or, return nested list, containing gross area ratio.
        }

        private static ParameterSet crossover(List<ParameterSet> gene, List<double> fitnessVal, int randomSeed, string agType)
        {
            Random r = new Random(randomSeed);

            //roullette wheel
            double fitnessSum = 0;
            for (int i = 0; i < fitnessVal.Count; i++)
            {
                fitnessSum += fitnessVal[i];
            }

            double[] roullette = { r.NextDouble(), r.NextDouble() };
            List<int> select = new List<int>();
            for (int i = 0; i < 2; i++)
            {
                int j = -1;
                double sum = 0;
                while (sum < roullette[i] * fitnessSum)
                {
                    j += 1;
                    sum += fitnessVal[j];
                }
                select.Add(j);
            }

            if (select[0] == -1)
                select[0] = r.Next(gene.Count - 1);
            if (select[1] == -1)
                select[1] = r.Next(gene.Count - 1);
            //crossover
            List<double> newGene = new List<double>();
            int geneLen = gene[select[0]].Parameters.Length;
            int crossPoint = r.Next(geneLen + 1);
            int geneMarker = 0;
            int whichGene = 0;

            while (geneMarker != geneLen)
            {
                if (geneMarker == crossPoint)
                {
                    whichGene = 1;
                }
                newGene.Add(gene[select[whichGene]].Parameters[geneMarker]);
                geneMarker += 1;
            }

            ParameterSet resultGene = new ParameterSet(newGene.ToArray());
            return resultGene;
        }

        private static ParameterSet mutation(ParameterSet gene, ApartmentGeneratorBase ag, double mutationFactor, int randomSeed)
        {
            //random gaussian
            Random rand = new Random(randomSeed); //reuse this if you are generating many
            bool offBound = true;
            double randStdNormal = double.MaxValue;
            while (offBound)
            {
                double u1 = rand.NextDouble(); //these are uniform(0,1) random doubles
                double u2 = rand.NextDouble();
                randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2); //random normal(0,1)
                if (randStdNormal > -mutationFactor && randStdNormal < mutationFactor)
                    offBound = false;
            }

            double fitThreeSigma = Math.Max(-mutationFactor, randStdNormal);
            fitThreeSigma = Math.Min(mutationFactor, fitThreeSigma);
            fitThreeSigma /= (mutationFactor * 2);

            //mutation
            List<double> newGene = new List<double>();
            for (int i = 0; i < gene.Parameters.Length; i++)
            {
                double parameterDomainLength = ag.MaxInput[i] - ag.MinInput[i];
                double mutatedParameter = gene.Parameters[i] + parameterDomainLength * fitThreeSigma;
                mutatedParameter = Math.Max(ag.MinInput[i], mutatedParameter);
                mutatedParameter = Math.Min(ag.MaxInput[i], mutatedParameter);
                if(i <= 1)
                    mutatedParameter = Math.Floor(mutatedParameter);
                //width - 100 씩
                if (i == 2)
                    mutatedParameter = Math.Round(mutatedParameter / 100) * 100;
                newGene.Add(mutatedParameter);
                
            }
            ParameterSet resultGene = new ParameterSet(newGene.ToArray());
            return resultGene;
        }

        private static double maxRectDirection(Plot plot)
        {
            Polyline x;
            plot.Boundary.TryGetPolyline(out x);

            //parameters
            int gridResolution = 23;
            int angleResolution = 23;
            int ratioResolution = 5;
            int binarySearchIteration = 7;
            double ratioMaximum = 100;
            int gridSubResolution = 4;
            int angleSubResolution = 3;
            int ratioSubResolution = 4;
            int subIter = 4;
            double convergenceFactor = 1.1;

            double maxR = ratioMaximum;
            int binaryIter = binarySearchIteration;
            int rRes = ratioResolution;
            double alpha = 0.01;
            int edgeAngleNum = 5;

            //sub constants
            double gridDom = 0.3;
            double angleDom = 0.3;
            double ratioDom = 0.3;

            //plot
            Polyline xcurve = x;
            Curve plotCurve = xcurve.ToNurbsCurve();

            //loop start parameters
            double areaMax = 0;

            double solWidth = 0;
            Point3d solPoint = new Point3d(0, 0, 0);
            double solR = 15;
            double solAngle = 0;

            //output
            List<Point3d> coreOri = new List<Point3d>();
            List<Rectangle3d> coreShape = new List<Rectangle3d>();

            //search space
            List<double> angles = new List<double>();
            for (int i = 0; i < angleResolution; i++)
            {
                angles.Add(Math.PI * 2 * i / angleResolution);
            }
            List<Line> outlineSegments = x.GetSegments().ToList();
            RhinoList<Line> outSegRL = new RhinoList<Line>(outlineSegments);
            List<double> outlineSegLength = outlineSegments.Select(n => n.Length).ToList();
            outSegRL.Sort(outlineSegLength.ToArray());
            outlineSegments = outSegRL.ToList();
            outlineSegments.Reverse();
            for (int i = 0; i < Math.Min(edgeAngleNum, outlineSegments.Count); i++)
            {
                Vector3d vector = outlineSegments[i].UnitTangent;
                if (vector.Y < 0)
                    vector.Reverse();
                double angleTemp = -Math.Acos(Vector3d.Multiply(Vector3d.XAxis, vector));
                angles.Add(angleTemp);
                angles.Add(angleTemp + Math.PI / 2);
            }

            //loop
            for (int ang = 0; ang < angles.Count; ang++)
            {
                Polyline xClone = new Polyline(x);
                xClone.Transform(Transform.Rotation(angles[ang], new Point3d(0, 0, 0)));

                //find bounding box, grid dimensions
                var bBox = xClone.BoundingBox;
                Point3d minP = bBox.Min;
                Point3d maxP = bBox.Max;
                List<double> ressG = new List<double>();
                ressG.Add(maxP.X - minP.X - 2 * alpha);
                ressG.Add(maxP.Y - minP.Y - 2 * alpha);
                List<double> resG = new List<double>(ressG.Select(val => val / gridResolution));

                //1st search
                for (int i = 0; i < gridResolution + 1; i++)
                {
                    //create lines
                    Line lineY = new Line(new Point3d(minP.X + alpha + i * resG[0], minP.Y, 0), new Point3d(minP.X + alpha + i * resG[0], maxP.Y, 0));
                    Line lineX = new Line(new Point3d(minP.X, minP.Y + i + alpha * resG[1], 0), new Point3d(maxP.X, minP.Y + alpha + i * resG[1], 0));

                    //create mid points of segments
                    List<Point3d> midsX = new List<Point3d>(intersectionMids(lineX, xClone));
                    List<Point3d> midsY = new List<Point3d>(intersectionMids(lineY, xClone));
                    List<Point3d> mids = new List<Point3d>();
                    foreach (Point3d j in midsX)
                    {
                        mids.Add(j);
                    }
                    foreach (Point3d j in midsY)
                    {
                        mids.Add(j);
                    }

                    foreach (Point3d j in mids)
                    {
                        //get max height and max width
                        Line midlineY = new Line(new Point3d(j.X, minP.Y, 0), new Point3d(j.X, maxP.Y, 0));
                        Line midlineX = new Line(new Point3d(minP.X, j.Y, 0), new Point3d(maxP.X, j.Y, 0));
                        List<Point3d> widthP = new List<Point3d>(intersectionPoints(midlineX, xClone));
                        List<Point3d> heightP = new List<Point3d>(intersectionPoints(midlineY, xClone));
                        List<double> widthV = new List<double>();
                        List<double> heightV = new List<double>();

                        foreach (Point3d k in widthP)
                        {
                            widthV.Add(k.DistanceTo(j));
                        }
                        foreach (Point3d k in heightP)
                        {
                            heightV.Add(k.DistanceTo(j));
                        }

                        double maxWidth;
                        double maxHeight;
                        if (widthV.Count == 0)
                            maxWidth = 0;
                        else
                            maxWidth = widthV.Min() * 2;
                        if (heightV.Count == 0)
                            maxHeight = 0;
                        else
                            maxHeight = heightV.Min() * 2;

                        //binary search
                        double minRatio = Math.Max(areaMax / maxWidth / maxWidth, 1 / maxR);
                        double maxRatio = Math.Min(maxHeight * maxHeight / areaMax, maxR);

                        double r = 1;

                        if (minRatio < maxRatio)
                        {
                            for (int a = -rRes; a < rRes + 1; a++)
                            {

                                if (a < 0)
                                {
                                    r = 1 / (1 + (1 / minRatio - 1) / Math.Pow(2, -a));
                                }
                                else if (a == 1)
                                {
                                    r = 1;
                                }
                                else
                                {
                                    r = 1 + (maxRatio - 1) / Math.Pow(2, a);
                                }

                                //solution boundary

                                List<double> minWidths = new List<double>();
                                minWidths.Add(Math.Sqrt(areaMax / r));

                                double minSolWidth = minWidths.Max();
                                double maxSolWidth = Math.Min(maxWidth, maxHeight / r);
                                double searchWidth = (minSolWidth + maxSolWidth) / 2;
                                double binRes = searchWidth / 2;
                                //binary
                                if (minSolWidth < maxSolWidth)
                                {
                                    for (int b = 0; b < binaryIter; b++)
                                    {
                                        if (inCheck(j, searchWidth, r, xClone) == 1)
                                        {
                                            if (areaMax < searchWidth * searchWidth * r && searchWidth * searchWidth * r * 4 < PolygonArea(xcurve))
                                            {

                                                areaMax = searchWidth * searchWidth * r;
                                                solWidth = searchWidth;
                                                solPoint = j;
                                                solR = r;
                                                solAngle = angles[ang];

                                            }

                                            searchWidth += binRes;
                                        }
                                        else
                                        {
                                            searchWidth -= binRes;
                                        }
                                        binRes /= 2;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            Rectangle3d ohGod = new Rectangle3d(Plane.WorldXY, new Point3d(solPoint.X - solWidth, solPoint.Y - solWidth * solR, 0), new Point3d(solPoint.X + solWidth, solPoint.Y + solWidth * solR, 0));
            ohGod.Transform(Transform.Rotation(-solAngle, new Point3d(0, 0, 0)));

            //2nd search
            double solWidthNew = solWidth;
            Point3d solPointNew = solPoint;
            double solRNew = solR;
            double solAngleNew = solAngle;
            int token = 0;

            while (token < subIter)
            {
                gridDom /= convergenceFactor;
                angleDom /= convergenceFactor;
                ratioDom /= convergenceFactor;
                for (int ang = -angleSubResolution; ang < angleSubResolution + 1; ang++)
                {
                    double searchAngle = solAngle + ang * Math.PI / 2 * angleDom / 2 / angleSubResolution;
                    Polyline xCloneNew = new Polyline(x);
                    xCloneNew.Transform(Transform.Rotation(searchAngle, new Point3d(0, 0, 0)));
                    Curve xCloneNewCurve = xCloneNew.ToNurbsCurve();

                    //find bounding box, grid dimensions
                    var bBox = xCloneNew.BoundingBox;

                    Point3d minP = bBox.Min;
                    Point3d maxP = bBox.Max;
                    List<double> ressG = new List<double>();
                    ressG.Add(maxP.X - minP.X);
                    ressG.Add(maxP.Y - minP.Y);
                    List<double> resG = new List<double>(ressG.Select(val => val / (2 * gridSubResolution) * gridDom));
                    List<Point3d> mids = new List<Point3d>();
                    for (int i = -gridSubResolution; i < gridSubResolution + 1; i++)
                    {
                        for (int ii = -gridSubResolution; ii < gridSubResolution + 1; ii++)
                        {
                            Point3d solPointTemp = solPoint;
                            //solPointTemp.Transform(Transform.Rotation(searchAngle, new Point3d(0, 0, 0)));
                            Point3d gridPoint = new Point3d(solPointTemp.X + i * resG[0], solPointTemp.Y + ii * resG[1], 0);
                            if (xCloneNewCurve.Contains(gridPoint) == PointContainment.Inside)
                            {
                                mids.Add(gridPoint);
                            }
                        }
                    }

                    foreach (Point3d j in mids)
                    {
                        //get max height and max width
                        Line midlineY = new Line(new Point3d(j.X, minP.Y, 0), new Point3d(j.X, maxP.Y, 0));
                        Line midlineX = new Line(new Point3d(minP.X, j.Y, 0), new Point3d(maxP.X, j.Y, 0));
                        List<Point3d> widthP = new List<Point3d>(intersectionPoints(midlineX, xCloneNew));
                        List<Point3d> heightP = new List<Point3d>(intersectionPoints(midlineY, xCloneNew));
                        List<double> widthV = new List<double>();
                        List<double> heightV = new List<double>();
                        foreach (Point3d k in widthP)
                        {
                            widthV.Add(k.DistanceTo(j));
                        }
                        foreach (Point3d k in heightP)
                        {
                            heightV.Add(k.DistanceTo(j));
                        }
                        double maxWidth = widthV.Min();
                        double maxHeight = heightV.Min();

                        //binary search

                        double r;

                        for (int a = -ratioSubResolution; a < ratioSubResolution + 1; a++)
                        {

                            r = solR * (1 + a * ratioDom / ratioSubResolution);
                            //solution boundary
                            List<double> minWidths = new List<double>();
                            minWidths.Add(Math.Sqrt(areaMax / r));

                            double minSolWidth = minWidths.Max();
                            double maxSolWidth = Math.Min(maxWidth, maxHeight / r);
                            double searchWidth = (minSolWidth + maxSolWidth) / 2;
                            double binRes = searchWidth / 2;
                            //binary
                            if (minSolWidth < maxSolWidth)
                            {
                                for (int b = 0; b < binaryIter; b++)
                                {
                                    if (inCheck(j, searchWidth, r, xCloneNew) == 1)
                                    {
                                        if (areaMax < searchWidth * searchWidth * r && searchWidth * searchWidth * r * 4 < PolygonArea(xcurve))
                                        {

                                            areaMax = searchWidth * searchWidth * r;
                                            solWidthNew = searchWidth;
                                            solPointNew = j;
                                            solRNew = r;
                                            solAngleNew = searchAngle;

                                        }

                                        searchWidth += binRes;
                                    }
                                    else
                                    {
                                        searchWidth -= binRes;
                                    }
                                    binRes /= 2;
                                }
                            }
                        }
                    }
                }
                solWidth = solWidthNew;
                solPoint = solPointNew;
                solR = solRNew;
                solAngle = solAngleNew;
                token += 1;
            }

            solPoint.Transform(Transform.Rotation(-solAngle, new Point3d(0, 0, 0)));


            Rectangle3d ohGoditsworkingNew = new Rectangle3d(Plane.WorldXY, new Point3d(solPointNew.X - solWidthNew, solPointNew.Y - solWidthNew * solRNew, 0), new Point3d(solPointNew.X + solWidthNew, solPointNew.Y + solWidthNew * solRNew, 0));
            ohGoditsworkingNew.Transform(Transform.Rotation(-solAngleNew, new Point3d(0, 0, 0)));
            Rectangle3d outlineRectangle = ohGoditsworkingNew;
            List<Point3d> pts = new List<Point3d>(outlineRectangle.ToPolyline());
            Vector3d vectorT = Vector3d.Subtract(new Vector3d(pts[0]), new Vector3d(pts[1]));
            if (vectorT.Y < 0)
                vectorT.Reverse();
            double outAngleTemp = -Math.Acos(Vector3d.Multiply(Vector3d.XAxis, Vector3d.Multiply(vectorT, 1 / vectorT.Length)));

            return outAngleTemp;
        }

        private static List<Point3d> intersectionMids(Line gridL, Polyline outline)
        {
            List<Point3d> no = new List<Point3d>();
            List<Point3d> mids = new List<Point3d>();
            Curve curveA = gridL.ToNurbsCurve();
            Curve curveB = outline.ToNurbsCurve();
            List<Point3d> points = new List<Point3d>();
            int k = Rhino.Geometry.Intersect.Intersection.CurveCurve(curveA, curveB, 0, 0).Count;
            for (int i = 0; i < k; i++)
            {
                points.Add(Rhino.Geometry.Intersect.Intersection.CurveCurve(curveA, curveB, 0, 0)[i].PointA);
            }
            if (points.Count % 2 == 1)
            {
                return no;
            }
            else
            {
                for (int i = 0; i < points.Count; i += 2)
                {
                    mids.Add(new Point3d((points[i].X + points[i + 1].X) / 2, (points[i].Y + points[i + 1].Y) / 2, 0));
                }
                return mids;
            }

        }
        private static List<Point3d> intersectionPoints(Line gridL, Polyline outline)
        {
            List<Point3d> no = new List<Point3d>();
            List<Point3d> mids = new List<Point3d>();
            Curve curveA = gridL.ToNurbsCurve();
            Curve curveB = outline.ToNurbsCurve();
            List<Point3d> points = new List<Point3d>();
            int k = Rhino.Geometry.Intersect.Intersection.CurveCurve(curveA, curveB, 0, 0).Count;
            for (int i = 0; i < k; i++)
            {
                points.Add(Rhino.Geometry.Intersect.Intersection.CurveCurve(curveA, curveB, 0, 0)[i].PointA);
            }
            if (points.Count % 2 == 1)
            {
                return no;
            }
            else
            {
                return points;
            }

        }
        private static int inCheck(Point3d midPoint, double width, double ratio, Polyline outline)
        {
            Rectangle3d rect = new Rectangle3d(Plane.WorldXY, new Point3d(midPoint.X - width, midPoint.Y - width * ratio, 0), new Point3d(midPoint.X + width, midPoint.Y + width * ratio, 0));
            List<Point3d> no = new List<Point3d>();
            List<Point3d> mids = new List<Point3d>();
            Curve curveA = rect.ToNurbsCurve();
            Curve curveB = outline.ToNurbsCurve();
            List<Point3d> points = new List<Point3d>();
            int k = Rhino.Geometry.Intersect.Intersection.CurveCurve(curveA, curveB, 0, 0).Count;
            if (k == 0)
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }
        private static double PolygonArea(Polyline x)
        {
            List<Point3d> y = new List<Point3d>(x);
            double area = 0;
            for (int i = 0; i < y.Count - 1; i++)
            {
                area += y[i].X * y[i + 1].Y;
                area -= y[i].Y * y[i + 1].X;
            }

            area /= 2;
            return (area < 0 ? -area : area);
        }


    }
}