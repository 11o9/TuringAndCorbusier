using System;
using System.Collections.Generic;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;

namespace TuringAndCorbusier
{
    [System.Runtime.InteropServices.Guid("5b128afe-34bb-4d73-b822-532ad59cfb19")]
    public class TuringAndCorbusierCommand : Command
    {
        public TuringAndCorbusierCommand()
        {
            // Rhino only creates one instance of each command class defined in a
            // plug-in, so it is safe to store a refence in a static property.
            Instance = this;
        }

        ///<summary>The only instance of this command.</summary>
        public static TuringAndCorbusierCommand Instance
        {
            get; private set;
        }

        ///<returns>The command name as it appears on the Rhino command line.</returns>
        public override string EnglishName
        {
            get { return "TuringAndCorbusierCommand"; }
        }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            
            //TuringAndCorbusierPlugIn.Instance.initEvent();
            System.Guid panelId = TuringHost.PanelId;
            bool bVisible = Rhino.UI.Panels.IsPanelVisible(panelId);
            /*
            string prompt = (bVisible)
              ? "Sample panel is visible. New value"
              : "Sample Manager panel is hidden. New value";

            Rhino.Input.Custom.GetOption go = new Rhino.Input.Custom.GetOption();
            int hide_index = go.AddOption("Hide");
            int show_index = go.AddOption("Show");
            int toggle_index = go.AddOption("Toggle");

            go.Get();
            if (go.CommandResult() != Rhino.Commands.Result.Success)
                return go.CommandResult();

            Rhino.Input.Custom.CommandLineOption option = go.Option();
            if (null == option)
                return Rhino.Commands.Result.Failure;

            int index = option.Index;

            if (index == hide_index)
            {
                if (bVisible)
                    Rhino.UI.Panels.ClosePanel(panelId);
            }
            else if (index == show_index)
            {
                if (!bVisible)
                    Rhino.UI.Panels.OpenPanel(panelId);
            }
            else if (index == toggle_index)
            {*/
            if (bVisible)
                Rhino.UI.Panels.ClosePanel(panelId);
            else
                Rhino.UI.Panels.OpenPanel(panelId);
            // }

            


            return Result.Success;
        }
    }
}
