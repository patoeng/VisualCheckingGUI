using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Camstar.WCF.ObjectStack;
using Camstar.WCF.Services;
using ComponentFactory.Krypton.Navigator;
using ComponentFactory.Krypton.Toolkit;
using MesData;

namespace VisualCheckingGUI.Model
{
    public partial class Level3Form : KryptonForm
    {
        private readonly VcNgReason _vcNgReason;
        private readonly string _group;

        public Level3Form(ref VcNgReason vcNgReason, string group)
        {
            InitializeComponent();
            _vcNgReason = vcNgReason;
            _group = group;
            Text = group;
            InitCheckBox();
        }

        private void InitCheckBox()
        {
                var page = new KryptonPage(_group.Remove(_group.IndexOf(" - ", StringComparison.Ordinal),3));
                page.AutoScroll = true;
            
                var left = 5;
                var top = 5;
                var listGroupGroup = _vcNgReason.NgReasons.Where(x => x.Reason.Contains(_group)).ToList();
                foreach (var reason in listGroupGroup)
                {

                    {
                        var cb = new RoundCheckbox();
                        cb.AutoSize = true;
                        cb.Font = reason.CheckBox.Font;
                        cb.Text = reason.CheckBox.Text;
                        cb.Appearance = reason.CheckBox.Appearance;
                        cb.FlatStyle = reason.CheckBox.FlatStyle;
                        cb.Checked = reason.CheckBox.Checked;
                        cb.BackColor = reason.CheckBox.BackColor;
                        cb.CheckedChanged += CbReasonChanged;
                        cb.FlatAppearance.CheckedBackColor = reason.CheckBox.FlatAppearance.CheckedBackColor;
                        cb.FlatAppearance.BorderSize = reason.CheckBox.FlatAppearance.BorderSize;
                        cb.Refresh();
                        if (left + cb.Width + 5 > kryptonNavigator1.Width - 100)
                        {
                            left = 5;
                            top += cb.Height + 20;
                        }
                        cb.Left = left;
                        cb.Top = top;

                        page.Controls.Add(cb);
                        left += cb.Width + 5;
                    }

                }

                kryptonNavigator1.Pages.Add(page);
        }

        private void CbReasonChanged(object sender, EventArgs e)
        {
            var cb = (CheckBox)sender;
            var cbList = _vcNgReason.NgReasons.Where(x => x.Reason.Contains(cb.Text) && x.Reason.Contains(_group)).ToList();
            if (cbList.Count > 0)
            {
                _vcNgReason.NgReasons[cbList[0].Id-1].CheckBox.Checked = cb.Checked;
            }
        }

        private void btnOk_Click(object sender, System.EventArgs e)
        {
            Close();
        }

        private void Level3Form_Load(object sender, System.EventArgs e)
        {

        }

        private void Level3Form_FormClosing(object sender, FormClosingEventArgs e)
        {
            var cbCheck = _vcNgReason.NgReasons
                .Where(x => x.Reason.Contains(_group) && x.CheckBox.Checked).ToList();
            DialogResult = cbCheck.Count <= 0 ? DialogResult.Cancel : DialogResult.OK;
        }
    }
}
