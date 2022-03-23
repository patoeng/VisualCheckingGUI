using System;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using Camstar.WCF.ObjectStack;
using ComponentFactory.Krypton.Toolkit;
using MesData;
using OpcenterWikLibrary;
using VisualCheckingGUI.Enumeration;


namespace VisualCheckingGUI
{
    public partial class Main : KryptonForm
    {
        #region CONSTRUCTOR

        public  Main()
        {
            InitializeComponent();
            Rectangle r = new Rectangle(0, 0, Pb_IndicatorPicture.Width, Pb_IndicatorPicture.Height);
            System.Drawing.Drawing2D.GraphicsPath gp = new System.Drawing.Drawing2D.GraphicsPath();
            int d = 28;
            gp.AddArc(r.X, r.Y, d, d, 180, 90);
            gp.AddArc(r.X + r.Width - d, r.Y, d, d, 270, 90);
            gp.AddArc(r.X + r.Width - d, r.Y + r.Height - d, d, d, 0, 90);
            gp.AddArc(r.X, r.Y + r.Height - d, d, d, 90, 90);
            Pb_IndicatorPicture.Region = new Region(gp);

#if MiniMe
            var  name = "Visual Checking Minime";
            Text = Mes.AddVersionNumber(Text + " MiniMe");
#elif Ariel
            var  name = "Visual Checking Ariel";
            Text = Mes.AddVersionNumber(Text + " Ariel");
#endif
            _mesData = new Mes(name);
           


            WindowState = FormWindowState.Normal;
            Size = new Size(810, 703);
            MyTitle.Text = $"Visual Checking - {AppSettings.Resource}";
            ResourceGrouping.Values.Heading = $"Resource Status: {AppSettings.Resource}";
            ResourceDataGroup.Values.Heading = $"Resource Data Collection: {AppSettings.Resource}";
            
        }

        #endregion

        #region INSTANCE VARIABLE

        private static DateTime _dMoveIn;
        private readonly Mes _mesData;
        private VisualCheckingState _visualCheckingState;

        #endregion

        #region FUNCTION USEFULL

        #endregion

