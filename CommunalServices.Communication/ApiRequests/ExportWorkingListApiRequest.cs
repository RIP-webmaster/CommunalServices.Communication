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
    /// 
    /// </summary>
    public class ExportWorkingListApiRequest : ApiRequestBase
    {
        public ExportWorkingListApiRequest(string orgPPAGUID, int k_post, string houseGuid, DateTime periodFrom, DateTime periodTo)
        {
            this.OrgPpaGuid = orgPPAGUID;
            this.KPost = k_post;
            this.HouseGuid = houseGuid;
            this.PeriodFrom = periodFrom;
            this.PeriodTo = periodTo;
        }
                
        public string HouseGuid { get; set; }
        public DateTime PeriodFrom { get; set; }
        public DateTime PeriodTo { get; set; }

        public static ApiResult ExportWorkingList_Begin(string orgPPAGUID, string houseGuid, DateTime periodFrom, DateTime periodTo)
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
                hdr.Item = orgPPAGUID;

                hdr.IsOperatorSignature = true;
                hdr.IsOperatorSignatureSpecified = true;

                //period
                DateTime t_end = periodTo;
                DateTime t_start = periodFrom;

                var request = new exportWorkingListRequest();
                request.Id = "signed-data-container";

                //критерии
                List<object> itemsList = new List<object>(3);
                List<ItemsChoiceType4> choicesList = new List<ItemsChoiceType4>(3);

                itemsList.Add(new exportWorkingListRequestMonthYearFrom()
                {
                    Month = t_start.Month, Year = (short)t_start.Year
                });

                choicesList.Add(ItemsChoiceType4.MonthYearFrom);

                itemsList.Add(new exportWorkingListRequestMonthYearTo()
                {
                    Month = t_end.Month,
                    Year = (short)t_end.Year
                });

                choicesList.Add(ItemsChoiceType4.MonthYearTo);

                itemsList.Add(houseGuid);
                choicesList.Add(ItemsChoiceType4.FIASHouseGuid);

                request.Items = itemsList.ToArray();
                request.ItemsElementName = choicesList.ToArray();

                try
                {
                    long t1 = Environment.TickCount;
                    AckRequest ack;

                    //Отправка запроса
                    var rq = new exportWorkingListRequest1(hdr, request);
                    var res = proxy.exportWorkingList(hdr, request, out ack);

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

                    return apires;
                }
                catch (Exception exc)
                {
                    ApiResultBase.InitExceptionResult(apires, "ExportWorkingList", exc);
                    return apires;
                }
                finally
                {
                    try { proxy.Close(); }
                    catch (Exception) { }
                }

            }//end lock
        }

        public static ExportWorkingListApiResult ExportWorkingList_Check(string message_guid, string orgPPAGUID)
        {
            lock (GisAPI.csLock)
            {
                GisAPI.LastRequest = ""; GisAPI.LastResponce = "";
                var proxy = new ServicesPortsTypeAsyncClient("ServicesAsyncPort");

                var apires = new ExportWorkingListApiResult();
                List<WorkingListItem> wlItems = new List<WorkingListItem>();
                //формирование входных параметров запроса

                RequestHeader hdr = new RequestHeader();//заголовок запроса
                hdr.Date = DateTime.Now;
                hdr.MessageGUID = Guid.NewGuid().ToString();
                hdr.ItemElementName = ItemChoiceType.orgPPAGUID;
                hdr.Item = orgPPAGUID;

                hdr.IsOperatorSignature = true;
                hdr.IsOperatorSignatureSpecified = true;

                try
                {
                    StringBuilder sb = new StringBuilder(300);
                    var request = new getStateRequest1();
                    request.RequestHeader = hdr;

                    //тело запроса                        
                    getStateRequest body = new getStateRequest();
                    body.MessageGUID = message_guid;
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
                            if (item is exportWorkingListResultType)
                            {
                                sb.AppendLine("* exportWorkingListResultType *");
                                var exportRes = item as exportWorkingListResultType;
                                var rawItems = exportRes.WorkingList.WorkListItem;
                                if (rawItems == null) rawItems = new exportWorkingListResultTypeWorkingListWorkListItem[0];
                                sb.AppendLine("WorkListGUID: " + exportRes.WorkingList.WorkListGUID);
                                sb.AppendLine("WorkList items count: " + rawItems.Length.ToString());

                                for (int i = 0; i < rawItems.Length; i++)
                                {
                                    //элемент перечня услуг
                                    WorkingListItem wli = new WorkingListItem();
                                    wli.ContractGUID = (exportRes.WorkingList.ContractGUID);
                                    wli.FIASHouseGuid = (exportRes.WorkingList.FIASHouseGuid);
                                    wli.PeriodFrom = new DateTime(exportRes.WorkingList.MonthYearFrom.Year, exportRes.WorkingList.MonthYearFrom.Month, 1);
                                    wli.PeriodTo = new DateTime(exportRes.WorkingList.MonthYearTo.Year, exportRes.WorkingList.MonthYearTo.Month, 1);
                                    wli.Status = exportRes.WorkingList.WorkListStatus.ToString();
                                    wli.WorkListGUID = (exportRes.WorkingList.WorkListGUID);
                                    wli.WorkListItemGUID = (rawItems[i].WorkListItemGUID);
                                    wli.Count = (int)(wli.PeriodTo.Subtract(wli.PeriodFrom).TotalDays / 30.0);
                                    wlItems.Add(wli);
                                }
                            }
                            else if (item is CommonResultType)
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
                    apires.Items = wlItems.ToArray();

                    return apires;
                }
                catch (Exception exc)
                {
                    ApiResultBase.InitExceptionResult(apires, "ExportWorkingList_Check", exc);
                    return apires;
                }

            }//end lock
        }

        public override ApiResultBase Send()
        {
            ApiResultBase ret = ExportWorkingList_Begin(this.OrgPpaGuid, this.HouseGuid, this.PeriodFrom, this.PeriodTo);
            this.MessageGuid = ret.messageGUID;
            return ret;
        }

        public override ApiResultBase CheckState()
        {
            ApiResultBase ret = ExportWorkingList_Check(this.MessageGuid, this.OrgPpaGuid);
            return ret;
        }
    }
}

