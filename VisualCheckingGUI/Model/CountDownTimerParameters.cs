using Hmi.Helpers.Enum;
using Hmi.TypeConverter;
using System;
using System.ComponentModel;
using System.Drawing;


namespace VisualCheckingGUI.Model
{
    public class CountDownTimerParameters
    {
        public CountDownTimerParameters()
        {

        }
        public CountDownTimerParameters(string name, string text, Color color)
        {
            Name = name;
            MessageText = text;
            MessageColor = color;
        }
        [Category("Count Down Timer Parameters"), Browsable(true), ReadOnly(false), DefaultValue(""), DesignOnly(false),
        Description("Timer Name"), DisplayName("Name")]
        public string Name { get; set; } = "Timer Name";
        [Category("Count Down Timer Parameters"), Browsable(true), ReadOnly(false), DefaultValue(""), DesignOnly(false),
        Description("Message Text"), DisplayName("Message Text")]
        public string MessageText { get; set; } = "Timer Text";
        [Category("Count Down Timer Parameters"), Browsable(true), ReadOnly(false), DefaultValue(""), DesignOnly(false),
        Description("Message Color"), DisplayName("Message Color")]
        public Color MessageColor { get; set; } = Color.Red;
        [Category("Count Down Timer Parameters"), Browsable(true), ReadOnly(false), DefaultValue(""), DesignOnly(false),
        Description("Time Setting in seconds"), DisplayName("Time Setting")]
        public int Interval { get; set; } = 10;
        [Category("Count Down Timer Parameters"), Browsable(true), ReadOnly(false), DefaultValue(""), DesignOnly(false),
        Description("Enable Timer"), DisplayName("Enable Timer")]
        public YesNo Enable { get; set; } = YesNo.Yes;
        [Category("Count Down Timer Parameters"), Browsable(true), ReadOnly(false), DefaultValue(""), DesignOnly(false),
        Description("Sound File Location"), DisplayName("Sound File Location")]
        [Editor(typeof(FileEditorSound), typeof(System.Drawing.Design.UITypeEditor))]
        public string SoundLocation { get; set; } = @"C:\Windows\Media\Speech On.wav";
        [Category("Count Down Timer Parameters"), Browsable(true), ReadOnly(false), DefaultValue(""), DesignOnly(false),
        Description("Enable Sound"), DisplayName("Enable Sound")]
        public YesNo EnableSound { get; set; } = YesNo.Yes;
        [Category("Count Down Timer Parameters"), Browsable(true), ReadOnly(false), DefaultValue(""), DesignOnly(false),
        Description("Sound Repeat"), DisplayName("Sound Repeat")]
        public int SoundRepeat { get; set; } = 1;
        [Category("Count Down Timer Parameters"), Browsable(true), ReadOnly(false), DefaultValue(""), DesignOnly(false),
        Description("Message Font Style"), DisplayName("Message Font Style")]
        public Font MessageFontStyle { get; set; } = new Font("Arial",50, System.Drawing.FontStyle.Bold);
        [Category("Count Down Timer Parameters"), Browsable(true), ReadOnly(false), DefaultValue(""), DesignOnly(false),
       Description("Number Font Style"), DisplayName("Number Font Style")]
        public Font NumberFontStyle { get; set; } = new Font("Arial", 150, System.Drawing.FontStyle.Bold);
    }
    public class CountDownTimerParametersTypeConverter : TypeConverter
    {
        public override object ConvertTo(ITypeDescriptorContext context,
            System.Globalization.CultureInfo culture, object value, Type destinationType)
        {//This method is used to shown information in the PropertyGrid.
            if (destinationType == typeof(string))
            {
                var d = (CountDownTimerParameters)value;
                return $"{d.Name}, {d.Interval}, Enable={d.Enable:g}";
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }
        public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext context, object value, Attribute[] attributes)
        {
            return TypeDescriptor.GetProperties(typeof(CountDownTimerParameters), attributes).Sort();
        }
        public override bool GetPropertiesSupported(ITypeDescriptorContext context)
        {
            return true;
        }
    }
}
