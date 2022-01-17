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
        public static DolgData[] GetDolgData(string houseGuid, string t_nomer_kv, int god, int mes)
        {
            if (t_nomer_kv.StartsWith("ком."))
            {
                t_nomer_kv = t_nomer_kv.Replace("ком.", string.Empty).Trim();
            }
            else if (t_nomer_kv.Contains("ком."))
            {
                int index = t_nomer_kv.IndexOf("ком.");

                if (index >= 2)
                {
                    t_nomer_kv = t_nomer_kv.Substring(0, index).Trim();
                }
            }

            List<DolgData> ret = new List<DolgData>();
            SqlConnection con = new SqlConnection(DatabaseParams.curr.ConnectionString);
            con.Open();

            using (con)
            {
                SqlCommand cmd = new SqlCommand();

                cmd.CommandText = @"
SELECT addm,k_s4,fio, 
(SELECT sum(saldo_n+p_saldo_n-(oplt+p_oplt)) FROM [ripo].[dbo].[rob] 
WHERE k_s4=rls.k_s4 AND god=rls.god AND mes=rls.mes AND k_s1<>999) AS ""Dolg"" 
FROM [ripo_uk].[dbo].[houses] INNER JOIN ripo.dbo.rls 
ON houses.street=rls.t_s5_name AND houses.nhouse=rls.t_dom 
WHERE god=@god and mes=@mes and house_guid=@house_guid and t_nomer_kv=@t_nomer_kv";

                cmd.Connection = con;
                cmd.CommandTimeout = 2 * 60;
                cmd.Parameters.AddWithValue("house_guid", houseGuid);
                cmd.Parameters.AddWithValue("t_nomer_kv", t_nomer_kv);
                cmd.Parameters.AddWithValue("god", god);
                cmd.Parameters.AddWithValue("mes", mes);
                SqlDataReader rd = cmd.ExecuteReader();

                while (true)
                {
                    if (rd.Read() == false) break;

                    DolgData d = new DolgData();
                    d.k_s4 = Convert.ToInt32(rd["k_s4"]);
                    d.Addm = rd["addm"].ToString().Trim();

                    string fio = string.Empty;

                    if (!rd.IsDBNull(rd.GetOrdinal("fio")))
                    {
                        fio = rd["fio"].ToString();
                    }

                    if (fio == null) d.FIO = string.Empty;
                    else d.FIO = fio.Trim();

                    if (!rd.IsDBNull(rd.GetOrdinal("Dolg")))
                    {
                        d.Sum = Convert.ToDecimal(rd["Dolg"]);
                    }

                    ret.Add(d);
                }
            }//end using

            return ret.ToArray();
        }
    }

    public class DolgData
    {
        public int k_s4 { get; set; }
        public string Addm { get; set; }
        public string FIO { get; set; }
        public decimal Sum { get; set; }
    }
}
