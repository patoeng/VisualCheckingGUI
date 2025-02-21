using System;

namespace VisualCheckingGUI.Model
{
    public class InspectionTimer
    {
        private static CountDownTimer _instance;
        public static CountDownTimer Instance => _instance ?? ReloadInstance();
        public static CountDownTimer ReloadInstance()
        {
            return _instance = new CountDownTimer(ApplicationConfig.Instance.InspectionTimer);
        }
        public static void LoadParameter(CountDownTimerParameters cnt)
        {
            Instance?.LoadParameter(cnt);
        }
        public static void Stop()
        {
            Instance?.Stop();
        }
        public static void Start()
        {
            Instance?.Start();
        }
        public static void PlaySound()
        {
            Instance?.PlaySound();
        }
    }
}
