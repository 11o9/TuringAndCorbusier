using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace TuringAndCorbusier.cs
{

    [System.Runtime.InteropServices.Guid("0DD4FA36-B410-488D-BB69-1D8891A30274")]
    public class TestCommand : Rhino.Commands.Command
    {
       
        public override string EnglishName
        {
            get { return "Bumlae"; }
        }

        protected override Rhino.Commands.Result RunCommand(Rhino.RhinoDoc doc, Rhino.Commands.RunMode mode)
        {
            doc.Objects.AddTextDot(new Rhino.Geometry.TextDot("Special Thanks to - Bumlae", new Rhino.Geometry.Point3d(0, 0, 0)));
                return Rhino.Commands.Result.Success;
        }

    }

    public class Egg
    {
        public static Egg instance = null;
        public static double Time = 0;

        private Egg()
        {
            instance = this;

        }

        public void initevent()
        {
            Rhino.RhinoApp.Idle += SetTimeFlow;
            Rhino.RhinoApp.Idle += Idle;
        }

        public void SetTimeFlow(object sender, System.EventArgs e)
        {

            Time += 0.1f;
        }

        public void Idle(object sender, System.EventArgs e)
        {

            
        }


    }

    public class Vector3
    {
        public float x;
        public float y;
        public float z;
        public float Length
        {
            get { return Length; }
            set { Math.Sqrt(x * x + y * y + z * z); }
        }

        public Vector3(float _x, float _y, float _z)
        {
            x = _x;
            y = _y;
            z = _z;
        }

        public static Vector3 operator + (Vector3 vector1, Vector3 vector2)
        {
            return new Vector3(vector1.x + vector2.x, vector1.y + vector2.y, vector1.z + vector2.z);
        }

        public static Vector3 operator - (Vector3 vector1, Vector3 vector2)
        {
            return new Vector3(vector1.x - vector2.x, vector1.y - vector2.y, vector1.z - vector2.z);
        }

    }

    public class Physics
    {

        

        public static void Translate(Vector3 vector,float speed)
        {

        }

    }

}
