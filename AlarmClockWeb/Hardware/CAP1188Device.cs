using System;
using System.Collections.Generic;
using System.Device.I2c;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlarmClock.Hardware
{
    public class CAP1188DeviceI2C : CAP1188Device, IDisposable
    {
        I2cDevice i2c;

        public CAP1188DeviceI2C(I2cDevice i2c) : base()
        {
            this.i2c = i2c;
        }

        public override byte _read_register(byte address)
        {
            // Return 8 bit value of register at address.//
            i2c.WriteByte(address);
            return i2c.ReadByte();
        }

        public override void _write_register(byte address, byte value)
        {
            byte[] data = new byte[2] { address, value };
            ReadOnlySpan<byte> s = data;
            i2c.Write(s);
        }

        public override byte[] _read_block(byte start, byte length)
        {
            byte[] result = new byte[length];
            Span<byte> d = result;
            d.Fill(0);
            i2c.Write(d);
            i2c.Read(result);

            return result;
        }

        public override void _write_block(byte start, byte[] data)
        {
            Span<byte> d = data;
            i2c.Write(d);
        }

        public void Dispose()
        {
            i2c.Dispose();
            i2c = null;
        }
    }

    public class CAP1188Device
    {
        private const byte _CAP1188_MID = 0x5D;
        private const byte _CAP1188_PID = 0x50;
        private const byte _CAP1188_MAIN_CONTROL = 0x00;
        private const byte _CAP1188_GENERAL_STATUS = 0x02;
        private const byte _CAP1188_INPUT_STATUS = 0x03;
        private const byte _CAP1188_LED_STATUS = 0x04;
        private const byte _CAP1188_NOISE_FLAGS = 0x0A;
        private byte[] _CAP1188_DELTA_COUNT = new byte[] { 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17 };
        private const byte _CAP1188_SENSITIVTY = 0x1F;
        private const byte _CAP1188_CAL_ACTIVATE = 0x26;
        private const byte _CAP1188_MULTI_TOUCH_CFG = 0x2A;
        internal static byte _CAP1188_THESHOLD_1 = 0x30;
        private const byte _CAP1188_STANDBY_CFG = 0x41;
        private const byte _CAP1188_LED_LINKING = 0x72;
        private const byte _CAP1188_PRODUCT_ID = 0xFD;
        private const byte _CAP1188_MANU_ID = 0xFE;
        private const byte _CAP1188_REVISION = 0xFF;

        private int[] _SENSITIVITY = new int[] { 128, 64, 32, 16, 8, 4, 2, 1 };
        CAP1188_Channel[] _channels = new CAP1188_Channel[8];

        public CAP1188Device()
        {
        }

        public void InitDevice(bool AllowMultiTouch = false)
        {
            int mid = _read_register(_CAP1188_MANU_ID);
            if (mid != _CAP1188_MID)
                throw new ApplicationException($"Failed to find CAP1188! Manufacturer ID: 0x{mid.ToString("x2")}");

            int pid = _read_register(_CAP1188_PRODUCT_ID);
            if (pid != _CAP1188_PID)
                throw new ApplicationException($"Failed to find CAP1188! Product ID: 0x{pid.ToString("x2")}");

            _write_register(_CAP1188_LED_LINKING, 0xFF); // # turn on LED linking
            _write_register(_CAP1188_MULTI_TOUCH_CFG, AllowMultiTouch ? (byte)0x00 : (byte)0x80);  // #  multi touch 0x00 = allow multiple touch, 0x80 - single touch
            _write_register(0x2F, 0x10);  // # turn off input-1-sets-all-inputs feature

            // Enable Interrupt
            _write_register(0x44, 0x41);  // 0x44 default 0x40 use 0x41  — Set interrupt on press but not release
            _write_register(0x28, 0xFF);  // 0x28 default 0xFF use 0x00  — Turn off interrupt repeat on button hold

            recalibrate();
        }

        CAP1188_Channel getitem(byte key)
        {
            byte pin = key;
            int index = key - 1;
            if (pin < 1 || pin > 8)
                throw new ApplicationException("Pin must be a value 1-8.");
            if (_channels[index] == null)
                _channels[index] = new CAP1188_Channel(this, pin);
            return _channels[index];
        }

        public byte touched()
        {
            // Return 8 bit value representing touch state of all pins.
            // # clear the INT bit and any previously touched pins
            byte current = _read_register(_CAP1188_MAIN_CONTROL);
            _write_register(_CAP1188_MAIN_CONTROL, (byte)(current & ~0x01));

            // return only currently touched pins
            return _read_register(_CAP1188_INPUT_STATUS);
        }

        public byte thresholds
        {
            get
            {
                var data = threshold_values();
                if (data != null)
                    return data[0];
                else
                    return 255;
            }
            set
            {
                if (value > 127)
                {
                    throw new ArgumentOutOfRangeException(nameof(thresholds), value, "Threshold value must be in range 0 to 127.");
                }
                byte[] data = new byte[8];
                Span<byte> d = data;
                d.Fill(value);
                _write_block(_CAP1188_THESHOLD_1, d.ToArray());
            }
        }

        public virtual byte[] threshold_values()
        {
            return _read_block(_CAP1188_THESHOLD_1, 8);
        }

        public void recalibrate()
        {
            // Perform a self recalibration on all the pins.
            recalibrate_pins(0xFF);
        }

        public int delta_count(int pin)
        {
            // Return the 8 bit delta count value for the channel.//
            if (pin < 1 || pin > 8)
                throw new ArgumentOutOfRangeException(nameof(pin), pin, "Pin must be a value 1-8.");

            // 8 bit 2's complement
            int raw_value = _read_register(_CAP1188_DELTA_COUNT[pin - 1]); // return 0-> 255 which means -127 -> 127, bit 7 = sign bit
            if ((raw_value & 128) == 128)
                raw_value = raw_value - 256;
            return raw_value;
        }

        public void recalibrate_pins(byte mask)
        {
            // Recalibrate pins specified by bit mask.
            _write_register(_CAP1188_CAL_ACTIVATE, mask);
        }

        public virtual byte _read_register(byte address)
        {
            // Return 8 bit value of register at address.//
            throw new NotImplementedException();
        }

        public virtual void _write_register(byte address, byte value)
        {
            // Return 8 bit value of register at address.//
            throw new NotImplementedException();
        }

        public virtual byte[] _read_block(byte start, byte length)
        {
            // Return byte array of values from start address to length.//
            throw new NotImplementedException();
        }

        public virtual void _write_block(byte start, byte[] data)
        {
            // Return byte array of values from start address to length.//
            throw new NotImplementedException();
        }
    }

    public class CAP1188_Channel
    {
        CAP1188Device _cap1188;
        byte _pin;
        public CAP1188_Channel(CAP1188Device cap1188, byte pin)
        {
            _cap1188 = cap1188;
            _pin = pin;
        }

        public byte value
        {
            get
            {
                return (byte)(_cap1188.touched() & 1 << _pin - 1);
            }
        }

        public byte raw_value
        {
            get
            {
                return (byte)_cap1188.delta_count(_pin);
            }
        }

        public byte threshold
        {
            get { return _cap1188._read_register((byte)(CAP1188Device._CAP1188_THESHOLD_1 + _pin - 1)); }
            set
            {
                if (value > 127)
                    throw new ArgumentOutOfRangeException(nameof(value), value, "Threshold value must be in range 0 to 127.");

                _cap1188._write_register((byte)(CAP1188Device._CAP1188_THESHOLD_1 + _pin - 1), value);
            }
        }

        public void recalibrate()
        {
            // Perform a self recalibration.//
            _cap1188.recalibrate_pins((byte)(1 << _pin - 1));
        }
    }
}
