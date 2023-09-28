using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace FastRawSelector.LOGIC
{
    public static class Log
    {
        private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private static readonly string appName = Assembly.GetExecutingAssembly().GetName().Name;

        private static string GetPath(string fileName)
        {
            int startIndex = fileName.LastIndexOf(appName);
            startIndex = startIndex == -1 ? 0 : startIndex + appName.Length + 1;
            return fileName.Substring(startIndex);
        }

        public static void Debug(string msg, [CallerMemberName] string methodName = null, [CallerFilePath] string fileName = "")
        {
            logger.Debug($"{msg} [{GetPath(fileName)} - {methodName}()]");
        }

        public static void Info(string msg, [CallerMemberName] string methodName = null, [CallerFilePath] string fileName = "")
        {
            logger.Info($"{msg} [{GetPath(fileName)} - {methodName}()]");
        }

        public static void Warn(string msg, [CallerMemberName] string methodName = null, [CallerFilePath] string fileName = "")
        {
            logger.Warn($"{msg} [{GetPath(fileName)} - {methodName}()]");
        }

        public static void Error(string msg, [CallerMemberName] string methodName = null, [CallerFilePath] string fileName = "")
        {
            logger.Error($"{msg} [{GetPath(fileName)} - {methodName}()]");
        }

        public static void Fatal(string msg, [CallerMemberName] string methodName = null, [CallerFilePath] string fileName = "")
        {
            logger.Fatal($"{msg} [{GetPath(fileName)} - {methodName}()]");
        }

        public static void Exception(Exception ex, [CallerMemberName] string methodName = null, [CallerFilePath] string fileName = "")
        {
            logger.Error($"Type : {ex.GetType().FullName}, Message : {ex.Message}, Stack Trace : {ex.StackTrace} [{GetPath(fileName)} - {methodName}()]");
        }

        public static void ExceptionWithMsg(string msg, Exception ex, [CallerMemberName] string methodName = null, [CallerFilePath] string fileName = "")
        {
            logger.Error($"Msg : {msg}, Type : {ex.GetType().FullName}, Message : {ex.Message}, Stack Trace : {ex.StackTrace} [{GetPath(fileName)} - {methodName}()]");
        }
    }
}
