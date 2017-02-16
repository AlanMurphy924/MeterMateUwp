using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Gpio;

namespace MeterMateUwp
{
    public class GpioAccess
    {
        private static GpioController controller;

        public static GpioController Controller
        {
            get
            {
                if (controller == null)
                {
                    controller = GpioController.GetDefault();
                }

                return controller;
            }
        }

        private static IDictionary<int, GpioPin> gpioPins;

        public static IDictionary<int, GpioPin> Pins
        {
            get
            {
                if (gpioPins == null)
                {
                    gpioPins = new Dictionary<int, GpioPin>();
                }

                return gpioPins;
            }
        }

        public static GpioPin GetPin(int pin)
        {
            if (!Pins.ContainsKey(pin))
            {
                return null;
            }

            return Pins[pin];
        }

        public static void AddPin(int pin)
        {
            if (Pins.ContainsKey(pin))
            {
                throw new Exception("GPIO Pin already exists.");
            }

            GpioPin newPin = Controller.OpenPin(pin);

            newPin.Write(GpioPinValue.High);
            newPin.SetDriveMode(GpioPinDriveMode.Output);

            Pins.Add(pin, newPin);
        }
    }
}
