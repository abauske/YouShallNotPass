using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Logging
{
    public class DataWriter
    {
        private readonly string _path;
        private StringBuilder builder = new StringBuilder();
        private CultureInfo culture = new CultureInfo("en-US");

        public DataWriter(string path) {
            _path = path;
        }
 
        public void WriteLine(string line)
        {
            File.AppendAllText(_path, line + "\r\n");
        }
 
        public void WriteCsv(IList<object> data)
        {
            builder.Clear();
            foreach (object d in data)
            {
                builder.Append(d is float f ? f.ToString(culture) : d);
                builder.Append(';');
            }

            builder.Remove(builder.Length - 1, 1);
            builder.Append("\r\n");
            File.AppendAllText(_path, builder.ToString());
        }
 
        public void WriteText(List<string> data)
        {
            foreach(string t in data)
            {
                WriteLine(t);
            }

            Debug.Log("done writing file");
        }
    }
}
