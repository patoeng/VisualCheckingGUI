using System;
using System.Windows.Forms;

namespace VisualCheckingGUI.Model
{
    public class NgReason
    {
        public int Id { get; protected set; }
        public string Group { get; protected set; }
        public string Reason { get; protected set; }
        public RoundCheckbox CheckBox { get; protected set; }
        public NgReason(int id, string group, string reason, RoundCheckbox checkBox)
        {
            Id = id;
            Group = group;
            Reason = reason;
            CheckBox = checkBox;
        }
    }
}
