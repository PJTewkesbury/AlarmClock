using System;
using System.Threading;
using Iot.Device.Display;
using AlarmClock;
using System.Device.I2c;

namespace AlarmClockPi
{
    public class ClockDisplayDriver : IDisposable
    {
        LED4x7SegDisplay display;
        int c = 0;

        LED7SegAnimation scanning = LED7SegAnimation.LoadAnimationFile(4, LED4x7SegAnimations.LEDTest);

        public ClockDisplayDriver(I2cDevice i2CDevice)
        {
            display = new LED4x7SegDisplay(i2CDevice);
            display.Brightness = 1;
            display.BlinkRate = BlinkRate.Off;
            display.DisplayOn = true;
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

        public void Dispose()
        {
            display.Write("");
            display.DisplayOn = false;
            display?.Dispose();
            display = null;
        }
    }
}
