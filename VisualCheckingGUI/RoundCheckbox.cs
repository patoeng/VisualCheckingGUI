using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace VisualCheckingGUI
{
    public class RoundCheckbox : CheckBox
    {
        protected override void OnPaint(PaintEventArgs e)
        {
           Rectangle bounds = new Rectangle(0, 0, ClientSize.Width, ClientSize.Height);

            int radius = 5;

            int diameter = radius * 2;
            Size size = new Size(diameter, diameter);
            Rectangle arc = new Rectangle(bounds.Location, size);
            GraphicsPath path = new GraphicsPath();


            // top left arc  
            path.AddArc(arc, 180, 90);

            // top right arc  
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270, 90);

            // bottom right arc  
            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0, 90);

            // bottom left arc 
            arc.X = bounds.Left;
            path.AddArc(arc, 90, 90);

            path.CloseFigure();
            this.Region = new System.Drawing.Region(path);
            base.OnPaint(e);
        }
    }
}
