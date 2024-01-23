using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;

namespace AlarmClock.Hardware
{
    public class LEDRingAnimationFrame
    {
        public int Duration { get; set; }
        public bool Loop { get; set; }
        public Color[] _pixels;

        public Span<Color> Pixels => _pixels;

        public LEDRingAnimationFrame(int LEDCount)
        {
            _pixels = new Color[LEDCount];
        }

        public static LEDRingAnimationFrame ParseFrame(int LedCount, string s)
        {
            LEDRingAnimationFrame frame = new LEDRingAnimationFrame(LedCount);
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
                int A = 255;
                int R = 0;
                int G = 0;
                int B = 0;
                if (c.Length == 3)
                {
                    R = Convert.ToInt32("0x" + c.Substring(0, 1) + c.Substring(0, 1), 16);
                    G = Convert.ToInt32("0x" + c.Substring(1, 1) + c.Substring(1, 1), 16);
                    B = Convert.ToInt32("0x" + c.Substring(2, 1) + c.Substring(2, 1), 16);
                }
                else if (c.Length == 6)
                {
                    R = Convert.ToInt32("0x" + c.Substring(0, 2), 16);
                    G = Convert.ToInt32("0x" + c.Substring(2, 2), 16);
                    B = Convert.ToInt32("0x" + c.Substring(4, 2), 16);

                }
                frame.Pixels[i] = Color.FromArgb(A, R, G, B);
                i++;
                if (i > frame.Pixels.Length)
                    break;
            }
            return frame;
        }
    }

    public class LEDRingAnimation
    {
        public List<LEDRingAnimationFrame> Frames { get; set; } = new List<LEDRingAnimationFrame>();
        public string Name { get; set; }

        public int LedCount { get; set; } = 12;

        public LEDRingAnimation(int LedCount, string Name = "")
        {
            this.LedCount = LedCount;
            this.Name = Name;
        }

        public static LEDRingAnimation LoadAnimationFile(int LedCount, StreamReader s, string Name = "")
        {
            LEDRingAnimation animation = new LEDRingAnimation(LedCount, Name);
            do
            {
                string line = s.ReadLine();
                if (string.IsNullOrWhiteSpace(line))
                    break;
                animation.Frames.Add(LEDRingAnimationFrame.ParseFrame(LedCount, line));
            } while (true);

            return animation;
        }

        public static LEDRingAnimation LoadAnimationFile(int LedCount, string s, string Name = "")
        {
            byte[] byteArray = Encoding.ASCII.GetBytes(s);
            MemoryStream stream = new MemoryStream(byteArray);

            // convert stream to string
            StreamReader reader = new StreamReader(stream);
            return LoadAnimationFile(LedCount, reader, Name);
        }
    }

    public static class Animations
    {
        public static string JarvisWake =
            "11:000000,000000,000000,000000,FFF,FFF,000000,000000,000000,000000,000000,000000" + Environment.NewLine +
            "12:000000,000000,000000,000000,FDF,FDF,000000,000000,000000,000000,000000,000000" + Environment.NewLine +
            "13:000000,000000,000000,FFF,FAF,FAF,FFF,000000,000000,000000,000000,000000" + Environment.NewLine +
            "14:000000,000000,000000,FDF,F0F,F0F,FDF,000000,000000,000000,000000,000000" + Environment.NewLine +
            "15:000000,000000,FFF,FAF,F0F,F0F,FAF,FFF,000000,000000,000000,000000" + Environment.NewLine +
            "16:000000,000000,FDF,F0F,F0F,F0F,F0F,FDF,000000,000000,000000,000000" + Environment.NewLine +
            "17:000000,FFF,FAF,F0F,F0F,F0F,F0F,FAF,FFF,000000,000000,000000" + Environment.NewLine +
            "18:000000,FDF,F0F,F0F,F0F,F0F,F0F,F0F,FDF,000000,000000,000000" + Environment.NewLine +
            "19:FFF,FAF,F0F,F0F,F0F,F0F,F0F,F0F,FAF,FFF,000000,000000" + Environment.NewLine +
            "20:FDF,F0F,F0F,F0F,F0F,F0F,F0F,F0F,F0F,FDF,000000,000000" + Environment.NewLine +
            "21:FAF,F0F,F0F,F0F,F0F,F0F,F0F,F0F,F0F,FAF,FFF,FFF" + Environment.NewLine +
            "22:F0F,F0F,F0F,F0F,F0F,F0F,F0F,F0F,F0F,F0F,FDF,FDF" + Environment.NewLine +
            "22:F0F,F0F,F0F,F0F,F0F,F0F,F0F,F0F,F0F,F0F,FAF,FAF" + Environment.NewLine +
            "loop" + Environment.NewLine +
            "1000:F0F,F0F,F0F,F0F,F0F,F0F,F0F,F0F,F0F,F0F,F0F,F0F";

        public static string JarvisEnd =
         "22:F0F,F0F,F0F,F0F,F0F,F0F,F0F,F0F,F0F,F0F,F0F,F0F" + Environment.NewLine +
         "21:F0F,F0F,F0F,F0F,F0F,F0F,F0F,F0F,F0F,F0F,FFF,FFF" + Environment.NewLine +
         "20:F0F,F0F,F0F,F0F,F0F,F0F,F0F,F0F,F0F,F0F,000000,000000" + Environment.NewLine +
         "20:F0F,F0F,F0F,F0F,F0F,F0F,F0F,F0F,F0F,F0F,000000,000000" + Environment.NewLine +
         "20:FFF,F0F,F0F,F0F,F0F,F0F,F0F,F0F,F0F,FFF,000000,000000" + Environment.NewLine +
         "20:FFF,F0F,F0F,F0F,F0F,F0F,F0F,F0F,F0F,FFF,000000,000000" + Environment.NewLine +
         "20:000000,FFF,F0F,F0F,F0F,F0F,F0F,F0F,FFF,000000,000000,000000" + Environment.NewLine +
         "20:000000,FFF,F0F,F0F,F0F,F0F,F0F,F0F,FFF,000000,000000,000000" + Environment.NewLine +
         "20:000000,000000,FFF,F0F,F0F,F0F,F0F,FFF,000000,000000,000000,000000" + Environment.NewLine +
         "20:000000,000000,FFF,F0F,F0F,F0F,F0F,FFF,000000,000000,000000,000000" + Environment.NewLine +
         "15:000000,000000,000000,FFF,F0F,F0F,FFF,000000,000000,000000,000000,000000" + Environment.NewLine +
         "15:000000,000000,000000,FFF,F0F,F0F,FFF,000000,000000,000000,000000,000000" + Environment.NewLine +
         "15:000000,000000,000000,000000,FFF,FFF,000000,000000,000000,000000,000000,000000" + Environment.NewLine +
         "15:000000,000000,000000,000000,FFF,FFF,000000,000000,000000,000000,000000,000000" + Environment.NewLine +
         "loop" + Environment.NewLine +
         "1000:000000,000000,000000,000000,000000,000000,000000,000000,000000,000000,000000,000000";


        public static string JarvisListen =
            "22:F0F,F8F,F0F,F8F,F0F,F8F,F0F,F8F,F0F,F8F,F0F,F8F" + Environment.NewLine +
            "22:F8F,F0F,F8F,F0F,F8F,F0F,F8F,F0F,F8F,F0F,F8F,F0F" + Environment.NewLine +
            "loop" + Environment.NewLine +
            "22:F0F,F8F,F0F,F8F,F0F,F8F,F0F,F8F,F0F,F8F,F0F,F8F" + Environment.NewLine +
            "22:F8F,F0F,F8F,F0F,F8F,F0F,F8F,F0F,F8F,F0F,F8F,F0F";

        public static string AlexaWake =
            "11:000000,000000,000000,000000,0FF,0FF,000000,000000,000000,000000,000000,000000" + Environment.NewLine +
            "12:000000,000000,000000,000000,0DF,0DF,000000,000000,000000,000000,000000,000000" + Environment.NewLine +
            "13:000000,000000,000000,0FF,0AF,0AF,0FF,000000,000000,000000,000000,000000" + Environment.NewLine +
            "14:000000,000000,000000,0DF,00F,00F,0DF,000000,000000,000000,000000,000000" + Environment.NewLine +
            "15:000000,000000,0FF,0AF,00F,00F,0AF,0FF,000000,000000,000000,000000" + Environment.NewLine +
            "16:000000,000000,0DF,00F,00F,00F,00F,0DF,000000,000000,000000,000000" + Environment.NewLine +
            "17:000000,0FF,0AF,00F,00F,00F,00F,0AF,0FF,000000,000000,000000" + Environment.NewLine +
            "18:000000,0DF,00F,00F,00F,00F,00F,00F,0DF,000000,000000,000000" + Environment.NewLine +
            "19:0FF,0AF,00F,00F,00F,00F,00F,00F,0AF,0FF,000000,000000" + Environment.NewLine +
            "20:0DF,00F,00F,00F,00F,00F,00F,00F,00F,0DF,000000,000000" + Environment.NewLine +
            "21:0AF,00F,00F,00F,00F,00F,00F,00F,00F,0AF,0FF,0FF" + Environment.NewLine +
            "22:00F,00F,00F,00F,00F,00F,00F,00F,00F,00F,0DF,0DF" + Environment.NewLine +
            "22:00F,00F,00F,00F,00F,00F,00F,00F,00F,00F,0AF,0AF" + Environment.NewLine +
            "loop" + Environment.NewLine +
            "1000:00F,00F,00F,00F,00F,00F,00F,00F,00F,00F,0FF,0FF";

        public static string AlexaThinking =
            "loop" + Environment.NewLine +
            "100:09F,00f,0FF,05F,0CE,00F,0FF,05f,00F,05f,0ff,00F" + Environment.NewLine +
            "100:00F,05F,09F,0CE,00F,09F,05f,0FF,00f,0ce,00f,0ce" + Environment.NewLine +
            "100:0ff,0ce,00F,09F,05F,00F,0FF,00f,0ce,0ff,0ce,00f" + Environment.NewLine +
            "100:00F,09F,05f,0FF,09F,05F,0CE,0CE,05f,05F,00F,09F" + Environment.NewLine +
            "100:0FF,00f,0ce,05f,00f,0CE,00F,05F,0ce,00f,0CE,00f" + Environment.NewLine +
            "100:05f,09F,00f,0CE,0CE,00F,09F,0FF,00f,09F,00F,0ff" + Environment.NewLine +
            "100:00F,0ce,0FF,05f,00F,0FF,0ce,05f,0ff,05F,0ce,00F" + Environment.NewLine +
            "100:0FF,05f,09F,00F,0CE,05f,00f,0CE,05F,0FF,00F,09F";

        public static string AlexaSpeaking =
            "100:0ff,0ff,0ff,0ff,0ff,0ff,0ff,0ff,0ff,0ff,0ff,0ff" + Environment.NewLine +
            "90:0ef,0ef,0ef,0ef,0ef,0ef,0ef,0ef,0ef,0ef,0ef,0ef" + Environment.NewLine +
            "90:0df,0df,0df,0df,0df,0df,0df,0df,0df,0df,0df,0df" + Environment.NewLine +
            "80:0cf,0cf,0cf,0cf,0cf,0cf,0cf,0cf,0cf,0cf,0cf,0cf" + Environment.NewLine +
            "70:0bf,0bf,0bf,0bf,0bf,0bf,0bf,0bf,0bf,0bf,0bf,0bf" + Environment.NewLine +
            "60:0af,0af,0af,0af,0af,0af,0af,0af,0af,0af,0af,0af" + Environment.NewLine +
            "50:09f,09f,09f,09f,09f,09f,09f,09f,09f,09f,09f,09f" + Environment.NewLine +
            "40:08f,08f,08f,08f,08f,08f,08f,08f,08f,08f,08f,08f" + Environment.NewLine +
            "40:07f,07f,07f,07f,07f,07f,07f,07f,07f,07f,07f,07f" + Environment.NewLine +
            "40:06f,06f,06f,06f,06f,06f,06f,06f,06f,06f,06f,06f" + Environment.NewLine +
            "40:05F,05F,05F,05F,05F,05F,05F,05F,05F,05F,05F,05F" + Environment.NewLine +
            "40:04F,04F,04F,04F,04F,04F,04F,04F,04F,04F,04F,04F" + Environment.NewLine +
            "40:03F,03F,03F,03F,03F,03F,03F,03F,03F,03F,03F,03F" + Environment.NewLine +
            "40:02F,02F,02F,02F,02F,02F,02F,02F,02F,02F,02F,02F" + Environment.NewLine +
            "100:00F,00F,00F,00F,00F,00F,00F,00F,00F,00F,00F,00F" + Environment.NewLine +
            "40:02F,02F,02F,02F,02F,02F,02F,02F,02F,02F,02F,02F" + Environment.NewLine +
            "40:03F,03F,03F,03F,03F,03F,03F,03F,03F,03F,03F,03F" + Environment.NewLine +
            "40:04F,04F,04F,04F,04F,04F,04F,04F,04F,04F,04F,04F" + Environment.NewLine +
            "40:05f,05f,05f,05f,05f,05f,05f,05f,05f,05f,05f,05f" + Environment.NewLine +
            "40:06F,06f,06f,06f,06f,06f,06f,06f,06f,06f,06f,06f" + Environment.NewLine +
            "40:07f,07f,07f,07f,07f,07f,07f,07f,07f,07f,07f,07f" + Environment.NewLine +
            "50:08F,08f,08f,08f,08f,08f,08f,08f,08f,08f,08f,08f" + Environment.NewLine +
            "60:09f,09f,09f,09f,09f,09f,09f,09f,09f,09f,09f,09f" + Environment.NewLine +
            "70:0af,0af,0af,0af,0af,0af,0af,0af,0af,0af,0af,0af" + Environment.NewLine +
            "80:0bf,0bf,0bf,0bf,0bf,0bf,0bf,0bf,0bf,0bf,0bf,0bf" + Environment.NewLine +
            "90:0cf,0cf,0cf,0cf,0cf,0cf,0cf,0cf,0cf,0cf,0cf,0cf" + Environment.NewLine +
            "90:0df,0df,0df,0df,0df,0df,0df,0df,0df,0df,0df,0df" + Environment.NewLine +
            "90:0ef,0ef,0ef,0ef,0ef,0ef,0ef,0ef,0ef,0ef,0ef,0ef" + Environment.NewLine +
            "100:0ff,0ff,0ff,0ff,0ff,0ff,0ff,0ff,0ff,0ff,0ff,0ff" + Environment.NewLine +
            "90:0ef,0ef,0ef,0ef,0ef,0ef,0ef,0ef,0ef,0ef,0ef,0ef" + Environment.NewLine +
            "90:0df,0df,0df,0df,0df,0df,0df,0df,0df,0df,0df,0df" + Environment.NewLine +
            "80:0cf,0cf,0cf,0cf,0cf,0cf,0cf,0cf,0cf,0cf,0cf,0cf" + Environment.NewLine +
            "70:0bf,0bf,0bf,0bf,0bf,0bf,0bf,0bf,0bf,0bf,0bf,0bf" + Environment.NewLine +
            "60:0af,0af,0af,0af,0af,0af,0af,0af,0af,0af,0af,0af" + Environment.NewLine +
            "50:09f,09f,09f,09f,09f,09f,09f,09f,09f,09f,09f,09f" + Environment.NewLine +
            "40:08f,08f,08f,08f,08f,08f,08f,08f,08f,08f,08f,08f" + Environment.NewLine +
            "40:07f,07f,07f,07f,07f,07f,07f,07f,07f,07f,07f,07f" + Environment.NewLine +
            "40:06f,06f,06f,06f,06f,06f,06f,06f,06f,06f,06f,06f" + Environment.NewLine +
            "40:05F,05F,05F,05F,05F,05F,05F,05F,05F,05F,05F,05F" + Environment.NewLine +
            "40:04F,04F,04F,04F,04F,04F,04F,04F,04F,04F,04F,04F" + Environment.NewLine +
            "40:03F,03F,03F,03F,03F,03F,03F,03F,03F,03F,03F,03F" + Environment.NewLine +
            "40:02F,02F,02F,02F,02F,02F,02F,02F,02F,02F,02F,02F" + Environment.NewLine +
            "100:00F,00F,00F,00F,00F,00F,00F,00F,00F,00F,00F,00F" + Environment.NewLine +
            "40:02F,02F,02F,02F,02F,02F,02F,02F,02F,02F,02F,02F" + Environment.NewLine +
            "40:03F,03F,03F,03F,03F,03F,03F,03F,03F,03F,03F,03F" + Environment.NewLine +
            "40:04F,04F,04F,04F,04F,04F,04F,04F,04F,04F,04F,04F" + Environment.NewLine +
            "40:05f,05f,05f,05f,05f,05f,05f,05f,05f,05f,05f,05f" + Environment.NewLine +
            "40:06F,06f,06f,06f,06f,06f,06f,06f,06f,06f,06f,06f" + Environment.NewLine +
            "40:07f,07f,07f,07f,07f,07f,07f,07f,07f,07f,07f,07f" + Environment.NewLine +
            "50:08F,08f,08f,08f,08f,08f,08f,08f,08f,08f,08f,08f" + Environment.NewLine +
            "60:09f,09f,09f,09f,09f,09f,09f,09f,09f,09f,09f,09f" + Environment.NewLine +
            "70:0af,0af,0af,0af,0af,0af,0af,0af,0af,0af,0af,0af" + Environment.NewLine +
            "80:0bf,0bf,0bf,0bf,0bf,0bf,0bf,0bf,0bf,0bf,0bf,0bf" + Environment.NewLine +
            "90:0cf,0cf,0cf,0cf,0cf,0cf,0cf,0cf,0cf,0cf,0cf,0cf" + Environment.NewLine +
            "90:0df,0df,0df,0df,0df,0df,0df,0df,0df,0df,0df,0df" + Environment.NewLine +
            "90:0ef,0ef,0ef,0ef,0ef,0ef,0ef,0ef,0ef,0ef,0ef,0ef";

        public static string AlexaEnd =
            "22:00F,00F,00F,00F,00F,00F,00F,00F,00F,00F,0FF,0FF" + Environment.NewLine +
            "21:00F,00F,00F,00F,00F,00F,00F,00F,00F,00F,0FF,0FF" + Environment.NewLine +
            "20:00F,00F,00F,00F,00F,00F,00F,00F,00F,00F,000000,000000" + Environment.NewLine +
            "20:00F,00F,00F,00F,00F,00F,00F,00F,00F,00F,000000,000000" + Environment.NewLine +
            "20:0FF,00F,00F,00F,00F,00F,00F,00F,00F,0FF,000000,000000" + Environment.NewLine +
            "20:0FF,00F,00F,00F,00F,00F,00F,00F,00F,0FF,000000,000000" + Environment.NewLine +
            "20:000000,0FF,00F,00F,00F,00F,00F,00F,0FF,000000,000000,000000" + Environment.NewLine +
            "20:000000,0FF,00F,00F,00F,00F,00F,00F,0FF,000000,000000,000000" + Environment.NewLine +
            "20:000000,000000,0FF,00F,00F,00F,00F,0FF,000000,000000,000000,000000" + Environment.NewLine +
            "20:000000,000000,0FF,00F,00F,00F,00F,0FF,000000,000000,000000,000000" + Environment.NewLine +
            "15:000000,000000,000000,0FF,00F,00F,0FF,000000,000000,000000,000000,000000" + Environment.NewLine +
            "15:000000,000000,000000,0FF,00F,00F,0FF,000000,000000,000000,000000,000000" + Environment.NewLine +
            "15:000000,000000,000000,000000,0FF,0FF,000000,000000,000000,000000,000000,000000" + Environment.NewLine +
            "15:000000,000000,000000,000000,0FF,0FF,000000,000000,000000,000000,000000,000000" + Environment.NewLine +
            "loop" + Environment.NewLine +
            "1000:000000,000000,000000,000000,000000,000000,000000,000000,000000,000000,000000,000000";

    }
}
