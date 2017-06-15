using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rhino.Geometry;
using Rhino.Display;
using Rhino.DocObjects;
using System.Drawing;
using GISData.DataStruct;
using System.ComponentModel;

namespace TuringAndCorbusier
{

    public class PlotPreview : DisplayConduit, INotifyPropertyChanged
    {
        List<Pilji> selectedPilji = new List<Pilji>();
        MultiPolygon Merged = null;

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
                handler(this, e);
        }

        protected void OnPropertyChanged(string propertyname)
        {
            OnPropertyChanged(new PropertyChangedEventArgs(propertyname));
        }

        public PlotPreview()
        {

        }

        public void Update(Pilji selected)
        {

            //포함되는것 - 제거
            if (Contain(selected))
            {
                selectedPilji.Remove(selected);
            }
            //포함되지않는것 - 추가
            else
            {
                //접하는가
                if (Border(selected))
                {
                    selectedPilji.Add(selected);
                }
                else
                {
                    selectedPilji.Clear();
                    selectedPilji.Add(selected);
                }
            }

            OnPropertyChanged("SelectedPilji");
            Merge();
        }

        bool Contain(Pilji selected)
        {
            bool result = false;
            foreach (var p in selectedPilji)
            {
                if (selected.Name == p.Name)
                {
                    result = true;
                    break;
                }
            }

            return result;
        }

        bool Border(Pilji selected)
        {
            bool isborder = false;
            foreach (var p in selectedPilji)
            {
                foreach (var reg1 in p.Region)
                {
                    foreach (var reg2 in selected.Region)
                    {
                        var result = Curve.PlanarCurveCollision(reg1, reg2, Plane.WorldXY, 1);
                        if (result)
                        {
                            if (Curve.CreateBooleanUnion(new Curve[] { reg1, reg2 }).Length == 1)
                            {
                                isborder = true;
                                break;
                            }
                        }
                    }

                    if (isborder)
                        break;
                }

                if (isborder)
                    break;
            }
            return isborder;
        }

        private void Merge()
        {
            if (selectedPilji.Count == 0)
                Merged = null;
            else if (selectedPilji.Count == 1)
            {
                Merged = selectedPilji[0].Shape;
            }
            else
            {
                Merged = PolygonTools.Merge(SelectedPilji.Select(n => n.Shape).ToList());
            }
        }

        protected override void DrawForeground(DrawEventArgs e)
        {

            //Draw(selectedPilji.Select(n => n.Outbound[0]).ToList(), Color.Gold, 10, e);
            if (Merged != null)
            {
                //Merged.Shape.ForEach(n=>e.Display.DrawPolyline(n, Color.Gold, 10));


                foreach (var p in Merged.Polygons)
                {
                    p.Subtractors.ForEach(n => e.Display.DrawPolyline(n, Color.Red, 10));
                    e.Display.DrawPolyline(p.OutBound, Color.Green, 10);
                }
                //Merged.Subtractors.ForEach(n=>e.Display.DrawPolyline(n, Color.Red, 10));
                // e.Display.DrawPolyline(Merged.OutBound, Color.Green, 10);
            }
        }

        private void Draw(List<Curve> crvs, Color color, int thickness, DrawEventArgs e)
        {
            Polyline[] polys = new Polyline[crvs.Count];
            crvs.ForEach(n => n.TryGetPolyline(out polys[crvs.IndexOf(n)]));
            polys.ToList().ForEach(n => e.Display.DrawPolyline(n, color, thickness));
        }


        public void Clear()
        {
            Merged = null;
            selectedPilji.Clear();
        }

        public List<Pilji> SelectedPilji
        {
            get { return selectedPilji; }
            set { if (value != selectedPilji) { selectedPilji = value; OnPropertyChanged("SelectedPilji"); } }
        }

        public MultiPolygon MergedPlot { get { return Merged; } }
        //public List<Curve> MergedOutlines { get { return MergedOutline(); } }
    }

    class RoughPlotPreview : DisplayConduit
    {
        Brep[] geometry = null;

        Point3d[,] points = null;


        protected override void DrawForeground(DrawEventArgs e)
        {
            if (geometry != null)
            {
                if (geometry.Length != 0)
                {
                    foreach (var g in geometry)
                    {
                        if (g == null)
                            continue;
                        e.Display.DrawBrepShaded(g, new DisplayMaterial(Color.Green, 0.4));
                    }
                }

            }

            if (points != null)
            {
                foreach (var i in points)
                {
                    e.Display.DrawPoint(i, PointStyle.X, 100, Color.Green);
                }
            }

        }

    }
}
