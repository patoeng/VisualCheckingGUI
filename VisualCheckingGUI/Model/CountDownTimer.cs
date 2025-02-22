using Hmi.Helpers.Enum;
using System;
using System.IO;
using System.Media;
using System.Threading;
using Timer = System.Windows.Forms.Timer;

namespace VisualCheckingGUI.Model
{
    public class CountDownTimer
    {
        public event EventHandler<string> CountDownStopped;
        public event EventHandler<string> CountDownStarted;
        public event EventHandler<string> CountDownReached;
        public event EventHandler<int> CountDownTick;
        public CountDownTimer(CountDownTimerParameters param)
        {
            _param = param;
            _timer = new Timer
            {
                Interval = 1000
            };
            _timer.Tick += TimerTick;
        }

        public void LoadParameter(CountDownTimerParameters cnt)
        {
            _param = cnt;
        }

        private void TimerTick(object sender, EventArgs e)
        {
            Tick++;
            var thread = new Thread(x => CountDownTick?.Invoke(this, CountDown));
            thread.Start();
            if (Tick >= _param.Interval)
            {
                Stop();
                CountDownReached?.Invoke(this, $"{_param.Name} :Count Down Timer Reached!");                
            }
        }
        public void Start()
        {
            Tick = 0;
            if (_param.Enable == YesNo.Yes)
            {
                _timer.Start();
                CountDownStarted?.Invoke(this, $"{_param.Name} :Count Down Timer Started!");
            }
            else
            {
                throw new Exception($"Trying to start disabled {_param.Name} : Count Down Timer");
            }
           
        }
        public void Stop()
        {
            _timer.Stop();
            CountDownStopped?.Invoke(this, $"{_param.Name} :Count Down Timer Stopped!");
           
        }
        public bool Started => _timer.Enabled;
        public int Tick { get; set; } = 0;
        public int CountDown => _param.Interval - Tick;

        private Timer _timer;
        private CountDownTimerParameters _param;
        public CountDownTimerParameters Parameters => _param;

        public void PlaySound(int repeat=1)
        {
            if (File.Exists(_param.SoundLocation))
            {
                SoundPlayer my_wave_file = new SoundPlayer(_param.SoundLocation);
                int i = 0;
                while (i < repeat)
                {
                    i++;
                    my_wave_file.PlaySync();
                }
               
            }
        }
        public void PlaySoundAsync()
        {
            if (File.Exists(_param.SoundLocation))
            {
                SoundPlayer my_wave_file = new SoundPlayer(_param.SoundLocation);
                my_wave_file.Play();
            }
        }
    }
}
