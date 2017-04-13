using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Gpio;
using Windows.Devices.SerialCommunication;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace MeterMateUwp
{
    public class Emr3
    {
        static bool isRunning = false;
        static bool pollStatus = false;

        static bool InDeliveryMode = false;
        static bool ProductFlowing = false;
        static bool MeterError = false;
        static bool InCalibration = false;

        private const byte DELIMITER = 0x7e;
        private const byte ESCAPE_CHAR = 0x7d;
        private const byte DESTINATION = 0x01;
        private const byte SOURCE = 0xff;

        private const string JSON_FAILURE = "\"Result\": -1";

        private Emr3()
        {

        }

        public Emr3(SerialDevice port, MainPage page)
        {
            SerialPort = port;

            ParentPage = page;
 
            ReadCancellationTokenSource = new CancellationTokenSource();
        }

        private static MainPage ParentPage
        {
            get;set;
        }

        public static bool IsRunning()
        {
            return isRunning;
        }

        public static SerialDevice SerialPort
        {
            get;
            private set;
        }

        private const byte STX = 0x02;
        private const byte ETX = 0x03;

        private static CancellationTokenSource ReadCancellationTokenSource;

        public void Start()
        {
            // Create the Serial Port
            SerialPort.BaudRate = 9600;
            SerialPort.DataBits = 8;
            SerialPort.StopBits = SerialStopBitCount.One;
            SerialPort.Parity = SerialParity.None;

            SerialPort.ReadTimeout = TimeSpan.FromMilliseconds(1000.0);
            SerialPort.WriteTimeout = TimeSpan.FromMilliseconds(1000.0);

            main();

            isRunning = true;
        }

        private async void main()
        {
            int idx = 0;

            // Poll forever
            while (true)
            {
                try
                {
                    // Poll meter for realtime litres.
                    await Pda.ProcessMessage("Grl");

                    switch (idx)
                    {
                        case 0:
                            // Poll meter for status.
                            await Pda.ProcessMessage("Gs");
                            break;

                        case 1:
                            // Poll meter for preset litres.
                            await Pda.ProcessMessage("Gpl");
                            break;

                        case 2:
                            // Poll meter for current temperature.
                            await Pda.ProcessMessage("Gt");
                            break;

                        case 3:
                            // Poll MeterMate for Version/Model
                            await Pda.ProcessMessage("Gv");
                            break;
                    }

                    if (++idx == 4)
                    {
                        idx = 0;
                    }
                }
                catch (Exception)
                {
                }

                // A message is sent every 100 ms
                await Task.Delay(TimeSpan.FromMilliseconds(50));
            }
        }

        private static string CreateCommandResponse(string command, string json)
        {
            StringBuilder builder = new StringBuilder();

            builder.Append("{\"Command\": \"");
            builder.Append(command);
            builder.Append("\", ");
            builder.Append(json);
            builder.Append("}");

            return builder.ToString();
        }

        public static string GetFeatures()
        {
            return CreateCommandResponse("Gtr", "\"Result\": 0, \"Features\": \"Preset,Realtime\""); 
        }

        public static byte[] CreateGetPresetMessage()
        {
            return Encoding.ASCII.GetBytes("Gc");
        }

        public static async Task<string> GetPreset()
        {
            // Assume failure.
            string jsonBody = JSON_FAILURE;

            try
            {
                // Send message to meter.
                byte[] reply = await SendMessage(CreateGetPresetMessage());

                if (reply != null)
                {
                    // Meter reply is Fc followed by 4 byte float.
                    if (reply[0] == 'F' && reply[1] == 'c')
                    {
                        int presetLitres = (int)reply.GetSingle(2);

                        jsonBody = string.Format("\"Result\": 0, \"Litres\": {0}", presetLitres);

                        // Update the preset litres displayed on screen
                        await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                        {
                            ParentPage.Pumping.PresetLitres = presetLitres;
                        });
                        //ParentPage.PresetLitres.Text = presetLitres.ToString("#,##0");
                    }
                }
            }
            catch (Exception)
            {
            }

            return CreateCommandResponse("Gpl", jsonBody);
        }

        private static byte[] CreateGetRealtimeLitresMessage()
        {
            return Encoding.ASCII.GetBytes("GK");
        }

        public static async Task<string> GetRealtime()
        {
            // Assume failure.
            string jsonBody = JSON_FAILURE;

            try
            {
                // Send message to meter.
                byte[] reply = await SendMessage(CreateGetRealtimeLitresMessage());

                if (reply != null)
                {
                    // Meter reply is FK followed by 4 byte float.
                    if (reply[0] == 'F' && reply[1] == 'K')
                    {
                        int litres = (int)reply.GetDouble(2);

                        jsonBody = "\"Result\": 0, \"Litres\": " + litres;

                        // Only update the pumped litres if product is actually being pumped
                        if (ParentPage.ProductDelivering.LedOn)
                        {
                            // Update the realtime litres displayed on screen
                            ParentPage.Pumping.DeliveredLitres = litres;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("EMR3.GetRealtime: Exception " + ex.Message);
            }

            return CreateCommandResponse("Grl", jsonBody); 
        }

        private static byte[] CreateGetStatusMessage()
        {
            return new byte[] { (byte)'T', 0x01 };
        }

        public static async Task<string> GetStatus()
        {
            // Assume failure.
            string jsonBody = JSON_FAILURE;

            try
            {
                // Send message to meter.
                byte[] reply = await SendMessage(CreateGetStatusMessage());

                if (reply != null)
                {
                    // There must be 3 bytes in the reply
                    if (reply.Length == 3)
                    {
                        bool newInDeliveryMode = false;
                        bool newProductFlowing = false;

                        // Update Data class.
                        if ((reply[2] & 0x01) == 0x01)
                        {
                            newInDeliveryMode = false;
                            newProductFlowing = false;
                        }

                        if ((reply[2] & 0x02) == 0x02)
                        {
                            newInDeliveryMode = true;
                            newProductFlowing = true;
                        }

                        if ((reply[2] & 0x04) == 0x04)
                        {
                            newInDeliveryMode = true;
                            newProductFlowing = false;
                        }

                        if ((reply[2] & 0x08) == 0x08)
                        {
                            newInDeliveryMode = false;
                            newProductFlowing = true;
                        }

                        InDeliveryMode = newInDeliveryMode;

                        ParentPage.ProductDelivering.LedOn = InDeliveryMode;

                        var deliveringPin = GpioAccess.Pins[MainPage.DELIVERING_PIN];

                        if (deliveringPin != null)
                        {
                            deliveringPin.Write(InDeliveryMode ? GpioPinValue.Low : GpioPinValue.High);
                        }

                        ProductFlowing = newProductFlowing;

                        ParentPage.ProductFlowing.LedOn = ProductFlowing;

                        var flowingPin = GpioAccess.Pins[MainPage.FLOWING_PIN];

                        if (flowingPin != null)
                        {
                            flowingPin.Write(ProductFlowing ? GpioPinValue.Low : GpioPinValue.High);
                        }

                        MeterError = ((reply[2] & 0x40) == 0x40);
                        InCalibration = ((reply[2] & 0x80) == 0x80);

                        jsonBody = "\"Result\": 0, \"InDeliveryMode\": " + InDeliveryMode + ", \"ProductFlowing\": " + ProductFlowing + ", \"Error\": " + MeterError + ", \"InCalibration\": " + InCalibration;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("EMR3.GetStatus: Exception " + ex.Message);
            }

            return CreateCommandResponse("Gs", jsonBody); 
        }

        private static byte[] CreateGetTemperatureMessage()
        {
            return Encoding.ASCII.GetBytes("Gt");
        }

        public static async Task<string> GetTemperature()
        {
            // Assume failure.
            string jsonBody = JSON_FAILURE;

           try
            {
                byte[] reply = await SendMessage(CreateGetTemperatureMessage());

                if (reply != null)
                {
                    // Meter reply is "Ft" followed by 4 bytes holding
                    // a float of the temperature in Farenheit
                    if (reply[0] == 'F' && reply[1] == 't')
                    {
                        // Retrieve the temperature in Farenheit & convert to centigrade
                        Temperature temperature = Temperature.CreateFromFahrenheit(reply.GetSingle(2));

                        jsonBody = "\"Result\": 0, \"Temp\": " + temperature.Celsius.ToString("n1"); 

                        // Update the thermometer control with the temperature in Celsius
                        ParentPage.Thermometer.Temperature = temperature.Celsius;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("EMR3.GetTemperature: Exception " + ex.Message);
            }

            return CreateCommandResponse("Gt", jsonBody); 
        }

        private static byte[] CreateGetTransactionMessage(string transactionNumber)
        {
            // Parse to the transaction number
            uint transNo = uint.Parse(transactionNumber);

            byte[] message = new byte[] { (byte)'H', 0x01, (byte)(transNo >> 8), (byte)transNo };

            return message;
        }

        public static async Task<string> GetTran(string tranNo)
        {
            // Assume failure.
            string jsonBody = JSON_FAILURE;

            try
            {
                // Send message to meter.
                byte[] reply = await SendMessage(CreateGetTransactionMessage(tranNo));

                if (reply != null)
                {
                    // Meter reply is I 0x03 followed by 146 bytes of ticket data.
                    if (reply[0] == 'I' && reply[1] == 0x03)
                    {
                        StringBuilder builder = new StringBuilder();

                        // Build JSON string.
                        builder.Append("\"Result\": 0, ");

                        // Ticket no.
                        builder.AppendFormat("\"TicketNo\":{0},", reply.GetUnsignedInt(2));

                        // Not sure about this stuff!
                        builder.AppendFormat("\"TranType\":{0},", reply.GetUnsignedShort(6));

                        builder.AppendFormat("\"Index\":{0},", reply[8]);

                        builder.AppendFormat("\"NoSummaryRecords\":{0},", reply[9]);

                        builder.AppendFormat("\"NoRecordsSummarised\":{0},", reply[10]);

                        // Product ID.
                        builder.AppendFormat("\"ProductID\":{0},", reply[11]);

                        // Product description.
                        byte[] product = new byte[16];                        
                        Array.Copy(reply, 12, product, 0, 16);

                        builder.AppendFormat("\"ProductDesc\":\"{0}\",", new string(Encoding.UTF8.GetChars(product)));

                        // Start date/time.
                        builder.Append("\"Start\":\"");
                        builder.Append(reply[30]);
                        builder.Append("/");
                        builder.Append(reply[32]);
                        builder.Append("/");
                        builder.Append(reply[33] + 2000);
                        builder.Append(" ");
                        builder.Append(reply[29]);
                        builder.Append(":");
                        builder.Append(reply[28]);
                        builder.Append(":");
                        builder.Append(reply[31]);
                        builder.Append("\",");

                        // Finish date/time.
                        builder.Append("\"Finish\":\"");
                        builder.Append(reply[36]);
                        builder.Append("/");
                        builder.Append(reply[38]);
                        builder.Append("/");
                        builder.Append(reply[39] + 2000);
                        builder.Append(" ");
                        builder.Append(reply[35]);
                        builder.Append(":");
                        builder.Append(reply[34]);
                        builder.Append(":");
                        builder.Append(reply[37]);
                        builder.Append("\",");

                        // Totalisers.
                        builder.AppendFormat("\"totaliserStart\":{0},", reply.GetDouble(48));

                        builder.AppendFormat("\"totaliserEnd\":{0},", reply.GetDouble(56));

                        // Volume.
                        builder.AppendFormat("\"grossVolume\":{0},", reply.GetDouble(64));

                        builder.AppendFormat("\"volume\":{0},", reply.GetDouble(72));

                        // Retrieve the temperature in Farenheit & convert to Centigrade
                        Temperature temperature = Temperature.CreateFromFahrenheit(reply.GetSingle(80));

                        builder.AppendFormat("\"temperature\":{0},", temperature.Celsius.ToString("n1"));

                        // Flags.
                        builder.AppendFormat("\"flags\":{0}", reply.GetUnsignedShort(126));

                        jsonBody = builder.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("EMR3.GetTran: Exception " + ex.Message);
            }

            return CreateCommandResponse("Gtr", jsonBody); 
        }

        private static byte[] CreateGetTransactionCountMessage()
        {
            return new byte[] { (byte)'H', 0x00 };
        }

        public static async Task<string> GetTranCount()
        {
            // Assume failure.
            string jsonBody = JSON_FAILURE;

            try
            {
                byte[] reply = await SendMessage(CreateGetTransactionCountMessage());
                    
                if (reply != null)
                {
                    // Meter reply is I 0x00 followed by 2 byte counter.
                    if (reply[0] == 'I' && reply[1] == 0x00)
                    {
                        jsonBody = "\"Result\": 0, \"TranCount\": " + reply.GetSingle(2);
                    }
                }
            }
            catch (Exception)
            {
            }

            return CreateCommandResponse("Gtc", jsonBody); 
        }

        public static string SetPolling(string flag)
        {
            pollStatus = flag == "1";

            return CreateCommandResponse("Spl", "\"Result\": 0"); 
        }

        public static async Task<string> SetPreset(string litresString)
        {
            // Assume failure.
            string jsonBody = JSON_FAILURE;

            try
            {
                // Convert string to a float.
                int litres = 0;
                try
                {
                    litres = Convert.ToInt32(litresString);
                }
                catch
                {
                }

                // Before presetting the meter, we press the MODE button 
                // until the device is in security mode. This is done to
                // ensure the meter is awake and after changing the preset
                // it helps to redraw the screen display too.
                byte[] reply2 = null;

                for (int i = 0; i < 3; i++)
                {
                    // Press MODE button.
                    byte[] message1 = new byte[3];
                    message1[0] = (byte)'S';
                    message1[1] = (byte)'u';
                    message1[2] = 0x02;

                    await SendMessage(message1);

                    // Read meter display mode.
                    byte[] message2 = new byte[3];
                    message2[0] = (byte)'G';
                    message2[1] = (byte)'k';
                    message2[2] = 0x02;

                    reply2 = await SendMessage(message2);

                    // Check if in SECURITY mode.
                    if (reply2 != null && reply2[2] == 0x03)
                    {
                        break;
                    }
                }

                if (reply2 != null && reply2[2] == 0x03)
                {
                    // Sc - Set preset.
                    byte[] message3 = new byte[6];
                    message3[0] = (byte)'S';
                    message3[1] = (byte)'c';

                    float f = (float)Math.Floor((double)litres);

                    byte[] litreBytes = BitConverter.GetBytes(f);

                    message3[2] =  litreBytes[0];
                    message3[3] = litreBytes[1];
                    message3[4] = litreBytes[2];
                    message3[5] = litreBytes[3];

                    byte[] reply = await SendMessage(message3);

                    if (reply != null)
                    {
                        // Meter reply is A followed by:
                        //   0 - No error
                        //   1 - Error - requested code/action not understood
                        //   2 - Error - requested action can not be performed
                        if (reply[0] == 'A')
                        {
                            jsonBody = "\"Result\": " + (int)reply[1];
                        }

                        if (reply[1] == 0x00)
                        {
                            // Press MODE to switch back to volume mode.
                            byte[] message4 = new byte[3];

                            message4[0] = (byte)'S';
                            message4[1] = (byte)'u';
                            message4[2] = 0x02;

                            await SendMessage(message4);
                        }

                        // Preset has just been set, therefore set the pumped litres to zero
                        ParentPage.Pumping.DeliveredLitres = 0;
                        //ParentPage.RealtimeLitres.Text = "0";
                    }
                }
            }
            catch (Exception)
            {
                Debug.WriteLine("Failed to Set Preset");
            }

            return CreateCommandResponse("Sp", jsonBody); 
        }

        private static void AddCharacterToBuffer(IList<byte> buffer, byte b)
        {
            // If this is an ESCAPE or DELIMITER character it needs to be escaped
            if (b == ESCAPE_CHAR || b == DELIMITER)
            {
                buffer.Add(ESCAPE_CHAR);

                b ^= 0x20;
            }

            buffer.Add(b);
        }

        private static IList<byte> EncodeMessage(byte[] message)
        {
            IList<byte> txBuffer = new List<byte>();

            // Start message with delimiter
            txBuffer.Add(DELIMITER);

            // Destination address
            txBuffer.Add(DESTINATION);

            // Source address
            txBuffer.Add(SOURCE);

            // Checksum is 0x100 -> (DESTINATION + SOUCRE + message)
            int checksum = DESTINATION + SOURCE;

            // Process each byte in the message
            foreach (byte b in message)
            {
                // Add to the checksum
                checksum += b;

                // These characters must be 'escaped'
                AddCharacterToBuffer(txBuffer, b);
            }

            // Add the checksum
            AddCharacterToBuffer(txBuffer, (byte)(0x00 - checksum));

            // Close message with delimiter
            txBuffer.Add(DELIMITER);

            return txBuffer;
        }

        private static IList<byte> DecodeMessage(byte[] message)
        {
            byte[] reply = null;

            int numberOfBytes = message.Length;
            int messageLength = numberOfBytes - 5;

            if (messageLength > 0)
            {
                reply = new byte[messageLength];

                for (int i = 0, j = 3; i < messageLength; i++, j++)
                {
                    if (message[j] == ESCAPE_CHAR)
                    {
                        reply[i] = (byte)(message[++j] ^ 0x20);
                    }
                    else
                    {
                        reply[i] = message[j];
                    }
                }
            }

            return reply;
        }

        private static async Task<byte[]> SendMessage(byte[] message)
        {
            byte[] encodedMessage = EncodeMessage(message).ToArray();

            using (var w = new DataWriter(SerialPort.OutputStream))
            {
                // Send message to the meter
                w.WriteBytes(encodedMessage);

                await w.StoreAsync();

                w.DetachStream();
            }

            byte[] readBuffer = null;

            try
            {
                readBuffer = await ReadAsync(ReadCancellationTokenSource.Token);
            }
            catch (Exception e)
            {
                Debug.WriteLine("Failed to read response");
                Debug.WriteLine(e.Message);

                throw;
            }

            return DecodeMessage(readBuffer).ToArray();
        }

        private static async Task<byte[]> ReadAsync(CancellationToken cancellationToken)
        {
            uint readBufferLength = 1024;

            // If task cancellation was requested, comply
            cancellationToken.ThrowIfCancellationRequested();

            using (var reader = new DataReader(SerialPort.InputStream))
            {
                using (var cts = new CancellationTokenSource(5000))
                {
                    // Set InputStreamOptions to complete the asynchronous read operation 
                    // when one or more bytes is available
                    reader.InputStreamOptions = InputStreamOptions.Partial;

                    uint bytesRead = 0;

                    try
                    {
                        bytesRead = await reader.LoadAsync(readBufferLength).AsTask(cts.Token);

                        await ParentPage.Emr3ConnectionConnected(true);

                        if (bytesRead > 0)
                        {
                            byte[] buffer = new byte[bytesRead];

                            reader.ReadBytes(buffer);

                            reader.DetachStream();

                            return buffer;
                        }
                    }
                    catch (TaskCanceledException e)
                    {
                        Debug.WriteLine("Task was cancelled after timeout");
                        Debug.WriteLine(e.Message);

                        await ParentPage.Emr3ConnectionConnected(false);

                        throw;
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine("Failed LoadAsync");
                        Debug.WriteLine(e.Message);

                        throw;
                    }
                    finally
                    {
                        reader.DetachStream();
                    }
                }

                return new byte[0];
            }
        }
    }
}
