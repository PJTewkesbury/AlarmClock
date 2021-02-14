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

        static void Main(string[] args)
        {
            Console.WriteLine($"AlarmClock Test V1.2");
            var random = new Random();

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

            // Init GPIO - We use Pin12 for Touch Sensor IRQ and pin 5 to power the LED Ring on Respeaker
            gpio = new GpioController();

            // Init LED Ring
            ledRing = new LedRing(gpio,12);

            // Init Amplifier and set gain to 30db
            I2cConnectionSettings ampSettings = new I2cConnectionSettings(1, 0x58);
            I2cDevice ampDevice = I2cDevice.Create(ampSettings);
            AmpDriver amp = new AmpDriver(ampDevice, 30);

            // Init Clock Display
            I2cConnectionSettings LedDisplaySettings = new I2cConnectionSettings(1, 0x70);
            I2cDevice i2cDevice4x7Display = I2cDevice.Create(LedDisplaySettings);
            var clockDisplay = new ClockDisplayDriver(i2cDevice4x7Display);

            var autoEvent = new AutoResetEvent(false);
            var stateTimer = new Timer(clockDisplay.CheckStatus, autoEvent, 250, 250); // 1000,1000

            // Init Touch Sensor - Use GPIO12 to detect IRQ from Touch Sensor to avoid polling.
            I2cConnectionSettings touchSettings = new I2cConnectionSettings(1, 0x29);
            I2cDevice touchI2CDevice = I2cDevice.Create(touchSettings);
            TouchDriver touchDriver = new TouchDriver(touchI2CDevice, gpio,12);

            try
            {
                ledRing.PlayAnimation(alexaWake);
                ledRing.PlayAnimation(alexaEnd);

                // Wait for key press, but do not block threads from running or events from firing.
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

            clockDisplay.Dispose();
            touchDriver.Dispose();
            ledRing.Dispose();

            Console.WriteLine("All Done. Press Enter to close");
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
