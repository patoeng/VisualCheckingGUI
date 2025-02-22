using Hmi.Thread;
using System;
using System.Windows.Forms;
using VisualCheckingGUI.Model;

namespace VisualCheckingGUI
{
    public partial class TestForm : Form
    {
        public TestForm()
        {
            InitializeComponent();
            InitCountDownTimers();
        }
        private void InitCountDownTimers()
        {
            CleaningTimer.ReloadInstance();
            InspectionTimer.ReloadInstance();

            CleaningTimer.Instance.CountDownTick += CleaningTimer_CountDownTick;
            CleaningTimer.Instance.CountDownReached += CleaningTimer_CountDownReached;
            CleaningTimer.Instance.CountDownStarted += CountDownStarted;
            CleaningTimer.Instance.CountDownStopped += CountDownStopped;
            InspectionTimer.Instance.CountDownTick += CleaningTimer_CountDownTick;
            InspectionTimer.Instance.CountDownReached += CleaningTimer_CountDownReached;
            InspectionTimer.Instance.CountDownStarted += CountDownStarted;
            InspectionTimer.Instance.CountDownStopped += CountDownStopped;
        }

        private void CountDownStopped(object sender, string e)
        {
            lblCountDownNumber.Visible = false;
            lblCountDownTitle.Visible = false;
        }

        private void CountDownStarted(object sender, string e)
        {
            var cdt = (CountDownTimer)sender;
            lblCountDownNumber.ForeColor = cdt.Parameters.MessageColor;
            lblCountDownTitle.ForeColor = cdt.Parameters.MessageColor;
            lblCountDownNumber.Text = cdt.CountDown.ToString();
            lblCountDownTitle.Text = cdt.Parameters.MessageText;
            lblCountDownNumber.Visible = true;
            lblCountDownTitle.Visible = true;
        }

        private void CleaningTimer_CountDownTick(object sender, int e)
        {
            var cdt = (CountDownTimer)sender;
            ThreadHelper.ControlUpdate(lblCountDownNumber, cdt.CountDown.ToString());
        }

        private void CleaningTimer_CountDownReached(object sender, string e)
        {
            var cdt = (CountDownTimer)sender;
            if (cdt.Parameters.SoundRepeat == 1)
            {
                cdt.PlaySoundAsync();
            }
            else
            {
                cdt.PlaySound(cdt.Parameters.SoundRepeat);
            }
           
        }
        private void button1_Click(object sender, EventArgs e)
        {
            lblCountDownNumber.Font = ApplicationConfig.Instance.CleaningTimer.NumberFontStyle;
            lblCountDownTitle.Font = ApplicationConfig.Instance.CleaningTimer.MessageFontStyle;
            CleaningTimer.Start();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            lblCountDownNumber.Font = ApplicationConfig.Instance.InspectionTimer.NumberFontStyle;
            lblCountDownTitle.Font = ApplicationConfig.Instance.InspectionTimer.MessageFontStyle;
            InspectionTimer.Start();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            var frm = ApplicationConfig.Load();
            var d = frm.ShowForm();
            if (d != DialogResult.OK) return;
            frm.Save();
            ApplicationConfig.ReloadInstance();
        }
    }
}
