using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Configuration;
using AlarmClock.Voice;
using AlarmClock.Hardware;
using System.Device.Gpio;
using System.Device.I2c;
using System.Diagnostics.Eventing.Reader;
using OpenTK.Graphics.OpenGL;
using Iot.Device.Rtc;
using System.Timers;
using System.Text;
using Humanizer;
using OpenTK.Graphics.ES20;
using System.Drawing;

namespace AlarmClock
{
    public class AlarmClock : IDisposable
    {
        public static AlarmClockState alarmClockState = new AlarmClockState();

        public static GpioController gpio { get; private set; } = null;
        public static LedRing ledRing { get; private set; } = null;

        public static LEDRingAnimation alexaWake = LEDRingAnimation.LoadAnimationFile(12, Animations.AlexaWake, "AlexaWake");
        public static LEDRingAnimation alexaThinking = LEDRingAnimation.LoadAnimationFile(12, Animations.AlexaThinking, "AlexaThinking");
        public static LEDRingAnimation alexaSpeaking = LEDRingAnimation.LoadAnimationFile(12, Animations.AlexaSpeaking, "AlexaSpeaking");
        public static LEDRingAnimation alexaEnd = LEDRingAnimation.LoadAnimationFile(12, Animations.AlexaEnd, "AlexaEnd");

        public static LEDRingAnimation JarvisWake = LEDRingAnimation.LoadAnimationFile(12, Animations.JarvisWake, "JarvisWake");
        public static LEDRingAnimation JarvisEnd = LEDRingAnimation.LoadAnimationFile(12, Animations.JarvisEnd, "JarvisEnd");
        public static LEDRingAnimation JarvisListen = LEDRingAnimation.LoadAnimationFile(12, Animations.JarvisListen, "JarvisListen");

        public static ClockDisplayDriver clockDisplay;
        public static TouchDriver touchDriver;
        public static IDisposable touchObservable;
        public static Audio audio;        

        IConfiguration config;
        public AlarmClock(IConfiguration config)
        {
            this.config = config;        
        }

        public void Init()
        {
            Console.WriteLine("Init Volume");
          
            // Init GPIO - We use Pin12 for Touch Sensor IRQ and pin 5 to power the LED Ring on Respeaker
            Console.WriteLine("Init GPIO Controller");
            gpio = new GpioController();

            // Init LED Ring
            Console.WriteLine("Init LEDRing,");
            ledRing = new LedRing(gpio, 12);
            ledRing.SetAllLEDsToColor(Color.Green);

            // Init Amplifier and set gain to 30db
            Console.WriteLine("Init Amplifier");
            I2cConnectionSettings ampSettings = new I2cConnectionSettings(1, 0x58);
            I2cDevice ampDevice = I2cDevice.Create(ampSettings);
            AmpDriver amp = new AmpDriver(ampDevice, 30);

            // Init Clock Display
            Console.WriteLine("Init Clock Display");
            I2cConnectionSettings LedDisplaySettings = new I2cConnectionSettings(1, 0x70);
            I2cDevice i2cDevice4x7Display = I2cDevice.Create(LedDisplaySettings);
            clockDisplay = new ClockDisplayDriver(i2cDevice4x7Display);
            clockDisplay.WhatToDisplay = ClockDisplayDriver.enumShow.Time;

            // Init Touch Sensor - Use GPIO12 to detect IRQ from Touch Sensor to avoid polling.
            Console.WriteLine("Init Touch Sensor and IRQ on GPIO12");
            I2cConnectionSettings touchSettings = new I2cConnectionSettings(1, 0x29);
            I2cDevice touchI2CDevice = I2cDevice.Create(touchSettings);
            TouchDriver touchDriver = new TouchDriver(touchI2CDevice, gpio, 12, true);
            touchObservable = touchDriver.rxTouch.Subscribe(r =>
            {
                ProcessTouch(r);
            });

            Console.WriteLine("Show User init completed");
            audio = new Audio();                        
            Task.Run(() =>
            {
                // Play Animaition and sound if time is between 6am and 10pm
                // If we get power outage over night we do not want to play sound or light as it might wake us up.
                if (DateTime.Now.Hour > 6 && DateTime.Now.Hour < 22)
                {
                    ledRing.PlayAnimation(alexaWake);
                    audio.PlayMP3("./Sounds/ful/ful_system_alerts_melodic_01_short.wav");
                    while (audio.IsMusicFilePlaying())
                    {
                        Thread.Sleep(10);
                    }
                    ledRing.PlayAnimation(alexaEnd);
                }
            });
        }

