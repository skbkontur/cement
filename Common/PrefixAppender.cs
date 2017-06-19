using System;
using log4net;
using log4net.Core;

namespace Common
{
	public class PrefixAppender : ILog
	{
		private readonly ILog log;
		private readonly string prefix;

		public PrefixAppender(string prefix, ILog log)
		{
			this.log = log;
			this.prefix = prefix;
		}

		public ILogger Logger => log.Logger;

	    public void Debug(object message)
		{
			log.Debug($"[{prefix}] " + message);
		}

		public void Debug(object message, Exception exception)
		{
			log.Debug($"[{prefix}] " + message, exception);
		}

		public void DebugFormat(string format, params object[] args)
		{
			log.DebugFormat($"[{prefix}] " + format, args);
		}

		public void DebugFormat(string format, object arg0)
		{
			log.DebugFormat($"[{prefix}] " + format, arg0);
		}

		public void DebugFormat(string format, object arg0, object arg1)
		{
			log.DebugFormat($"[{prefix}] " + format, arg0, arg1);
		}

		public void DebugFormat(string format, object arg0, object arg1, object arg2)
		{
			log.DebugFormat($"[{prefix}] " + format, arg0, arg1, arg2);
		}

		public void DebugFormat(IFormatProvider provider, string format, params object[] args)
		{
			log.DebugFormat(provider, $"[{prefix}] " + format, args);
		}

		public void Info(object message)
		{
			log.Info($"[{prefix}] " + message);
		}

		public void Info(object message, Exception exception)
		{
			log.Info($"[{prefix}] " + message, exception);
		}

		public void InfoFormat(string format, params object[] args)
		{
			log.InfoFormat($"[{prefix}] " + format, args);
		}

		public void InfoFormat(string format, object arg0)
		{
			log.InfoFormat($"[{prefix}] " + format, arg0);
		}

		public void InfoFormat(string format, object arg0, object arg1)
		{
			log.InfoFormat($"[{prefix}] " + format, arg0, arg1);
		}

		public void InfoFormat(string format, object arg0, object arg1, object arg2)
		{
			log.InfoFormat($"[{prefix}] " + format, arg0, arg1, arg2);
		}

		public void InfoFormat(IFormatProvider provider, string format, params object[] args)
		{
			log.InfoFormat(provider, $"[{prefix}] " + format, args);
		}

		public void Warn(object message)
		{
			log.Warn($"[{prefix}] " + message);
		}

		public void Warn(object message, Exception exception)
		{
			log.Warn($"[{prefix}] " + message, exception);
		}

		public void WarnFormat(string format, params object[] args)
		{
			log.WarnFormat($"[{prefix}] " + format, args);
		}

		public void WarnFormat(string format, object arg0)
		{
			log.WarnFormat($"[{prefix}] " + format, arg0);
		}

		public void WarnFormat(string format, object arg0, object arg1)
		{
			log.WarnFormat($"[{prefix}] " + format, arg0, arg1);
		}

		public void WarnFormat(string format, object arg0, object arg1, object arg2)
		{
			log.WarnFormat($"[{prefix}] " + format, arg0, arg1, arg2);
		}

		public void WarnFormat(IFormatProvider provider, string format, params object[] args)
		{
			log.WarnFormat(provider, $"[{prefix}] " + format, args);
		}

		public void Error(object message)
		{
			log.Error($"[{prefix}] " + message);
		}

		public void Error(object message, Exception exception)
		{
			log.Error($"[{prefix}] " + message, exception);
		}

		public void ErrorFormat(string format, params object[] args)
		{
			log.ErrorFormat($"[{prefix}] " + format, args);
		}

		public void ErrorFormat(string format, object arg0)
		{
			log.ErrorFormat($"[{prefix}] " + format, arg0);
		}

		public void ErrorFormat(string format, object arg0, object arg1)
		{
			log.ErrorFormat($"[{prefix}] " + format, arg0, arg1);
		}

		public void ErrorFormat(string format, object arg0, object arg1, object arg2)
		{
			log.ErrorFormat($"[{prefix}] " + format, arg0, arg1, arg2);
		}

		public void ErrorFormat(IFormatProvider provider, string format, params object[] args)
		{
			log.ErrorFormat(provider, $"[{prefix}] " + format, args);
		}

		public void Fatal(object message)
		{
			log.Fatal($"[{prefix}] " + message);
		}

		public void Fatal(object message, Exception exception)
		{
			log.Fatal($"[{prefix}] " + message, exception);
		}

		public void FatalFormat(string format, params object[] args)
		{
			log.FatalFormat($"[{prefix}] " + format, args);
		}

		public void FatalFormat(string format, object arg0)
		{
			log.FatalFormat($"[{prefix}] " + format, arg0);
		}

		public void FatalFormat(string format, object arg0, object arg1)
		{
			log.FatalFormat($"[{prefix}] " + format, arg0, arg1);
		}

		public void FatalFormat(string format, object arg0, object arg1, object arg2)
		{
			log.FatalFormat($"[{prefix}] " + format, arg0, arg1, arg2);
		}

		public void FatalFormat(IFormatProvider provider, string format, params object[] args)
		{
			log.FatalFormat(provider, $"[{prefix}] " + format, args);
		}

		public bool IsDebugEnabled => log.IsDebugEnabled;

	    public bool IsInfoEnabled => log.IsInfoEnabled;

	    public bool IsWarnEnabled => log.IsWarnEnabled;

	    public bool IsErrorEnabled => log.IsErrorEnabled;

	    public bool IsFatalEnabled => log.IsFatalEnabled;
	}
}
