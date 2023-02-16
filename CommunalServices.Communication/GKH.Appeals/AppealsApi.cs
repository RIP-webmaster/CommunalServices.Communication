/* Communal services system integration 
 * Copyright (c) 2021,  Svitkin V.G. 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using CommunalServices.Communication.Data;
using GISGKHIntegration;

namespace GKH.Appeals
{
    public class AppealsApi
    {
        public static ApiResult GetNewAppeals_Begin(string orgPPAGUID)
        {
            lock (GisAPI.csLock)
            {
                AppealsAsyncPortClient proxy = new AppealsAsyncPortClient("AppealsAsyncPort");
                GisAPI.LastRequest = ""; GisAPI.LastResponce = "";

                ApiResult apires = new ApiResult();

                //формирование входных параметров запроса

                RequestHeader hdr = new RequestHeader();//заголовок запроса

                hdr.Date = DateTime.Now;
                hdr.MessageGUID = Guid.NewGuid().ToString();
                hdr.ItemElementName = ItemChoiceType.orgPPAGUID;
                hdr.Item = orgPPAGUID;

                hdr.IsOperatorSignature = true;
                hdr.IsOperatorSignatureSpecified = true;

                exportAppealRequest request = new exportAppealRequest();
                request.Id = "signed-data-container";

                request.Items = new object[] { 
                    new DateTime(DateTime.Now.Year, 1, 1),
                    new exportAppealRequestStatusOfAppeal(){Sent=true,SentSpecified=true, }
                };

                request.ItemsElementName = new ItemsChoiceType5[] { ItemsChoiceType5.StartDate, ItemsChoiceType5.StatusOfAppeal };

                try
                {
                    AckRequest ack;
                    long t1 = Environment.TickCount;

                    //Отправка запроса
                    exportAppealRequest1 req = new exportAppealRequest1(hdr, request);
                    ResultHeader res = proxy.exportAppeal(hdr, request, out ack);

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
                    ApiResultBase.InitExceptionResult(apires, "GetNewAppeals", exc);
                    return apires;
                }
            }//end lock
        }

        public static GISGKHIntegration.API.GetAppealsRes GetNewAppeals_Check(string message_guid, string orgPPAGUID,int k_post)
        {
            lock (GisAPI.csLock)
            {
                GisAPI.LastRequest = ""; GisAPI.LastResponce = "";
                AppealsAsyncPortClient proxy = new AppealsAsyncPortClient("AppealsAsyncPort");
                {
                    var apires = new GISGKHIntegration.API.GetAppealsRes();
                    //формирование входных параметров запроса

                    RequestHeader hdr = new RequestHeader();//заголовок запроса

                    hdr.Date = DateTime.Now;
                    hdr.MessageGUID = Guid.NewGuid().ToString();
                    hdr.ItemElementName = ItemChoiceType.orgPPAGUID;
                    hdr.Item = orgPPAGUID;

                    hdr.IsOperatorSignature = true;
                    hdr.IsOperatorSignatureSpecified = true;
                    GISGKHIntegration.Data.Appeal entry;

                    try
                    {
                        StringBuilder sb = new StringBuilder(300);
                        GKH.Appeals.getStateRequest1 request = new GKH.Appeals.getStateRequest1();
                        request.RequestHeader = hdr;

                        //тело запроса                        
                        getStateRequest body = new getStateRequest();
                        body.MessageGUID = message_guid;
                        request.getStateRequest = body;
                        
                        getStateResult gsr; //переменная для результата
                        long t1 = Environment.TickCount;
                        GisAPI.DisableSignature = true;//запрос не подписывается

                        /* Отправка запроса*/
                        var res = proxy.getState(hdr, body, out gsr);

                        GisAPI.DisableSignature = false;
                        long t2 = Environment.TickCount;
                        apires.query_duration = (t2 - t1) / 1000.0M;
                        apires.date_query = DateTime.Now;
                        apires.in_xml = GisAPI.LastRequest;
                        apires.out_xml = GisAPI.LastResponce;

                        if (res == null) { apires.text = ("service returned null"); return apires; }

                        //Обработка результатов запроса

                        sb.AppendLine("RequestState: " + gsr.RequestState.ToString());//статус обработки запроса
                        apires.RequestState = (int)gsr.RequestState;

                        if (gsr.Items != null)
                        {
                            sb.AppendLine();
                            for (int i = 0; i < gsr.Items.Length; i++)
                            {
                                var item = gsr.Items[i];

                                if (item is GKH.Appeals.ErrorMessageType)
                                {
                                    sb.AppendLine("* Error *");
                                    sb.AppendLine("ErrorCode: " + (item as GKH.Appeals.ErrorMessageType).ErrorCode);
                                    sb.AppendLine("ErrorMes: " + (item as GKH.Appeals.ErrorMessageType).Description);
                                    sb.AppendLine("StackTrace: ");
                                    sb.AppendLine((item as GKH.Appeals.ErrorMessageType).StackTrace);

                                    apires.error = true;
                                    apires.ErrorCode = (item as GKH.Appeals.ErrorMessageType).ErrorCode;
                                    apires.ErrorMessage = (item as GKH.Appeals.ErrorMessageType).Description;
                                    apires.StackTrace = (item as GKH.Appeals.ErrorMessageType).StackTrace;
                                    break;
                                }
                                else if (item is GKH.Appeals.exportAppealResultType)
                                {
                                    sb.AppendLine("* Обращение *");
                                    GKH.Appeals.exportAppealResultType appeal = item as GKH.Appeals.exportAppealResultType;
                                    sb.AppendLine("#"+appeal.AppealNumber);
                                    sb.AppendLine(appeal.AppealCreateDate.ToString());

                                    entry = new GISGKHIntegration.Data.Appeal();
                                    entry.Number = appeal.AppealNumber;
                                    entry.DateCreated = appeal.AppealCreateDate;
                                    entry.Text = appeal.AppealText;

                                    string topic = "(unknown topic)";
                                    if (appeal.Item != null)
                                    {
                                        if (appeal.Item is string)
                                        {
                                            string s = (string)appeal.Item;
                                            sb.AppendLine(s);
                                            if (s.Trim().Length > 0) topic = s;
                                        }
                                        else if (appeal.Item is nsiRef)
                                        {
                                            nsiRef nr = (nsiRef)appeal.Item;
                                            sb.AppendLine(nr.Name);
                                            topic = nr.Name;
                                        }
                                        else sb.AppendLine(appeal.Item.GetType().ToString());
                                    }

                                    entry.Topic = topic;
                                    string addr_house = "";
                                    string addr_kv = "";
                                    
                                    for (int j = 0; j < appeal.ApplicantInfo.Items.Length; j++)
                                    {
                                        switch (appeal.ApplicantInfo.ItemsElementName[j])
                                        {
                                            case ItemsChoiceType3.Person:
                                                sb.AppendLine("ФИО:");
                                                var person = (appeal.ApplicantInfo.Items[j] as GKH.Appeals.ApplicantTypePerson);
                                                if (person != null)
                                                {
                                                    entry.FIO = person.Surname + " " + person.FirstName + " " + person.Patronymic;
                                                    sb.AppendLine(entry.FIO);
                                                }
                                                break;
                                            case ItemsChoiceType3.PostAddress:
                                                sb.AppendLine("Адрес:");
                                                if (appeal.ApplicantInfo.Items[j] is string)
                                                {
                                                    addr_house = appeal.ApplicantInfo.Items[j].ToString();
                                                    sb.AppendLine(addr_house);
                                                }
                                                else sb.AppendLine(appeal.ApplicantInfo.Items[j].GetType().ToString());
                                                break;
                                            case ItemsChoiceType3.ApartmentNumber:
                                                sb.AppendLine("Квартира:");
                                                if (appeal.ApplicantInfo.Items[j] is string)
                                                {
                                                    addr_kv = appeal.ApplicantInfo.Items[j].ToString();
                                                    sb.AppendLine(addr_kv);
                                                }
                                                else sb.AppendLine(appeal.ApplicantInfo.Items[j].GetType().ToString());
                                                break;
                                            case ItemsChoiceType3.Email:
                                                sb.AppendLine("E-Mail:");
                                                if (appeal.ApplicantInfo.Items[j] is string)
                                                {
                                                    entry.EMail = appeal.ApplicantInfo.Items[j].ToString();
                                                    sb.AppendLine(entry.EMail);
                                                }
                                                else sb.AppendLine(appeal.ApplicantInfo.Items[j].GetType().ToString());
                                                break;
                                            case ItemsChoiceType3.PhoneNumber:
                                                sb.AppendLine("Телефон: ");
                                                if (appeal.ApplicantInfo.Items[j] is string)
                                                {
                                                    entry.Phone = appeal.ApplicantInfo.Items[j].ToString();
                                                    sb.AppendLine(entry.Phone);
                                                }
                                                else sb.AppendLine(appeal.ApplicantInfo.Items[j].GetType().ToString());
                                                break;
                                            default:
                                                sb.AppendLine(appeal.ApplicantInfo.ItemsElementName[j].ToString()+": ");

                                                if (appeal.ApplicantInfo.Items[j] is string)
                                                    sb.AppendLine(appeal.ApplicantInfo.Items[j].ToString());
                                                else
                                                    sb.AppendLine(appeal.ApplicantInfo.Items[j].GetType().ToString());
                                                break;
                                        }
                                    }//end for

                                    if (addr_house.Length > 0)
                                    {
                                        entry.Addr = addr_house;

                                        if (addr_kv.Length > 0) entry.Addr += ", кв. "+addr_kv;
                                    }

                                    entry.HouseGUID = appeal.FIASHouseGuid;
                                    entry.NKV = addr_kv;

                                    sb.AppendLine("Текст обращения:");
                                    sb.AppendLine(appeal.AppealText);
                                                                        
                                    if (appeal.Attachment != null)
                                    {
                                        sb.AppendLine("Файлы:");
                                        string files_string = "";
                                        files_string += "Файлов: "+appeal.Attachment.Length.ToString()+". ";

                                        for (int j = 0; j < appeal.Attachment.Length; j++)
                                        {
                                            sb.AppendLine((j+1).ToString()+". "+appeal.Attachment[j].Name);
                                            files_string += appeal.Attachment[j].Name + "; ";

                                            if (appeal.Attachment[j].Description != null)
                                            {
                                                sb.AppendLine("Description: " + appeal.Attachment[j].Description);
                                            }

                                            sb.AppendLine("Hash: " + appeal.Attachment[j].AttachmentHASH);
                                            sb.AppendLine("GUID: " + appeal.Attachment[j].Attachment.AttachmentGUID);
                                            sb.AppendLine();
                                        }

                                        if (appeal.Attachment.Length > 0) entry.FilesText = files_string;
                                    }

                                    entry.OrgCode = k_post;
                                    apires.Entries.Add(entry);
                                }
                                else sb.AppendLine(item.GetType().ToString());

                                sb.AppendLine();
                            }
                        }
                        else sb.AppendLine("(no items)");
                        sb.AppendLine();

                        sb.AppendLine("Дата и время запроса: " + apires.date_query);
                        sb.AppendLine("Длительность обработки запроса: " + (apires.query_duration).ToString("F3") + " c.");
                        apires.text = sb.ToString();

                        return apires;
                    }
                    catch (Exception exc)
                    {
                        ApiResultBase.InitExceptionResult(apires, "GetNewAppeals_Check", exc);
                        return apires;
                    }
                }
            }//end lock
        }

        public static ApiResultBase GetNewAppeals(string orgPPAGUID, int k_post)
        {
            ApiResultBase ar = GetNewAppeals_Begin(orgPPAGUID);
            GISGKHIntegration.API.GetAppealsRes res;
                        
            if (ar.error == true || ar.exception == true)
            {
                return ar;
            }

            int n = 0;
            while (true)
            {
                res = GetNewAppeals_Check((ar as ApiResult).messageGUID, orgPPAGUID, k_post);

                if (res.error == true || res.exception == true)
                {
                    return res;
                }
                else
                {
                    if (res.RequestState == RequestStates.RS_PROCESSED)
                    {
                        break;
                    }
                    else
                    {                        
                        Thread.Sleep(5000);
                    }
                }

                n++;
                if (n >= 5) {
                    res = new GISGKHIntegration.API.GetAppealsRes();
                    res.error = true;
                    res.ErrorCode = "TIMEOUT";
                    res.ErrorMessage = "5 trys failed when trying to get new appeals";
                    res.text = "5 trys failed when trying to get new appeals";
                    return res;
                }
            }

            return res;
        }
    }
}
