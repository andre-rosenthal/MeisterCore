using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MeisterCore
{
    /// <summary>
    /// Summary description for Meister Core Library Definistions - SIngleton pattern
    /// </summary>

    internal sealed class Meister : SingletonBase<Meister>
    {
        private const string MeisterHost = @"/sap/opu/odata/MEISTER/ENGINE/Execute?Endpoint=";
        private const string MeisterParms = "&Parms=";
        private const string MeisterPayload = "&Json=";
        private const string InJson = "&$format=json";
        private const string Root = "d";
        private const string Content = "Json";
        private const char quote = '"';
        private const string escaped = @"\""";
        private const string singleQuote = "'";
        private const string body = @"<body>";
        private const string http = @"HTTP";
        public enum Authentications
        {
            Basic,
            OAuth,
            X509,
            Kerberos
        }
        internal HttpStatusCode statusCode;
        public List<string> Errors { get; set; }
        private AuthenticationHeaderValue Credentials { get; set; }    
        public int WaitFactor { get; set; }
        public Uri Uri { get; set; }
        private HttpClient Client { get; set; }

        /// <summary>
        /// Ctor 
        /// </summary>
        private Meister()
        {
        }

        /// <summary>
        /// Ctor with Uri
        /// </summary>
        /// <param name="uri"></param>
        public Meister(Uri uri)
        {
            Uri = uri;
            WaitFactor = 90;
        }

        /// <summary>
        /// synch ctor
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="wait"></param>
        public Meister(Uri uri, int wait) : base()
        {
            if (wait > 0)
                WaitFactor = wait;
            Uri = uri;
        }

        /// <summary>
        /// add quote
        /// </summary>
        /// <returns></returns>
        public string Quote()
        {
            return quote.ToString();
        }

        /// <summary>
        /// Executes the call per se into Meister at SAP
        /// </summary>
        /// <param name="call"></param>
        /// <param name="parms"></param>
        /// <param name="json"></param>
        /// <param name="async"></param>
        /// <returns></returns>
        public dynamic ExecuteCall(string call, string parms, string json, bool async = true, bool cpx = false)
        {
            dynamic d = null;
            if (Credentials == null)
                throw new UnauthorizedAccessException();
            if (async)
            {
                Task<dynamic> t = GetAsync<dynamic>(call, parms, json, cpx);
                try
                {
                    t.Wait();
                }
                catch (AggregateException ae)
                {
                    foreach (var e in ae.Flatten().InnerExceptions)
                        if (e is MeisterException)
                            throw e;
                }
                d = t.Result;
            }
            else
            {
                Task t = new Task(() =>
                {
                    try
                    {
                        d = Get<dynamic>(call, parms, json, cpx);
                    }
                    catch (AggregateException ae)
                    {
                        foreach (var e in ae.Flatten().InnerExceptions)
                            if (e is MeisterException)
                                throw e;
                    }
                });
                try
                {       
                    t.RunSynchronously();
                }
                catch (AggregateException ae)
                {
                    foreach (var e in ae.Flatten().InnerExceptions)
                        if (e is MeisterException)
                            throw e;
                }
            }
            return d;
        }

        /// <summary>
        /// Get the specified pl, parms and json.
        /// </summary>
        /// <returns>The get.</returns>
        /// <param name="pl">Pl.</param>
        /// <param name="parms">Parms.</param>
        /// <param name="json">Json.</param>
        /// <typeparam name="T">The 1st type parameter.</typeparam>
        public T Get<T>(string pl, string parms, string json, bool cpx)
        {
            T t = default(T);
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = Credentials;
                string call = CreateCall(pl, parms, json, cpx);
                var response = client.GetAsync(call).Result;
                if (HttpResponseInValidRange(response.StatusCode))
                {
                    var responseContent = response.Content;
                    string result = responseContent.ReadAsStringAsync().Result;
                    try
                    {
                        t = JsonConvert.DeserializeObject<T>(result);
                    }
                    catch (Exception)
                    {
                        
                    }
                    finally
                    {
                        statusCode = response.StatusCode;
                    }
                }
                else
                    statusCode = response.StatusCode;
            }
            return t;
        }

        /// <summary>
        /// Check values of Response
        /// </summary>
        /// <param name="statusCode"></param>
        /// <returns></returns>
        public bool HttpResponseInValidRange(HttpStatusCode statusCode)
        {
            return (statusCode >= HttpStatusCode.OK && statusCode < HttpStatusCode.BadRequest);
        }
        /// <summary>
        /// Async Get the specified pl, parms and json.
        /// </summary>
        /// <returns>The async.</returns>
        /// <param name="ep">Ep.</param>
        /// <param name="parms">Parms.</param>
        /// <param name="json">Json.</param>
        /// <typeparam name="T">The 1st type parameter.</typeparam>
        public async Task<T> GetAsync<T>(string ep, string parms, string json, bool cpx)
        {
            try
            {
                T t = default(T);
                using (var client = new HttpClient())
                {
                    if (WaitFactor > 0)
                        client.Timeout = TimeSpan.FromMinutes(WaitFactor);
                    client.DefaultRequestHeaders.Authorization = Credentials;
                    string call = CreateCall(ep, parms, json, cpx);
                    using (HttpResponseMessage response = await client.GetAsync(call))
                        if (HttpResponseInValidRange(response.StatusCode))
                        {
                            using (HttpContent content = response.Content)
                            {
                                string result = await content.ReadAsStringAsync();
                                try
                                {
                                    t = JsonConvert.DeserializeObject<T>(result);
                                }
                                catch (Exception)
                                {
                                    
                                }
                                finally
                                {
                                    statusCode = response.StatusCode;
                                }
                            }
                        }
                        else
                            statusCode = response.StatusCode;
                }
                return t;
            }
            catch (MeisterException)
            {
                throw;
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Under OData 2.0 Mode
        /// </summary>
        /// <returns>The call.</returns>
        /// <param name="pl">Pl.</param>
        /// <param name="parms">Parms.</param>
        /// <param name="json">Json.</param>
        private string CreateCall(string pl, string parms, string json, bool cpx)
        {
            StringBuilder sb = new StringBuilder();
            if (cpx)
                json = Zip(json);
            sb.Append(Uri);
            sb.Append(MeisterHost);
            sb.Append(AddSingleQuotes(pl));
            sb.Append(MeisterParms);
            sb.Append(AddSingleQuotes(parms));
            sb.Append(MeisterPayload);
            sb.Append(AddSingleQuotes(json));
            sb.Append(InJson);
            return sb.ToString();
        }

        /// <summary>
        /// Adds the quotes.
        /// </summary>
        /// <returns>The quotes.</returns>
        /// <param name="s">S.</param>
        private string AddQuotes(string s)
        {
            return Quote() + s + Quote();
        }

        /// <summary>
        /// Adds the single quotes.
        /// </summary>
        /// <returns>The single quotes.</returns>
        /// <param name="s">S.</param>
        private string AddSingleQuotes(string s)
        {
            string q = singleQuote;
            return q + s + q;
        }

        /// <summary>
        /// To string under OData 2.0
        /// </summary>
        /// <returns>The string.</returns>
        /// <param name="d">D.</param>
        private string ToString(dynamic d)
        {
            string s = string.Empty;
            try
            {
                if (d == null)
                    return s;
                Type type = d?.GetType();
                if (type.IsArray == true)
                {
                    dynamic j = d[Root].results[0];
                    if (j != null)
                        s = j[Content];
                }
                else
                    s = d as string;
            }
            catch
            {
                throw new InvalidCastException();
            }
            return s;
        }

        /// <summary>
        /// Copies to.
        /// </summary>
        /// <param name="src">Source.</param>
        /// <param name="dest">Destination.</param>
        public static void CopyTo(Stream src, Stream dest)
        {
            byte[] bytes = new byte[4096];
            int cnt;
            while ((cnt = src.Read(bytes, 0, bytes.Length)) != 0)
                dest.Write(bytes, 0, cnt);
        }

        /// <summary>
        /// Bytes the array to string.
        /// </summary>
        /// <returns>The array to string.</returns>
        /// <param name="ba">Ba.</param>
        public static string ByteArrayToString(byte[] ba)
        {
            string s = string.Empty;
            foreach (var b in ba)
            {
                s += b.ToString();
            }
            return s;
        }

        /// <summary>
        /// Strings to byte array.
        /// </summary>
        /// <returns>The to byte array.</returns>
        /// <param name="st">St.</param>
        public static byte[] StringToByteArray(string st)
        {
            string h = st.Replace(System.Environment.NewLine, String.Empty);
            int NumberChars = h.Length / 2;
            byte[] bytes = new byte[NumberChars];
            using (var sr = new StringReader(h))
                for (int i = 0; i < NumberChars; i++)
                    bytes[i] = Convert.ToByte(new string(new char[2] { (char)sr.Read(), (char)sr.Read() }), 16);
            return bytes;
        }

        /// <summary>
        /// Zip the specified str.
        /// </summary>
        /// <returns>The zip.</returns>
        /// <param name="str">String.</param>
        public static string Zip(string str)
        {
            byte[] buffer = Encoding.ASCII.GetBytes(str);
            MemoryStream ms = new MemoryStream();
            using (GZipStream zip = new GZipStream(ms, CompressionMode.Compress, true))
            {
                zip.Write(buffer, 0, buffer.Length);
            }
            ms.Position = 0;
            byte[] ba = new byte[ms.Length];
            ms.Read(ba, 0, ba.Length);
            byte[] bytes = new byte[ba.Length / 2];
            return ShowByteValues(ba, ba.Length);
        }

        private static string ShowByteValues(byte[] bytes, int last)
        {
            string s = string.Empty;
            for (int ctr = 0; ctr <= last - 1; ctr++)
            {
                s += String.Format("{0:X2}", bytes[ctr]);
            }
            return s;
        }
        /// <summary>
        /// Froms the list.
        /// </summary>
        /// <returns>The list.</returns>
        /// <param name="l">L.</param>
        /// <typeparam name="T">The 1st type parameter.</typeparam>
        public string FromList<T>(List<T> l)
        {
            return JsonConvert.SerializeObject(l,
                    new JsonSerializerSettings
                    {
                        Error = delegate (object sender, Newtonsoft.Json.Serialization.ErrorEventArgs args)
                        {
                            Errors.Add(args.ErrorContext.Error.Message);
                            args.ErrorContext.Handled = true;
                        },
                        Converters = { new ExpandoObjectConverter() }
                    });
        }
        /// <summary>
        /// From String
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="s"></param>
        /// <returns></returns>
        public List<T> FromString<T>(string s)
        {
            string json = Unescape(s);
            List<T> list = new List<T>();
            try
            {
                list = JsonConvert.DeserializeObject<List<T>>(json);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return list;
        }

        /// <summary>
        /// Unescape
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public string Unescape(string s)
        {
            while (s.Contains(Meister.escaped))
                s = s.Replace(escaped, Quote());
            string json = s.Replace(Quote() + "[", "[");
            json = json.Replace("]" + Quote(), "]");
            return json;
        }

        /// <summary>
        /// Froms the list.
        /// </summary>
        /// <returns>The list.</returns>
        /// <param name="d">D.</param>
        /// <typeparam name="T">The 1st type parameter.</typeparam>
        public string FromList<T>(Dictionary<string, string> d)
        {
            return JsonConvert.SerializeObject(d);
        }

        /// <summary>
        /// Authenticate the specified userid and password.
        /// </summary>
        /// <param name="userid">Userid.</param>
        /// <param name="password">Password.</param>
        public void Authenticate(String userid, Byte[] password, Authentications au = Authentications.Basic)
        {
            string uid = string.Empty;
            string psw = string.Empty;
            uid = userid;
            psw = System.Text.Encoding.Default.GetString(password);
            var uap = $"{uid}:{psw}";
            var byteArray = Encoding.Default.GetBytes(uap);
            switch (au)
            {
                case Authentications.Basic:
                    Credentials = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
                    break;
                case Authentications.OAuth:
                    break;
                case Authentications.X509:
                    break;
                case Authentications.Kerberos:
                    break;
                default:
                    break;
            }           
        }

        /// <summary>
        /// Unzip the specified st.
        /// </summary>
        /// <returns>The unzip.</returns>
        /// <param name="st">St.</param>
        public static string Unzip(string st)
        {
            var bytes = StringToByteArray(st);
            using (var msi = new MemoryStream(bytes))
            using (var mso = new MemoryStream())
            {
                using (var gs = new GZipStream(msi, CompressionMode.Decompress))
                {
                    CopyTo(gs, mso);
                }
                return Encoding.Default.GetString(mso.ToArray(), 0, mso.ToArray().Length);
            }
        }
    }

    /// <summary>
    /// extending Json
    /// </summary>
    internal static class SerializeList
    {
        /// <summary>
        /// ToJson
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="self"></param>
        /// <returns></returns>
        public static string ToJson<T>(this List<T> self) => JsonConvert.SerializeObject(self, Converter.Settings);
    }

    /// <summary>
    /// generic converter
    /// </summary>
    internal class Converter
    {
        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
            Converters = { new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal } },
        };
    }

    /// <summary>
    /// Converter to be extended by applications
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Converter<T> : JsonConverter where T : Enum
    {
        /// <summary>
        /// Can Convert ?
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public override bool CanConvert(Type t) => t == typeof(T);

        /// <summary>
        /// Reads the Json
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="t"></param>
        /// <param name="existingValue"></param>
        /// <param name="serializer"></param>
        /// <returns></returns>
        public override object ReadJson(JsonReader reader, Type t, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;
            var settings = new JsonSerializerSettings();
            var value = serializer.Deserialize<T>(reader);
            JsonConvert.DefaultSettings = (() =>
            {
                settings.Converters.Add(new StringEnumConverter { CamelCaseText = true });
                return settings;
            });
            return settings;
        }

        /// <summary>
        /// Writes the json
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="untypedValue"></param>
        /// <param name="serializer"></param>
        public override void WriteJson(JsonWriter writer, object untypedValue, JsonSerializer serializer)
        {
            if (untypedValue == null)
            {
                serializer.Serialize(writer, null);
                return;
            }
            var value = (T)untypedValue;
            serializer.Serialize(writer, value);
            throw new MeisterException("Cannot marshal type");
        }
    }
}

