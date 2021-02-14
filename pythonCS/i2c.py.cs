
using i2c_device = adafruit_bus_device.i2c_device;

using @const = micropython.@const;

using CAP1188 = adafruit_cap1188.cap1188.CAP1188;

public static class i2c {
    
    static i2c() {
        @"
`adafruit_cap1188.i2c`
====================================================

CircuitPython I2C driver for the CAP1188 8-Key Capacitive Touch Sensor Breakout.

* Author(s): Carter Nelson

Implementation Notes
--------------------

**Hardware:**

* `CAP1188 - 8-Key Capacitive Touch Sensor Breakout <https://www.adafruit.com/product/1602>`_

**Software and Dependencies:**

* Adafruit CircuitPython firmware for the supported boards:
  https://github.com/adafruit/circuitpython/releases

* Adafruit's Bus Device library: https://github.com/adafruit/Adafruit_CircuitPython_BusDevice
";
    }
    
    public static string @__version__ = "0.0.0-auto.0";
    
    public static string @__repo__ = "https://github.com/adafruit/Adafruit_CircuitPython_CAP1188.git";
    
    public static object _CAP1188_DEFAULT_ADDRESS = @const(0x29);
    
    // Driver for the CAP1188 connected over I2C.
    public class CAP1188_I2C
        : CAP1188 {
        
        public object _buf;
        
        public object _i2c;
        
        public CAP1188_I2C(object i2c, object address = _CAP1188_DEFAULT_ADDRESS) {
            this._i2c = i2c_device.I2CDevice(i2c, address);
            this._buf = bytearray(2);
            base.@__init__();
        }
        
        // Return 8 bit value of register at address.
        public virtual object _read_register(object address) {
            this._buf[0] = address;
            using (var i2c = this._i2c) {
                i2c.write_then_readinto(this._buf, this._buf, out_end: 1, in_start: 1);
            }
            return this._buf[1];
        }
        
        // Write 8 bit value to registter at address.
        public virtual object _write_register(object address, object value) {
            this._buf[0] = address;
            this._buf[1] = value;
            using (var i2c = this._i2c) {
                i2c.write(this._buf);
            }
        }
        
        // Return byte array of values from start address to length.
        public virtual object _read_block(object start, object length) {
            var result = bytearray(length);
            using (var i2c = this._i2c) {
                i2c.write(bytes(ValueTuple.Create(start)));
                i2c.readinto(result);
            }
            return result;
        }
        
        // Write out data beginning at start address.
        public virtual object _write_block(object start, object data) {
            using (var i2c = this._i2c) {
                i2c.write(bytes(ValueTuple.Create(start)) + data);
            }
        }
    }
}
