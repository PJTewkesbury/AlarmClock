using System;
using System.Device.I2c;

namespace AlarmClockPi
{
    internal static class I2CHelpers
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
