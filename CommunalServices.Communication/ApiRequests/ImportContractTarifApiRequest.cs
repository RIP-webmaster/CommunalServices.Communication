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
    /// Размещение информации о размере платы за жилое помещение (тарифах на жил. услуги) в договоре управления
    /// </summary>
    public class ImportContractTarifApiRequest : ApiRequestBase
    {
        public ImportContractTarifApiRequest(string orgPPAGUID, int k_post, string houseGuid, string contractVersionGuid, 
            DateTime dtStart, DateTime dtEnd, HouseUslData[] items, bool voting)
        {
            this.OrgPpaGuid = orgPPAGUID;
            this.KPost = k_post;
            this.HouseGUID = houseGuid;
            this.ContractVersionGUID = contractVersionGuid;
            this.Start = dtStart;
            this.End = dtEnd;
            this.Items = items;
            this.IsByVoting = voting;
        }

        public string HouseGUID { get; set; }
        public string ContractVersionGUID { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public HouseUslData[] Items { get; set; }
        public bool IsByVoting { get; set; }

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

                var request = new importContractRequest();
                request.Id = "signed-data-container";

                var contract = new importContractRequestContract();
                var info = new importContractRequestContractPlaceContractPaymentsInfo();
                info.BeginDate = this.Start;
                info.EndDate = this.End;
                info.ContractVersionGUID = this.ContractVersionGUID;
                info.Items = new object[0];

                if (this.IsByVoting) info.Type = ContractPaymentsInfoTypeType.P;
                else info.Type = ContractPaymentsInfoTypeType.C;

                decimal tarifSod = 0.0M;
                decimal tarifUpr = 0.0M;
                var outUsl = new ContractPaymentsInfoTypeServicePayment[this.Items.Length];

                for (int i = 0; i < this.Items.Length; i++)
                {
                    if (string.Equals(this.Items[i].UslName, "Содержание и текущий ремонт", StringComparison.Ordinal))
                    {
                        tarifSod = this.Items[i].UslTarif;
                    }
                    else if (string.Equals(this.Items[i].UslName, "Услуги управления", StringComparison.Ordinal))
                    {
                        tarifUpr = this.Items[i].UslTarif;
                    }

                    outUsl[i] = new ContractPaymentsInfoTypeServicePayment();
                    outUsl[i].Service = new nsiRef();
                    outUsl[i].Service.Code = this.Items[i].UslCode;
                    outUsl[i].Service.GUID = this.Items[i].UslGuid;
                    outUsl[i].ServicePaymentSize = this.Items[i].UslTarif;
                    outUsl[i].ServicePaymentSizeSpecified = true;
                    info.ContractObjectVersionGUID = this.Items[i].ContractObjectVersionGUID;
                }

                info.ServicePayment = outUsl;

                if (tarifUpr > 0.0M && this.IsByVoting)
                {
                    info.HouseManagementPaymentSize = tarifUpr; //цена за услуги управления УК по протоколу собрания
                }
                else if (!this.IsByVoting)
                {
                    info.HouseManagementPaymentSize = tarifSod; //цена за содержание по протоколу конкурса
                }
                else
                {
                    info.HouseManagementPaymentSize = 0.2M * tarifSod; //цена за услуги управления УК - условно 20% от содержания
                }

                contract.Item = info;
                contract.TransportGUID = Guid.NewGuid().ToString();
                request.Contract = new importContractRequestContract[]{contract};
                
                try
                {
                    long t1 = Environment.TickCount;
                    AckRequest ack;

                    //Отправка запроса
                    var res = proxy.importContractData(hdr, request, out ack);

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
                    ApiResultBase.InitExceptionResult(apires, "ImportContractTarif", exc);
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
