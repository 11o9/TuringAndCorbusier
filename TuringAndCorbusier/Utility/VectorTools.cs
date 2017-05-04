using System;
using System.Collections.Generic;
using Rhino.Geometry;
using System.Linq;
using Rhino.Display;
using System.IO;

namespace TuringAndCorbusier.Utility
{
    class VectorTools
    {
        public static Vector3d RotateVectorXY(Vector3d baseVector, double angle)
        {
            Vector3d rotatedVector = new Vector3d(baseVector.X * Math.Cos(angle) - baseVector.Y * Math.Sin(angle), baseVector.X * Math.Sin(angle) + baseVector.Y * Math.Cos(angle), 0);
            return rotatedVector;
        }

        public static Vector3d ChangeCoordinate(Vector3d baseVector, Plane fromPln, Plane toPln)
        {
            Vector3d changedVector = baseVector;
            changedVector.Transform(Transform.ChangeBasis(fromPln, toPln));

            return changedVector;
        }

        /// <summary>
        /// 주어진 폴리라인 위의 한 세그먼트에 대해 폴리라인 내부로 향하는 단위벡터를 구해줍니다.
        /// </summary>
        public static Vector3d GetInnerPerpUnit(Line segment, Polyline boundary, double tolerance)
        {
            Vector3d perpVector = RotateVectorXY(segment.UnitTangent, Math.PI / 2);
            Vector3d perpVector2 = RotateVectorXY(segment.UnitTangent, -Math.PI / 2);
            Point3d basePt = segment.PointAt(0.5) + perpVector * tolerance;
            int decider = (int)boundary.ToNurbsCurve().Contains(basePt);

            if (decider == 1)
                return perpVector;

            return perpVector2;
        }

        /// <summary>
        /// 반시계방향, 동일평면, 동일시작점 기준으로 기준으로 현재 벡터가 두 벡터 사이에 있는지를 판별합니다. 평행한 경우 제외. 외적 방향 pre -> post
        /// </summary>
        public static bool IsBetweenVectorStrict(Vector3d preVector, Vector3d postVector, Vector3d testVector)
        {
            Vector3d toPreCross = Vector3d.CrossProduct(testVector, preVector);
            Vector3d toPostCross = Vector3d.CrossProduct(testVector, postVector);
            Vector3d preToPostCross = Vector3d.CrossProduct(preVector, postVector);

            bool IsToPostNegZAlign = Vector3d.Multiply(toPostCross, -Vector3d.ZAxis) > 0;
            bool IsToPreZAlign = Vector3d.Multiply(toPreCross, Vector3d.ZAxis) > 0;
            bool IsPreToPostZAlign = Vector3d.Multiply(preToPostCross, Vector3d.ZAxis) > 0;

            if (IsToPreZAlign && IsToPostNegZAlign)
                return true;

            if (IsToPreZAlign && !IsToPostNegZAlign)
            {
                if (IsPreToPostZAlign)
                    return true;
                return false;
            }

            if (!IsToPreZAlign && IsToPostNegZAlign)
            {
                if (IsPreToPostZAlign)
                    return true;
                return false;
            }
            
            return false;
        }

        /// <summary>
        /// 반시계방향, 동일평면, 동일시작점 기준으로 기준으로 현재 벡터가 두 벡터 사이에 있는지를 판별합니다. 평행한 경우 포함. 외적 방향 pre -> post
        /// </summary>
        public static bool IsBetweenVector(Vector3d preVector, Vector3d postVector, Vector3d testVector)
        {
            //unitize
            Vector3d preUnit = preVector / preVector.Length;
            Vector3d postUnit = postVector / postVector.Length;
            Vector3d testUnit = testVector / testVector.Length;

            //set
            Vector3d toPreCross = Vector3d.CrossProduct(testUnit, preUnit);
            Vector3d toPostCross = Vector3d.CrossProduct(testUnit, postUnit);
            Vector3d preToPostCross = Vector3d.CrossProduct(preUnit, postUnit);

            //check
            bool IsToPostNegZAlign = Vector3d.Multiply(toPostCross, -Vector3d.ZAxis) >= 0;
            bool IsToPreZAlign = Vector3d.Multiply(toPreCross, Vector3d.ZAxis) >= 0;
            bool IsPreToPostZAlign = Vector3d.Multiply(preToPostCross, Vector3d.ZAxis) >= 0;

            if (IsToPreZAlign && IsToPostNegZAlign)
                return true;

            if (IsToPreZAlign && !IsToPostNegZAlign)
            {
                if (IsPreToPostZAlign)
                    return true;
                return false;
            }

            if (!IsToPreZAlign && IsToPostNegZAlign)
            {
                if (IsPreToPostZAlign)
                    return true;
                return false;
            }

            return false;
        }


