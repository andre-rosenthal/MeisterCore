using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Core
{
    public abstract class MeisterCoreCalls<T> : IEquatable<T> where T : MeisterCoreCalls<T>
    {
        public string Value { get; }

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="value"></param>
        protected MeisterCoreCalls(string value) => this.Value = value;

        /// <summary>
        /// ToString
        /// </summary>
        /// <returns></returns>
        public override string ToString() => this.Value;

        /// <summary>
        /// No remorse parse
        /// </summary>
        /// <returns></returns>
        public static List<T> AsList()
        {
            return typeof(T)
                .GetProperties(BindingFlags.Public | BindingFlags.Static)
                .Where(p => p.PropertyType == typeof(T))
                .Select(p => (T)p.GetValue(null))
                .ToList();
        }

        public static T Parse(string value)
        {
            List<T> all = AsList();

            if (!all.Any(a => a.Value == value))
                throw new InvalidOperationException($"\"{value}\" is not a valid value for the type {typeof(T).Name}");
            return all.Single(a => a.Value == value);
        }

        /// <summary>
        /// Equal To 
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(T other)
        {
            if (other == null) return false;
            return this.Value == other?.Value;
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            else if (obj is T other) return this.Equals(other);
                else return false;
        }

        /// <summary>
        /// Hash
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode() => this.Value.GetHashCode();

        /// <summary>
        /// Equal operator
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool operator ==(MeisterCoreCalls<T> a, MeisterCoreCalls<T> b) => a?.Equals(b) ?? false;

        /// <summary>
        /// Not equal operator
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool operator !=(MeisterCoreCalls<T> a, MeisterCoreCalls<T> b) => !(a?.Equals(b) ?? false);

#pragma warning disable CS0693 // Type parameter has the same name as the type parameter from outer type
        /// <summary>
        /// Conversion Logic
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public class JsonConverter<T> : JsonConverter where T : MeisterCoreCalls<T>
#pragma warning restore CS0693 // Type parameter has the same name as the type parameter from outer type
        {
            /// <summary>
            /// Check read ability
            /// </summary>
            public override bool CanRead => true;
            /// <summary>
            /// Check write ability
            /// </summary>
            public override bool CanWrite => true;

            /// <summary>
            /// CanConvert
            /// </summary>
            /// <param name="objectType"></param>
            /// <returns></returns>
            public override bool CanConvert(Type objectType) => ImplementsGeneric(objectType, typeof(MeisterCoreCalls<>));

            /// <summary>
            /// Generic implementation
            /// </summary>
            /// <param name="type"></param>
            /// <param name="generic"></param>
            /// <returns></returns>
            private static bool ImplementsGeneric(Type type, Type generic)
            {
                while (type != null)
                {
                    if (type.IsGenericType && type.GetGenericTypeDefinition() == generic)
                        return true;
                    type = type.BaseType;
                }
                return false;
            }

            /// <summary>
            /// The read json
            /// </summary>
            /// <param name="reader"></param>
            /// <param name="objectType"></param>
            /// <param name="existingValue"></param>
            /// <param name="serializer"></param>
            /// <returns></returns>
            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                JToken item = JToken.Load(reader);
                string value = item.Value<string>();
                return MeisterCoreCalls<T>.Parse(value);
            }
            
            /// <summary>
            /// Write the Json
            /// </summary>
            /// <param name="writer"></param>
            /// <param name="value"></param>
            /// <param name="serializer"></param>
            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                if (value is MeisterCoreCalls<T> v)
                    JToken.FromObject(v.Value).WriteTo(writer);
            }
        }
    }
}
