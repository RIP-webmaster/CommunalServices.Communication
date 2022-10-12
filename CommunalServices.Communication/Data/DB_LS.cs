//Svitkin 2021
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Text;
using GISGKHIntegration;

namespace CommunalServices.Communication.Data
{
    public static class DB_LS
    {
        public static decimal GetNachislByRange(int k_s4, int god_start, int mes_start, int god_end, int mes_end)
        {
            SqlConnection con = new SqlConnection(DatabaseParams.curr.ConnectionString);
            con.Open();

            using (con)
            {
                SqlCommand cmd = new SqlCommand();
                string condition;

                if (god_end != god_start)
                {
                    condition =
                        " (god=@god_start AND mes>=@mes_start) OR (god>@god_start AND god<@god_end) OR (god=@god_end AND mes<=@mes_end) ";
                }
                else
                {
                    condition =
                        " (god=@god_start AND mes>=@mes_start AND mes<=@mes_end) ";
                }

                string query = @"SELECT sum(isnull(nach,0.0)+isnull(p_nach,0.0)) FROM [ripo].[dbo].[Rob]
where k_s4=@k_s4 and k_s1<>999 AND ( {0} )";
                cmd.CommandText = string.Format(query, condition);
                cmd.Connection = con;
                cmd.Parameters.AddWithValue("k_s4", k_s4);
                cmd.Parameters.AddWithValue("god_start", god_start);
                cmd.Parameters.AddWithValue("god_end", god_end);
                cmd.Parameters.AddWithValue("mes_start", mes_start);
                cmd.Parameters.AddWithValue("mes_end", mes_end);
                object val = cmd.ExecuteScalar();

                if (val == null || val == DBNull.Value) return 0.0M;
                else return Convert.ToDecimal(val);
            }
        }
    }

    public class DolgData
    {
        public int k_s4 { get; set; }
        public string Addm { get; set; }
        public string FIO { get; set; }
        public decimal Sum { get; set; }
        public decimal DolgUsl { get; set; }
    }
}
