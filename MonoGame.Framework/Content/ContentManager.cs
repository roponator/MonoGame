// MonoGame - Copyright (C) The MonoGame Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

//#define ROPO_PRINT
#define ROPO_ADD_TIME // for callback version
//#define ROPO_ADD_TIME_SINGLE_THREADED // for non callback version
//#define ROPO_TASK_TIME_WITH_THREAD_ID
//#define ROPO_TASK_TIME_PLOT

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Linq.Expressions;
using System.Linq;
using Microsoft.Xna.Framework.Utilities;
using Microsoft.Xna.Framework.Graphics;

#if !WINRT
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Media;
using System.Linq;
#endif

namespace Microsoft.Xna.Framework.Content
{
    public partial class ContentManager : IDisposable
    {
        const byte ContentCompressedLzx = 0x80;
        const byte ContentCompressedLz4 = 0x40;

        private string _rootDirectory = string.Empty;
        private IServiceProvider serviceProvider;
        private IGraphicsDeviceService graphicsDeviceService;
        private Dictionary<string, object> loadedAssets = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        private List<IDisposable> disposableAssets = new List<IDisposable>();
        private bool disposed;
        private byte[] scratchBuffer;

        private static object ContentManagerLock = new object();
        private static List<WeakReference> ContentManagers = new List<WeakReference>();

        private static readonly List<char> targetPlatformIdentifiers = new List<char>()
        {
            'w', // Windows (DirectX)
            'x', // Xbox360
            'i', // iOS
            'a', // Android
            'd', // DesktopGL
            'X', // MacOSX
            'W', // WindowsStoreApp
            'n', // NativeClient
            'M', // WindowsPhone8
            'r', // RaspberryPi
            'P', // PlayStation4
            'v', // PSVita
            'O', // XboxOne

            // NOTE: There are additional idenfiers for consoles that 
            // are not defined in this repository.  Be sure to ask the
            // console port maintainers to ensure no collisions occur.

            
            // Legacy identifiers... these could be reused in the
            // future if we feel enough time has passed.

            'p', // PlayStationMobile
            'g', // Windows (OpenGL)
            'l', // Linux
        };


        static partial void PlatformStaticInit();

        static ContentManager()
        {
            // Allow any per-platform static initialization to occur.
            PlatformStaticInit();
        }

        private static void AddContentManager(ContentManager contentManager)
        {
            lock (ContentManagerLock)
            {
                // Check if the list contains this content manager already. Also take
                // the opportunity to prune the list of any finalized content managers.
                bool contains = false;
                for (int i = ContentManagers.Count - 1; i >= 0; --i)
                {
                    var contentRef = ContentManagers[i];
                    if (ReferenceEquals(contentRef.Target, contentManager))
                        contains = true;
                    if (!contentRef.IsAlive)
                        ContentManagers.RemoveAt(i);
                }
                if (!contains)
                    ContentManagers.Add(new WeakReference(contentManager));
            }
        }

        private static void RemoveContentManager(ContentManager contentManager)
        {
            lock (ContentManagerLock)
            {
                // Check if the list contains this content manager and remove it. Also
                // take the opportunity to prune the list of any finalized content managers.
                for (int i = ContentManagers.Count - 1; i >= 0; --i)
                {
                    var contentRef = ContentManagers[i];
                    if (!contentRef.IsAlive || ReferenceEquals(contentRef.Target, contentManager))
                        ContentManagers.RemoveAt(i);
                }
            }
        }

        internal static void ReloadGraphicsContent()
        {
            lock (ContentManagerLock)
            {
                // Reload the graphic assets of each content manager. Also take the
                // opportunity to prune the list of any finalized content managers.
                for (int i = ContentManagers.Count - 1; i >= 0; --i)
                {
                    var contentRef = ContentManagers[i];
                    if (contentRef.IsAlive)
                    {
                        var contentManager = (ContentManager)contentRef.Target;
                        if (contentManager != null)
                            contentManager.ReloadGraphicsAssets();
                    }
                    else
                    {
                        ContentManagers.RemoveAt(i);
                    }
                }
            }
        }

        // Use C# destructor syntax for finalization code.
        // This destructor will run only if the Dispose method
        // does not get called.
        // It gives your base class the opportunity to finalize.
        // Do not provide destructors in types derived from this class.
        ~ContentManager()
        {
            // Do not re-create Dispose clean-up code here.
            // Calling Dispose(false) is optimal in terms of
            // readability and maintainability.
            Dispose(false);
        }

        public ContentManager(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException("serviceProvider");
            }
            this.serviceProvider = serviceProvider;
            AddContentManager(this);
        }

