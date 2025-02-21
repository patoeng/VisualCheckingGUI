using System.ComponentModel;
using System.Drawing;
using System.IO.Ports;
using Hmi.Helpers;
using Hmi.Helpers.Enum;
using Hmi.Model;
using Hmi.Settings;
using Hmi.TypeConverter;

namespace VisualCheckingGUI.Model
{
    public class ApplicationConfig : AppSettings<ApplicationConfig>
    {
        [Category("General"), Browsable(true), ReadOnly(false), DefaultValue(""), DesignOnly(false),
         Description("Data Location"), DisplayName("Data Location")]
        [Editor(typeof(FolderNameEditor2), typeof(System.Drawing.Design.UITypeEditor))]
        public string DataLocation { get; set; } = "C:/temp/raw_data";

        [Category("General"), Browsable(true), ReadOnly(false), DefaultValue(""), DesignOnly(false),
         Description("Data Record Location"), DisplayName("Data Record Location")]
        [Editor(typeof(FolderNameEditor2), typeof(System.Drawing.Design.UITypeEditor))]
        public string RecordLocation { get; set; } = "C:/temp";

        [Browsable(false)]
        public string PasswordQc { get; set; } = Encryption.Encrypt("wik123");

        [Category("General"), Browsable(true), ReadOnly(false), DefaultValue(""), DesignOnly(false),
         Description("Shift Definition."), DisplayName("Shift")]
        [TypeConverter(typeof(ShiftModelTypeConverter))]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public ShiftModel ShiftModel { get; set; } = new ShiftModel
        { Shift1 = "07:00", Shift2 = "19:00", ShiftType = ShiftType.DualShift };

        [Category("General"), Browsable(true), ReadOnly(false), DefaultValue(""), DesignOnly(false),
         Description("Line Code"), DisplayName("Line Code")]
        public char LineId { get; set; } = 'X';

        [Category("General"), Browsable(true), ReadOnly(false), DefaultValue(""), DesignOnly(false),
         Description("Brand Code"), DisplayName("Brand Code")]
        public string BrandCode { get; set; } = "B50";

        [Category("Weighing Database"), Browsable(true), ReadOnly(false), DefaultValue(""), DesignOnly(false),
        Description("Database Server"), DisplayName("Database Server")]
        public string DatabaseServer { get; set; } = "127.0.0.1";

        [Category("Weighing Database"), Browsable(false)]
        public string WeighingDatabaseConnection => $"Data Source=\"{DatabaseServer}\";Initial Catalog=\"{DbCatalog}\";User id=\"{DbUserId}\";Password=\"{DbPassword}\";";
        [Category("Weighing Database"), Browsable(true), DisplayName("Password")]
        [PasswordPropertyText(true)]
        public string DbPassword { get; set; } = "passwordwik";
        [Category("Weighing Database"), Browsable(true), DisplayName("User Id")]
        public string DbUserId { get; set; } = "wik";
        [Category("Weighing Database"), Browsable(true), DisplayName("Database Catalog")]
        public string DbCatalog { get; set; } = "9630";

        [Category("Timers"), Browsable(true), ReadOnly(false), DefaultValue(""), DesignOnly(false),
        Description("Cleaning Timer in Seconds"), DisplayName("Cleaning Timer")]
        [TypeConverter(typeof(CountDownTimerParametersTypeConverter))]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public CountDownTimerParameters CleaningTimer { get; set; } = new CountDownTimerParameters("Cleaning Timer","Please Clean the Unit!", Color.Blue);
        [Category("Timers"), Browsable(true), ReadOnly(false), DefaultValue(""), DesignOnly(false),
         Description("Inspection Timer"), DisplayName("Inspection Timer")]
        [TypeConverter(typeof(CountDownTimerParametersTypeConverter))]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public CountDownTimerParameters InspectionTimer { get; set; } = new CountDownTimerParameters("Inspection Timer", "Please do a Visual Check! ", Color.Red);

        [Category("PSN Scanner Serial Com"), Browsable(true), ReadOnly(false), DefaultValue(""), DesignOnly(false),
         DescriptionAttribute("Com Name"), DisplayName("Com Name")]
        [TypeConverter(typeof(SerialComListConverter))]
        public string PsnScannerComName { get; set; } = "COM1";
        [Category("PSN Scanner Serial Com"), Browsable(true), ReadOnly(false), DefaultValue(""), DesignOnly(false),
         DescriptionAttribute("Com Baud Rate"), DisplayName("Com Baud Rate")]
        public int PsnScannerBaudRate { get; set; } = 115200;
        [Category("PSN Scanner Serial Com"), Browsable(true), ReadOnly(false), DefaultValue(""), DesignOnly(false),
         DescriptionAttribute("Com Data Bits"), DisplayName("Com Data Bits")]
        public int PsnScannerDataBits { get; set; } = 8;
        [Category("PSN Scanner Serial Com"), Browsable(true), ReadOnly(false), DefaultValue(""), DesignOnly(false),
         DescriptionAttribute("Com Parity"), DisplayName("Com Parity")]
        public Parity PsnScannerParity { get; set; } = Parity.None;
        [Category("PSN Scanner Serial Com"), Browsable(true), ReadOnly(false), DefaultValue(""), DesignOnly(false),
         DescriptionAttribute("Com Stop Bits"), DisplayName("Com Stop Bits")]
        public StopBits PsnScannerStopBits { get; set; } = StopBits.One;

        private static ApplicationConfig _application;

        public static ApplicationConfig ReloadInstance()
        {
            _application = Load();
            return _application;
        }

        public static ApplicationConfig Instance => _application ?? ReloadInstance();

    }
}
