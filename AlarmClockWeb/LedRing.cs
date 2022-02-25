using System;
using System.Device.Gpio;
using System.Device.Spi;
using System.Device.I2c;
using System.Drawing;

namespace AlarmClockPi
{
    /// <summary>
    /// Driver for APA102. A double line transmission integrated control LED
    /// </summary>
    public class LedRing : IDisposable
    {
        /// <summary>
        /// Colors of LEDs
        /// </summary>
        // public Span<Color> Pixels => _pixels;
        Iot.Device.Apa102.Apa102 apa102Device;
        SpiDevice _spiDevice;
        public int LedCount { get; private set; } = 12;
        private GpioController gpio { get; set; } = null;

        public int StartLedIndex { get; set; } = 0;
        public Color[] Pixels { get; set; } = null;


        /// <summary>
        /// Initializes a new instance of the APA102 device.
        /// </summary>
        /// <param name="spiDevice">The SPI device used for communication.</param>
        /// <param name="length">Number of LEDs</param>
        public LedRing(GpioController gpio, int LedCount=12)
        {
            this.LedCount = LedCount;
            Pixels = new Color[LedCount];

            // Connect to LEDRing over SPI
            _spiDevice = SpiDevice.Create(new SpiConnectionSettings(0, 1)
            {
                ClockFrequency = 20_000_000,
                DataFlow = DataFlow.MsbFirst,
                Mode = SpiMode.Mode3 // ensure data is ready at clock rising edge
            });
            apa102Device = new Iot.Device.Apa102.Apa102(_spiDevice, LedCount);

            // Turn Power on to LEDRing
            this.gpio = gpio;
            PowerLEDS(true);

            // Clear pixels
            ClearPixels();
        }

        public void PowerLEDS(bool Power = true)
        {
            if (gpio == null)
                throw new ArgumentNullException(nameof(gpio));

            // Turn power on/off for LED Ring.
            if (!gpio.IsPinOpen(5))
                gpio.OpenPin(5, PinMode.Output);

            if (Power)
                gpio.Write(5, PinValue.High);
            else
                gpio.Write(5, PinValue.Low);
        }
        
        public int LedLitCount { get; set; }  = 0;
        /// <summary>
        /// Update color data to LEDs
        /// </summary>
        public void Render()
        {
            int idx = StartLedIndex;
            LedLitCount = 0;
            foreach (Color pix in Pixels)
            {
                if (pix.R>0 || pix.G>0 || pix.B>0)
                    LedLitCount++;

                apa102Device.Pixels[idx] = pix;
                idx++;
                if (idx >= LedCount)
                    idx = 0;
            }
            apa102Device.Flush();
        }

        public void ClearPixels()
        {
            Span<Color> pixels = apa102Device.Pixels;
            pixels.Fill(Color.FromArgb(0, 0, 0, 0));
            Render();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            ClearPixels();
            PowerLEDS(false);

            apa102Device?.Dispose();
            _spiDevice?.Dispose();
            _spiDevice = null!;
            apa102Device = null;
        }

        public Color MergeColor(Color Color1, int Pixel)
        {
            int A = Color1.A + Pixels[Pixel].A;
            if (A > 255) A = 255;

            int R = Color1.R + Pixels[Pixel].R;
            if (R > 255) R = 255;

            int G = Color1.G + Pixels[Pixel].G;
            if (G > 255) G = 255;

            int B = Color1.B + Pixels[Pixel].B;
            if (B > 255) B = 255;

            Pixels[Pixel] = Color.FromArgb(A, R, G, B);
            return Pixels[Pixel];
        }

        public void PlayAnimation(LEDRingAnimation animation)
        {
            if (animation == null)
                return;

            foreach(var f in animation.Frames)
            {
                if (f.Loop)
                    continue;

                for (int i = 0; i < LedCount; i++)
                {
                    Pixels[i] = f.Pixels[i];
                }
                Render();
                System.Threading.Thread.Sleep(f.Duration);
            }
        }
    }
}
