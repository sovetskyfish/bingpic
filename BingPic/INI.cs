using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BingPic
{
    class INI
    {
        readonly string path;

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        static extern long WritePrivateProfileString(string Section, string Key, string Value, string FilePath);

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        static extern int GetPrivateProfileString(string Section, string Key, string Default, StringBuilder RetVal, int Size, string FilePath);

        public INI(string Path)
        {
            path = new FileInfo(Path).FullName;
        }

        public string Read(string Key, string Section = "Core")
        {
            var RetVal = new StringBuilder(255);
            GetPrivateProfileString(Section, Key, "", RetVal, 255, path);
            return RetVal.ToString();
        }

        public void Write(string Key, string Value, string Section = "Core")
        {
            WritePrivateProfileString(Section, Key, Value, path);
        }

        public void DeleteKey(string Key, string Section = "Core")
        {
            Write(Key, null, Section);
        }

        public void DeleteSection(string Section = "Core")
        {
            Write(null, null, Section);
        }

        public bool KeyExists(string Key, string Section = "Core")
        {
            return Read(Key, Section).Length > 0;
        }
    }
}
