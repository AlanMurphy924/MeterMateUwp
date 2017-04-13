using Gauges;
using LedControls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.Gpio;
using Windows.Devices.I2c;
using Windows.Devices.SerialCommunication;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace MeterMateUwp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private const int EMR_CONNECTED_LED_PIN = 5;
        private const int BT_CONNECTED_PIN = 6;
        public const int DELIVERING_PIN = 13;
        public const int FLOWING_PIN = 19;

        public const int MajorVersion = 3;
        public const int MinorVersion = 0;
        public const string Model = "EMR3";

        public MainPage()
        {
            this.InitializeComponent();

            InitGpio();
        }

        private void InitGpio()
        {
            GpioAccess.AddPin(EMR_CONNECTED_LED_PIN);
            GpioAccess.AddPin(BT_CONNECTED_PIN);
            GpioAccess.AddPin(FLOWING_PIN);
            GpioAccess.AddPin(DELIVERING_PIN);
        }

        private async Task<SerialDevice> GetSerialPort(string portName)
        {
            // Get the device selector for serial ports
            var deviceSelector = SerialDevice.GetDeviceSelector();

            // Find all the serial ports
            var devices = await DeviceInformation.FindAllAsync(deviceSelector);

            // If the are any devices found, attempt to find the one required
            if (devices.Count > 0)
            {
                // Loop through all discovered serial devices
                foreach (var device in devices)
                {
                    // If the device name matches return the serial device
                    if (string.Compare(device.Name, portName, true) == 0)
                    {
                        return await SerialDevice.FromIdAsync(device.Id);
                    }
                }
            }

            // Serial Device was not found so return null
            return null;
        }

        public ThermometerGauge Thermometer
        {
            get
            {
                return this.fuelTemperature;
            }
        }

        public PumpingGauge Pumping
        {
            get
            {
                return this.litres;
            }
        }

        public SimpleLed ProductDelivering
        {
            get
            {
                return ledProductDelivering;
            }
        }

        public SimpleLed ProductFlowing
        {
            get
            {
                return ledProductFlowing;
            }
        }

        public SimpleLed HandsetConnected
        {
            get
            {
                return ledHandsetConnected;
            }
        }

        public SimpleLed EmrConnected
        {
            get
            {
                return ledEmr3Connected;
            }
        }

        public async Task Emr3ConnectionConnected(bool isConnected)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                ledEmr3Connected.LedOn = isConnected;

                var pin = GpioAccess.Pins[EMR_CONNECTED_LED_PIN];

                if (pin != null)
                {
                    pin.Write(isConnected ? GpioPinValue.Low : GpioPinValue.High);
                }
            });
        }

        public async Task BluetoothConnectionConnected(bool isConnected)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                ledHandsetConnected.LedOn = isConnected;

                var pin = GpioAccess.Pins[BT_CONNECTED_PIN];

                if (pin != null)
                {
                    pin.Write(isConnected ? GpioPinValue.Low : GpioPinValue.High);
                }
            });
        }

        public async Task ResetTimer()
        {
            await BluetoothConnectionConnected(true);

            // set the time to trigger every 12 seconds
            timer.Change(12000, 12000);
        }

        private Timer timer;

        private async void TimerExpired(object state)
        {
            await BluetoothConnectionConnected(false);
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            // Display the version
            txtVersion.Text = string.Format("MeterMate {0} V{1}.{2}", Model, MajorVersion, MinorVersion);

            // Display the Copyright notice
            txtCopyright.Text = "Swiftsoft - Copyright © 2016-2017";

            // Show that the handset by default is not connected
            ledHandsetConnected.LedOn = false;
            ledEmr3Connected.LedOn = false;

            // Create timer to expire immediately and then every 12 seconds thereafter
            timer = new Timer(TimerExpired, null, 0, 12000);

            SerialDevice meterMatePort = null;
            SerialDevice bluetoothPort = null;

            try
            {
                // meterMatePort = await GetSerialPort("prolific usb-to-serial comm port");
                //meterMatePort = await GetSerialPort("usb serial converter");
                meterMatePort = await GetSerialPort("USB-RS232 Cable");
                //meterMatePort = await GetSerialPort("usb <-> serial");
                //meterMatePort = await GetSerialPort("cp2102 usb to uart bridge controller");
                bluetoothPort = await GetSerialPort("minwinpc");
            }
            catch (NullReferenceException)
            {
                //txtStatus.Text = "Could not obtain serial port.";
            }

            // Start the EMR3 thread
            Emr3 emr3 = new Emr3(meterMatePort, this);

            emr3.Start();

            // Start the PDA thread
            Pda pda = new Pda(bluetoothPort, this);

            pda.Start();
        }
    }
}
