using System;
using System.Device.I2c;

namespace AlarmClockPi
{
    class AmpDriver
    {
        I2cDevice ampDevice;

        public AmpDriver(I2cDevice i2cDevice, int gain=10)
        {
            this.ampDevice = i2cDevice;

            byte ControlReg = ampDevice.ReadReg(0x01);
            if ((ControlReg & 0b11000000) != 0b11000000)
            {
                ampDevice.WriteReg(0x01, 0b11000011);
            }

            int oldGain = GetGain();
            SetGain(gain);
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
}
