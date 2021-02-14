
using time;

using SevenSegment = Adafruit_LED_Backpack.SevenSegment;

using System.Collections.Generic;

using System;

using System.Linq;

public static class sevensegment_test {
    
    public static object display = SevenSegment.SevenSegment();
    
    static sevensegment_test() {
        display.begin();
        display.clear();
        display.print_float(i);
        display.set_colon(colon);
        display.write_display();
        time.sleep(1.0);
        display.clear();
        display.print_float(i, decimal_digits: 1);
        display.set_colon(colon);
        display.write_display();
        time.sleep(1.0);
        display.clear();
        display.print_float(i, decimal_digits: 0, justify_right: false);
        display.set_colon(colon);
        display.write_display();
        time.sleep(1.0);
        display.clear();
        display.print_hex(i);
        display.set_colon(colon);
        display.write_display();
        time.sleep(0.25);
        display.set_invert(true);
        display.clear();
        display.print_hex(i);
        display.set_colon(colon);
        display.write_display();
        time.sleep(0.25);
        display.set_invert(false);
    }
    
    public static bool colon = false;
    
    public static List<double> numbers = new List<double> {
        0.0,
        1.0,
        -1.0,
        0.55,
        -0.55,
        10.23,
        -10.2,
        100.5,
        -100.5
    };
    
    public static bool colon = !colon;
    
    static sevensegment_test() {
        Console.WriteLine("Press Ctrl-C to quit.");
        while (true) {
            foreach (var i in numbers) {
                // Clear the display buffer.
                // Print a floating point number to the display.
                // Set the colon on or off (True/False).
                // Write the display buffer to the hardware.  This must be called to
                // update the actual display LEDs.
                // Delay for a second.
            }
            foreach (var i in numbers) {
            }
            foreach (var i in numbers) {
            }
            foreach (var i in Enumerable.Range(0, 0xFF)) {
            }
            foreach (var i in Enumerable.Range(0, 0xFF)) {
            }
        }
    }
}
