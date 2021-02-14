
// using @const = micropython.@const;

using System;

using System.Collections.Generic;

using System.Linq;

public static class cap1188 {

    private const int _CAP1188_MID = 0x5D;
    private const int _CAP1188_PID = 0x50;
    private const int _CAP1188_MAIN_CONTROL = 0x00;
    private const int _CAP1188_GENERAL_STATUS = 0x02;
    private const int _CAP1188_INPUT_STATUS = 0x03;
    private const int _CAP1188_LED_STATUS = 0x04;
    private const int _CAP1188_NOISE_FLAGS = 0x0A;
    private int[] _CAP1188_DELTA_COUNT = new int[] { 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17 };
    private const int _CAP1188_SENSITIVTY = 0x1F;
    private const int _CAP1188_CAL_ACTIVATE = 0x26;
    private const int _CAP1188_MULTI_TOUCH_CFG = 0x2A;
    internal static int _CAP1188_THESHOLD_1 = 0x30;
    private const int _CAP1188_STANDBY_CFG = 0x41;
    private const int _CAP1188_LED_LINKING = 0x72;
    private const int _CAP1188_PRODUCT_ID = 0xFD;
    private const int _CAP1188_MANU_ID = 0xFE;
    private const int _CAP1188_REVISION = 0xFF;

    private int[] _SENSITIVITY = new int[] { 128, 64, 32, 16, 8, 4, 2, 1 };
    int[] _channels = new int[8] { 0, 0, 0, 0, 0, 0, 0, 0 };

    // Helper class to represent a touch channel on the CAP1188. Not meant to
    //     be used directly.
    public class CAP1188_Channel {

        public object _cap1188;

        public int _pin;

        public CAP1188_Channel(CAP1188 cap1188, object pin) {
            this._cap1188 = cap1188;
            this._pin = pin;
        }

        // Whether the pin is being touched or not.
        public object value {
            get {
                return (this._cap1188.touched() & 1 << this._pin - 1) != 0;
            }
        }

        // The raw touch measurement.
        public object raw_value {
            get {
                return this._cap1188.delta_count(this._pin);
            }
        }

        // The touch threshold value.
        public object threshold {
            get {
                return this._cap1188._read_register(_CAP1188_THESHOLD_1 + this._pin - 1);
            }
            set {
                value = Convert.ToInt32(value);
                if (!(0 <= value && value <= 127)) {
                    throw new ValueError("Threshold value must be in range 0 to 127.");
                }
                this._cap1188._write_register(_CAP1188_THESHOLD_1 + this._pin - 1, value);
            }
        }

        // Perform a self recalibration.
        public virtual object recalibrate() {
            this._cap1188.recalibrate_pins(1 << this._pin - 1);
        }
    }

    // CAP1188 driver base, must be extended for I2C/SPI interfacing.
    public class CAP1188 {

        public object _channels;

        public CAP1188() {
            var mid = this._read_register(_CAP1188_MANU_ID);
            if (mid != _CAP1188_MID) {
                throw new RuntimeError("Failed to find CAP1188! Manufacturer ID: 0x{:02x}".format(mid));
            }
            var pid = this._read_register(_CAP1188_PRODUCT_ID);
            if (pid != _CAP1188_PID) {
                throw new RuntimeError("Failed to find CAP1188! Product ID: 0x{:02x}".format(pid));
            }
            this._channels = new List<void> {
                null
            } * 8;
            this._write_register(_CAP1188_LED_LINKING, 0xFF);
            this._write_register(_CAP1188_MULTI_TOUCH_CFG, 0x00);
            this._write_register(0x2F, 0x10);
            this.recalibrate();
        }

        public virtual object @__getitem__(object key) {
            var pin = key;
            var index = key - 1;
            if (pin < 1 || pin > 8) {
                throw new IndexError("Pin must be a value 1-8.");
            }
            if (this._channels[index] == null) {
                this._channels[index] = new CAP1188_Channel(this, pin);
            }
            return this._channels[index];
        }

        // A tuple of touched state for all pins.
        public object touched_pins {
            get {
                var touched = this.touched();
                return tuple((from i in Enumerable.Range(0, 8)
                    select @bool(touched >> i & 0x01)).ToList());
            }
        }

        // Return 8 bit value representing touch state of all pins.
        public virtual int touched() {
            // clear the INT bit and any previously touched pins
            var current = this._read_register(_CAP1188_MAIN_CONTROL);
            this._write_register(_CAP1188_MAIN_CONTROL, current & ~0x01);
            // return only currently touched pins
            return this._read_register(_CAP1188_INPUT_STATUS);
        }

        // The sensitvity of touch detections. Range is 1 (least) to 128 (most).
        public object sensitivity {
            get {
                return _SENSITIVITY[this._read_register(_CAP1188_SENSITIVTY) >> 4 & 0x07];
            }
            set {
                if (!_SENSITIVITY.Contains(value)) {
                    throw new ValueError("Sensitivty must be one of: {}".format(_SENSITIVITY));
                }
                value = _SENSITIVITY.index(value) << 4;
                var new_setting = this._read_register(_CAP1188_SENSITIVTY) & 0x8F | value;
                this._write_register(_CAP1188_SENSITIVTY, new_setting);
            }
        }

        // Touch threshold value for all channels.
        public object thresholds {
            get {
                return this.threshold_values();
            }
            set {
                value = Convert.ToInt32(value);
                if (!(0 <= value && value <= 127)) {
                    throw new ValueError("Threshold value must be in range 0 to 127.");
                }
                this._write_block(_CAP1188_THESHOLD_1, bytearray(ValueTuple.Create(value) * 8));
            }
        }

        // Return tuple of touch threshold values for all channels.
        public virtual object threshold_values() {
            return tuple(this._read_block(_CAP1188_THESHOLD_1, 8));
        }

        // Perform a self recalibration on all the pins.
        public virtual object recalibrate() {
            this.recalibrate_pins(0xFF);
        }

        // Return the 8 bit delta count value for the channel.
        public virtual object delta_count(object pin) {
            if (pin < 1 || pin > 8) {
                throw new IndexError("Pin must be a value 1-8.");
            }
            // 8 bit 2's complement
            var raw_value = this._read_register(_CAP1188_DELTA_COUNT[pin - 1]);
            raw_value = raw_value & 128 ? raw_value - 256 : raw_value;
            return raw_value;
        }

        // Recalibrate pins specified by bit mask.
        public virtual object recalibrate_pins(object mask) {
            this._write_register(_CAP1188_CAL_ACTIVATE, mask);
        }

        // Return 8 bit value of register at address.
        public virtual object _read_register(object address) {
            throw new NotImplementedException();
        }

        // Write 8 bit value to registter at address.
        public virtual object _write_register(object address, object value) {
            throw new NotImplementedException();
        }

        // Return byte array of values from start address to length.
        public virtual object _read_block(object start, object length) {
            throw new NotImplementedException();
        }

        // Write out data beginning at start address.
        public virtual object _write_block(object start, object data) {
            throw new NotImplementedException();
        }
    }
}
