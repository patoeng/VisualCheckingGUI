﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Configuration;
using System.Windows.Forms;
using Camstar.WCF.ObjectStack;
using ComponentFactory.Krypton.Navigator;
using ComponentFactory.Krypton.Toolkit;
using MesData;
using MesData.Login;
using MesData.Ppa;
using MesData.Repair;
using Newtonsoft.Json;
using OpcenterWikLibrary;
using VisualCheckingGUI.Enumeration;
using VisualCheckingGUI.Model;

namespace VisualCheckingGUI
{
    public partial class Main : KryptonForm
    {
        #region CONSTRUCTOR
        public Main()
        {
            InitializeComponent();
#if MiniMe
            var  name = "Visual Checking Minime";
            Text = Mes.AddVersionNumber(name);
#elif Ariel
            var name = "Visual Checking Ariel";
            Text = Mes.AddVersionNumber(name);
#endif
            _mesData = new Mes(name, AppSettings.Resource,name);

            lbTitle.Text =AppSettings.Resource;

            kryptonNavigator1.SelectedIndex = 0;

            EventLogUtil.LogEvent("Application Start");

            var maintStrings = new[] { "Resource", "MaintenanceType", "MaintenanceReq", "NextDateDue", "NextThruputQtyDue", "MaintenanceState" };
            for (int i = 0; i < Dg_Maintenance.Columns.Count; i++)
            {
                if (!maintStrings.Contains(Dg_Maintenance.Columns[i].DataPropertyName))
                {
                    Dg_Maintenance.Columns[i].Visible = false;
                }
                else
                {
                    switch (Dg_Maintenance.Columns[i].HeaderText)
                    {

                        case "MaintenanceType":
                            Dg_Maintenance.Columns[i].HeaderText = @"Maintenance Type";
                            break;
                        case "MaintenanceReq":
                            Dg_Maintenance.Columns[i].HeaderText = @"Maintenance Requirement";
                            break;
                        case "NextDateDue":
                            Dg_Maintenance.Columns[i].HeaderText = @"Next Due Date";
                            break;
                        case "NextThruputQtyDue":
                            Dg_Maintenance.Columns[i].HeaderText = @"Next Thruput Quantity Due";
                            break;
                        case "MaintenanceState":
                            Dg_Maintenance.Columns[i].HeaderText = @"Maintenance State";
                            _indexMaintenanceState = Dg_Maintenance.Columns[i].Index;
                            break;
                    }

                }
            }
        }

        public sealed override string Text
        {
            get => base.Text;
            set => base.Text = value;
        }

        #endregion

#region INSTANCE VARIABLE
      
        private VisualCheckingState _visualCheckingState;
        private readonly Mes _mesData;
        private DateTime _dMoveIn;
        private DateTime _dMoveOut;
        private string _containerResult = ResultString.False;
        private VcNgReason _vcNgReason;
        private string _wrongOperationPosition;
        #endregion

        #region FUNCTION USEFULL

