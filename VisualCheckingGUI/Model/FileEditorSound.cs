using System;
using System.ComponentModel;
using System.Drawing.Design;
using System.Windows.Forms;

namespace VisualCheckingGUI.Model
{
     public class FileEditorSound : UITypeEditor
    {
        private OpenFileDialog ofd = new OpenFileDialog();
        public override UITypeEditorEditStyle GetEditStyle(
            ITypeDescriptorContext context)
        {
            return UITypeEditorEditStyle.Modal;
        }

        public override object EditValue(
            ITypeDescriptorContext context,
            IServiceProvider provider,
            object value)
        {
            ofd.FileName = value.ToString();
            ofd.Filter = "Wave File|*.WAV|Mid File|*.MID|All Files|*.*";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                return ofd.FileName;
            }
            return base.EditValue(context, provider, value);
        }
    }
}
