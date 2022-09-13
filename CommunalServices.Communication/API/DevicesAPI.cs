/* Communal services system integration 
 * Copyright (c) 2021,  Svitkin V.G. 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Globalization;
using CommunalServices.Communication.Data;
using GKH;

namespace GISGKHIntegration
{
    public class DevicesAPI
    {
        public static ApiResult ExportDevices_Begin(string house_guid, string orgPPAGUID)
        {
            lock (GisAPI.csLock)
            {
                GisAPI.LastRequest = ""; GisAPI.LastResponce = "";
                GKH.HouseManagementPortsTypeAsync proxy = new HouseManagementPortsTypeAsyncClient("HouseManagementPortAsync");

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

                    exportMeteringDeviceDataRequest request = new exportMeteringDeviceDataRequest();
                    request.Id = "signed-data-container";
                    request.Items = new object[] { house_guid };
                    request.ItemsElementName = new ItemsChoiceType16[] { ItemsChoiceType16.FIASHouseGuid };

                    try
                    {
                        exportMeteringDeviceDataResponse res;
                        long t1 = Environment.TickCount;

                        //Отправка запроса
                        exportMeteringDeviceDataRequest1 rq = new exportMeteringDeviceDataRequest1(hdr, request);
                        res = proxy.exportMeteringDeviceData(rq);

                        long t2 = Environment.TickCount;
                        apires.in_xml = GisAPI.LastRequest;
                        apires.out_xml = GisAPI.LastResponce;
                        apires.query_duration = (t2 - t1) / 1000.0M;
                        apires.date_query = DateTime.Now;

                        StringBuilder sb = new StringBuilder(300);

                        if (res == null) { apires.text = ("service returned null"); return apires; }

                        var resAck = res.AckRequest.Ack;
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
                        ApiResultBase.InitExceptionResult(apires, "ExportDevices", exc);
                        return apires;
                    }

                }
            }//end lock
        }

        public static ExportDeviceRes ExportDevices_Check(string message_guid, string house_guid, string orgPPAGUID)
        {
            lock (GisAPI.csLock)
            {
                GisAPI.LastRequest = ""; GisAPI.LastResponce = "";
                GKH.HouseManagementPortsTypeAsync proxy = new HouseManagementPortsTypeAsyncClient("HouseManagementPortAsync");
                {
                    ExportDeviceRes apires = new ExportDeviceRes();
                    MDevice dev;

                    apires.HouseGUID = house_guid;

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

                        getStateResponse2 res;//переменная для результата
                        long t1 = Environment.TickCount;
                        GisAPI.DisableSignature = true;//запрос не подписывается

                        /* Отправка запроса*/
                        res = proxy.getState(request);

                        GisAPI.DisableSignature = false;
                        long t2 = Environment.TickCount;
                        apires.query_duration = (t2 - t1) / 1000.0M;
                        apires.date_query = DateTime.Now;
                        apires.in_xml = GisAPI.LastRequest;
                        apires.out_xml = GisAPI.LastResponce;

                        if (res == null) { apires.text = ("service returned null"); return apires; }

                        //Обработка результатов запроса

                        sb.AppendLine("RequestState: " + res.getStateResult.RequestState.ToString());//статус обработки запроса
                        apires.RequestState = (int)res.getStateResult.RequestState;

                        int i = 0;
                        if(res.getStateResult!=null && res.getStateResult.Items!=null)
                        foreach (object item in res.getStateResult.Items)
                        {
                            if (item is exportMeteringDeviceDataResultType)
                            {
                                dev = new MDevice();
                                dev.regnum = (item as exportMeteringDeviceDataResultType).BasicChatacteristicts.MeteringDeviceNumber;
                                dev.gisgkh_num = (item as exportMeteringDeviceDataResultType).MeteringDeviceGISGKHNumber;
                                dev.DeviceGUID = (item as exportMeteringDeviceDataResultType).MeteringDeviceRootGUID;
                                dev.DeviceVGUID = (item as exportMeteringDeviceDataResultType).MeteringDeviceVersionGUID;

                                i++;
                                sb.AppendLine("* Device # " + i.ToString());

                                sb.AppendLine("Num: " + dev.regnum);
                                sb.AppendLine("Model: " +
                                    (item as exportMeteringDeviceDataResultType).BasicChatacteristicts.MeteringDeviceModel);

                                sb.AppendLine("GIS Number: " + dev.gisgkh_num);
                                sb.AppendLine("GUID: " + dev.DeviceGUID);

                                sb.AppendLine("Item: " +
                                    (item as exportMeteringDeviceDataResultType).Item.ToString());

                                if ((item as exportMeteringDeviceDataResultType).MeteringOwner != null)
                                {
                                    foreach (object it in (item as exportMeteringDeviceDataResultType).MeteringOwner)
                                    {
                                        if (it.ToString() == orgPPAGUID)
                                            sb.AppendLine("Owner: " + it.ToString() + " (this)");
                                        else
                                            sb.AppendLine("Owner: " + it.ToString() + " (other)");

                                    }
                                }

                                if ((item as exportMeteringDeviceDataResultType).Items != null)
                                {
                                    foreach (object it in (item as exportMeteringDeviceDataResultType).Items)
                                    {
                                        if (it is GKH.MunicipalResourceNotElectricExportType)
                                        {
                                            dev.ResourceGUID = (it as GKH.MunicipalResourceNotElectricExportType).MunicipalResource.GUID;
                                            sb.AppendLine("*" + (it as GKH.MunicipalResourceNotElectricExportType).MunicipalResource.Code +
                                                " - " + (it as GKH.MunicipalResourceNotElectricExportType).MeteringValue.ToString());
                                        }
                                        else
                                        {
                                            dev.IsElectric = true;
                                            sb.AppendLine("* " + it.GetType().ToString());
                                        }
                                    }
                                }

                                //device type...
                                if ((item as exportMeteringDeviceDataResultType).BasicChatacteristicts.Item
                                    is GKH.MeteringDeviceBasicCharacteristicsTypeResidentialPremiseDevice)
                                {
                                    var el = ((item as exportMeteringDeviceDataResultType).BasicChatacteristicts.Item)
                                    as GKH.MeteringDeviceBasicCharacteristicsTypeResidentialPremiseDevice;

                                    if (el.AccountGUID != null)
                                        foreach (string s in (el.AccountGUID as string[]))
                                        {
                                            dev.AccountGUID = s;
                                        }

                                    if (el.PremiseGUID.Length > 0) dev.PremisesGUID = el.PremiseGUID[0];
                                    else dev.PremisesGUID = "";

                                }
                                else if ((item as exportMeteringDeviceDataResultType).BasicChatacteristicts.Item
                                   is GKH.MeteringDeviceBasicCharacteristicsTypeNonResidentialPremiseDevice)
                                {
                                    var el = ((item as exportMeteringDeviceDataResultType).BasicChatacteristicts.Item)
                                    as GKH.MeteringDeviceBasicCharacteristicsTypeNonResidentialPremiseDevice;

                                    if (el.AccountGUID != null)
                                        foreach (string s in (el.AccountGUID as string[]))
                                        {
                                            dev.AccountGUID = s;
                                        }

                                    if (el.PremiseGUID.Length > 0) dev.PremisesGUID = el.PremiseGUID[0];
                                    else dev.PremisesGUID = "";
                                }
                                else if ((item as exportMeteringDeviceDataResultType).BasicChatacteristicts.Item
                                is GKH.MeteringDeviceBasicCharacteristicsTypeLivingRoomDevice)
                                {
                                    var el = ((item as exportMeteringDeviceDataResultType).BasicChatacteristicts.Item)
                                    as GKH.MeteringDeviceBasicCharacteristicsTypeLivingRoomDevice;

                                    if (el.AccountGUID != null)
                                        foreach (string s in (el.AccountGUID as string[]))
                                        {
                                            dev.AccountGUID = s;
                                        }

                                    dev.PremisesGUID = "";
                                    if (el.LivingRoomGUID != null)
                                    {
                                        dev.PremisesGUID = el.LivingRoomGUID[0];
                                    }
                                }
                                else if ((item as exportMeteringDeviceDataResultType).BasicChatacteristicts.Item
                                is GKH.MeteringDeviceBasicCharacteristicsTypeCollectiveApartmentDevice)
                                {
                                    var el = ((item as exportMeteringDeviceDataResultType).BasicChatacteristicts.Item)
                                    as GKH.MeteringDeviceBasicCharacteristicsTypeCollectiveApartmentDevice;

                                    if (el.AccountGUID != null)
                                        foreach (string s in (el.AccountGUID as string[]))
                                        {
                                            dev.AccountGUID = s;
                                        }

                                    if (el.PremiseGUID.Length > 0) dev.PremisesGUID = el.PremiseGUID[0];
                                    else dev.PremisesGUID = "";
                                }
                                else
                                {
                                    sb.AppendLine("Item: " +
                                    (item as exportMeteringDeviceDataResultType).BasicChatacteristicts.Item.GetType().ToString());
                                    continue;//SKIP OTHERS
                                }//end device type

                                sb.AppendLine("AccountGUID: " + dev.AccountGUID.ToString());
                                sb.AppendLine("PremisesGUID: " + dev.PremisesGUID.ToString());
                                apires.Devices.Add(dev);

                                sb.AppendLine();
                            }
                            else if (item is ErrorMessageType)
                            {
                                sb.AppendLine("* Error *");
                                sb.AppendLine("ErrorCode: " + (item as ErrorMessageType).ErrorCode);
                                sb.AppendLine("ErrorMes: " + (item as ErrorMessageType).Description);
                                sb.AppendLine("StackTrace: ");
                                sb.AppendLine((item as ErrorMessageType).StackTrace);
                                apires.error = true;
                                apires.ErrorCode = (item as ErrorMessageType).ErrorCode;
                                apires.ErrorMessage = (item as ErrorMessageType).Description;
                                apires.StackTrace = (item as ErrorMessageType).StackTrace;
                            }
                        }//end foreach


                        sb.AppendLine("Дата и время запроса: " + apires.date_query);
                        sb.AppendLine("Длительность обработки запроса: " + (apires.query_duration).ToString("F3") + " c.");
                        apires.text = sb.ToString();

                        return apires;
                    }
                    catch (Exception exc)
                    {
                        ApiResultBase.InitExceptionResult(apires, "ExportDevices_Check", exc);
                        return apires;
                    }
                }
            }//end lock
        }

        public static ImportPkzRes ImportPkz_Begin(string house_guid, string orgPPAGUID, PkzData[] pkz)
        {
            lock (GisAPI.csLock)
            {
                GisAPI.LastRequest = ""; GisAPI.LastResponce = "";
                var proxy = new GKH.DeviceMeteringPortTypesAsyncClient("DeviceMeteringPortAsync");

                {
                    ImportPkzRes apires = new ImportPkzRes();

                    //формирование входных параметров запроса

                    RequestHeader hdr = new RequestHeader();//заголовок запроса

                    hdr.Date = DateTime.Now;
                    hdr.MessageGUID = Guid.NewGuid().ToString();
                    hdr.ItemElementName = ItemChoiceType4.orgPPAGUID;
                    hdr.Item = orgPPAGUID;

                    hdr.IsOperatorSignature = true;
                    hdr.IsOperatorSignatureSpecified = true;

                    var request = new importMeteringDeviceValuesRequest();
                    request.Id = "signed-data-container";
                    request.FIASHouseGuid = house_guid;

                    var list = new List<importMeteringDeviceValuesRequestMeteringDevicesValues>();
                    var entries = new List<ImportPkzResEntry>(pkz.Length);

                    foreach (PkzData item in pkz)
                    {
                        if (String.IsNullOrEmpty(item.DeviceGUID))
                        {
                            ImportPkzResEntry entry = new ImportPkzResEntry();
                            entry.gisgkh_num = "";
                            entry.transportGUID = "";
                            entries.Add(entry);
                            continue;
                        }

                        var vals = new importMeteringDeviceValuesRequestMeteringDevicesValues();
                        vals.Item = item.DeviceGUID;
                        vals.ItemElementName = ItemChoiceType7.MeteringDeviceRootGUID;

                        string transportGUID=Guid.NewGuid().ToString();

                        if (item.Resource == "Электрическая энергия")
                        {
                            var electric = new importMeteringDeviceValuesRequestMeteringDevicesValuesElectricDeviceValue();
                            electric.CurrentValue = new ElectricMeteringValueImportType();
                            electric.CurrentValue.TransportGUID = transportGUID;
                            electric.CurrentValue.DateValue = item.DatePkz;

                            if (item.CountPkz > 1)
                            {
                                electric.CurrentValue.MeteringValueT2 = item.Pkz2.ToString(CultureInfo.InvariantCulture);
                            }
                            electric.CurrentValue.MeteringValueT1 = item.Pkz1.ToString(CultureInfo.InvariantCulture);
                            vals.Item1 = electric;
                        }
                        else
                        {
                            var x = new importMeteringDeviceValuesRequestMeteringDevicesValuesOneRateDeviceValue();
                            x.CurrentValue = new OneRateMeteringValueImportType[] {
                                new OneRateMeteringValueImportType()
                            };
                            x.CurrentValue[0].DateValue = item.DatePkz;
                            x.CurrentValue[0].MunicipalResource = new nsiRef();
                            x.CurrentValue[0].TransportGUID = transportGUID;

                            if (item.Resource == "Холодная вода")
                            {
                                x.CurrentValue[0].MunicipalResource.Code = "1";
                                x.CurrentValue[0].MunicipalResource.GUID = "82F90CCA-24DC-4FF7-AC66-05E53070E5A3".ToLower();
                            }
                            else if (item.Resource == "Горячая вода")
                            {
                                x.CurrentValue[0].MunicipalResource.Code = "2";
                                x.CurrentValue[0].MunicipalResource.GUID = "7459C9F5-5D7F-42B4-9CD0-6674737D79FA".ToLower();
                            }
                            else if (item.Resource == "Тепловая энергия")
                            {
                                x.CurrentValue[0].MunicipalResource.Code = "5";
                                x.CurrentValue[0].MunicipalResource.GUID = "25A29BAE-E430-4424-8F34-8AD83C578657".ToLower();
                            }
                            
                            x.CurrentValue[0].MeteringValue = item.Pkz1.ToString(CultureInfo.InvariantCulture);
                            vals.Item1 = x;

                            
                        }//endif

                        list.Add(vals);

                        ImportPkzResEntry pentry = new ImportPkzResEntry();
                        pentry.gisgkh_num = item.GisgkhNum;
                        pentry.transportGUID = transportGUID;
                        entries.Add(pentry);

                    }//end foreach

                    request.MeteringDevicesValues = list.ToArray();

                    try
                    {
                        GKH.ResultHeader res;
                        AckRequest ack;
                        long t1 = Environment.TickCount;

                        //Отправка запроса
                        res = proxy.importMeteringDeviceValues(hdr,request,out ack);

                        long t2 = Environment.TickCount;
                        apires.in_xml = GisAPI.LastRequest;
                        apires.out_xml = GisAPI.LastResponce;
                        apires.query_duration = (t2 - t1) / 1000.0M;
                        apires.date_query = DateTime.Now;

                        StringBuilder sb = new StringBuilder(300);

                        if (res == null) { apires.text = ("service returned null"); return apires; }

                        var resAck = ack.Ack;
                        apires.MessageGUID = resAck.MessageGUID;
                        apires.entries = entries;
                        sb.AppendLine("RequesterMessageGUID: " + resAck.RequesterMessageGUID);
                        sb.AppendLine("MessageGUID: " + resAck.MessageGUID);

                        sb.AppendLine("Дата и время запроса: " + apires.date_query);
                        sb.AppendLine("Длительность обработки запроса: " + (apires.query_duration).ToString("F3") + " c.");
                        apires.text = sb.ToString();

                        return apires;
                    }
                    catch (Exception exc)
                    {
                        ApiResultBase.InitExceptionResult(apires, "ImportPkz_Begin", exc);
                        return apires;
                    }

                }
            }//end lock
        }

        public static ApiResult ImportPkz_Check(string message_guid, string orgPPAGUID)
        {
            lock (GisAPI.csLock)
            {
                GisAPI.LastRequest = ""; GisAPI.LastResponce = "";
                var proxy = new GKH.DeviceMeteringPortTypesAsyncClient("DeviceMeteringPortAsync");
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

                    try
                    {
                        StringBuilder sb = new StringBuilder(300);
                        var request = new getStateRequest();
                        request.MessageGUID = message_guid;
                        var result = new getStateResult1();

                        GKH.ResultHeader res;//переменная для результата
                        long t1 = Environment.TickCount;
                        GisAPI.DisableSignature = true;//запрос не подписывается

                        /* Отправка запроса*/
                        res = proxy.getState(hdr, request, out result);

                        long t2 = Environment.TickCount;
                        apires.query_duration = (t2 - t1) / 1000.0M;
                        apires.date_query = DateTime.Now;
                        apires.in_xml = GisAPI.LastRequest;
                        apires.out_xml = GisAPI.LastResponce;

                        if (res == null) { apires.text = ("service returned null"); return apires; }

                        //Обработка результатов запроса

                        sb.AppendLine("RequestState: " + result.RequestState.ToString());//статус обработки запроса
                        apires.RequestState = (int)result.RequestState;

                        object[] items=null;
                        items = result.Items;
                        if (items == null) items = new object[0];

                        sb.AppendLine("Items count: "+items.Length.ToString());
                        apires.entries = new List<ApiResultEntry>(items.Length);

                        for (int i = 0; i < items.Length; i++)
                        {
                            if (items[i] is ErrorMessageType)
                            {
                                sb.AppendLine("* Error *");
                                sb.AppendLine("ErrorCode: " + (items[i] as ErrorMessageType).ErrorCode);
                                sb.AppendLine("ErrorMes: " + (items[i] as ErrorMessageType).Description);
                                sb.AppendLine("StackTrace: ");
                                sb.AppendLine((items[i] as ErrorMessageType).StackTrace);
                            }
                            else if (items[i] is CommonResultType)
                            {
                                var crt = items[i] as CommonResultType;
                                sb.AppendLine("* ImportResult *");
                                sb.AppendLine("GUID: " + crt.GUID);
                                sb.AppendLine("TransportGUID: " + crt.TransportGUID);

                                object[] itemsarr;
                                if (crt.Items != null)
                                {
                                    sb.AppendLine("Items: " + crt.Items.Length.ToString());
                                    itemsarr = crt.Items;
                                }
                                else itemsarr = new object[0];

                                ApiResultEntry entry = new ApiResultEntry();
                                entry.TransportGUID = crt.TransportGUID;
                                entry.success = true;

                                foreach (object x in itemsarr)
                                {
                                    if (x is CommonResultTypeError)
                                    {
                                        entry.success = false;
                                        entry.ErrorCode = (x as CommonResultTypeError).ErrorCode;
                                        entry.ErrorMessage = (x as CommonResultTypeError).Description;
                                        entry.StackTrace = (x as CommonResultTypeError).StackTrace;
                                    }
                                    else if (x is DateTime)
                                    {
                                        entry.success = true;
                                        entry.ImportDate = ((DateTime)x);
                                    }
                                }

                                apires.entries.Add(entry);

                            }
                            else sb.AppendLine(items[i].GetType().ToString());
                        }

                        sb.AppendLine("Дата и время запроса: " + apires.date_query);
                        sb.AppendLine("Длительность обработки запроса: " + (apires.query_duration).ToString("F3") + " c.");
                        apires.text = sb.ToString();

                        return apires;
                    }
                    catch (Exception exc)
                    {
                        ApiResultBase.InitExceptionResult(apires, "ImportPkz_Check", exc);
                        return apires;
                    }
                    finally { GisAPI.DisableSignature = false; }
                }
            }//end lock
        }
    }
}
