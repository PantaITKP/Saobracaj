using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Saobracaj.Agent.CimProvera.Model
{
    /// <summary>Zajednička JSON podešavanja (snake_case, kao u šemi koju dobija Claude).</summary>
    internal static class Json
    {
        public static readonly NamingStrategy Imena = new SnakeCaseNamingStrategy();

        public static readonly JsonSerializerSettings Postavke = new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver { NamingStrategy = Imena },
            NullValueHandling = NullValueHandling.Ignore,
            MissingMemberHandling = MissingMemberHandling.Ignore,
            Formatting = Formatting.Indented,
        };

        public static string U(object o) => JsonConvert.SerializeObject(o, Postavke);
        public static T Iz<T>(string s) => JsonConvert.DeserializeObject<T>(s, Postavke);
    }

    /// <summary>
    /// JSON šema za structured output, izvedena iz C# modela. Sva polja su obavezna, bez null i bez unija tipova,
    /// additionalProperties = false (uslov za strukturirani izlaz).
    /// </summary>
    internal static class Sema
    {
        public static JObject Za<T>() => Objekat(typeof(T));

        private static JObject Tip(Type t, OpisAttribute opis)
        {
            JObject s;
            if (t == typeof(string))
            {
                s = new JObject { ["type"] = "string" };
                if (opis?.Vrednosti != null) s["enum"] = new JArray(opis.Vrednosti.Cast<object>().ToArray());
            }
            else if (t == typeof(bool)) s = new JObject { ["type"] = "boolean" };
            else if (t == typeof(int) || t == typeof(long)) s = new JObject { ["type"] = "integer" };
            else if (t == typeof(double) || t == typeof(decimal)) s = new JObject { ["type"] = "number" };
            else if (t.IsGenericType && typeof(IEnumerable).IsAssignableFrom(t))
                s = new JObject { ["type"] = "array", ["items"] = Tip(t.GetGenericArguments()[0], null) };
            else if (t.IsClass) s = Objekat(t);
            else throw new NotSupportedException("Tip nije podržan u šemi: " + t);

            if (!string.IsNullOrEmpty(opis?.Tekst)) s["description"] = opis.Tekst;
            return s;
        }

        private static JObject Objekat(Type t)
        {
            var props = new JObject();
            var req = new JArray();
            foreach (var p in Svojstva(t))
            {
                var ime = Json.Imena.GetPropertyName(p.Name, false);
                props[ime] = Tip(p.PropertyType, p.GetCustomAttribute<OpisAttribute>());
                req.Add(ime);
            }
            var s = new JObject
            {
                ["type"] = "object",
                ["properties"] = props,
                ["required"] = req,
                ["additionalProperties"] = false,
            };
            var opis = t.GetCustomAttribute<OpisAttribute>();
            if (opis != null) s["description"] = opis.Tekst;
            return s;
        }

        /// <summary>Svojstva koja popunjava Claude: javna, sa setter-om, bez [Interni] i [JsonIgnore]. Bazna klasa prva.</summary>
        internal static IEnumerable<PropertyInfo> Svojstva(Type t)
        {
            var lanac = new List<Type>();
            for (var x = t; x != null && x != typeof(object); x = x.BaseType) lanac.Insert(0, x);
            foreach (var x in lanac)
                foreach (var p in x.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                {
                    if (p.SetMethod == null || !p.SetMethod.IsPublic) continue;
                    if (p.GetCustomAttribute<InterniAttribute>() != null) continue;
                    if (p.GetCustomAttribute<JsonIgnoreAttribute>() != null) continue;
                    yield return p;
                }
        }
    }
}
