using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace Extensions
{
    public static class Extensions
    {
        // JToken Extension Methods
        public static bool ContainsKey(this JToken jt, string key)
        {
            return jt[key] != null;
        }
    }
}
