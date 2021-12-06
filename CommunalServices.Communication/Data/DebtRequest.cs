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
        
        public static string ProcessRequests(DebtRequest[] requests, string title, int god, int mes, 
            RequestsFilter filter, out DebtRequest[] result)
        {
            List<DebtRequest> requests_selected = new List<DebtRequest>(requests.Length);
            StringBuilder sb = new StringBuilder(1000);
            sb.AppendLine("Запросы о наличии задолженности, подтвержденной судебным актом: " + title);

            switch (filter)
            {
                case RequestsFilter.HasDebt: sb.AppendLine("(с долгами)"); break;
                case RequestsFilter.NoDebt: sb.AppendLine("(без долгов)"); break;
            }

            if (filter == RequestsFilter.All)
            {
                sb.AppendLine("Всего запросов: " + requests.Length);
            }

            sb.AppendLine(DateTime.Now.ToString());
            sb.AppendLine();

            sb.Append("|Номер запроса  |");
            sb.Append("Адрес                              |");
            sb.Append("ЛС      |");
            sb.Append("ФИО                                |");
            sb.Append("Долг      |");
            sb.AppendLine();

            sb.Append("|---------------|");
            sb.Append("-----------------------------------|");
            sb.Append("--------|");
            sb.Append("-----------------------------------|");
            sb.Append("----------|");
            sb.AppendLine();

            for (int i = 0; i < requests.Length; i++)
            {
                DolgData[] dolgdata = DB_LS.GetDolgData(requests[i].HouseGUID, requests[i].HouseNkv, god, mes);

                if (dolgdata.Length == 0)
                {
                    sb.Append("|");
                    sb.Append(requests[i].Number.PadLeft(15, ' ') + "|");

                    string addr = "???, кв. " + requests[i].HouseNkv;

                    sb.Append(addr.PadRight(35, ' ') + "|");
                    sb.Append("(Нет ЛС)|");
                    sb.AppendLine();
                    continue;
                }

                bool has_dolg = false;

                if (filter != RequestsFilter.All)
                {
                    for (int j = 0; j < dolgdata.Length; j++)
                    {
                        if (dolgdata[j].Sum > 0.0M)
                        {
                            has_dolg = true; break;
                        }
                    }
                }

                for (int j = 0; j < dolgdata.Length; j++)
                {
                    switch (filter)
                    {
                        case RequestsFilter.HasDebt: if (!has_dolg) continue;
                            break;
                        case RequestsFilter.NoDebt: if (has_dolg) continue;
                            break;
                    }

                    sb.Append("|");
                    sb.Append(requests[i].Number.PadLeft(15, ' ') + "|");

                    string addr = dolgdata[j].Addm + ", кв. " + requests[i].HouseNkv;

                    sb.Append(addr.PadRight(35, ' ') + "|");
                    sb.Append(dolgdata[j].k_s4.ToString().PadLeft(8, ' ') + "|");
                    sb.Append(dolgdata[j].FIO.PadRight(35, ' ') + "|");
                    sb.Append(dolgdata[j].Sum.ToString().PadLeft(10, ' ') + "|");
                    sb.AppendLine();
                }

                if (!has_dolg)
                {
                    requests_selected.Add(requests[i]);
                }
            }

            result = requests_selected.ToArray();
            return sb.ToString();
        }

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

    }

    public enum RequestsFilter
    {
        All = 1, HasDebt = 2, NoDebt = 3
    }
}
