/* Communal services system integration 
 * Copyright (c) 2021,  Svitkin V.G. 
 * License: BSD 2.0 */
//GISGKH_Integration: клиент API для получения запросов о задолженности из ГИС ЖКХ
using System;
using System.Collections.Generic;
using System.Text;
using CommunalServices.Communication.API;
using CommunalServices.Communication.Data;
using GisgkhServices.Debt;

namespace GISGKHIntegration
{
    public static class DebtAPI
    {
        public static ApiResult ExportDebtRequests_Begin(string orgPPAGUID, string pageGuid)
        {
            lock (GisAPI.csLock)
            {
                GisAPI.LastRequest = ""; GisAPI.LastResponce = "";
                var proxy = new DebtRequestsAsyncPortClient("DebtRequestsAsyncPort");
                ApiResult apires = new ApiResult();

                //формирование входных параметров запроса

                RequestHeader hdr = new RequestHeader();//заголовок запроса

                hdr.Date = DateTime.Now;
                hdr.MessageGUID = Guid.NewGuid().ToString();
                hdr.ItemElementName = ItemChoiceType.orgPPAGUID;
                hdr.Item = orgPPAGUID;

                hdr.IsOperatorSignature = true;
                hdr.IsOperatorSignatureSpecified = true;

                //period ~= 5 month
                DateTime t_end = DateTime.Now;
                DateTime t_start = DateTime.Now.Subtract(TimeSpan.FromDays(5*30));

                var request = new exportDSRsRequest();
                request.Id = "signed-data-container";

                if (!string.IsNullOrEmpty(pageGuid))
                {
                    request.Items = new object[] { new Period(){
                        startDate = t_start,
                        endDate = t_end
                    },ResponseStatusType.NotSent,pageGuid};

                    request.ItemsElementName = new ItemsChoiceType3[] { 
                     ItemsChoiceType3.periodOfSendingRequest,
                     ItemsChoiceType3.responseStatus,
                     ItemsChoiceType3.exportSubrequestGUID
                    };
                }
                else
                {
                    request.Items = new object[] { new Period(){
                        startDate = t_start,
                        endDate = t_end
                    },ResponseStatusType.NotSent};

                    request.ItemsElementName = new ItemsChoiceType3[] { 
                     ItemsChoiceType3.periodOfSendingRequest,
                     ItemsChoiceType3.responseStatus
                    };
                }
                
                try
                {
                    long t1 = Environment.TickCount;
                    AckRequest ack;

                    //Отправка запроса
                    var rq = new exportDebtSubrequestsRequest(hdr, request);
                    var res = proxy.exportDebtSubrequests(hdr, request, out ack);

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
                    ApiResultBase.InitExceptionResult(apires, "ExportDebtRequests", exc);
                    return apires;
                }
                finally
                {
                    try { proxy.Close(); }
                    catch (Exception) { }
                }

            }//end lock
        }

