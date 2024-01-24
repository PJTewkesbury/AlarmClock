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

namespace AlarmClock
{
    public class AlarmClock : IDisposable
    {
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

            // Init Amplifier and set gain to 30db
            Console.WriteLine("Init Amplifier");
            I2cConnectionSettings ampSettings = new I2cConnectionSettings(1, 0x58);
            I2cDevice ampDevice = I2cDevice.Create(ampSettings);
            AmpDriver amp = new AmpDriver(ampDevice, 20);

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
                audio.PlayMP3("./Sounds/ful/ful_system_alerts_melodic_01_short.wav", 1.0f);

                // Play Animaition
                ledRing.PlayAnimation(alexaWake);
                ledRing.PlayAnimation(alexaEnd);                
            });
        }

        public void Run(string[] args)
        {
            // Main loop
            try
            {
                Console.WriteLine("Show Alexa wait and end");
                ledRing.PlayAnimation(alexaWake);
                ledRing.PlayAnimation(alexaEnd);

                do
                {                  
                    Thread.Sleep(10);
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

                clockDisplay.Dispose();
                touchDriver.Dispose();
                ledRing.Dispose();
                gpio.Dispose();                
                touchObservable.Dispose();
                audio.Dispose();
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
                Jarvis.SayText("Toggle display mode", null);

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

        private static void ProcessMQTTMessages(string s)
        {
            Console.WriteLine(s);

            if (s.StartsWith("PicoListen", StringComparison.CurrentCultureIgnoreCase))
            {
                ledRing.PlayAnimation(JarvisWake);
                QuiteVolume();
            }
            if (s.StartsWith("PicoEnd", StringComparison.CurrentCultureIgnoreCase))
            {
                ledRing.PlayAnimation(JarvisEnd);
            }
            if (s.StartsWith("JarvisListen", StringComparison.CurrentCultureIgnoreCase))
            {
                ledRing.PlayAnimation(JarvisListen);
            }

            if (s.StartsWith("PicoEnd", StringComparison.CurrentCultureIgnoreCase))
            {
                ledRing.PlayAnimation(alexaEnd);
                NormalVolume();
            }

            if (s.StartsWith("AlexaWakeup", StringComparison.CurrentCultureIgnoreCase))
            {
                ledRing.PlayAnimation(alexaWake);
            }
            if (s.StartsWith("AlexaListen", StringComparison.CurrentCultureIgnoreCase))
            {
                ledRing.PlayAnimation(JarvisListen);
            }
            if (s.StartsWith("AlexaThinking", StringComparison.CurrentCultureIgnoreCase))
            {
                ledRing.PlayAnimation(alexaThinking);
            }
            if (s.StartsWith("AlexaSpeaking", StringComparison.CurrentCultureIgnoreCase))
            {
                ledRing.PlayAnimation(alexaSpeaking);
            }
            if (s.StartsWith("AlexaEnd", StringComparison.CurrentCultureIgnoreCase))
            {
                ledRing.PlayAnimation(alexaEnd);
            }
            if (s.StartsWith("Startup", StringComparison.CurrentCultureIgnoreCase))
            {
                ledRing.PlayAnimation(alexaWake);
            }

            if (s.StartsWith("Play", StringComparison.CurrentCultureIgnoreCase))
            {                
                PlayRadio();
                return;
            }
            if (s.StartsWith("Pause", StringComparison.CurrentCultureIgnoreCase))
            {
                PauseRadio();
                return;
            }
            if (s.StartsWith("Stop", StringComparison.CurrentCultureIgnoreCase))
            {
                StopRadio();
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
                        audio.SetRadioVolume(0.8f);
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
                audio.SetRadioVolume(0.2f);
            }            
        }

        public static void NormalVolume()
        {            
            audio.SetRadioVolume(0.8f);
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
            clockDisplay.Dispose();
            clockDisplay = null;

            ledRing.Dispose();
            ledRing = null;

            touchDriver.Dispose();
            touchDriver = null;

            gpio.Dispose();
            gpio = null;
        }
    }
}