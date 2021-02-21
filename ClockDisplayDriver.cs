using System;
using System.Threading;
using Iot.Device.Display;
using AlarmClock;
using System.Device.I2c;
using System.Threading.Tasks;

namespace AlarmClockPi
{
    public class ClockDisplayDriver : IDisposable
    {
        LED4x7SegDisplay display;
        int c = 0;
        bool bQuit = false;

        public LED7SegAnimation Animation { get; set; } = LED7SegAnimation.LoadAnimationFile(4, LED4x7SegAnimations.LEDTest);

        public enum enumShow { Date, Time, IPAddress, NoWifi, Animation };

        public enumShow WhatToDisplay { get; set; } = enumShow.Animation;

        private byte _Brightness = 1;
        public byte Brightness
        {
            get
            {
                return _Brightness;
            }
            set
            {
                if (value < 1 && value > 15)
                    throw new ArgumentOutOfRangeException(nameof(Brightness), $"The Brightness should be between 0 and 15");
                _Brightness = value;
            }
        }

        public ClockDisplayDriver(I2cDevice i2CDevice)
        {
            display = new LED4x7SegDisplay(i2CDevice);
            display.Brightness = 1;
            display.BlinkRate = BlinkRate.Off;
            display.DisplayOn = true;

            Task.Run(()=> {
                // Display Pump
                while (bQuit == false)
                {
                    display.Brightness = Brightness;

                    int Duration = 1000;
                    switch(WhatToDisplay)
                    {
                        case enumShow.Date:
                            {
                                display.Write(DateTime.Now.ToString("ddMM"));
                            }
                            break;

                        case enumShow.Animation:
                        {
                                var aniFrame = Animation.Frames[Animation.Index];
                                Duration = aniFrame.Duration;
                                ReadOnlySpan<Segment> span = aniFrame.Frame;
                                display.Write(span, 0);
                                Animation.Index++;
                                if (Animation.Index >= Animation.Frames.Count)
                                    Animation.Index = 0;
                            }
                        break;

                        case enumShow.Time:
                            {
                                DateTime dt = DateTime.Now;
                                // Console.WriteLine($"Showing Time {dt.ToString("HH:mm")} ");

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
                            break;
                    }
                    System.Threading.Thread.Sleep(Duration);
                }
            });
        }

        public void PlayAnimation(LED7SegAnimation animation)
        {
            if (animation == null)
                return;
            Animation = animation;
            Brightness = 15;
            WhatToDisplay = enumShow.Animation;
        }

        // This method is called by the timer delegate.
        public void CheckStatus(Object stateInfo)
        {
            AutoResetEvent autoEvent = (AutoResetEvent)stateInfo;

            if (WhatToDisplay == enumShow.Date)
            {
                display.Write(DateTime.Now.ToString("ddMM"));
            }

            if (WhatToDisplay == enumShow.Animation)
            {
                ReadOnlySpan<Segment> span = Animation.Frames[Animation.Index].Frame;
                display.Write(span, 0);
                Animation.Index++;
                if (Animation.Index >= Animation.Frames.Count)
                    Animation.Index = 0;
            }

            if (WhatToDisplay == enumShow.Time)
            {
                DateTime dt = DateTime.Now;
                // Console.WriteLine($"Showing Time {dt.ToString("HH:mm")} ");

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

        public void Dispose()
        {
            // Stop display pump
            bQuit = true;

            // Blank display & turn off
            display.Write("");
            display.DisplayOn = false;

            display?.Dispose();
            display = null;
        }
    }
}
