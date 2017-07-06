using System;
using System.IO;
using System.Text;
using System.Threading;
using log4net.Appender;
using log4net.Core;
using log4net.Filter;
using log4net.Layout;
using log4net.Util;

namespace Common.Logging
{
    public class AsyncRollingFileAppender : IBulkAppender, IOptionHandler
    {
        public void ActivateOptions()
        {
            lock (syncObject)
            {
                Shutdown();

                appender.ActivateOptions();

                pendingEvents = new BoundedBuffer<LoggingEvent>(QueueSizeLimit);
                shutdownEvent = new ManualResetEvent(false);
                appenderThread = new Thread(AppenderRoutine)
                {
                    IsBackground = true
                };
                appenderThread.Start();
            }
        }

        public void Close()
        {
            lock (syncObject)
            {
                Shutdown();
            }

            GC.SuppressFinalize(this);
        }

        ~AsyncRollingFileAppender()
        {
            lock (syncObject)
            {
                Shutdown();
            }
        }

        public void DoAppend(LoggingEvent loggingEvent)
        {
            if (!FilterEvent(loggingEvent))
                return;

            var eventsQueue = pendingEvents;
            if (eventsQueue == null)
                return;

            if (!eventsQueue.TryAdd(loggingEvent))
                Interlocked.Increment(ref discardedEventsCounter);
        }

        public void DoAppend(LoggingEvent[] loggingEvents)
        {
            foreach (var loggingEvent in loggingEvents)
                DoAppend(loggingEvent);
        }

        public void AddFilter(IFilter filter)
        {
            appender.AddFilter(filter);
        }

        public void ClearFilters()
        {
            appender.ClearFilters();
        }

        private void Shutdown()
        {
            if (shutdownEvent == null || appenderThread == null)
                return;

            try
            {
                shutdownEvent.Set();

                if (!appenderThread.Join(TimeSpan.FromSeconds(5)))
                {
                    appenderThread.Abort();
                    appenderThread.Join();
                }

                appender.Close();

                appenderThread = null;
                shutdownEvent = null;
            }
            catch (Exception error)
            {
                LogLog.Error(GetType(), "Failure in shutdown process.", error);
            }
        }

        private void AppenderRoutine()
        {
            while (true)
            {
                bool shuttingDown = shutdownEvent.WaitOne(FlushPeriodMilliseconds);

                AppendPendingEvents();

                if (shuttingDown)
                    return;
            }
        }

        private void AppendPendingEvents()
        {
            try
            {
                var loggingEvents = pendingEvents.Drain();
                if (loggingEvents.Length == 0)
                    return;

                appender.DoAppend(loggingEvents);

                int discardedEvents = Interlocked.Exchange(ref discardedEventsCounter, 0);
                if (discardedEvents > 0)
                    LogDiscardedEventsCount(discardedEvents);
            }
            catch (Exception error)
            {
                LogLog.Error(GetType(), "Failure in appender routine.", error);
            }
        }

        private void LogDiscardedEventsCount(int discardedEvents)
        {
            appender.DoAppend(new LoggingEvent(new LoggingEventData
            {
                Level = Level.Warn,
                Message = String.Format("[{0}] Buffer overflow. {1} logging events were lost (queue size limit = {2}).", GetType().Name, discardedEvents, QueueSizeLimit),
                TimeStamp = DateTime.Now,
                LoggerName = GetType().FullName
            }));
        }

        #region Custom properties

        public int QueueSizeLimit
        {
            get
            {
                return queueSizeLimit;
            }
            set
            {
                if (value <= 0)
                    return;

                queueSizeLimit = value;
            }
        }

        public int FlushPeriodMilliseconds
        {
            get
            {
                return flushPeriodMilliseconds;
            }
            set
            {
                if (value <= 0)
                    return;

                flushPeriodMilliseconds = value;
            }
        }

        #endregion

        #region Delegating properties

        public Boolean AppendToFile
        {
            get { return appender.AppendToFile; }
            set { appender.AppendToFile = value; }
        }

        public Int32 CountDirection
        {
            get { return appender.CountDirection; }
            set { appender.CountDirection = value; }
        }

        public string DatePattern
        {
            get { return appender.DatePattern; }
            set { appender.DatePattern = value; }
        }

        public RollingFileAppender.IDateTime DateTimeStrategy
        {
            get { return appender.DateTimeStrategy; }
            set { appender.DateTimeStrategy = value; }
        }

        public Encoding Encoding
        {
            get { return appender.Encoding; }
            set { appender.Encoding = value; }
        }

        public IErrorHandler ErrorHandler
        {
            get { return appender.ErrorHandler; }
            set { appender.ErrorHandler = value; }
        }

        public string File
        {
            get { return appender.File; }
            set { appender.File = value; }
        }

        public IFilter FilterHead
        {
            get { return appender.FilterHead; }
        }

        public Boolean ImmediateFlush
        {
            get { return appender.ImmediateFlush; }
            set { appender.ImmediateFlush = value; }
        }

        public ILayout Layout
        {
            get { return appender.Layout; }
            set { appender.Layout = value; }
        }

        public FileAppender.LockingModelBase LockingModel
        {
            get { return appender.LockingModel; }
            set { appender.LockingModel = value; }
        }

        public Int64 MaxFileSize
        {
            get { return appender.MaxFileSize; }
            set { appender.MaxFileSize = value; }
        }

        public Int32 MaxSizeRollBackups
        {
            get { return appender.MaxSizeRollBackups; }
            set { appender.MaxSizeRollBackups = value; }
        }

        public string MaximumFileSize
        {
            get { return appender.MaximumFileSize; }
            set { appender.MaximumFileSize = value; }
        }

        public string Name
        {
            get { return appender.Name; }
            set { appender.Name = value; }
        }

        public Boolean PreserveLogFileNameExtension
        {
            get { return appender.PreserveLogFileNameExtension; }
            set { appender.PreserveLogFileNameExtension = value; }
        }

        public RollingFileAppender.RollingMode RollingStyle
        {
            get { return appender.RollingStyle; }
            set { appender.RollingStyle = value; }
        }

        public Boolean StaticLogFileName
        {
            get { return appender.StaticLogFileName; }
            set { appender.StaticLogFileName = value; }
        }

        public Level Threshold
        {
            get { return appender.Threshold; }
            set { appender.Threshold = value; }
        }

        public TextWriter Writer
        {
            get { return appender.Writer; }
            set { appender.Writer = value; }
        }

        #endregion

        #region Filtering (taken from AppenderSkeleton)

        private Boolean FilterEvent(LoggingEvent loggingEvent)
        {
            if (!IsAsSevereAsThreshold(loggingEvent.Level))
                return false;

            IFilter filter = appender.FilterHead;

            while (filter != null)
            {
                switch (filter.Decide(loggingEvent))
                {
                    case FilterDecision.Deny:
                        return false;
                    case FilterDecision.Neutral:
                        filter = filter.Next;
                        break;
                    case FilterDecision.Accept:
                        filter = null;
                        break;
                }
            }
            return true;
        }

        private Boolean IsAsSevereAsThreshold(Level level)
        {
            return Threshold == null || level >= Threshold;
        }

        #endregion

        private readonly RollingFileAppender appender = new RollingFileAppender();
        private readonly object syncObject = new object();

        private BoundedBuffer<LoggingEvent> pendingEvents;
        private ManualResetEvent shutdownEvent;
        private Thread appenderThread;
        private int discardedEventsCounter;

        private int queueSizeLimit = 100 * 1000;
        private int flushPeriodMilliseconds = 200;
    }
}