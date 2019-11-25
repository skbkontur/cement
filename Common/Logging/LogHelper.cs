using System;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Common.Logging
{
    public static class LogHelper
    {
        public static void InitializeFileAndElkLogging(string logFileName)
        {
            LogManager.InitializeHerculesLogger();
            LogManager.InitializeFileLogger(logFileName);
        }

        public static void InitializeGlobalFileAndElkLogging()
        {
            LogManager.InitializeHerculesLogger();
            LogManager.InitializeFileLogger(null);
        }

        public static void SaveLog(string log)
        {
            try
            {
                var file = Path.Combine(Helper.GetCementInstallDirectory(), "log.log");
                if (!File.Exists(file))
                    File.Create(file).Close();
                File.AppendAllText(file, "\n" + log);
            }
            catch (Exception)
            {
                // ignored
            }
        }

        public static void SendSavedLog()
        {
            try
            {
                var file = Path.Combine(Helper.GetCementInstallDirectory(), "log.log");
                if (!File.Exists(file))
                    File.Create(file).Close();
                var lines = File.ReadAllLines(file).Where(l => l.Length > 0).ToList();
                File.WriteAllText(file, "");

                var log = LogManager.GetLogger("SAVED-LOG");

                foreach (var line in lines)
                {
                    log.LogDebug(line);
                }
            }
            catch (Exception)
            {
                // ignored
            }
        }
    }
}