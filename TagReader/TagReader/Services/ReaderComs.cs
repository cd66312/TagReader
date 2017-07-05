using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.SerialCommunication;
using Windows.Storage.Streams;
using Windows.UI.Core;

namespace TagReader.Services
{
    static public class ReaderComs
    {

        #region Private Vars

        static private SerialDevice serialPort = null;
        static private DataWriter dataWriteObject = null;
        static private DataReader dataReaderObject = null;
        static private ObservableCollection<DeviceInformation> listOfDevices;
        static private CancellationTokenSource ReadCancellationTokenSource;
        static private string partialSerialLineStart = "";

        #endregion

        #region Public Access

        /// <summary>
        /// Returns bool depicting the connection state of the serial line
        /// </summary>
        static public bool Connected { get; private set; } = false;

        /// <summary>
        /// Connect to our reader
        /// </summary>
        /// <returns></returns>
        static public async Task<bool> Initialize()
        {
            return await autoConnect();
        }

        /// <summary>
        /// Terminates connected to reader
        /// </summary>
        static public void Disconnect() { if (Connected) { closeDevice(); } }

        /// <summary>
        /// Sends cmds to the reader
        /// </summary>
        /// <param name="_Sent">command</param>
        static public void Write(string _Sent)
        {
            if (Connected)
            {
                //// Create the DataWriter object and attach to OutputStream
                dataWriteObject = new DataWriter(serialPort.OutputStream);

                writeAsync(_Sent);

            }

        }

        #endregion


        #region Private Methods

        /// <summary>
        /// Connected to serialdevice using non configurable settings
        /// </summary>
        /// <returns></returns>
        static private async Task<bool> autoConnect()
        {

            try
            {
                string aqs = SerialDevice.GetDeviceSelector();
                var dis = await DeviceInformation.FindAllAsync(aqs);

                DeviceInformation entry = (DeviceInformation)dis[3];

                serialPort = await SerialDevice.FromIdAsync(entry.Id);


                // Configure serial settings
                serialPort.WriteTimeout = TimeSpan.FromMilliseconds(1000);
                serialPort.ReadTimeout = TimeSpan.FromMilliseconds(1000);
                serialPort.BaudRate = 115200;
                serialPort.Parity = SerialParity.None;
                serialPort.StopBits = SerialStopBitCount.One;
                serialPort.DataBits = 8;
                serialPort.Handshake = SerialHandshake.None;


                // Create cancellation token object to close I/O operations when closing the device
                ReadCancellationTokenSource = new CancellationTokenSource();

                // start serial listener
                listen();
                Connected = true;

                generateLineReadEvent("ReaderConnected");

                return true;
            }
            catch (Exception ex)
            {

                generateLineReadEvent("ReaderDisConnected");
                // clean exit cause we messed up
                closeDevice();
                return false;
            }
        }

        /// <summary>
        /// WriteAsync: Task that asynchronously writes data to the OutputStream 
        /// </summary>
        /// <returns></returns>
        static private async Task writeAsync(string _SendText)
        {
            Task<UInt32> storeAsyncTask;




            if (_SendText.Length != 0)
            {
                // Load the text from the sendText input text box to the dataWriter object
                dataWriteObject.WriteString(_SendText + Environment.NewLine);

                // Launch an async task to complete the write operation
                storeAsyncTask = dataWriteObject.StoreAsync().AsTask();

                UInt32 bytesWritten = await storeAsyncTask;
                if (bytesWritten > 0)
                {
                    // success, maybe return positive bool here? TODO
                }
            }

            // Cleanup once complete
            if (dataWriteObject != null)
            {
                dataWriteObject.DetachStream();
                dataWriteObject = null;
            }
        }