        private async Task SetVisualCheckingState(VisualCheckingState visualCheckingState)
        {
            _visualCheckingState = visualCheckingState;
            switch (_visualCheckingState)
            {
                case VisualCheckingState.PlaceUnit:
                    Tb_Scanner.Enabled = false;
                    lblCommand.ForeColor = Color.Red;
                    lblCommand.Text = @"Resource is not in ""Up"" condition!";
                    break;
                case VisualCheckingState.ScanUnitSerialNumber:

                    btnFail.Visible = false;
                    btnPass.Visible = false;
                    panelPassFail.Visible = false;
                    btnSubmit.Visible = false;
                    _containerResult = "";
                    Tb_SerialNumber.Clear();

                    if (_mesData.ResourceStatusDetails == null || _mesData.ResourceStatusDetails?.Availability != "Up")
                    {
                        await SetVisualCheckingState(VisualCheckingState.PlaceUnit);
                        break;
                    }
                    // check if fail by maintenance Past Due
                    var transPastDue = Mes.GetMaintenancePastDue(_mesData.MaintenanceStatusDetails);
                    if (transPastDue.Result)
                    {
                        KryptonMessageBox.Show(this, "This resource under maintenance, need to complete!", "Move In",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        break;
                    }
                    Tb_Scanner.Enabled = true;
                    lblCommand.ForeColor = Color.LimeGreen;
                    lblCommand.Text = @"Scan Unit Serial Number!";
                    ActiveControl = Tb_Scanner;
                    _readScanner = true;
                    break;
                case VisualCheckingState.CheckUnitStatus:
                    lblCommand.Text = @"Checking Unit Status";
                    if (_mesData.ResourceStatusDetails == null || _mesData.ResourceStatusDetails?.Availability != "Up")
                    {
                        await SetVisualCheckingState(VisualCheckingState.PlaceUnit);
                        break;
                    }
                    // check if fail by maintenance Past Due
                    transPastDue = Mes.GetMaintenancePastDue(_mesData.MaintenanceStatusDetails);
                    if (transPastDue.Result)
                    {
                        KryptonMessageBox.Show(this, "This resource under maintenance, need to complete!", "Move In",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        break;
                    }
                    Tb_Scanner.Enabled = false;
                    lblCommand.Text = @"Checking Unit Status";
                    _dMoveIn = DateTime.Now;
                    lbMoveIn.Text = _dMoveIn.ToString(Mes.DateTimeStringFormat);
                    lbMoveOut.Text = "";
                    var oContainerStatus = await Mes.GetContainerStatusDetails(_mesData, Tb_SerialNumber.Text, _mesData.DataCollectionName);
                    if (oContainerStatus != null)
                    {
                        if (oContainerStatus.Operation != null)
                        {
                            if (oContainerStatus.Operation.Name != _mesData.OperationName)
                            {
                                _wrongOperationPosition = oContainerStatus.Operation.Name;
                                await SetVisualCheckingState(VisualCheckingState.WrongPosition);
                                break;
                            }
                            if (oContainerStatus.MfgOrderName != null && _mesData.ManufacturingOrder == null)
                            {
                                var mfg = await Mes.GetMfgOrder(_mesData, oContainerStatus.MfgOrderName.ToString());
                                _mesData.SetManufacturingOrder(mfg);
                                Tb_PO.Text = oContainerStatus.MfgOrderName.ToString();
                                Tb_Product.Text = oContainerStatus.Product.Name;
                                Tb_ProductDesc.Text = oContainerStatus.ProductDescription.Value;
                                var img = await Mes.GetImage(_mesData, oContainerStatus.Product.Name);
                                pictureBox1.ImageLocation = img.Identifier.Value;

                                var cnt = await Mes.GetCounterFromMfgOrder(_mesData);
                                Tb_VisualQty.Text = cnt.ToString();
                            }

                        }

                        var vcAttempt = oContainerStatus.Attributes?.Where(x => x.Name == "VcAttempt").ToList();
                        if (vcAttempt != null && vcAttempt.Count > 0)
                        {
                            _vcAttempt = Convert.ToInt32(vcAttempt[0].AttributeValue.ToString());
                        }
                        else
                        {
                            _vcAttempt = 0;
                        }

                        await SetVisualCheckingState(VisualCheckingState.VisualCheckResult);
                        break;
                    }
                    else
                    {
                        var position = await Mes.GetCurrentContainerStep(_mesData, Tb_SerialNumber.Text);
                        if (position != _mesData.OperationName)
                        {
                            _wrongOperationPosition = position;
                            await SetVisualCheckingState(VisualCheckingState.WrongPosition);
                            break;
                        }
                    }
                    await SetVisualCheckingState(VisualCheckingState.UnitNotFound);
                    break;
                case VisualCheckingState.VisualCheckResult:
                    ResetNgReason();
                    btnFail.Visible = true;
                    btnPass.Visible = true;
                    panelPassFail.Visible = true;
                    panelReason.Visible = false;
                    Tb_Scanner.Enabled = false;
                    lblCommand.Text = @"Visual Checking Result?";
                    panelPassFail.Visible = true;
                    break;
                case VisualCheckingState.UpdateMoveInMove:
                    Tb_Scanner.Enabled = false;
                    lblCommand.Text = @"Container Move In"; 

                    var cDataPoint = new DataPointDetails[1];
                    cDataPoint[0] = new DataPointDetails()
                    { DataName = "RESULT", DataValue = _containerResult, DataType = DataTypeEnum.String };
                    oContainerStatus = await Mes.GetContainerStatusDetails(_mesData, Tb_SerialNumber.Text, _mesData.DataCollectionName);
                    if (oContainerStatus != null)
                    {
                        var resultMoveIn = await Mes.ExecuteMoveIn(_mesData, oContainerStatus.ContainerName.Value,
                            _dMoveIn);
                        if (resultMoveIn.Result)
                        {
                            lblCommand.Text = @"Container Move Standard";
                            _dMoveOut = DateTime.Now;
                            lbMoveOut.Text = _dMoveOut.ToString(Mes.DateTimeStringFormat);
                            var resultMoveStd = await Mes.ExecuteMoveStandard(_mesData, oContainerStatus.ContainerName.Value, _dMoveOut, cDataPoint);
                            if (resultMoveStd.Result)
                            {
                                _vcAttempt += 1;
                                if (_containerResult == ResultString.False)
                                {
                                    var reason = GetReasonAttribute();
                                    await Mes.ExecuteContainerAttrMaint(_mesData, oContainerStatus, reason.ToArray());
                                }
                                else
                                {
                                    var reason2 = new List<ContainerAttrDetail>
                                    {
                                        new ContainerAttrDetail { Name = "VcAttempt", DataType = TrivialTypeEnum.Integer, AttributeValue = _vcAttempt.ToString(), IsExpression = false }
                                    };
                                    await Mes.ExecuteContainerAttrMaint(_mesData, oContainerStatus, reason2.ToArray());
                                } 
                                
                                //Update Counter

                                await Mes.UpdateCounter(_mesData, 1);
                                    var mfg = await Mes.GetMfgOrder(_mesData,
                                        _mesData.ManufacturingOrder.Name.Value);
                                    _mesData.SetManufacturingOrder(mfg);
                                    var count = await Mes.GetCounterFromMfgOrder(_mesData);
                                    Tb_VisualQty.Text = count.ToString();
                            }
                            await SetVisualCheckingState(resultMoveStd.Result
                                ? VisualCheckingState.ScanUnitSerialNumber
                                : VisualCheckingState.MoveInOkMoveFail);
                            break;
                        }
                        // check if fail by maintenance Past Due
                        transPastDue = Mes.GetMaintenancePastDue(_mesData.MaintenanceStatusDetails);
                        if (transPastDue.Result)
                        {
                            KryptonMessageBox.Show(this, "This resource under maintenance, need to complete!", "Move In",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                        await SetVisualCheckingState(VisualCheckingState.MoveInFail);
                        break;
                    }
                    await SetVisualCheckingState(VisualCheckingState.UnitNotFound);
                    break;
                case VisualCheckingState.MoveSuccess:
                    break;
                case VisualCheckingState.MoveInOkMoveFail:
                    lblCommand.ForeColor = Color.Red;
                    Tb_Scanner.Enabled = false;
                    lblCommand.Text = @"Move Standard Fail";
                    break;
                case VisualCheckingState.MoveInFail:
                    lblCommand.ForeColor = Color.Red;
                    Tb_Scanner.Enabled = false;
                    lblCommand.Text = @"Move In Fail";
                    break;
                case VisualCheckingState.Done:
                    break;
                case VisualCheckingState.FailReason:
                    Tb_Scanner.Enabled = true;
                    lblCommand.Text = @"Select NG reason!";
                    break;
                case VisualCheckingState.WrongPosition:
                    _readScanner = false;
                    lblCommand.ForeColor = Color.Red;
                    lblCommand.Text = $@"Wrong Operation, Container in {_wrongOperationPosition}";
                    break;
                case VisualCheckingState.WaitPreparation:
                    _readScanner = false;
                    lblCommand.ForeColor = Color.Red;
                    lblCommand.Text = @"Wait Preparation";
                    btnStartPreparation.Enabled = true;
                    break;
                case VisualCheckingState.UnitNotFound:
                    Tb_Scanner.Enabled = false;
                    lblCommand.ForeColor = Color.Red;
                    lblCommand.Text = @"Unit Not Found";
                    break;
            }
        }

        private void ClrContainer()
        {
            Tb_Scanner.Clear();
            Tb_SerialNumber.Clear();
            lbMoveIn.Text = "";
            lbMoveOut.Text = "";
        }

        #endregion

#region FUNCTION STATUS OF RESOURCE

        private async Task GetStatusMaintenanceDetails()
        {
           
                try
                {
                    var maintenanceStatusDetails = await Mes.GetMaintenanceStatusDetails(_mesData);
                    _mesData.SetMaintenanceStatusDetails(maintenanceStatusDetails);
                    if (maintenanceStatusDetails != null)
                    {//get past due, warning, and tolerance
                        var pastDue = maintenanceStatusDetails.Where(x => x.MaintenanceState == "Past Due").ToList();
                        var due = maintenanceStatusDetails.Where(x => x.MaintenanceState == "Due").ToList();
                        var pending = maintenanceStatusDetails.Where(x => x.MaintenanceState == "Pending").ToList();

                        if (pastDue.Count > 0)
                        {
                            lblResMaintMesg.Text = @"Resource Maintenance Past Due";
                            lblResMaintMesg.BackColor = Color.Red;
                            lblResMaintMesg.Visible = true;
                            return;
                        }
                        if (due.Count > 0)
                        {
                            lblResMaintMesg.Text = @"Resource Maintenance Due";
                            lblResMaintMesg.BackColor = Color.Orange;
                            lblResMaintMesg.Visible = true;
                            return;
                        }
                        if (pending.Count > 0)
                        {
                            lblResMaintMesg.Text = @"Resource Maintenance Pending";
                            lblResMaintMesg.BackColor = Color.Yellow;
                            lblResMaintMesg.Visible = true;
                            return;
                        }
                    }
                    lblResMaintMesg.Visible = false;
                    lblResMaintMesg.Text = "";
                    getMaintenanceStatusDetailsBindingSource.Clear();
                }
                catch (Exception ex)
                {
                    ex.Source = AppSettings.AssemblyName == ex.Source ? MethodBase.GetCurrentMethod()?.Name : MethodBase.GetCurrentMethod()?.Name + "." + ex.Source;
                    EventLogUtil.LogErrorEvent(ex.Source, ex);
                }
           
        }
        private async Task GetStatusOfResource()
        {
            try
            {
                var resourceStatus = await Mes.GetResourceStatusDetails(_mesData);
                _mesData.SetResourceStatusDetails(resourceStatus);

                if (resourceStatus != null)
                {
                    if (resourceStatus.Status != null) Tb_StatusCode.Text = resourceStatus.Reason?.Name;
                    if (resourceStatus.Availability != null)
                    {
                        if (resourceStatus.Availability.Value == "Up")
                        {
                            Tb_StatusCode.StateCommon.Content.Color1 = resourceStatus.Reason?.Name == "Quality Inspection" ? Color.Orange : Color.Green;
                        }
                        else if (resourceStatus.Availability.Value == "Down")
                        {
                            Tb_StatusCode.StateCommon.Content.Color1 = Color.Red;
                        }
                    }
                    else
                    {
                        Tb_StatusCode.StateCommon.Content.Color1 = Color.Orange;
                    }

                    if (resourceStatus.TimeAtStatus != null)
                        Tb_TimeAtStatus.Text = $@"{Mes.OaTimeSpanToString(resourceStatus.TimeAtStatus.Value)}";
                }
            }
            catch (Exception ex)
            {
                ex.Source = AppSettings.AssemblyName == ex.Source ? MethodBase.GetCurrentMethod()?.Name : MethodBase.GetCurrentMethod()?.Name + "." + ex.Source;
                EventLogUtil.LogErrorEvent(ex.Source, ex);
            }
        }

        private async Task GetStatusOfResourceDetail()
        {
            try
            {
                var resourceStatus = await Mes.GetResourceStatusDetails(_mesData);
                _mesData.SetResourceStatusDetails(resourceStatus);

                if (resourceStatus != null)
                {
                    if (resourceStatus.Status != null) Cb_StatusCode.Text = resourceStatus.Status.Name;
                    await Task.Delay(1000);
                    if (resourceStatus.Reason != null) Cb_StatusReason.Text = resourceStatus.Reason.Name;
                    if (resourceStatus.Availability != null)
                    {
                        Tb_StatusCodeM.Text = resourceStatus.Availability.Value;
                        if (resourceStatus.Availability.Value == "Up")
                        {
                            Tb_StatusCodeM.StateCommon.Content.Color1 = Color.Green;
                        }
                        else if (resourceStatus.Availability.Value == "Down")
                        {
                            Tb_StatusCodeM.StateCommon.Content.Color1 = Color.Red;
                        }
                    }
                    else
                    {
                        Tb_StatusCodeM.StateCommon.Content.Color1 = Color.Orange;
                    }

                    if (resourceStatus.TimeAtStatus != null)
                        Tb_TimeAtStatus.Text = $@"{Mes.OaTimeSpanToString(resourceStatus.TimeAtStatus.Value)}";
                }
            }
            catch (Exception ex)
            {
                ex.Source = AppSettings.AssemblyName == ex.Source
                    ? MethodBase.GetCurrentMethod()?.Name
                    : MethodBase.GetCurrentMethod()?.Name + "." + ex.Source;
                EventLogUtil.LogErrorEvent(ex.Source, ex);
            }
        }

        private async Task GetResourceStatusCodeList()
        {
            try
            {
                var oStatusCodeList = await Mes.GetListResourceStatusCode(_mesData);
                if (oStatusCodeList != null)
                {
                    Cb_StatusCode.DataSource = oStatusCodeList.Where(x=>x.Name.IndexOf("VC", StringComparison.Ordinal)==0).ToList();
                }
            }
            catch (Exception ex)
            {
                ex.Source = AppSettings.AssemblyName == ex.Source ? MethodBase.GetCurrentMethod()?.Name : MethodBase.GetCurrentMethod()?.Name + "." + ex.Source;
                EventLogUtil.LogErrorEvent(ex.Source, ex);
            }
        }
        #endregion

        #region COMPONENT EVENT

        private async Task InitNgReasonList()
        {
            int index=0;
            _vcNgReason = new VcNgReason();
            var transactionListGroup = await Mes.GetContainerDefectReasonGroup(_mesData, "Visual Checking Fail");
            if (transactionListGroup.Result)
            {
                var lastGroupHeight = 0;
                   var listGroup = (ContDefectReasonGroupChanges) transactionListGroup.Data;
                foreach (var listGroupGroup in listGroup.Groups)
                {
                    var page = new GroupBox();
                    page.Font = new Font("Segoe UI", 14);
                    page.Top = lastGroupHeight + 5;
                    page.Width = tableLayoutPanel4.ClientSize.Width-40;
                    page.Text = listGroupGroup.Name;
                    //page. = true;
                    var transReasonByGroup = await Mes.GetContainerDefectReasonGroup(_mesData, listGroupGroup.Name);
                    if (transReasonByGroup.Result)
                    {
                        var listReasonByGroup = (ContDefectReasonGroupChanges)transReasonByGroup.Data;
                        var left = 5;
                        var top = 30;
                        foreach (var reason in listReasonByGroup.Entries)
                        {
                            index++;
                            if (reason.Name.Contains(" - "))
                            {
                                var split = reason.Name.Split('-');
                                   var grp = split[0].Trim(' ', '\r', '\n');
                               
                                    var cb = new RoundCheckbox();
                                    cb.TabIndex = index;
                                    cb.Font = new Font("Segoe UI", 14);
                                    cb.AutoSize = true;
                                    cb.Text = split[1].Trim(' ', '\r', '\n');
                                    cb.Appearance = Appearance.Button;
                                    cb.FlatStyle = FlatStyle.Flat;
                                    cb.BackColor = Color.FromArgb(0xDE,0xEA,0xF8);
                                    cb.FlatAppearance.CheckedBackColor = Color.Red;
                                    cb.FlatAppearance.BorderSize = 0;
                                    
                                    var exist = _vcNgReason.Level3Group.Where(x => x == grp + " - ").ToList();
                                    if (exist.Count <= 0)
                                    {
                                        _vcNgReason.Level3Group.Add(grp + " - ");
                                        var cbGroup = new RoundCheckbox();
                                        cbGroup.TabIndex = cb.TabIndex;
                                        cbGroup.AutoSize = true;
                                        cbGroup.Font = new Font("Segoe UI", 14);
                                        cbGroup.Text = grp;
                                        cbGroup.Appearance = Appearance.Button;
                                        cbGroup.FlatStyle = FlatStyle.Flat;
                                        cbGroup.BackColor = Color.DodgerBlue;
                                        cbGroup.FlatAppearance.CheckedBackColor = Color.IndianRed;
                                        cbGroup.FlatAppearance.BorderSize = 0;
                                        cbGroup.AutoCheck = false;
                                        
                                        if (left + cbGroup.Width + 10 > tableLayoutPanel4.ClientSize.Width - 100)
                                        {
                                            left = 5;
                                            top += cbGroup.Height + 20;
                                        }
                                        page.Height = top + cbGroup.Height + 20;
                                        cbGroup.Left = left;
                                        cbGroup.Top = top;

                                        cbGroup.Click += CbReasonClickLevel3;
                                        page.Controls.Add(cbGroup);
                                        _vcNgReason.Level3CheckBoxes.Add(cbGroup);
                                        left += cbGroup.Width + 10;
                                    }
                                   
                                  
                                    var ngReason = new NgReason(index, listGroupGroup.Name, reason.Name, cb);
                                    _vcNgReason.NgReasons.Add(ngReason);
                            }
                            else
                            {
                                var cb = new RoundCheckbox();
                                cb.TabIndex = index;
                                cb.Font = new Font("Segoe UI", 14);
                                cb.AutoSize = true;
                                cb.Text = reason.Name;
                                cb.Appearance = Appearance.Button;
                                cb.FlatStyle = FlatStyle.Flat;
                                cb.BackColor = Color.FromArgb(0xDE, 0xEA, 0xF8);
                                cb.CheckedChanged += CbReasonChanged;
                                cb.FlatAppearance.CheckedBackColor = Color.Red;
                                cb.FlatAppearance.BorderSize = 0;
                                cb.Refresh();
                                if (left + cb.Width + 10 > tableLayoutPanel4.ClientSize.Width - 100)
                                {
                                    left = 5;
                                    top += cb.Height + 20;
                                }
                                cb.Left = left;
                                cb.Top = top;
                                page.Height = top + cb.Height + 20;

                                page.Controls.Add(cb);
                                left += cb.Width + 10;
                                var ngReason = new NgReason(index, listGroupGroup.Name, reason.Name, cb);
                                _vcNgReason.NgReasons.Add(ngReason);
                            }

                        }
                      
                    }
                    panelReason.Controls.Add(page);
                    lastGroupHeight =page.Top + page.Height;
                }

            }
        }

       
        private void CbReasonClickLevel3(object sender, EventArgs e)
        {
            var cb = (CheckBox)sender;
            using (var frm = new Level3Form(ref _vcNgReason, cb.Text+" - "))
            {
                var dialog = frm.ShowDialog();
                cb.Checked = dialog != DialogResult.Cancel;
            }
            //cb.BackColor = cb.Checked ? Color.Red : Color.LightGray;
        }

        private void ResetNgReason()
        {
            if (_vcNgReason==null)return;
            foreach (var ngReason in _vcNgReason.NgReasons)
            {
                ngReason.CheckBox.Checked = false;
            }
            foreach (var cb in _vcNgReason.Level3CheckBoxes)
            {
                cb.Checked = false;
            }
        }

        private List<ContainerAttrDetail> GetReasonAttribute()
        {
            var d = new List<ContainerAttrDetail>();
            if (_vcNgReason == null) return d;
            var sc = new DefectSource {ShortName = "VC", LongName = "Visual Checking Inspection"};
            d.Add(new ContainerAttrDetail { Name = "defectSource", DataType = TrivialTypeEnum.String, AttributeValue = JsonConvert.SerializeObject(sc), IsExpression = false });
            d.Add(new ContainerAttrDetail { Name = "AfterRepair", DataType = TrivialTypeEnum.String, AttributeValue = "No", IsExpression = false });
            d.Add(new ContainerAttrDetail { Name = "VcAttempt", DataType = TrivialTypeEnum.Integer, AttributeValue = _vcAttempt.ToString(), IsExpression = false });
            int index = 0;
            foreach (var ngReason in _vcNgReason.NgReasons)
            {
                if (ngReason.CheckBox.Checked)
                {
                    var defect = new DefectItem {DefectName = ngReason.Reason, Value = "", TestAttempt = _vcAttempt.ToString()};
                    var attribute = new ContainerAttrDetail { Name = $"defectVC_{_vcAttempt}_{++index}", DataType = TrivialTypeEnum.String, AttributeValue = JsonConvert.SerializeObject(defect), IsExpression = false };
                    d.Add(attribute);
                }
            }

            return d;
        }
        private void CbReasonChanged(object sender, EventArgs e)
        {
            var cb = (CheckBox) sender;
           // cb.BackColor = cb.Checked ? Color.Red : Color.LightGray;
        }

        private async void TimerRealtime_Tick(object sender, EventArgs e)
        {
            await GetStatusOfResource();
            await GetStatusMaintenanceDetails();
        }
        private async void btnResetState_Click(object sender, EventArgs e)
        {
            if (_visualCheckingState == VisualCheckingState.WaitPreparation) return;
            await SetVisualCheckingState(VisualCheckingState.ScanUnitSerialNumber);
            Tb_Scanner.Focus();
        }

        private bool _readScanner;
        private bool _ignoreScanner;
        private readonly int _indexMaintenanceState;
        private int _vcAttempt;


        private async void Tb_Scanner_KeyUp(object sender, KeyEventArgs e)
        {
            if(!_readScanner) Tb_Scanner.Clear();
            if (_ignoreScanner) e.Handled = true;
            if (e.KeyCode == Keys.Enter)
            {
                _ignoreScanner = true;
                if (string.IsNullOrEmpty(Tb_Scanner.Text))return;
                switch (_visualCheckingState)
                {
                    case VisualCheckingState.ScanUnitSerialNumber:
                        Tb_SerialNumber.Text = Tb_Scanner.Text;
                        Tb_Scanner.Clear();
                        await SetVisualCheckingState(VisualCheckingState.CheckUnitStatus);
                        break;
                }
                _ignoreScanner = false;
                    Tb_Scanner.Clear();
            }
        }
#endregion

        private async void Main_Load(object sender, EventArgs e)
        {
            await GetStatusOfResource();
            await GetStatusMaintenanceDetails();
            await GetResourceStatusCodeList();
            await InitNgReasonList();
            await SetVisualCheckingState(VisualCheckingState.WaitPreparation);
        }

        private void ClearPo()
        {
            Tb_PO.Clear();
            Tb_Product.Clear();
            Tb_ProductDesc.Clear();
            Tb_VisualQty.Clear();
            Tb_FinishedGoodCounter.Clear();
            pictureBox1.ImageLocation = null;
            ClrContainer();
        }
      
        private void kryptonGroupBox2_Panel_Paint(object sender, PaintEventArgs e)
        {

        }

        private async void Cb_StatusCode_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                var oStatusCode = await Mes.GetResourceStatusCode(_mesData, Cb_StatusCode.SelectedValue != null ? Cb_StatusCode.SelectedValue.ToString() : "");
                if (oStatusCode != null)
                {
                    Tb_StatusCodeM.Text = oStatusCode.Availability.ToString();
                    if (oStatusCode.ResourceStatusReasons != null)
                    {
                        var oStatusReason = await Mes.GetResourceStatusReasonGroup(_mesData, oStatusCode.ResourceStatusReasons.Name);
                        Cb_StatusReason.DataSource = oStatusReason.Entries;
                    }
                    else
                    {
                        Cb_StatusReason.Items.Clear();
                    }
                }
            }
            catch (Exception ex)
            {
                ex.Source = AppSettings.AssemblyName == ex.Source ? MethodBase.GetCurrentMethod()?.Name : MethodBase.GetCurrentMethod()?.Name + "." + ex.Source;
                EventLogUtil.LogErrorEvent(ex.Source, ex);
            }
        }
        private async void btnSetMachineStatus_Click(object sender, EventArgs e)
        {
            try
            {
                var result = false;
                if (Cb_StatusCode.Text != "" && Cb_StatusReason.Text != "")
                {
                    result = await Mes.SetResourceStatus(_mesData, Cb_StatusCode.Text, Cb_StatusReason.Text);
                }
                else if (Cb_StatusCode.Text != "")
                {
                    result = await Mes.SetResourceStatus(_mesData, Cb_StatusCode.Text, "");
                }

                await GetStatusOfResourceDetail();
                await GetStatusOfResource();
                KryptonMessageBox.Show(result ? "Setup status successful" : "Setup status failed");

            }
            catch (Exception ex)
            {
                ex.Source = AppSettings.AssemblyName == ex.Source ? MethodBase.GetCurrentMethod()?.Name : MethodBase.GetCurrentMethod()?.Name + "." + ex.Source;
                EventLogUtil.LogErrorEvent(ex.Source, ex);
            }
        }

        private async void kryptonNavigator1_SelectedPageChanged(object sender, EventArgs e)
        {
            if (kryptonNavigator1.SelectedIndex == 1)
            {
                await GetStatusOfResourceDetail();
            }
            
            if (kryptonNavigator1.SelectedIndex == 3)
            {
                lblPo.Text = $@"Serial Number of PO: {_mesData.ManufacturingOrder?.Name}";
                lblLoading.Visible = true;
                await GetFinishedGoodRecord();
                lblLoading.Visible = false;
            }

        }
        private async Task GetFinishedGoodRecord()
        {
            var data = await Mes.GetFinishGoodRecord(_mesData, _mesData.ManufacturingOrder?.Name.ToString());
            if (data != null)
            {
                var list = await Mes.ContainerStatusesToFinishedGood(data);
                finishedGoodBindingSource.DataSource = new BindingList<FinishedGood>(list);
                kryptonDataGridView1.DataSource = finishedGoodBindingSource;
                Tb_FinishedGoodCounter.Text = list.Length.ToString();
            }
        }
      
        private void kryptonNavigator1_Selecting(object sender, KryptonPageCancelEventArgs e)
        {
            if (e.Index != 1 && e.Index != 2) return;

            using (var ss = new LoginForm24(e.Index == 1 ? "Maintenance" : "Quality"))
            {
                var dlg = ss.ShowDialog(this);
                if (dlg == DialogResult.Abort)
                {
                    KryptonMessageBox.Show("Login Failed");
                    e.Cancel = true;
                    return;
                }
                if (dlg == DialogResult.Cancel)
                {
                    e.Cancel = true;
                    return;
                }
                if (ss.UserDetails.UserRole == UserRole.Maintenance && e.Index != 1) e.Cancel = true;
                if (ss.UserDetails.UserRole == UserRole.Quality && e.Index != 2) e.Cancel = true;
            }


        }

        private async void btnCallMaintenance_Click(object sender, EventArgs e)
        {
            try
            {
                var result = await Mes.SetResourceStatus(_mesData, "VC - Internal Downtime", "Maintenance");
                await GetStatusOfResource();
                KryptonMessageBox.Show(result ? "Setup status successful" : "Setup status failed");

            }
            catch (Exception ex)
            {
                ex.Source = AppSettings.AssemblyName == ex.Source ? MethodBase.GetCurrentMethod()?.Name : MethodBase.GetCurrentMethod()?.Name + "." + ex.Source;
                EventLogUtil.LogErrorEvent(ex.Source, ex);
            }
        }

        private async void btnFinishPreparation_Click(object sender, EventArgs e)
        {
            if (_mesData.ResourceStatusDetails == null) return;
            if (_mesData.ResourceStatusDetails.Reason.Name == "Maintenance") return;
            var result = await Mes.SetResourceStatus(_mesData, "VC - Productive Time", "Pass");
            await GetStatusOfResource();
            if (result)
            {
                btnFinishPreparation.Enabled = false;
                btnStartPreparation.Enabled = true;
                await SetVisualCheckingState(VisualCheckingState.ScanUnitSerialNumber);
            }
        }

        private async void btnStartPreparation_Click(object sender, EventArgs e)
        {
            ClearPo();
            if (_mesData.ResourceStatusDetails==null) return;
            if (_mesData.ResourceStatusDetails?.Reason?.Name=="Maintenance") return;
            _mesData.SetManufacturingOrder(null);
            var result = await Mes.SetResourceStatus(_mesData, "VC - Planned Downtime", "Preparation");
            await GetStatusOfResource();
            if (result)
            {
                btnFinishPreparation.Enabled = true;
                btnStartPreparation.Enabled = false;
            }
        }


        private void Tb_Scanner_TextChanged(object sender, EventArgs e)
        {

        }

        private async void btnSubmit_Click(object sender, EventArgs e)
        {
            if (_containerResult == "")
            {
                KryptonMessageBox.Show("Please select Visual Inspection Result");
                return;
            }
            await SetVisualCheckingState(VisualCheckingState.UpdateMoveInMove);
        }

        private void btnPass_Click(object sender, EventArgs e)
        {
            _containerResult = ResultString.True;
            btnSubmit.Visible = true;
        }

     
        private async void btnFail_Click(object sender, EventArgs e)
        {
            panelReason.Visible = true;
            await SetVisualCheckingState(VisualCheckingState.FailReason);
            _containerResult = ResultString.False;
            btnSubmit.Visible = true;
        }

        private void Main_SizeChanged(object sender, EventArgs e)
        {
           
        }

        private void panelReason_Paint(object sender, PaintEventArgs e)
        {

        }

        private void Dg_Maintenance_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            try
            {
                foreach (DataGridViewRow row in Dg_Maintenance.Rows)
                {
                    switch (Convert.ToString(row.Cells[_indexMaintenanceState].Value))
                    {
                        //Console.WriteLine(Convert.ToString(row.Cells["MaintenanceState"].Value));
                        case "Pending":
                            row.DefaultCellStyle.BackColor = Color.Yellow;
                            break;
                        case "Due":
                            row.DefaultCellStyle.BackColor = Color.Orange;
                            break;
                        case "Past Due":
                            row.DefaultCellStyle.BackColor = Color.Red;
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                ex.Source = AppSettings.AssemblyName == ex.Source ? MethodBase.GetCurrentMethod()?.Name : MethodBase.GetCurrentMethod()?.Name + "." + ex.Source;
                EventLogUtil.LogErrorEvent(ex.Source, ex);
            }
        }
    }
}