        public ContentManager(IServiceProvider serviceProvider, string rootDirectory)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException("serviceProvider");
            }
            if (rootDirectory == null)
            {
                throw new ArgumentNullException("rootDirectory");
            }
            this.RootDirectory = rootDirectory;
            this.serviceProvider = serviceProvider;
            AddContentManager(this);
        }

        public void Dispose()
        {
            Dispose(true);
            // Tell the garbage collector not to call the finalizer
            // since all the cleanup will already be done.
            GC.SuppressFinalize(this);
            // Once disposed, content manager wont be used again
            RemoveContentManager(this);
        }

        // If disposing is true, it was called explicitly and we should dispose managed objects.
        // If disposing is false, it was called by the finalizer and managed objects should not be disposed.
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    Unload();
                }
                disposed = true;
            }
        }

        public virtual T Load<T>(string assetName)
        {
            if (string.IsNullOrEmpty(assetName))
            {
                throw new ArgumentNullException("assetName");
            }
            if (disposed)
            {
                throw new ObjectDisposedException("ContentManager");
            }

            T result = default(T);

            // On some platforms, name and slash direction matter.
            // We store the asset by a /-seperating key rather than how the
            // path to the file was passed to us to avoid
            // loading "content/asset1.xnb" and "content\\ASSET1.xnb" as if they were two 
            // different files. This matches stock XNA behavior.
            // The dictionary will ignore case differences
            var key = assetName.Replace('\\', '/');

            // Check for a previously loaded asset first
            object asset = null;
            if (loadedAssets.TryGetValue(key, out asset))
            {
                if (asset is T)
                {
                    return (T)asset;
                }
            }

            // Load the asset.
            result = ReadAsset<T>(assetName, null);

            loadedAssets[key] = result;
            return result;
        }

        static object lockLoadedAssets = new object();

        public virtual void LoadCallback<T>(ContentManager.ResTask task, string assetName, ResCallback onDone)
        {
            if (string.IsNullOrEmpty(assetName))
            {
                throw new ArgumentNullException("assetName");
            }
            if (disposed)
            {
                throw new ObjectDisposedException("ContentManager");
            }

            // On some platforms, name and slash direction matter.
            // We store the asset by a /-seperating key rather than how the
            // path to the file was passed to us to avoid
            // loading "content/asset1.xnb" and "content\\ASSET1.xnb" as if they were two 
            // different files. This matches stock XNA behavior.
            // The dictionary will ignore case differences
            var key = assetName.Replace('\\', '/');

            // Check for a previously loaded asset first
            object asset = null;
            bool res = false;

            lock (lockLoadedAssets)
            {
                loadedAssets.TryGetValue(key, out asset); // NOTE: PUT THIS AND THE CALL loadedAssets[key] = result; INTO ONE LARGE LOCK, IN CASE A DIFFERENT SAME NAMED RESOURCES WOULD BE LOADED?
            }

            if (res)
            {
                onDone((T)asset);
            }
            else
            {
                ResCallback intermediaryCallback = (callbackResult) =>
                {

                    // store to manager first, then continue calling users callback
                    lock (lockLoadedAssets)
                    {
                        loadedAssets[key] = callbackResult; // LOCK THIS!
                    }

                    onDone(callbackResult);
                };
                // Load the asset.
                ReadAssetCallback<T>(task, assetName, null, intermediaryCallback);
            }



        }

        protected virtual Stream OpenStream(string assetName)
        {

            //System.Diagnostics.Stopwatch stopwatchReadAsset = new Stopwatch ();

            Stream stream;
            try
            {
                var assetPath = Path.Combine(RootDirectory, assetName) + ".xnb";

                // This is primarily for editor support. 
                // Setting the RootDirectory to an absolute path is useful in editor
                // situations, but TitleContainer can ONLY be passed relative paths.                
#if DESKTOPGL || MONOMAC || WINDOWS
                if (Path.IsPathRooted(assetPath))                
                    stream = File.OpenRead(assetPath);                
                else
#endif
                //  stopwatchReadAsset.Reset ();
                //  stopwatchReadAsset.Start ();

                stream = TitleContainer.OpenStream(assetPath);

                //    stopwatchReadAsset.Stop ();
                //    addTime ("ContentManager_OpenStream_OpenStream: "+ assetName, stopwatchReadAsset.ElapsedMilliseconds);
#if ANDROID
                // Read the asset into memory in one go. This results in a ~50% reduction
                // in load times on Android due to slow Android asset streams.

                //   stopwatchReadAsset.Reset ();
                //   stopwatchReadAsset.Start ();

                MemoryStream memStream = new MemoryStream();
                stream.CopyTo(memStream);
                memStream.Seek(0, SeekOrigin.Begin);
                stream.Close();
                stream = memStream;

                //   stopwatchReadAsset.Stop ();
                //   addTime ("ContentManager_OpenStream_MemoryStream: " + assetName, stopwatchReadAsset.ElapsedMilliseconds);
#endif
            }
            catch (FileNotFoundException fileNotFound)
            {
                throw new ContentLoadException("The content file was not found.", fileNotFound);
            }
#if !WINRT
            catch (DirectoryNotFoundException directoryNotFound)
            {
                throw new ContentLoadException("The directory was not found.", directoryNotFound);
            }
#endif
            catch (Exception exception)
            {
                throw new ContentLoadException("Opening stream error.", exception);
            }
            return stream;


        }

        static Dictionary<String, long> g_times = new Dictionary<string, long>();
        static object g_lockyTimer = new object();
        public static void addTime(string key, long millis)
        {
          //  Game.Instance.Window.log("ropo", "addTime: "+ key +": "+ millis + "\n");

#if ROPO_PRINT
            Android.Util.Log.Info ("ropo_stopwatch", key);
#endif
            lock (g_lockyTimer)
            {
                if (g_times.ContainsKey(key) == false)
                {
                    g_times.Add(key, millis);
                }
                else
                {
                    g_times[key] += millis;
                }
            }
        }

        static List<string> g_plotTimes = new List<string>();
        static object g_lockyPlotTimer = new object();
        const string g_plotTimerToken_StartEntry = "@{a}";
        const string g_plotTimerToken_StartText = "@{b}";
        const string g_plotTimerToken_StartTime = "@{c}";
        const string g_plotTimerToken_EndTime = "@{d}";
        const string g_plotTimerToken_EndEntry = "@{e}";

        const string g_plotTimerToken_XYPair_StartEntry = "X{a}";
        const string g_plotTimerToken_XYPair_StartX = "X{b}";
        const string g_plotTimerToken_XYPair_StartY = "X{c}";
        const string g_plotTimerToken_XYPair_EndEntry = "X{d}";

