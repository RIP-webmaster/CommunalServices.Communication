/* Communal services system integration 
 * Copyright (c) 2021,  Svitkin V.G. 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;


namespace GISGKHIntegration
{
    /// <summary>
    /// Класс представляет платежный документ.
    /// Содержит информацию о начислениях в виде, подготовленном для загрузки в ГИС ЖКХ. 
    /// 
    /// 18.11.2016
    /// </summary>
    public class Kvit
    {
        public DataTable raw_data = null;//исходные данные из RIPO для формирования квитанции
        public DataTable raw_dolg_data = null;

        /*Общие параметры*/
        public int god;//год платежного периода
        public int mes;//месяц платежного периода
        public int k_s4;//код ЛС
        public string GKUID = "";//идентификатор ЖКУ из ГИС ЖКХ
        
        public List<GisgkhUsl> usl_list = null;//список услуг
        public decimal pl_o = 0.0M;//площадь по лицевому счету

        public decimal dolg = 0.0M;//задолженность на начало месяца
        
        public bool calculated = false;//квитанция рассчитана

        /*Начисления*/
        public decimal pen = 0.0M;
        public decimal cap_rem_sum = 0.0M;
        public decimal cap_rem_per = 0.0M;
        public decimal cap_remont = 0.0M;//взнос на кап.ремонт
        public List<KvitEntry> entries = null;//строки квитанции

        /// <summary>
        /// Формирует новый платежный документ, изначально в нерассчитанном состоянии
        /// </summary>
        /// <param name="g">Год</param>
        /// <param name="m">Месяц</param>
        /// <param name="s4">Код ЛС</param>
        /// <param name="gku">Идентификатор ЖКУ</param>
        /// <param name="dt">Исходные данные начислений</param>
        /// <param name="usl">Услуги</param>
        /// <param name="pl">Площадь</param>
        public Kvit(int g,int m,int s4,string gku,DataTable dt, 
            List<GisgkhUsl> usl,decimal pl,DataTable dolg_data)
        {
            this.god = g; this.mes = m;
            this.k_s4 = s4; this.GKUID = gku;
            this.pl_o = pl;

            this.raw_data = dt;
            this.raw_dolg_data = dolg_data;
            this.usl_list = usl;
            this.calculated = false;
        }

        /// <summary>
        /// Загружает информацию о начислениях из базы данных
        /// </summary>
        /// <param name="god">Год</param>
        /// <param name="mes">Месяц</param>
        /// <param name="k_s4">Код ЛС</param>
        /// <returns></returns>
        public static DataTable GetNachisl(int god, int mes, int k_s4)
        {
            SqlConnection con = new SqlConnection(DatabaseParams.curr.ConnectionString);
            con.Open();
            using (con)
            {
                SqlCommand cmd;
                SqlDataReader rd;

                cmd = new SqlCommand("GetNachisl", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("god", god);
                cmd.Parameters.AddWithValue("mes", mes);
                cmd.Parameters.AddWithValue("k_s4", k_s4);
                rd = cmd.ExecuteReader();
                return DB.GetReaderTable(rd);
            }
        }

        /// <summary>
        /// Загружает информацию о долгах из базы данных
        /// </summary>
        /// <param name="god">Год</param>
        /// <param name="mes">Месяц</param>
        /// <param name="k_s4">Код ЛС</param>
        /// <returns></returns>
        public static DataTable GetDolg(int god, int mes, int k_s4)
        {
            SqlConnection con = new SqlConnection(DatabaseParams.curr.ConnectionString);
            con.Open();
            using (con)
            {
                SqlCommand cmd;
                SqlDataReader rd;

                cmd = new SqlCommand("GetDolg", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("god", god);
                cmd.Parameters.AddWithValue("mes", mes);
                cmd.Parameters.AddWithValue("k_s4", k_s4);
                rd = cmd.ExecuteReader();
                return DB.GetReaderTable(rd);
            }
        }

        /// <summary>
        /// Рассчитывает эту квитанцию, формируя строки, соответствующие услугам ГИС ЖКХ
        /// </summary>
        public void Calculate(bool fCapRem = false)//30.11.2017 - Признак выделения кап.ремонта
        {
            KvitEntry e;
            decimal tarif;
            bool found;

            entries = new List<KvitEntry>();
            calculated = false;

            //поиск начислений по которым не определена услуга
            foreach (DataRow row in raw_data.Rows)
            {
                found = false;
                foreach (GisgkhUsl usl in usl_list)
                {
                    if (usl.id == ((int)row["id_usl"])) found = true;
                }

                if (!found)
                {
                    (row["id_usl"]) = 8;//перекидываем на "Содержание"
                    row["type"] = 1; row["name"] = "Плата за содержание жилого помещения";
                }
            }

            //поиск долгов по которым не определена услуга
            foreach (DataRow row in raw_dolg_data.Rows)
            {
                found = false;
                foreach (GisgkhUsl usl in usl_list)
                {
                    if (usl.id == ((int)row["id_usl"])) found = true;
                }

                if (!found)
                {
                    (row["id_usl"]) = 8;//перекидываем на "Содержание"
                    row["type"] = 1; row["name"] = "Плата за содержание жилого помещения";
                }
            }

            //формирование строк квитанции
            foreach (GisgkhUsl usl in usl_list)
            {
                e = new KvitEntry();
                e.tarif = 0.0M;
                e.odom_vol = 0.0M;
                e.odom_nach = 0.0M;
                e.ind_vol = 0.0M;
                e.ind_nach = 0.0M;
                e.all_nach = 0.0M;
                e.pereras = 0.0M;
                e.sum = 0.0M;
                e.ind_sum = 0.0M;
                e.dolg = 0.0M;

                foreach (DataRow row in raw_dolg_data.Rows)//обработка долгов и пеней
                {
                    if (((int)row["id_usl"]) == usl.id)
                    {
                        e.dolg += Convert.ToDecimal(row["dolg"]);
                    }
                }

                switch (usl.type) //обработка текущих начислений
                {
                    case 1://содержание и текущий ремонт
                        
                        foreach (DataRow row in raw_data.Rows)
                        {
                            int type = Convert.ToInt32(row["type"]);

                            if (((int)row["id_usl"]) == usl.id ||
                                (((int)row["id_usl"]) == DB.ID_CAP_REMONT && fCapRem == false) ||
                                type == 4)
                            {
                                //цена суммируется - кроме комм. ресурсов на СОИ
                                if (type != 4)
                                {
                                    e.tarif += Convert.ToDecimal(row["cena"]);
                                }

                                //начисления суммируются
                                e.ind_nach += Convert.ToDecimal(row["it_dengi"]);
                                e.sum += Convert.ToDecimal(row["os_dengi"]);

                                //перерасчеты
                                e.pereras += Convert.ToDecimal(row["per_dengi"]);
                                e.per_descr = row["per_descr"].ToString();
                            }
                        }

                        e.ind_vol = pl_o;//объем соответствует площади
                        e.odom_vol = (decimal)0;//общедомовых нет                       
                        e.all_nach = e.ind_nach;//общая сумма

                        /*25.07.2017 Svitkin*/
                        if (e.ind_vol > 0) e.tarif = Math.Round(e.sum / e.ind_vol, 2);
                        break;
                    case 2: //коммунальные услуги                           
                        foreach (DataRow row in raw_data.Rows)
                        {
                            if (((int)row["id_usl"]) == usl.id)
                            {
                                tarif = Convert.ToDecimal(row["cena"]);
                                if (tarif > e.tarif) e.tarif = tarif;

                                //суммирование начислений и объемов
                                if ((bool)row["odom"])
                                {
                                    e.odom_vol += Convert.ToDecimal(row["rashod_r"]);
                                    e.odom_nach += Convert.ToDecimal(row["it_dengi"]);
                                }
                                else
                                {
                                    e.ind_vol += Convert.ToDecimal(row["rashod_r"]);
                                    e.ind_nach += Convert.ToDecimal(row["it_dengi"]);
                                    e.ind_sum += Convert.ToDecimal(row["os_dengi"]);
                                }

                                e.sum += Convert.ToDecimal(row["os_dengi"]);
                                //перерасчеты
                                e.pereras += Convert.ToDecimal(row["per_dengi"]);
                                e.per_descr = row["per_descr"].ToString();
                            }
                        }
                        //итоги для данной КУ                        

                        e.all_nach = e.odom_nach + e.ind_nach;//общая сумма
                        if (e.odom_vol + e.ind_vol > 0)
                            e.tarif = Math.Round(e.sum / (e.odom_vol + e.ind_vol), 2);
                        break;

                    case 3://дополнительные услуги
                        foreach (DataRow row in raw_data.Rows)
                        {
                            if ((int)row["id_usl"] == DB.ID_CAP_REMONT && fCapRem != false)//кап.ремонт
                            {
                                this.cap_rem_per = Convert.ToDecimal(row["per_dengi"]);
                                this.cap_rem_sum = Convert.ToDecimal(row["os_dengi"]);
                                this.cap_remont = Convert.ToDecimal(row["it_dengi"]);
                                continue;
                            }

                            //суммирование начилений
                            if (((int)row["id_usl"]) == usl.id)
                            {
                                e.tarif += Convert.ToDecimal(row["cena"]);

                                if ((bool)row["odom"])
                                {
                                    e.odom_nach += Convert.ToDecimal(row["it_dengi"]);
                                }
                                else
                                {
                                    e.ind_nach += Convert.ToDecimal(row["it_dengi"]);
                                }

                                e.sum += Convert.ToDecimal(row["os_dengi"]);
                                //перерасчеты
                                e.pereras += Convert.ToDecimal(row["per_dengi"]);
                                e.per_descr = row["per_descr"].ToString();
                            }

                        }
                        e.ind_vol = (decimal)1;
                        e.all_nach = e.odom_nach + e.ind_nach;//общая сумма
                        e.tarif = e.sum;/*20.06.2017 (Исправления для "уборки л.к.")*/
                        break;
                }

                /*Общие параметры услуги*/
                e.usl_id = usl.id;
                e.usl_name = usl.name;
                e.type = usl.type;
                e.usl_guid = usl.gisgkh_guid;
                e.usl_code = usl.gisgkh_binding;

                /*Исключается кап. ремонт и нулевые строки*/
                if (e.usl_id != DB.ID_CAP_REMONT && (e.all_nach != 0.0M || e.dolg != 0.0M))
                {
                    entries.Add(e);
                }

            }
            //расчет завершен
            calculated = true;

        }

    }

    /// <summary>
    /// Класс представляет строку платежного документа
    /// </summary>
    public class KvitEntry
    {
        public int usl_id;//номер услуги
        public string usl_name;//название услуги
        public string usl_guid;
        public string usl_code;
        public int type;//тип услуги

        public decimal odom_vol;//объем общедомового потребления (для коммунальных услуг)
        public decimal ind_vol;//объем индивидуального потребления

        public decimal tarif;//тариф (цена)
        public decimal sum;//начисление
        public decimal ind_sum;

        public decimal pereras;
        public decimal odom_nach;//сумма к оплате общедомовых начислений (для коммунальных услуг)
        public decimal ind_nach;//сумма к оплате индивидуальных начислений
        public decimal all_nach;//сумма к оплате всех начислений

        public decimal dolg;//долг на начало периода + пени
        public string per_descr = "";

    }
}
