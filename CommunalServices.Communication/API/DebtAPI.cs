/* Communal services system integration 
 * Copyright (c) 2021,  Svitkin V.G. 
 * License: BSD 2.0 */
//GISGKH_Integration: клиент API для получения запросов о задолженности из ГИС ЖКХ

using System;
using System.Collections.Generic;
using System.Text;
using GisgkhServices.Debt;

namespace GISGKHIntegration
{
    public static class DebtAPI
    {
        public static ApiResult ExportDebtRequests_Begin(string orgPPAGUID)
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
                request.Items = new object[] { new Period(){
                        startDate = t_start,
                        endDate = t_end
                    }};
                request.ItemsElementName = new ItemsChoiceType3[] { ItemsChoiceType3.periodOfSendingRequest };
                
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

        public static ApiResult ExportDebtRequests_Check(string message_guid, string orgPPAGUID)
        {
            lock (GisAPI.csLock)
            {
                GisAPI.LastRequest = ""; GisAPI.LastResponce = "";
                var proxy = new DebtRequestsAsyncPortClient("DebtRequestsAsyncPort");
                {
                    var apires = new ApiResult();
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
                                    }

                                    if (dsr.subrequestData != null)
                                    {
                                        for (int i = 0; i < dsr.subrequestData.Length; i++)
                                        {
                                            sb.AppendLine("subrequestData #"+i.ToString());
                                            sb.AppendLine(dsr.subrequestData[i].subrequestGUID);
                                            sb.AppendLine("Organisation: " + dsr.subrequestData[i].requestInfo.organization.name);
                                            sb.AppendLine("Request number: " + dsr.subrequestData[i].requestInfo.requestNumber);
                                            sb.AppendLine("Sent: " + dsr.subrequestData[i].requestInfo.sentDate.ToString());
                                            sb.AppendLine("Status: " + dsr.subrequestData[i].requestInfo.status.ToString());

                                            if (dsr.subrequestData[i].requestInfo.executorInfo != null)
                                            {
                                                sb.AppendLine("Executor: " + dsr.subrequestData[i].requestInfo.executorInfo.fio);
                                            }

                                            var hfo = dsr.subrequestData[i].requestInfo.housingFundObject;

                                            if (hfo == null) 
                                                hfo = new ExportHousingFundObjectInfoType() { address = "", addressDetails = "" };

                                            sb.AppendLine("Address: " + hfo.address);
                                            sb.AppendLine("Address details: " + hfo.addressDetails);
                                            sb.AppendLine();
                                        }//end for
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


                        sb.AppendLine("Дата и время запроса: " + apires.date_query);
                        sb.AppendLine("Длительность обработки запроса: " + (apires.query_duration).ToString("F3") + " c.");
                        apires.text = sb.ToString();

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
    }
}
