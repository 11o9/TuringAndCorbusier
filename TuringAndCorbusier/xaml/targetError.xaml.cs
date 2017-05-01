using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Rhino.Collections;
using Rhino.Geometry;

namespace TuringAndCorbusier
{
    /// <summary>
    /// errorMessage.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class targetError : Window
    {
        List<Apartment> returnValue = new List<Apartment>();
        ApartmentGeneratorBase AG = new AG1();
        Plot Plot = new Plot();
        ParameterSet ExistingParameter = new ParameterSet();
        Target ExistingTarget = new Target();

        public targetError()
        {
            InitializeComponent();
            this.Left = TuringAndCorbusierPlugIn.InstanceClass.navigationHost.Left + TuringAndCorbusierPlugIn.InstanceClass.navigationHost.Width / 2 - this.Width / 2;
            this.Top = TuringAndCorbusierPlugIn.InstanceClass.navigationHost.Top + TuringAndCorbusierPlugIn.InstanceClass.navigationHost.Height / 2 - this.Height / 2;

        }

        private List<Target> getProposedArea()
        {
            List<List<double>> targetArea = new List<List<double>>();
            List<List<double>> targetRatio = new List<List<double>>();

            targetArea.Add(new List<double>(new double[] { 29, 59, 84 }));
            targetRatio.Add(new List<double>(new double[] {1, 1, 1}));

            targetArea.Add(new List<double>(new double[] { 29, 59 }));
            targetRatio.Add(new List<double>(new double[] { 1, 2 }));

            targetArea.Add(new List<double>(new double[] { 29, 84 }));
            targetRatio.Add(new List<double>(new double[] { 1, 2 }));

            targetArea.Add(new List<double>(new double[] { 59, 84 }));
            targetRatio.Add(new List<double>(new double[] { 2, 1 }));

            targetArea.Add(new List<double>(new double[] { 59, 84 }));
            targetRatio.Add(new List<double>(new double[] { 1, 1 }));

            List<Target> output = new List<Target>();

            for(int i= 0; i < targetArea.Count(); i++)
                output.Add(new Target(targetArea[i], targetRatio[i]));

            return output;
        }

        public List<Apartment> showDialogAndReturnValue(ApartmentGeneratorBase AG, Plot plot, ParameterSet existingParameter, Target existingTarget,  List<Apartment> existingOutput)
        {
            returnValue = existingOutput;
            this.AG = AG;
            this.Plot = plot;
            this.ExistingParameter = existingParameter;
            this.ExistingTarget = existingTarget;

            this.ShowDialog();

            return returnValue;
        }

        private void createUnderGroundParking_Click(object sender, RoutedEventArgs e)
        {
            Apartment newAGoutput = this.AG.generator(this.Plot, this.ExistingParameter, this.ExistingTarget);

            List<Apartment> newAGOutputSet = FinalizeApartment.finalizeAGoutput(newAGoutput, TuringAndCorbusierPlugIn.InstanceClass.page1Settings.MaxFloorAreaRatio, TuringAndCorbusierPlugIn.InstanceClass.page1Settings.MaxBuildingCoverage, true);

            if (newAGOutputSet.Count != 0)
                returnValue = newAGOutputSet;

            this.Close();
        }

        private void useProposedArea_Click(object sender, RoutedEventArgs e)
        {
            List<Target> sortedTarget = SuggestTarget.bestTarget(this.ExistingTarget, getProposedArea());

            for(int i = 0; i < sortedTarget.Count(); i++ )
            {
                Apartment newAGoutput = this.AG.generator(this.Plot, this.ExistingParameter, this.ExistingTarget);
                List<Apartment> newAGOutputSet = FinalizeApartment.finalizeAGoutput(newAGoutput, TuringAndCorbusierPlugIn.InstanceClass.page1Settings.MaxFloorAreaRatio, TuringAndCorbusierPlugIn.InstanceClass.page1Settings.MaxBuildingCoverage, false);

                if (newAGOutputSet.Count != 0)
                {
                    returnValue = newAGOutputSet;
                    break;
                }
            }

            this.Close();
        }

        private void cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        class SuggestTarget
        {
            public static List<Target> bestTarget(Target now, List<Target> targetList)
            {
                List<double> scores = targetList.Select(n => fitness(n, now, 10)).ToList();
                RhinoList<Target> targetListCopy = new RhinoList<Target>(targetList);
                targetListCopy.Sort(scores.ToArray());

                return targetListCopy.ToList();
            }

            private static double fitness(Target a, Target b, double adjust)
            {
                try
                {
                    double score = 0;
                    for (int i = 0; i < a.Area.Count; i++)
                    {
                        double iScore = 0;
                        for (int j = 0; j < b.Area.Count; j++)
                        {
                            double dist = Math.Pow(Math.Abs(a.Area[i] - b.Area[j]) + adjust, 1);
                            iScore += ratioScaling(a.Ratio)[i] * ratioScaling(b.Ratio)[j] / dist;
                        }
                        score += 1 / iScore;
                    }
                    return score;
                }
                catch(Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                    return 0;
                }
            }

            private static List<double> ratioScaling(List<double> beforeRatio)
            {
                List<double> output = beforeRatio.Select(n => n / beforeRatio.Sum()).ToList();
                return output;
            }
        }
    }
}
