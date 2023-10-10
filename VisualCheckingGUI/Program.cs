using OpcenterWikLibrary;
using System;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using System.Windows.Forms;
using VCGUI;

namespace VisualCheckingGUI
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            var mutexId = $"Global\\{{{{{Guid}}}}}";
            var allowEveryoneRule =
                new MutexAccessRule(
                    new SecurityIdentifier(WellKnownSidType.WorldSid, null),
                    MutexRights.FullControl, AccessControlType.Allow);
            var securitySettings = new MutexSecurity();
            securitySettings.AddAccessRule(allowEveryoneRule);
            using (var mutex = new Mutex(false, mutexId, out bool createdNew, securitySettings))
            {
                var hasHandle = mutex.WaitOne(1000, false);
                if (!hasHandle)
                {
                    MessageBox.Show(@"Aplikasi sudah berjalan!", @"Application");
                }
                else
                {
                    AppSettings.AssemblyName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    Application.Run(new Main());
                }
            }
        }
        public const string Guid = "9C6BB693-AEBE-401F-861C-97C2A1CBE74D";
    }
}
