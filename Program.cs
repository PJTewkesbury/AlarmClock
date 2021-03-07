using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.Device.Spi;
using System.Device.I2c;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using Iot.Device.Display;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using AlarmClock;
using System.Net;
using Libmpc;

namespace AlarmClockPi
{
    class Program
    {
        public static GpioController gpio { get; private set; } = null;
        public static LedRing ledRing { get; private set; } = null;

        public static LEDRingAnimation alexaWake = LEDRingAnimation.LoadAnimationFile(12, Animations.AlexaWake);
        public static LEDRingAnimation alexaThinking = LEDRingAnimation.LoadAnimationFile(12, Animations.AlexaThinking);
        public static LEDRingAnimation alexaTalking = LEDRingAnimation.LoadAnimationFile(12, Animations.AlexaTalking);
        public static LEDRingAnimation alexaEnd = LEDRingAnimation.LoadAnimationFile(12, Animations.AlexaEnd);

        public static ClockDisplayDriver clockDisplay;
        public static Libmpc.Mpc mpc;

        static void Main(string[] args)
        {
            Console.WriteLine($"AlarmClock Test V1.3");
            var random = new Random();

            if (args.Length > 0 && args[0].Equals("Debug", StringComparison.CurrentCultureIgnoreCase))
            {
                DateTime dt = DateTime.Now;
                Console.WriteLine("Waiting for debugger to attach or any key to continue");
                for (; ; )
                {
                    if ((DateTime.Now - dt).TotalMinutes > 2)
                        break;

                    if (Console.KeyAvailable)
                    {
                        Console.ReadKey();
                        break;
                    }

                    if (Debugger.IsAttached)
                        break;
                    System.Threading.Thread.Sleep(1000);
                }
            }

            // Init GPIO - We use Pin12 for Touch Sensor IRQ and pin 5 to power the LED Ring on Respeaker
            Console.WriteLine("Init GPIO Controller");
            gpio = new GpioController();

            // Init LED Ring
            Console.WriteLine("Init LEDRing,");
            ledRing = new LedRing(gpio,12);

            // Init Amplifier and set gain to 30db
            Console.WriteLine("Init Amplifier");
            I2cConnectionSettings ampSettings = new I2cConnectionSettings(1, 0x58);
            I2cDevice ampDevice = I2cDevice.Create(ampSettings);
            AmpDriver amp = new AmpDriver(ampDevice, 10);

            // Init Clock Display
            Console.WriteLine("Init Clock Display");
            I2cConnectionSettings LedDisplaySettings = new I2cConnectionSettings(1, 0x70);
            I2cDevice i2cDevice4x7Display = I2cDevice.Create(LedDisplaySettings);
            clockDisplay = new ClockDisplayDriver(i2cDevice4x7Display);
            clockDisplay.WhatToDisplay = ClockDisplayDriver.enumShow.Time;

            // var autoEvent = new AutoResetEvent(false);
            // var stateTimer = new Timer(clockDisplay.CheckStatus, autoEvent, 250, 250); // 1000,1000

            // Init Touch Sensor - Use GPIO12 to detect IRQ from Touch Sensor to avoid polling.
            Console.WriteLine("Init Touch Sensor and IRQ on GPIO12");
            I2cConnectionSettings touchSettings = new I2cConnectionSettings(1, 0x29);
            I2cDevice touchI2CDevice = I2cDevice.Create(touchSettings);
            TouchDriver touchDriver = new TouchDriver(touchI2CDevice, gpio,12, true);
            //var autoEvent2 = new AutoResetEvent(false);
            //var touchTimer = new Timer(touchDriver.CheckStatus, autoEvent2, 250, 250); // 1000,1000
            // touchDriver.OnTouched += TouchDriver_OnTouched;
            var touchObservable = touchDriver.rxTouch.Subscribe(r=>
            {               
                ProcessTouch(r);
            });           

            // Init the music player so we can play music when it is time for the alarm to go off.
            var mpdEndpoint = new IPEndPoint(IPAddress.Loopback, 6600);
            mpc= new Libmpc.Mpc();
            mpc.OnConnected += Mpc_OnConnected;
            mpc.OnDisconnected += Mpc_OnDisconnected;
            mpc.Connection = new Libmpc.MpcConnection(mpdEndpoint);
            mpc.Connection.AutoConnect = true;

            try
            {
                Console.WriteLine("Show Alexa wait and end");
                ledRing.PlayAnimation(alexaWake);
                ledRing.PlayAnimation(alexaEnd);

                // Wait for key press, but do not block threads from running or events from firing.
                Console.WriteLine("Waiting for key press");
                do
                {
                    if (Console.KeyAvailable)
                        break;
                    System.Threading.Thread.Sleep(100);
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
                clockDisplay.Dispose();
                touchDriver.Dispose();
                ledRing.Dispose();
                gpio.Dispose();
                touchObservable.Dispose();
            }

            Console.WriteLine("All Done");
        }

        private static void Mpc_OnDisconnected(Libmpc.Mpc connection)
        {
            Console.WriteLine("MPC Disconnected");
        }

        private static void Mpc_OnConnected(Libmpc.Mpc connection)
        {
            Console.WriteLine("MPC Connected");

            // Remove old playb list
            connection.Clear();

            // Add UCB1
            connection.Add("http://edge-audio-05-gos2.sharp-stream.com:80/ucbuk.mp3");
        }

        static int aniCount = 0;
        private static void TouchDriver_OnTouched(object sender, TouchEventArgs e)
        {
            Console.WriteLine("Touch Event triggered on IRQ");
        }

        public static object RingLock = new object();
        public static void ProcessTouch(byte t)
        {
            Console.WriteLine($"Debounced Touch : {t}");            
            if ((t & 129)==129)
            {
                if(mpc.Status().State== MpdState.Play)
                    mpc.Stop();
                else
                    mpc.Play();
                return;
            }

            if ((t & 1)==1)
            {
                Console.WriteLine("Toggle Clock Display");

                if (Program.clockDisplay.WhatToDisplay == ClockDisplayDriver.enumShow.Animation)
                    Program.clockDisplay.WhatToDisplay = ClockDisplayDriver.enumShow.Time;
                else if (Program.clockDisplay.WhatToDisplay == ClockDisplayDriver.enumShow.Time)
                {
                    switch (aniCount)
                    {
                        case 0:
                            Program.clockDisplay.PlayAnimation(LED7SegAnimation.LoadAnimationFile(4, LED4x7SegAnimations.LEDTest));
                            break;
                        case 1:
                            Program.clockDisplay.PlayAnimation(LED7SegAnimation.LoadAnimationFile(4, LED4x7SegAnimations.Scanning));
                            break;
                        case 2:
                            Program.clockDisplay.PlayAnimation(LED7SegAnimation.LoadAnimationFile(4, LED4x7SegAnimations.Scanning2));
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
                Task.Run(() => {
                    lock (RingLock)
                    {
                        Program.ledRing.PlayAnimation(Program.alexaWake);
                        Program.ledRing.PlayAnimation(Program.alexaEnd);
                    }
                });
                return;
            }
        }
    }
}
