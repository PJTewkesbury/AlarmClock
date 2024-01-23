using Iot.Device.Display;

using System;
using System.Device.I2c;
using System.Runtime.InteropServices;

namespace AlarmClock.Hardware
{
    //
    // Summary:
    //     Adafruit 1.2" 4-Digit 7-Segment Display w/I2C Backpack
    //
    // Remarks:
    //     Comes in yellow, green and red colors: https://www.adafruit.com/product/1268
    //     https://www.adafruit.com/product/1269 https://www.adafruit.com/product/1270 Sources:
    //     https://github.com/adafruit/Adafruit_LED_Backpack/blob/master/Adafruit_LEDBackpack.cpp
    //     https://github.com/sobek1985/Adafruit_LEDBackpack/blob/master/Adafruit_LEDBackpack/AlphaNumericFourCharacters.cs
    public class LED4x7SegDisplay : Ht16k33, ISevenSegmentDisplay
    {
        /// <summary>
        /// This class is a modified version of the Iot.Device.Display.Large4Digit7SegmentDisplay class which allows the decimal points to be controlled.
        /// </summary>
        /// <param name="i2cDevice"></param>
        public LED4x7SegDisplay(I2cDevice i2cDevice) : base(i2cDevice)
        {
        }

        //
        // Summary:
        //     Digit address within display buffer
        private enum Address
        {
            //
            // Summary:
            //     First digit
            Digit1 = 1,
            //
            // Summary:
            //     Second digit
            Digit2 = 3,
            //
            // Summary:
            //     Dot setting bits
            Dots = 5,
            //
            // Summary:
            //     Third digit
            Digit3 = 7,
            //
            // Summary:
            //     Fourth digit
            Digit4 = 9
        }

        //
        // Summary:
        //     Number of digits supported by display
        private const int MaxNumberOfDigits = 4;

        //
        // Summary:
        //     This display does not support dot bits for each digit, so the first bit should
        //     be masked before flushing to the device
        private const byte SegmentMask = 255; // was 127

        //
        // Summary:
        //     List of digit addresses for sequential writing
        private static readonly Address[] s_digitAddressList = new Address[4]
        {
            Address.Digit1,
            Address.Digit2,
            Address.Digit3,
            Address.Digit4
        };

        //
        // Summary:
        //     Empty display buffer
        private static readonly byte[] s_clearBuffer = new byte[10]
        {
            0,
            0,
            1,
            0,
            2,
            0,
            3,
            0,
            4,
            0
        };

        //
        // Summary:
        //     Display buffer
        private readonly byte[] _displayBuffer = new byte[10]
        {
            0,
            0,
            1,
            0,
            2,
            0,
            3,
            0,
            4,
            0
        };

        public int NumberOfDigits { get; } = 4;

        //
        // Summary:
        //     Gets or sets a single digit's segments by id
        //
        // Parameters:
        //   address:
        //     digit address (0..3)
        //
        // Returns:
        //     Segment in display buffer for the given address
        public Segment this[int address]
        {
            get
            {
                return (Segment)_displayBuffer[TranslateDigitToBufferAddress(address)];
            }
            set
            {
                _displayBuffer[TranslateDigitToBufferAddress(address)] = (byte)value;
                AutoFlush();
            }
        }

        //
        // Summary:
        //     Gets or sets dot configuration
        //
        // Remarks:
        //     The Iot.Device.Display.Large4Digit7SegmentDisplay.Clear method also clears the
        //     dots as well.
        public Dot Dots
        {
            get
            {
                return (Dot)_displayBuffer[5];
            }
            set
            {
                _displayBuffer[5] = (byte)value;
                AutoFlush();
            }
        }

        //
        // Summary:
        //     Translate digit number to buffer address
        //
        // Parameters:
        //   digit:
        //     digit to translate
        //
        // Exceptions:
        //   T:System.ArgumentOutOfRangeException:
        //     digit
        private static int TranslateDigitToBufferAddress(int digit)
        {
            switch (digit)
            {
                case 0:
                    return 1;
                case 1:
                    return 3;
                case 2:
                    return 7;
                case 3:
                    return 9;
                default:
                    throw new ArgumentOutOfRangeException("digit");
            }
        }
        //
        // Exceptions:
        //   T:System.ArgumentOutOfRangeException:
        //     startAddress
        //
        //   T:System.ArgumentOutOfRangeException:
        //     Iot.Device.Display.Large4Digit7SegmentDisplay only supports Iot.Device.Display.Large4Digit7SegmentDisplay.MaxNumberOfDigits
        //     digits
        //
        // Remarks:
        //     Write clears dots, you'll have to reset them afterwards
        public override void Write(ReadOnlySpan<byte> digits, int startAddress = 0)
        {
            if (digits.Length != 0)
            {
                if (startAddress < 0 || startAddress >= 4)
                {
                    throw new ArgumentOutOfRangeException("startAddress");
                }

                if (digits.Length + startAddress > 4)
                {
                    throw new ArgumentOutOfRangeException(string.Format("{0} only supports {1} digits starting from address {2}", "Large4Digit7SegmentDisplay", 4 - startAddress, startAddress));
                }

                ReadOnlySpan<byte> readOnlySpan = digits;
                foreach (byte b in readOnlySpan)
                {
                    _displayBuffer[(int)s_digitAddressList[startAddress++]] = (byte)(b & SegmentMask);
                }

                AutoFlush();
            }
        }

