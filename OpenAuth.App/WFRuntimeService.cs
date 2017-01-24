﻿using System;
using System.Collections.Generic;
using Infrastructure;
using OpenAuth.App.Extention;
using OpenAuth.App.SSO;
using OpenAuth.Domain;
using OpenAuth.Domain.Interface;

namespace OpenAuth.App
{


    /// <summary>
    /// 流程运行
    /// <para>李玉宝新增于2017-01-17 9:02:02</para>
    /// </summary>
    public class WFRuntimeService 
    {
        private IUnitWork _unitWork;
        private WFProcessInstanceService wfProcessInstanceService;

        public WFRuntimeService(IUnitWork unitWork, WFProcessInstanceService service)
        {
            _unitWork = unitWork;
            wfProcessInstanceService = service;
        }

        private string delegateUserList = "";

        #region 流程处理API
        /// <summary>
        /// 创建一个实例
        /// </summary>
        /// <param name="processId">进程GUID</param>
        /// <param name="schemeInfoId">模板信息ID</param>
        /// <param name="wfLevel"></param>
        /// <param name="code">进程编号</param>
        /// <param name="customName">自定义名称</param>
        /// <param name="description">备注</param>
        /// <param name="frmData">表单数据信息</param>
        /// <returns></returns>
        public bool CreateInstance(Guid processId, Guid schemeInfoId, WFProcessInstance WFProcessInstance, string frmData = null)
        {
            
            try
            {
                WFSchemeInfo WFSchemeInfo = _unitWork.FindSingle<WFSchemeInfo>(u =>u.Id == schemeInfoId);
                WFSchemeContent WFSchemeContent = _unitWork.FindSingle<WFSchemeContent>(u =>
                u.SchemeInfoId==schemeInfoId && u.SchemeVersion ==WFSchemeInfo.SchemeVersion);

                WF_RuntimeInitModel wfRuntimeInitModel = new WF_RuntimeInitModel()
                {
                    schemeContent = WFSchemeContent.SchemeContent,
                    currentNodeId = "",
                    frmData = frmData,
                    processId = processId
                };
                IWF_Runtime wfruntime = null;

                    if(frmData == null)
                    {
                         throw new Exception("自定义表单需要提交表单数据");
                    }
                    else
                    {
                        wfruntime = new WF_Runtime(wfRuntimeInitModel);
                    }
                    

                #region 实例信息
                WFProcessInstance.ActivityId = wfruntime.runtimeModel.nextNodeId;
                WFProcessInstance.ActivityType = wfruntime.GetStatus();//-1无法运行,0会签开始,1会签结束,2一般节点,4流程运行结束
                WFProcessInstance.ActivityName = wfruntime.runtimeModel.nextNode.name;
                WFProcessInstance.PreviousId = wfruntime.runtimeModel.currentNodeId;
                WFProcessInstance.SchemeType = WFSchemeInfo.SchemeType;
                WFProcessInstance.FrmType = WFSchemeInfo.FrmType;
                WFProcessInstance.EnabledMark = 1;//正式运行
                WFProcessInstance.MakerList =(wfruntime.GetStatus() != 4 ? GetMakerList(wfruntime) : "");//当前节点可执行的人信息
                WFProcessInstance.IsFinish = (wfruntime.GetStatus() == 4 ? 1 : 0);
                #endregion

                #region 实例模板
                var data = new
                {
                    SchemeContent = WFSchemeContent.SchemeContent,
                    frmData = frmData
                };
                WFProcessScheme WFProcessScheme = new WFProcessScheme { 
                    SchemeInfoId = schemeInfoId,
                    SchemeVersion = WFSchemeInfo.SchemeVersion,
                    ProcessType = 1,//1正式，0草稿
                    SchemeContent = data.ToJson().ToString()
                };
                #endregion

                #region 流程操作记录
                WFProcessOperationHistory processOperationHistoryEntity = new WFProcessOperationHistory();
                processOperationHistoryEntity.Content = "【创建】" + "todo"+ "创建了一个流程进程【" + WFProcessInstance.Code + "/" + WFProcessInstance.CustomName + "】";
                #endregion

                #region 流转记录
                WFProcessTransitionHistory processTransitionHistoryEntity = new WFProcessTransitionHistory();
                processTransitionHistoryEntity.FromNodeId = wfruntime.runtimeModel.currentNodeId;
                processTransitionHistoryEntity.FromNodeName = wfruntime.runtimeModel.currentNode.name.Value;
                processTransitionHistoryEntity.FromNodeType = wfruntime.runtimeModel.currentNodeType;
                processTransitionHistoryEntity.ToNodeId = wfruntime.runtimeModel.nextNodeId;
                processTransitionHistoryEntity.ToNodeName = wfruntime.runtimeModel.nextNode.name.Value;
                processTransitionHistoryEntity.ToNodeType = wfruntime.runtimeModel.nextNodeType;
                processTransitionHistoryEntity.TransitionSate =0;
                processTransitionHistoryEntity.IsFinish = (processTransitionHistoryEntity.ToNodeType == 4 ? 1 : 0);
                #endregion

                #region 委托记录
                //List<WFDelegateRecord> delegateRecordEntitylist = GetDelegateRecordList(schemeInfoId, WFProcessInstance.Code, WFProcessInstance.CustomName, WFProcessInstance.MakerList);
                //WFProcessInstance.MakerList += delegateUserList;
                #endregion

                wfProcessInstanceService.SaveProcess(wfruntime.runtimeModel, WFProcessInstance, WFProcessScheme, processOperationHistoryEntity, processTransitionHistoryEntity);

                return true;
            }
            catch
            {
                throw;
            }
            
        }
        /// <summary>
        /// 创建一个实例(草稿创建)
        /// </summary>
        /// <param name="processId"></param>
        /// <param name="code"></param>
        /// <param name="customName"></param>
        /// <param name="description"></param>
        /// <param name="frmData"></param>
        /// <returns></returns>
        public bool CreateInstance(WFProcessInstance WFProcessInstance, string frmData = null)
        {
            try
            {
                WFProcessInstance _WFProcessInstance = wfProcessInstanceService.GetEntity(WFProcessInstance.Id);
                WFProcessScheme WFProcessScheme = _unitWork.FindSingle<WFProcessScheme>(u =>u.Id ==WFProcessInstance.ProcessSchemeId);
                dynamic schemeContentJson = WFProcessScheme.SchemeContent.ToJson();//获取工作流模板内容的json对象;
                WF_RuntimeInitModel wfRuntimeInitModel = new WF_RuntimeInitModel()
                {
                    schemeContent = schemeContentJson.SchemeContent.Value,
                    currentNodeId = "",
                    frmData = frmData,
                    processId = WFProcessScheme.Id
                };
                IWF_Runtime wfruntime = null;

                if (frmData == null)
                {
                    throw new Exception("自定义表单需要提交表单数据");
                }
                else
                {
                    wfruntime = new WF_Runtime(wfRuntimeInitModel);
                }
                    

                #region 实例信息
                WFProcessInstance.ActivityId = wfruntime.runtimeModel.nextNodeId;
                WFProcessInstance.ActivityType = wfruntime.GetStatus();//-1无法运行,0会签开始,1会签结束,2一般节点,4流程运行结束
                WFProcessInstance.ActivityName = wfruntime.runtimeModel.nextNode.name;
                WFProcessInstance.PreviousId = wfruntime.runtimeModel.currentNodeId;
                WFProcessInstance.EnabledMark = 1;//正式运行
                WFProcessInstance.MakerList = (wfruntime.GetStatus() != 4 ? GetMakerList(wfruntime) : "");//当前节点可执行的人信息
                WFProcessInstance.IsFinish = (wfruntime.GetStatus() == 4 ? 1 : 0);
                #endregion

                #region 实例模板
                var data = new
                {
                    SchemeContent = schemeContentJson.SchemeContent.Value,
                    frmData = frmData
                };
                WFProcessScheme.ProcessType = 1;//1正式，0草稿
                WFProcessScheme.SchemeContent = data.ToJson().ToString();
                #endregion

                #region 流程操作记录
                WFProcessOperationHistory processOperationHistoryEntity = new WFProcessOperationHistory();
                processOperationHistoryEntity.Content = "【创建】" + "todo name" + "创建了一个流程进程【" + WFProcessInstance.Code + "/" + WFProcessInstance.CustomName + "】";
                #endregion

                #region 流转记录
                WFProcessTransitionHistory processTransitionHistoryEntity = new WFProcessTransitionHistory();
                processTransitionHistoryEntity.FromNodeId = wfruntime.runtimeModel.currentNodeId;
                processTransitionHistoryEntity.FromNodeName = wfruntime.runtimeModel.currentNode.name.Value;
                processTransitionHistoryEntity.FromNodeType = wfruntime.runtimeModel.currentNodeType;
                processTransitionHistoryEntity.ToNodeId = wfruntime.runtimeModel.nextNodeId;
                processTransitionHistoryEntity.ToNodeName = wfruntime.runtimeModel.nextNode.name.Value;
                processTransitionHistoryEntity.ToNodeType = wfruntime.runtimeModel.nextNodeType;
                processTransitionHistoryEntity.TransitionSate = 0;
                processTransitionHistoryEntity.IsFinish = (processTransitionHistoryEntity.ToNodeType == 4 ? 1 : 0);
                #endregion

                #region 委托记录
                //List<WFDelegateRecord> delegateRecordEntitylist = GetDelegateRecordList(WFProcessScheme.SchemeInfoId, WFProcessInstance.Code, WFProcessInstance.CustomName, WFProcessInstance.MakerList);
                //WFProcessInstance.MakerList += delegateUserList;
                #endregion

                wfProcessInstanceService.SaveProcess(wfruntime.runtimeModel, WFProcessInstance, WFProcessScheme, processOperationHistoryEntity, processTransitionHistoryEntity);
                return true;
            }
            catch
            {
                throw;
            }
        }
        /// <summary>
        /// 编辑表单再次提交(驳回后处理)
        /// </summary>
        /// <param name="processId"></param>
        /// <param name="description"></param>
        /// <param name="frmData"></param>
        /// <returns></returns>
        public bool EditionInstance(Guid processId, string description, string frmData = null)
        {
            try
            {
                WFProcessInstance WFProcessInstance = wfProcessInstanceService.GetEntity(processId);
                WFProcessScheme WFProcessScheme = _unitWork.FindSingle< WFProcessScheme>(u =>u.Id ==WFProcessInstance.ProcessSchemeId);
                dynamic schemeContentJson = WFProcessScheme.SchemeContent.ToJson();//获取工作流模板内容的json对象;
                var data = new
                {
                    SchemeContent = schemeContentJson.SchemeContent.Value,
                    frmData = frmData
                };
                WFProcessScheme.SchemeContent = data.ToJson().ToString();

                WFProcessInstance.IsFinish = 0;
                if (string.IsNullOrEmpty(description))
                {
                    WFProcessInstance.Description = description;
                }
                WFProcessInstance.CreateDate = DateTime.Now;

                #region 流程操作记录
                WFProcessOperationHistory processOperationHistoryEntity = new WFProcessOperationHistory();
                processOperationHistoryEntity.Content = "【创建】" + "todo name" + "创建了一个流程进程【" + WFProcessInstance.Code + "/" + WFProcessInstance.CustomName + "】";
                #endregion

                #region 委托记录
                //List<WFDelegateRecord> delegateRecordEntitylist = GetDelegateRecordList(WFProcessScheme.SchemeInfoId, WFProcessInstance.Code, WFProcessInstance.CustomName, WFProcessInstance.MakerList);
                //WFProcessInstance.MakerList += delegateUserList;
                #endregion

                wfProcessInstanceService.SaveProcess(WFProcessInstance, WFProcessScheme, processOperationHistoryEntity);

                return true;
            }
            catch
            {
                throw;
            }
        }
        /// <summary>
        /// 创建一个草稿
        /// </summary>
        /// <param name="processId"></param>
        /// <param name="schemeInfoId"></param>
        /// <param name="wfLevel"></param>
        /// <param name="code"></param>
        /// <param name="customName"></param>
        /// <param name="description"></param>
        /// <param name="frmData"></param>
        /// <returns></returns>
        public bool CreateRoughdraft(Guid processId, Guid schemeInfoId, WFProcessInstance WFProcessInstance, string frmData = null)
        {
            try
            {
                WFSchemeInfo WFSchemeInfo = _unitWork.FindSingle<WFSchemeInfo>(u =>u.Id ==schemeInfoId);
                WFSchemeContent WFSchemeContent = _unitWork.FindSingle<WFSchemeContent>(u =>u.SchemeInfoId ==schemeInfoId 
                && u.SchemeVersion ==WFSchemeInfo.SchemeVersion);
                
                WFProcessInstance.ActivityId = "";
                WFProcessInstance.ActivityName = "";
                WFProcessInstance.ActivityType = 0;//开始节点
                WFProcessInstance.IsFinish = 0;
                WFProcessInstance.SchemeType = WFSchemeInfo.SchemeType;
                WFProcessInstance.EnabledMark = 3;//草稿
                WFProcessInstance.CreateDate = DateTime.Now;
                WFProcessInstance.FrmType = WFSchemeInfo.FrmType;

                WFProcessScheme WFProcessScheme = new WFProcessScheme();
                WFProcessScheme.SchemeInfoId = schemeInfoId;
                WFProcessScheme.SchemeVersion = WFSchemeInfo.SchemeVersion;
                WFProcessScheme.ProcessType = WFProcessInstance.EnabledMark;
                var data = new
                {
                    SchemeContent = WFSchemeContent.SchemeContent,
                    frmData = frmData
                };
                WFProcessScheme.SchemeContent = data.ToJson();

                wfProcessInstanceService.SaveProcess(processId,WFProcessInstance, WFProcessScheme);

                return true;
            }
            catch
            {
                throw;
            }
        }
        /// <summary>
        /// 创建一个草稿
        /// </summary>
        /// <param name="WFProcessInstance"></param>
        /// <param name="frmData"></param>
        /// <returns></returns>
        public bool EditionRoughdraft(WFProcessInstance WFProcessInstance, string frmData = null)
        {
            try
            {
                WFProcessScheme WFProcessScheme = _unitWork.FindSingle<WFProcessScheme>(u =>u.Id ==WFProcessInstance.ProcessSchemeId);
                dynamic schemeContentJson = WFProcessScheme.SchemeContent.ToJson();//获取工作流模板内容的json对象;
                var data = new
                {
                    SchemeContent = schemeContentJson.SchemeContent.Value,
                    frmData = frmData
                };
                WFProcessScheme.SchemeContent = data.ToJson().ToString();
                WFProcessInstance.IsFinish = 0;
                WFProcessInstance.CreateDate = DateTime.Now;
                wfProcessInstanceService.SaveProcess(WFProcessInstance.Id,WFProcessInstance, WFProcessScheme);
                return true;
            }
            catch
            {
                throw;
            }
        }
        /// <summary>
        /// 节点审核
        /// </summary>
        /// <param name="processId"></param>
        /// <returns></returns>
        public bool NodeVerification(Guid processId, bool flag, string description = "")
        {
            bool _res = false;
            try
            {
                string _sqlstr="", _dbbaseId="";
                WFProcessInstance WFProcessInstance = wfProcessInstanceService.GetEntity(processId);
                WFProcessScheme WFProcessScheme = _unitWork.FindSingle<WFProcessScheme>(u =>u.Id ==WFProcessInstance.ProcessSchemeId);
                WFProcessOperationHistory WFProcessOperationHistory = new WFProcessOperationHistory();//操作记录
                WFProcessTransitionHistory processTransitionHistoryEntity = null;//流转记录

                dynamic schemeContentJson = WFProcessScheme.SchemeContent.ToJson();//获取工作流模板内容的json对象;
                WF_RuntimeInitModel wfRuntimeInitModel = new WF_RuntimeInitModel()
                {
                    schemeContent = schemeContentJson.SchemeContent.Value,
                    currentNodeId = WFProcessInstance.ActivityId,
                    frmData = schemeContentJson.frmData.Value,
                    previousId = WFProcessInstance.PreviousId,
                    processId = processId
                };
                IWF_Runtime wfruntime = new WF_Runtime(wfRuntimeInitModel);


                #region 会签
                if (WFProcessInstance.ActivityType == 0)//会签
                {
                    wfruntime.MakeTagNode(wfruntime.runtimeModel.currentNodeId, 1,"");//标记当前节点通过
                    ///寻找需要审核的节点Id
                    string _VerificationNodeId = "";
                    List<string> _nodelist = wfruntime.GetCountersigningNodeIdList(wfruntime.runtimeModel.currentNodeId);
                    string _makerList = "";
                    foreach (string item in _nodelist)
                    {
                        _makerList = GetMakerList(wfruntime.runtimeModel.nodeDictionary[item], wfruntime.runtimeModel.processId);
                        if (_makerList != "-1")
                        {
                            foreach (string one in _makerList.Split(','))
                            {
                                if (AuthUtil.GetUserName() == one || AuthUtil.GetUserName().IndexOf(one) != -1)
                                {
                                    _VerificationNodeId = item;
                                    break;
                                }
                            }
                        }
                    }

                    if (_VerificationNodeId != "")
                    {
                        if (flag)
                        {
                            WFProcessOperationHistory.Content = "【" + "todo name" + "】【" + wfruntime.runtimeModel.nodeDictionary[_VerificationNodeId].name + "】【" + DateTime.Now.ToString("yyyy-MM-dd HH:mm") + "】同意,备注：" + description;
                        }
                        else
                        {
                            WFProcessOperationHistory.Content = "【" + "todo name" + "】【" + wfruntime.runtimeModel.nodeDictionary[_VerificationNodeId].name + "】【" + DateTime.Now.ToString("yyyy-MM-dd HH:mm") + "】不同意,备注：" + description;
                        }

                        string _Confluenceres = wfruntime.NodeConfluence(_VerificationNodeId, flag, AuthUtil.GetUserName(), description);
                        var _data = new {
                            SchemeContent = wfruntime.runtimeModel.schemeContentJson.ToString(),
                            frmData = (WFProcessInstance.FrmType == 0?wfruntime.runtimeModel.frmData:null)
                        };
                        WFProcessScheme.SchemeContent = _data.ToJson().ToString();
                        switch (_Confluenceres)
                        {
                            case "-1"://不通过
                                WFProcessInstance.IsFinish = 3;
                                break;
                            case "1"://等待
                                break;
                            default://通过
                                WFProcessInstance.PreviousId = WFProcessInstance.ActivityId;
                                WFProcessInstance.ActivityId = wfruntime.runtimeModel.nextNodeId;
                                WFProcessInstance.ActivityType = wfruntime.runtimeModel.nextNodeType;//-1无法运行,0会签开始,1会签结束,2一般节点,4流程运行结束
                                WFProcessInstance.ActivityName = wfruntime.runtimeModel.nextNode.name;
                                WFProcessInstance.IsFinish = (wfruntime.runtimeModel.nextNodeType == 4 ? 1 : 0);
                                WFProcessInstance.MakerList = (wfruntime.runtimeModel.nextNodeType == 4 ? GetMakerList(wfruntime) : "");//当前节点可执行的人信息
                               
                                #region 流转记录
                                processTransitionHistoryEntity = new WFProcessTransitionHistory();
                                processTransitionHistoryEntity.FromNodeId = wfruntime.runtimeModel.currentNodeId;
                                processTransitionHistoryEntity.FromNodeName = wfruntime.runtimeModel.currentNode.name.Value;
                                processTransitionHistoryEntity.FromNodeType = wfruntime.runtimeModel.currentNodeType;
                                processTransitionHistoryEntity.ToNodeId = wfruntime.runtimeModel.nextNodeId;
                                processTransitionHistoryEntity.ToNodeName = wfruntime.runtimeModel.nextNode.name.Value;
                                processTransitionHistoryEntity.ToNodeType = wfruntime.runtimeModel.nextNodeType;
                                processTransitionHistoryEntity.TransitionSate = 0;
                                processTransitionHistoryEntity.IsFinish = (processTransitionHistoryEntity.ToNodeType == 4 ? 1 : 0);
                                #endregion

                               

                                if (wfruntime.runtimeModel.currentNode.setInfo != null && wfruntime.runtimeModel.currentNode.setInfo.NodeSQL != null)
                                {
                                    _sqlstr = wfruntime.runtimeModel.currentNode.setInfo.NodeSQL.Value;
                                    _dbbaseId = wfruntime.runtimeModel.currentNode.setInfo.NodeDataBaseToSQL.Value;
                                }
                                break;
                        }
                    }
                    else
                    {
                        throw(new Exception("审核异常,找不到审核节点"));
                    }
                }
                #endregion

                #region 一般审核
                else//一般审核
                {
                    if (flag)
                    {
                        wfruntime.MakeTagNode(wfruntime.runtimeModel.currentNodeId, 1, AuthUtil.GetUserName(), description);
                        WFProcessInstance.PreviousId = WFProcessInstance.ActivityId;
                        WFProcessInstance.ActivityId = wfruntime.runtimeModel.nextNodeId;
                        WFProcessInstance.ActivityType = wfruntime.runtimeModel.nextNodeType;//-1无法运行,0会签开始,1会签结束,2一般节点,4流程运行结束
                        WFProcessInstance.ActivityName = wfruntime.runtimeModel.nextNode.name;
                        WFProcessInstance.MakerList = (wfruntime.runtimeModel.nextNodeType == 4 ? GetMakerList(wfruntime) : "");//当前节点可执行的人信息
                        WFProcessInstance.IsFinish = (wfruntime.runtimeModel.nextNodeType == 4 ? 1 : 0);
                        #region 流转记录
                        processTransitionHistoryEntity = new WFProcessTransitionHistory();
                        processTransitionHistoryEntity.FromNodeId = wfruntime.runtimeModel.currentNodeId;
                        processTransitionHistoryEntity.FromNodeName = wfruntime.runtimeModel.currentNode.name.Value;
                        processTransitionHistoryEntity.FromNodeType = wfruntime.runtimeModel.currentNodeType;
                        processTransitionHistoryEntity.ToNodeId = wfruntime.runtimeModel.nextNodeId;
                        processTransitionHistoryEntity.ToNodeName = wfruntime.runtimeModel.nextNode.name.Value;
                        processTransitionHistoryEntity.ToNodeType = wfruntime.runtimeModel.nextNodeType;
                        processTransitionHistoryEntity.TransitionSate = 0;
                        processTransitionHistoryEntity.IsFinish = (processTransitionHistoryEntity.ToNodeType == 4 ? 1 : 0);
                        #endregion

                     

                        if (wfruntime.runtimeModel.currentNode.setInfo != null && wfruntime.runtimeModel.currentNode.setInfo.NodeSQL != null)
                        {
                            _sqlstr = wfruntime.runtimeModel.currentNode.setInfo.NodeSQL.Value;
                            _dbbaseId = wfruntime.runtimeModel.currentNode.setInfo.NodeDataBaseToSQL.Value;
                        }
                      
                        WFProcessOperationHistory.Content = "【" + "todo name" + "】【" + wfruntime.runtimeModel.currentNode.name + "】【" + DateTime.Now.ToString("yyyy-MM-dd HH:mm") + "】同意,备注：" + description;
                    }
                    else
                    {
                        WFProcessInstance.IsFinish = 3; //表示该节点不同意
                        wfruntime.MakeTagNode(wfruntime.runtimeModel.currentNodeId, -1, AuthUtil.GetUserName(), description);

                        WFProcessOperationHistory.Content = "【" + "todo name" + "】【" + wfruntime.runtimeModel.currentNode.name + "】【" + DateTime.Now.ToString("yyyy-MM-dd HH:mm") + "】不同意,备注：" + description;
                    }
                    var data = new
                    {
                        SchemeContent = wfruntime.runtimeModel.schemeContentJson.ToString(),
                        frmData = (WFProcessInstance.FrmType == 0 ? wfruntime.runtimeModel.frmData : null)
                    };
                    WFProcessScheme.SchemeContent = data.ToJson().ToString();
                }
                #endregion 

                _res = true;
                wfProcessInstanceService.SaveProcess(_sqlstr, _dbbaseId,WFProcessInstance, WFProcessScheme, WFProcessOperationHistory, processTransitionHistoryEntity);
                return _res;
            }
            catch {
                throw;
            }
        }
        /// <summary>
        /// 驳回
        /// </summary>
        /// <param name="processId"></param>
        /// <param name="nodeId"></param>
        /// <param name="description"></param>
        /// <returns></returns>
        public bool NodeReject(Guid processId,string nodeId, string description = "")
        {
            try
            {
                WFProcessInstance WFProcessInstance = wfProcessInstanceService.GetEntity(processId);
                WFProcessScheme WFProcessScheme = _unitWork.FindSingle<WFProcessScheme>(u =>u.Id ==WFProcessInstance.ProcessSchemeId);
                WFProcessOperationHistory WFProcessOperationHistory = new WFProcessOperationHistory();
                WFProcessTransitionHistory processTransitionHistoryEntity = null;
                dynamic schemeContentJson = WFProcessScheme.SchemeContent.ToJson();//获取工作流模板内容的json对象;
                WF_RuntimeInitModel wfRuntimeInitModel = new WF_RuntimeInitModel()
                {
                    schemeContent = schemeContentJson.SchemeContent.Value,
                    currentNodeId = WFProcessInstance.ActivityId,
                    frmData = schemeContentJson.frmData.Value,
                    previousId = WFProcessInstance.PreviousId,
                    processId = processId
                };
                IWF_Runtime wfruntime = new WF_Runtime(wfRuntimeInitModel);
             

                string resnode = "";
                if (nodeId == "")
                {
                    resnode = wfruntime.RejectNode();
                }
                else
                {
                    resnode = nodeId;
                }
                wfruntime.MakeTagNode(wfruntime.runtimeModel.currentNodeId, 0, AuthUtil.GetUserName(), description);
                WFProcessInstance.IsFinish = 4;//4表示驳回（需要申请者重新提交表单）
                if (resnode != "")
                {
                    WFProcessInstance.PreviousId = WFProcessInstance.ActivityId;
                    WFProcessInstance.ActivityId = resnode;
                    WFProcessInstance.ActivityType = wfruntime.GetNodeStatus(resnode);//-1无法运行,0会签开始,1会签结束,2一般节点,4流程运行结束
                    WFProcessInstance.ActivityName = wfruntime.runtimeModel.nodeDictionary[resnode].name;
                    WFProcessInstance.MakerList = GetMakerList(wfruntime.runtimeModel.nodeDictionary[resnode], WFProcessInstance.PreviousId);//当前节点可执行的人信息
                    #region 流转记录
                    processTransitionHistoryEntity = new WFProcessTransitionHistory();
                    processTransitionHistoryEntity.FromNodeId = wfruntime.runtimeModel.currentNodeId;
                    processTransitionHistoryEntity.FromNodeName = wfruntime.runtimeModel.currentNode.name.Value;
                    processTransitionHistoryEntity.FromNodeType = wfruntime.runtimeModel.currentNodeType;
                    processTransitionHistoryEntity.ToNodeId = wfruntime.runtimeModel.nextNodeId;
                    processTransitionHistoryEntity.ToNodeName = wfruntime.runtimeModel.nextNode.name.Value;
                    processTransitionHistoryEntity.ToNodeType = wfruntime.runtimeModel.nextNodeType;
                    processTransitionHistoryEntity.TransitionSate = 1;//
                    processTransitionHistoryEntity.IsFinish = (processTransitionHistoryEntity.ToNodeType == 4 ? 1 : 0);
                    #endregion
                }
                var data = new
                {
                    SchemeContent = wfruntime.runtimeModel.schemeContentJson.ToString(),
                    frmData = (WFProcessInstance.FrmType == 0 ? wfruntime.runtimeModel.frmData : null)
                };
                WFProcessScheme.SchemeContent = data.ToJson().ToString();
                WFProcessOperationHistory.Content = "【" + "todo name" + "】【" + wfruntime.runtimeModel.currentNode.name + "】【" + DateTime.Now.ToString("yyyy-MM-dd HH:mm") + "】驳回,备注：" + description;

                wfProcessInstanceService.SaveProcess(WFProcessInstance, WFProcessScheme, WFProcessOperationHistory, processTransitionHistoryEntity);
                return true;
            }
            catch
            {
                throw;
            }
        }
        /// <summary>
        /// 召回流程进程
        /// </summary>
        /// <param name="processId"></param>
        public void CallingBackProcess(Guid processId)
        {
            try
            {
                wfProcessInstanceService.OperateVirtualProcess(processId,2);
            }
            catch
            {
                throw;
            }
        }
        /// <summary>
        /// 终止一个实例(彻底删除)
        /// </summary>
        /// <param name="processId"></param>
        /// <returns></returns>
        public void KillProcess(Guid processId)
        {
            try
            {
                _unitWork.Delete<WFProcessInstance>(u =>u.Id == processId);
            }
            catch
            {
                throw;
            }
        }
        /// <summary>
        /// 获取某个节点（审核人所能看到的提交表单的权限）
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public string GetProcessSchemeContentByNodeId(string data, string nodeId)
        {
            try
            {
                List<dynamic> list = new List<dynamic>();
                dynamic schemeContentJson = data.ToJson();//获取工作流模板内容的json对象;
                string schemeContent1 = schemeContentJson.SchemeContent.Value;
                dynamic schemeContentJson1 = schemeContent1.ToJson();
                string FrmContent = schemeContentJson1.Frm.FrmContent.Value;
                dynamic FrmContentJson = FrmContent.ToJson();

                foreach (var item in schemeContentJson1.Flow.nodes)
                {
                    if (item.id.Value == nodeId)
                    {
                        foreach (var item1 in item.setInfo.frmPermissionInfo)
                        {
                            foreach (var item2 in FrmContentJson)
                            {
                                if (item2.control_field.Value == item1.fieldid.Value)
                                {
                                    if (item1.look.Value == true)
                                    {
                                        if (item1.down != null)
                                        {
                                            item2.down = item1.down.Value;
                                        }
                                        list.Add(item2);
                                    } 
                                    break;
                                }
                            }
                        }
                        break;
                    }
                }
                schemeContentJson1.Frm.FrmContent = list.ToJson().ToString();
                schemeContentJson.SchemeContent = schemeContentJson1.ToString();
                return schemeContentJson.ToString();
            }
            catch
            {
                throw;
            }
        }
        /// <summary>
        /// 获取某个节点（审核人所能看到的提交表单的权限）
        /// </summary>
        /// <param name="data"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public string GetProcessSchemeContentByUserId(string data, string userId)
        {
            try
            {
                List<dynamic> list = new List<dynamic>();
                dynamic schemeContentJson = data.ToJson();//获取工作流模板内容的json对象;
                string schemeContent1 = schemeContentJson.SchemeContent.Value;
                dynamic schemeContentJson1 = schemeContent1.ToJson();
                string FrmContent = schemeContentJson1.Frm.FrmContent.Value;
                dynamic FrmContentJson = FrmContent.ToJson();

                foreach (var item in schemeContentJson1.Flow.nodes)
                {
                    if (item.setInfo != null && item.setInfo.UserId != null && item.setInfo.UserId.Value == userId)
                    {
                        foreach (var item1 in item.setInfo.frmPermissionInfo)
                        {
                            foreach (var item2 in FrmContentJson)
                            {
                                if (item2.control_field.Value == item1.fieldid.Value)
                                {
                                    if (item1.look.Value == true)
                                    {
                                        if (item1.down != null)
                                        {
                                            item2.down = item1.down.Value;
                                        }
                                        list.Add(item2);
                                    }
                                    break;
                                }
                            }
                        }
                        break;
                    }
                }
                schemeContentJson1.Frm.FrmContent = list.ToJson().ToString();
                schemeContentJson.SchemeContent = schemeContentJson1.ToString();
                return schemeContentJson.ToString();
            }
            catch
            {
                throw;
            }
        }
        #endregion

