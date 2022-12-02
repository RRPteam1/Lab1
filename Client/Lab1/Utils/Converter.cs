using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lab1.Utils
{
    public static class Converter
    {
        public static string ToStr(string s)
        {
            var data = s.Split('-').Select(b => Convert.ToByte(b, 16)).ToArray();
            return Encoding.ASCII.GetString(data);
        }
    }
}
