using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace GameFoundation.Localization
{
    /// <summary>Source-text lookup for legacy content, backed by the same editable localization table.</summary>
    public static class GameText
    {
        static LocalizationTable cachedTable;
        static string cachedLanguage;
        static readonly Dictionary<string,string> english=new(StringComparer.Ordinal);
        static readonly Dictionary<string,string> russian=new(StringComparer.Ordinal);
        static readonly Dictionary<string,string> formattedSources=new(StringComparer.Ordinal);
        static Regex sourcePattern, translatedPattern;
        static void Prepare()
        {
            var table=LocalizationService.Instance?LocalizationService.Instance.Table:null;
            string language=LocalizationService.Instance?.Language??"ru";
            if(table==cachedTable&&language==cachedLanguage)return;
            cachedTable=table;cachedLanguage=language;english.Clear();russian.Clear();formattedSources.Clear();
            sourcePattern=translatedPattern=null;if(!table)return;
            int ru=table.languages.IndexOf("ru"),en=table.languages.IndexOf(language);
            foreach(var entry in table.entries)
            {
                if(ru<0||en<0||entry.values.Count<=Math.Max(ru,en))continue;
                string a=entry.values[ru],b=entry.values[en];if(string.IsNullOrEmpty(a)||string.IsNullOrEmpty(b))continue;
                if(a!=b)english.TryAdd(a,b);
                foreach(string translated in entry.values)
                    if(!string.IsNullOrEmpty(translated)&&translated!=a)russian.TryAdd(translated,a);
            }
            sourcePattern=Pattern(english.Keys);translatedPattern=Pattern(russian.Keys);
        }
        static Regex Pattern(IEnumerable<string> values)
        {
            var patterns=values.Where(v=>!v.Contains("{0}")).OrderByDescending(v=>v.Length).Select(v=>Regex.Escape(v)).ToArray();
            if (patterns.Length == 0) return null;
            return new Regex(@"(?<![\p{L}])(?:"+string.Join("|",patterns)+@")(?![\p{L}])",RegexOptions.CultureInvariant);
        }
        public static string Source(string value)
        {
            if(string.IsNullOrEmpty(value))return value;Prepare();
            if(formattedSources.TryGetValue(value,out var original))return original;
            if(russian.TryGetValue(value,out original))return original;
            return translatedPattern==null?value:translatedPattern.Replace(value,m=>russian[m.Value]);
        }
        public static string Get(string source)
        {
            if(string.IsNullOrEmpty(source))return source;Prepare();
            if(LocalizationService.Instance?.Language=="ru")return source;
            if(english.TryGetValue(source,out var result))return result;
            return sourcePattern==null?source:sourcePattern.Replace(source,m=>english[m.Value]);
        }
        public static string Format(string source,params object[] values)
        {
            string original=string.Format(CultureInfo.InvariantCulture,source,values);
            string result=string.Format(CultureInfo.InvariantCulture,Get(source),values);
            if(result!=original){if(formattedSources.Count>2048)formattedSources.Clear();formattedSources[result]=original;}
            return result;
        }
    }
}