        void OnTimedEvent(object sender, ElapsedEventArgs e)
        {            
            if (AlarmClock.alarmClockState.AlarmEnabled && AlarmClock.alarmClockState.Hour == e.SignalTime.Hour && AlarmClock.alarmClockState.Minute == e.SignalTime.Minute && e.SignalTime.Second==0)
            {
                Console.WriteLine($"Alarm Time - Playing radio - {AlarmClock.alarmClockState.Hour} {AlarmClock.alarmClockState.Minute}");
                if (!AlarmClock.audio.RadioIsPlaying())
                    AlarmClock.PlayRadio();
                if (alarmClockState.ShowLights)
                {
                    Console.WriteLine($"Alarm Time - Showing lights");
                    ledRing.SetAllLEDsToColor(System.Drawing.Color.White);
                }
            }
        }
        
        public void Run()
        {
            System.Timers.Timer t = new System.Timers.Timer(TimeSpan.FromSeconds(1));
            t.Elapsed += new ElapsedEventHandler(OnTimedEvent);
            t.AutoReset = true;
            t.Enabled = true;
            t.Start();

            // Main loop
            try
            {
                do
                {                   
                    Thread.Sleep(10);
                    if (Program.cancellationToken.IsCancellationRequested)
                        break;                    
                }
                while (true);

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
            finally
            {
                Console.WriteLine("Shutdown devices - Clock Display, Touch Driver, LedRing & GPIO");

                // Clear LED and LEDRing displays.
                clockDisplay.WhatToDisplay = ClockDisplayDriver.enumShow.Blank;
                ledRing.ClearPixels();

                if (clockDisplay!=null) clockDisplay.Dispose();
                if (touchDriver!=null) touchDriver.Dispose();
                if (ledRing!=null)ledRing.Dispose();
                if (gpio!=null) gpio.Dispose();                
                if (touchObservable!=null) touchObservable.Dispose();
                if (audio!=null) audio.Dispose();
            }

            Console.WriteLine("All Done");
        }

        static int aniCount = 0;
        public static object RingLock = new object();
        public static void ProcessTouch(byte t)
        {
            Console.WriteLine($"Debounced Touch : {t}");
            if ((t & 128) == 128)
            {
                Console.WriteLine("Play/Pause Radio");
                if (audio.RadioIsPlaying())
                    audio.StopRadio();
                else 
                    audio.PlayRadio();
                return;
            }

            if ((t & 1) == 1)
            {
                Console.WriteLine("Toggle Clock Display");
                // Jarvis.SayText("Toggle display mode", null);

                if (clockDisplay.WhatToDisplay == ClockDisplayDriver.enumShow.Animation)
                    clockDisplay.WhatToDisplay = ClockDisplayDriver.enumShow.Time;
                else if (clockDisplay.WhatToDisplay == ClockDisplayDriver.enumShow.Time)
                {
                    switch (aniCount)
                    {
                        case 0:
                            clockDisplay.PlayAnimation(LED7SegAnimation.LoadAnimationFile(4, LED4x7SegAnimations.LEDTest));
                            break;
                        case 1:
                            clockDisplay.PlayAnimation(LED7SegAnimation.LoadAnimationFile(4, LED4x7SegAnimations.Scanning));
                            break;
                        case 2:
                            clockDisplay.PlayAnimation(LED7SegAnimation.LoadAnimationFile(4, LED4x7SegAnimations.Scanning2));
                            break;
                    }
                    aniCount++;
                    if (aniCount > 2)
                        aniCount = 0;
                }
                return;
            }

            if ((t & 128) == 128)
            {
                Console.WriteLine("Show Alexa Wake and End");
                Task.Run(() =>
                {
                    lock (RingLock)
                    {
                        ledRing.PlayAnimation(alexaWake);
                        ledRing.PlayAnimation(alexaEnd);
                    }
                });
                return;
            }
        }
      
        public static void PlayRadio()
        {
            Console.WriteLine("Play Radio");
            Task.Run(() =>
            {
                try
                {
                    if (audio.RadioIsPlaying())
                        audio.SetRadioVolume();
                    else
                        audio.PlayRadio();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
                }
            });
        }

        public static void PauseRadio()
        {
            Console.WriteLine("Pause Radio");
            Task.Run(() =>
            {
                try
                {
                    if (audio.RadioIsPlaying())
                        audio.SetRadioVolume(0.0f);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
                }
            });
        }

        public static void StopRadio()
        {
            Task.Run(() =>
            {
                Console.WriteLine("Stop Radio");
                audio.StopRadio();
            });
        }

        public static void QuiteVolume()
        {
            Console.WriteLine($"Making volume quite");
            if (audio.RadioIsPlaying())
            {
                audio.MutePlayback();
            }            
        }

        public static void NormalVolume()
        {
            audio.UnmutePlayback();
        }

        internal static void ChangeVolume(int Direction, Dictionary<string, string> slots = null)
        {            
            float volume = audio.GetVolume();
            float volumeChange = 0.1f;
            Console.WriteLine($"Changing Volume from {volume}");

            if (slots != null && slots.ContainsKey("volumeChange"))
            {
                volumeChange = Convert.ToInt32(slots["volumeChange"].Replace("%", ""))/100.0f;
            }
            Console.WriteLine($"Change Volume by {volumeChange}");

            if (Direction == 0)
                volume = volumeChange;
            if (Direction > 0)
                volume += volumeChange;
            if (Direction < 0)
                volume -= volumeChange;

            if (volume < 0.0f)
                volume = 0.0f;

            if (volume > 1.0f)
                volume = 1.0f;
            
            audio.SetVolume(volume);
            Console.WriteLine($"Changing Volume to {volume}");
        }

        public void Dispose()
        {
            if (clockDisplay != null)
            {
                clockDisplay.ClearDisplay();
                clockDisplay.Dispose();
            }
            clockDisplay = null;

            if (ledRing != null)
            {
                ledRing.ClearPixels();
                ledRing.Dispose();
            }
            ledRing = null;

            if (touchDriver!=null)
                touchDriver.Dispose();

            touchDriver = null;

            if (gpio!=null)
                gpio.Dispose();
            gpio = null;

            if (audio != null)
            {
                audio.StopRadio();
                audio.SetVolume(0.5f);
                audio.Dispose();
            }
            audio = null;
        }

        internal static void SetAlarmTime(int hour, int minute)
        {
            AlarmClock.alarmClockState.Hour = hour;
            AlarmClock.alarmClockState.Minute= minute;
        }
    }

