
using spi_device = adafruit_bus_device.spi_device;

using @const = micropython.@const;

using CAP1188 = adafruit_cap1188.cap1188.CAP1188;

public static class spi {
    
    static spi() {
        @"
`adafruit_cap1188.spi`
====================================================

CircuitPython SPI driver for the CAP1188 8-Key Capacitive Touch Sensor Breakout.

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
    
    public static object _CAP1188_SPI_SET_ADDR = @const(0x7D);
    
    public static object _CAP1188_SPI_WRITE_DATA = @const(0x7E);
    
    public static object _CAP1188_SPI_READ_DATA = @const(0x7F);
    
    // Driver for the CAP1188 connected over SPI.
    public class CAP1188_SPI
        : CAP1188 {
        
        public object _buf;
        
        public object _spi;
        
        public CAP1188_SPI(object spi, object cs) {
            this._spi = spi_device.SPIDevice(spi, cs);
            this._buf = bytearray(4);
            base.@__init__();
        }
        
        // Return 8 bit value of register at address.
        public virtual object _read_register(object address) {
            // pylint: disable=no-member
            this._buf[0] = _CAP1188_SPI_SET_ADDR;
            this._buf[1] = address;
            this._buf[2] = _CAP1188_SPI_READ_DATA;
            using (var spi = this._spi) {
                spi.write_readinto(this._buf, this._buf);
            }
            return this._buf[3];
        }
        
        // Write 8 bit value to registter at address.
        public virtual object _write_register(object address, object value) {
            // pylint: disable=no-member
            this._buf[0] = _CAP1188_SPI_SET_ADDR;
            this._buf[1] = address;
            this._buf[2] = _CAP1188_SPI_WRITE_DATA;
            this._buf[3] = value;
            using (var spi = this._spi) {
                spi.write(this._buf);
            }
        }
        
        // Return byte array of values from start address to length.
        public virtual object _read_block(object start, object length) {
            // pylint: disable=no-member
            this._buf[0] = _CAP1188_SPI_SET_ADDR;
            this._buf[1] = start;
            this._buf[2] = _CAP1188_SPI_READ_DATA;
            var result = bytearray(ValueTuple.Create(_CAP1188_SPI_READ_DATA) * length);
            using (var spi = this._spi) {
                spi.write(this._buf, end: 3);
                spi.write_readinto(result, result);
            }
            return result;
        }
        
        // Write out data beginning at start address.
        public virtual object _write_block(object start, object data) {
            // pylint: disable=no-member
            this._buf[0] = _CAP1188_SPI_SET_ADDR;
            this._buf[1] = start;
            using (var spi = this._spi) {
                spi.write(this._buf, end: 2);
                this._buf[0] = _CAP1188_SPI_WRITE_DATA;
                foreach (var value in data) {
                    this._buf[1] = value;
                    spi.write(this._buf, end: 2);
                }
            }
        }
    }
}
