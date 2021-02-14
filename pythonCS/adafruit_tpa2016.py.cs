
using @const = micropython.@const;

using i2cdevice = adafruit_bus_device.i2c_device;

using RWBits = adafruit_register.i2c_bits.RWBits;

using RWBit = adafruit_register.i2c_bit.RWBit;

using System;

public static class adafruit_tpa2016 {
    
    static adafruit_tpa2016() {
        @"
`adafruit_tpa2016`
================================================================================

CircuitPython driver for TPA2016 Class D Amplifier.


* Author(s): Kattni Rembor

Implementation Notes
--------------------

**Hardware:**

* `Adafruit TPA2016 - I2C Control AGC <https://www.adafruit.com/product/1712>`_

**Software and Dependencies:**

* Adafruit CircuitPython firmware for the supported boards:
  https://github.com/adafruit/circuitpython/releases

* Adafruit's Bus Device library: https://github.com/adafruit/Adafruit_CircuitPython_BusDevice
* Adafruit's Register library: https://github.com/adafruit/Adafruit_CircuitPython_Register
";
    }
    
    public static string @__version__ = "0.0.0-auto.0";
    
    public static string @__repo__ = "https://github.com/adafruit/Adafruit_CircuitPython_TPA2016.git";
    
    // Driver for the TPA2016 class D amplifier.
    // 
    //     :param busio.I2C i2c_bus: The I2C bus the TPA2016 is connected to.
    // 
    //     
    public class TPA2016 {
        
        public object _attack_control;
        
        public object _fixed_gain_control;
        
        public object _hold_time_control;
        
        public int _max_gain;
        
        public int _output_limiter_level;
        
        public object _release_control;
        
        public object i2c_device;
        
        public object COMPRESSION_1_1 = @const(0x0);
        
        public object COMPRESSION_2_1 = @const(0x1);
        
        public object COMPRESSION_4_1 = @const(0x2);
        
        public object COMPRESSION_8_1 = @const(0x3);
        
        public object NOISE_GATE_1 = @const(0x0);
        
        public object NOISE_GATE_4 = @const(0x1);
        
        public object NOISE_GATE_10 = @const(0x2);
        
        public object NOISE_GATE_20 = @const(0x3);
        
        public object _attack_control = RWBits(6, 0x02, 0);
        
        public object _release_control = RWBits(6, 0x03, 0);
        
        public object _hold_time_control = RWBits(6, 0x04, 0);
        
        public object _fixed_gain_control = RWBits(6, 0x05, 0);
        
        public object _output_limiter_level = RWBits(5, 0x05, 0);
        
        public object _max_gain = RWBits(4, 0x07, 4);
        
        public object speaker_enable_r = RWBit(0x01, 7);
        
        static TPA2016() {
            @"Enables right speaker. Defaults to enabled. Set to ``False`` to disable.";
            @"Enables left speaker. Defaults to enabled. Set to ``False`` to disable.";
            @"Amplifier shutdown. Amplifier is disabled if ``True``. Defaults to ``False``. If ``True``,
    device is in software shutdown, e.g. control, bias and oscillator are inactive.";
            @"Over-current event on right channel indicated by returning ``True``. Reset by setting to
    ``False``.";
            @"Over-current event on left channel indicated by returning ``True``. Reset by setting to
    ``False``.";
            @"Thermal software shutdown indicated by returning ``True``. Reset by setting to ``False``.";
            @"NoiseGate function enable. Enabled by default. Can only be enabled when compression ratio
    is NOT 1:1. To disable, set to ``False``.";
            @"

    Output limiter disable.

    Enabled by default when compression ratio is NOT 1:1. Can only be
    disabled if compression ratio is 1:1. To disable, set to ``True``.
    ";
            @"
    Noise Gate threshold in mV.

    Noise gate settings are 1mV, 4mV, 10mV, and 20mV. Settings
    options are NOISE_GATE_1, NOISE_GATE_4, NOISE_GATE_10, NOISE_GATE_20. Only functional when
    compression ratio is NOT 1:1. Defaults to 4mV.

    This example sets the noise gate threshold to 10mV.

    .. code-block:: python

        import adafruit_tpa2016
        import busio
        import board

        i2c = busio.I2C(board.SCL, board.SDA)
        tpa = adafruit_tpa2016.TPA2016(i2c)

        tpa.noise_gate_threshold = tpa.NOISE_GATE_10

    ";
            @"
    The compression ratio.

    Ratio settings are: 1:1. 2:1, 4:1, 8:1. Settings options are:
    COMPRESSION_1_1, COMPRESSION_2_1, COMPRESSION_4_1, COMPRESSION_8_1. Defaults to 4:1.

    This example sets the compression ratio to 2:1.

    .. code-block:: python

        import adafruit_tpa2016
        import busio
        import board

        i2c = busio.I2C(board.SCL, board.SDA)
        tpa = adafruit_tpa2016.TPA2016(i2c)

        tpa.compression_ratio = tpa.COMPRESSION_2_1

    ";
        }
        
        public object speaker_enable_l = RWBit(0x01, 6);
        
        public object amplifier_shutdown = RWBit(0x01, 5);
        
        public object reset_fault_r = RWBit(0x01, 4);
        
        public object reset_Fault_l = RWBit(0x01, 3);
        