        #region FUNCTION STATUS OF RESOURCE
        private async Task SetVisualCheckingState(VisualCheckingState visualCheckingState)
        {
            _visualCheckingState = visualCheckingState;
            switch (_visualCheckingState)
            {
                case VisualCheckingState.PlaceUnit:
                    Tb_Scanner.Enabled = false;
                    lblCommand.ForeColor = Color.Red;
                    lblCommand.Text = "Resource is not in \"Up\" condition!";
                    break;
                case VisualCheckingState.ScanUnitSerialNumber:
                    btnFail.Visible = false;
                    btnPass.Visible = false;

                    Tb_Operation.Clear();
                    Tb_PO.Clear();
                    Tb_ContainerPosition.Clear();
                    Tb_ReasonNG.Clear();
                    Cb_PassFail.Text = "";
                    Tb_SerialNumber.Clear();

                    if (_mesData.ResourceStatusDetails == null || _mesData.ResourceStatusDetails?.Availability != "Up")
                    {
                        await SetVisualCheckingState(VisualCheckingState.PlaceUnit);
                        break;
                    }
                    Tb_Scanner.Enabled = true;
                    lblCommand.ForeColor = Color.LimeGreen;
                    lblCommand.Text = "Scan Unit Serial Number!";
                    ActiveControl = Tb_Scanner;
                    break;
                case VisualCheckingState.CheckUnitStatus:
                    Tb_Scanner.Enabled = false;
                    lblCommand.Text = "Checking Unit Status";

                    var oContainerStatus = await Mes.GetContainerStatusDetails(_mesData, Tb_SerialNumber.Text, _mesData.DataCollectionName);
                    Tb_ContainerPosition.Text = await Mes.GetCurrentContainerStep(_mesData, Tb_SerialNumber.Text);
                    if (oContainerStatus != null)
                    {
                        if (oContainerStatus.MfgOrderName != null) Tb_PO.Text = oContainerStatus.MfgOrderName.ToString();
                        if (oContainerStatus.Operation != null)
                        {
                            Tb_Operation.Text = oContainerStatus.Operation.Name;
                            if (oContainerStatus.Operation.Name != _mesData.DataCollectionName)
                            {
                                await SetVisualCheckingState(VisualCheckingState.WrongPosition);
                                break;
                            }
                        }
                        _dMoveIn = DateTime.Now;
                        await SetVisualCheckingState(VisualCheckingState.VisualCheckResult);
                        break;
                    }
                    await SetVisualCheckingState(VisualCheckingState.UnitNotFound);
                    break;
                case VisualCheckingState.UnitNotFound:
                    Tb_Scanner.Enabled = false;
                    lblCommand.ForeColor = Color.Red;
                    lblCommand.Text = "Unit Not Found";
                    break;
                case VisualCheckingState.VisualCheckResult:
                    btnFail.Visible = true;
                    btnPass.Visible = true;
                    Tb_Scanner.Enabled = false;
                    lblCommand.Text = "Visual Checking Result?";
                    break;
                case VisualCheckingState.UpdateMoveInMove:
                    Tb_Scanner.Enabled = false;
                    lblCommand.Text = "Container Move In";

                    var sPassFail = Cb_PassFail.Text != "" ? Cb_PassFail.Text : ResultString.False;
                    var cDataPoint = new DataPointDetails[2];
                    cDataPoint[0] = new DataPointDetails()
                    {
                        DataName = "Reason NG (Not Good)",
                        DataValue = Tb_ReasonNG.Text != "" ? Tb_ReasonNG.Text : "Empty",
                        DataType = DataTypeEnum.String
                    };
                    cDataPoint[1] = new DataPointDetails()
                    { DataName = "RESULT", DataValue = sPassFail, DataType = DataTypeEnum.String };
                    oContainerStatus = await Mes.GetContainerStatusDetails(_mesData, Tb_SerialNumber.Text, _mesData.DataCollectionName);
                    if (oContainerStatus != null)
                    {
                        var resultMoveIn = await Mes.ExecuteMoveIn(_mesData, oContainerStatus.ContainerName.Value,
                            _dMoveIn);
                        if (resultMoveIn)
                        {
                            lblCommand.Text = "Container Move Standard";
                            var resultMoveStd = await Mes.ExecuteMoveStandard(_mesData, oContainerStatus.ContainerName.Value, DateTime.Now, cDataPoint);
                            await SetVisualCheckingState(resultMoveStd
                                ? VisualCheckingState.ScanUnitSerialNumber
                                : VisualCheckingState.MoveInOkMoveFail);
                            break;
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
                    lblCommand.Text = "Move Standard Fail";
                    break;
                case VisualCheckingState.MoveInFail:
                    lblCommand.ForeColor = Color.Red;
                    Tb_Scanner.Enabled = false;
                    lblCommand.Text = "Move In Fail";
                    break;
                case VisualCheckingState.Done:
                    break;
                case VisualCheckingState.FailReason:
                    Tb_Scanner.Enabled = true;
                    lblCommand.Text = "Type in NG reason, then press Enter!";
                    break;
                case VisualCheckingState.WrongPosition:
                    lblCommand.ForeColor = Color.Red;
                    Tb_Scanner.Enabled = false;
                    lblCommand.Text = "Incorrect Product Operation";
                    break;
            }
        }
        private async Task GetStatusMaintenanceDetails()
        {
            var maintenanceStatusDetails = await Mes.GetMaintenanceStatusDetails(_mesData);
            if (maintenanceStatusDetails != null)
            {
                Dg_Maintenance.DataSource = maintenanceStatusDetails;
                Dg_Maintenance.DataSource = _mesData.MaintenanceStatusDetails;
                    Dg_Maintenance.Columns["Due"].Visible = false;
                    Dg_Maintenance.Columns["Warning"].Visible = false;
                    Dg_Maintenance.Columns["PastDue"].Visible = false;
                    Dg_Maintenance.Columns["MaintenanceReqName"].Visible = false;
                    Dg_Maintenance.Columns["MaintenanceReqDisplayName"].Visible = false;
                    Dg_Maintenance.Columns["ResourceStatusCodeName"].Visible = false;
                    Dg_Maintenance.Columns["UOMName"].Visible = false;
                    Dg_Maintenance.Columns["ResourceName"].Visible = false;
                    Dg_Maintenance.Columns["UOM2Name"].Visible = false;
                    Dg_Maintenance.Columns["MaintenanceReqRev"].Visible = false;
                    Dg_Maintenance.Columns["NextThruputQty2Warning"].Visible = false;
                    Dg_Maintenance.Columns["NextThruputQty2Limit"].Visible = false;
                    Dg_Maintenance.Columns["UOM2"].Visible = false;
                    Dg_Maintenance.Columns["ThruputQty2"].Visible = false;
                    Dg_Maintenance.Columns["Resource"].Visible = false;
                    Dg_Maintenance.Columns["ResourceStatusCode"].Visible = false;
                    Dg_Maintenance.Columns["NextThruputQty2Due"].Visible = false;
                    Dg_Maintenance.Columns["MaintenanceClassName"].Visible = false;
                    Dg_Maintenance.Columns["MaintenanceStatus"].Visible = false;
                    Dg_Maintenance.Columns["ExportImportKey"].Visible = false;
                    Dg_Maintenance.Columns["DisplayName"].Visible = false;
                    Dg_Maintenance.Columns["Self"].Visible = false;
                    Dg_Maintenance.Columns["IsEmpty"].Visible = false;
                    Dg_Maintenance.Columns["FieldAction"].Visible = false;
                    Dg_Maintenance.Columns["IgnoreTypeDifference"].Visible = false;
                    Dg_Maintenance.Columns["ListItemAction"].Visible = false;
                    Dg_Maintenance.Columns["ListItemIndex"].Visible = false;
                    Dg_Maintenance.Columns["CDOTypeName"].Visible = false;
                    Dg_Maintenance.Columns["key"].Visible = false;
            }
           
        }

        private async Task GetStatusOfResource()
        {
           var resourceStatus = await Mes.GetResourceStatusDetails(_mesData); 
           _mesData.SetResourceStatusDetails(resourceStatus);
           if (resourceStatus != null)
           {
               if (resourceStatus.Status != null)
                   Tb_StatusCode.Text = resourceStatus.Status.Name;
               if (resourceStatus.Reason != null)
                   Tb_StatusReason.Text = resourceStatus.Reason.Name;
               if (resourceStatus.Availability != null)
               {
                   Tb_Availability.Text = resourceStatus.Availability.Value;
                   if (resourceStatus.Availability.Value == "Up")
                   {
                       Pb_IndicatorPicture.BackColor = Color.Green;
                   }
                   else if (resourceStatus.Availability.Value == "Down")
                   {
                       Pb_IndicatorPicture.BackColor = Color.Red;
                   }
               }
               else
               {
                   Pb_IndicatorPicture.BackColor = Color.Orange;
               }

               var zeroEpoch = new DateTime(1899, 12, 30);
               if (resourceStatus.TimeAtStatus != null)
                   Tb_TimeAtStatus.Text =
                       $@"{(DateTime.FromOADate(resourceStatus.TimeAtStatus.Value) - zeroEpoch):G}";
           }

        }

        #endregion

        #region COMPONENT EVENT
        private async void Main_Load(object sender, EventArgs e)
        {
            await GetStatusOfResource();
            await GetStatusMaintenanceDetails();
            await SetVisualCheckingState(VisualCheckingState.ScanUnitSerialNumber);
        }

        private async void btnResourceSetup_Click(object sender, EventArgs e)
        {
            Mes.ResourceSetupForm(this, _mesData, MyTitle.Text);
            await GetStatusOfResource();
        }
        private void Dg_Maintenance_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            try
            {
                foreach (DataGridViewRow row in Dg_Maintenance.Rows)
                {
                    //Console.WriteLine(Convert.ToString(row.Cells["MaintenanceState"].Value));
                    if (Convert.ToString(row.Cells["MaintenanceState"].Value) == "Pending")
                    {
                        row.DefaultCellStyle.BackColor = Color.Yellow;
                    }
                    else if (Convert.ToString(row.Cells["MaintenanceState"].Value) == "Due")
                    {
                        row.DefaultCellStyle.BackColor = Color.Orange;
                    }
                    else if (Convert.ToString(row.Cells["MaintenanceState"].Value) == "Past Due")
                    {
                        row.DefaultCellStyle.BackColor = Color.Red;
                    }
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

        private async void TimerRealtime_Tick(object sender, EventArgs e)
        {
            TimerRealtime.Stop();
            await GetStatusOfResource();
            await GetStatusMaintenanceDetails();
            TimerRealtime.Start();
        }

        private async void btnResetState_Click(object sender, EventArgs e)
        {
            await SetVisualCheckingState(VisualCheckingState.ScanUnitSerialNumber);
        }
       
        

        private async void Tb_Scanner_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (string.IsNullOrEmpty(Tb_Scanner.Text)) return;
                switch (_visualCheckingState)
                {
                    case VisualCheckingState.ScanUnitSerialNumber:
                        Tb_SerialNumber.Text = Tb_Scanner.Text;
                        Tb_Scanner.Clear();
                        await SetVisualCheckingState(VisualCheckingState.CheckUnitStatus);
                        break;
                    case VisualCheckingState.FailReason:
                        Tb_ReasonNG.Text = Tb_Scanner.Text;
                        Tb_Scanner.Clear();
                        await SetVisualCheckingState(VisualCheckingState.UpdateMoveInMove);
                        break;
                }
            }
        }

        private async void btnPass_Click(object sender, EventArgs e)
        {
            btnFail.Visible = false;
            btnPass.Visible = false;
            Cb_PassFail.Text = "True";
            await SetVisualCheckingState(VisualCheckingState.UpdateMoveInMove);
        }

        private async void btnFail_Click(object sender, EventArgs e)
        {
            btnFail.Visible = false;
            btnPass.Visible = false;
            Cb_PassFail.Text = "False";
            await SetVisualCheckingState(VisualCheckingState.FailReason);
        }
        #endregion

        
    }
}
