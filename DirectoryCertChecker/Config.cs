using System.Configuration;
using System.Reflection;
using log4net;

namespace DirectoryCertChecker
{
    internal class Config
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public static string GetAppSetting(string key)
        {
            var value = ConfigurationManager.AppSettings[key];

            if (value == null)
                throw new ConfigurationErrorsException($"Unable to read {key}");

            return value;
        }

        public static string GetAppSetting(string key, string defaultValue)
        {
            return ConfigurationManager.AppSettings[key] ?? defaultValue;
        }

        public static bool GetBoolAppSetting(string key)
        {
            var value = ConfigurationManager.AppSettings[key];
            if (!string.IsNullOrEmpty(value))
                return bool.Parse(value);

            return false;
        }
    }
}