        /// <summary>
        /// 寻找该节点执行人
        /// </summary>
        /// <param name="wfruntime"></param>
        /// <returns></returns>
        private string GetMakerList(IWF_Runtime wfruntime)
        {
            try
            {
                string makerList = "";
                if (wfruntime.runtimeModel.nextNodeId == "-1")
                {
                    throw (new Exception("无法寻找到下一个节点"));
                }
                if (wfruntime.runtimeModel.nextNodeType == 0)//如果是会签节点
                {
                    List<string> _nodelist = wfruntime.GetCountersigningNodeIdList(wfruntime.runtimeModel.nextNodeId);
                    string _makerList = "";
                    foreach (string item in _nodelist)
                    {
                        _makerList = GetMakerList(wfruntime.runtimeModel.nodeDictionary[item], wfruntime.runtimeModel.processId);
                        if (_makerList == "-1")
                        {
                            throw (new Exception("无法寻找到会签节点的审核者,请查看流程设计是否有问题!"));
                        }
                        else if (_makerList == "1")
                        {
                            throw (new Exception("会签节点的审核者不能为所有人,请查看流程设计是否有问题!"));
                        }
                        else
                        {
                            if (makerList != "")
                            {
                                makerList += ",";
                            }
                            makerList += _makerList;
                        }
                    }
                }
                else
                {
                    makerList = GetMakerList(wfruntime.runtimeModel.nextNode, wfruntime.runtimeModel.processId);
                    if (makerList == "-1")
                    {
                        throw (new Exception("无法寻找到节点的审核者,请查看流程设计是否有问题!"));
                    }
                }

                return makerList;
            }
            catch
            {
                throw;
            }
        }
        /// <summary>
        /// 寻找该节点执行人
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private string GetMakerList(dynamic node, string processId)
        {
            try
            {
                string makerlsit = "";

                if (node.setInfo == null)
                {
                    makerlsit = "-1";
                }
                else
                {
                    if (node.setInfo.NodeDesignate.Value == "NodeDesignateType1")//所有成员
                    {
                        makerlsit = "1";
                    }
                    else if (node.setInfo.NodeDesignate.Value == "NodeDesignateType2")//指定成员
                    {
                        makerlsit = ArrwyToString(node.setInfo.NodeDesignateData.role, makerlsit);
                        makerlsit = ArrwyToString(node.setInfo.NodeDesignateData.post, makerlsit);
                        makerlsit = ArrwyToString(node.setInfo.NodeDesignateData.usergroup, makerlsit);
                        makerlsit = ArrwyToString(node.setInfo.NodeDesignateData.user, makerlsit);

                        if (makerlsit == "")
                        {
                            makerlsit = "-1";
                        }
                    }
                    //else if (node.setInfo.NodeDesignate.Value == "NodeDesignateType3")//发起者领导
                    //{
                    //    UserEntity userEntity = userService.GetEntity(OperatorProvider.Provider.Current().UserId);
                    //    if (string.IsNullOrEmpty(userEntity.ManagerId))
                    //    {
                    //        makerlsit = "-1";
                    //    }
                    //    else
                    //    {
                    //        makerlsit = userEntity.ManagerId;
                    //    }
                    //}
                    //else if (node.setInfo.NodeDesignate.Value == "NodeDesignateType4")//前一步骤领导
                    //{
                    //    WFProcessTransitionHistory transitionHistoryEntity = wfProcessTransitionHistoryService.GetEntity(processId, node.id.Value);
                    //    UserEntity userEntity = userService.GetEntity(transitionHistoryEntity.CreateUserId);
                    //    if (string.IsNullOrEmpty(userEntity.ManagerId))
                    //    {
                    //        makerlsit = "-1";
                    //    }
                    //    else
                    //    {
                    //        makerlsit = userEntity.ManagerId;
                    //    }
                    //}
                    //else if (node.setInfo.NodeDesignate.Value == "NodeDesignateType5")//发起者部门领导
                    //{
                    //    UserEntity userEntity = userService.GetEntity(OperatorProvider.Provider.Current().UserId);
                    //    DepartmentEntity departmentEntity = departmentService.GetEntity(userEntity.DepartmentId);

                    //    if (string.IsNullOrEmpty(departmentEntity.ManagerId))
                    //    {
                    //        makerlsit = "-1";
                    //    }
                    //    else
                    //    {
                    //        makerlsit = departmentEntity.ManagerId;
                    //    }
                    //}
                    //else if (node.setInfo.NodeDesignate.Value == "NodeDesignateType6")//发起者公司领导
                    //{
                    //    UserEntity userEntity = userService.GetEntity(OperatorProvider.Provider.Current().UserId);
                    //    OrganizeEntity organizeEntity = organizeService.GetEntity(userEntity.OrganizeId);

                    //    if (string.IsNullOrEmpty(organizeEntity.ManagerId))
                    //    {
                    //        makerlsit = "-1";
                    //    }
                    //    else
                    //    {
                    //        makerlsit = organizeEntity.ManagerId;
                    //    }
                    //}
                }
                return makerlsit;
            }
            catch
            {
                throw;
            }
        }
        /// <summary>
        /// 将数组转化成逗号相隔的字串
        /// </summary>
        /// <param name="data"></param>
        /// <param name="Str"></param>
        /// <returns></returns>
        private string ArrwyToString(dynamic data, string Str)
        {
            string resStr = Str;
            foreach (var item in data)
            {
                if (resStr != "")
                {
                    resStr += ",";
                }
                resStr += item.Value;
            }
            return resStr;
        }

