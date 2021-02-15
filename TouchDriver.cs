using System;
using System.Device.Gpio;
using System.Device.I2c;
using System.Threading;
using System.Threading.Tasks;

namespace AlarmClockPi
{
    public class TouchDriver : IDisposable
    {
        CAP1188DeviceI2C touch;
        GpioController gpio;
        int TouchIRQPinNumber = -1;

        public TouchDriver(I2cDevice i2cDevice, GpioController gpio, int IRQPinNumber=-1, bool AllowMultiTouch=false)
        {
            this.touch = new CAP1188DeviceI2C(i2cDevice);
            this.touch.InitDevice(AllowMultiTouch);
            this.gpio = gpio;
            this.TouchIRQPinNumber = IRQPinNumber;
            if (this.TouchIRQPinNumber > 0)
            {
                if (this.gpio.IsPinOpen(TouchIRQPinNumber))
                    this.gpio.ClosePin(TouchIRQPinNumber);
                this.gpio.OpenPin(TouchIRQPinNumber, PinMode.Input);

                gpio.RegisterCallbackForPinValueChangedEvent(TouchIRQPinNumber, PinEventTypes.Falling, TouchIRQHandler);
            }
        }

        private void TouchIRQHandler(object sender, PinValueChangedEventArgs args)
        {
            Console.WriteLine($"IRQ on GPIO {args.PinNumber} {args.ChangeType.ToString()}");
            CheckStatus(null);
        }

        public void Dispose()
        {
            if (TouchIRQPinNumber > 0)
            {
                gpio.UnregisterCallbackForPinValueChangedEvent(TouchIRQPinNumber, TouchIRQHandler);
                gpio.ClosePin(TouchIRQPinNumber);
            }
            touch?.Dispose();
            touch = null;
        }

        public void CheckStatus(Object stateInfo)
        {
            AutoResetEvent autoEvent = (AutoResetEvent)stateInfo;

            byte b = touch.touched();
            if (b == 0)
                return;

            if ((b & 0x1) == 1)
            {
                Task.Run(() =>
                {
                    Program.ledRing.PlayAnimation(Program.alexaWake);
                    Program.ledRing.PlayAnimation(Program.alexaThinking);
                    Thread.Sleep(2000);
                    Program.ledRing.PlayAnimation(Program.alexaTalking);
                    Thread.Sleep(1000);
                    Program.ledRing.PlayAnimation(Program.alexaEnd);
                    Program.ledRing.ClearPixels();
                }
                );
            }
            if ((b & 0x80) == 0x80)
            {
                Task.Run(() =>
                {
                    System.Diagnostics.Process.Start("aplay", "-D plughw:0,0 /Apps/magic.wav");
                });
            }

            Console.WriteLine($"Touch : {b}");
        }
    }
}
