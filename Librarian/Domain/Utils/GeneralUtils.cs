using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace Nyerguds.Util
{
    public static class GeneralUtils
    {
        public static bool IsNumeric(string str)
        {
            foreach (char c in str)
                if (c < '0' || c > '9')
                    return false;
            return true;
        }
        
        /// <summary>
        /// Checks if the given value starts with T, J, Y, O (TRUE, JA, YES, OUI) or is 1
        /// If the value is null or the parse fails, the default is False.
        /// </summary>
        /// <param name="value">String to parse</param>
        /// <returns>True if the string's first letter matches J, Y, O, 1 or T</returns>
        public static bool IsTrueValue(string value)
        {
            return IsTrueValue(value, false);
        }
        /// <summary>
        /// Checks if the given value starts with T, J, Y, O (TRUE, JA, YES, OUI) or is 1
        /// </summary>
        /// <param name="value">String to parse</param>
        /// <param name="defaultVal">Default value to return in case parse fails</param>
        /// <returns>True if the string's first letter matches J, Y, O, 1 or T</returns>
        public static bool IsTrueValue(string value, bool defaultVal)
        {
            if (string.IsNullOrEmpty(value))
                return defaultVal;
            return Regex.IsMatch(value, "^(([TJYO].*)|(0*1))$", RegexOptions.IgnoreCase);
        }

        public static bool IsHexadecimal(string str)
        {
            return Regex.IsMatch(str, "^[0-9A-F]*$", RegexOptions.IgnoreCase);
        }

        public static string GetApplicationPath()
        {
            return Path.GetDirectoryName(Application.ExecutablePath);
        }

        public static TEnum TryParseEnum<TEnum>(string value, TEnum defaultValue, bool ignoreCase) where TEnum : struct
        {
            if (string.IsNullOrEmpty(value))
                return defaultValue;
            try { return (TEnum)Enum.Parse(typeof(TEnum), value, ignoreCase); }
            catch (ArgumentException) { return defaultValue; }
        }

        public static string GetAbsolutePath(string relativePath, string basePath)
        {
            if (relativePath == null)
                return null;
            if (basePath == null)
                basePath = Path.GetFullPath("."); // quick way of getting current working directory
            else
                basePath = GetAbsolutePath(basePath, null); // to be REALLY sure ;)
            string path;
            // specific for windows paths starting on \ - they need the drive added to them.
            // I constructed this piece like this for possible Mono support.
            if (!Path.IsPathRooted(relativePath) || "\\".Equals(Path.GetPathRoot(relativePath)))
            {
                if (relativePath.StartsWith(Path.DirectorySeparatorChar.ToString()))
                    path = Path.Combine(Path.GetPathRoot(basePath), relativePath.TrimStart(Path.DirectorySeparatorChar));
                else
                    path = Path.Combine(basePath, relativePath);
            }
            else
                path = relativePath;
            // resolves any internal "..\" to get the true full path.
            int filenameStart = path.LastIndexOf(Path.DirectorySeparatorChar);
            string dirPart = path.Substring(0, filenameStart + 1);
            string filePart = path.Substring(filenameStart + 1);
            if (filePart.Contains("*") || filePart.Contains("?"))
            {
                dirPart = Path.GetFullPath(dirPart);
                return Path.Combine(dirPart, filePart);
            }
            return Path.GetFullPath(path);
        }

        public static DateTime GetDosDateTime(ushort dosTime, ushort dosDate)
        {
            int sec = (dosTime & 0x1F) * 2;
            int min = ((dosTime >> 5) & 0x3F);
            int hour = ((dosTime >> 11) & 0x1F);
            if (sec > 59 || min > 59 || hour > 23)
                throw new ArgumentException("Bad time stamp.");
            int day = (dosDate & 0x1F);
            int month = ((dosDate >> 5) & 0x0F);
            int year = 1980 + ((dosDate >> 9) & 0x7F);
            if (day == 0 || month == 0 || month > 12)
                throw new ArgumentException("Bad date stamp.");
            return new DateTime(year, month, day, hour, min, sec);
        }

        public static ushort GetDosDateInt(DateTime datestamp)
        {
            int year = Math.Max(Math.Min(0, datestamp.Year - 1980), 127);
            return (ushort)((datestamp.Day) | (datestamp.Month << 5) | (year << 9));
        }

        public static ushort GetDosTimeInt(DateTime datestamp)
        {
            return (ushort)((datestamp.Second >> 1) | (datestamp.Minute << 5) | (datestamp.Hour << 11));
        }

        /// <summary>
        /// Tool to get date string for ExtraInfo from a dateTime.
        /// </summary>
        /// <param name="datestamp">date stamp</param>
        /// <returns>String for ExtraInfo</returns>
        public static string GetDateString(DateTime datestamp)
        {
            return "Date: " + datestamp.Year.ToString("D4") + "-" + datestamp.Month.ToString("D2") + "-" + datestamp.Day.ToString("D2") + "\n"
                    + "Time: " + datestamp.Hour.ToString("D2") + ":" + datestamp.Minute.ToString("D2") + ":" + datestamp.Second.ToString("D2");
        }

        public static string GetDos83FileName(string file)
        {
            string filename = Path.GetFileNameWithoutExtension(file) ?? string.Empty;
            filename = new string(filename.Replace(' ', '_').Where(x => x > 0x20 && x < 0x7F).ToArray());
            string extension = Path.GetExtension(file) ?? string.Empty;
            extension = new string(extension.Replace(' ', '_').Where(x => x > 0x20 && x < 0x7F).ToArray());
            if (filename.Length > 8)
                filename = filename.Substring(0, 8);
            if (extension.Length > 4)
                extension = extension.Substring(0, 4);
            return (filename + extension).ToUpperInvariant();
        }

        public static string ProgramVersion()
        {
            FileVersionInfo ver = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);
            //Version v = AssemblyName.GetAssemblyName(Assembly.GetExecutingAssembly().Location).Version;
            string version = string.Format("v{0}.{1}", ver.FileMajorPart, ver.FileMinorPart);
            if (ver.FileBuildPart > 0)
                version += "." + ver.FileBuildPart;
            if (ver.FilePrivatePart > 0)
                version += "." + ver.FilePrivatePart;
            return version;
        }

        public static string DoubleFirstAmpersand(string input)
        {
            if (input == null)
                return null;
            int index = input.IndexOf('&');
            if (index == -1)
                return input;
            return input.Substring(0, index) + '&' + input.Substring(index);
        }

        /// <summary>
        /// Reads all remaining bytes from a stream behind the current Position. This does not close the stream.
        /// </summary>
        /// <param name="stream">Stream</param>
        /// <returns>The contents of the stream, behind the current Position.</returns>
        public static byte[] ReadAllBytes(this Stream stream)
        {
            using (BinaryReader reader = new BinaryReader(new NonDisposingStream(stream)))
            {
                return ReadAllBytes(reader);
            }
        }

        public static byte[] ReadAllBytes(this BinaryReader reader)
        {
            const int bufferSize = 4096;
            using (var ms = new MemoryStream())
            {
                byte[] buffer = new byte[bufferSize];
                int count;
                while ((count = reader.Read(buffer, 0, buffer.Length)) != 0)
                    ms.Write(buffer, 0, count);
                return ms.ToArray();
            }
        }
    }
}
