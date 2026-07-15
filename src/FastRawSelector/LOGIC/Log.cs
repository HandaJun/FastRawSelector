using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using log4net;
using log4net.Core;
using log4net.Repository.Hierarchy;

namespace FastRawSelector.LOGIC
{
    /// <summary>
    /// log4net 래퍼. 출력 위치: %AppData%\Roaming\FastRawSelector\logs\
    /// 루트 레벨은 ApplicationSetting.LogLevel 과 ApplyRootLevel 로 맞춘다.
    /// </summary>
    public static class Log
    {
        private static readonly ILog logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private static readonly string appName = Assembly.GetExecutingAssembly().GetName().Name;

        private static string GetPath(string fileName)
        {
            int startIndex = fileName.LastIndexOf(appName);
            startIndex = startIndex == -1 ? 0 : startIndex + appName.Length + 1;
            return fileName.Substring(startIndex);
        }

        /// <summary>
        /// 루트 로거 레벨 적용. "DEBUG" | "INFO"(기본) | "WARN" | "ERROR".
        /// root 가 INFO 이면 DEBUG 메시지는 appender 에 도달하지 않는다(DEBUG_*.log 비대화 방지).
        /// </summary>
        public static void ApplyRootLevel(string levelName)
        {
            try
            {
                string name = string.IsNullOrWhiteSpace(levelName) ? "INFO" : levelName.Trim().ToUpperInvariant();
                if (name != "DEBUG" && name != "INFO" && name != "WARN" && name != "ERROR")
                {
                    name = "INFO";
                }

                var hierarchy = (Hierarchy)LogManager.GetRepository();
                Level level = hierarchy.LevelMap[name] ?? Level.Info;
                hierarchy.Root.Level = level;
                hierarchy.RaiseConfigurationChanged(EventArgs.Empty);
                // Configure 직후에는 logger 가 아직 초기 레벨일 수 있어 Info 로 기록
                logger.Info($"로그 레벨 적용: {name}");
            }
            catch (Exception ex)
            {
                try
                {
                    logger.Error($"로그 레벨 적용 실패: {ex.Message}");
                }
                catch
                {
                }
            }
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
