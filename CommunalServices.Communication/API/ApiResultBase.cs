/* Communal services system integration 
 * Copyright (c) 2021,  Svitkin V.G. 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;
using CommunalServices.Communication.Data;

namespace GISGKHIntegration
{
    public class ApiResultBase
    {
        public bool exception = false;
        public string text = "";
        public DateTime date_query;
        public decimal query_duration = 0;
        public Exception ex = null;
        public string in_xml = "";
        public string out_xml = "";

        public bool error = false;
        public string ErrorCode;
        public string ErrorMessage;
        public string StackTrace;

        public string messageGUID = "";

        public int RequestState { get; set; }

        public static T GetExceptionResult<T>(
            string name, Exception exc, DateTime date_query, decimal query_duration
            ) where T : ApiResultBase, new()
        {
            T apires = new T();
            apires.in_xml = GisAPI.LastRequest;
            apires.out_xml = GisAPI.LastResponce;
            StringBuilder sb = new StringBuilder(300);
            sb.AppendLine(name + " failed!");
            sb.AppendLine(exc.ToString());
            sb.AppendLine();
            sb.AppendLine("Дата и время запроса: " + date_query);
            sb.AppendLine("Длительность обработки запроса: " + (query_duration).ToString("F3") + " c.");
            apires.text = (sb.ToString());
            apires.ex = exc;
            apires.exception = true;
            return apires;
        }

        public static void InitExceptionResult(
            ApiResultBase apires,string name, Exception exc
            ) 
        {
            apires.in_xml = GisAPI.LastRequest;
            apires.out_xml = GisAPI.LastResponce;
            StringBuilder sb = new StringBuilder(300);
            sb.AppendLine(name + " failed!");
            sb.AppendLine(exc.ToString());
            sb.AppendLine();
            sb.AppendLine("Дата и время запроса: " + apires.date_query);
            sb.AppendLine("Длительность обработки запроса: " + (apires.query_duration).ToString("F3") + " c.");
            apires.text = (sb.ToString());
            apires.ex = exc;
            apires.exception = true;
            apires.ErrorCode = exc.GetType().ToString();
            apires.ErrorMessage = exc.Message;
            apires.StackTrace = exc.ToString();
        }

        public override string ToString()
        {
            return this.text;
        }

        public bool IsSuccessfullyProcessed()
        {
            if (this.error || this.exception) return false;

            return this.RequestState == RequestStates.RS_PROCESSED;
        }
    }
}