        public WFProcessScheme GetProcessSchemeEntity(Guid keyValue)
        {
            return _unitWork.FindSingle<WFProcessScheme>(u => u.Id == keyValue);
        }

        /// <summary>
        /// 已办流程进度查看，根据当前访问人的权限查看表单内容
        /// <para>李玉宝于2017-01-20 15:35:13</para>
        /// </summary>
        /// <param name="keyValue">The key value.</param>
        /// <returns>WFProcessScheme.</returns>
        public WFProcessScheme GetProcessSchemeByUserId(Guid keyValue)
        {
            var entity = GetProcessSchemeEntity(keyValue);
            entity.SchemeContent = GetProcessSchemeContentByUserId(entity.SchemeContent, AuthUtil.GetCurrentUser().User.Id.ToString());
            return entity;
        }


        /// <summary>
        /// 已办流程进度查看，根据当前节点的权限查看表单内容
        /// <para>李玉宝于2017-01-20 15:34:35</para>
        /// </summary>
        /// <param name="keyValue">The key value.</param>
        /// <param name="nodeId">The node identifier.</param>
        /// <returns>WFProcessScheme.</returns>
        public WFProcessScheme GetProcessSchemeEntityByNodeId(Guid keyValue, string nodeId)
        {
            var entity = GetProcessSchemeEntity(keyValue);
            entity.SchemeContent = GetProcessSchemeContentByNodeId(entity.SchemeContent, nodeId);
            return entity;
        }

