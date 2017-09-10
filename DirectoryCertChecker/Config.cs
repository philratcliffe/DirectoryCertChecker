#region Copyright and license information

// Copyright © 2017 Phil Ratcliffe
// 
// This file is part of DirectoryCertChecker program.
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

#endregion

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Reflection;
using log4net;

namespace DirectoryCertChecker
{
    /// <summary>
    ///     A wrapper around ConfigurationManager that provides methods for getting configuration information.
    /// </summary>
    internal class Config
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static readonly char ConfigItemSeparator = ';'; 
        

        internal static string GetAppSetting(string key)
        {
            var value = ConfigurationManager.AppSettings[key];

            if (value == null)
                throw new ConfigurationErrorsException($"Unable to read {key}");

            return value;
        }

        internal static string GetAppSetting(string key, string defaultValue)
        {
            return ConfigurationManager.AppSettings[key] ?? defaultValue;
        }

        internal static int GetIntAppSetting(string key, int defaultValue)
        {
            int i;

            if (!int.TryParse(GetAppSetting(key, defaultValue.ToString(CultureInfo.InvariantCulture)), out i))
            {
                Log.Error(key + " value is not a valid integer.");
                return defaultValue;
            }

            return i;
        }

        internal static bool GetBoolAppSetting(string key)
        {
            var value = ConfigurationManager.AppSettings[key];
            if (!string.IsNullOrEmpty(value))
                return bool.Parse(value);

            return false;
        }

        internal static List<string> GetListAppSetting(string key)
        {
            String value = GetAppSetting(key, "");
            if (!string.IsNullOrEmpty(value))
            {
                return new List<string>(value.Split(ConfigItemSeparator));
            }

            return new List<string>();
        }
    }
}