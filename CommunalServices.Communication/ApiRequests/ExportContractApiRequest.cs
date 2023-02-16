/* Communal services system integration 
 * Copyright (c) 2022,  Svitkin V.G. 
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
    /// Экспорт информации о договоре управления (уставе), являющимся основанием для управления домом
    /// </summary>
    public class ExportContractApiRequest : ApiRequestBase
    {
        public ExportContractApiRequest(string orgPPAGUID, int k_post, string houseGuid)
        {
            this.OrgPpaGuid = orgPPAGUID;
            this.KPost = k_post;
            this.HouseGuid = houseGuid;
        }

        public string HouseGuid { get; set; }

        public static ApiResult ExportContract_Begin(string orgPPAGUID, string houseGuid)
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
                hdr.Item = orgPPAGUID;

                hdr.IsOperatorSignature = true;
                hdr.IsOperatorSignatureSpecified = true;
                
                var request = new exportCAChAsyncRequest();
                request.Id = "signed-data-container";
                request.version = "13.1.2.1";

                //критерии
                List<object> itemsList = new List<object>(3);
                List<ItemsChoiceType37> choicesList = new List<ItemsChoiceType37>(3);
                itemsList.Add(orgPPAGUID);
                choicesList.Add(ItemsChoiceType37.UOGUID);
                itemsList.Add(houseGuid);
                choicesList.Add(ItemsChoiceType37.FIASHouseGuid);
                itemsList.Add(true);
                choicesList.Add(ItemsChoiceType37.LastVersionOnly);
                exportCAChRequestCriteriaType crit = new exportCAChRequestCriteriaType();
                crit.Items = itemsList.ToArray();
                crit.ItemsElementName = choicesList.ToArray();
                request.Criteria = new exportCAChRequestCriteriaType[]{crit};

                try
                {
                    long t1 = Environment.TickCount;
                    AckRequest ack;

                    //Отправка запроса
                    var res = proxy.exportCAChData(hdr, request, out ack);

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
                    ApiResultBase.InitExceptionResult(apires, "ExportContract", exc);
                    return apires;
                }
                finally
                {
                    try { proxy.Close(); }
                    catch (Exception) { }
                }

            }//end lock
        }

        public static ApiResult ExportContract_Check(string message_guid, string orgPPAGUID)
        {
            lock (GisAPI.csLock)
            {
                GisAPI.LastRequest = ""; GisAPI.LastResponce = "";
                var proxy = new HouseManagementPortsTypeAsyncClient("HouseManagementPortAsync");

                var apires = new ApiResult();
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
                    var request = new getStateRequest1();
                    request.RequestHeader = hdr;

                    //тело запроса                        
                    getStateRequest body = new getStateRequest();
                    body.MessageGUID = message_guid;
                    request.getStateRequest = body;

                    long t1 = Environment.TickCount;
                    GisAPI.DisableSignature = true;//запрос не подписывается

                    /* Отправка запроса*/
                    getStateResult2 result;
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
                            if (item is exportCAChResultType)
                            {
                                sb.AppendLine("* exportCAChResultType *");
                                exportCAChResultType cach = (exportCAChResultType)item;
                                
                                if (cach.Item is exportCAChResultTypeContract)
                                {
                                    var contract = (exportCAChResultTypeContract)cach.Item;
                                    sb.AppendLine("ContractGUID: " + contract.ContractGUID);
                                    sb.AppendLine("ContractVersionGUID: " + contract.ContractVersionGUID);
                                    sb.AppendLine("DocNum: " + contract.DocNum);
                                    sb.AppendLine("SigningDate: " + contract.SigningDate.ToString());
                                    sb.AppendLine("Status: " + contract.ContractStatus.ToString());
                                    
                                    if (contract.ContractStatus == ContractStatusExportType.Approved ||
                                        contract.ContractStatus == ContractStatusExportType.Reviewed)
                                    {
                                        apires.SetData("ContractGUID", contract.ContractGUID);
                                        apires.SetData("ContractVersionGUID", contract.ContractVersionGUID);

                                        if (contract.ContractObject != null && contract.ContractObject.Length == 1)
                                        {
                                            sb.AppendLine("ContractObjectVersionGUID: " + contract.ContractObject[0].ContractObjectVersionGUID);
                                            apires.SetData("ContractObjectVersionGUID", contract.ContractObject[0].ContractObjectVersionGUID);
                                        }
                                    }
                                }
                                else if (cach.Item is exportCAChResultTypeCharter)
                                {
                                    var charter = (exportCAChResultTypeCharter)cach.Item;
                                    sb.AppendLine("CharterGUID: " + charter.CharterGUID);
                                    sb.AppendLine("CharterVersionGUID: " + charter.CharterVersionGUID);
                                    sb.AppendLine("Date: " + charter.Date.ToString());
                                    sb.AppendLine("Status: " + charter.CharterStatus.ToString());

                                    if (charter.CharterStatus == CharterStatusExportType.Approved ||
                                        charter.CharterStatus == CharterStatusExportType.Reviewed)
                                    {
                                        apires.SetData("ContractGUID", charter.CharterGUID);
                                        apires.SetData("ContractVersionGUID", charter.CharterVersionGUID);

                                        if (charter.ContractObject != null && charter.ContractObject.Length == 1)
                                        {
                                            sb.AppendLine("ContractObjectVersionGUID: " + charter.ContractObject[0].ContractObjectVersionGUID);
                                            apires.SetData("ContractObjectVersionGUID", charter.ContractObject[0].ContractObjectVersionGUID);
                                        }
                                    }
                                }
                                else
                                {
                                    sb.AppendLine(cach.Item.GetType().ToString());
                                }
                            }
                            else if (item is getStateResultImportResult)
                            {
                                var ires = (getStateResultImportResult)item;
                                if (ires.Items == null) ires.Items = new object[0];

                                sb.AppendLine("getStateResultImportResult, items: " + ires.Items.Length.ToString());

                                foreach (object x in ires.Items)
                                {
                                    if (x is ErrorMessageType)
                                    {
                                        sb.AppendLine("-Error: " + (x as ErrorMessageType).Description);
                                    }
                                    else if (x is getStateResultImportResultCommonResult)
                                    {
                                        var rcr = (getStateResultImportResultCommonResult)x;
                                        if (rcr.Items == null) rcr.Items = new object[0];

                                        foreach (object y in rcr.Items)
                                        {
                                            if (y is CommonResultTypeError)
                                            {
                                                sb.AppendLine("--Error: " + (y as CommonResultTypeError).Description);
                                                apires.error = true;
                                                apires.ErrorCode = (y as CommonResultTypeError).ErrorCode;
                                                apires.ErrorMessage = (y as CommonResultTypeError).Description;
                                                apires.StackTrace = (y as CommonResultTypeError).StackTrace;
                                            }
                                            else sb.AppendLine("--" + y.GetType().ToString());
                                        }
                                    }
                                    else sb.AppendLine("-" + x.GetType().ToString());
                                }
                            }
                            else if (item is exportVotingProtocolResultType)
                            {
                                var vprt = (exportVotingProtocolResultType)item;
                                sb.AppendLine("* exportVotingProtocolResultType *");
                                sb.AppendLine("VotingProtocolGUID: " + vprt.VotingProtocolGUID);
                                sb.AppendLine("RootProtocolGUID: " + vprt.RootProtocolGUID);
                                sb.AppendLine("ProtocolDate: " + vprt.ProtocolDate.ToString());
                                sb.AppendLine("ProtocolNum: " + vprt.ProtocolNum);
                                sb.AppendLine("StatusProtocol: " + vprt.StatusProtocol);
                                sb.AppendLine("StatusVersionProtocol: " + vprt.StatusVersionProtocol);
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

                    return apires;
                }
                catch (Exception exc)
                {
                    GisAPI.DisableSignature = false;
                    ApiResultBase.InitExceptionResult(apires, "ExportWorkingList_Check", exc);
                    return apires;
                }

            }//end lock
        }

        public override ApiResultBase Send()
        {
            ApiResultBase ret = ExportContract_Begin(this.OrgPpaGuid, this.HouseGuid);
            this.MessageGuid = ret.messageGUID;
            return ret;
        }

        public override ApiResultBase CheckState()
        {
            ApiResultBase ret = ExportContract_Check(this.MessageGuid, this.OrgPpaGuid);
            return ret;
        }
    }
}

