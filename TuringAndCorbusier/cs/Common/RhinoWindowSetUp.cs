using System;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rhino;
using System.IO;



//using TuringAndCorbusier.BuildCDG;


namespace TuringAndCorbusier
{
    public class RhinoWindowSetUp
    {

        public static RhinoWindowSetUp instance = null;
        public static List<Guid> closedpanel = new List<Guid>();
        public static bool isvisible = false;
        public static List<string> asdfasdf = new List<string>();
        

        private RhinoWindowSetUp()
        {
            if (instance != null)
                instance = this;

        }

        //레이어 검정 => 하양
        public static void LayerColorChange(RhinoDoc doc,Color layercolor)
        {
            Color defaultcolor = Color.Gold;
            Color tempcolor = Color.Black;
            if (layercolor == Color.Black)
            
                tempcolor = Color.White;
           

            var matching_layers = (from layer in doc.Layers
                                   where layer.Color == layercolor
                                   select layer).ToList<Rhino.DocObjects.Layer>();

            Rhino.DocObjects.Layer layer_to_change = null;
            if (matching_layers.Count == 0)
            {
                RhinoApp.WriteLine("Layer" + layercolor.ToKnownColor() + "does not exist.");

            }


            else if (matching_layers.Count > 0)
            {

                
                for (int i = 0; i < matching_layers.Count; i++)
                {

                    if (matching_layers[i].Name == "Default")
                    {
                        matching_layers[i].Color = Color.Gold;
                        matching_layers[i].CommitChanges();
                        continue;
                    }
                       


                    layer_to_change = matching_layers[i];

                    var obj = doc.Objects.FindByLayer(layer_to_change);
                   
                    foreach (Rhino.DocObjects.RhinoObject c in obj)
                    {
                        if (c.Attributes.ColorSource == Rhino.DocObjects.ObjectColorSource.ColorFromLayer)
                            continue;
                        c.Attributes.ColorSource = Rhino.DocObjects.ObjectColorSource.ColorFromLayer;
                        c.CommitChanges();
                    }

                    
                    layer_to_change.Color = tempcolor;
                    layer_to_change.CommitChanges();
                    

                }
            }
        }


        

        public static void ResetUI(RhinoDoc doc)
        {

            

            LayerColorChange(doc,Color.White);
            //Rhino.Render.RenderContent[] rendercontents = Rhino.Render.UI.UserInterfaceSection.FromWindow(RhinoApp.MainWindow()).GetContentList();
            //for (int i = 0; i < rendercontents.Length; i++)
            //{
            //    RhinoApp.WriteLine(rendercontents[i].Name);

            //}
            //RhinoWindows.Forms.WindowsInterop.

            //System.Windows.WindowState asdf = System.Windows.WindowState.Maximized;

            //editregi("normal_start", "14474460");
            //editregi("normal_end", "14474460");
            //editregi("hot_start", "14474460");
            //editregi("normal_border", "14474460");
            Rhino.ApplicationSettings.AppearanceSettings.SetPaintColor(Rhino.ApplicationSettings.PaintColor.NormalBorder, Color.LightGray);
            Rhino.ApplicationSettings.AppearanceSettings.SetPaintColor(Rhino.ApplicationSettings.PaintColor.NormalEnd, Color.LightGray);
            Rhino.ApplicationSettings.AppearanceSettings.SetPaintColor(Rhino.ApplicationSettings.PaintColor.NormalStart, Color.LightGray);
            Rhino.ApplicationSettings.AppearanceSettings.SetPaintColor(Rhino.ApplicationSettings.PaintColor.HotStart, Color.LightGray);
            Rhino.ApplicationSettings.AppearanceSettings.SetPaintColor(Rhino.ApplicationSettings.PaintColor.TextEnabled, Color.Black);
            Rhino.ApplicationSettings.AppearanceSettings.SetPaintColor(Rhino.ApplicationSettings.PaintColor.TextDisabled, Color.Gray);
            Rhino.ApplicationSettings.AppearanceSettings.CommandPromptBackgroundColor = Color.White;
            Rhino.ApplicationSettings.AppearanceSettings.CommandPromptTextColor = Color.Black;

            Rhino.ApplicationSettings.AppearanceSettings.ViewportBackgroundColor = Color.LightGray;

            

            Rhino.UI.ToolbarFileCollection collection = RhinoApp.ToolbarFiles;

            foreach (string i in asdfasdf)
            {
                collection.Open(i);
            }

            asdfasdf.Clear();

            Rhino.ApplicationSettings.ModelAidSettings.DisplayControlPolygon = false;

            foreach (Guid i in closedpanel)
            {
                Rhino.UI.Panels.OpenPanel(i);
            }

            Rhino.UI.ToolbarFileCollection.SidebarIsVisible = true;


            Rhino.DocObjects.Tables.ViewTable viewinfo = doc.Views;
            foreach (Rhino.Display.RhinoView i in viewinfo)
            {
                Rhino.Display.RhinoViewport vp = i.ActiveViewport;
                

                if (vp.Name == "Perspective")
                {
                    //currentview.Add(i);
                    
                    vp.ParentView.Maximized = false;
                    vp.ConstructionGridVisible = true;
                    Rhino.Display.DisplayModeDescription dm = vp.DisplayMode;
                    if (dm.EnglishName != "Shaded")
                    {
                        Rhino.Display.DisplayModeDescription[] dms = Rhino.Display.DisplayModeDescription.GetDisplayModes();

                        for (int j = 0; j < dms.Length; j++)
                        {
                            string english_name = dms[j].EnglishName;
                            english_name = english_name.Replace("_", "");
                            english_name = english_name.Replace(" ", "");
                            english_name = english_name.Replace("-", "");
                            english_name = english_name.Replace(",", "");
                            english_name = english_name.Replace(".", "");

                            if (english_name == "Shaded")
                                vp.DisplayMode = Rhino.Display.DisplayModeDescription.FindByName(dms[j].EnglishName);



                        }
                    }

                }
                else
                {
                    //   i.Close();
                }
            }
            isvisible = true;







        }