    public class AlarmClockState
    {
        public bool AlarmEnabled { get; set; } = false;
        public int Hour { get; set; } = 7;
        public int Minute { get; set; } = 0;
        public string AlarmPlayList { get; set; } = "https://edge-audio-03-gos2.sharp-stream.com/ucbuk.mp3?device=ukradioplayer&=&&___cb=479109455";
        public int SnoozeTime { get; set; } = 10;
        public int DurationTime{ get; set; } = 60;
        public bool ShowLights{ get; set; } = true;

        public AlarmClockState() { }

        public override string ToString()
        {
            return $"AlarmEnabled:{(AlarmEnabled?"Yes":"No")}, Hour:{Hour}, Minute:{Minute}, ShowLights:{ShowLights} - {DateTime.Now.Hour}:{DateTime.Now.Minute}";
        }

        public string GetAlarmTimeAsSpeechText()
        {
            StringBuilder sb = new StringBuilder();

            // Mintute To/Past Hour AM/PM
            int m = Minute;            
            if (m > 0)
            {
                if (m > 30)
                    m = 60 - m;

                string sm = m.ToString();
                if (m == 30)
                    sm = " half ";
                if (m == 15)
                    sm = " quarter ";
                sb.Append(sm);
                if (m > 30)
                    sb.Append(" to ");
                else
                    sb.Append(" past ");
            }

            int h = Hour;
            if (h > 12)
                h = h - 12;
            sb.Append(h.ToWords()+" ");

            if (Hour>12)
                sb.Append(" PM");
            else 
                sb.Append(" AM");

            return sb.ToString();
        }

        internal void Save()
        {
            /// TODO : Save settings to file.
        }
    }
}