//Svitkin, 2021
using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Collections.Generic;
using System.Text;
using GISGKHIntegration;

namespace CommunalServices.Communication.Data
{
    public class DebtRequest
    {
        public string Number { get; set; }
        public string SubrequestGUID { get; set; }
        public string HouseGUID { get; set; }
        public string HouseAddress { get; set; }
        public string HouseNkv { get; set; }
        public DateTime RequestDate { get; set; }
        public DateTime? MailDate { get; set; }
        public bool IsAnswered { get; set; }
        public int KPost { get; set; }
        
        public static string GetExecutorGuid(int k_post)
        {
            SqlConnectionStringBuilder b = new SqlConnectionStringBuilder(
                DatabaseParams.curr.ConnectionString);
            b.InitialCatalog = "RIPO_UK";

            SqlConnection con = new SqlConnection(b.ConnectionString);

            object val;
            con.Open();
            using (con)
            {
                SqlCommand cmd;
                cmd = new SqlCommand(@"SELECT employeeGUID FROM DataProviders WHERE k_post=@k_post", con);
                cmd.Parameters.AddWithValue("k_post", k_post);
                val = cmd.ExecuteScalar();
                if (val == null || val == DBNull.Value) val = String.Empty;//normalize
                return val.ToString().ToLower();
            }
        }
        
        public static Tuple<int, string>[] GetOrganisations()
        {
            SqlConnection con = new SqlConnection(DatabaseParams.curr.ConnectionString);
            con.Open();

            List<Tuple<int, string>> res = new List<Tuple<int, string>>(100);
            Tuple<int, string> item;

            SqlCommand cmd = new SqlCommand(
                @"SELECT k_post,orgPPAGUID FROM ripo_uk.dbo.DataProviders WHERE k_post<>0 AND employeeGUID IS NOT NULL",
                con);

            SqlDataReader rd = cmd.ExecuteReader();

            using (rd)
            {
                while (true)
                {
                    if (rd.Read() == false) break;
                    item = new Tuple<int, string>(Convert.ToInt32(rd["k_post"]), rd["OrgPPAGUID"].ToString());
                    res.Add(item);
                }
            }

            return res.ToArray();
        }

    }
}
