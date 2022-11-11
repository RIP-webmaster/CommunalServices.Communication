/* Communal services system integration 
 * Copyright (c) 2022,  Svitkin V.G. 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Text;
using CommunalServices.Communication.API;
using CommunalServices.Communication.Data;
using GISGKHIntegration;
using GisgkhServices.Usl;

namespace CommunalServices.Communication.ApiRequests
{
    /// <summary>
    /// Представляет запрос к API на добавление перечня работ и услуг в ГИС ЖКХ
    /// </summary>
    public class ImportWorkingListApiRequest : ApiRequestBase
    {
        public ImportWorkingListApiRequest(string orgPPAGUID, int k_post, DateTime dtStart, DateTime dtEnd,
            string houseGUID, HouseUslData[] items )
        {
            this.OrgPpaGuid = orgPPAGUID;
            this.KPost = k_post;
            this.HouseGUID = houseGUID;
            this.Items = items;
            this.Start = dtStart;
            this.End = dtEnd;
        }

        public string HouseGUID { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public HouseUslData[] Items { get; set; }

        public override ApiResultBase Send()
        {
            lock (GisAPI.csLock)
            {
                GisAPI.LastRequest = ""; GisAPI.LastResponce = "";
                var proxy = new ServicesPortsTypeAsyncClient("ServicesAsyncPort");
                ApiResult apires = new ApiResult();

                //формирование входных параметров запроса

                RequestHeader hdr = new RequestHeader();//заголовок запроса
                hdr.Date = DateTime.Now;
                hdr.MessageGUID = Guid.NewGuid().ToString();
                hdr.ItemElementName = ItemChoiceType.orgPPAGUID;
                hdr.Item = this.OrgPpaGuid;

                hdr.IsOperatorSignature = true;
                hdr.IsOperatorSignatureSpecified = true;

                var request = new importWorkingListRequest();
                request.Id = "signed-data-container";

                int count;

                if (this.Start.Year == this.End.Year)
                {
                    count = this.End.Month - this.Start.Month + 1;
                }
                else
                {
                    count = 13 - this.Start.Month;

                    if (this.End.Year - this.Start.Year >= 2)
                    {
                        count += 12 * (this.End.Year - this.Start.Year - 1);
                    }

                    count += this.End.Month;
                }

                if (count < 0) count = 0;
                
                var data = new importWorkingListRequestApprovedWorkingListData();
                data.TransportGUID = Guid.NewGuid().ToString();
                data.MonthYearFrom = new WorkingListBaseTypeMonthYearFrom();
                data.MonthYearFrom.Year = (short)this.Start.Year;
                data.MonthYearFrom.Month = this.Start.Month;
                data.MonthYearTo = new WorkingListBaseTypeMonthYearTo();
                data.MonthYearTo.Year = (short)this.End.Year;
                data.MonthYearTo.Month = this.End.Month;
                data.FIASHouseGuid = this.HouseGUID;
                var wlitems = new importWorkingListRequestApprovedWorkingListDataWorkListItem[this.Items.Length];
                
                for (int i = 0; i < this.Items.Length; i++)
                {
                    var wlitem = new importWorkingListRequestApprovedWorkingListDataWorkListItem();
                    wlitem.TransportGUID = Guid.NewGuid().ToString();
                    wlitem.Index = (i + 1).ToString();
                    wlitem.Items = new object[3];
                    wlitem.ItemsElementName = new ItemsChoiceType3[3];

                    wlitem.Items[0] = this.Items[i].UslTarif;
                    wlitem.ItemsElementName[0] = ItemsChoiceType3.Price;
                    wlitem.Items[1] = this.Items[i].HouseArea;
                    wlitem.ItemsElementName[1] = ItemsChoiceType3.Amount;
                    wlitem.Items[2] = count.ToString();
                    wlitem.ItemsElementName[2] = ItemsChoiceType3.Count;
                    
                    wlitem.WorkItemNSI = new nsiRef();
                    wlitem.WorkItemNSI.GUID = this.Items[i].UslGuid;
                    wlitem.WorkItemNSI.Code = this.Items[i].UslCode;
                    wlitems[i] = wlitem;
                }

                data.WorkListItem = wlitems;
                request.Item = data;

                try
                {
                    long t1 = Environment.TickCount;
                    AckRequest ack;

                    //Отправка запроса
                    var rq = new importWorkingListRequest1(hdr, request);
                    var res = proxy.importWorkingList(hdr, request, out ack);

                    long t2 = Environment.TickCount;
                    apires.in_xml = GisAPI.LastRequest;
                    apires.out_xml = GisAPI.LastResponce;
                    apires.query_duration = (t2 - t1) / 1000.0M;
                    apires.date_query = DateTime.Now;

                    StringBuilder sb = new StringBuilder(300);

                    if (res == null) { apires.text = ("service returned null"); return apires; }

                    var resAck = ack.Ack;
                    apires.messageGUID = resAck.MessageGUID;
                    sb.AppendLine("RequesterMessageGUID: " + resAck.RequesterMessageGUID);
                    sb.AppendLine("MessageGUID: " + resAck.MessageGUID);
                    sb.AppendLine("TransportGUID: " + apires.transportGUID);

                    sb.AppendLine("Дата и время запроса: " + apires.date_query);
                    sb.AppendLine("Длительность обработки запроса: " + (apires.query_duration).ToString("F3") + " c.");
                    apires.text = sb.ToString();
                    this.MessageGuid = apires.messageGUID;

                    return apires;
                }
                catch (Exception exc)
                {
                    ApiResultBase.InitExceptionResult(apires, "ImportWorkingList", exc);
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
            lock (GisAPI.csLock)
            {
                GisAPI.LastRequest = ""; GisAPI.LastResponce = "";
                var proxy = new ServicesPortsTypeAsyncClient("ServicesAsyncPort");
                var apires = new ApiResult();

                //формирование входных параметров запроса
                RequestHeader hdr = new RequestHeader();//заголовок запроса
                hdr.Date = DateTime.Now;
                hdr.MessageGUID = Guid.NewGuid().ToString();
                hdr.ItemElementName = ItemChoiceType.orgPPAGUID;
                hdr.Item = this.OrgPpaGuid;

                hdr.IsOperatorSignature = true;
                hdr.IsOperatorSignatureSpecified = true;

                try
                {
                    StringBuilder sb = new StringBuilder(300);
                    var request = new getStateRequest1();
                    request.RequestHeader = hdr;

                    //тело запроса                        
                    getStateRequest body = new getStateRequest();
                    body.MessageGUID = this.MessageGuid;
                    request.getStateRequest = body;

                    long t1 = Environment.TickCount;
                    GisAPI.DisableSignature = true;//запрос не подписывается

                    /* Отправка запроса*/
                    getStateResult result;
                    var res = proxy.getState(request.RequestHeader, body, out result);

                    GisAPI.DisableSignature = false;
                    long t2 = Environment.TickCount;
                    apires.query_duration = (t2 - t1) / 1000.0M;
                    apires.date_query = DateTime.Now;
                    apires.in_xml = GisAPI.LastRequest;
                    apires.out_xml = GisAPI.LastResponce;

                    if (res == null) { apires.text = ("service returned null"); return apires; }

                    //Обработка результатов запроса

                    sb.AppendLine("RequestState: " + result.RequestState.ToString());//статус обработки запроса
                    apires.RequestState = (int)result.RequestState;

                    if (result != null && result.Items != null)
                    {
                        ImportWorkingPlanApiRequest.ProcessResult(result, sb, apires);
                    }                        

                    if (result != null && result.Items != null)
                    {
                        sb.AppendLine("Items: " + result.Items.Length);
                    }

                    sb.AppendLine("Дата и время запроса: " + apires.date_query);
                    sb.AppendLine("Длительность обработки запроса: " + (apires.query_duration).ToString("F3") + " c.");
                    apires.text = sb.ToString();

                    return apires;
                }
                catch (Exception exc)
                {
                    GisAPI.DisableSignature = false;
                    ApiResultBase.InitExceptionResult(apires, "ImportWorkingList_Check", exc);
                    return apires;
                }

            }//end lock
        }
    }
}
