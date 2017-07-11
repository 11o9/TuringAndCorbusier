using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rhino.Geometry;
using GISData.DataStruct;
using Rhino;
using TuringAndCorbusier.Utility;
using System.Data.SqlTypes;
namespace GISData.Extract
{
    public class SiteCounter
    {
        public static void Count(List<Pilji> selected)
        {
            DateTime time = DateTime.Now;
            
            List<string> codes = selected.Select(n => n.Code).ToList();
            var sqlFormattedDate = time.Date.ToString(string.Format("yyyy-MM-dd {0}:{1}:{2}",time.Hour,time.Minute,time.Second));
            
            foreach (var c in codes)
            {
                ServerConnection.WriteLog(c, sqlFormattedDate);
            }
            
        }
    }
}
