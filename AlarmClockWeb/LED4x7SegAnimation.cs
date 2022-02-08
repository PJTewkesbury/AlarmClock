using Iot.Device.Display;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlarmClock
{
    public class LED7SegFrame
    {
        public Segment[] Frame { get; set; }
        public int Duration { get; set; }
        public bool Loop { get; set; }

        public LED7SegFrame(int DigitCount)
        {
            Frame = new Segment[DigitCount];
        }

        public static LED7SegFrame ParseFrame(int LedCount, string s)
        {
            LED7SegFrame frame = new LED7SegFrame(LedCount);
            if (s == "loop")
            {
                frame.Loop = true;
                return frame;
            }
            frame.Loop = false;

            string[] parts = s.Split(':');
            frame.Duration = Convert.ToInt32(parts[0]);

            string[] clr = parts[1].Split(',');
            int i = 0;
            foreach (string c in clr)
            {
                int S = Convert.ToInt32("0x" + c.Substring(0, 2), 16);
                frame.Frame[i] = (Segment)S;
                i++;
                if (i > frame.Frame.Length)
                    break;
            }
            return frame;
        }
    }

    public class LED7SegAnimation
    {
        public List<LED7SegFrame> Frames = new List<LED7SegFrame>();
        public int Index { get; set; } = 0;
        int DigitCount = 4;

        public LED7SegAnimation(int DigitCount)
        {
            this.DigitCount = DigitCount;
        }

        public void AddFrame(LED7SegFrame frame)
        {
            Frames.Add(frame);
        }

        public void AddFrame(params Segment[] segments)
        {
            LED7SegFrame frame = new LED7SegFrame(DigitCount);

            int idx = 0;
            foreach (Segment s in segments)
            {
                frame.Frame[idx] = s;
                idx++;
            }
            Frames.Add(frame);
        }

        public static LED7SegAnimation LoadAnimationFile(int LedCount, StreamReader s)
        {
            LED7SegAnimation animation = new LED7SegAnimation(LedCount);
            do
            {
                string line = s.ReadLine();
                if (String.IsNullOrWhiteSpace(line))
                    break;
                animation.Frames.Add(LED7SegFrame.ParseFrame(LedCount, line));
            } while (true);

            return animation;
        }

        public static LED7SegAnimation LoadAnimationFile(int LedCount, String s)
        {
            byte[] byteArray = Encoding.ASCII.GetBytes(s);
            MemoryStream stream = new MemoryStream(byteArray);

            // convert stream to string
            StreamReader reader = new StreamReader(stream);
            return LoadAnimationFile(LedCount, reader);
        }
    }

    public static class LED4x7SegAnimations
    {
        public static string Scanning =
            "100:30,00,00,00" + Environment.NewLine +
            "100:06,00,00,00" + Environment.NewLine +
            "100:00,30,00,00" + Environment.NewLine +
            "100:00,06,00,00" + Environment.NewLine +
            "100:00,00,30,00" + Environment.NewLine +
            "100:00,00,06,00" + Environment.NewLine +
            "100:00,00,00,30" + Environment.NewLine +
            "100:00,00,00,06" + Environment.NewLine +
            "100:00,00,00,30" + Environment.NewLine +
            "100:00,00,06,00" + Environment.NewLine +
            "100:00,00,30,00" + Environment.NewLine +
            "100:00,06,00,00" + Environment.NewLine +
            "100:00,30,00,00" + Environment.NewLine +
            "100:06,00,00,00" + Environment.NewLine +
            "loop";

        public static string Scanning2 =
            "100:01,00,00,00" + Environment.NewLine +
            "100:00,01,00,00" + Environment.NewLine +
            "100:00,00,01,00" + Environment.NewLine +
            "100:00,00,00,01" + Environment.NewLine +
            "100:00,00,00,02" + Environment.NewLine +
            "100:00,00,00,04" + Environment.NewLine +
            "100:00,00,00,08" + Environment.NewLine +
            "100:00,00,08,00" + Environment.NewLine +
            "100:00,08,00,00" + Environment.NewLine +
            "100:08,00,00,00" + Environment.NewLine +
            "100:10,00,00,00" + Environment.NewLine +
            "100:20,00,00,00" + Environment.NewLine +
            "loop";

        public static string LEDTest=
            "100:01,00,00,00" + Environment.NewLine + // Digit 1
            "100:02,00,00,00" + Environment.NewLine +
            "100:04,00,00,00" + Environment.NewLine +
            "100:08,00,00,00" + Environment.NewLine +
            "100:10,00,00,00" + Environment.NewLine +
            "100:20,00,00,00" + Environment.NewLine +
            "100:40,00,00,00" + Environment.NewLine +
            "100:80,00,00,00" + Environment.NewLine + // Digit 2
            "100:00,01,00,00" + Environment.NewLine +
            "100:00,02,00,00" + Environment.NewLine +
            "100:00,04,00,00" + Environment.NewLine +
            "100:00,08,00,00" + Environment.NewLine +
            "100:00,10,00,00" + Environment.NewLine +
            "100:00,20,00,00" + Environment.NewLine +
            "100:00,40,00,00" + Environment.NewLine +
            "100:00,80,00,00" + Environment.NewLine +
            "100:00,00,01,00" + Environment.NewLine + // Digit 3
            "100:00,00,02,00" + Environment.NewLine +
            "100:00,00,04,00" + Environment.NewLine +
            "100:00,00,08,00" + Environment.NewLine +
            "100:00,00,10,00" + Environment.NewLine +
            "100:00,00,20,00" + Environment.NewLine +
            "100:00,00,40,00" + Environment.NewLine +
            "100:00,00,80,00" + Environment.NewLine +
            "100:00,00,00,01" + Environment.NewLine + // Digit 4
            "100:00,00,00,02" + Environment.NewLine +
            "100:00,00,00,04" + Environment.NewLine +
            "100:00,00,00,08" + Environment.NewLine +
            "100:00,00,00,10" + Environment.NewLine +
            "100:00,00,00,20" + Environment.NewLine +
            "100:00,00,00,40" + Environment.NewLine +
            "100:00,00,00,80" + Environment.NewLine +
            "loop";
    }
}
