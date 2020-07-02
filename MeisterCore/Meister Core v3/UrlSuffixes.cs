using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
namespace MeisterCore
{
    public static class UrlSuffixes
    {
        public static string AddUrlParams(this string urlString, Dictionary<string, string> parameters)
        {
            if (parameters == null || !parameters.Keys.Any())
                return urlString;
            var tempUrl = new StringBuilder($"{urlString}?");
            int count = 0;
            foreach (KeyValuePair<string, string> parameter in parameters)
            {
                if (count > 0)
                    tempUrl.Append("&");
                tempUrl.Append($"{WebUtility.UrlEncode(parameter.Key)}={WebUtility.UrlEncode(parameter.Value)}");
                count++;
            }
            return tempUrl.ToString();
        }
    }
}
