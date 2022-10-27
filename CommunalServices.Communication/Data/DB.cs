/* Communal services system integration 
 * Copyright (c) 2021,  Svitkin V.G. 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Data;
using CommunalServices.Communication.Data;
using GISGKHIntegration.Data;

namespace GISGKHIntegration
{
    /// <summary>
    /// Модуль взаимодействия с базой данных
    /// 06.06.2017
    /// </summary>
    public class DB
    {
        /// <summary>
        /// Объект для синхронизации работы фоновых потоков
        /// </summary>
        public static object csSync = new object();

        public const int ID_CAP_REMONT = 24;

        public static string GetStartDate()
        {
            return "20170601";
        }

        public static object ValueOrNull(object x)
        {
            if (x == null) return DBNull.Value;
            else return x;
        }

        public static object ValueOrNull<T>(Nullable<T> x) where T:struct
        {
            if (!x.HasValue) return DBNull.Value;
            else return x.Value;
        }

        public static object ValueOrEmptyString(object x)
        {
            if (x == null) return String.Empty;
            else return x;
        }

        /// <summary>
        /// Возвращает таблицу всех данных объекта SqlDataReader
        /// </summary>
        /// <param name="rd">Объект, который необходимо преобразовать в таблицу</param>
        /// <returns>Таблица данных</returns>
        public static DataTable GetReaderTable(SqlDataReader rd)
        {
            DataTable t = new DataTable();
            int i;
            object val;
            string str;

            using (rd)
            {
                if (rd.Read() == true)
                {
                    //формирование столбцов таблицы
                    for (i = 0; i < rd.FieldCount; i++)
                    {
                        t.Columns.Add(rd.GetName(i), rd.GetFieldType(i));
                    }

                    //заполнение таблицы
                    while (true)
                    {

                        DataRow row = t.NewRow();
                        for (i = 0; i < rd.FieldCount; i++)
                        {
                            val = rd.GetValue(i);
                            if (val is string)
                            {
                                val = (val as string).Trim();
                            }
                            //if (val == DBNull.Value) val = "";
                            str = val.GetType().ToString();
                            row[i] = val;
                        }
                        t.Rows.Add(row);
                        if (rd.Read() == false) break;
                    }

                }
                else
                {
                    //формирование столбцов таблицы
                    for (i = 0; i < rd.FieldCount; i++)
                    {
                        t.Columns.Add(rd.GetName(i));
                    }
                    //throw new Exception("Не удалось загрузить данные");
                }

            }

            return t;
        }

        /// <summary>
        /// Возвращает актуальный на данный момент для БД ripo месяц (предыдущий месяц)
        /// </summary>
        /// <returns>DateTime, представляющий актуальный на данный момент месяц</returns>
        public static DateTime GetActualMonth()
        {
            return DateTime.Now.Subtract(TimeSpan.FromDays(30));
        }

        /// <summary>
        /// Возвращает таблицу лицевых счетов указанной УК
        /// за указанный явно или актуальный на данный момент месяц
        /// (при указании god=0 и mes=0 - возвращает за актуальный месяц)
        /// </summary>
        /// <param name="k_post">Код поставщика УК</param>
        /// <returns>Таблица лицевых счетов</returns>
        public static DataTable GetUkLS(int k_post,int god=0,int mes=0)
        {

            SqlConnectionStringBuilder b = new SqlConnectionStringBuilder(
                DatabaseParams.curr.ConnectionString);
            b.InitialCatalog = "RIPO";
            //b.ConnectTimeout = 999999;

            SqlConnection con = new SqlConnection(b.ConnectionString);
            con.Open();

            using (con)
            {
                SqlCommand cmd = new SqlCommand();
                cmd.CommandTimeout = 9999;
                cmd.CommandText = @"select t_s5_name, t_dom, t_kor, t_nomer_kv,
dbo.FullLS(k_s8, k_s2, k_s4) as LS,k_s10,pl_o,fio,t_nomer_ko,
ripo_uk.dbo.GetHouseGUID(t_s5_name,t_dom,t_kor) AS FIAS_ID,
ripo_uk.dbo.GetPremisesGUID(t_s5_name,t_dom,t_kor,t_nomer_kv,t_nomer_ko) AS PremisesGUID,
ripo_uk.dbo.GetAccountGUID(dbo.FullLS(k_s8, k_s2, k_s4)) AS AccountGUID
from rls where k_s1upr = @k_post 
and god = @god and mes = @mes and pl_o>0 
ORDER BY t_s5_name, t_dom, nkv";
                cmd.Connection = con;
                cmd.Parameters.AddWithValue("k_post", k_post);
                if (god == 0 && mes == 0)
                {
                    DateTime dt = GetActualMonth();
                    god = dt.Year; mes = dt.Month;
                }
                
                cmd.Parameters.AddWithValue("god", god);
                cmd.Parameters.AddWithValue("mes", mes);
                SqlDataReader rd = cmd.ExecuteReader();
                return GetReaderTable(rd);
            }

        }

        public static DataTable GetUkLS_House(int k_post, string houseGUID, int god = 0, int mes = 0)
        {

            SqlConnectionStringBuilder b = new SqlConnectionStringBuilder(
                DatabaseParams.curr.ConnectionString);
            b.InitialCatalog = "RIPO";

            SqlConnection con = new SqlConnection(b.ConnectionString);
            con.Open();

            using (con)
            {
                SqlCommand cmd = new SqlCommand();
                cmd.CommandText = @"select k_s4,t_nomer_kv,ripo_uk.dbo.GetIDF(dbo.FullLS(k_s8, k_s2, k_s4)),dbo.FullLS(k_s8, k_s2, k_s4),t_s5_name,t_dom,ripo_uk.dbo.GetELS(dbo.FullLS(k_s8, k_s2, k_s4)), ripo_uk.dbo.IsBoundToRoom(dbo.FullLS(k_s8, k_s2, k_s4)) as IsRoom,  k_s10,t_kor from rls where god = @god and mes = @mes and k_s1upr=@k_post AND pl_o>0 
AND ripo_uk.dbo.GetHouseGUID(t_s5_name,t_dom,t_kor) = @houseGUID 
ORDER BY addm,nkv";
                cmd.Connection = con;
                cmd.Parameters.AddWithValue("k_post", k_post);
                if (god == 0 && mes == 0)
                {
                    DateTime dt = GetActualMonth();
                    god = dt.Year; mes = dt.Month;
                }

                cmd.Parameters.AddWithValue("god", god);
                cmd.Parameters.AddWithValue("mes", mes);
                cmd.Parameters.AddWithValue("houseGUID", houseGUID);
                SqlDataReader rd = cmd.ExecuteReader();
                return GetReaderTable(rd);
            }

        }

        public static DataTable GetUkLS1(int k_s4, int god = 0, int mes = 0)
        {

            SqlConnectionStringBuilder b = new SqlConnectionStringBuilder(
                DatabaseParams.curr.ConnectionString);
            b.InitialCatalog = "RIPO";

            SqlConnection con = new SqlConnection(b.ConnectionString);
            con.Open();

            using (con)
            {
                SqlCommand cmd = new SqlCommand();
                cmd.CommandTimeout = 9999;
                cmd.CommandText = @"select t_s5_name, t_dom, t_kor, t_nomer_kv,
dbo.FullLS(k_s8, k_s2, k_s4) as LS,k_s10,pl_o,fio,t_nomer_ko,
ripo_uk.dbo.GetHouseGUID(t_s5_name,t_dom,t_kor) AS FIAS_ID,
ripo_uk.dbo.GetPremisesGUID(t_s5_name,t_dom,t_kor,t_nomer_kv,t_nomer_ko) AS PremisesGUID,
ripo_uk.dbo.GetAccountGUID(dbo.FullLS(k_s8, k_s2, k_s4)) AS AccountGUID
from rls where k_s4 = @k_s4 
and god = @god and mes = @mes and pl_o>0 
ORDER BY t_s5_name, t_dom, nkv";
                cmd.Connection = con;
                cmd.Parameters.AddWithValue("k_s4", k_s4);
                if (god == 0 && mes == 0)
                {
                    DateTime dt = GetActualMonth();
                    god = dt.Year; mes = dt.Month;
                }

                cmd.Parameters.AddWithValue("god", god);
                cmd.Parameters.AddWithValue("mes", mes);
                SqlDataReader rd = cmd.ExecuteReader();
                return GetReaderTable(rd);
            }

        }


        public static DataTable GetUkLS_special(int k_post, int god = 0, int mes = 0)
        {

            SqlConnectionStringBuilder b = new SqlConnectionStringBuilder(
                DatabaseParams.curr.ConnectionString);
            b.InitialCatalog = "RIPO";
            //b.ConnectTimeout = 999999;

            SqlConnection con = new SqlConnection(b.ConnectionString);
            con.Open();

            using (con)
            {
                SqlCommand cmd = new SqlCommand();
                cmd.CommandTimeout = 9999;
                cmd.CommandText = @"select t_s5_name, t_dom, t_kor, t_nomer_kv,
dbo.FullLS(k_s8, k_s2, k_s4) as LS,k_s10,pl_o,fio,t_nomer_ko,
ripo_uk.dbo.GetHouseGUID(t_s5_name,t_dom,t_kor) AS FIAS_ID,
ripo_uk.dbo.GetPremisesGUID(t_s5_name,t_dom,t_kor,t_nomer_kv,t_nomer_ko) AS PremisesGUID,
ripo_uk.dbo.GetAccountGUID(dbo.FullLS(k_s8, k_s2, k_s4)) AS AccountGUID, k_s4
from rls where k_s1upr = @k_post 
and god = @god and mes = @mes and k_s8=3 and k_s2=10
ORDER BY t_s5_name, t_dom, nkv";
                cmd.Connection = con;
                cmd.Parameters.AddWithValue("k_post", k_post);
                if (god == 0 && mes == 0)
                {
                    DateTime dt = GetActualMonth();
                    god = dt.Year; mes = dt.Month;
                }

                cmd.Parameters.AddWithValue("god", god);
                cmd.Parameters.AddWithValue("mes", mes);
                SqlDataReader rd = cmd.ExecuteReader();
                return GetReaderTable(rd);
            }

        }

        public static DataTable GetUkLS1_special(int k_s4, int god = 0, int mes = 0)
        {

            SqlConnectionStringBuilder b = new SqlConnectionStringBuilder(
                DatabaseParams.curr.ConnectionString);
            b.InitialCatalog = "RIPO";

            SqlConnection con = new SqlConnection(b.ConnectionString);
            con.Open();

            using (con)
            {
                SqlCommand cmd = new SqlCommand();
                cmd.CommandTimeout = 9999;
                cmd.CommandText = @"select t_s5_name, t_dom, t_kor, t_nomer_kv,
dbo.FullLS(k_s8, k_s2, k_s4) as LS,k_s10,pl_o,fio,t_nomer_ko,
ripo_uk.dbo.GetHouseGUID(t_s5_name,t_dom,t_kor) AS FIAS_ID,
ripo_uk.dbo.GetPremisesGUID(t_s5_name,t_dom,t_kor,t_nomer_kv,t_nomer_ko) AS PremisesGUID,
ripo_uk.dbo.GetAccountGUID(dbo.FullLS(k_s8, k_s2, k_s4)) AS AccountGUID, k_s4
from rls where k_s4 = @k_s4 
and god = @god and mes = @mes and k_s8=3 and k_s2=10
ORDER BY t_s5_name, t_dom, nkv";
                cmd.Connection = con;
                cmd.Parameters.AddWithValue("k_s4", k_s4);
                if (god == 0 && mes == 0)
                {
                    DateTime dt = GetActualMonth();
                    god = dt.Year; mes = dt.Month;
                }

                cmd.Parameters.AddWithValue("god", god);
                cmd.Parameters.AddWithValue("mes", mes);
                SqlDataReader rd = cmd.ExecuteReader();
                return GetReaderTable(rd);
            }

        }
        

        /// <summary>
        /// Возвращает таблицу лицевых счетов указанной УК без кодов домов по ФИАС
        /// за указанный явно или актуальный на данный момент месяц
        /// (при указании god=0 и mes=0 - возвращает за актуальный месяц)
        /// </summary>
        /// <param name="k_post">Код поставщика УК</param>
        /// <returns>Таблица лицевых счетов</returns>
        public static DataTable GetUkLS_Lite(int k_post, int god = 0, int mes = 0,string house="",int n=0)
        {

            SqlConnectionStringBuilder b = new SqlConnectionStringBuilder(
                DatabaseParams.curr.ConnectionString);
            b.InitialCatalog = "RIPO";
            //b.ConnectTimeout = 999999;

            string verb;
            if (n <= 0) verb = "SELECT";
            else verb = "SELECT TOP " + n.ToString() + " ";

            SqlConnection con = new SqlConnection(b.ConnectionString);
            con.Open();

            using (con)
            {
                SqlCommand cmd = new SqlCommand();
                cmd.CommandTimeout = 9999;

                //выборка ЛС, для которых не был загружен платежный документ
                cmd.CommandText = verb+@" k_s4,t_s5_name, t_dom, t_kor, t_nomer_kv,
dbo.FullLS(k_s8, k_s2, k_s4) as LS,k_s10,pl_o,fio,t_nomer_ko,
ripo_uk.dbo.GetGKUID(dbo.FullLS(k_s8, k_s2, k_s4)) AS GKUID,
ripo_uk.dbo.GetLsDolg(k_s4,@god,@mes) AS dolg,
ripo_uk.dbo.GetLsPen(k_s4,@god,@mes) AS pen,
ripo_uk.dbo.GetAccountGUID(dbo.FullLS(k_s8, k_s2, k_s4)) AS AccountGUID
from rls where k_s1upr = @k_post 
and god = @god and mes = @mes AND pl_o>0.0 AND 
NOT EXISTS (SELECT MessageGUID FROM [ripo_uk].[dbo].[PayDocs_Status] WHERE god=rls.god AND mes=rls.mes AND k_s4=rls.k_s4)
AND ripo_uk.dbo.GetAccountGUID(dbo.FullLS(k_s8, k_s2, k_s4)) IS NOT NULL";

                if (house != "") cmd.CommandText += " AND addm=@house ";
                cmd.CommandText += " ORDER BY t_s5_name, t_dom, nkv";

                cmd.Connection = con;
                cmd.Parameters.AddWithValue("k_post", k_post);
                if (god == 0 && mes == 0)
                {
                    DateTime dt = GetActualMonth();
                    god = dt.Year; mes = dt.Month;
                }

                cmd.Parameters.AddWithValue("god", god);
                cmd.Parameters.AddWithValue("mes", mes);
                if (house != "") cmd.Parameters.AddWithValue("house", house);
                SqlDataReader rd = cmd.ExecuteReader();

                return GetReaderTable(rd);
            }

        }

        public static DataTable GetUkLS_Lite1(int k_s4, int god = 0, int mes = 0)
        {

            SqlConnectionStringBuilder b = new SqlConnectionStringBuilder(
                DatabaseParams.curr.ConnectionString);
            b.InitialCatalog = "RIPO";
            //b.ConnectTimeout = 999999;

            SqlConnection con = new SqlConnection(b.ConnectionString);
            con.Open();

            using (con)
            {
                SqlCommand cmd = new SqlCommand();
                cmd.CommandTimeout = 9999;
                cmd.CommandText = @"select k_s4,t_s5_name, t_dom, t_kor, t_nomer_kv,
dbo.FullLS(k_s8, k_s2, k_s4) as LS,k_s10,pl_o,fio,t_nomer_ko,
ripo_uk.dbo.GetGKUID(dbo.FullLS(k_s8, k_s2, k_s4)) AS GKUID,
ripo_uk.dbo.GetLsDolg(k_s4,@god,@mes) AS dolg,
ripo_uk.dbo.GetLsPen(k_s4,@god,@mes) AS pen,
ripo_uk.dbo.GetAccountGUID(dbo.FullLS(k_s8, k_s2, k_s4)) AS AccountGUID
from rls where k_s4 = @k_s4 
and god = @god and mes = @mes AND pl_o>0.0 ORDER BY t_s5_name, t_dom, nkv";
                cmd.Connection = con;
                cmd.Parameters.AddWithValue("k_s4", k_s4);
                if (god == 0 && mes == 0)
                {
                    DateTime dt = GetActualMonth();
                    god = dt.Year; mes = dt.Month;
                }

                cmd.Parameters.AddWithValue("god", god);
                cmd.Parameters.AddWithValue("mes", mes);
                SqlDataReader rd = cmd.ExecuteReader();
                return GetReaderTable(rd);
            }

        }

        

        public static DataTable GetTrustedLS()
        {

            SqlConnectionStringBuilder b = new SqlConnectionStringBuilder(
                DatabaseParams.curr.ConnectionString);
            b.InitialCatalog = "RIPO_UK";
            //b.ConnectTimeout = 999999;

            SqlConnection con = new SqlConnection(b.ConnectionString);
            con.Open();

            using (con)
            {
                SqlCommand cmd = new SqlCommand();
                cmd.CommandTimeout = 9999;
                cmd.CommandText = @"SELECT [RIP_LS],[ELS],[idf],[GKUID],[k_s1upr]
FROM [ripo_uk].[dbo].[v_ELS] where k_s1upr IN (SELECT k_post FROM v_TrustedVendors)";
                cmd.Connection = con;
                
                SqlDataReader rd = cmd.ExecuteReader();
                return GetReaderTable(rd);
            }

        }

        

        /// <summary>
        /// Возвращает список услуг указанной организации
        /// </summary>
        /// <param name="k_post">Код поставщика</param>
        /// <param name="god">(необязательно) год</param>
        /// <param name="mes">(необязательно) месяц</param>
        /// <returns>Таблица услуг</returns>
        public static DataTable GetUkUsl(int k_post, int god = 0, int mes = 0)
        {

            SqlConnectionStringBuilder b = new SqlConnectionStringBuilder(
                DatabaseParams.curr.ConnectionString);
            b.InitialCatalog = "RIPO_UK";

            SqlConnection con = new SqlConnection(b.ConnectionString);
            con.Open();

            using (con)
            {
                SqlCommand cmd = new SqlCommand();
                cmd.CommandTimeout = 200;
                cmd.CommandText = @"GetUslList";
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Connection = con;
                cmd.Parameters.AddWithValue("k_post", k_post);
                if (god == 0 && mes == 0)
                {
                    DateTime dt = GetActualMonth();
                    god = dt.Year; mes = dt.Month;
                }

                cmd.Parameters.AddWithValue("god", god);
                cmd.Parameters.AddWithValue("mes", mes);
                SqlDataReader rd = cmd.ExecuteReader();
                return GetReaderTable(rd);
            }

        }

        

        /// <summary>
        /// Получение списка организаций
        /// </summary>
        /// <returns>Список объектов, представляющих данные организации</returns>
        public static List<ListEntry> GetOrganisations()
        {
            SqlConnection con = new SqlConnection(DatabaseParams.curr.ConnectionString);
            List<ListEntry> res = new List<ListEntry>();
            ListEntry val;
            con.Open();
            using (con)
            {
                SqlCommand cmd = new SqlCommand(
                    @"SELECT k_post,namorg,dbo.LsIntegrationStatus(k_post) as ls_flag, dbo.UslIntegrationStatus(k_post) as usl_flag, dbo.PuDataIntegrationStatus(k_post) as pu_data_flag, dbo.PuNumbersIntegrationStatus(k_post) as pu_numbers_flag FROM ripo.dbo.sorg WHERE Enable_Login='True' AND k_post>0",
                    con);

                SqlDataReader rd = cmd.ExecuteReader();

                using (rd)
                {
                    while (true)
                    {
                        if (rd.Read() == false) break;
                        val = new ListEntry();
                        val.id = rd.GetInt16(rd.GetOrdinal("k_post"));
                        val.name = rd.GetString(rd.GetOrdinal("namorg"));
                        val.ls_flag = rd.GetBoolean(rd.GetOrdinal("ls_flag"));
                        val.usl_flag = rd.GetBoolean(rd.GetOrdinal("usl_flag"));
                        val.pu_data_flag = rd.GetBoolean(rd.GetOrdinal("pu_data_flag"));
                        val.pu_numbers_flag = rd.GetBoolean(rd.GetOrdinal("pu_numbers_flag"));
                        res.Add(val);
                    }
                }

                return res;
            }

        }

        /// <summary>
        /// Получение платежа по указанному Pay ID
        /// </summary>        
        public static Payment GetPaymentById(string pay_id)
        {
            SqlConnectionStringBuilder b = new SqlConnectionStringBuilder(
                DatabaseParams.curr.ConnectionString);
            b.InitialCatalog = "RIPO_UK";

            SqlConnection con = new SqlConnection(b.ConnectionString);

            Payment val;
            con.Open();
            using (con)
            {
                SqlCommand cmd = new SqlCommand(
                    @"SELECT Pay_ID,Pay_Date,LSCHET,v_Payments.god,v_Payments.mes, Total, NameORG, INN, KPP 
FROM (v_Payments INNER JOIN ripo.dbo.rls ON v_Payments.k_s4 = rls.k_s4)  
INNER JOIN v_Organisations ON rls.k_s1sod = v_Organisations.k_post WHERE Pay_ID=@Pay_ID AND rls.god=@god AND rls.mes=@mes",
                    con);
                cmd.Parameters.AddWithValue("Pay_ID", pay_id);
                cmd.Parameters.AddWithValue("god", DateTime.Now.Subtract(TimeSpan.FromDays(30)).Year);
                cmd.Parameters.AddWithValue("mes", DateTime.Now.Subtract(TimeSpan.FromDays(30)).Month);
                SqlDataReader rd = cmd.ExecuteReader();

                using (rd)
                {
                    while (true)
                    {
                        if (rd.Read() == false) break;
                        val = new Payment();
                        val.Pay_ID = rd["Pay_ID"].ToString();
                        val.Pay_Date = (DateTime)rd["Pay_Date"];
                        val.god = (int)rd["god"];
                        val.mes = (int)rd["mes"];
                        val.LS = rd["LSCHET"].ToString();
                        val.total_sum = (decimal)rd["Total"];

                        val.OrgName = rd["NameORG"].ToString();
                        val.OrgINN = rd["INN"].ToString();
                        if (rd["KPP"] == null || rd["KPP"] == DBNull.Value) val.OrgKPP = "662301001";
                        else val.OrgKPP = rd["KPP"].ToString();
                        return val;
                    }
                }

                return null;
            }

        }

        public static Payment GetTransactionById(string pay_id)
        {
            SqlConnectionStringBuilder b = new SqlConnectionStringBuilder(
                DatabaseParams.curr.ConnectionString);
            b.InitialCatalog = "RIPO_UK";

            SqlConnection con = new SqlConnection(b.ConnectionString);

            Payment val;
            con.Open();
            using (con)
            {
                SqlCommand cmd = new SqlCommand(
                    @"SELECT Pay_ID,Pay_Date,LSCHET,god,mes, Total, NameORG, INN, KPP 
FROM v_GkhTransactions WHERE Pay_ID=@Pay_ID",
                    con);
                cmd.Parameters.AddWithValue("Pay_ID", pay_id);                
                SqlDataReader rd = cmd.ExecuteReader();

                using (rd)
                {
                    while (true)
                    {
                        if (rd.Read() == false) break;
                        val = new Payment();
                        val.Pay_ID = rd["Pay_ID"].ToString();
                        val.Pay_Date = (DateTime)rd["Pay_Date"];
                        val.god = (int)rd["god"];
                        val.mes = (int)rd["mes"];
                        val.LS = rd["LSCHET"].ToString();
                        val.total_sum = (decimal)rd["Total"];

                        val.OrgName = rd["NameORG"].ToString();
                        val.OrgINN = rd["INN"].ToString();
                        if (rd["KPP"] == null || rd["KPP"] == DBNull.Value) val.OrgKPP = "662301001";
                        else val.OrgKPP = rd["KPP"].ToString();
                        return val;
                    }
                }

                return null;
            }

        }

        /// <summary>
        /// Получение следующей пачки платежей для отправки в ГИС ЖКХ
        /// </summary>
        /// <param name="kass">true - брать из SDB.MDF, false - брать из RIP_Pay</param>        
        public static List<Payment> GetPayments(bool error, bool kass = true)
        {
            string name;

            if (kass) name = "GetTransactions2";
            else name = "GetPayments";
            if (error) name += "_Error";

            SqlConnectionStringBuilder b = new SqlConnectionStringBuilder(
                DatabaseParams.curr.ConnectionString);
            b.InitialCatalog = "RIPO_UK";

            SqlConnection con = new SqlConnection(b.ConnectionString);
            List<Payment> pays = new List<Payment>(200);
            Payment val;
            con.Open();
            using (con)
            {
                SqlCommand cmd;
                if (kass)
                {
                    cmd = new SqlCommand(
                    @"EXEC "+ name+" @StartDate='20170615',@EndDate='20200101'",
                    con);
                }
                else
                {
                    cmd = new SqlCommand(
                    @"EXEC " + name + " @StartDate='"+GetStartDate()+"',@EndDate='20200101',@PaySystem=0",
                    con);
                }
                cmd.CommandTimeout = 60;
                SqlDataReader rd = cmd.ExecuteReader();

                using (rd)
                {
                    while (true)
                    {
                        if (rd.Read() == false) break;
                        val = new Payment();
                        val.Pay_ID = rd["Pay_ID"].ToString();
                        val.Pay_Date = (DateTime)rd["Pay_Date"];
                        val.god = (int)rd["god"];
                        val.mes = (int)rd["mes"];
                        val.LS = rd["LSCHET"].ToString();
                        val.total_sum = (decimal)rd["Total"];
                        val.OrgName = rd["NameORG"].ToString();
                        val.OrgINN = rd["INN"].ToString();
                        if (rd["KPP"] == null || rd["KPP"] == DBNull.Value ||rd["KPP"].ToString()=="") val.OrgKPP = "662301001";
                        else val.OrgKPP = rd["KPP"].ToString();
                        pays.Add(val);
                    }
                }

                return pays;
            }

        }

        /// <summary>
        /// ПОлучение общего числа платежей на очереди для отправки в ГИС ЖКХ 
        /// </summary>
        /// <param name="kass">true - брать из SDB.MDF, false - брать из RIP_Pay</param>        
        public static int GetPaymentsCount(bool error,bool kass=true)
        {
            string name;

            if (kass) name = "GetTransactionsCount2";
            else name = "GetPaymentsCount";
            if (error) name += "_Error";

            SqlConnectionStringBuilder b = new SqlConnectionStringBuilder(
                DatabaseParams.curr.ConnectionString);
            b.InitialCatalog = "RIPO_UK";

            SqlConnection con = new SqlConnection(b.ConnectionString);
            List<Payment> pays = new List<Payment>(200);
            object val;
            con.Open();
            using (con)
            {
                SqlCommand cmd;
                if (kass)
                {
                    cmd = new SqlCommand(
                    @"EXEC " + name + " @StartDate='20170615',@EndDate='20200101'",
                    con);
                }
                else
                {
                    cmd = new SqlCommand(
                    @"EXEC " + name + " @StartDate='" + GetStartDate() + "',@EndDate='20200101',@PaySystem=0",
                    con);
                }
                cmd.CommandTimeout = 60;
                    
                val = cmd.ExecuteScalar();
                if (val == null) return 0;
                if (val == DBNull.Value) return 0;
                return (int)val;
            }

        }

        public static List<Payment> GetTransToAktir()
        {
             
            SqlConnectionStringBuilder b = new SqlConnectionStringBuilder(
                DatabaseParams.curr.ConnectionString);
            b.InitialCatalog = "RIP_Pay";

            SqlConnection con = new SqlConnection(b.ConnectionString);
            List<Payment> pays = new List<Payment>(200);
            Payment val;
            con.Open();
            using (con)
            {
                SqlCommand cmd;
                cmd = new SqlCommand(@"SELECT [Pay_ID],[Pay_Date],[Name],[id_PaySystem],[LevelT] 
FROM CP.[SDB.MDF].[dbo].[v_AktirTrans] where Pay_ID IN ( 
SELECT Pay_ID FROM RIP_Pay.dbo.GISGKH_PaymentState WHERE ImportState=1 )", con);
                cmd.CommandTimeout = 60;
                SqlDataReader rd = cmd.ExecuteReader();

                using (rd)
                {
                    while (true)
                    {
                        if (rd.Read() == false) break;
                        val = new Payment();
                        val.Pay_ID = rd["Pay_ID"].ToString();
                        val.Pay_Date = (DateTime)rd["Pay_Date"]; 
                        pays.Add(val);
                    }
                }

                return pays;
            }

        }

        /// <summary>
        /// Получение множества запросов (Message GUID), находящихся в данный момент в ожидании обработки ГИС ЖКХ
        /// </summary>        
        public static List<string> GetWaitingRequests()
        {
            SqlConnectionStringBuilder b = new SqlConnectionStringBuilder(
                DatabaseParams.curr.ConnectionString);
            SqlCommand cmd;
            SqlDataReader rd;
            b.InitialCatalog = "RIPO_UK";

            SqlConnection con = new SqlConnection(b.ConnectionString);
            List<string> reqs = new List<string>(200);
            string val;
            con.Open();
            using (con)
            {
               cmd = new SqlCommand(
                    @"EXEC Pay_GetWaitingRequests",
                    con);
                rd = cmd.ExecuteReader();

                using (rd)
                {
                    while (true)
                    {
                        if (rd.Read() == false) break;
                        val = rd[0].ToString();
                        if (val.Length > 0) reqs.Add(val);
                    }
                }

                cmd = new SqlCommand(
                    @"SELECT [message_guid] FROM GisRequests",
                    con);
                rd = cmd.ExecuteReader();

                using (rd)
                {
                    while (true)
                    {
                        if (rd.Read() == false) break;
                        val = rd[0].ToString();
                        if (val.Length > 0) reqs.Add(val);
                    }
                }

                return reqs;
            }

        }

               
        public static Payment GetPaymentByMessageGUID(string message_guid)
        {
            
            SqlConnection con = new SqlConnection(DatabaseParams.curr.ConnectionString);

            Payment val;
            con.Open();
            using (con)
            {
                SqlCommand cmd = new SqlCommand(
                    @"SELECT GISGKH_PaymentState.Pay_ID,Pay_Date,LSCHET,god,mes, Total,MessageGUID, 
TransportGUID,RequestState,ImportState, 
UniqueNumber, EntityGUID, ImportDate, ErrorCode, ErrorMessage, StackTrace 
FROM ripo_uk.dbo.v_Payments INNER JOIN 
RIP_Pay.dbo.GISGKH_PaymentState ON ripo_uk.dbo.v_Payments.Pay_ID = GISGKH_PaymentState.Pay_ID
WHERE MessageGUID=@MessageGUID",
                    con);
                cmd.Parameters.AddWithValue("MessageGUID", new Guid(message_guid));
                SqlDataReader rd = cmd.ExecuteReader();

                using (rd)
                {
                    while (true)
                    {
                        if (rd.Read() == false) break;
                        val = new Payment();
                        val.Pay_ID = rd["Pay_ID"].ToString();
                        val.Pay_Date = (DateTime)rd["Pay_Date"];
                        val.god = (int)rd["god"];
                        val.mes = (int)rd["mes"];
                        val.LS = rd["LSCHET"].ToString();
                        val.total_sum = (decimal)rd["Total"];

                        //***
                        val.RequestState = Convert.ToInt32(rd["RequestState"]);
                        val.ImportState = Convert.ToInt32(rd["ImportState"]);
                        val.ErrorCode = rd["ErrorCode"].ToString();
                        val.ErrorMessage = rd["ErrorMessage"].ToString();
                        val.StackTrace = rd["StackTrace"].ToString();

                        if (rd["MessageGUID"] != null && rd["MessageGUID"] != DBNull.Value)
                            val.message_guid = rd["MessageGUID"].ToString();
                        if (rd["TransportGUID"] != null && rd["TransportGUID"] != DBNull.Value)
                            val.transport_guid = rd["TransportGUID"].ToString();
                        if (rd["EntityGUID"] != null && rd["EntityGUID"] != DBNull.Value)
                            val.EntityGUID = rd["EntityGUID"].ToString();
                        if (rd["UniqueNumber"] != null && rd["UniqueNumber"] != DBNull.Value)
                            val.UniqueNumber = rd["UniqueNumber"].ToString();
                        if (rd["ImportDate"] != null && rd["ImportDate"] != DBNull.Value)
                            val.ImportDate = (DateTime)rd["ImportDate"];

                        return val;
                    }
                }

                return null;
            }

        }

        /// <summary>
        /// Поиск приборов учета по заданным критериям
        /// </summary>
        /// <param name="match">Критерии поиска (должно быть задано regnum,GisNumber, AccountGUID,ResourceGUID)</param>
        /// <returns>Набор idf ПУ, удовлетворяющих критериям</returns>
        public static List<string> FindDevices(MDevice match)
        {
                       
            SqlConnection con = new SqlConnection(DatabaseParams.curr.ConnectionString);
            List<string> res = new List<string>(10);
            
            con.Open();
            using (con)
            {
                SqlCommand cmd;
                cmd = new SqlCommand(@"PU_Search_Proc2", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandTimeout = 50;
                cmd.Parameters.AddWithValue("regnum", match.regnum);
                cmd.Parameters.AddWithValue("GisNumber", match.gisgkh_num);
                cmd.Parameters.AddWithValue("AccountGUID", match.AccountGUID);
                cmd.Parameters.AddWithValue("ResourceGUID", match.ResourceGUID);
                
                SqlDataReader rd = cmd.ExecuteReader();

                using (rd)
                {
                    while (true)
                    {
                        if (rd.Read() == false) break;                        
                        res.Add(rd[0].ToString());
                    }
                }

                return res;
            }

        }

        public static List<string> FindDevicesGisNum(string gisnum)
        {

            SqlConnection con = new SqlConnection(DatabaseParams.curr.ConnectionString);
            List<string> res = new List<string>(10);

            con.Open();
            using (con)
            {
                SqlCommand cmd;
                cmd = new SqlCommand(@"PU_Search_Proc2", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandTimeout = 50;                
                cmd.Parameters.AddWithValue("GisNumber", gisnum);                

                SqlDataReader rd = cmd.ExecuteReader();

                using (rd)
                {
                    while (true)
                    {
                        if (rd.Read() == false) break;
                        res.Add(rd[0].ToString());
                    }
                }

                return res;
            }

        }



        /// <summary>
        /// Обновление данных состояния платежа в базе
        /// </summary>
        /// <param name="pay">Данные платежа</param>        
        public static int UpdatePaymentData(Payment pay)
        {            

            SqlConnection con = new SqlConnection(DatabaseParams.curr.ConnectionString);
            object val;
            
            con.Open();
            using (con)
            {
                SqlCommand cmd = new SqlCommand(
                    @"SELECT COUNT(Pay_ID) FROM RIP_Pay.dbo.GISGKH_PaymentState WHERE Pay_ID=@Pay_ID",
                    con);
                cmd.Parameters.AddWithValue("Pay_ID", pay.Pay_ID);
                val = cmd.ExecuteScalar();

                if (val == null || val == DBNull.Value || Convert.ToInt32(val) == 0)
                {
                    cmd = new SqlCommand(
                    @"INSERT INTO RIP_Pay.dbo.[GISGKH_PaymentState] 
([Pay_ID],[MessageGUID],[TransportGUID],[RequestState],[ImportState],[UniqueNumber],[EntityGUID],[ImportDate] 
,[ErrorCode],[ErrorMessage],[StackTrace],[DateChange]) 
VALUES (@Pay_ID,@MessageGUID,@TransportGUID,@RequestState,@ImportState,@UniqueNumber,@EntityGUID 
,@ImportDate,@ErrorCode,@ErrorMessage,@StackTrace,GETDATE())",con);                    
                }
                else
                {
                    cmd = new SqlCommand(
                    @"UPDATE RIP_Pay.dbo.[GISGKH_PaymentState] SET 
[MessageGUID]=@MessageGUID,[TransportGUID]=@TransportGUID,[RequestState]=@RequestState,
[ImportState]=@ImportState,[UniqueNumber]=@UniqueNumber,[EntityGUID]=@EntityGUID,[ImportDate]=@ImportDate, 
[ErrorCode]=@ErrorCode,[ErrorMessage]=@ErrorMessage,[StackTrace]=@StackTrace,[DateChange]=GETDATE()  
WHERE [Pay_ID]=@Pay_ID",con);
                }

                cmd.Parameters.AddWithValue("Pay_ID", pay.Pay_ID);
                cmd.Parameters.AddWithValue("RequestState", pay.RequestState);
                cmd.Parameters.AddWithValue("ImportState", pay.ImportState);

                if (pay.message_guid != null && pay.message_guid != "")
                    cmd.Parameters.AddWithValue("MessageGUID", pay.message_guid);
                else
                    cmd.Parameters.AddWithValue("MessageGUID", DBNull.Value);

                if (pay.transport_guid != null && pay.transport_guid != "")
                    cmd.Parameters.AddWithValue("TransportGUID", pay.transport_guid);
                else
                    cmd.Parameters.AddWithValue("TransportGUID", DBNull.Value);                

                if (pay.UniqueNumber != null && pay.UniqueNumber != "")
                    cmd.Parameters.AddWithValue("UniqueNumber", pay.UniqueNumber);
                else
                    cmd.Parameters.AddWithValue("UniqueNumber", DBNull.Value);

                if (pay.EntityGUID != null && pay.EntityGUID != "")
                    cmd.Parameters.AddWithValue("EntityGUID", pay.EntityGUID);
                else
                    cmd.Parameters.AddWithValue("EntityGUID", DBNull.Value);

                if (DateTime.Compare(pay.ImportDate, new DateTime(2000, 1, 1)) > 0)
                    cmd.Parameters.AddWithValue("ImportDate", pay.ImportDate);
                else
                    cmd.Parameters.AddWithValue("ImportDate", DBNull.Value);

                cmd.Parameters.AddWithValue("ErrorCode", pay.ErrorCode);
                cmd.Parameters.AddWithValue("ErrorMessage", pay.ErrorMessage);
                if(pay.StackTrace!=null)
                    cmd.Parameters.AddWithValue("StackTrace", pay.StackTrace);
                else
                    cmd.Parameters.AddWithValue("StackTrace", "");
                return cmd.ExecuteNonQuery();
                
            }

        }

        /// <summary>
        /// Обновление данных состояния запроса
        /// </summary>
        /// <param name="message_guid">ID запроса</param>
        /// <param name="RequestState">Новое состояние</param>
        /// <param name="ErrorCode">Код ошибки, если есть</param>
        /// <param name="ErrorMessage">Сообщение об ошибке, если есть</param>
        /// <param name="StackTrace">Отладочная информация, если есть</param>        
        public static int UpdateRequestState(string message_guid,int RequestState,
            string ErrorCode="",string ErrorMessage="",string StackTrace="")
        {
            if (ErrorCode == null) ErrorCode = "";
            if (ErrorMessage == null) ErrorMessage = "";
            SqlCommand cmd;  

            switch (RequestState)
            {
                case RequestStates.RS_SENT: ErrorCode = "WAIT"; ErrorMessage = "Запрос отправлен"; break;
                case RequestStates.RS_RECEIVED: ErrorCode = "WAIT"; ErrorMessage = "Запрос получен ГИСЖКХ"; break;
                case RequestStates.RS_PROCESSING: ErrorCode = "WAIT"; ErrorMessage = "Запрос обрабатывается ГИСЖКХ"; break; 
            }
            SqlConnection con = new SqlConnection(DatabaseParams.curr.ConnectionString);
            con.Open();
                
           
            using (con)
            {
                //*** 10.07.2017 Актирование
                cmd = new SqlCommand(
                        @"SELECT DISTINCT ImportState FROM RIP_Pay.dbo.GISGKH_PaymentState WHERE MessageGUID=@MessageGUID",
                        con);
                cmd.Parameters.AddWithValue("MessageGUID", message_guid);
                object val = cmd.ExecuteScalar();
                int ImportState;
                if (val == null || val == DBNull.Value) ImportState = 0;
                else ImportState = Convert.ToInt32(val);

                if (ImportState == Data.Payment.IS_AKTIR_WAIT && RequestState == RequestStates.RS_ERROR)
                {
                    RequestState = RequestStates.RS_PROCESSED;
                }
                //***              

                cmd = new SqlCommand(
                    @"UPDATE RIP_Pay.dbo.GISGKH_PaymentState SET RequestState=@RequestState, 
ErrorCode=@ErrorCode, ErrorMessage=@ErrorMessage, StackTrace=@StackTrace WHERE MessageGUID=@MessageGUID",
                    con);
                cmd.Parameters.AddWithValue("RequestState", Convert.ToInt16(RequestState));
                cmd.Parameters.AddWithValue("ErrorCode", ErrorCode);
                cmd.Parameters.AddWithValue("ErrorMessage", ErrorMessage);

                if(StackTrace!=null)
                    cmd.Parameters.AddWithValue("StackTrace", StackTrace);
                else
                    cmd.Parameters.AddWithValue("StackTrace", "");

                cmd.Parameters.AddWithValue("MessageGUID", message_guid);
                return cmd.ExecuteNonQuery();

            }

        }


        public static int UpdateImportState(string message_guid, int ImportState)
        {
            
            SqlConnection con = new SqlConnection(DatabaseParams.curr.ConnectionString);

            con.Open();
            using (con)
            {
                SqlCommand cmd;

                //*** 10.07.2017 Актирование
                cmd = new SqlCommand(
                        @"SELECT DISTINCT ImportState FROM RIP_Pay.dbo.GISGKH_PaymentState WHERE MessageGUID=@MessageGUID",
                        con);
                cmd.Parameters.AddWithValue("MessageGUID", message_guid);
                object val = cmd.ExecuteScalar();
                int OldImportState;
                if (val == null || val == DBNull.Value) OldImportState = 0;
                else OldImportState = Convert.ToInt32(val);

                if (OldImportState == Data.Payment.IS_AKTIR_WAIT)
                {
                    if(ImportState == Data.Payment.IS_ERROR)
                        ImportState = Data.Payment.IS_AKTIR_WAIT;
                    else if(ImportState==Data.Payment.IS_SUCCESS)
                        ImportState = Data.Payment.IS_AKTIR;
                }
                //***

                cmd = new SqlCommand(
                    @"UPDATE RIP_Pay.dbo.GISGKH_PaymentState SET ImportState=@ImportState WHERE MessageGUID=@MessageGUID",
                    con);
                cmd.Parameters.AddWithValue("ImportState", Convert.ToInt16(ImportState));                
                cmd.Parameters.AddWithValue("MessageGUID", message_guid);
                return cmd.ExecuteNonQuery();
            }

        }

        /// <summary>
        /// Обновляет состояние платежа в БД, основываясь на результатах импорат в ГИС ЖКХ
        /// </summary>
        /// <param name="data">Данные, возвращенные ГИС ЖКХ</param>        
        public static int UpdatePaymentState(ApiResultEntry data)
        {
            short ImportState;
            if (data.success)
            {
                ImportState = Payment.IS_SUCCESS;
                data.ErrorCode = "OK";
                data.ErrorMessage = "Принят ГИС ЖКХ";
                data.StackTrace = "";
            }
            else
            {
                ImportState = Payment.IS_ERROR;
            }

            if (data.ErrorCode == null) data.ErrorCode = "";
            if (data.ErrorMessage == null) data.ErrorMessage = "";
            if (data.StackTrace == null) data.StackTrace = "";

            SqlConnection con = new SqlConnection(DatabaseParams.curr.ConnectionString);
            SqlCommand cmd;

            con.Open();
            using (con)
            {                
                //*** 10.07.2017 Актирование
                cmd = new SqlCommand(
                        @"SELECT ImportState FROM RIP_Pay.dbo.GISGKH_PaymentState WHERE TransportGUID=@TransportGUID",
                        con);
                cmd.Parameters.AddWithValue("TransportGUID", data.TransportGUID);
                object val = cmd.ExecuteScalar();
                int OldImportState;
                if (val == null || val == DBNull.Value) OldImportState = 0;
                else OldImportState = Convert.ToInt32(val);

                if (OldImportState == Data.Payment.IS_AKTIR_WAIT)
                {
                    if (data.success)
                    {
                        ImportState = Data.Payment.IS_AKTIR;
                        data.ErrorCode = "АКТИР";
                        data.ErrorMessage = "Успешно актирован";
                        data.StackTrace = "";
                    }
                    else
                    {
                        ImportState = Data.Payment.IS_AKTIR_WAIT;
                    }
                }
                //*** 

                cmd = new SqlCommand(
                    @"UPDATE RIP_Pay.dbo.GISGKH_PaymentState SET ImportState=@ImportState, UniqueNumber=@UniqueNumber, 
EntityGUID=@EntityGUID, ImportDate=@ImportDate,
ErrorCode=@ErrorCode, ErrorMessage=@ErrorMessage, StackTrace=@StackTrace, DateChange=GETDATE() WHERE TransportGUID=@TransportGUID",
                    con);
                cmd.Parameters.AddWithValue("ImportState", ImportState);

                if (data.UniqueNumber != null && data.UniqueNumber != "")
                {
                    cmd.Parameters.AddWithValue("UniqueNumber", data.UniqueNumber);
                }
                else cmd.Parameters.AddWithValue("UniqueNumber", DBNull.Value);

                if (data.EntityGUID != null && data.EntityGUID != "")
                {
                    cmd.Parameters.AddWithValue("EntityGUID", data.EntityGUID);
                }
                else cmd.Parameters.AddWithValue("EntityGUID", DBNull.Value);

                if (DateTime.Compare(data.ImportDate,new DateTime(2000,1,1))>0)
                {
                    cmd.Parameters.AddWithValue("ImportDate", data.ImportDate);
                }
                else cmd.Parameters.AddWithValue("ImportDate", DBNull.Value);

                cmd.Parameters.AddWithValue("ErrorCode", data.ErrorCode);
                cmd.Parameters.AddWithValue("ErrorMessage", data.ErrorMessage);
                cmd.Parameters.AddWithValue("StackTrace", data.StackTrace);

                cmd.Parameters.AddWithValue("TransportGUID", data.TransportGUID);

                return cmd.ExecuteNonQuery();

            }

        }

        public static int UpdateTransactionLevel(string pay_id,string level)
        {

            SqlConnection con = new SqlConnection(DatabaseParams.curr.ConnectionString);            

            con.Open();
            using (con)
            {
                SqlCommand cmd = new SqlCommand(
                    @"UPDATE CP.[SDB.MDF].dbo.v_AllTrans SET levelT=CONVERT(int,CONVERT(nvarchar(50),levelT)+@level) WHERE guid=@pay_id",
                    con);
                cmd.Parameters.AddWithValue("Pay_ID", pay_id);
                cmd.Parameters.AddWithValue("level", level);                
                return cmd.ExecuteNonQuery();

            }

        }


        public static DataTable GetPaymentsReport(DateTime StartDate,DateTime EndDate,ReportType type)
        {

            SqlConnectionStringBuilder b = new SqlConnectionStringBuilder(
                DatabaseParams.curr.ConnectionString);
            b.InitialCatalog = "RIPO_UK";
            bool Kass;

            if (type == ReportType.Transactions) Kass = true;
            else Kass = false;

            SqlConnection con = new SqlConnection(b.ConnectionString);
            con.Open();

            using (con)
            {
                SqlCommand cmd = new SqlCommand();
                cmd.CommandTimeout = 120;
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Connection = con;
                cmd.Parameters.AddWithValue("StartDate", StartDate);
                cmd.Parameters.AddWithValue("EndDate", EndDate);

                if (type == ReportType.Payments || type == ReportType.Transactions)
                {
                    cmd.CommandText = @"Report_PaymentStatus";
                    cmd.Parameters.AddWithValue("Kass", Kass);
                }
                else
                    cmd.CommandText = @"Report_PaymentStatusByOper";
                
                SqlDataReader rd = cmd.ExecuteReader();
                return GetReaderTable(rd);
            }

        }

        public static int AddPayDocument(ApiResultEntry ent)
        {
            if (ent.IsPayDocument == false) throw new ArgumentException("ApiResultEntry is invalid");

            SqlConnection con = new SqlConnection(DatabaseParams.curr.ConnectionString);
            SqlCommand cmd;
            string s="";
            int n;
            object val;
            int k_s4=0;
            int res;

            con.Open();
            using (con)
            {
                cmd = new SqlCommand(
                    @"SELECT COUNT(id) FROM ripo_uk.dbo.PayDocs WHERE id=@id",
                    con);
                cmd.Parameters.AddWithValue("id", ent.id);
                val = cmd.ExecuteScalar();
                if (val == null || val == DBNull.Value) n = 0;
                else n = (int)val;
                if (n > 0) return 0;//такой документ уже существует

                cmd = new SqlCommand(
                    @"INSERT INTO ripo_uk.dbo.PayDocs (god,mes,id,number,ELS,GKUID,LS,sum,purpose,orgname,orginn,orgmail,addr,nkv,k_s4) VALUES (@god,@mes,@id,@number,@ELS,@GKUID,@LS,@sum,@purpose,@orgname,@orginn,@orgmail,@addr,@nkv,@k_s4)",
                    con);
                cmd.Parameters.AddWithValue("god", ent.god);
                cmd.Parameters.AddWithValue("mes", ent.mes);
                cmd.Parameters.AddWithValue("id", ent.id);
                cmd.Parameters.AddWithValue("number", ent.number);
                cmd.Parameters.AddWithValue("ELS", ent.ELS);
                cmd.Parameters.AddWithValue("GKUID", ent.GKUID);
                cmd.Parameters.AddWithValue("LS", ent.LS);
                cmd.Parameters.AddWithValue("sum", ent.sum);
                cmd.Parameters.AddWithValue("purpose", ent.purpose);
                cmd.Parameters.AddWithValue("orgname", ent.orgname);
                cmd.Parameters.AddWithValue("orginn", ent.orginn);
                cmd.Parameters.AddWithValue("orgmail", ent.orgmail);
                cmd.Parameters.AddWithValue("addr", ent.addr);
                cmd.Parameters.AddWithValue("nkv", ent.nkv);

                if (ent.LS.Length == 11)
                {
                    s = ent.LS.Substring(4, 6);
                    if (Int32.TryParse(s, out k_s4))
                        cmd.Parameters.AddWithValue("k_s4", k_s4);
                    else
                        cmd.Parameters.AddWithValue("k_s4", DBNull.Value);
                }
                else cmd.Parameters.AddWithValue("k_s4", DBNull.Value);
                                
                res = cmd.ExecuteNonQuery();
                
                return res;

            }

        }


        public static int AddGisRequest(string message_guid,string gkuid,int god,int mes)
        {
            SqlConnection con = new SqlConnection(DatabaseParams.curr.ConnectionString);
            SqlCommand cmd;            
            int n;
            object val;            

            con.Open();
            using (con)
            {
                cmd = new SqlCommand(
                    @"SELECT COUNT(message_guid) FROM ripo_uk.dbo.GisRequests WHERE message_guid=@message_guid",
                    con);
                cmd.Parameters.AddWithValue("message_guid", message_guid);
                val = cmd.ExecuteScalar();
                if (val == null || val == DBNull.Value) n = 0;
                else n = (int)val;
                if (n > 0) return 0;//такой запрос уже существует

                cmd = new SqlCommand(
                    @"INSERT INTO ripo_uk.dbo.GisRequests (message_guid,gkuid,god,mes,sent) VALUES (@message_guid,@gkuid,@god,@mes,GETDATE())",
                    con);
                cmd.Parameters.AddWithValue("message_guid", message_guid);
                cmd.Parameters.AddWithValue("gkuid", gkuid);
                cmd.Parameters.AddWithValue("god", god);
                cmd.Parameters.AddWithValue("mes", mes);    
                return cmd.ExecuteNonQuery();

            }

        }

        public static int DeleteGisRequest(string message_guid)
        {


            SqlConnection con = new SqlConnection(DatabaseParams.curr.ConnectionString);
            SqlCommand cmd;            

            con.Open();
            using (con)
            {
                cmd = new SqlCommand(
                    @"DELETE FROM ripo_uk.dbo.GisRequests WHERE message_guid=@message_guid",
                    con);
                cmd.Parameters.AddWithValue("message_guid", message_guid);                
                return cmd.ExecuteNonQuery();
            }

        }

        public static int UpdateLS(Data.Account acc)
        {

            if (acc == null) throw new ArgumentNullException("'acc' must be specified!");
            if (acc.ReasonType == null) acc.ReasonType = "";

            SqlConnection con = new SqlConnection(DatabaseParams.curr.ConnectionString);
            SqlCommand cmd;
            object val;
            int c;

            con.Open();
            using (con)
            {


                //найти есть ли уже такой ЛС
                cmd = new SqlCommand(
                    @"SELECT COUNT(*) FROM ripo_uk.dbo.LS WHERE GKUID=@GKUID",
                    con);
                cmd.Parameters.AddWithValue("GKUID", acc.GKUID);                

                val = cmd.ExecuteScalar();
                if (val == null || val == DBNull.Value) c = 0;
                else c = (int)val;

                if (c == 0)
                {
                    //удалить дублирующиеся записи
                    cmd = new SqlCommand(@"DELETE FROM ripo_uk.dbo.LS WHERE RIP_LS=@LS", con);
                    cmd.Parameters.AddWithValue("LS", acc.LS);
                    cmd.ExecuteNonQuery();

                    //вставка новой записи
                    cmd = new SqlCommand(
                    @"INSERT INTO ripo_uk.dbo.LS (RIP_LS,ELS,GKUID,AccountGUID,PremisesGUID,Reason,ReasonGUID,idf,k_post) VALUES (@LS,@ELS,@GKUID,@AccountGUID,@PremisesGUID,@Reason,@ReasonGUID,(SELECT TOP 1 idf FROM idf WHERE premises_guid=@PremisesGUID),(SELECT TOP 1 k_post FROM DataProviders WHERE orgPPAGUID=@OrgPPAGUID))",
                    con);
                    cmd.Parameters.AddWithValue("LS", acc.LS);
                    cmd.Parameters.AddWithValue("ELS", acc.ELS);
                    cmd.Parameters.AddWithValue("GKUID", acc.GKUID);
                    cmd.Parameters.AddWithValue("AccountGUID", acc.AccountGUID);
                    cmd.Parameters.AddWithValue("Reason", acc.ReasonType);
                    cmd.Parameters.AddWithValue("OrgPPAGUID", acc.OrgPPAGUID);

                    if(acc.PremisesGUID!=null&&acc.PremisesGUID.Trim()!="")
                        cmd.Parameters.AddWithValue("PremisesGUID", acc.PremisesGUID);
                    else
                        cmd.Parameters.AddWithValue("PremisesGUID", DBNull.Value);

                    if (acc.ReasonGUID != null && acc.ReasonGUID.Trim() != "")
                        cmd.Parameters.AddWithValue("ReasonGUID", acc.ReasonGUID);
                    else
                        cmd.Parameters.AddWithValue("ReasonGUID", DBNull.Value);                    

                    return cmd.ExecuteNonQuery();
                }
                else
                {
                    //обновление существующей записи
                    cmd = new SqlCommand(
                    @"UPDATE ripo_uk.dbo.LS SET RIP_LS=@LS,ELS=@ELS,AccountGUID=@AccountGUID,PremisesGUID=@PremisesGUID,Reason=@Reason,ReasonGUID=@ReasonGUID,k_post=(SELECT TOP 1 k_post FROM DataProviders WHERE orgPPAGUID=@OrgPPAGUID) WHERE GKUID=@GKUID", con);
                    cmd.Parameters.AddWithValue("LS", acc.LS);
                    cmd.Parameters.AddWithValue("ELS", acc.ELS);
                    cmd.Parameters.AddWithValue("GKUID", acc.GKUID);
                    cmd.Parameters.AddWithValue("AccountGUID", acc.AccountGUID);
                    cmd.Parameters.AddWithValue("Reason", acc.ReasonType);
                    cmd.Parameters.AddWithValue("OrgPPAGUID", acc.OrgPPAGUID);

                    if (acc.PremisesGUID != null && acc.PremisesGUID.Trim() != "")
                        cmd.Parameters.AddWithValue("PremisesGUID", acc.PremisesGUID);
                    else
                        cmd.Parameters.AddWithValue("PremisesGUID", DBNull.Value);

                    if (acc.ReasonGUID != null && acc.ReasonGUID.Trim() != "")
                        cmd.Parameters.AddWithValue("ReasonGUID", acc.ReasonGUID);
                    else
                        cmd.Parameters.AddWithValue("ReasonGUID", DBNull.Value);
                    return cmd.ExecuteNonQuery();
                }
            }

        }

        /// <summary>
        /// Обновляет или добавляет данные о внутренних идентифкаторах ПУ ГИС ЖКХ в таблице PU_guids
        /// </summary>
        /// <param name="dev">Параметры прибора учета (обязательны все, кроме ResourceGUID)</param>
        /// <returns>Число обновленных записей</returns>
        public static int UpdatePU(MDevice dev)
        {

            if (dev == null) throw new ArgumentNullException("'dev' must be specified!");
            

            SqlConnection con = new SqlConnection(DatabaseParams.curr.ConnectionString);
            SqlCommand cmd;
            object val;
            int c;
            int n=0;

            con.Open();
            using (con)
            {
                cmd = new SqlCommand(
                    @"SELECT COUNT(*) FROM ripo_uk.dbo.PU_guids WHERE idf=@idf",
                    con);
                cmd.Parameters.AddWithValue("idf", dev.idf);

                val = cmd.ExecuteScalar();
                if (val == null || val == DBNull.Value) c = 0;
                else c = (int)val;

                if (c == 0)
                {
                    
                    //вставка новой записи
                    cmd = new SqlCommand(
                    @"INSERT INTO ripo_uk.dbo.PU_guids (idf,DeviceGUID,DeviceVGUID,AccountGUID,PremisesGUID) VALUES (@idf,@DeviceGUID,@DeviceVGUID,@AccountGUID,@PremisesGUID)",
                    con);
                    cmd.Parameters.AddWithValue("idf", dev.idf);
                    cmd.Parameters.AddWithValue("DeviceGUID", dev.DeviceGUID);                    
                    cmd.Parameters.AddWithValue("AccountGUID", dev.AccountGUID);                    
                    cmd.Parameters.AddWithValue("PremisesGUID", dev.PremisesGUID);
                    cmd.Parameters.AddWithValue("DeviceVGUID", dev.DeviceVGUID);
                    
                    n+= cmd.ExecuteNonQuery();
                }
                else
                {
                    //обновление существующей записи
                    cmd = new SqlCommand(
                    @"UPDATE ripo_uk.dbo.PU_guids SET DeviceGUID=@DeviceGUID,DeviceVGUID=@DeviceVGUID, AccountGUID=@AccountGUID,PremisesGUID=@PremisesGUID WHERE idf=@idf", con);
                    cmd.Parameters.AddWithValue("idf", dev.idf);
                    cmd.Parameters.AddWithValue("DeviceGUID", dev.DeviceGUID);
                    cmd.Parameters.AddWithValue("AccountGUID", dev.AccountGUID);
                    cmd.Parameters.AddWithValue("PremisesGUID", dev.PremisesGUID);
                    cmd.Parameters.AddWithValue("DeviceVGUID", dev.DeviceVGUID);
                    n+= cmd.ExecuteNonQuery();
                }

                //обновление gisgkh_num
                cmd = new SqlCommand(
                @"UPDATE ripo_uk.dbo.PU SET gisgkh_num=@gisgkh_num WHERE idf=@idf AND gisgkh_num IS NULL", con);
                cmd.Parameters.AddWithValue("idf", dev.idf);
                cmd.Parameters.AddWithValue("gisgkh_num", dev.gisgkh_num);                
                cmd.ExecuteNonQuery();
            }
            return n;
        }

        public static int InsertPkz(PkzEntry pkz)
        {

            if (pkz == null) throw new ArgumentNullException("'pkz' must be specified!");
            if (pkz.DatePkz.CompareTo(new DateTime(2000, 1, 1)) < 0)
            {
                pkz.DatePkz = DateTime.Now;
            }

            SqlConnection con = new SqlConnection(DatabaseParams.curr.ConnectionString);
            SqlCommand cmd;
            object val;
            string idf;
            int k_s4;
            int k_s23;
            string t_s23_knam,prim;
            
            int n = 0;

            con.Open();
            using (con)
            {
                cmd = new SqlCommand(
                    @"SELECT TOP 1 idf FROM ripo_uk.dbo.PU_guids WHERE DeviceGUID=@DeviceGUID",
                    con);
                cmd.Parameters.AddWithValue("DeviceGUID", pkz.DeviceGUID);

                val = cmd.ExecuteScalar();
                if (val == null || val == DBNull.Value) return 0;
                idf = val.ToString();


                cmd = new SqlCommand(
                    @"SELECT k_s4,k_s23,t_s23_knam,prim FROM ripo.dbo.lpu WHERE idf=@idf",
                    con);
                cmd.Parameters.AddWithValue("idf", idf);

                SqlDataReader rd = cmd.ExecuteReader();
                using (rd)
                {
                    if (rd.Read() == false) return 0;

                    k_s4 = Convert.ToInt32(rd["k_s4"]);
                    k_s23 = Convert.ToInt32(rd["k_s23"]);
                    t_s23_knam = (rd["t_s23_knam"]).ToString().Trim();
                    prim = (rd["prim"]).ToString().Trim();
                }

                switch (t_s23_knam)
                {
                    case "Г/В": t_s23_knam += " счетчик горячей воды"; break;
                    case "Х/В": t_s23_knam += " счетчик холодной воды"; break;
                    case "ЭЛ.2": t_s23_knam += " эл. счетчик 2 тариф."; break;
                    case "ЭЛ.1": t_s23_knam += " эл. счетчик 1 тариф."; break;
                    case "ОТОПЛ": t_s23_knam += " счетчик отопления"; break;
                }

                //вставка новой записи
                cmd = new SqlCommand(
                @"INSERT INTO [PokSRV].[dbo].[NewPok] (DataPok,LS,ID_PU,CodePU,TypePU,CodePok,prim,pok,PokID,DataObrab,SourcePok,kod_oper) VALUES (@DatePok,@k_s4,@idf,@k_s23,@TypePU,@n_pkz,@prim,@PkzValue,NEWID ( ) ,GETDATE(),1,0)",
                con);
                cmd.Parameters.AddWithValue("DatePok", pkz.DatePkz);
                cmd.Parameters.AddWithValue("k_s4", k_s4);
                cmd.Parameters.AddWithValue("idf",idf);
                cmd.Parameters.AddWithValue("k_s23", k_s23);
                cmd.Parameters.AddWithValue("TypePU", t_s23_knam);
                cmd.Parameters.AddWithValue("n_pkz", pkz.n_pkz);
                cmd.Parameters.AddWithValue("prim", prim);
                cmd.Parameters.AddWithValue("PkzValue", pkz.Value);

                n += cmd.ExecuteNonQuery();


            }
            return n;

        }

        public static int UpdateOrgUsl(int k_post, IEnumerable<Data.NsiItem> items,out string info)
        {
            SqlConnection con = new SqlConnection(DatabaseParams.curr.ConnectionString);
            SqlCommand cmd;
            con.Open();
            using (con)
            {
                
                int n = 0;
                object val;
                int c;
                info = "";

                foreach (Data.NsiItem item in items)//обновление данных
                {
                    if (item.Name == null) item.Name = "";
                    if (item.Name2 == null) item.Name2 = "";

                    //найти есть ли такая услуга
                    cmd = new SqlCommand(
                        @"SELECT COUNT(*) FROM ripo_uk.dbo.usl WHERE name=@name",
                        con);                    
                    cmd.Parameters.AddWithValue("name", item.Name);

                    val = cmd.ExecuteScalar();
                    if (val == null || val == DBNull.Value) c = 0;
                    else c = (int)val;

                    if (c == 0) { info += "Нет услуги "+item.Name+Environment.NewLine; continue; }//-> услуги НЕТ!

                    //найти есть ли уже такой элемент
                    cmd = new SqlCommand(
                        @"SELECT COUNT(*) FROM ripo_uk.dbo.gisgkh_usl WHERE k_post=@k_post AND id_usl=(SELECT TOP 1 id FROM usl WHERE name=@name)",con);
                    cmd.Parameters.AddWithValue("k_post", k_post);
                    cmd.Parameters.AddWithValue("name", item.Name);

                    val = cmd.ExecuteScalar();
                    if (val == null || val == DBNull.Value) c = 0;
                    else c = (int)val;

                    if (c == 0)
                    {

                        //вставка новой записи
                        cmd = new SqlCommand(
                        @"INSERT INTO ripo_uk.dbo.gisgkh_usl (k_post,id_usl,gisgkh_num,guid) VALUES  (@k_post,(SELECT TOP 1 id FROM usl WHERE name=@Name),@Code,@GUID)",con);
                        cmd.Parameters.AddWithValue("k_post", k_post);
                        cmd.Parameters.AddWithValue("GUID", item.GUID);
                        cmd.Parameters.AddWithValue("Code", item.Code);                        
                        cmd.Parameters.AddWithValue("Name", item.Name);
                        
                        n += cmd.ExecuteNonQuery();
                    }
                    else
                    {
                        //обновление существующей записи
                        cmd = new SqlCommand(
                        @"UPDATE ripo_uk.dbo.gisgkh_usl SET gisgkh_num=@Code,guid=@GUID WHERE k_post=@k_post AND id_usl=(SELECT TOP 1 id FROM usl WHERE name=@Name)", con);

                        cmd.Parameters.AddWithValue("k_post", k_post);
                        cmd.Parameters.AddWithValue("GUID", item.GUID);
                        cmd.Parameters.AddWithValue("Code", item.Code);                        
                        cmd.Parameters.AddWithValue("Name", item.Name);
                        
                        n += cmd.ExecuteNonQuery();
                    }
                }//end foreach

                /*Жилищные услуги*/

                return n;
            }
        }

        public static int SetPremisesGuid(string house_guid,string num,string nkom,string idf,string premises_guid)
        {

            if (nkom == null) nkom = "";

            SqlConnection con = new SqlConnection(DatabaseParams.curr.ConnectionString);
            SqlCommand cmd;
            object val;
            int c;

            con.Open();
            using (con)
            {
                

                //найти есть ли уже такое помещение
                cmd = new SqlCommand(
                    @"SELECT COUNT(*) FROM ripo_uk.dbo.idf WHERE house_guid=@house_guid AND number_room=@num AND nkom=@nkom",
                    con);
                cmd.Parameters.AddWithValue("house_guid", house_guid);
                cmd.Parameters.AddWithValue("num", num);
                cmd.Parameters.AddWithValue("nkom", nkom);

                val=cmd.ExecuteScalar();
                if (val == null || val == DBNull.Value) c = 0;
                else c = (int)val;
                    
                if (c == 0)
                {
                    //удалить помещения с таким же идентификатором
                    cmd = new SqlCommand(@"DELETE FROM ripo_uk.dbo.idf WHERE idf=@idf", con);
                    cmd.Parameters.AddWithValue("idf", idf);
                    cmd.ExecuteNonQuery();

                    //вставка новой записи
                    cmd = new SqlCommand(
                    @"INSERT INTO ripo_uk.dbo.idf (house_guid,address,number_room,nkom,idf,premises_guid) VALUES (@house_guid,(SELECT 'обл. Свердловская, г. Нижний Тагил, '+street+', д.'+nhouse FROM ripo_uk.dbo.houses WHERE  house_guid=@house_guid),@num,@nkom,@idf,@premises_guid)",
                    con);
                    cmd.Parameters.AddWithValue("house_guid", house_guid);
                    cmd.Parameters.AddWithValue("num", num);
                    cmd.Parameters.AddWithValue("nkom", nkom);
                    cmd.Parameters.AddWithValue("idf", idf);
                    cmd.Parameters.AddWithValue("premises_guid", premises_guid);
                    return cmd.ExecuteNonQuery();
                }
                else
                {
                    //обновление существующей записи
                    cmd = new SqlCommand(
                    @"UPDATE ripo_uk.dbo.idf SET premises_guid=@premises_guid,idf=@idf WHERE house_guid=@house_guid AND number_room=@num AND nkom=@nkom",con);
                    cmd.Parameters.AddWithValue("house_guid", house_guid);
                    cmd.Parameters.AddWithValue("num", num);
                    cmd.Parameters.AddWithValue("nkom", nkom);
                    cmd.Parameters.AddWithValue("idf", idf);
                    cmd.Parameters.AddWithValue("premises_guid", premises_guid);
                    return cmd.ExecuteNonQuery();
                }
            }

        }


        public static int SetNsiData(string SpravNumber,IEnumerable<Data.NsiItem> items)
        {
            SqlConnection con = new SqlConnection(DatabaseParams.curr.ConnectionString);
            SqlCommand cmd;
            con.Open();
            using (con)
            {
                //удалить старые записи
                if (SpravNumber != "")
                {
                    cmd = new SqlCommand(
                            @"DELETE FROM ripo_uk.dbo.nsidata WHERE SpravNumber=@SpravNumber",
                            con);
                    cmd.Parameters.AddWithValue("SpravNumber", SpravNumber);
                    cmd.ExecuteNonQuery();
                }
                int n = 0;

                foreach (Data.NsiItem item in items)//обновление данных
                {
                    if (item.Name == null) item.Name = "";
                    if (item.Name2 == null) item.Name2 = "";


                    object val;
                    int c;


                    //найти есть ли уже такой элемент
                    cmd = new SqlCommand(
                        @"SELECT COUNT(*) FROM ripo_uk.dbo.nsidata WHERE GUID=@GUID",
                        con);
                    cmd.Parameters.AddWithValue("GUID", item.GUID);
                    
                    val = cmd.ExecuteScalar();
                    if (val == null || val == DBNull.Value) c = 0;
                    else c = (int)val;

                    if (c == 0)
                    {                      

                        //вставка новой записи
                        cmd = new SqlCommand(
                        @"INSERT INTO ripo_uk.dbo.nsidata (SpravNumber,GUID,Code,Created,Name,Name2,[Values]) VALUES  (@SpravNumber,@GUID,@Code,@Created,@Name,@Name2,@Values)",
                        con);
                        cmd.Parameters.AddWithValue("SpravNumber", item.SpravNumber);
                        cmd.Parameters.AddWithValue("GUID", item.GUID);
                        cmd.Parameters.AddWithValue("Code", item.Code);
                        cmd.Parameters.AddWithValue("Created", item.Created);
                        cmd.Parameters.AddWithValue("Name", item.Name);
                        cmd.Parameters.AddWithValue("Name2", item.Name2);
                        cmd.Parameters.AddWithValue("Values", item.Values);
                        n += cmd.ExecuteNonQuery();
                    }
                    else
                    {
                        //обновление существующей записи
                        cmd = new SqlCommand(
                        @"UPDATE ripo_uk.dbo.nsidata SET SpravNumber=@SpravNumber,Code=@Code,Created=@Created,Name=@Name,Name2=@Name2,[Values]=@Values WHERE GUID=@GUID", con);
                        cmd.Parameters.AddWithValue("SpravNumber", item.SpravNumber);
                        cmd.Parameters.AddWithValue("GUID", item.GUID);
                        cmd.Parameters.AddWithValue("Code", item.Code);
                        cmd.Parameters.AddWithValue("Created", item.Created);
                        cmd.Parameters.AddWithValue("Name", item.Name);
                        cmd.Parameters.AddWithValue("Name2", item.Name2);
                        cmd.Parameters.AddWithValue("Values", item.Values);
                        n+= cmd.ExecuteNonQuery();
                    }
                }
                return n;
            }
        }

        static SqlConnection GetOrgPPAGUID_con = null;

        public static string GetOrgPPAGUID(int k_post)
        {
            if (GetOrgPPAGUID_con == null){ GetOrgPPAGUID_con = new SqlConnection(DatabaseParams.curr.ConnectionString); }

            SqlConnection con = GetOrgPPAGUID_con;
                        
            if(con.State!=ConnectionState.Open)con.Open();

            
                SqlCommand cmd = new SqlCommand(
                    @"SELECT orgPPAGUID FROM ripo_uk.dbo.DataProviders WHERE k_post=@k_post",
                    con);
                cmd.Parameters.AddWithValue("k_post",k_post);
                object result = cmd.ExecuteScalar();
                if (result == null || result == DBNull.Value) return null;
                else return result.ToString();
        }

        public static List<Tuple<int,string>> GetAllOrgPPAGUID()
        {

            if (GetOrgPPAGUID_con == null) { GetOrgPPAGUID_con = new SqlConnection(DatabaseParams.curr.ConnectionString); }

            SqlConnection con = GetOrgPPAGUID_con;

            if (con.State != ConnectionState.Open) con.Open();

            List<Tuple<int, string>> res = new List<Tuple<int, string>>(100);
            Tuple<int, string> item;
                        
            
                SqlCommand cmd = new SqlCommand(
                    @"SELECT k_post,orgPPAGUID FROM ripo_uk.dbo.DataProviders WHERE k_post<>0",
                    con);

                SqlDataReader rd = cmd.ExecuteReader();

                using (rd)
                {
                    while (true)
                    {
                        if (rd.Read() == false) break;
                        item = new Tuple<int, string>(Convert.ToInt32(rd["k_post"]),rd["OrgPPAGUID"].ToString());
                        res.Add(item);
                    }
                }

                return res;
        }

    }//end class
    
    /// <summary>
    /// Класс представляет данные организации, для использования в списках элементов
    /// </summary>
    public class ListEntry
    {
        public int id;//код
        public string name;//название
        public bool ls_flag;//передана информация об ЛС
        public bool usl_flag;//передана информация об услугах
        public bool pu_data_flag;//переданы данные приборов учета
        public bool pu_numbers_flag;//переданы номера приборов учета в ГИС ЖКХ
    }
}