        public static void SetUIForTC(RhinoDoc doc)
        {

            isvisible = false;
            //editregi("normal_start", "0");
            //editregi("normal_end", "0");
            //editregi("hot_start", "0");
            //editregi("normal_border", "0");

            /////프레임색변경!
            //Rhino.ApplicationSettings.AppearanceSettings.SetPaintColor(Rhino.ApplicationSettings.PaintColor.NormalBorder, Color.Black);
            //Rhino.ApplicationSettings.AppearanceSettings.SetPaintColor(Rhino.ApplicationSettings.PaintColor.NormalEnd, Color.Black);
            //Rhino.ApplicationSettings.AppearanceSettings.SetPaintColor(Rhino.ApplicationSettings.PaintColor.NormalStart, Color.Black);
            //Rhino.ApplicationSettings.AppearanceSettings.SetPaintColor(Rhino.ApplicationSettings.PaintColor.HotStart, Color.Black);
            //Rhino.ApplicationSettings.AppearanceSettings.SetPaintColor(Rhino.ApplicationSettings.PaintColor.TextDisabled, Color.Gray);
            //Rhino.ApplicationSettings.AppearanceSettings.SetPaintColor(Rhino.ApplicationSettings.PaintColor.TextEnabled, Color.White);
            //Rhino.ApplicationSettings.AppearanceSettings.CommandPromptBackgroundColor = Color.Black;
            //Rhino.ApplicationSettings.AppearanceSettings.CommandPromptTextColor = Color.Gold;

            //Rhino.ApplicationSettings.AppearanceSettings.ViewportBackgroundColor = Color.Black;
            //Rhino.ApplicationSettings.AppearanceSettings.FrameBackgroundColor = Color.Black;

            //Rhino.ApplicationSettings.AppearanceSettings.CurrentLayerBackgroundColor = Color.Black;


            //툴바 닫기
            Rhino.UI.ToolbarFileCollection toolbars = RhinoApp.ToolbarFiles;

            for (int i = 0; i < toolbars.ToArray().Length; i++)
            {
                asdfasdf.Add(toolbars[i].Path);
                RhinoApp.WriteLine(toolbars[i].Path);
                toolbars[i].Close(false);
            }

            //


            //패널 닫기
            List<Guid> panelids = new List<Guid>();
            panelids.AddRange(Rhino.UI.Panels.GetOpenPanelIds());
            //Guid thisguid = new Guid("2DAC4903-E95B-4BD3-9591-7E8C03F3F1F7");

            Guid thisguid = TuringHost.PanelId;
            foreach (Guid i in panelids)
            {
                if (i != thisguid)
                {
                    closedpanel.Add(i);
                    Rhino.UI.Panels.ClosePanel(i);
                }
            }

            if (!Rhino.UI.Panels.IsPanelVisible(TuringHost.PanelId))
            Rhino.UI.Panels.OpenPanel(TuringHost.PanelId);
            Guid currentPanel = TuringHost.PanelId;


            
            

            
            
            

            RhinoApp.WriteLine("PROJECT BOUNDLESS-X");
            RhinoApp.WriteLine("BY BOUNDLESS");
            RhinoApp.WriteLine("Ver 2.10 For SH");
            RhinoApp.WriteLine("Load Complete");

            //
            //레이어 검정색 -> 하얀색
            LayerColorChange(doc, Color.Black);





            Rhino.DocObjects.Tables.ViewTable viewinfo = doc.Views;                 //ActiveDoc사용 문제시 수정
                                                                                    //List < Rhino.Display.RhinoView > currentview = new List<Rhino.Display.RhinoView>();
            

            //perspective maximizing
            foreach (Rhino.Display.RhinoView i in viewinfo)
            {

                

                Rhino.Display.RhinoViewport vp = i.ActiveViewport;

                
                Guid dpmguid = Guid.Empty;
                

                if (vp.Name == "Perspective")
                {
                    //currentview.Add(i)

                    vp.WorldAxesVisible = false;
                    vp.ParentView.Maximized = true;
                    Rhino.Display.DisplayModeDescription dm = vp.DisplayMode;
                    if (dm.EnglishName != "Shaded")
                    {
                        Rhino.Display.DisplayModeDescription[] dms = Rhino.Display.DisplayModeDescription.GetDisplayModes();

                        for (int j = 0; j < dms.Length; j++)
                        {
                            string english_name = dms[j].EnglishName;
                            english_name = english_name.Replace("_", "");
                            english_name = english_name.Replace(" ", "");
                            english_name = english_name.Replace("-", "");
                            english_name = english_name.Replace(",", "");
                            english_name = english_name.Replace(".", "");

                            if (english_name == "Shaded")
                            {
 
                                vp.DisplayMode = Rhino.Display.DisplayModeDescription.FindByName(dms[j].EnglishName);

                            }

                        }
                    }
                }
                else
                {
                    //   i.Close();
                }
            }




        }



    }

}
