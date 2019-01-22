using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System;
using System.Web;
using System.Collections.Generic;
using System.Net;

namespace MeisterCore
{
    public class Resource<REQ, RES>
    {
        public HttpStatusCode StatusCode { get; set; }
        private bool passed;
        private List<string> Errors { get; set; }
        private Parameters Parm = null;

        internal Meister Meister;
        public Resource(Uri uri)
        {
            Meister = Meister.Instance;
            Meister.Uri = uri;
            Parm = new Parameters();
        }

        public Resource(Uri uri, Parameters p)
        {
            Meister = Meister.Instance;
            Meister.Uri = uri;
            Parm = p;
        }

        /// <summary>
        /// Executing the call : This calls are reported back as IEnumerators so we need to rehydrate
        /// </summary>
        /// <typeparam name="P"></typeparam>
        /// <param name="call"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        public List<RES> Execute(string call, REQ p, bool async = true)
        {
            bool cpx = Parm.Compression == Parameters.Inbound ? true : false ;
            return ToList<RES>(DExecute(call, p, cpx, async));
        }

        /// <summary>
        /// JObject mode execute : This is a direct call with JObject as return
        /// </summary>
        /// <typeparam name="REQ"></typeparam>
        /// <param name="call"></param>
        /// <param name="p"></param>
        /// <param name="sync"></param>
        /// <returns></returns>
        public JArray JExecute(string call, REQ p, bool async = true)
        {
            return ToJarray(DExecute(call, p, async));
        }

        /// <summary>
        /// DObject mode execute : This is an indirect call receiving a dynamic ... 
        /// Also can be used for execution where the sender wishes to operate on a dynamic
        /// </summary>
        /// <typeparam name="REQ"></typeparam>
        /// <param name="call"></param>
        /// <param name="p"></param>
        /// <param name="async"></param>
        /// <returns></returns>
        public dynamic DExecute(string call, REQ p, bool async = true, bool cpx = false)
        {
            dynamic d = null;
            string parms = JsonConvert.SerializeObject(Parm);
            string payload = JsonConvert.SerializeObject(p);
            d = Meister.ExecuteCall(call, parms, payload,async, cpx);
            if (Meister.HttpResponseInValidRange(Meister.statusCode))
                return d;
            else
                throw new MeisterException(Enum.GetName(typeof(HttpStatusCode), Meister.statusCode), Meister.statusCode);
        }
        /// <summary>
        /// Errors found ...
        /// </summary>
        /// <returns></returns>
        public List<string> GetErrors()
        {
            return Errors;
        }

        /// <summary>
        /// passthrough authentication
        /// </summary>
        /// <param name="userid"></param>
        /// <param name="password"></param>
        public void Authenticate(String userid, Byte[] password)
        {
            Meister.Authenticate(userid, password);
        }

        /// <summary>
        /// To List
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="d"></param>
        /// <returns></returns>
        private List<T> ToList<T>(dynamic d)
        {
            List<T> list = new List<T>();
            list = JsonConvert.DeserializeObject<List<T>>(ToString(d),
                    new JsonSerializerSettings
                    {
                        Error = delegate (object sender, Newtonsoft.Json.Serialization.ErrorEventArgs args)
                        {
                            Errors.Add(args.ErrorContext.Error.Message);
                            args.ErrorContext.Handled = true;
                        },
                        Converters = { new XmlNodeConverter() }
                    });
            return list;
        } 

        /// <summary>
        /// Fetch the right container
        /// </summary>
        /// <param name="d"></param>
        /// <returns></returns>
        private String ToString(dynamic d)
        {
            if (Errors != null)
                Errors.Clear();
            passed = true;
            String json = string.Empty;
            try
            {
                dynamic j = d["d"].results[0];
                json = j["Json"];
            }
            catch (Exception ex)
            {
                throw new MeisterException("Serialization Error at Json return", ex);
            }
            if (passed)
            {
                if (Parm.Compression == Parameters.Outbound)
                    json = Meister.Unzip(json);
                json = Meister.Unescape(json);
            }
            return json;
        }
        /// <summary>
        /// To Jarray: returns the naive value of the first object that has meaning, ie., results
        /// </summary>
        /// <param name="d"></param>
        /// <returns></returns>
        private JArray ToJarray(dynamic d)
        {
            String json = ToString(d);
            JToken jt = JToken.Parse(json);
            JArray ja = jt as JArray;
            return ja;
        }
    }
}