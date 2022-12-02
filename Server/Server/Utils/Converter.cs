using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Utils
{
    public static class Converter
    {
        public static string BuildStr(List<Network.DB.TOP> tOPs)
        {
            string empty = string.Empty;
            foreach (var tOP in tOPs)
                empty+=tOP.ToString();

            return empty;
        }
    }
}
