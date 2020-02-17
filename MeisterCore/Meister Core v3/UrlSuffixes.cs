using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
namespace MeisterCore
{
    public static class UrlSuffixes
    {
        public static string AddUrlParams(this string url, Dictionary<string, string> parameters)
        {
            if (parameters == null || !parameters.Keys.Any())
                return url;
            var tempUrl = new StringBuilder($"{url}?");
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
