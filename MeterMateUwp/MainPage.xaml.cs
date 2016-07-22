using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
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
        public const int MajorVersion = 2;
        public const int MinorVersion = 0;
        public const string Model = "EMR3";

        public MainPage()
        {
            this.InitializeComponent();
        }

        private async Task<SerialDevice> GetSerialPort(string portName)
        {
            // Get the device selector for serial ports
            var deviceSelector = SerialDevice.GetDeviceSelector(); // portName);

            // Find all the serial ports
            var devices = await DeviceInformation.FindAllAsync(deviceSelector);

            foreach (var device in devices)
            {
                if (string.Compare(device.Name, portName, true) == 0)
                {
                    return await SerialDevice.FromIdAsync(device.Id);
                }
            }

            return null;
        }

        public Thermometer Thermometer
        {
            get
            {
                return this.fuelTemperature;
            }
        }

        public TextBlock PresetLitres
        {
            get
            {
                return this.txtPreset;
            }
        }

        public TextBlock RealtimeLitres
        {
            get
            {
                return this.txtRealtimeLitres;
            }
        }

        public Image ProductFlowing
        {
            get
            {
                return imgFlowing;
            }
        }

        public Image ProductNotFlowing
        {
            get
            {
                return imgFlowingDisabled;
            }
        }

        public Image ProductDelivering
        {
            get
            {
                return imgDelivering;
            }
        }

        public Image ProductNotDelivering
        {
            get
            {
                return imgDeliveringDisabled;
            }
        }

        public void ResetTimer()
        {
            imageBluetoothDisabled.Visibility = Visibility.Collapsed;
            imageBluetoothEnabled.Visibility = Visibility.Visible;
            tbHandsetConnected.Visibility = Visibility.Visible;

            timer.Change(12000, 12000);
        }

        private Timer timer;

        private async void TimerExpired(object state)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                imageBluetoothDisabled.Visibility = Visibility.Visible;
                imageBluetoothEnabled.Visibility = Visibility.Collapsed;
                tbHandsetConnected.Visibility = Visibility.Collapsed;
            });
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            // Display the version
            txtVersion.Text = string.Format("MeterMate {0} V{1}.{2}", Model, MajorVersion, MinorVersion);

            tbHandsetConnected.Visibility = Visibility.Collapsed;

            timer = new Timer(TimerExpired, null, 0, 12000);

            SerialDevice meterMatePort = null;
            SerialDevice bluetoothPort = null;

            try
            {
                meterMatePort = await GetSerialPort("usb serial converter"); // "COM12");
                bluetoothPort = await GetSerialPort("minwinpc"); // "COM21");
            }
            catch (NullReferenceException ex)
            {
                //txtStatus.Text = "Could not obtain serial port.";
            }

            Emr3 emr3 = new Emr3(meterMatePort, this);

            emr3.Start();

            Pda pda = new Pda(bluetoothPort, this);

            pda.Start();
        }
    }
}
