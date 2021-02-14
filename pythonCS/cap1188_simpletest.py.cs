
using board;

using busio;

using CAP1188_I2C = adafruit_cap1188.i2c.CAP1188_I2C;

using System;

using System.Linq;

public static class cap1188_simpletest {
    
    public static object i2c = busio.I2C(board.SCL, board.SDA);
    
    public static object cap = CAP1188_I2C(i2c);
    
    // SPI setup
    // from digitalio import DigitalInOut, Direction
    // from adafruit_cap1188.spi import CAP1188_SPI
    // spi = busio.SPI(board.SCK, board.MOSI, board.MISO)
    // cs = DigitalInOut(board.D5)
    // cap = CAP1188_SPI(spi, cs)
    static cap1188_simpletest() {
        while (true) {
            foreach (var i in Enumerable.Range(1, 9 - 1)) {
                if (cap[i].value) {
                    Console.WriteLine("Pin {} touched!".format(i));
                }
            }
        }
    }
}
