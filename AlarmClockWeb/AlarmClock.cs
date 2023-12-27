﻿using System;
using System.Device.Gpio;
using System.Device.I2c;
using System.Threading;
using System.Threading.Tasks;
using AlarmClock;
using System.Net;
using Libmpc;
using System.Collections.Generic;
using Alsa.Net;
using System.Net.Http;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace AlarmClockPi
{
    public class AlarmClock :IDisposable
    {
        public static GpioController gpio { get; private set; } = null;
        public static LedRing ledRing { get; private set; } = null;

        public static LEDRingAnimation alexaWake = LEDRingAnimation.LoadAnimationFile(12, Animations.AlexaWake, "AlexaWake");
        public static LEDRingAnimation alexaThinking = LEDRingAnimation.LoadAnimationFile(12, Animations.AlexaThinking, "AlexaThinking");
        public static LEDRingAnimation alexaSpeaking = LEDRingAnimation.LoadAnimationFile(12, Animations.AlexaSpeaking, "AlexaSpeaking");
        public static LEDRingAnimation alexaEnd = LEDRingAnimation.LoadAnimationFile(12, Animations.AlexaEnd, "AlexaEnd");

        public static LEDRingAnimation JarvisWake = LEDRingAnimation.LoadAnimationFile(12, Animations.JarvisWake, "JarvisWake");
        public static LEDRingAnimation JarvisEnd = LEDRingAnimation.LoadAnimationFile(12, Animations.JarvisEnd, "JarvisEnd");

        public static ClockDisplayDriver clockDisplay;
        public static TouchDriver touchDriver;
        public static IDisposable touchObservable;

        public static Libmpc.Mpc mpc;
        // public static MQTT mqtt;

        public static ISoundDevice alsaDevice = null;
        public static long CurrentVolume = 0;
        /// <summary>
        /// Volume as percentage
        /// </summary>
        public static long volume
        {
            get
            {
                double v = Convert.ToDouble(alsaDevice.PlaybackVolume) / 655.35;
                Console.WriteLine($"Current Volume = {v}% (Raw = {alsaDevice.PlaybackVolume})");
                return Convert.ToInt64(v);
            }
            set
            {
                double v = (long)((double)value * (double)655.35);
                alsaDevice.PlaybackVolume = (long)((double)value * (double)655.35);
                Console.WriteLine($"Set Current Volume to = {value}% {v} (Raw = {alsaDevice.PlaybackVolume})");
                Console.WriteLine($"Current Volume = {value}%");
            }
        }

        public static string Topic = "AlarmClock";

        IConfiguration config;
        public AlarmClock(IConfiguration config)
        {
            this.config = config;
        }

        public void Init()
        {
            Console.WriteLine("Init Volume");
            alsaDevice = AlsaDeviceBuilder.Create(new SoundDeviceSettings());
            CurrentVolume = volume;            
            Console.WriteLine($"CurrentVolume Raw = {alsaDevice.PlaybackVolume}");
            Console.WriteLine($"CurrentVolume = {volume}");
            volume = 60;            
            Console.WriteLine($"CurrentVolume now  = {volume}");

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

            // Init the music player so we can play music when it is time for the alarm to go off.
            var mpdEndpoint = new IPEndPoint(IPAddress.Loopback, 6600);
            mpc = new Libmpc.Mpc();
            mpc.OnConnected += Mpc_OnConnected;
            mpc.OnDisconnected += Mpc_OnDisconnected;
            mpc.Connection = new Libmpc.MpcConnection(mpdEndpoint);
            mpc.Connection.AutoConnect = true;                    

            Console.WriteLine("Show Alexa wait and end");
            Task.Run(() =>
            {
                // Set Master Vol to 50
                volume = 50;

                // Set MPD Vol to 50%
                mpc.Connection.Connect();
                mpc.SetVol(50);
                mpc.Connection.Disconnect();

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
                    // Thread.Yield();                    
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
            }

            Console.WriteLine("All Done");
        }

        private static void KeepAliveCallback(object state)
        {
            var mpdStatus = AlarmClock.mpc.Status();
            // Console.WriteLine($"{mpdStatus.ToString()}");
            // AlarmClock.mqtt.SendMessage(AlarmClock.Topic, "MPD StillAlive");
        }

        private static void Mpc_OnDisconnected(Libmpc.Mpc connection)
        {
            Console.WriteLine("MPC Disconnected");
            //mpc.Connection.Connect();
        }

        private static void Mpc_OnConnected(Libmpc.Mpc connection)
        {
            Console.WriteLine("MPC Connected");
        }

        static int aniCount = 0;
        public static object RingLock = new object();
        public static void ProcessTouch(byte t)
        {
            Console.WriteLine($"Debounced Touch : {t}");
            if ((t & 128) == 128)
            {
                Console.WriteLine("Play/Pause Radio");
                if (mpc.Status().State == MpdState.Play)
                    StopRadio();
                else
                    PlayRadio();
                return;
            }

            if ((t & 1) == 1)
            {
                Console.WriteLine("Toggle Clock Display");                
                Jarvis.SayText("Toggle display mode", null);

                if (AlarmClock.clockDisplay.WhatToDisplay == ClockDisplayDriver.enumShow.Animation)
                    AlarmClock.clockDisplay.WhatToDisplay = ClockDisplayDriver.enumShow.Time;
                else if (AlarmClock.clockDisplay.WhatToDisplay == ClockDisplayDriver.enumShow.Time)
                {
                    switch (aniCount)
                    {
                        case 0:
                            AlarmClock.clockDisplay.PlayAnimation(LED7SegAnimation.LoadAnimationFile(4, LED4x7SegAnimations.LEDTest));
                            break;
                        case 1:
                            AlarmClock.clockDisplay.PlayAnimation(LED7SegAnimation.LoadAnimationFile(4, LED4x7SegAnimations.Scanning));
                            break;
                        case 2:
                            AlarmClock.clockDisplay.PlayAnimation(LED7SegAnimation.LoadAnimationFile(4, LED4x7SegAnimations.Scanning2));
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
                        AlarmClock.ledRing.PlayAnimation(AlarmClock.alexaWake);
                        AlarmClock.ledRing.PlayAnimation(AlarmClock.alexaEnd);
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
                ledRing.PlayAnimation(alexaWake);
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
                if (mpc.Connected == false)
                    mpc.Connection.Connect();

                if (mpc.Status().State != MpdState.Pause)
                    mpc.Pause(true);

                mpc.Connection.Disconnect();
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
                if (mpc.Connected == false)
                    mpc.Connection.Connect();

                if (mpc.Status().State != MpdState.Play)
                {
                    // Remove old playlist
                    mpc.Clear();

                    // Add UCB1        
                    mpc.Add("https://edge-audio-04-thn.sharp-stream.com/ucbuk.mp3?device=ukradioplayer");
                    mpc.Play();
                }
                mpc.Connection.Disconnect();
            });
        }

        public static void StopRadio()
        {
            Task.Run(() => { 
                Console.WriteLine("Stop Radio");
            if (mpc.Connected == false)
                mpc.Connection.Connect();
            if (mpc.Status().State != MpdState.Stop)
                mpc.Stop();
                mpc.Connection.Disconnect();
            }); 
        }

        public static void QuiteVolume()
        {
            Console.WriteLine($"Making volume quite");
            if (volume > 20)
            {
                CurrentVolume = volume;
                volume = 20;
            }
        }

        public static void NormalVolume()
        {
            Console.WriteLine($"Restore volume to previous level of {volume}");
            volume = CurrentVolume;
        }

        internal static void ChangeVolume(int Direction, Dictionary<string, string> slots = null)
        {
            Console.WriteLine($"Changing Volume from {volume}");

            int volumeChange = 10;

            if (slots != null && slots.ContainsKey("volumeChange"))
            {
                volumeChange = Convert.ToInt32(slots["volumeChange"].Replace("%",""));
            }
            Console.WriteLine($"Change Volume by {volumeChange}");

            if (Direction == 0)
                volume = volumeChange;
            if (Direction > 0)
                volume += volumeChange;
            if (Direction < 0)
                volume -= volumeChange;

            if (volume < 0)
                volume = 0;

            if (volume > 100)
                volume = 100;

            CurrentVolume = volume;

            Console.WriteLine($"Changing Volume to {volume}");
        }     

        public void Dispose()
        {
            clockDisplay.Dispose();
            clockDisplay=null;

            ledRing.Dispose();
            ledRing=null;

            touchDriver.Dispose();
            touchDriver=null;

            gpio.Dispose();
            gpio=null;
        }
    }
}