        public static ExportDebtApiResult ExportDebtRequests_Check(string message_guid, string orgPPAGUID)
        {
            lock (GisAPI.csLock)
            {
                GisAPI.LastRequest = ""; GisAPI.LastResponce = "";
                var proxy = new DebtRequestsAsyncPortClient("DebtRequestsAsyncPort");
                {
                    var apires = new ExportDebtApiResult();
                    List<DebtRequest> requests = new List<DebtRequest>();
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
                        var res = proxy.getState(request.RequestHeader,body,out result);

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
                                if (item is exportDSRsResultType)
                                {
                                    var dsr = (exportDSRsResultType)item;
                                    sb.AppendLine("* exportDSRsResultType *");

                                    if (dsr.notFound != null)
                                    {
                                        if(dsr.notFound.Items!=null)
                                        for (int i = 0; i < dsr.notFound.Items.Length; i++)
                                        {
                                            sb.AppendLine("not found: " + dsr.notFound.Items[i]);

                                            if (dsr.notFound.ItemsElementName != null &&
                                                i < dsr.notFound.ItemsElementName.Length)
                                            {
                                                sb.AppendLine(dsr.notFound.ItemsElementName[i].ToString());
                                            }
                                        }//end for
                                    }

                                    if (dsr.pagedOutput != null)
                                    {
                                         sb.AppendLine("pagedOutput");

                                         if (dsr.pagedOutput.Item is string)
                                         {
                                             sb.AppendLine("next: " + (string)dsr.pagedOutput.Item);
                                             apires.NextPageGuid = (string)dsr.pagedOutput.Item;
                                         }
                                    }

                                    if (dsr.subrequestData != null)
                                    {
                                        for (int i = 0; i < dsr.subrequestData.Length; i++)
                                        {
                                            DebtRequest debtRequest = new DebtRequest();
                                            sb.AppendLine("subrequestData #"+i.ToString());
                                            sb.AppendLine(dsr.subrequestData[i].subrequestGUID);
                                            debtRequest.SubrequestGUID = dsr.subrequestData[i].subrequestGUID;
                                            sb.AppendLine("Request number: " + dsr.subrequestData[i].requestInfo.requestNumber);
                                            debtRequest.Number = dsr.subrequestData[i].requestInfo.requestNumber;
                                            
                                            var hfo = dsr.subrequestData[i].requestInfo.housingFundObject;

                                            if (hfo == null) 
                                                hfo = new ExportHousingFundObjectInfoType() { address = "", addressDetails = "" };

                                            sb.AppendLine("Address: " + hfo.address +" "+hfo.addressDetails);
                                            debtRequest.HouseAddress = hfo.address;
                                            debtRequest.HouseGUID = hfo.fiasHouseGUID;
                                            debtRequest.HouseNkv = hfo.addressDetails;

                                            if (dsr.subrequestData[i].responseData != null &&
                                                dsr.subrequestData[i].responseData.executorInfo != null)
                                            {
                                                //responded
                                                sb.AppendLine("Responder FIO: " + dsr.subrequestData[i].responseData.executorInfo.fio);

                                                sb.AppendLine("Responder GUID: " +
                                                    dsr.subrequestData[i].responseData.executorInfo.GUID.ToString());
                                            }
                                            else
                                            {
                                                requests.Add(debtRequest);
                                            }

                                            sb.AppendLine();
                                        }//end for
                                    }
                                }
                                else if (item is CommonResultType)
                                {
                                    var crt = (CommonResultType)item;
                                    sb.AppendLine("* CommonResultType *");
                                    sb.AppendLine("GUID: "+crt.GUID);
                                    sb.AppendLine("TransportGUID: " + crt.TransportGUID);

                                    if (crt.Items != null)
                                    {
                                        foreach (var innerItem in crt.Items)
                                        {
                                            sb.AppendLine("-"+item.GetType().ToString());
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
                        apires.Requests = requests.ToArray();

                        return apires;
                    }
                    catch (Exception exc)
                    {
                        ApiResultBase.InitExceptionResult(apires, "ExportDebtRequests_Check", exc);
                        return apires;
                    }
                }
            }//end lock
        }

        public static ApiResult ImportDebtResponces_Begin(string orgPPAGUID, DebtRequest[] requests,string executorGuid)
        {
            lock (GisAPI.csLock)
            {
                GisAPI.LastRequest = ""; GisAPI.LastResponce = "";
                var proxy = new DebtRequestsAsyncPortClient("DebtRequestsAsyncPort");
                ApiResult apires = new ApiResult();

                //формирование входных параметров запроса

                RequestHeader hdr = new RequestHeader();//заголовок запроса

                hdr.Date = DateTime.Now;
                hdr.MessageGUID = Guid.NewGuid().ToString();
                hdr.ItemElementName = ItemChoiceType.orgPPAGUID;
                hdr.Item = orgPPAGUID;

                hdr.IsOperatorSignature = true;
                hdr.IsOperatorSignatureSpecified = true;

                //period ~= 5 month
                DateTime t_end = DateTime.Now;
                DateTime t_start = DateTime.Now.Subtract(TimeSpan.FromDays(5 * 30));

                var request = new importDSRResponsesRequest();
                request.Id = "signed-data-container";

                var actions = new List<importDSRResponsesRequestAction>();
                for (int i = 0; i < requests.Length; i++)
                {
                    importDSRResponsesRequestAction act = new importDSRResponsesRequestAction();
                    act.subrequestGUID = requests[i].SubrequestGUID;
                    act.TransportGUID = Guid.NewGuid().ToString();
                    act.actionType = DSRResponseActionType.Send;
                    act.responseData = new ImportDSRResponseType();
                    act.responseData.hasDebt = false;
                    act.responseData.executorGUID = executorGuid;
                    actions.Add(act);
                }

                request.action = actions.ToArray();

                try
                {
                    long t1 = Environment.TickCount;
                    AckRequest ack;

                    //Отправка запроса                    
                    var res = proxy.importResponses(hdr, request, out ack);

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

                    sb.AppendLine("Дата и время запроса: " + apires.date_query);
                    sb.AppendLine("Длительность обработки запроса: " + (apires.query_duration).ToString("F3") + " c.");
                    apires.text = sb.ToString();

                    return apires;
                }
                catch (Exception exc)
                {
                    ApiResultBase.InitExceptionResult(apires, "ImportDebtResponces", exc);
                    return apires;
                }
                finally
                {
                    try { proxy.Close(); }
                    catch (Exception) { }
                }

            }//end lock
        }
    }

    /// <summary>
    /// Представляет запрос к API ГИСЖКХ на экспорт запросов о задолженности, подтвержденной судебным актом
    /// </summary>
    public class ExportDebtApiRequest : ApiRequestBase
    {
        /// <summary>
        /// Создает новый запрос на экспорт запросов о задолженности
        /// </summary>
        /// <param name="orgPPAGUID">ИД поставщика информации</param>
        /// <param name="pageGuid">
        /// ИД следующей страницы для запроса с постраничным выводом или пустая строка для получения первой страницы
        /// </param>
        /// <remarks>
        /// Постраничный вывод используется, когда число результатов превышает 100 (в ответе от ГИС ЖКХ заполнен 
        /// элемент PagedOutput). В первоначальном запросе значение pageGuid должно быть пустым. Если запрос вернул более 
        /// одной страницы, в ответе на него будет заполнено свойство <see cref="ExportDebtApiResult.NextPageGuid"/>. 
        /// Это значение нужно передать как pageGuid в следующий запрос. 
        /// В последнем ответе значение <see cref="ExportDebtApiResult.NextPageGuid"/> будет пустым.
        /// </remarks>
        public ExportDebtApiRequest(string orgPPAGUID, string pageGuid)
        {
            this.OrgPpaGuid = orgPPAGUID;
            this.PageGuid = pageGuid;
        }

        /// <summary>
        /// ИД следующей страницы для запроса с постраничным выводом или пустая строка, если запрошена первая страница
        /// </summary>
        public string PageGuid { get; set; }

        public override ApiResultBase Send()
        {
            ApiResultBase ret = DebtAPI.ExportDebtRequests_Begin(this.OrgPpaGuid, this.PageGuid);
            this.MessageGuid = ret.messageGUID;
            return ret;
        }

        public override ApiResultBase CheckState()
        {
            ApiResultBase ret = DebtAPI.ExportDebtRequests_Check(this.MessageGuid, this.OrgPpaGuid);
            return ret;
        }
    }
}
