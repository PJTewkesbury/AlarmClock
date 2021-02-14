
using System.Collections.Generic;

using System.Collections;

using System.Linq;

public static class SevenSegment {
    
    public static Dictionary<string, int> DIGIT_VALUES = new Dictionary<object, object> {
        {
            " ",
            0x00},
        {
            "-",
            0x40},
        {
            "0",
            0x3F},
        {
            "1",
            0x06},
        {
            "2",
            0x5B},
        {
            "3",
            0x4F},
        {
            "4",
            0x66},
        {
            "5",
            0x6D},
        {
            "6",
            0x7D},
        {
            "7",
            0x07},
        {
            "8",
            0x7F},
        {
            "9",
            0x6F},
        {
            "A",
            0x77},
        {
            "B",
            0x7C},
        {
            "C",
            0x39},
        {
            "D",
            0x5E},
        {
            "E",
            0x79},
        {
            "F",
            0x71}};
    
    public static Dictionary<string, int> IDIGIT_VALUES = new Dictionary<object, object> {
        {
            " ",
            0x00},
        {
            "-",
            0x40},
        {
            "0",
            0x3F},
        {
            "1",
            0x30},
        {
            "2",
            0x5B},
        {
            "3",
            0x79},
        {
            "4",
            0x74},
        {
            "5",
            0x6D},
        {
            "6",
            0x6F},
        {
            "7",
            0x38},
        {
            "8",
            0x7F},
        {
            "9",
            0x7D},
        {
            "A",
            0x7E},
        {
            "B",
            0x67},
        {
            "C",
            0x0F},
        {
            "D",
            0x73},
        {
            "E",
            0x4F},
        {
            "F",
            0x4E}};
    
    // Seven segment LED backpack display.
    public class SevenSegment
        : HT16K33.HT16K33 {
        
        public object invert;
        
        public SevenSegment(object invert = false, Hashtable kwargs) {
            this.invert = invert;
        }
        
        // Set whether the display is upside-down or not.
        //         
        public virtual object set_invert(object _invert) {
            this.invert = _invert;
        }
        
        // Set digit at position to raw bitmask value.  Position should be a value
        //         of 0 to 3 with 0 being the left most digit on the display.
        public virtual void set_digit_raw(object pos, object bitmask) {
            if (pos < 0 || pos > 3) {
                // Ignore out of bounds digits.
                return;
            }
            // Jump past the colon at position 2 by adding a conditional offset.
            var offset = pos < 2 ? 0 : 1;
            // Calculate the correct position depending on orientation
            if (this.invert) {
                pos = 4 - (pos + offset);
            } else {
                pos = pos + offset;
            }
            // Set the digit bitmask value at the appropriate position.
            this.buffer[pos * 2] = bitmask & 0xFF;
        }
        
        // Turn decimal point on or off at provided position.  Position should be
        //         a value 0 to 3 with 0 being the left most digit on the display.  Decimal
        //         should be True to turn on the decimal point and False to turn it off.
        //         
        public virtual void set_decimal(int pos, bool @decimal) {
            if (pos < 0 || pos > 3) {
                // Ignore out of bounds digits.
                return;
            }
            // Jump past the colon at position 2 by adding a conditional offset.
            var offset = pos < 2 ? 0 : 1;
            // Calculate the correct position depending on orientation
            if (this.invert) {
                pos = 4 - (pos + offset);
            } else {
                pos = pos + offset;
            }
            // Set bit 7 (decimal point) based on provided value.
            if (@decimal) {
                this.buffer[pos * 2] |= 1 << 7;
            } else {
                this.buffer[pos * 2] |= ~(1 << 7);
            }
        }
        
        // Set digit at position to provided value.  Position should be a value
        //         of 0 to 3 with 0 being the left most digit on the display.  Digit should
        //         be a number 0-9, character A-F, space (all LEDs off), or dash (-).
        //         
        public virtual void set_digit(int pos, object digit, bool @decimal = false) {
            if (this.invert) {
                this.set_digit_raw(pos, IDIGIT_VALUES.get(digit.ToString().upper(), 0x00));
            } else {
                this.set_digit_raw(pos, DIGIT_VALUES.get(digit.ToString().upper(), 0x00));
            }
            if (@decimal) {
                this.set_decimal(pos, true);
            }
        }
        
        // Turn the colon on with show colon True, or off with show colon False.
        public virtual object set_colon(object show_colon) {
            if (show_colon) {
                this.buffer[4] |= 0x02;
            } else {
                this.buffer[4] |= ~0x02 & 0xFF;
            }
        }
        
        // Turn the left colon on with show color True, or off with show colon
        //         False.  Only the large 1.2" 7-segment display has a left colon.
        //         
        public virtual object set_left_colon(object show_colon) {
            if (show_colon) {
                this.buffer[4] |= 0x04;
                this.buffer[4] |= 0x08;
            } else {
                this.buffer[4] |= ~0x04 & 0xFF;
                this.buffer[4] |= ~0x08 & 0xFF;
            }
        }
        
        // Turn on/off the single fixed decimal point on the large 1.2" 7-segment
        //         display.  Set show_decimal to True to turn on and False to turn off.
        //         Only the large 1.2" 7-segment display has this decimal point (in the
        //         upper right in the normal orientation of the display).
        //         
        public virtual object set_fixed_decimal(object show_decimal) {
            if (show_decimal) {
                this.buffer[4] |= 0x10;
            } else {
                this.buffer[4] |= ~0x10 & 0xFF;
            }
        }
        
        // Print a 4 character long string of numeric values to the display.
        //         Characters in the string should be any supported character by set_digit,
        //         or a decimal point.  Decimal point characters will be associated with
        //         the previous character.
        //         
        public virtual void print_number_str(string value, bool justify_right = true) {
            // Calculate length of value without decimals.
            var length = map(x => x != "." ? 1 : 0, value).Sum();
            // Error if value without decimals is longer than 4 characters.
            if (length > 4) {
                this.print_number_str("----");
                return;
            }
            // Calculcate starting position of digits based on justification.
            var pos = justify_right ? 4 - length : 0;
            // Go through each character and print it on the display.
            foreach (var _tup_1 in value.Select((_p_1,_p_2) => Tuple.Create(_p_2, _p_1))) {
                var i = _tup_1.Item1;
                var ch = _tup_1.Item2;
                if (ch == ".") {
                    // Print decimal points on the previous digit.
                    this.set_decimal(pos - 1, true);
                } else {
                    this.set_digit(pos, ch);
                    pos += 1;
                }
            }
        }
        
        // Print a numeric value to the display.  If value is negative
        //         it will be printed with a leading minus sign.  Decimal digits is the
        //         desired number of digits after the decimal point.
        //         
        public virtual object print_float(object value, object decimal_digits = 2, object justify_right = true) {
            var format_string = "{{0:0.{0}F}}".format(decimal_digits);
            this.print_number_str(format_string.format(value), justify_right);
        }
        
        // Print a numeric value in hexadecimal.  Value should be from 0 to FFFF.
        //         
        public virtual object print_hex(object value, object justify_right = true) {
            if (value < 0 || value > 0xFFFF) {
                // Ignore out of range values.
                return;
            }
            this.print_number_str("{0:X}".format(value), justify_right);
        }
    }
}
