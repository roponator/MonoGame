using System;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Windows.System.Threading;

namespace SaladFuzzTester
{
    public enum LogChannel
    {
        Main,
        Pings,
        Game,
    }

    // Mono doesn't seem to support blocking (sync) socket operations to we simulate them.
    public class FuzzTesterHelpers
    {
        #region PublicInterface

        // Must be called only once at start from the UI thread
        public static void StartPingingFromUIThread()
        {
            int threadId = Environment.CurrentManagedThreadId;

            // must run on UI thread
            Windows.Foundation.IAsyncAction action = Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                    Windows.UI.Core.CoreDispatcherPriority.Normal,
                async () =>
                {
                    if (threadId != Environment.CurrentManagedThreadId)
                    {
                        string err = "ERROR: you did not start the Fuzz Tester UI thread ping task from the UI thread, this is required or you can deadlock";
                        logToFileBlocking(err, LogChannel.Main);
                        throw new Exception(err);
                    }

                    while (true)
                    {
                        sendMessageToFuzzTesterAsync(FuzzTesterEvent.PingFromUiThread);

                        await Task.Delay(getPingSendIntervalMillis()); // this thread is not critical to execute that often since it just checks timestamps.
                    }

                });
        }

        // Call this every game frame from the game thread, it triggers a ping to fuzz tester every once in a while
        public static void TryDoingSinglePingFromGameThread(GameTime gameTime)
        {
            _gameThreadPingTimer += gameTime.ElapsedGameTime.Milliseconds;
            if (_gameThreadPingTimer > getPingSendIntervalMillis())
            {
                _gameThreadPingTimer = 0.0f;
                sendMessageToFuzzTesterAsync(FuzzTesterEvent.PingFromGameThread);
            }
        }

        #endregion

        #region Implementation

        static float _gameThreadPingTimer = 0.0f;

        // Event message which can be sent to fuzz tester
        // NO ENUM VALUES MUST BE ZERO BECAUSE OF WINDOWS OS PORT SCANNING!
        enum FuzzTesterEvent
        {
            // [0,255] RANGE ONLY SUPPORTED NOW BECAUSE 1 BYTE IS SENT!
            PingFromUiThread = 1, // MUST NOT BE 0 BECAUSE OF WINDOWS OS PORT SCANNING ACTION!
            PingFromGameThread = 2  // MUST NOT BE 0 BECAUSE OF WINDOWS OS PORT SCANNING ACTION!

        }

        public static int gamePos = -1;

        class UserData
        {
            public byte[] msg;
            public string timestamp;
        }

        static int getPingSendIntervalMillis()
        {
            return 3000; // MUST BE MUCH LARGER THAN FUZZ TESTER PING TASK SLEEP AMOUNT FOR THE PING RECIEVER THREAD!!!
        }

        static void sendMessageToFuzzTesterAsync(FuzzTesterEvent message)
        {
            // Data buffer for incoming data.  
            byte[] bytes = new byte[1];

            try
            {

                    logToFileBlocking("SendMessageToFuzzTesterAsync 1: msg: " + message.ToString(), LogChannel.Pings);
 

                int port = 43151;
                IPEndPoint remoteEP = new IPEndPoint(IPAddress.Parse("127.0.0.1"), port);
                Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                SocketAsyncEventArgs eventArgs = new SocketAsyncEventArgs();
                eventArgs.RemoteEndPoint = remoteEP;
                eventArgs.Completed += new EventHandler<SocketAsyncEventArgs>(onConnectedAsync);
                UserData ud = new UserData();
                ud.msg = BitConverter.GetBytes((byte)message);
                ud.timestamp = DateTime.Now.ToString("HH:mm:ss.ff");
                eventArgs.UserToken = ud;

                    logToFileBlocking("SendMessageToFuzzTesterAsync 2: msg: " + message.ToString(), LogChannel.Pings);
               

                // returns false if executed sync OR returns true if async (in which case we must wait for callback).
                if (!socket.ConnectAsync(eventArgs))
                {
                 
                        logToFileBlocking("SendMessageToFuzzTesterAsync 3: msg: " + message.ToString(), LogChannel.Pings);
               

                    onConnectedAsync(socket, eventArgs);
                }

          
                    logToFileBlocking("SendMessageToFuzzTesterAsync 4: msg: " + message.ToString(), LogChannel.Pings);
                

            }
            catch (Exception e)
            {
                showErrorDialog("Error: exception trying to send data to Fuzz tester: " + e.ToString());
            }
        }