        public object reset_thermal = RWBit(0x01, 2);
        
        public object noise_gate_enable = RWBit(0x01, 0);
        
        public object output_limiter_disable = RWBit(0x06, 7);
        
        public object noise_gate_threshold = RWBits(2, 0x06, 5);
        
        public object compression_ratio = RWBits(2, 0x07, 0);
        
        public TPA2016(object i2c_bus) {
            this.i2c_device = i2cdevice.I2CDevice(i2c_bus, 0x58);
        }
        
        // The attack time. This is the minimum time between gain decreases. Set to ``1`` - ``63``
        //         where 1 = 0.1067ms and the time increases 0.1067ms with each step, for a maximum of 6.722ms.
        //         Defaults to 5, or 0.5335ms.
        // 
        //         This example sets the attack time to 1, or 0.1067ms.
        // 
        //         .. code-block:: python
        // 
        //             import adafruit_tpa2016
        //             import busio
        //             import board
        // 
        //             i2c = busio.I2C(board.SCL, board.SDA)
        //             tpa = adafruit_tpa2016.TPA2016(i2c)
        // 
        //             tpa.attack_time = 1
        // 
        //         
        public object attack_time {
            get {
                return this._attack_control;
            }
            set {
                if (1 <= value && value <= 63) {
                    this._attack_control = value;
                } else {
                    throw new ValueError("Attack time must be 1 to 63!");
                }
            }
        }
        
        // The release time. This is the minimum time between gain increases. Set to ``1`` - ``63``
        //         where 1 = 0.0137ms, and the time increases 0.0137ms with each step, for a maximum of
        //         0.8631ms. Defaults to 11, or 0.1507ms.
        // 
        //         This example sets release time to 1, or 0.0137ms.
        // 
        //         .. code-block:: python
        // 
        //             import adafruit_tpa2016
        //             import busio
        //             import board
        // 
        //             i2c = busio.I2C(board.SCL, board.SDA)
        //             tpa = adafruit_tpa2016.TPA2016(i2c)
        // 
        //             tpa.release_time = 1
        // 
        //         
        public object release_time {
            get {
                return this._release_control;
            }
            set {
                if (1 <= value && value <= 63) {
                    this._release_control = value;
                } else {
                    throw new ValueError("Release time must be 1 to 63!");
                }
            }
        }
        
        // The hold time. This is the minimum time between attack and release. Set to ``0`` -
        //         ``63`` where 0 = disabled, and the time increases 0.0137ms with each step, for a maximum of
        //         0.8631ms. Defaults to 0, or disabled.
        // 
        //         This example sets hold time to 1, or 0.0137ms.
        // 
        //         .. code-block:: python
        // 
        //             import adafruit_tpa2016
        //             import busio
        //             import board
        // 
        //             i2c = busio.I2C(board.SCL, board.SDA)
        //             tpa = adafruit_tpa2016.TPA2016(i2c)
        // 
        //             tpa.hold_time = 1
        // 
        //         
        public object hold_time {
            get {
                return this._hold_time_control;
            }
            set {
                if (0 <= value && value <= 63) {
                    this._hold_time_control = value;
                } else {
                    throw new ValueError("Hold time must be 0 to 63!");
                }
            }
        }
        
        // The fixed gain of the amplifier in dB. If compression is enabled, fixed gain is
        //         adjustable from ``–28`` to ``30``. If compression is disabled, fixed gain is adjustable
        //         from ``0`` to ``30``.
        // 
        //         The following example sets the fixed gain to -16dB.
        // 
        //         .. code-block:: python
        // 
        //             import adafruit_tpa2016
        //             import busio
        //             import board
        // 
        //             i2c = busio.I2C(board.SCL, board.SDA)
        //             tpa = adafruit_tpa2016.TPA2016(i2c)
        // 
        //             tpa.fixed_gain = -16
        // 
        //         
        public object fixed_gain {
            get {
                return this._fixed_gain_control;
            }
            set {
                if (this.compression_ratio) {
                    if (-28 <= value && value <= 30) {
                        var ratio = value & 0x3F;
                        this._fixed_gain_control = ratio;
                    } else {
                        throw new ValueError("Gain must be -28 to 30!");
                    }
                } else if (0 <= value && value <= 30) {
                    this._fixed_gain_control = value;
                } else {
                    throw new ValueError("Compression is disabled, gain must be 0 to 30!");
                }
            }
        }
        
        // The output limiter level in dBV. Must be between ``-6.5`` and ``9``, set in increments
        //         of 0.5.
        public object output_limiter_level {
            get {
                return -6.5 + 0.5 * this._output_limiter_level;
            }
            set {
                if (-6.5 <= value && value <= 9) {
                    var output = Convert.ToInt32((value + 6.5) / 0.5);
                    this._output_limiter_level = output;
                } else {
                    throw new ValueError("Output limiter level must be -6.5 to 9!");
                }
            }
        }
        
        // The max gain in dB. Must be between ``18`` and ``30``.
        public object max_gain {
            get {
                return this._max_gain + 18;
            }
            set {
                if (18 <= value && value <= 30) {
                    var max_value = value - 18;
                    this._max_gain = max_value;
                } else {
                    throw new ValueError("Max gain must be 18 to 30!");
                }
            }
        }
    }
}
