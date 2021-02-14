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
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Text;
using AlarmClock;

namespace AlarmClockPi
{
    class Program
    {
        public static GpioController gpio { get; private set; } = null;
        public static Apa102 apa102 { get; private set; } = null;

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
            gpio.OpenPin(5, PinMode.Output);
            gpio.Write(5, PinValue.High);

            using SpiDevice spiDevice = SpiDevice.Create(new SpiConnectionSettings(0, 0)
            {
                ClockFrequency = 20_000_000,
                DataFlow = DataFlow.MsbFirst,
                Mode = SpiMode.Mode0 // ensure data is ready at clock rising edge
            });
            Iot.Device.Apa102.Apa102 LEDRing = new Iot.Device.Apa102.Apa102(spiDevice, 12);
            while (true)
            {
                for (var i = 0; i < LEDRing.Pixels.Length; i++)
                {
                    LEDRing.Pixels[i] = Color.FromArgb(255, random.Next(256), random.Next(256), random.Next(256));
                }

                LEDRing.Flush();
                Thread.Sleep(1000);
                if (Console.KeyAvailable)
                    break;
            }
            LEDRing.Dispose();
            LEDRing = null;

            // Init LED Ring
            apa102 = new Apa102();
            apa102.PowerLEDS(gpio);
            apa102.ClearPixels();

            // Init Amplifier and set gain to 30db
            I2cConnectionSettings ampSettings = new I2cConnectionSettings(1, 0x58);
            I2cDevice ampDevice = I2cDevice.Create(ampSettings);
            AmpDriver amp = new AmpDriver(ampDevice);
            amp.SetGain(30);

            // Init Clock Display
            I2cConnectionSettings LedDisplaySettings = new I2cConnectionSettings(1, 0x70);
            I2cDevice LedDisplayDevice = I2cDevice.Create(LedDisplaySettings);


            //Large4Digit7SegmentDisplay d = new Large4Digit7SegmentDisplay(LedDisplayDevice);
            LED4x7SegDisplay d = new LED4x7SegDisplay(LedDisplayDevice);
            d.Brightness = 1;
            d.BlinkRate = BlinkRate.Off;
            d.DisplayOn = true;
            var autoEvent = new AutoResetEvent(false);
            var clockDisplay = new ClockDisplayDriver(d);
            var stateTimer = new Timer(clockDisplay.CheckStatus, autoEvent, 250, 250); // 1000,1000

            // Init Touch Sensor - Use GPIO12 to detect IRQ from Touch Sensor to avoid polling.
            I2cConnectionSettings touchSettings = new I2cConnectionSettings(1, 0x29);
            I2cDevice touchDevice = I2cDevice.Create(touchSettings);
            CAP1188DeviceI2C touch = new CAP1188DeviceI2C(touchDevice);
            touch.InitDevice();
            TouchDriver touchDriver = new TouchDriver(touch, gpio);
            // var stateTimer2 = new Timer(touchDriver.CheckStatus, autoEvent, 250, 250);

            // Touch Sensor IRQ
            gpio.OpenPin(12, PinMode.Input);
            PinChangeEventHandler handler = (object sender, PinValueChangedEventArgs args) =>
            {
                Console.WriteLine($"IRQ on GPIO {args.PinNumber} {args.ChangeType.ToString()}");
                touchDriver.CheckStatus(null);
            };
            gpio.RegisterCallbackForPinValueChangedEvent(12, PinEventTypes.Falling, handler);

            try
            {
                apa102.PlayAnimation(alexaWake);
                apa102.PlayAnimation(alexaEnd);

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

            d.DisplayOn = false;
            apa102.Clear();
            apa102.PowerLEDS(gpio, false);

            Console.WriteLine("All Done. Press Enter to close");
        }

        // Alexa LED States
        // https://developer.amazon.com/en-US/docs/alexa/alexa-voice-service/ux-design-attention.html
        static void SetClockPixel(Apa102 apa, int v, double div, Color color)
        {
            double d = v / div;
            double f = Math.Floor(d);

            double g = (f + 1) - d;
            double h = f + 1;
            double i = 1 - g;

            int ledA = (int)f;
            int ledB = (int)h;
            if (ledA > 11)
                ledA = 0;
            if (ledB > 11)
                ledB = 0;

            int a = color.A;
            int a1 = ((int)(a * g));
            if (a1 > 255)
                a1 = 255;
            apa.MergeColor(Color.FromArgb(a1, color.R, color.G, color.B), ledA);

            int a2 = ((int)(a * i));
            if (a2 > 255)
                a2 = 255;
            apa.MergeColor(Color.FromArgb(a2, color.R, color.G, color.B), ledB);
        }

        void PinChangeEventHandler(object sender, PinValueChangedEventArgs args)
        {
            Console.WriteLine($"Touch IRQ Event : {args.ChangeType.ToString()} on Pin {args.PinNumber.ToString()}");
        }
    }

    class AmpDriver
    {
        I2cDevice ampDevice;

        public AmpDriver(I2cDevice i2cDevice)
        {
            this.ampDevice = i2cDevice;

            byte ControlReg = ampDevice.ReadReg(0x01);
            if ((ControlReg & 0b11000000) != 0b11000000)
            {
                ampDevice.WriteReg(0x01, 0b11000011);
            }

            int gain = GetGain();
            SetGain(10);
        }

        public int GetGain()
        {
            byte gainValue = ampDevice.ReadReg(0x05);
            int gain = gainValue & 0b11111;

            if ((gainValue & 0b100000) == 0b100000)
            {
                gain = gain * -1;
            }

            return gain;
        }