#if ROPO_TASK_TIME_PLOT
        public static Stopwatch g_stopwatchPlotTimer = new Stopwatch();
#endif

        // entry structures: start_token + threadName + start_text_token + text + start_time_token + startTime + end_time_token + end_time + end_token
        public static void addPlotTime(string threadName, string text, long startTime, long endTime)
        {
            // dont print very short times as it can spam console so output dies
            if((endTime-startTime)<3)
            {
                return; 
            }

          //  Game.Instance.Window.log("ropo", "addPlotTime: "+ threadName+": "+ startTime+", "+endTime+"\n");

            lock (g_lockyPlotTimer)
            {
                //  todo ropo could this be slow?
                g_plotTimes.Add(
                    g_plotTimerToken_StartEntry +
                   threadName +
                   g_plotTimerToken_StartText +
                   text +
                   g_plotTimerToken_StartTime +
                   startTime +
                   g_plotTimerToken_EndTime +
                   endTime +
                  g_plotTimerToken_EndEntry);
            }
        }

        public static void addPlotXYPair(string groupName, long x, long y)
        {
          
            lock (g_lockyPlotTimer)
            {
                //  todo ropo could this be slow?
                g_plotTimes.Add(
                    g_plotTimerToken_XYPair_StartEntry +
                   groupName +
                   g_plotTimerToken_XYPair_StartX +
                   x +
                   g_plotTimerToken_XYPair_StartY +
                   y +
                  g_plotTimerToken_XYPair_EndEntry);
            }
        }

        public static void printPlotTimes()
        {
            const string tag = "ropo_plotTimes";
#if ANDROID
            Game.Instance.Window.log(tag,"PLOT_TIMES---------------\n");
#endif

            // print by time usage order
            int x = 0;
            foreach (var v in g_plotTimes)
            {
#if ANDROID
                Game.Instance.Window.log(tag, v + "\n"); // would be too large to print otherwise
                ++x;

              /*  if(x>150)
                {
                x=0;
                  System.Threading.Thread.Sleep(300);
                }*/

#endif

            }
#if ANDROID
            Game.Instance.Window.log(tag, "PLOT_TIMES END ---------------\n");
#endif


        }


        public static void printLoadingTimes()
        {
            String t = "";

            // print by time usage order
            foreach (KeyValuePair<string, long> entry in g_times.OrderBy(key => key.Value))
            {
                t += entry.Key + ": " + entry.Value + "\n";
            }


#if ANDROID
            Game.Instance.Window.log("ropo_stopwatch", t);
#endif
        }

        //  public delegate void ResCallback<T> (T result);
        public delegate void ResCallback(object result);

        public class ResTask
        {
#if ROPO_TASK_TIME_PLOT
            public string plotTimeTaskName = null;
#endif

        
            ResCallback onExecute = null;
            ResTask next = null; // gets executed immediately after this one, is not appended to queue. Needed for memory optimization

            // long tasks should not be execute on main thread as it can block scheduling work to worker threads
            public bool IsShortTask
            {
                get;
                internal set;
            }

            public ResTask(bool isShortTask)
            {
                this.IsShortTask = isShortTask;
            }

            public ResTask(bool isShortTask, ResCallback onExecute)
            {
                this.IsShortTask = isShortTask;
                this.onExecute = onExecute;
            }

            public void Execute()
            {
                onExecute(null);

                if (next != null)
                {
                    next.Execute();
                }
            }

            // WARNING: THIS MUST BE CALLED BEFORE 'SetNextTask' OR THAT CALL WILL OVERWRITE IT!
            public void SetWorkToExecuteInTask(ResCallback onExecute)
            {
                this.onExecute = onExecute;
            }

            public void SetNextTask(ResTask next)
            {
                if (this.next != null)
                {
                    throw new Exception("next task is already set, you can only set one next task, you most likely wanted to use some other task");
                }

                this.next = next;

            }
        }


        class WorkerTask
        {
            public WorkerTask(System.Threading.Tasks.Task task, System.Threading.CancellationTokenSource token)
            {
                this.task = task;
                this.cancelToken = token;
            }

            public System.Threading.Tasks.Task task = null;
            public System.Threading.CancellationTokenSource cancelToken;
        }

        // Allows searching for short tasks without having to enqueue long tasks at the end of the queue. 
        // The problem with vanilla C# concurrent queues is that long items are pushed to end of queue when main thread searches
        //for fast tasks to execute so we get long tasks at end which causes waiting for other threads.
        class ConcurrentTaskQueue
        {
            private List<ResTask> _items = new List<ResTask>();
            private object _lock = new object();

            public void Enqueue(ResTask t, bool enqueueAtFront = false)
            {
                lock(_lock)
                 {
                    if(enqueueAtFront)
                    {
                        _items.Insert(0, t);
                    }
                    else
                    {
                        _items.Add(t);
                    }            
                 }
            }

            public int Count
            {
                get
                {
                    lock (_lock)
                    {
                        return _items.Count;
                    }
                }
            }

            // Returns null if none. This is where the magic happens as it doesn't push long tasks on end of the queue.
            public bool TryDequeueShortTask(out ResTask outTask)
            {
                lock (_lock)
                {
                    for(int i=0;i<_items.Count;++i)
                    {
                        ResTask t = _items[i];
                        if(t.IsShortTask)
                        {
                            _items.RemoveAt(i);
                            outTask = t;
                            return true;
                        }
                    }

                    // no more items
                    outTask = null;
                    return false;
                }
            }

            // Returns null if none
            public bool TryDequeue(out ResTask outTask)
            {
                lock (_lock)
                {
                    if(_items.Count > 0)
                    {
                        ResTask t = _items[0];
                        _items.RemoveAt(0);
                        outTask = t;
                        return true;
                    }

                    // no more items
                    outTask = null;
                    return false;
                }
            }
        }

        // Using a stack instead of the queue so that the last added tasks gets processed first, otherwise due to the limit of max
        // tasks being executed at same time it will never finish as tasks that are required to be executed in order for some other task
        // to finish will be pushed to end of the queue, so they will never get executed because the queue items won't get processed
        // since the tasks that were added to the end of the queue would need to be processed first for the entire task to finish.
        static ConcurrentTaskQueue m_resourceLoadingTasksMainThread = new ConcurrentTaskQueue();

        // high priority queue gets polled first for any tasks before low priority queue.
        static ConcurrentTaskQueue m_resourceLoadingTasksLowPriorityWorkerThread = new ConcurrentTaskQueue();
        static ConcurrentTaskQueue m_resourceLoadingTasksHighPriorityWorkerThread = new ConcurrentTaskQueue();

        static List<WorkerTask> m_workerTasks = new List<WorkerTask>();
        public static System.Threading.AutoResetEvent m_tasksMainThreadWait = new System.Threading.AutoResetEvent(true);
        public static System.Threading.ManualResetEvent m_workerThreadEvent = new System.Threading.ManualResetEvent(true);
        static System.Threading.SynchronizationContext m_mainThreadSyncContext = null;

        public static void EnqueueResourceLoadingTaskOnMainThread(ResTask task, bool enqueueAtFront = false)
        {
         //   Game.Instance.Window.log("ropo_enq", "EnqueueResourceLoadingTaskOnMainThread");
          DID FORCE ENQUEUE DOESNT WORK WELL
            m_resourceLoadingTasksMainThread.Enqueue(task, enqueueAtFront);
            m_tasksMainThreadWait.Set();
        }

        public static void EnqueueResourceLoadingTaskOnWorkerThread(ResTask task, bool enqueueAtFront = false)
        {
            // Game.Instance.Window.log("ropo_stopwatch", "EnqueueResourceLoadingTaskOnWorkerThread");
            m_resourceLoadingTasksLowPriorityWorkerThread.Enqueue(task, enqueueAtFront);
            m_workerThreadEvent.Set();
        }

        public static void EnqueueResourceLoadingTaskOnHighPriorityWorkerThread(ResTask task, bool enqueueAtFront)
        {
            // Game.Instance.Window.log("ropo_stopwatch", "EnqueueResourceLoadingTaskOnWorkerThread");
            m_resourceLoadingTasksHighPriorityWorkerThread.Enqueue(task, enqueueAtFront);
            m_workerThreadEvent.Set();
        }

        public static bool HasAnyWorkerTasksLeft()
        {
            return m_resourceLoadingTasksHighPriorityWorkerThread.Count>0 || m_resourceLoadingTasksLowPriorityWorkerThread.Count>0;
        }

        public static bool HasAnyMainThreadTasksLeft()
        {
            return m_resourceLoadingTasksMainThread.Count > 0;
        }

        public static int GetNumTotalRemainingTaks()
        {
            return m_resourceLoadingTasksHighPriorityWorkerThread.Count +
                m_resourceLoadingTasksLowPriorityWorkerThread.Count +
                m_resourceLoadingTasksMainThread.Count;
        }

        public static int GetNumWorkerThreadRemainingTaks()
        {
            return m_resourceLoadingTasksHighPriorityWorkerThread.Count +
                m_resourceLoadingTasksLowPriorityWorkerThread.Count;
        }

        public static int GetNumMainThreadRemainingTasks()
        {
            return m_resourceLoadingTasksMainThread.Count;
        }

        // return num tasks it processed, first tries to read from main task queue, if none it tried from multithreaded task queue.
        // Does not run long running tasks marker by 'task.CanExecuteOnMainThread'.
        public static int TryRunningMainThreadEnqueuedOrShortWorkerTask()
        {
            /* int numProcessedTasks = 0;

             ResTask task = null;
             System.Collections.Concurrent.ConcurrentQueue<ResTask> queueTaskWasRetrievedFrom = null;

             if (m_resourceLoadingTasksMainThread.TryDequeue(out task))
             {
                 queueTaskWasRetrievedFrom = m_resourceLoadingTasksMainThread;
             }

             // if no main thread tasks were loaded pick one from high priority worker thread
             if (queueTaskWasRetrievedFrom == null || task == null)
             {
                 if (m_resourceLoadingTasksHighPriorityWorkerThread.TryDequeue(out task))
                 {
                     queueTaskWasRetrievedFrom = m_resourceLoadingTasksHighPriorityWorkerThread;                
                 }
             }

             // if no main thread tasks were loaded pick one from low priority worker thread
             if (queueTaskWasRetrievedFrom == null || task == null)
             {
                 if (m_resourceLoadingTasksLowPriorityWorkerThread.TryDequeue(out task))
                 {
                     queueTaskWasRetrievedFrom = m_resourceLoadingTasksLowPriorityWorkerThread;                              
                 }
             }



             // if task was dequeued and if it can be run by main thread: run it, otherwise put it back to queue it came from.
              if (queueTaskWasRetrievedFrom != null && task != null &&
                   (task.IsShortTask || queueTaskWasRetrievedFrom == m_resourceLoadingTasksMainThread) // always execute tasks dequeued from main thread
                   )
               {
 #if ROPO_TASK_TIME_PLOT
                 long plotStartTimeInner = g_stopwatchPlotTimer.ElapsedMilliseconds;
 #endif

                 task.Execute();

 #if ROPO_TASK_TIME_PLOT
                 addPlotTime("Main Task", task.plotTimeTaskName == null ? "Main Wait Task" : task.plotTimeTaskName, plotStartTimeInner, g_stopwatchPlotTimer.ElapsedMilliseconds);
 # endif

                 ++numProcessedTasks;
               }
               else if(queueTaskWasRetrievedFrom != null && task != null) // shouldn't happen but for sanity
               {
                   queueTaskWasRetrievedFrom.Enqueue(task); // put it back if we can't execute it. cannot use peak as some other thread could take it meanwhile
               }
   #if DEBUG
               else
               {
                 throw new Exception("Error: shouldn't happen, bug in above algorithm");
               }
   #endif

             // nudge workers
             m_workerThreadEvent.Set();
             return numProcessedTasks;
             
             */


             int numProcessedTasks = 0;
           ResTask task = null;

            m_resourceLoadingTasksMainThread.TryDequeue(out task);
    
           // if no main thread tasks were loaded pick one from high priority worker thread
           if ( task == null)
           {
                m_resourceLoadingTasksHighPriorityWorkerThread.TryDequeueShortTask(out task);
           }

           // if no main thread tasks were loaded pick one from low priority worker thread
           if (task == null)
           {
                m_resourceLoadingTasksLowPriorityWorkerThread.TryDequeueShortTask(out task);
           }

           // if task was dequeued and if it can be run by main thread: run it, otherwise put it back to queue it came from.
            if ( task != null)
             {
#if ROPO_TASK_TIME_PLOT
               long plotStartTimeInner = g_stopwatchPlotTimer.ElapsedMilliseconds;
#endif

               task.Execute();

#if ROPO_TASK_TIME_PLOT
               addPlotTime("Main Task", task.plotTimeTaskName == null ? "Main Wait Task" : task.plotTimeTaskName, plotStartTimeInner, g_stopwatchPlotTimer.ElapsedMilliseconds);
# endif

               ++numProcessedTasks;
             }
            

           // nudge workers
           m_workerThreadEvent.Set();

           return numProcessedTasks;
        }


        public static int RunNextTaskFromMainThreadQueue(bool executeOnlyShortTasks)
        {
            int numProcessedTasks = 0;

            ResTask task = null;
            if(executeOnlyShortTasks)
            {
                m_resourceLoadingTasksMainThread.TryDequeueShortTask(out task);
            }
            else
            {
                m_resourceLoadingTasksMainThread.TryDequeue(out task);
            }
            
            if(task != null)
            {
#if ROPO_TASK_TIME_PLOT
                long plotStartTimeInner = g_stopwatchPlotTimer.ElapsedMilliseconds;
#endif
                task.Execute();        

#if ROPO_TASK_TIME_PLOT
                addPlotTime("Main Task", task.plotTimeTaskName == null ? "Main Next Task" : task.plotTimeTaskName, plotStartTimeInner, g_stopwatchPlotTimer.ElapsedMilliseconds);
# endif
                ++numProcessedTasks;
            }

            // nudge workers
            m_workerThreadEvent.Set();

            return numProcessedTasks;
        }

        // public const int MaxNumTaskThreads = 8; // is capped because texture loading can use up a lot of memory so that we don't run out

        static bool m_isMultithreadedLoadingStarted = false;


        // static Dictionary<int, string> g_plotTimerThreadNames = new Dictionary<int, string>();


