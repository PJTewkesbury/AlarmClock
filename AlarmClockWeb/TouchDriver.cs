using System;
using System.Device.Gpio;
using System.Device.I2c;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

namespace AlarmClockPi
{
    public class TouchDriver : IDisposable
    {
        CAP1188DeviceI2C touch;
        GpioController gpio;
        int TouchIRQPinNumber = -1;

        public event EventHandler<TouchEventArgs> OnTouched = null;
        PinValue pv;

        public Subject<byte> rxTouchInternal { get; private set; } = null;

        public IObservable<byte> rxTouch { get; private set; } = null;

        public TouchDriver(I2cDevice i2cDevice, GpioController gpio, int IRQPinNumber = -1, bool AllowMultiTouch = false)
        {
            this.touch = new CAP1188DeviceI2C(i2cDevice);
            this.touch.InitDevice(AllowMultiTouch);
            this.gpio = gpio;
            this.TouchIRQPinNumber = IRQPinNumber;
            if (this.TouchIRQPinNumber > 0)
            {
                if (this.gpio.IsPinOpen(TouchIRQPinNumber))
                    this.gpio.ClosePin(TouchIRQPinNumber);
                this.gpio.OpenPin(TouchIRQPinNumber, PinMode.InputPullDown);
                gpio.RegisterCallbackForPinValueChangedEvent(TouchIRQPinNumber, PinEventTypes.Falling, TouchIRQHandler);
            }

            // Needed to make IRQ work correctly
            byte t = touch.touched();
            Console.WriteLine($"Touch : {t.ToString("D3")} ");
            if (TouchIRQPinNumber > 0)
            {
                PinValue pvNew = this.gpio.Read(TouchIRQPinNumber);
                Console.WriteLine($"GPIO12 Changed to : {(pvNew == PinValue.Low ? "Low" : "High")} ");
            }

            // Debounce touch input and create an observable to allow caller to get debounced events
            rxTouchInternal = new Subject<byte>();
            rxTouch = rxTouchInternal.Throttle(TimeSpan.FromMilliseconds(200));            
        }

        private void TouchIRQHandler(object sender, PinValueChangedEventArgs args)
        {
            // Send touch byte to Observerable
            byte t = touch.touched();
            Console.WriteLine($"IRQ on GPIO {args.PinNumber} {args.ChangeType.ToString()} : Touch Value : {t}");
            rxTouchInternal.OnNext(t);

            if (OnTouched != null)
            {
                TouchEventArgs touchArgs = new TouchEventArgs();
                touchArgs.Touched = t;
                OnTouched.Invoke(this, touchArgs);
            }
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

        //public void CheckStatus(Object stateInfo)
        //{
        //    AutoResetEvent autoEvent = (AutoResetEvent)stateInfo;

        //    byte b = touch.touched();
        //    if (b == 0)
        //        return;

        //    PinValue gpio12 = gpio.Read(TouchIRQPinNumber);

        //    Console.WriteLine($"Touch {b:d3} {(gpio12== PinValue.High?"High":"Low")} ");

        //    if ((b & 0x1) == 1)
        //    {
        //        Task.Run(() =>
        //        {
        //            Program.ledRing.PlayAnimation(Program.alexaWake);
        //            Program.ledRing.PlayAnimation(Program.alexaThinking);
        //            Thread.Sleep(2000);
        //            Program.ledRing.PlayAnimation(Program.alexaTalking);
        //            Thread.Sleep(1000);
        //            Program.ledRing.PlayAnimation(Program.alexaEnd);
        //            Program.ledRing.ClearPixels();
        //        }
        //        );
        //    }
        //    if ((b & 0x80) == 0x80)
        //    {
        //        Task.Run(() =>
        //        {
        //            System.Diagnostics.Process.Start("aplay", "-D plughw:0,0 /Apps/magic.wav");
        //        });
        //    }

        //    Console.WriteLine($"Touch : {b}");
        //}
    }

    public class TouchEventArgs : EventArgs
    {
        /// <summary>
        /// int containing bit flags of which touch sensors have been pressed.
        /// </summary>
        public int Touched { get; set; } = 0;
    }
}
