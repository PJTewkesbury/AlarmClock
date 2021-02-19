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

            var autoEvent = new AutoResetEvent(false);
            var stateTimer = new Timer(clockDisplay.CheckStatus, autoEvent, 250, 250); // 1000,1000

            // Init Touch Sensor - Use GPIO12 to detect IRQ from Touch Sensor to avoid polling.
            Console.WriteLine("Init Touch Sensor and IRQ on GPIO12");
            I2cConnectionSettings touchSettings = new I2cConnectionSettings(1, 0x29);
            I2cDevice touchI2CDevice = I2cDevice.Create(touchSettings);
            TouchDriver touchDriver = new TouchDriver(touchI2CDevice, gpio,12, true);
            //var autoEvent2 = new AutoResetEvent(false);
            //var touchTimer = new Timer(touchDriver.CheckStatus, autoEvent2, 250, 250); // 1000,1000
            touchDriver.OnTouched += TouchDriver_OnTouched;

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
            }

            Console.WriteLine("All Done");
        }

        private static void TouchDriver_OnTouched(object sender, TouchEventArgs e)
        {
            Console.WriteLine("Touch Event triggered on IRG");
            int t = e.Touched;
            if ((t & 1)==1)
            {
                Console.WriteLine("Toggle Clock Display");
                if (Program.clockDisplay.WhatToDisplay == ClockDisplayDriver.enumShow.Scanning)
                    Program.clockDisplay.WhatToDisplay = ClockDisplayDriver.enumShow.Time;
                else if (Program.clockDisplay.WhatToDisplay == ClockDisplayDriver.enumShow.Time)
                    Program.clockDisplay.WhatToDisplay = ClockDisplayDriver.enumShow.Scanning;
            }

            if ((t & 128) == 128)
            {
                Console.WriteLine("Show Alexa Wake and End");
                Task.Run(() => {
                    Program.ledRing.PlayAnimation(Program.alexaWake);
                    Program.ledRing.PlayAnimation(Program.alexaEnd);
                });
            }
        }
    }

    internal static class Helpers
    {
        public static void WriteReg(this I2cDevice device, Byte reg, Byte value)
        {
            if (device == null)
                return;

            Span<Byte> d = new byte[2];
            d[0] = reg;
            d[1] = value;
            device.Write(d);
        }
        public static Byte ReadReg(this I2cDevice device, Byte reg)
        {
            device.WriteByte(reg);
            return device.ReadByte();
        }
    }
}
