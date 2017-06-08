using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
namespace TuringAndCorbusier
{
    class ProgramInformation
    {
        public string Version = "1.010530";
        public string Company = "SPACEWALK";
        public string MessageURL = @"Data Source=spacewalk.koreasouth.cloudapp.azure.com,1433;Initial Catalog=mssql_System;USER ID=sa;PASSWORD=building39!";
        
        public string Message = "";

        public void GetMessage()
        {
            using (SqlConnection con = new SqlConnection(MessageURL))
            {
                try
                {
                    string query = @"SELECT [Message] FROM [mssql_System].[dbo].[Message] WHERE Version = '" + Version + "'";
                    con.Open();
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        using (SqlDataReader rdr = cmd.ExecuteReader())
                        {
                            while (rdr.Read())
                            {
                                try
                                {
                                    Message = rdr[0].ToString();
                                }
                                catch (Exception e)
                                {
                                    //RhinoApp.WriteLine(e.Message);
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    
                }
            }
        }
    }
}