        public void Write(ReadOnlySpan<Segment> digits, int startAddress = 0)
        {
            Write(MemoryMarshal.Cast<Segment, byte>(digits), startAddress);
        }

        public void Write(ReadOnlySpan<Font> characters, int startAddress = 0)
        {
            Write(MemoryMarshal.Cast<Font, byte>(characters), startAddress);
        }

        public override void Clear()
        {
            s_clearBuffer.CopyTo(_displayBuffer, 0);
        }

        public override void Flush()
        {
            _i2cDevice.Write(_displayBuffer);
        }

        //
        // Summary:
        //     Write integer value as decimal digits
        //
        // Parameters:
        //   value:
        //     integer value
        //
        //   alignment:
        //     alignment on display (left or right, right is default)
        //
        // Exceptions:
        //   T:System.ArgumentOutOfRangeException:
        //     value must be between -999..9999
        public void Write(int value, Alignment alignment = Alignment.Right)
        {
            if (value > 9999 || value < -999)
            {
                throw new ArgumentOutOfRangeException("value", "value must be between -999..9999");
            }

            Write(value.ToString(), alignment);
        }

        //
        // Summary:
        //     Write string value to display
        //
        // Parameters:
        //   value:
        //     value to display, max 4 characters, or 5 characters if the 3rd character is ':'
        //     (this also turns on the center colon), or 6 characters if 1st character is also
        //     ':'
        //
        //   alignment:
        //     alignment on display (left or right, right is default)
        //
        // Exceptions:
        //   T:System.ArgumentException:
        //     value[2] must be a ':'
        //
        //   T:System.ArgumentOutOfRangeException:
        //     value can contain maximum 5 characters
        //
        // Remarks:
        //     * Unsupported characters will be replaced as whitespace * This method clears
        //     the buffer before writing, so dots have to be reset afterwards
        public void Write(string value, Alignment alignment = Alignment.Left)
        {
            Clear();
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            if (value[0] == ':' && alignment == Alignment.Left)
            {
                Dots = Dot.LeftColon;
                if (value.Length == 1)
                {
                    return;
                }

                value = value.Substring(1);
            }

            switch (value.Length)
            {
                case 3:
                    if (alignment == Alignment.Left && value[2] == ':')
                    {
                        Dots |= Dot.CenterColon;
                        value = value.Substring(0, 2);
                        break;
                    }

                    if (alignment == Alignment.Right && value[0] == ':')
                    {
                        Dots |= Dot.CenterColon;
                        value = "  " + value.Substring(1);
                        break;
                    }

                    if (alignment == Alignment.Right)
                    {
                        goto IL_019f;
                    }

                    if (alignment == Alignment.Left)
                    {
                        break;
                    }

                    goto default;
                case 4:
                    if (alignment == Alignment.Left && value[2] == ':')
                    {
                        Dots |= Dot.CenterColon;
                        value = value.Substring(0, 2) + value[3];
                    }
                    else if (alignment == Alignment.Right && value[1] == ':')
                    {
                        Dots |= Dot.CenterColon;
                        value = " " + value[0] + value.Substring(2, 2);
                    }

                    break;
                case 5:
                    if (value[2] != ':')
                    {
                        throw new ArgumentException("value", "value[2] must be a ':'");
                    }

                    Dots |= Dot.CenterColon;
                    value = value.Substring(0, 2) + value.Substring(3, 2);
                    break;
                case 1:
                    if (alignment == Alignment.Right)
                    {
                        goto IL_019f;
                    }

                    if (alignment == Alignment.Left)
                    {
                        break;
                    }

                    goto default;
                case 2:
                    if (alignment == Alignment.Right)
                    {
                        goto IL_019f;
                    }

                    if (alignment == Alignment.Left)
                    {
                        break;
                    }

                    goto default;
                default:
                    {
                        throw new ArgumentOutOfRangeException("value", "value can contain maximum 5 characters");
                    }

                IL_019f:
                    value = value.PadLeft(4);
                    break;
            }

            Write(FontHelper.GetString(value));
        }
    }
}