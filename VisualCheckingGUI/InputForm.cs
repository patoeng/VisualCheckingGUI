using System;
using System.Windows.Forms;

namespace Hmi.Helpers
{
    public partial class InputForm : Form
    {

        public InputForm(string title, string message,string defaultValue="")
        {
            InitializeComponent();
            this.Text = title;
            label1.Text = message;
            textBox1.Text = defaultValue;

        }

        public sealed override string Text
        {
            get { return base.Text; }
            set { base.Text = value; }
        }

        public string Value { get; set; }
        private void btnOk_Click(object sender, EventArgs e)
        {
            Value = textBox1.Text;
            Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            Value = "";
            Close();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            textBox1.Clear();
        }

        private void textBox1_Enter(object sender, EventArgs e)
        {
          
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        public static string Show(string title, string message, string defaultValue = "")
        {
            var frm = new InputForm(title, message, defaultValue);
            var dlg = frm.ShowDialog();

            return frm.Value;

        }
     
    }
}