        static void onConnectedAsync(object sender, SocketAsyncEventArgs e)
        {
            // needed for special cases when windows scans input ports
            if (e.SocketError == SocketError.ConnectionReset)
            {
         
                    logToFileBlocking("WARNING: onConnectedAsync: RESET SCANNING PORT FIX", LogChannel.Pings); // TODO REMOVE THIS LOG
                
                return;
            }

            UserData ud = (UserData)e.UserToken;
            byte[] dataToSend = (byte[])ud.msg;
          
                logToFileBlocking("onConnectedAsync 1, msg: " + dataToSend[0] + ", from: " + ud.timestamp, LogChannel.Pings);
            

            bool op1 = e.LastOperation != SocketAsyncOperation.Connect;
        
                logToFileBlocking("onConnectedAsync 1.1, msg: " + dataToSend[0] + ", from: " + ud.timestamp, LogChannel.Pings);
            

            bool op2 = e.SocketError != SocketError.Success;
       
                logToFileBlocking("onConnectedAsync 1.2, msg: " + dataToSend[0] + ", from: " + ud.timestamp, LogChannel.Pings);
         

            if (op1 || op2)
            {
            
                    logToFileBlocking("onConnectedAsync 1.3, msg: " + dataToSend[0] + ", from: " + ud.timestamp, LogChannel.Pings);
           

                showErrorDialog("Failed to connect to Fuzz Tester, onConnectedAsync. Last operation: " + e.LastOperation.ToString() + ", Socket status: " + e.SocketError.ToString() + ", data: " + dataToSend[0]);
            }

                logToFileBlocking("onConnectedAsync 1.4, msg: " + dataToSend[0] + ", from: " + ud.timestamp, LogChannel.Pings);
    

            SocketAsyncEventArgs eventArgs = new SocketAsyncEventArgs();
            eventArgs.Completed += new EventHandler<SocketAsyncEventArgs>(onDataSentAsync);
            eventArgs.SetBuffer(dataToSend, 0, dataToSend.Length);
          
                logToFileBlocking("onConnectedAsync 1.5, msg: " + dataToSend[0] + ", from: " + ud.timestamp, LogChannel.Pings);
        

            eventArgs.UserToken = e.UserToken;

            Socket newSocket = (Socket)sender;
          
                logToFileBlocking("onConnectedAsync 2 , msg: " + dataToSend[0] + ", from: " + ud.timestamp, LogChannel.Pings);
          

            if (!newSocket.SendAsync(eventArgs))
            {
                onDataSentAsync(newSocket, eventArgs);
            }
        }

        // required callback because socket is async so we know when its done.
        static async void onDataSentAsync(object sender, SocketAsyncEventArgs e)
        {
            // needed for special cases when windows scans input ports
            if (e.SocketError == SocketError.ConnectionReset)
            {
             
                    logToFileBlocking("WARNING: onDataSentAsync: RESET SCANNING PORT FIX", LogChannel.Pings); // TODO REMOVE THIS LOG
              
                return;
            }

            UserData ud = (UserData)e.UserToken;

       
                logToFileBlocking("onDataSentAsync 1 , msg: " + ud.msg[0] + ", at: " + ud.timestamp, LogChannel.Pings);
            

            if (e.LastOperation != SocketAsyncOperation.Send || e.SocketError != SocketError.Success)
            {
                showErrorDialog("Failed to send data to Fuzz Tester. Last operation: " + e.LastOperation.ToString() + ", Socket status: " + e.SocketError.ToString());
            }

         
                logToFileBlocking("onDataSentAsync 2 , msg: " + ud.msg[0] + ", at: " + ud.timestamp, LogChannel.Pings);
          

            try
            {

                Socket newSocket = (Socket)sender;
                if (newSocket.Connected == false)
                {
                    //newSocket.Shutdown(SocketShutdown.Both);
                    newSocket.Dispose();
                    logToFileBlocking("ERROR: socket failed to connect & send", LogChannel.Main);
                    throw new Exception("ERROR: socket failed to connect & send");
                }
            
                    logToFileBlocking("onDataSentAsync 2 at: " + ud.timestamp, LogChannel.Pings);
              

                await Task.Delay(1000); // for sanity before we kill it just in case

                newSocket.Dispose();

            }
            catch (Exception ex)
            {
                string s = ex.ToString();
                logToFileBlocking(s, LogChannel.Main);
            }

        }

        // stores ping tasks that were already started so that we cannot start multiple ping tasks for the same FuzzTesterEvent value.
        static HashSet<FuzzTesterEvent> _startedPingTasks = new HashSet<FuzzTesterEvent>();

        #endregion

        #region MiscHelpers

        // This does not work if the UI thread is blocked, no dialog will be shown! It will be logged to file, 
        // but exception may also not be thrown!
        static void showErrorDialog(string message)
        {
            string logLine = DateTime.Now.ToString("HH:mm:ss.ff") + ": " + message;

            logToFileBlocking("ERROR Dialog: " + logLine, LogChannel.Pings);

            // must run on UI thread
            Windows.Foundation.IAsyncAction action = Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                 Windows.UI.Core.CoreDispatcherPriority.High,
              () =>
              {
                  var dialog = new Windows.UI.Popups.MessageDialog(logLine);
                  var a = dialog.ShowAsync();
              });

