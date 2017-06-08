﻿using TuringAndCorbusier.Datastructure_Settings;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Interop;
using Rhino;
using Rhino.UI;
using System.Runtime.InteropServices;
using System;
using System.Text;
namespace TuringAndCorbusier
{
    ///<summary>
    /// <para>Every RhinoCommon .rhp assembly must have one and only one PlugIn-derived
    /// class. DO NOT create instances of this class yourself. It is the
    /// responsibility of Rhino to create an instance of this class.</para>
    /// <para>To complete plug-in information, please also see all PlugInDescription
    /// attributes in AssemblyInfo.cs (you might need to click "Project" ->
    /// "Show All Files" to see it in the "Solution Explorer" window).</para>
    ///</summary>
    public class TuringAndCorbusierPlugIn : Rhino.PlugIns.PlugIn
    {



        public class InstanceClass
        {
            public static bool ignoreNorthCurve = false;
            public static bool isSpecialCase = false;

            public static Page2 page2 = new Page2();
            public static Page3 page3 = new Page3();

            public static Corbusier navigationHost = new Corbusier();
            
            public static Plot plot;
            public static KDGinfo kdgInfoSet;


            public static Dictionary<string, string> pathsToUpload = new Dictionary<string, string>();
            /// <summary>
            /// 0518 민호 currentnewprojectwindow 정적 변수 생성
            /// </summary>
            public static MenuWindow theOnlyMenuWindow = new MenuWindow();
            //public static ModelingComplexity modelingComplexity = null;
            public static NewProjectWindow currentNewProjectWindow = null;
            public static Turing turing = null;

            public static Reports.showme showmewindow = new Reports.showme();

            public static RegulationSettings regSettings = new RegulationSettings();
            public static Settings_Page1 page1Settings = new Settings_Page1();
            public static Settings_Page2 page2Settings;
            public static List<FloorPlanLibrary> floorPlanLibrary = new List<FloorPlanLibrary>();

            public static ReportNavigation reportNavigationHose = new ReportNavigation();

            public static bool isfirst = true;

        }

        public TuringAndCorbusierPlugIn()
        {
            Instance = this;

        }

        ///<summary>Gets the only instance of the TuringAndCorbusierPlugIn plug-in.</summary>
        public static TuringAndCorbusierPlugIn Instance
        {
            get; private set;
        }



        protected override Rhino.PlugIns.LoadReturnCode OnLoad(ref string errorMessage)
        {
            //LoadManager.getInstance().SetDBFiles();



            System.Type panel_type = typeof(TuringHost);


            Rhino.UI.Panels.RegisterPanel(this, panel_type, "SPACEWALK_ApartmentGenerator", TuringAndCorbusier.Properties.Resources.Icon1);


            //Rhino.RhinoApp.Idle += idleEvent;
            Rhino.RhinoApp.Initialized += CallinitEvent;
            //(Rhino.UI.Panels.GetPanel(MainPaenlHost.PanelId) as Rhino.UI.Panels);
            return Rhino.PlugIns.LoadReturnCode.Success;
        }

        public void CallinitEvent(object sender, System.EventArgs e)
        {
            initEvent();
        }

        public void idleEvent(object sender, System.EventArgs e)
        {
            
            //System.Threading.Thread t1 = new System.Threading.Thread(new System.Threading.ThreadStart(CheckMemory));
            //t1.Start();
            
        }

        void CheckMemory()
        {
            if (GC.GetTotalMemory(false) > 2.1475e+9)
            {
                GC.Collect();
            }
            
        }
        public void initEvent()
        {

            

            if (isAdministrator())
                Rhino.RhinoApp.WriteLine("Admin");
            else
                Rhino.RhinoApp.WriteLine("NotAdmin");

            try
            {
                UIManager.getInstance().init();
            }
            catch (System.TypeInitializationException e1e)
            {
                System.Windows.Forms.MessageBox.Show(e1e.Message);
            }

            Rhino.ApplicationSettings.GeneralSettings.NewObjectIsoparmCount = -1;

            InstanceClass.turing.ProjectAddress.Text = "";// CommonFunc.getAddressFromServer(InstanceClass.turing.CurrentDataIdName.ToList(), InstanceClass.turing.CurrentDataId.ToList());

            Rhino.RhinoApp.Wait();

            Rhino.RhinoApp.Wait();

            //LoadManager.getInstance().importFileWithAdress();
            LoadManager.getInstance().LayerSetting();

            RhinoDoc.ActiveDoc.ModelUnitSystem = UnitSystem.Millimeters;
            Rhino.Render.RenderSettings r = new Rhino.Render.RenderSettings();
            //r.
            //Rhino.ApplicationSettings.FileSettings.TemplateFile = @"C://Users//user//AppData//Roaming//McNeel//Rhinoceros//5.0//Localization//en - US//Template Files//SPACEWALK - Millimeters.3dm";
            //string temptemplate = Rhino.ApplicationSettings.FileSettings.TemplateFile;
            ///6.3
            RhinoWindowSetUp.SetUIForTC(Rhino.RhinoDoc.ActiveDoc);
            RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.ChangeToParallelProjection(true);
            ///~6.3
            Rhino.RhinoApp.WriteLine("InitComplete");
        }

        public bool isAdministrator()
        {
            System.Security.Principal.WindowsIdentity identity = System.Security.Principal.WindowsIdentity.GetCurrent();
            if (identity != null)
            {
                System.Security.Principal.WindowsPrincipal principal = new System.Security.Principal.WindowsPrincipal(identity);


                return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
            }
            return false;
        }
    }

    [System.Runtime.InteropServices.Guid("8742B7C3-4BDF-4EAA-A242-D69C2C09988A")]
    public class TuringHost : RhinoWindows.Controls.WpfElementHost
    {
        public static TuringHost instance = null;

        public TuringHost() : base(new Turing(), null) // No view model (for this example)
        {
            instance = this;
        }

        public static System.Guid PanelId
        {
            get
            {
                return typeof(TuringHost).GUID;
            }
        }


        public void test()
        {

        }

    }

    
}