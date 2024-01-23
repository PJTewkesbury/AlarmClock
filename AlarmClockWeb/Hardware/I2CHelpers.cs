using System;
using System.Device.I2c;

namespace AlarmClock.Hardware
{
    internal static class I2CHelpers
    {
        public static void WriteReg(this I2cDevice device, byte reg, byte value)
        {
            if (device == null)
                return;

            Span<byte> d = new byte[2];
            d[0] = reg;
            d[1] = value;
            device.Write(d);
        }
        public static byte ReadReg(this I2cDevice device, byte reg)
        {
            device.WriteByte(reg);
            return device.ReadByte();
        }
    }
}
