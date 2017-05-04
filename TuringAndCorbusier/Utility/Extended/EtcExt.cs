using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TuringAndCorbusier.Utility
{
    static class EtcExtended
    {
        public static System.Data.DataTable Transpose(this System.Data.DataTable d)
        {
            System.Data.DataTable result = new System.Data.DataTable();

            result.Columns.Add("PropertyName");
            result.Columns.Add("Value");

            for (int i = 0; i < d.Columns.Count; i++)
            {
                var dtcolumn = d.Columns[i];
                var row = d.Rows[0].ItemArray[i];
                object[] data = new object[] {
                    dtcolumn,
                    row
                };
                result.Rows.Add(data);
            }

            return result;

        }
    }
}
