/* Communal services system integration 
 * Copyright (c) 2021,  Svitkin V.G. 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GISGKHIntegration.Data
{
    public class Payment
    {
        public const int RS_ERROR = -1;
        public const int RS_SENT = 0;
        public const int RS_RECEIVED = 1;
        public const int RS_PROCESSING = 2;
        public const int RS_PROCESSED = 3;

        public const int IS_ERROR = -1;
        public const int IS_UNKNOWN = 0;
        public const int IS_SUCCESS = 1;
        public const int IS_AKTIR_WAIT = 2;
        public const int IS_AKTIR = 3;

        //Payment data
        public string Pay_ID = "";
        public DateTime Pay_Date;
        public string LS = "";
        public int god;
        public int mes;
        public decimal total_sum;

        //organisation data
        public string OrgINN = "";
        public string OrgKPP = "";
        public string OrgName = "";

        //GISGKH data
        public string message_guid = "";
        public string transport_guid = "";
        public int RequestState=-1000;
        public int ImportState=0;
        public string UniqueNumber = "";
        public string EntityGUID = "";
        public DateTime ImportDate;
        public string ErrorCode = "?";
        public string ErrorMessage = "Результат неизвестен";
        public string StackTrace = "";

        public override string ToString()
        {
            StringBuilder b = new StringBuilder(300);
            b.AppendLine("*** " + Pay_ID+" ***");
            b.AppendLine("Дата платежа: " + Pay_Date.ToString());
            b.AppendLine("ЛС: " + LS);
            b.AppendLine("Период: " + god.ToString()+"-"+mes.ToString());
            b.AppendLine("Общая сумма: " + total_sum.ToString());
            b.AppendLine();

            if (OrgName != null && OrgName.Length > 0)
            {
                b.AppendLine("Поставщик: " + OrgName);
                b.AppendLine("ИНН поставщика: " + OrgINN);
                b.AppendLine("КПП поставщика: " + OrgKPP);
            }

            b.AppendLine();
            switch (RequestState)
            {
                case RS_ERROR: 
                    b.AppendLine("При отправке запроса в ГИС ЖКХ произошла ошибка");
                    b.AppendLine(ErrorCode+": "+ErrorMessage);
                    if (StackTrace != null && StackTrace.Length > 0)
                    {
                        b.AppendLine("StackTrace:");
                        b.AppendLine(StackTrace);
                    }
                break;
                case RS_SENT: b.AppendLine("Отправлен запрос в ГИС ЖКХ"); break;
                case RS_RECEIVED: b.AppendLine("Запрос получен ГИС ЖКХ"); break;
                case RS_PROCESSING: b.AppendLine("Запрос в обработке ГИС ЖКХ"); break;
                case RS_PROCESSED:
                    b.AppendLine("Запрос обработан ГИС ЖКХ");
                    switch (ImportState)
                    {
                        case IS_ERROR:
                            b.AppendLine("При обработке запроса произошла ошибка");
                            b.AppendLine(ErrorCode + ": " + ErrorMessage);
                            if (StackTrace != null && StackTrace.Length > 0)
                            {
                                b.AppendLine("StackTrace:");
                                b.AppendLine(StackTrace);
                            }
                            break;
                        case IS_SUCCESS:
                            b.AppendLine("Импорт данных прошел успешно");
                            b.AppendLine("Уникальный номер: "+UniqueNumber);
                            b.AppendLine("Дата импорта: " + ImportDate);
                            break;
                        default: b.AppendLine("Результат обработки неизвестен"); break;
                    }

                    break;
                default: b.AppendLine("Состояние запроса неизвестно"); break;
            }

            b.AppendLine();
            if (message_guid != null && message_guid.Length > 0)
            {
                b.AppendLine("Message GUID: " + message_guid);
            }
            if (transport_guid != null && transport_guid.Length > 0)
            {
                b.AppendLine("Transport GUID: " + transport_guid);
            }
            if (EntityGUID != null && EntityGUID.Length > 0)
            {
                b.AppendLine("Entity GUID: " + EntityGUID);
            }
            return b.ToString();
        }

    }
}