        public WFProcessInstance GetProcessInstanceEntity(Guid keyValue)
        {
            return _unitWork.FindSingle<WFProcessInstance>(u => u.Id == keyValue);
        }

        public void DeleteProcess(Guid keyValue)
        {
            var entity = _unitWork.FindSingle<WFProcessInstance>(u => u.Id == keyValue);
            _unitWork.Delete<WFProcessScheme>(u =>u.Id == entity.ProcessSchemeId);
            _unitWork.Delete<WFProcessInstance>(u =>u.Id == keyValue);
           
        }

        /// <summary>
        /// 审核流程
        /// <para>李玉宝于2017-01-20 15:44:45</para>
        /// </summary>
        /// <param name="processId">The process identifier.</param>
        /// <param name="verificationData">The verification data.</param>
        public void VerificationProcess(Guid processId, string verificationData)
        {
            try
            {
                dynamic verificationDataJson = verificationData.ToJson();

                //驳回
                if (verificationDataJson.VerificationFinally.Value == "3")
                {
                    string _nodeId = "";
                    if (verificationDataJson.NodeRejectStep != null)
                    {
                        _nodeId = verificationDataJson.NodeRejectStep.Value;
                    }
                    NodeReject(processId, _nodeId, verificationDataJson.VerificationOpinion.Value);
                }
                else if (verificationDataJson.VerificationFinally.Value == "2")//表示不同意
                {
                    NodeVerification(processId, false, verificationDataJson.VerificationOpinion.Value);
                }
                else if (verificationDataJson.VerificationFinally.Value == "1")//表示同意
                {
                    NodeVerification(processId, true, verificationDataJson.VerificationOpinion.Value);
                }
            }
            catch
            {
                throw;
            }
        }
    }
}