        /// <summary>
        /// 주어진 두 벡터가 반시계방향, 동일평면, 동일시작점 기준으로 볼록인지 오목인지를 알려줍니다. 외적 방향 pre -> post
        /// </summary>
        /// <param name="parallelTolerance">단위벡터 내적 및 외적 값 오차한계, 0.000~1.000</param>
        /// <returns></returns>
        public static Convexity CheckConvexity(Vector3d preVector, Vector3d postVector, double parallelTolerance)
        {
            //unitize
            Vector3d preUnit = preVector / preVector.Length;
            Vector3d postUnit = postVector / postVector.Length;

            //setting
            double prePostDot = Vector3d.Multiply(preVector, postVector);
            Vector3d prePostCross = Vector3d.CrossProduct(preVector, postVector);
            double crossZDot = Vector3d.Multiply(prePostCross, Vector3d.ZAxis);

            //check
            if(crossZDot<=-parallelTolerance)
                return Convexity.Convex;

            if (crossZDot >= parallelTolerance)
                return Convexity.Concave;

            if (prePostDot > 0)
                return Convexity.Parallel;

            return Convexity.AntiParallel;                        
        }

      

        /// <summary>
        /// 주어진 두 벡터 사이의 각이 둔각인지 예각인지를 알려줍니다.
        /// </summary>
        /// <param name="parallelTolerance">단위벡터 내적 값 오차한계, 0.000~1.000</param>
        /// <returns></returns>
        public static AngleType CheckAngleType(Vector3d preVector, Vector3d postVector, double parallelTolerance)
        {
            //unitize
            Vector3d preUnit = preVector / preVector.Length;
            Vector3d postUnit = postVector / postVector.Length;

            //setting
            double prePostDot = Vector3d.Multiply(preVector, postVector);

            //check
            if (prePostDot < -parallelTolerance)
            {
                if (prePostDot < parallelTolerance-1)
                    return AngleType.Straight;

                return AngleType.Obtuse;
            }
                    
            if (prePostDot > parallelTolerance)
            {
                if (prePostDot > 1 - parallelTolerance)
                    return AngleType.Zero;

                return AngleType.Acute;
            }

            return AngleType.Right;
        }

        /// <summary>
        /// 주어진 두 벡터 사이의 각이 반시계방향, 동일평면, 동일시작점 기준으로 둔각인지 예각인지를 알려줍니다. 외적 방향 pre -> post
        /// </summary>
        /// <param name="parallelTolerance">단위벡터 내적 및 외적 값 오차한계, 0.000~1.000</param>
        /// <returns></returns>
        public static AngleType CheckAngleTypeOriented(Vector3d preVector, Vector3d postVector, double parallelTolerance)
        {
            //unitize
            Vector3d preUnit = preVector / preVector.Length;
            Vector3d postUnit = postVector / postVector.Length;

            //setting
            double prePostDot = Vector3d.Multiply(preVector, postVector);
            Vector3d prePostCross = Vector3d.CrossProduct(preVector, postVector);
            double crossZDot = Vector3d.Multiply(prePostCross, Vector3d.ZAxis);

            //check
            if (crossZDot > parallelTolerance)
                return AngleType.Reentering;

            if (prePostDot <= -parallelTolerance)
            {
                if (prePostDot <= parallelTolerance - 1)
                    return AngleType.Straight;

                return AngleType.Obtuse;
            }

            if (prePostDot >= parallelTolerance)
            {
                if (prePostDot >= 1 - parallelTolerance)
                    return AngleType.Zero;

                return AngleType.Acute;
            }

           return AngleType.Right;
        }

        
    }

    public enum Convexity { Convex, Concave, Parallel, AntiParallel }
    public enum AngleType { Acute, Obtuse, Right, Straight, Zero, Reentering}
}