#if ROPO_TASK_TIME_WITH_THREAD_ID
        static int globalThreadLogCounter = 0;
#endif

        // Spawns loading threads. You MUST call 'FinishMultithreadedLoading' after your resources have been loading so threads are stopped.
        public static void StartOrContinueMultithreadedLoading()
        {
            // to allow this to be called every frame
            if (m_isMultithreadedLoadingStarted == true)
            {
                return;
            }
            else
            {
#if ROPO_TASK_TIME_PLOT
            g_stopwatchPlotTimer.Start();
#endif
            }

            m_mainThreadSyncContext = System.Threading.SynchronizationContext.Current;

            if (m_resourceLoadingTasksMainThread.Count > 0 || m_resourceLoadingTasksLowPriorityWorkerThread.Count > 0 || m_resourceLoadingTasksHighPriorityWorkerThread.Count > 0)
            {
                throw new Exception("bug, should both be 0");
            }
            if (m_workerTasks.Count > 0)
            {
                throw new Exception("bug, should both be 0");
            }

            // We must reset signals
            m_tasksMainThreadWait.Set();
            m_workerThreadEvent.Set();

            m_isMultithreadedLoadingStarted = true;

            // start as many tasks as cpu cores
            // int numCPU = Math.Min(System.Environment.ProcessorCount, MaxNumTaskThreads); // no need for limit as task system does the limiting, which is one abstraction level higher
            int numCPU = System.Environment.ProcessorCount - 1; // leave on thread free for main thread


            for (int i = 0; i < numCPU; ++i)
            {
                System.Threading.CancellationTokenSource token = new System.Threading.CancellationTokenSource();

#if ROPO_TASK_TIME_WITH_THREAD_ID
                int globalThreadIndexCountId = ++globalThreadLogCounter;
#endif


                System.Threading.Tasks.Task threadTask = System.Threading.Tasks.Task.Factory.StartNew(() =>
                 {
#if ROPO_TASK_TIME_PLOT

                  /*   int tid = System.Environment.CurrentManagedThreadId;
                     if (g_plotTimerThreadNames.ContainsKey(tid) == false)
                     {
                         g_plotTimerThreadNames.Add(tid, "Worker " + tid);
                     }*/
                     string threadName = "Worker "+System.Environment.CurrentManagedThreadId;
#endif

#if ROPO_TASK_TIME_WITH_THREAD_ID
                     Stopwatch sw = new Stopwatch();
#endif
                     int numProcessedTasks = 0;
                     while (token.IsCancellationRequested == false)
                     {
                         ResTask resTask = null;
                         if (m_resourceLoadingTasksHighPriorityWorkerThread.TryDequeue(out resTask))
                         {
                             // try running high priority tasks first
#if ROPO_TASK_TIME_WITH_THREAD_ID
                             sw.Reset();
                             sw.Start();
#endif

#if ROPO_TASK_TIME_PLOT
                             long plotStartTimeInner = g_stopwatchPlotTimer.ElapsedMilliseconds;
#endif
                             resTask.Execute();

#if ROPO_TASK_TIME_PLOT
                             addPlotTime(threadName, resTask.plotTimeTaskName == null ? "Task_Execute" : resTask.plotTimeTaskName, plotStartTimeInner, g_stopwatchPlotTimer.ElapsedMilliseconds);
#endif

#if ROPO_TASK_TIME_WITH_THREAD_ID
                             sw.Stop();
                             addTime("Task: Execute " + thisTid + ": ", sw.ElapsedMilliseconds);
#endif
                             ++numProcessedTasks;
                         }
                         else if (m_resourceLoadingTasksLowPriorityWorkerThread.TryDequeue(out resTask))
                         {
                             // try running low priority task
#if ROPO_TASK_TIME_WITH_THREAD_ID
                             sw.Reset();
                             sw.Start();
#endif

#if ROPO_TASK_TIME_PLOT
                             long plotStartTimeInner = g_stopwatchPlotTimer.ElapsedMilliseconds;
#endif
                             resTask.Execute();

#if ROPO_TASK_TIME_PLOT
                             addPlotTime(threadName, resTask.plotTimeTaskName == null ? "Task_Execute" : resTask.plotTimeTaskName, plotStartTimeInner, g_stopwatchPlotTimer.ElapsedMilliseconds);
#endif

#if ROPO_TASK_TIME_WITH_THREAD_ID
                             sw.Stop();
                             addTime("Task: Execute " + thisTid + ": ", sw.ElapsedMilliseconds);
#endif
                             ++numProcessedTasks;
                         }
                         else if (token.IsCancellationRequested == false)
                         {
                             m_workerThreadEvent.Reset();
                         }

#if ROPO_TASK_TIME_WITH_THREAD_ID
                         sw.Reset();
                         sw.Start();
#endif

#if ROPO_TASK_TIME_PLOT
                         long plotStartTime2 = g_stopwatchPlotTimer.ElapsedMilliseconds;
#endif

                         m_workerThreadEvent.WaitOne(); // TODO: CHECK HOW GOOD PARALELLILSM IS, SO THAT ALL THREADS ALL USED AND NOT ONLY ONE

#if ROPO_TASK_TIME_PLOT
                         addPlotTime(threadName, "Task_Wait", plotStartTime2, g_stopwatchPlotTimer.ElapsedMilliseconds);
#endif

#if ROPO_TASK_TIME_WITH_THREAD_ID
                         sw.Stop();
                         addTime("Task: WaitOne" + thisTid + ": ", sw.ElapsedMilliseconds);
#endif

                     }

                     //  Game.Instance.Window.log("ropo_numTasks", "Tid: "+Environment.CurrentManagedThreadId+": " +numProcessedTasks);

                 }, token.Token);

                m_workerTasks.Add(new WorkerTask(threadTask, token));
            }

        }

        // Stops loadings threads
        public static void FinishMultithreadedLoading()
        {
            // ignore if not running
            if (m_isMultithreadedLoadingStarted == false)
            {
                return;
            }

            foreach (WorkerTask task in m_workerTasks)
            {
                task.cancelToken.Cancel();
            }

            m_workerThreadEvent.Set();
            m_workerTasks.Clear();

            m_isMultithreadedLoadingStarted = false;
        }

        // todo ropo: this function can be reverted 99.9% sure
        protected T ReadAsset<T>(string assetName, Action<IDisposable> recordDisposableObject)
        {

            if (string.IsNullOrEmpty(assetName))
            {
                throw new ArgumentNullException("assetName");
            }
            if (disposed)
            {
                throw new ObjectDisposedException("ContentManager");
            }

            string originalAssetName = assetName;
            object result = null;

            if (this.graphicsDeviceService == null)
            {
                this.graphicsDeviceService = serviceProvider.GetService(typeof(IGraphicsDeviceService)) as IGraphicsDeviceService;
                if (this.graphicsDeviceService == null)
                {
                    throw new InvalidOperationException("No Graphics Device Service");
                }
            }

            // Try to load as XNB file

            var stream = OpenStream(assetName);

            BinaryReader xnbReader = new BinaryReader(stream);
            ContentReader reader = GetContentReaderFromXnb(assetName, stream, xnbReader, recordDisposableObject);

            result = reader.ReadAsset<T>();

            if (result is GraphicsResource)
                ((GraphicsResource)result).Name = originalAssetName;

#if ANDROID
                   reader.Close(); // monogame used the 'using' statement so they didn't need this
#endif

#if ANDROID
               xnbReader.Close(); // monogame used the 'using' statement so they didn't need this
#endif

            if (result == null)
                throw new ContentLoadException("Could not load " + originalAssetName + " asset!");


            if (result == null)
            {
                Game.Instance.Window.log("ropo_stopwatch", "SaladAssetHelper_BatchLoadAllTexturesInPack null tex 1");

            }
            return (T)result;
        }

        protected void ReadAssetCallback<T>(ContentManager.ResTask task, string assetName, Action<IDisposable> recordDisposableObject, ResCallback onDone)
        {
#if ANDROID && ROPO_PRINT
            Game.Instance.Window.log ("ropo_stopwatch", "ReadAsset 0: "+typeof(T).Name);
#endif
            if (string.IsNullOrEmpty(assetName))
            {
                throw new ArgumentNullException("assetName");
            }
            if (disposed)
            {
                throw new ObjectDisposedException("ContentManager");
            }

            string originalAssetName = assetName;

            if (this.graphicsDeviceService == null)
            {
                this.graphicsDeviceService = serviceProvider.GetService(typeof(IGraphicsDeviceService)) as IGraphicsDeviceService;
                if (this.graphicsDeviceService == null)
                {
                    throw new InvalidOperationException("No Graphics Device Service");
                }
            }

#if ANDROID && ROPO_PRINT
            Game.Instance.Window.log ("ropo_stopwatch", "ReadAsset 1");
#endif

            // Try to load as XNB file

#if ANDROID && ROPO_PRINT
            Game.Instance.Window.log ("ropo_stopwatch", "ReadAsset 2");
#endif
            var stream = OpenStream(assetName);
#if ANDROID && ROPO_PRINT
            Game.Instance.Window.log ("ropo_stopwatch", "ReadAsset 2");
#endif

#if ANDROID && ROPO_PRINT
            Game.Instance.Window.log ("ropo_stopwatch", "ReadAsset 2");
#endif

#if ROPO_ADD_TIME
            //  System.Diagnostics.Stopwatch stopwatchAction = new Stopwatch();
            //  stopwatchAction.Reset();
            //  stopwatchAction.Start();
#endif

#if ANDROID && ROPO_PRINT
                Game.Instance.Window.log ("ropo_stopwatch", "ReadAsset Task 1 " + typeof (T).Name);
#endif

            BinaryReader xnbReader = new BinaryReader(stream);

            ContentReader reader = GetContentReaderFromXnb(assetName, stream, xnbReader, recordDisposableObject);

            /* this FAILS WHEN RUN IN MULTITHREAD, CANNOT USE THREADPOOL BECAUSE ITS FULL OF QUEUES SO IT BLOCKS, TO BREAK
             THIS ACTION INTO 2 OR MAKE SMALLER ACTIONS THAT WILL FREE THE THREAD POOL

             MOVE THE ACTION ONE LEVEL DEEPER, SO THAT reader.ReadAsset does action call like this: reader.ReadAsset<T>(AsyncCallback)
             so that multithreading can be pushed on level down and conitnue so*/

            ResCallback onFinishedReadingAsset = (result) =>
            {
                object obj = result; // need so we can unsafely case below
                if (obj is GraphicsResource)
                    ((GraphicsResource)obj).Name = originalAssetName;

#if ANDROID // monogame used the 'using' statement so they didn't need this
                reader.Close();
                xnbReader.Close();
#endif

                if (result == null)
                {
                    throw new ContentLoadException("Could not load " + originalAssetName + " asset!");
                }

                onDone(result);
            };


            reader.ReadAssetCallback<T>(task, onFinishedReadingAsset);


#if ANDROID && ROPO_PRINT
                        Game.Instance.Window.log ("ropo_stopwatch", "ReadAsset Task 3 " + typeof (T).Name);
#endif



#if ANDROID && ROPO_PRINT
                Game.Instance.Window.log ("ropo_stopwatch", "ReadAsset End: " + typeof (T).Name);
#endif
#if ROPO_ADD_TIME
            //  stopwatchAction.Stop();
            //   addTime("ContentManager_new Entire Action", stopwatchAction.ElapsedMilliseconds);
#endif



#if ANDROID && ROPO_PRINT
            Game.Instance.Window.log ("ropo_stopwatch", "ReadAsset 3");
#endif


        }

        private ContentReader GetContentReaderFromXnb(string originalAssetName, Stream stream, BinaryReader xnbReader, Action<IDisposable> recordDisposableObject)
        {
            // The first 4 bytes should be the "XNB" header. i use that to detect an invalid file
            byte x = xnbReader.ReadByte();
            byte n = xnbReader.ReadByte();
            byte b = xnbReader.ReadByte();
            byte platform = xnbReader.ReadByte();

            if (x != 'X' || n != 'N' || b != 'B' ||
                !(targetPlatformIdentifiers.Contains((char)platform)))
            {
                throw new ContentLoadException("Asset does not appear to be a valid XNB file. Did you process your content for Windows?");
            }

            byte version = xnbReader.ReadByte();
            byte flags = xnbReader.ReadByte();

            bool compressedLzx = (flags & ContentCompressedLzx) != 0;
            bool compressedLz4 = (flags & ContentCompressedLz4) != 0;
            if (version != 5 && version != 4)
            {
                throw new ContentLoadException("Invalid XNB version");
            }

            // The next int32 is the length of the XNB file
            int xnbLength = xnbReader.ReadInt32();

            Stream decompressedStream = null;
            if (compressedLzx || compressedLz4)
            {
                // Decompress the xnb
                int decompressedSize = xnbReader.ReadInt32();

                if (compressedLzx)
                {
                    int compressedSize = xnbLength - 14;
                    decompressedStream = new LzxDecoderStream(stream, decompressedSize, compressedSize);
                }
                else if (compressedLz4)
                {
                    decompressedStream = new Lz4DecoderStream(stream);
                }
            }
            else
            {
                decompressedStream = stream;
            }

            var reader = new ContentReader(this, decompressedStream, this.graphicsDeviceService.GraphicsDevice,
                                                        originalAssetName, version, recordDisposableObject);

            return reader;
        }

        internal void RecordDisposable(IDisposable disposable)
        {
            Debug.Assert(disposable != null, "The disposable is null!");

            // Avoid recording disposable objects twice. ReloadAsset will try to record the disposables again.
            // We don't know which asset recorded which disposable so just guard against storing multiple of the same instance.
            if (!disposableAssets.Contains(disposable))
                disposableAssets.Add(disposable);
        }

        /// <summary>
        /// Virtual property to allow a derived ContentManager to have it's assets reloaded
        /// </summary>
        protected virtual Dictionary<string, object> LoadedAssets
        {
            get { return loadedAssets; }
        }

        protected virtual void ReloadGraphicsAssets()
        {
            foreach (var asset in LoadedAssets)
            {
                // This never executes as asset.Key is never null.  This just forces the 
                // linker to include the ReloadAsset function when AOT compiled.
                if (asset.Key == null)
                    ReloadAsset(asset.Key, Convert.ChangeType(asset.Value, asset.Value.GetType()));

#if WINDOWS_STOREAPP || WINDOWS_UAP
                var methodInfo = typeof(ContentManager).GetType().GetTypeInfo().GetDeclaredMethod("ReloadAsset");
#else
                var methodInfo = typeof(ContentManager).GetMethod("ReloadAsset", BindingFlags.NonPublic | BindingFlags.Instance);
#endif
                var genericMethod = methodInfo.MakeGenericMethod(asset.Value.GetType());
                genericMethod.Invoke(this, new object[] { asset.Key, Convert.ChangeType(asset.Value, asset.Value.GetType()) });
            }
        }

        protected virtual void ReloadAsset<T>(string originalAssetName, T currentAsset)
        {
            string assetName = originalAssetName;
            if (string.IsNullOrEmpty(assetName))
            {
                throw new ArgumentNullException("assetName");
            }
            if (disposed)
            {
                throw new ObjectDisposedException("ContentManager");
            }

            if (this.graphicsDeviceService == null)
            {
                this.graphicsDeviceService = serviceProvider.GetService(typeof(IGraphicsDeviceService)) as IGraphicsDeviceService;
                if (this.graphicsDeviceService == null)
                {
                    throw new InvalidOperationException("No Graphics Device Service");
                }
            }

            var stream = OpenStream(assetName);
            using (var xnbReader = new BinaryReader(stream))
            {
                using (var reader = GetContentReaderFromXnb(assetName, stream, xnbReader, null))
                {
                    reader.ReadAsset<T>(currentAsset);
                }
            }
        }

        public virtual void Unload()
        {
            // Look for disposable assets.
            foreach (var disposable in disposableAssets)
            {
                if (disposable != null)
                    disposable.Dispose();
            }
            disposableAssets.Clear();
            loadedAssets.Clear();
        }

        public string RootDirectory
        {
            get
            {
                return _rootDirectory;
            }
            set
            {
                _rootDirectory = value;
            }
        }

        internal string RootDirectoryFullPath
        {
            get
            {
                return Path.Combine(TitleContainer.Location, RootDirectory);
            }
        }

        public IServiceProvider ServiceProvider
        {
            get
            {
                return this.serviceProvider;
            }
        }

        internal byte[] GetScratchBuffer(int size)
        {
            size = Math.Max(size, 1024 * 1024);
            if (scratchBuffer == null || scratchBuffer.Length < size)
                scratchBuffer = new byte[size];
            return scratchBuffer;
        }

        static Dictionary<int, byte[]> threadScratchBuffer = new Dictionary<int, byte[]>();
        static object _threadScratchBufferLock = new object();

        internal byte[] GetScratchBufferForCurrentThread(int size)
        {
            int threadId = Environment.CurrentManagedThreadId;

            lock (_threadScratchBufferLock)
            {
                size = Math.Max(size, 1024 * 1024);
                byte[] buf = null;
                if (threadScratchBuffer.ContainsKey(threadId))
                {
                    buf = threadScratchBuffer[threadId];
                }
                if (buf == null || buf.Length < size)
                {
                    buf = new byte[size];
                    threadScratchBuffer[threadId] = buf;
                }
                return buf;
            }
        }
    }
}
