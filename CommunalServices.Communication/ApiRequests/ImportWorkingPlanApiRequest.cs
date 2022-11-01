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
    public class ImportWorkingPlanApiRequest : ApiRequestBase
    {
        public ImportWorkingPlanApiRequest(string orgPPAGUID, int k_post, string worklistGuid, int year, 
            WorkingListItem[] items, int monthStart, int monthEnd)
        {
            this.OrgPpaGuid = orgPPAGUID;
            this.KPost = k_post;
            this.WorkingListGuid = worklistGuid;
            this.Year = year;
            this.Items = items;
            this.MonthStart = monthStart;
            this.MonthEnd = monthEnd;
        }

        public string WorkingListGuid { get; set; }
        public int Year { get; set; }
        public WorkingListItem[] Items { get; set; }
        public int MonthStart { get; set; }
        public int MonthEnd { get; set; }

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

                var request = new importWorkingPlanRequest();
                request.Id = "signed-data-container";
                WorkingPlanType wpt = new WorkingPlanType();
                wpt.TransportGUID = Guid.NewGuid().ToString();
                wpt.WorkListGUID = this.WorkingListGuid;
                wpt.Year = (short)this.Year;
                WorkingPlanTypeWorkPlanItem plan_item;
                List<WorkingPlanTypeWorkPlanItem> plan_items = new List<WorkingPlanTypeWorkPlanItem>();

                for (int i = 0; i < this.Items.Length; i++)
                {
                    for (int j = this.MonthStart; j <= this.MonthEnd; j++)
                    {
                        plan_item = new WorkingPlanTypeWorkPlanItem();
                        plan_item.Items = new object[] { "1" };
                        plan_item.Year = (short)this.Year;
                        plan_item.Month = j;
                        plan_item.TransportGUID = Guid.NewGuid().ToString();
                        plan_item.WorkListItemGUID = this.Items[i].WorkListItemGUID;
                        plan_items.Add(plan_item);
                    }
                }

                wpt.WorkPlanItem = plan_items.ToArray();
                request.WorkingPlan = new WorkingPlanType[]{wpt};
                
                try
                {
                    long t1 = Environment.TickCount;
                    AckRequest ack;

                    //Отправка запроса
                    var rq = new importWorkingPlanRequest1(hdr, request);
                    var res = proxy.importWorkingPlan(hdr, request, out ack);

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
                    ApiResultBase.InitExceptionResult(apires, "ImportWorkingPlan", exc);
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
                        foreach (object item in result.Items)
                        {
                            if (item is CommonResultType)
                            {
                                var crt = (CommonResultType)item;
                                sb.AppendLine("* CommonResultType *");
                                sb.AppendLine("GUID: " + crt.GUID);
                                sb.AppendLine("TransportGUID: " + crt.TransportGUID);

                                if (crt.Items != null)
                                {
                                    foreach (var innerItem in crt.Items)
                                    {
                                        sb.AppendLine("-" + item.GetType().ToString());

                                        if (item is CommonResultType)
                                        {
                                            CommonResultType inner = (CommonResultType)item;

                                            if (inner.Items != null)
                                            {
                                                foreach (var x in inner.Items)
                                                {
                                                    sb.AppendLine("--" + x.GetType().ToString());
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            else if (item is ErrorMessageType)
                            {
                                sb.AppendLine("* Error *");
                                sb.AppendLine("ErrorCode: " + (item as ErrorMessageType).ErrorCode);
                                sb.AppendLine("ErrorMes: " + (item as ErrorMessageType).Description);
                                sb.AppendLine("StackTrace: ");
                                sb.AppendLine((item as ErrorMessageType).StackTrace);

                                //INT002012 - Нет объектов для экспорта
                                if ((item as ErrorMessageType).ErrorCode != "INT002012")
                                {
                                    apires.error = true;
                                    apires.ErrorCode = (item as ErrorMessageType).ErrorCode;
                                    apires.ErrorMessage = (item as ErrorMessageType).Description;
                                    apires.StackTrace = (item as ErrorMessageType).StackTrace;
                                }
                            }
                            else sb.AppendLine(item.GetType().ToString());
                        }//end foreach

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
                    ApiResultBase.InitExceptionResult(apires, "ImportWorkingPlan_Check", exc);
                    return apires;
                }

            }//end lock
        }
    }
}
