
using busio;

using board;

using adafruit_tpa2016;

public static class tpa2016_simpletest {
    
    public static object i2c = busio.I2C(board.SCL, board.SDA);
    
    public static object tpa = adafruit_tpa2016.TPA2016(i2c);
    
    static tpa2016_simpletest() {
        tpa.fixed_gain = -16;
    }
}