        public void SetGain(int gain)
        {
            Span<byte> writeRegData = new byte[2];

            int g = Math.Abs(gain);
            byte gainValue = (byte)g;
            if (gain < -28 || gain > 30)
                throw new ArgumentOutOfRangeException(nameof(gain), "Gain is value between -28 and 30db");

            // If gain <0 then add a sign bit at bit 5. Bits 0-4 hold gain value.
            if (gain < 0)
                gainValue = (byte)(gainValue + 32);

            ampDevice.WriteReg(0x05, gainValue);
        }
    }

    class TouchDriver
    {
        CAP1188DeviceI2C touch;

        GpioController gpio;
        //int TouchIRQPinNumber = 12;

        public TouchDriver(CAP1188DeviceI2C touch, GpioController gpio)
        {
            this.touch = touch;

            this.gpio = gpio;
            //gpio.OpenPin(TouchIRQPinNumber, PinMode.Input);
        }

        public void CheckStatus(Object stateInfo)
        {
            AutoResetEvent autoEvent = (AutoResetEvent)stateInfo;

            PinValue gpio12 = gpio.Read(12);
            Console.WriteLine($"GPIO 12 : {(gpio12 == PinValue.High ? "High" : "Low")}");

            byte b = touch.touched();
            if (b == 0)
                return;

            if ((b & 0x1) == 1)
            {
                Task.Run(() =>
                {
                    Program.apa102.PlayAnimation(Program.alexaWake);
                    Program.apa102.PlayAnimation(Program.alexaThinking);
                    Thread.Sleep(2000);
                    Program.apa102.PlayAnimation(Program.alexaTalking);
                    Thread.Sleep(1000);
                    Program.apa102.PlayAnimation(Program.alexaEnd);
                    Program.apa102.Clear();
                }
                );
            }
            if ((b & 0x80) == 0x80)
            {
                Task.Run(() =>
                {
                    System.Diagnostics.Process.Start("aplay", "-D plughw:0,0 /Apps/magic.wav");
                });
            }

            Console.WriteLine($"Touch : {b}");
        }
    }

    class ClockDisplayDriver
    {
        LED4x7SegDisplay display;
        int c = 0;

        LED7SegAnimation scanning = LED7SegAnimation.LoadAnimationFile(4, LED4x7SegAnimations.Scanning);

        public ClockDisplayDriver(LED4x7SegDisplay display)
        {
            this.display = display;
        }

        public void PlayAnimation(LED7SegAnimation animation)
        {
            if (animation == null)
                return;

            foreach (var f in animation.Frames)
            {
                if (f.Loop)
                    continue;

                System.Threading.Thread.Sleep(f.Duration);
            }
        }

        public enum enumShow { Date, Time, IPAddress, NoWifi, Scanning };

        public enumShow WhatToDisplay { get; set; } = enumShow.Scanning;

        // This method is called by the timer delegate.
        public void CheckStatus(Object stateInfo)
        {
            AutoResetEvent autoEvent = (AutoResetEvent)stateInfo;

            if (WhatToDisplay == enumShow.Date)
            {
                display.Write(DateTime.Now.ToString("ddMM"));
            }

            if (WhatToDisplay == enumShow.Scanning)
            {
                ReadOnlySpan<Segment> span = scanning.Frames[scanning.Index].Frame;
                display.Write(span, 0);
                scanning.Index++;
                if (scanning.Index >= scanning.Frames.Count)
                    scanning.Index = 0;
            }

            if (WhatToDisplay == enumShow.Time)
            {
                DateTime dt = DateTime.Now;
                Console.WriteLine($"Showing Time {dt.ToString("HH:mm")} ");

                // This should change depending on time of day and daylight hours
                // display.Brightness = (byte)(1 + ((byte)(dt.Second / 4)));

                string sTime = dt.ToString("HHmm");
                Segment[] s = new Segment[display.NumberOfDigits];
                for (int i = 0; i < display.NumberOfDigits; i++)
                {
                    s[i] = GetSegmentForNumber(sTime[i] - 48);
                }

                ReadOnlySpan<Segment> span = new ReadOnlySpan<Segment>(s);
                display.Write(span, 0);

                // Make center colon toggle on and off with seconds on clock.
                Dot dot = Dot.Off;
                if ((dt.Second % 2) == 0)
                    dot |= Dot.CenterColon;
                display.Dots = dot;
            }

            // autoEvent.Set();
        }

        Segment GetSegmentForNumber(int i)
        {
            switch (i)
            {
                case 0:
                    return Segment.Top | Segment.TopLeft | Segment.BottomLeft | Segment.Bottom | Segment.BottomRight | Segment.TopRight;
                case 1:
                    return Segment.BottomRight | Segment.TopRight;
                case 2:
                    return Segment.Top | Segment.Middle | Segment.BottomLeft | Segment.Bottom | Segment.TopRight;
                case 3:
                    return Segment.Top | Segment.Middle | Segment.BottomRight | Segment.Bottom | Segment.TopRight;
                case 4:
                    return Segment.TopLeft | Segment.Middle | Segment.BottomRight | Segment.TopRight;
                case 5:
                    return Segment.Top | Segment.TopLeft | Segment.Middle | Segment.Bottom | Segment.BottomRight;
                case 6:
                    return Segment.Top | Segment.TopLeft | Segment.BottomLeft | Segment.Bottom | Segment.BottomRight | Segment.Middle;
                case 7:
                    return Segment.Top | Segment.BottomRight | Segment.TopRight;
                case 8:
                    return Segment.Top | Segment.TopLeft | Segment.BottomLeft | Segment.Bottom | Segment.BottomRight | Segment.TopRight | Segment.Middle;
                case 9:
                    return Segment.Top | Segment.TopLeft | Segment.BottomRight | Segment.TopRight | Segment.Middle;
                default:
                    return Segment.None;
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
