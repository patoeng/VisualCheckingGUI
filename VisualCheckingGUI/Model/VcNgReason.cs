using System.Collections.Generic;
using System.Windows.Forms;

namespace VisualCheckingGUI.Model
{
    public class VcNgReason
    {
        public List<NgReason> NgReasons { get; protected set; }
        public List<string> Level3Group { get; protected set; }
        public List<CheckBox> Level3CheckBoxes { get; protected set; }

        public VcNgReason()
        {
            NgReasons = new List<NgReason>();
            Level3Group = new List<string>();
            Level3CheckBoxes = new List<CheckBox>();
        }
    }
}
