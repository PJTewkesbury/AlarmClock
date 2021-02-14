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
    public class Apa102 : IDisposable
    {
        /// <summary>
        /// Colors of LEDs
        /// </summary>
        public Span<Color> Pixels => _pixels;

        private SpiDevice _spiDevice;
        private Color[] _pixels;
        private byte[] _buffer;

        /// <summary>
        /// Initializes a new instance of the APA102 device.
        /// </summary>
        /// <param name="spiDevice">The SPI device used for communication.</param>
        /// <param name="length">Number of LEDs</param>
        public Apa102()
        {
            int length = 12;
            SpiConnectionSettings settings = new SpiConnectionSettings(0, 1);
            settings.ClockFrequency = 10_000_000;
            settings.DataFlow = DataFlow.MsbFirst;
            settings.Mode = SpiMode.Mode3;

            _spiDevice = SpiDevice.Create(settings);
            // Iot.Device.Apa102.Apa102 x = new Iot.Device.Apa102.Apa102(_spiDevice, 12);

            _pixels = new Color[length];
            _buffer = new byte[(length + 2) * 4];

            _buffer.AsSpan(0, 4).Fill(0x00); // start frame
            _buffer.AsSpan((length + 1) * 4, 4).Fill(0xFF); // end frame
        }

        public void PowerLEDS(GpioController gpio, bool Power = true)
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

        /// <summary>
        /// Update color data to LEDs
        /// </summary>
        public void Render()
        {
            for (var i = 0; i < _pixels.Length; i++)
            {
                var pixel = _buffer.AsSpan((i + 1) * 4);
                pixel[0] = (byte)((_pixels[i].A >> 3) | 0b11100000); // global brightness (alpha)
                pixel[1] = _pixels[i].B; // blue
                pixel[2] = _pixels[i].G; // green
                pixel[3] = _pixels[i].R; // red
            }

            _spiDevice.Write(_buffer);
        }

        public void ClearPixels()
        {
            for (var i = 0; i < _pixels.Length; i++)
            {
                this._pixels[i] = Color.FromArgb(0, 0, 0, 0);
            }
        }

        public void Clear()
        {
            ClearPixels();
            this.Render();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Clear();

            _spiDevice?.Dispose();
            _spiDevice = null!;
            _pixels = null!;
            _buffer = null!;
        }

        public Color MergeColor(Color Color1, int Pixel)
        {
            int A = Color1.A + _pixels[Pixel].A;
            if (A > 255) A = 255;

            int R = Color1.R + _pixels[Pixel].R;
            if (R > 255) R = 255;

            int G = Color1.G + _pixels[Pixel].G;
            if (G > 255) G = 255;

            int B = Color1.B + _pixels[Pixel].B;
            if (B > 255) B = 255;

            _pixels[Pixel] = Color.FromArgb(A, R, G, B);
            return _pixels[Pixel];
        }

        public void PlayAnimation(LEDRingAnimation animation)
        {
            if (animation == null)
                return;

            foreach(var f in animation.Frames)
            {
                if (f.Loop)
                    continue;

                for (int i = 0; i < Pixels.Length; i++)
                {
                    _pixels[i] = f.Pixels[i];
                }
                Render();
                System.Threading.Thread.Sleep(f.Duration);
            }
        }

        public void RotateClockwise()
        {

        }

        public void RotateAntiClockwise()
        {

        }
    }
}
