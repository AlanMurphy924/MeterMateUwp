using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.SerialCommunication;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Controls;

namespace MeterMateUwp
{
    public class Pda
    {
        static object lockObj = new Object();

        private Pda()
        {

        }

        public Pda(SerialDevice port, MainPage page)
        {
            SerialPort = port;

            ParentPage = page;

            ReadCancellationTokenSource = new CancellationTokenSource();
        }

        private static MainPage ParentPage
        {
            get;set;
        }

        public static SerialDevice SerialPort
        {
            get;
            private set;
        }

        private static DataWriter writer = null;

        public static DataWriter Writer
        {
            get
            {
                if (writer == null)
                {
                    writer = new DataWriter(SerialPort.OutputStream);
                }
                
                
                return writer;
            }
        }

        private DataReader reader = null;

        public DataReader Reader
        {
            get
            {
                if (reader == null)
                {
                    reader = new DataReader(SerialPort.InputStream);
                }

                return reader;
            }

            private set
            {
                reader = value;
            }
        }

        private const byte STX = 0x02;
        private const byte ETX = 0x03;

        private CancellationTokenSource ReadCancellationTokenSource;

        public async void Start()
        {
            // Create the Serial Port
            SerialPort.BaudRate = 115200;
            SerialPort.DataBits = 8;
            SerialPort.StopBits = SerialStopBitCount.One;
            SerialPort.Parity = SerialParity.None;

            SerialPort.ReadTimeout = TimeSpan.FromMilliseconds(1000.0);
            SerialPort.WriteTimeout = TimeSpan.FromMilliseconds(1000.0);

            await SendMessage("{\"Command\": \"AP\", \"Result\": 0 }");

            // Start background thread.
            await Listen();
        }

        private static async Task SendMessage(string json)
        {
            Writer.WriteString(string.Format("{0}{1}{2}", (char)STX, json, (char)ETX));

            await Writer.StoreAsync();
        }

        private async Task Listen()
        {
            try
            {
                StringBuilder message = new StringBuilder();

                while (true)
                {
                    byte b = await ReadAsync(ReadCancellationTokenSource.Token);

                    switch (b)
                    {
                        case STX:

                            // Start of message
                            message.Clear();
                            continue;

                        case ETX:
                            
                            // End of message
                            await ProcessMessage(message.ToString());
                            message.AppendLine();
                            continue;

                        default:
                            
                            // Append on to the message
                            message.Append((char)b);
                            continue;
                    }
                }
            }
            catch (Exception e)
            {
                string message = e.Message;
            }
            finally
            {
                if (Reader != null)
                {
                    Reader.DetachStream();
                    Reader = null;
                }
            }
        }

        private async Task<byte> ReadAsync(CancellationToken cancellationToken)
        {
            uint readBufferLength = 1;

            // If task cancellation was requested, comply
            cancellationToken.ThrowIfCancellationRequested();

            // Set InputStreamOptions to complete the asynchronous read operation 
            // when one or more bytes is available
            Reader.InputStreamOptions = InputStreamOptions.Partial;

            uint bytesRead = await Reader.LoadAsync(readBufferLength);

            if (bytesRead > 0)
            {
                return Reader.ReadByte();
            }

            return 0x00;
        }

        private static AsyncLock myLock = new AsyncLock();

        public static async Task ProcessMessage(string message)
        {
            using (var releaser = await myLock.LockAsync())
            {
                try
                {
                    // Construct default (error) JSON.
                    string json = "{\"Command\": \"\", \"Result\": -99}";

                    // Check EMR3 thread is running. If it is
                    // not then wait until it is.
                    while (!Emr3.IsRunning())
                    {
                        await Task.Delay(250);
                    }

                    // Split the message around commas.
                    string[] parts = message.Split(new char[] { ',' });

                    // Check there is at least one part
                    if (parts.Length >= 1)
                    {
                        switch (parts[0])
                        {
                            case "BL":  

                                // Bootloader.
                                //SystemUpdate.AccessBootloader();
                                break;

                            case "Gv":

                                // Get Version.
                                //await ParentPage.ResetTimer();

                                json = "{\"Command\": \"Gv\", \"Result\": 0, \"Version\": " + MainPage.MajorVersion + "." + MainPage.MinorVersion + ", \"Model\": \"" + MainPage.Model + "\"}";

                                break;

                            case "Gf":  

                                // Get features.
                                json = Emr3.GetFeatures();

                                break;

                            case "Gt": 
                                 
                                // Get temperature.
                                json = await Emr3.GetTemperature();

                                break;

                            case "Gs":  

                                // Get status.
                                json = await Emr3.GetStatus();

                                break;

                            case "Gpl":  

                                // Get preset litres.
                                json = await Emr3.GetPreset();

                                break;

                            case "Grl":  

                                // Get realtime litres.
                                json = await Emr3.GetRealtime();

                                break;

                            case "Gtc":  

                                // Get transaction count
                                json = await Emr3.GetTranCount();

                                break;

                            case "Gtr":

                                // Get transaction record
                                await ParentPage.ResetTimer();

                                if (parts.Length == 2)
                                {
                                    json = await Emr3.GetTran(parts[1]);
                                }

                                break;

                            case "Spl": 

                                // Set polling.
                                if (parts.Length == 2)
                                {
                                    json = Emr3.SetPolling(parts[1]);
                                }

                                break;

                            case "Sp":

                                // Set preset.
                                await ParentPage.ResetTimer();

                                if (parts.Length == 2)
                                {
                                    json = await Emr3.SetPreset(parts[1]);
                                }

                                break;

                            case "NOP":

                                await ParentPage.ResetTimer();

                                json = "{\"Command\": \"NOP\", \"Result\": 0}";

                                break;

                            default:

                                json = "{\"Command\": \"" + parts[0] + "\", \"Result\": -99}";

                                break;
                        }
                    }

                    // Send reply to PDA.
                    await SendMessage(json);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("PDA.ProcessMessage: Exception " + ex.Message);
                }
            }
        }
    }
}
