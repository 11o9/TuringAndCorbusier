using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rhino.Geometry;

namespace TuringAndCorbusier.Utility
{ 
    public class VectorSet
    {
        Vector3d xVector;
        Vector3d yVector;
        double xLength;
        double yLength;

        /// <summary>
        /// 직사각형만 사용하3
        /// 장축 = y , 단축 = x
        /// </summary>
        /// <param name="poly"></param>
        public VectorSet(Polyline poly)
        {
            //last = first
            //closed polyline
            var tempvector = new Vector3d(poly[poly.Count - 2] - poly.First);
            var tempvector2 = new Vector3d(poly[1] - poly.First);

            double d = tempvector.Length - tempvector2.Length;

            xVector = d > 0 ? tempvector2 : tempvector;
            yVector = d > 0 ? tempvector : tempvector2;
            xLength = xVector.Length;
            yLength = yVector.Length;

            xVector.Unitize();
            yVector.Unitize();
        }
        /// <summary>
        /// 주차구획 xy식별용
        /// </summary>
        /// <param name="poly"></param>
        /// <param name="forParking"></param>
        public VectorSet(Polyline poly, bool forParking)
        {
            if (forParking)
            {

                var tempvector = new Vector3d(poly[poly.Count - 2] - poly.First);
                var tempvector2 = new Vector3d(poly[1] - poly.First);


                bool d = false;


                if (tempvector.Length == 5000 && tempvector2.Length == 2300)
                    d = false;
                else if (tempvector.Length == 2000 && tempvector2.Length == 5000)
                    d = true;
                else if (tempvector.Length == 2000 && tempvector2.Length == 6000)
                    d = false;
                else if (tempvector.Length == 6000 && tempvector2.Length == 2000)
                    d = true;

                xVector = d ? tempvector2 : tempvector;
                yVector = d ? tempvector : tempvector2;
                xLength = xVector.Length;
                yLength = yVector.Length;

                xVector.Unitize();
                yVector.Unitize();

            }
            else { new VectorSet(poly); }

        }

        public Vector3d UnitX { get { return xVector; } }
        public Vector3d UnitY { get { return yVector; } }
        public Vector3d XVector { get { return xVector * xLength; } }
        public Vector3d YVector { get { return yVector * yLength; } }


    }
}
