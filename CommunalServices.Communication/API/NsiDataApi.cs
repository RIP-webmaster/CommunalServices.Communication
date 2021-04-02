/* Communal services system integration 
 * Copyright (c) 2021,  Svitkin V.G. 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using GKH;

namespace GISGKHIntegration.API
{
    public static class NsiDataApi
    {
        public static ApiResult ExportOrgNsi_Begin(string number, string orgPPAGUID)
        {
            lock (GisAPI.csLock)
            {
                GisAPI.LastRequest = ""; GisAPI.LastResponce = "";
                GKH.NsiPortsTypeAsync1Client proxy = new NsiPortsTypeAsync1Client("NsiPortAsync1");
                {
                    ApiResult apires = new ApiResult();

                    //формирование входных параметров запроса

                    RequestHeader hdr = new RequestHeader();//заголовок запроса

                    hdr.Date = DateTime.Now;
                    hdr.MessageGUID = Guid.NewGuid().ToString();
                    hdr.ItemElementName = ItemChoiceType4.orgPPAGUID;
                    hdr.Item = orgPPAGUID;
                    hdr.IsOperatorSignature = true;
                    hdr.IsOperatorSignatureSpecified = true;

                    //тело запроса
                    exportDataProviderNsiItemRequest request = new exportDataProviderNsiItemRequest();
                    request.Id = "signed-data-container";
                    switch (number)
                    {
                        case "1": request.RegistryNumber = exportDataProviderNsiItemRequestRegistryNumber.Item1;
                            break;
                        case "51": request.RegistryNumber = exportDataProviderNsiItemRequestRegistryNumber.Item51;
                            break;
                    }

                    try
                    {
                        AckRequest res;
                        long t1 = Environment.TickCount;

                        //Отправка запроса
                        var resp = proxy.exportDataProviderNsiItem(hdr, request, out res);

                        long t2 = Environment.TickCount;
                        apires.in_xml = GisAPI.LastRequest;
                        apires.out_xml = GisAPI.LastResponce;
                        apires.query_duration = (t2 - t1) / 1000.0M;
                        apires.date_query = DateTime.Now;

                        StringBuilder sb = new StringBuilder(300);

                        if (resp == null) { apires.text = ("service returned null"); return apires; }

                        var resAck = res.Ack;
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
                        ApiResultBase.InitExceptionResult(apires, "ExportOrgNsi_Begin", exc);
                        return apires;

                        /*apires.in_xml = GisAPI.LastRequest;
                        apires.out_xml = GisAPI.LastResponce;
                        StringBuilder sb = new StringBuilder(300);
                        sb.AppendLine("ExportOrgNsi_Begin failed!");
                        sb.AppendLine(exc.ToString());
                        sb.AppendLine();
                        sb.AppendLine("Дата и время запроса: " + apires.date_query);
                        sb.AppendLine("Длительность обработки запроса: " + (apires.query_duration).ToString("F3") + " c.");
                        apires.text = (sb.ToString());
                        apires.ex = exc;
                        apires.exception = true;
                        return apires;*/
                    }
                }
            }//end lock
        }

        public static ExportNsiRes ExportOrgNsi_Check(string message_guid, string number, string orgPPAGUID)
        {
            lock (GisAPI.csLock)
            {
                GisAPI.LastRequest = ""; GisAPI.LastResponce = "";
                GKH.NsiPortsTypeAsync1Client proxy = new NsiPortsTypeAsync1Client("NsiPortAsync1");
                {
                    ExportNsiRes apires = new ExportNsiRes();
                    
                    //формирование входных параметров запроса

                    RequestHeader hdr = new RequestHeader();//заголовок запроса

                    hdr.Date = DateTime.Now;
                    hdr.MessageGUID = Guid.NewGuid().ToString();
                    hdr.ItemElementName = ItemChoiceType4.orgPPAGUID;
                    hdr.Item = orgPPAGUID;

                    hdr.IsOperatorSignature = true;
                    hdr.IsOperatorSignatureSpecified = true;

                    try
                    {
                        StringBuilder sb = new StringBuilder(300);
                        getStateRequest3 request = new getStateRequest3();
                        request.RequestHeader = hdr;

                        //тело запроса                        
                        getStateRequest body = new getStateRequest();
                        body.MessageGUID = message_guid;
                        request.getStateRequest = body;

                        getStateResult4 resp;//переменная для результата
                        long t1 = Environment.TickCount;
                        GisAPI.DisableSignature = true;//запрос не подписывается

                        /* Отправка запроса*/
                        ResultHeader res = proxy.getState(request.RequestHeader,request.getStateRequest,out resp);

                        GisAPI.DisableSignature = false;
                        long t2 = Environment.TickCount;
                        apires.query_duration = (t2 - t1) / 1000.0M;
                        apires.date_query = DateTime.Now;
                        apires.in_xml = GisAPI.LastRequest;
                        apires.out_xml = GisAPI.LastResponce;

                        if (res == null) { apires.text = ("service returned null"); return apires; }

                        //Обработка результатов запроса
                        Data.NsiItem nsitem;
                        sb.AppendLine("RequestState: " + resp.RequestState.ToString());//статус обработки запроса
                        apires.RequestState = (int)resp.RequestState;
                        
                        int n = 0;
                        if (resp != null && resp.Items != null)
                            foreach (object item in resp.Items)
                            {
                                if (item is ErrorMessageType)
                                {
                                    sb.AppendLine("* Error *");
                                    sb.AppendLine("ErrorCode: " + (item as ErrorMessageType).ErrorCode);
                                    sb.AppendLine("ErrorMes: " + (item as ErrorMessageType).Description);
                                    sb.AppendLine("StackTrace: ");
                                    sb.AppendLine((item as ErrorMessageType).StackTrace);
                                    sb.AppendLine();
                                }

                                if (item is NsiItemType)
                                {
                                    NsiItemType nsi = item as NsiItemType;
                                    sb.AppendLine("* NSI item *");
                                    sb.AppendLine("Created: " + nsi.Created.ToString());
                                    sb.AppendLine("Registry number: " + nsi.NsiItemRegistryNumber);
                                    sb.AppendLine("Elements: " + nsi.NsiElement.Length.ToString());
                                    sb.AppendLine();


                                    foreach (NsiElementType x in nsi.NsiElement)
                                    {
                                        nsitem = new Data.NsiItem();
                                        nsitem.Created = nsi.Created;
                                        nsitem.SpravNumber = nsi.NsiItemRegistryNumber;
                                        nsitem.Code = x.Code;
                                        nsitem.GUID = x.GUID;
                                        nsitem.Values = "";

                                        sb.Append("- " + x.Code + " ");
                                        if (x.IsActual) sb.Append("(Actual) ");
                                        else sb.Append("(Not actual) ");

                                        if (x.ChildElement != null)
                                        {
                                            sb.Append("Childs: " + x.ChildElement.Length.ToString() + " ");
                                        }

                                        sb.AppendLine(x.GUID);

                                        n = 0;
                                        foreach (var y in x.NsiElementField)
                                        {
                                            if (y is NsiElementStringFieldType)
                                            {
                                                sb.AppendLine("* " + (y as NsiElementStringFieldType).Name + "=" +
                                                    (y as NsiElementStringFieldType).Value);
                                                nsitem.Values += (y as NsiElementStringFieldType).Name + "=" +
                                                    (y as NsiElementStringFieldType).Value + "; ";

                                                if (n == 0)//Name
                                                {
                                                    nsitem.Name = (y as NsiElementStringFieldType).Value;
                                                    n++;
                                                }
                                                else if (n == 1)//Second Name
                                                {
                                                    nsitem.Name2 = (y as NsiElementStringFieldType).Value;
                                                    n++;
                                                }
                                            }
                                            else if (y is NsiElementBooleanFieldType)
                                            {
                                                sb.AppendLine("* " + (y as NsiElementBooleanFieldType).Name + "=" +
                                                    (y as NsiElementBooleanFieldType).Value.ToString());
                                                nsitem.Values += (y as NsiElementBooleanFieldType).Name + "=" +
                                                    (y as NsiElementBooleanFieldType).Value.ToString() + "; ";
                                            }
                                            else
                                            {
                                                sb.AppendLine("* " + (y).Name + " - " +
                                                    y.GetType().ToString());
                                            }

                                        }

                                        if (x.IsActual) apires.NsiItems.Add(nsitem);

                                        sb.AppendLine();

                                    }

                                    sb.AppendLine();
                                }
                            }//end foreach


                        sb.AppendLine("Дата и время запроса: " + apires.date_query);
                        sb.AppendLine("Длительность обработки запроса: " + (apires.query_duration).ToString("F3") + " c.");
                        apires.text = sb.ToString();

                        return apires;
                    }
                    catch (Exception exc)
                    {
                        ApiResultBase.InitExceptionResult(apires, "ExportOrgNsi_Check", exc);
                        return apires;
                    }
                }
            }//end lock
        }//end method

        //*****
        /*public static ExportNsiRes ExportNsi(string number)
        {
            lock (csLock)
            {
                LastRequest = ""; LastResponce = "";
                using (GKH.NsiPortsTypeClient proxy =
                    new NsiPortsTypeClient("NsiPort"))
                {
                    ExportNsiRes apires = new ExportNsiRes();
                    Data.NsiItem item;
                    int n;

                    //формирование входных параметров запроса

                    ISRequestHeader hdr = new ISRequestHeader();//заголовок запроса

                    hdr.Date = DateTime.Now;
                    hdr.MessageGUID = Guid.NewGuid().ToString();

                    exportNsiItemRequest request = new exportNsiItemRequest();
                    request.Id = "signed-data-container";
                    request.RegistryNumber = number;
                    request.ListGroup = ListGroup.NSI;

                    //284f62c1-c88d-43b3-aebe-db755b57665d

                    try
                    {
                        exportNsiItemResult res;
                        long t1 = Environment.TickCount;

                        //Отправка запроса
                        var resp = proxy.exportNsiItem(hdr, request, out res);

                        long t2 = Environment.TickCount;
                        apires.in_xml = GisAPI.LastRequest;
                        apires.out_xml = GisAPI.LastResponce;
                        apires.query_duration = (t2 - t1) / 1000.0M;
                        apires.date_query = DateTime.Now;

                        StringBuilder sb = new StringBuilder(300);

                        if (resp == null) { apires.text = ("service returned null"); return apires; }

                        //Обработка результатов запроса

                        if (res.Item is ErrorMessageType)
                        {
                            sb.AppendLine("* Error *");
                            sb.AppendLine("ErrorCode: " + (res.Item as ErrorMessageType).ErrorCode);
                            sb.AppendLine("ErrorMes: " + (res.Item as ErrorMessageType).Description);
                            sb.AppendLine("StackTrace: ");
                            sb.AppendLine((res.Item as ErrorMessageType).StackTrace);
                            sb.AppendLine();
                        }

                        if (res.Item is NsiItemType)
                        {
                            NsiItemType nsi = res.Item as NsiItemType;
                            sb.AppendLine("* NSI item *");
                            sb.AppendLine("Created: " + nsi.Created.ToString());
                            sb.AppendLine("Registry number: " + nsi.NsiItemRegistryNumber);
                            sb.AppendLine("Elements: " + nsi.NsiElement.Length.ToString());
                            sb.AppendLine();


                            foreach (NsiElementType x in nsi.NsiElement)
                            {
                                item = new Data.NsiItem();
                                item.Created = nsi.Created;
                                item.SpravNumber = nsi.NsiItemRegistryNumber;
                                item.Code = x.Code;
                                item.GUID = x.GUID;
                                item.Values = "";

                                sb.Append("- " + x.Code + " ");
                                if (x.IsActual) sb.Append("(Actual) ");
                                else sb.Append("(Not actual) ");

                                if (x.ChildElement != null)
                                {
                                    sb.Append("Childs: " + x.ChildElement.Length.ToString() + " ");
                                }

                                sb.AppendLine(x.GUID);

                                n = 0;
                                foreach (var y in x.NsiElementField)
                                {
                                    if (y is NsiElementStringFieldType)
                                    {
                                        sb.AppendLine("* " + (y as NsiElementStringFieldType).Name + "=" +
                                            (y as NsiElementStringFieldType).Value);
                                        item.Values += (y as NsiElementStringFieldType).Name + "=" +
                                            (y as NsiElementStringFieldType).Value + "; ";

                                        if (n == 0)//Name
                                        {
                                            item.Name = (y as NsiElementStringFieldType).Value;
                                            n++;
                                        }
                                        else if (n == 1)//Second Name
                                        {
                                            item.Name2 = (y as NsiElementStringFieldType).Value;
                                            n++;
                                        }
                                    }
                                    else if (y is NsiElementBooleanFieldType)
                                    {
                                        sb.AppendLine("* " + (y as NsiElementBooleanFieldType).Name + "=" +
                                            (y as NsiElementBooleanFieldType).Value.ToString());
                                        item.Values += (y as NsiElementBooleanFieldType).Name + "=" +
                                            (y as NsiElementBooleanFieldType).Value.ToString() + "; ";
                                    }
                                    else
                                    {
                                        sb.AppendLine("* " + (y).Name + " - " +
                                            y.GetType().ToString());
                                    }

                                }

                                if (x.IsActual) apires.NsiItems.Add(item);

                                sb.AppendLine();


                            }

                            sb.AppendLine();
                        }


                        sb.AppendLine("Дата и время запроса: " + apires.date_query);
                        sb.AppendLine("Длительность обработки запроса: " + (apires.query_duration).ToString("F3") + " c.");
                        apires.text = sb.ToString();

                        return apires;
                    }
                    catch (Exception exc)
                    {
                        apires.in_xml = GisAPI.LastRequest;
                        apires.out_xml = GisAPI.LastResponce;
                        StringBuilder sb = new StringBuilder(300);
                        sb.AppendLine("ExportNsi failed!");
                        sb.AppendLine(exc.ToString());
                        sb.AppendLine();
                        sb.AppendLine("Дата и время запроса: " + apires.date_query);
                        sb.AppendLine("Длительность обработки запроса: " + (apires.query_duration).ToString("F3") + " c.");
                        apires.text = (sb.ToString());
                        apires.ex = exc;
                        apires.exception = true;
                        return apires;
                    }

                }
            }//end lock
        }*/
        //*****

    }
}
