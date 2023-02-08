/* Communal services system integration 
 * Copyright (c) 2023,  Svitkin V.G. 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;
using CommunalServices.Communication.API;
using CommunalServices.Communication.Data;
using GISGKHIntegration;
using GKH;

namespace CommunalServices.Communication.ApiRequests
{
    /// <summary>
    /// Размещение информации о размере платы за жилое помещение (тарифах на жил. услуги) в уставе ТСЖ
    /// </summary>
    public class ImportCharterTarifApiRequest : ApiRequestBase
    {
        public ImportCharterTarifApiRequest(string orgPPAGUID, int k_post, string houseGuid, string charterVersionGUID,
            DateTime dtStart, DateTime dtEnd, HouseUslData[] items)
        {
            this.OrgPpaGuid = orgPPAGUID;
            this.KPost = k_post;
            this.HouseGUID = houseGuid;
            this.CharterVersionGUID = charterVersionGUID;
            this.Start = dtStart;
            this.End = dtEnd;
            this.Items = items;
        }

        public string HouseGUID { get; set; }
        public string CharterVersionGUID { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public HouseUslData[] Items { get; set; }

        public override ApiResultBase Send()
        {
            lock (GisAPI.csLock)
            {
                GisAPI.LastRequest = ""; GisAPI.LastResponce = "";
                var proxy = new HouseManagementPortsTypeAsyncClient("HouseManagementPortAsync");
                ApiResult apires = new ApiResult();

                //формирование входных параметров запроса

                RequestHeader hdr = new RequestHeader();//заголовок запроса

                hdr.Date = DateTime.Now;
                hdr.MessageGUID = Guid.NewGuid().ToString();
                hdr.ItemElementName = ItemChoiceType4.orgPPAGUID;
                hdr.Item = this.OrgPpaGuid;

                hdr.IsOperatorSignature = true;
                hdr.IsOperatorSignatureSpecified = true;

                var request = new importCharterRequest();
                request.Id = "signed-data-container";
                
                var info = new importCharterRequestPlaceCharterPaymentsInfo();
                info.BeginDate = this.Start;
                info.EndDate = this.End;
                info.CharterVersionGUID = this.CharterVersionGUID;
                
                decimal tarifSod = 0.0M;
                var outUsl = new CharterPaymentsInfoTypeServicePayment[this.Items.Length];

                for (int i = 0; i < this.Items.Length; i++)
                {
                    if (string.Equals(this.Items[i].UslName, "Содержание и текущий ремонт", StringComparison.Ordinal))
                    {
                        tarifSod = this.Items[i].UslTarif;
                    }

                    outUsl[i] = new CharterPaymentsInfoTypeServicePayment();
                    outUsl[i].Service = new nsiRef();
                    outUsl[i].Service.Code = this.Items[i].UslCode;
                    outUsl[i].Service.GUID = this.Items[i].UslGuid;
                    outUsl[i].ServicePaymentSize = this.Items[i].UslTarif;
                    outUsl[i].ServicePaymentSizeSpecified = true;                    
                }

                info.ServicePayment = outUsl;
                info.Item = true; //all contract objects
                request.Item = info;
                request.TransportGUID = Guid.NewGuid().ToString();

                try
                {
                    long t1 = Environment.TickCount;
                    AckRequest ack;

                    //Отправка запроса
                    var res = proxy.importCharterData(hdr, request, out ack);

                    long t2 = Environment.TickCount;
                    apires.in_xml = GisAPI.LastRequest;
                    apires.out_xml = GisAPI.LastResponce;
                    apires.query_duration = (t2 - t1) / 1000.0M;
                    apires.date_query = DateTime.Now;

                    StringBuilder sb = new StringBuilder(300);

                    if (res == null) { apires.text = ("service returned null"); return apires; }

                    var resAck = ack.Ack;
                    apires.messageGUID = resAck.MessageGUID;
                    this.MessageGuid = resAck.MessageGUID;
                    sb.AppendLine("RequesterMessageGUID: " + resAck.RequesterMessageGUID);
                    sb.AppendLine("MessageGUID: " + resAck.MessageGUID);

                    sb.AppendLine("Дата и время запроса: " + apires.date_query);
                    sb.AppendLine("Длительность обработки запроса: " + (apires.query_duration).ToString("F3") + " c.");
                    apires.text = sb.ToString();

                    return apires;
                }
                catch (Exception exc)
                {
                    ApiResultBase.InitExceptionResult(apires, "ImportCharterTarif", exc);
                    return apires;
                }
                finally
                {
                    try { proxy.Close(); }
                    catch (Exception) { }
                }

            }//end lock
        }

        public override ApiResultBase CheckState()
        {
            ApiResultBase ret = ExportContractApiRequest.ExportContract_Check(this.MessageGuid, this.OrgPpaGuid);
            return ret;
        }
    }
}