            throw new Exception("ERROR Dialog: " + logLine);
        }

        static object loggerWriteLock = new object(); // to prevent 2 threads writing/reading at same time

        // WARNING: THIS FUNCTION CAN CAUSE LOCKUPS WHEN WRITING TO FILE WHICH COULD BE MISTAKEN FOR BUGS IN THE GAME,
        // SO THE CURRENT IMPLEMENTATION IS NOT IDEAL FOR MASSIVE LOGGING AS IT CAN LOCK FOR SOME STRANGE REASON
        // WHEN WAITING TO OPEN THE FILE !!!
        // Writes to C:\Users\<username>\AppData\Local\Packages\<app_package_name>\LocalState\FuzzTester.txt
        public static void logToFileBlocking(string logMessage, LogChannel channel)
        {
            if(channel == LogChannel.Pings)
            {
                return;
            }
          
            // TODO: COULD TRY WAITING FOR TASK? BUT MAY CRASH BEFORE WRITE...
           int threadId = Environment.CurrentManagedThreadId;
              String timeStamp1 = "Timestamp logToFileBlocking call: "+DateTime.Now.ToString("HH:mm:ss.ff.");
              WorkItemHandler workItemHandler = action =>
              {
                  String timeStamp2 = "Timestamp logToFileBlocking call: " + DateTime.Now.ToString("HH:mm:ss.ff.");

                  lock (loggerWriteLock)
                  {
                      String timeStamp3 = "Timestamp logToFileBlocking call: " + DateTime.Now.ToString("HH:mm:ss.ff.");

                      // Create sample file; replace if exists.
                      Windows.Storage.StorageFolder storageFolder =
                          Windows.Storage.ApplicationData.Current.LocalFolder;

                      Windows.Foundation.IAsyncOperation<Windows.Storage.StorageFile> sampleFile =
                           storageFolder.CreateFileAsync("FuzzTester.txt", Windows.Storage.CreationCollisionOption.OpenIfExists);

                      String timeStamp4 = "Timestamp logToFileBlocking call: " + DateTime.Now.ToString("HH:mm:ss.ff.");

                      Task<Windows.Storage.StorageFile> ft = sampleFile.AsTask();

                      ft.Wait();
                      Windows.Storage.StorageFile f = ft.Result;

                      String timeStamp = DateTime.Now.ToString("HH:mm:ss.ff");
                      string ts = ". " + timeStamp1 + " . " + timeStamp2 + " . " + timeStamp3 + " . " + timeStamp4 + " . ";
                      Windows.Foundation.IAsyncAction aa = Windows.Storage.FileIO.AppendTextAsync(f, timeStamp + ": " + " thread: " + threadId + " : "+ channel.ToString()+": "+ "gamePos: "+ gamePos+" : "+ logMessage + " @@"+ ts+ "\r\n"); ;
                      while (aa.Status != Windows.Foundation.AsyncStatus.Completed) ;
                  }

              };

              Windows.Foundation.IAsyncAction a = ThreadPool.RunAsync(workItemHandler, WorkItemPriority.High, WorkItemOptions.None);
              while (a.Status != Windows.Foundation.AsyncStatus.Completed) ;
              
/*
            // removed waits
            int threadId = Environment.CurrentManagedThreadId;
            String timeStamp1 = "Timestamp logToFileBlocking call: " + DateTime.Now.ToString("HH:mm:ss.ff.");
            Action workItemHandler = () =>
            {
                String timeStamp2 = "Timestamp logToFileBlocking call: " + DateTime.Now.ToString("HH:mm:ss.ff.");

                lock (loggerWriteLock)
                {
                    String timeStamp3 = "Timestamp logToFileBlocking call: " + DateTime.Now.ToString("HH:mm:ss.ff.");

                    // Create sample file; replace if exists.
                    Windows.Storage.StorageFolder storageFolder =
                        Windows.Storage.ApplicationData.Current.LocalFolder;

                    Windows.Foundation.IAsyncOperation<Windows.Storage.StorageFile> sampleFile =
                         storageFolder.CreateFileAsync("FuzzTester.txt", Windows.Storage.CreationCollisionOption.OpenIfExists);

                    String timeStamp4 = "Timestamp logToFileBlocking call: " + DateTime.Now.ToString("HH:mm:ss.ff.");

                    Task<Windows.Storage.StorageFile> ft = sampleFile.AsTask();

                    ft.Wait();
                    Windows.Storage.StorageFile f = ft.Result;

                    String timeStamp = DateTime.Now.ToString("HH:mm:ss.ff");
                    string ts = ". " + timeStamp1 + " . " + timeStamp2 + " . " + timeStamp3 + " . " + timeStamp4 + " . ";
                    Windows.Foundation.IAsyncAction aa = Windows.Storage.FileIO.AppendTextAsync(f, timeStamp + ": " + " thread: " + threadId + " : " + channel.ToString() + ": " + "gamePos: " + gamePos + " : " + logMessage + " @@" + ts + "\r\n"); ;
                    while (aa.Status != Windows.Foundation.AsyncStatus.Completed) ;
                }

            };

            Task task = Task.Factory.StartNew(() => workItemHandler());
       */
        }

        #endregion
    }
}
