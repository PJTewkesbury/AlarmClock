
using @division = @@__future__.division;

using System.Collections;

using I2C = Adafruit_GPIO.I2C;

using System.Collections.Generic;

using System.Linq;

public static class HT16K33 {
    
    public static int DEFAULT_ADDRESS = 0x70;
    
    public static int HT16K33_BLINK_CMD = 0x80;
    
    public static int HT16K33_BLINK_DISPLAYON = 0x01;
    
    public static int HT16K33_BLINK_OFF = 0x00;
    
    public static int HT16K33_BLINK_2HZ = 0x02;
    
    public static int HT16K33_BLINK_1HZ = 0x04;
    
    public static int HT16K33_BLINK_HALFHZ = 0x06;
    
    public static int HT16K33_SYSTEM_SETUP = 0x20;
    
    public static int HT16K33_OSCILLATOR = 0x01;
    
    public static int HT16K33_CMD_BRIGHTNESS = 0xE0;
    
    // Driver for interfacing with a Holtek HT16K33 16x8 LED driver.
    public class HT16K33
        : object {
        
        public object _device;
        
        public object buffer;
        
        public HT16K33(object address = DEFAULT_ADDRESS, object i2c = null, Hashtable kwargs) {
            if (i2c == null) {
                i2c = I2C;
            }
            this._device = i2c.get_i2c_device(address, kwargs);
            this.buffer = bytearray(new List<int> {
                0
            } * 16);
        }
        
        // Initialize driver with LEDs enabled and all turned off.
        public virtual object begin() {
            // Turn on the oscillator.
            this._device.writeList(HT16K33_SYSTEM_SETUP | HT16K33_OSCILLATOR, new List<object>());
            // Turn display on with no blinking.
            this.set_blink(HT16K33_BLINK_OFF);
            // Set display to full brightness.
            this.set_brightness(15);
        }
        
        // Blink display at specified frequency.  Note that frequency must be a
        //         value allowed by the HT16K33, specifically one of: HT16K33_BLINK_OFF,
        //         HT16K33_BLINK_2HZ, HT16K33_BLINK_1HZ, or HT16K33_BLINK_HALFHZ.
        //         
        public virtual object set_blink(object frequency) {
            if (!new List<int> {
                HT16K33_BLINK_OFF,
                HT16K33_BLINK_2HZ,
                HT16K33_BLINK_1HZ,
                HT16K33_BLINK_HALFHZ
            }.Contains(frequency)) {
                throw new ValueError("Frequency must be one of HT16K33_BLINK_OFF, HT16K33_BLINK_2HZ, HT16K33_BLINK_1HZ, or HT16K33_BLINK_HALFHZ.");
            }
            this._device.writeList(HT16K33_BLINK_CMD | HT16K33_BLINK_DISPLAYON | frequency, new List<object>());
        }
        
        // Set brightness of entire display to specified value (16 levels, from
        //         0 to 15).
        //         
        public virtual object set_brightness(object brightness) {
            if (brightness < 0 || brightness > 15) {
                throw new ValueError("Brightness must be a value of 0 to 15.");
            }
            this._device.writeList(HT16K33_CMD_BRIGHTNESS | brightness, new List<object>());
        }
        
        // Sets specified LED (value of 0 to 127) to the specified value, 0/False
        //         for off and 1 (or any True/non-zero value) for on.
        //         
        public virtual object set_led(object led, object value) {
            if (led < 0 || led > 127) {
                throw new ValueError("LED must be value of 0 to 127.");
            }
            // Calculate position in byte buffer and bit offset of desired LED.
            var pos = led / 8;
            var offset = led % 8;
            if (!value) {
                // Turn off the specified LED (set bit to zero).
                this.buffer[pos] |= ~(1 << offset);
            } else {
                // Turn on the speciried LED (set bit to one).
                this.buffer[pos] |= 1 << offset;
            }
        }
        
        // Write display buffer to display hardware.
        public virtual object write_display() {
            foreach (var _tup_1 in this.buffer.Select((_p_1,_p_2) => Tuple.Create(_p_2, _p_1))) {
                var i = _tup_1.Item1;
                var value = _tup_1.Item2;
                this._device.write8(i, value);
            }
        }
        
        // Clear contents of display buffer.
        public virtual object clear() {
            foreach (var _tup_1 in this.buffer.Select((_p_1,_p_2) => Tuple.Create(_p_2, _p_1))) {
                var i = _tup_1.Item1;
                var value = _tup_1.Item2;
                this.buffer[i] = 0;
            }
        }
    }
}