        /// <summary>
        /// - Create a DataReader object
        /// - Create an async task to read from the SerialDevice InputStream
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        static private async void listen()
        {
            try
            {
                if (serialPort != null)
                {
                    dataReaderObject = new DataReader(serialPort.InputStream);

                    // keep reading the serial input
                    while (true)
                    {
                        await readAsync(ReadCancellationTokenSource.Token);
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex.GetType().Name == "TaskCanceledException")
                {
                    closeDevice();
                }
                else
                {
                }
            }
            finally
            {
                // Cleanup once complete
                if (dataReaderObject != null)
                {
                    dataReaderObject.DetachStream();
                    dataReaderObject = null;
                }
            }
        }


        /// <summary>
        /// ReadAsync: Task that waits on data and reads asynchronously from the serial device InputStream
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        static private async Task readAsync(CancellationToken cancellationToken)
        {
            try
            {
                Task<UInt32> loadAsyncTask;

                uint ReadBufferLength = 128;

                // If task cancellation was requested, comply
                cancellationToken.ThrowIfCancellationRequested();

                // Set InputStreamOptions to complete the asynchronous read operation when one or more bytes is available
                dataReaderObject.InputStreamOptions = InputStreamOptions.Partial;

                // Create a task object to wait for data on the serialPort.InputStream
                loadAsyncTask = dataReaderObject.LoadAsync(ReadBufferLength).AsTask(cancellationToken);

                // Launch the task and wait
                UInt32 bytesRead = await loadAsyncTask;
                if (bytesRead > 0)
                {
                    //string test = dataReaderObject.ReadString(bytesRead);
                    byte[] _ByteCollection = new byte[bytesRead];
                    dataReaderObject.ReadBytes(_ByteCollection);

                    StringBuilder _BuiltString = new StringBuilder();

                    foreach (byte bytes in _ByteCollection)
                    {
                        if (bytes >= 0 && bytes <= 127)
                        {
                            _BuiltString.Append((Char)bytes);
                        }
                    }

                    // TODO tell someone to clean up their firmware a bit!
                    // there are times at which someone has used \n\r instead of \r\n which is not being recognized as a linefeed
                    _BuiltString.Replace("\n\r", Environment.NewLine);

                    string[] _Strings = _BuiltString.ToString().Split(new string[] { Environment.NewLine }, StringSplitOptions.None);

                    // add any partial line start
                    _Strings[0] = partialSerialLineStart + _Strings[0];
                    partialSerialLineStart = "";

                    // filter for incomplete line endings
                    if (!_Strings.Last().Contains(">") && _Strings.Last() != "")
                    {
                        // record line to be removed, then remove it
                        partialSerialLineStart = _Strings[_Strings.Count() - 1];
                        _Strings = _Strings.Take(_Strings.Count() - 1).ToArray();
                    }

                    // send out any information we might have
                    foreach (string s in _Strings)
                    {
                        if (s != "")
                        {
                            // need to remove those random line feeds that seem to occur
                            string newS = s.Substring(s.IndexOf('<'));
                            // ready to send!
                            generateLineReadEvent(newS);
                        }
                    }


                }
            }
            catch (Exception e)
            {

            }

        }

        /// <summary>
        /// CancelReadTask:
        /// - Uses the ReadCancellationTokenSource to cancel read operations
        /// </summary>
        static private void cancelReadTask()
        {
            if (ReadCancellationTokenSource != null)
            {
                if (!ReadCancellationTokenSource.IsCancellationRequested)
                {
                    ReadCancellationTokenSource.Cancel();
                }
            }
        }

        /// <summary>
        /// CloseDevice:
        /// - Disposes SerialDevice object
        /// - Clears the enumerated device Id list
        /// </summary>
        static private void closeDevice()
        {
            if (serialPort != null)
            {
                cancelReadTask();
                serialPort.Dispose();
            }
            serialPort = null;

            listOfDevices.Clear();

            generateLineReadEvent("ReaderDisConnected");
            Connected = false;
        }
        #endregion


        #region consumable events

        static private async void generateLineReadEvent(string line)
        {

            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                LineReadEvent?.Invoke(typeof(string), line);
            });
        }

        static public event EventHandler<string> LineReadEvent;

        #endregion
    }
}
