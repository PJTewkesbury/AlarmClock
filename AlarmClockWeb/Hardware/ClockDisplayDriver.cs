using System;
using System.Threading;
using Iot.Device.Display;
using System.Device.I2c;
using System.Threading.Tasks;
using Innovative.SolarCalculator;

namespace AlarmClock.Hardware
{
    public class ClockDisplayDriver : IDisposable
    {
        LED4x7SegDisplay display;
        // int c = 0;
        bool bQuit = false;

        public LED7SegAnimation Animation { get; set; } = LED7SegAnimation.LoadAnimationFile(4, LED4x7SegAnimations.LEDTest);

        public enum enumShow { Date, Time, IPAddress, NoWifi, Animation, Blank };

        public enumShow WhatToDisplay { get; set; } = enumShow.Animation;

        public bool AlarmOn { get; set; } = true;

        private byte _Brightness = 1;
        public byte Brightness
        {
            get
            {
                return _Brightness;
            }
            set
            {
                if (value < 1)
                    _Brightness = 1;
                if (value > 15)
                    _Brightness = 15;
                else
                    _Brightness = value;
            }
        }

        public ClockDisplayDriver(I2cDevice i2CDevice)
        {
            display = new LED4x7SegDisplay(i2CDevice);
            display.Brightness = 1;
            display.BlinkRate = BlinkRate.Off;
            display.DisplayOn = true;

            Task.Run(() =>
            {
                // Display Pump
                while (bQuit == false)
                {
                    DateTime n = DateTime.Now;
                    TimeZoneInfo tz = TimeZoneInfo.Local;
                    SolarTimes solarTimes = new SolarTimes(n.Date, 53.400788, -2.078266);        // Lat Long for Marple, UK
                    DateTime sunrise = TimeZoneInfo.ConvertTimeFromUtc(solarTimes.Sunrise.ToUniversalTime(), tz);
                    DateTime sunset = TimeZoneInfo.ConvertTimeFromUtc(solarTimes.Sunset.ToUniversalTime(), tz);

                    double TimeToSunRise = (n - sunrise).TotalMinutes;
                    double TimeToSunSet = (n - sunset).TotalMinutes;

                    if (TimeToSunRise >= 0 && TimeToSunRise < 31)
                    {
                        int iTimeToSunRise = (int)TimeToSunRise;
                        iTimeToSunRise = iTimeToSunRise / 2;
                        Brightness = (byte)iTimeToSunRise;
                    }
                    else if (TimeToSunSet >= 0 && TimeToSunSet < 31)
                    {
                        int iTimeToSunSet = (int)TimeToSunSet;
                        iTimeToSunSet = iTimeToSunSet / 2;
                        Brightness = (byte)(14 - iTimeToSunSet);
                    }
                    else
                    {
                        if (TimeToSunSet > 0 && TimeToSunRise > 0 || TimeToSunSet < 0 && TimeToSunRise < 0)
                            Brightness = 1;
                        else
                            Brightness = 15;
                    }

                    if (Brightness < 1) Brightness = 1;
                    if (Brightness > 15) Brightness = 15;
                    display.Brightness = Brightness;

                    int Duration = 1000;
                    switch (WhatToDisplay)
                    {
                        case enumShow.Blank:
                            {
                                display.Write("");
                                display.Dots = Dot.Off;
                            }
                            break;

                        case enumShow.Date:
                            {
                                display.Write(DateTime.Now.ToString("ddMM"));
                                display.Dots = Dot.Off;
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

                                string sTime = dt.ToString("HHmm");
                                Segment[] s = new Segment[display.NumberOfDigits];
                                for (int i = 0; i < display.NumberOfDigits; i++)
                                {
                                    if (i == 0 && sTime[0] == '0')
                                        s[i] = 0;
                                    else
                                        s[i] = GetSegmentForNumber(sTime[i] - 48);
                                }
                                if (AlarmOn)
                                {
                                    s[3] = s[3] | Segment.Dot;
                                }

                                ReadOnlySpan<Segment> span = new ReadOnlySpan<Segment>(s);
                                display.Write(span, 0);

                                // Make center colon toggle on and off with seconds on clock.
                                Dot dot = Dot.Off;
                                if (dt.Second % 2 == 0)
                                    dot |= Dot.CenterColon;
                                display.Dots = dot;
                            }
                            break;
                    }
                    Thread.Sleep(Duration);
                }
            });
        }

        public void PlayAnimation(LED7SegAnimation animation)
        {
            if (animation == null)
                return;
            Animation = animation;
            WhatToDisplay = enumShow.Animation;
        }

        // This method is called by the timer delegate.
        public void CheckStatus(object stateInfo)
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
                if (dt.Second % 2 == 0)
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

        public void ClearDisplay()
        {
            WhatToDisplay = enumShow.Blank;
        }
    }
}
