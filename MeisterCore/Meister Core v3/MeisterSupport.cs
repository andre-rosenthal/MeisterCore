﻿using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
namespace MeisterCore.Support
{
    /// <summary>
    /// Support for vanilla ref result set
    /// </summary>
    [JsonObject("VanillaResponse")]
    public partial class VanillaResponse<RES>
    {
        [JsonProperty("type")]
        public string type { get; set; }
        [JsonProperty("val")]
        public List<RES> res { get; set; }
    }
    /// <summary>
    /// Non parametrized Vanilla ref result set with simple string
    /// </summary>
    public partial class VanillaResponse
    {
        [JsonProperty("type")]
        public string type { get; set; }
        [JsonProperty("val")]
        public string res { get; set; }
    }
    /// <summary>
    /// Post class support - pre call
    /// </summary>
    /// <typeparam name="REQ"></typeparam>
    internal class Body<REQ>
    {
        public string Endpoint { get; set; }
        public Parameters Parms { get; set; }
        public REQ Json { get; set; }
        public Body(string endpoint, Parameters parms, REQ json)
        {
            Endpoint = endpoint;
            Parms = parms;
            Json = json;
        }
    }
    /// <summary>
    /// Post class support - at call
    /// </summary>
    /// <typeparam name="REQ"></typeparam>
    internal class BodyCall
    {
        public string Endpoint { get; set; }
        public string Parms { get; set; }
        public string Json { get; set; }
        public BodyCall()
        {
            Endpoint = string.Empty;
            Parms = string.Empty;
            Json = string.Empty;
        }
    }
    /// <summary>
    /// OData v4 support class
    /// </summary>
    [JsonObject("OD4Body")]
    public class OD4Body<RES>
    {
        [JsonProperty("@odata.context")]
        public string odatacontext { get; set; }
        [JsonProperty("value")]
        public string value { get; set; }
    }
    /// <summary>
    /// OData v2 class support
    /// </summary>
    /// <typeparam name="REQ"></typeparam>
    internal class OD2Body<REQ>
    {
        public D d { get; set; }
    }
    /// <summary>
    /// Node D of OData v2 payload
    /// </summary>
    internal class D
    {
        public List<ResultOD2> results { get; set; }
    }
    /// <summary>
    /// Node Result of OData v2 payload
    /// </summary>
    internal class ResultOD2
    {
        public __Metadata __metadata { get; set; }
        public string Endpoint { get; set; }
        public string Parms { get; set; }
        public string Json { get; set; }
    }
    /// <summary>
    /// Node __Metadata of OData v2 payload
    /// </summary>
    internal class __Metadata
    {
        public string id { get; set; }
        public string uri { get; set; }
        public string type { get; set; }
    }
    /// <summary>
    /// Support extensions ...
    /// </summary>
    public class SkipEmptyCollectionsContractResolver : DefaultContractResolver
    {
        public SkipEmptyCollectionsContractResolver(bool shareCache = false) : base() { }
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            JsonProperty property = base.CreateProperty(member, memberSerialization);
            if (property.PropertyType == typeof(string))
            {
                property.ShouldSerialize = instance =>
                {
                    return !string.IsNullOrWhiteSpace(instance.GetType().GetProperty(member.Name).GetValue(instance, null) as string);
                };
            }
            else if (property.PropertyType == typeof(DateTime))
            {
                property.ShouldSerialize = instance =>
                {
                    return Convert.ToDateTime(instance.GetType().GetProperty(member.Name).GetValue(instance, null)) != default(DateTime);
                };
            }
            else if (member != null && typeof(IEnumerable).IsAssignableFrom(property.PropertyType))
            {
                switch (member.MemberType)
                {
                    case MemberTypes.Property:
                        property.ShouldSerialize = instance =>
                        {
                            var enumerable = instance.GetType().GetProperty(member.Name).GetValue(instance, null) as IEnumerable;
                            return enumerable != null ? enumerable.GetEnumerator().MoveNext() : false;
                        };
                        break;
                    case MemberTypes.Field:
                        property.ShouldSerialize = instance =>
                        {
                            var enumerable = instance.GetType().GetField(member.Name).GetValue(instance) as IEnumerable;
                            return enumerable != null ? enumerable.GetEnumerator().MoveNext() : false;
                        };
                        break;
                }
            }
            return property;
        }
    }
    /// <summary>
    /// Generic support
    /// </summary>
    public static class MeisterSupport
    {
        static private string key = @"TWVpc3RlciBJZGVudGl0eSBEYXRhYmFzZSBTZWNyZXQgQ29kZSBpcyA6IE1laXN0ZXJ2Mg==";
        [Flags]
        /// <summary>
        /// SAP languages
        /// </summary>
        public enum Languages
        {
            CultureBased,
            AR,
            BG,
            CS,
            DA,
            DE,
            EL,
            EN,
            ES,
            FI,
            FR,
            HE,
            HI,
            HR,
            HU,
            IT,
            JA,
            KO,
            NL,
            NO,
            PL,
            PT,
            RO,
            RU,
            SH,
            SK,
            SL,
            SV,
            TH,
            TR,
            Z1,
            ZF,
            ZH
        }
        /// <summary>
        /// Protocols
        /// </summary>
        [Flags]
        public enum Protocols
        {
            ODataV2 = 0,
            ODataV4 = 1
        }
        /// <summary>
        /// Extensions 
        /// </summary>
        [Flags]
        public enum MeisterExtensions
        {
            NoPurification = 0,
            RemoveNulls = 1,
            RemoveNullsAndEmptyArrays = 2
        }
        /// <summary>
        /// Authentication types ...
        /// </summary>
        [Flags]
        public enum AuthenticationModes
        {
            Basic = 0,
            OAuth = 1,
            JWT = 2,
            SAML2 = 3
        }
        /// <summary>
        /// Meister options
        /// </summary>
        [Flags]
        public enum MeisterOptions
        {
            None = 0,
            CompressionsInbound = 1,
            CompressionsOutbound = 2,
            TestRun = 4,
            UseODataV4 = 16,
            AsyncMode = 32,
            QuequeMode = 64,
            UseCallback = 128,
            UseDelegate = 256,
            UseWebSocket = 512,
        }
        /// <summary>
        /// Runtime options 
        /// </summary>
        [Flags]
        public enum RuntimeOptions
        {
            ExecuteSync = 0,
            ExecuteAsync = 1,
            ExecuteDelegate = 2,
        }
        public const string AbapTrue = "X";
        public const string val = "val";
        private const char quote = '"';
        public static string Quote => quote.ToString();
        #region Support calls ...
        /// <summary>
        /// Append single quote
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public static string InQuotes(string json)
        {
            return "'" + json + "'";
        }
        /// <summary>
        /// To SecureString
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static SecureString ToSecureString(string s)
        {
            if (!string.IsNullOrEmpty(s))
            {
                SecureString sec = new SecureString();
                s.ToCharArray().ToList().ForEach(sec.AppendChar);
                sec.MakeReadOnly();
                return sec;
            }
            else
                return null;
        }
        /// <summary>
        /// Convert from secure string ...
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string ToUnSecureString(SecureString s)
        {
            IntPtr pointer = IntPtr.Zero;
            try
            {
                pointer = Marshal.SecureStringToGlobalAllocUnicode(s);
                return Marshal.PtrToStringUni(pointer);
            }
            finally
            {
                if (pointer != IntPtr.Zero)
                    Marshal.ZeroFreeGlobalAllocUnicode(pointer);
            }
        }
        /// <summary>
        /// Remove dref from Meister's dref parser
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public static string RemoveDrefAnnotations(string dref)
        {
            if (!string.IsNullOrEmpty(dref))
            {
                string json = dref.Replace("%type", "type");
                json = json.Replace("%val", "val");
                return json;
            }
            else
                return string.Empty;
        }
        /// <summary>
        /// Copies to.
        /// </summary>
        /// <param name="src">Source.</param>
        /// <param name="dest">Destination.</param>
        public static void CopyTo(Stream src, Stream dest)
        {
            if (src != null && dest != null)
            {
                byte[] bytes = new byte[4096];
                int cnt;
                while ((cnt = src.Read(bytes, 0, bytes.Length)) != 0)
                    dest.Write(bytes, 0, cnt);
            }
        }
        /// <summary>
        /// Bytes the array to string as ASCII - for all others use System.Text.Encoding.Unicode.GetString(ba)
        /// </summary>
        /// <returns>The array to string.</returns>
        /// <param name="ba">Ba.</param>
        public static string ByteArrayToString(byte[] ba)
        {
            string s = string.Empty;
            if (ba != null)
                foreach (var b in ba)
                    s += b.ToString();
            return s;
        }
        /// <summary>
        /// Strings to byte array as Unicode representation - for all others use System.Text.Encoding.Unicode.GetBytes(st)
        /// </summary>
        /// <returns>The to byte array.</returns>
        /// <param name="st">St.</param>
        public static byte[] StringToByteArray(string st)
        {
            if (!string.IsNullOrEmpty(st))
            {
                string h = st.Replace(System.Environment.NewLine, String.Empty);
                int NumberChars = h.Length / 2;
                byte[] bytes = new byte[NumberChars];
                using (var sr = new StringReader(h))
                    for (int i = 0; i < NumberChars; i++)
                        bytes[i] = Convert.ToByte(new string(new char[2] { (char)sr.Read(), (char)sr.Read() }), 16);
                return bytes;
            }
            return null;
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
        /// <summary>
        /// Show byte values
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="last"></param>
        /// <returns></returns>
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
        /// <summary>
        /// Encrypt
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static string DoEncrypt(string data)
        {
            string encData = null;
            byte[][] keys = GetHashKeys(key);
            try
            {
                encData = EncryptStringToBytes_Aes(data, keys[0], keys[1]);
            }
            catch (CryptographicException) { }
            catch (ArgumentNullException) { }
            return encData;
        }
        /// <summary>
        /// Decrypt
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        static public string DoDecrypt(string data)
        {
            string decData = null;
            byte[][] keys = GetHashKeys(key);
            try
            {
                decData = DecryptStringFromBytes_Aes(data, keys[0], keys[1]);
            }
            catch (CryptographicException) { }
            catch (ArgumentNullException) { }
            return decData;
        }
        /// <summary>
        /// GatHashKeys
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        static private byte[][] GetHashKeys(string key)
        {
            byte[][] result = new byte[2][];
            Encoding enc = Encoding.UTF8;
            SHA256 sha2 = new SHA256CryptoServiceProvider();
            byte[] rawKey = enc.GetBytes(key);
            byte[] rawba = enc.GetBytes(key);
            byte[] hashKey = sha2.ComputeHash(rawKey);
            byte[] hashba = sha2.ComputeHash(rawba);
            Array.Resize(ref hashba, 16);
            result[0] = hashKey;
            result[1] = hashba;
            return result;
        }
        /// <summary>
        /// EncryptStringToBytes_Aes
        /// Courtesy of https://msdn.microsoft.com/de-de/library/system.security.cryptography.aes(v=vs.110).aspx
        /// </summary>
        /// <param name="plainText"></param>
        /// <param name="key"></param>
        /// <param name="ba"></param>
        /// <returns></returns>
        private static string EncryptStringToBytes_Aes(string plainText, byte[] key, byte[] ba)
        {
            if (plainText == null || plainText.Length <= 0)
                throw new ArgumentNullException("plainText");
            if (key == null || key.Length <= 0)
                throw new ArgumentNullException("key");
            if (ba == null || ba.Length <= 0)
                throw new ArgumentNullException("ba");
            byte[] encrypted;
            using (AesManaged aesAlg = new AesManaged())
            {
                aesAlg.Key = key;
                aesAlg.IV = ba;
                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);
                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {
                            swEncrypt.Write(plainText);
                        }
                        encrypted = msEncrypt.ToArray();
                    }
                }
            }
            return Convert.ToBase64String(encrypted);
        }
        /// <summary>
        /// DecryptStringFromBytes_Aes
        /// Courtesy of https://msdn.microsoft.com/de-de/library/system.security.cryptography.aes(v=vs.110).aspx
        /// </summary>
        /// <param name="cipherTextString"></param>
        /// <param name="key"></param>
        /// <param name="ba"></param>
        /// <returns></returns>
        private static string DecryptStringFromBytes_Aes(string cipherTextString, byte[] key, byte[] ba)
        {
            byte[] cipherText = Convert.FromBase64String(cipherTextString);
            if (cipherText == null || cipherText.Length <= 0)
                throw new ArgumentNullException("cipherText");
            if (key == null || key.Length <= 0)
                throw new ArgumentNullException("key");
            if (ba == null || ba.Length <= 0)
                throw new ArgumentNullException("ba");
            string plaintext = null;
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = key;
                aesAlg.IV = ba;
                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);
                using (MemoryStream msDecrypt = new MemoryStream(cipherText))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {
                            plaintext = srDecrypt.ReadToEnd();
                        }
                    }
                }
            }
            return plaintext;
        }
        #endregion
    }
}
