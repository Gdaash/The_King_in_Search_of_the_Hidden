using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace GameFoundation.Localization
{
    public sealed class GoogleSheetsLocalizationSync : MonoBehaviour
    {
        [SerializeField] private LocalizationTable table;
        [Tooltip("Published Google Sheet CSV URL. Expected columns: key, ru, en, ...")]
        [SerializeField] private string publishedCsvUrl;
        public IEnumerator ImportAllLanguages(Action<string> completed = null)
        {
            using var request = UnityWebRequest.Get(publishedCsvUrl);
            yield return request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success) { completed?.Invoke(request.error); yield break; }
            ImportCsv(request.downloadHandler.text); completed?.Invoke(null);
        }
        public string ExportCsv()
        {
            if (table == null) return string.Empty;
            var lines=new List<string>{"key,"+string.Join(",",table.languages)};
            foreach(var entry in table.entries){var row=new List<string>{Escape(entry.key)}; for(int i=0;i<table.languages.Count;i++) row.Add(Escape(i<entry.values.Count?entry.values[i]:string.Empty)); lines.Add(string.Join(",",row));}
            return string.Join("\n",lines);
        }
        public void ImportCsv(string csv)
        {
            if(table==null||string.IsNullOrWhiteSpace(csv))return; var rows=csv.Replace("\r","").Split('\n'); if(rows.Length<1)return;
            var header=Parse(rows[0]); if(header.Count<2||header[0]!="key")return; table.languages=header.GetRange(1,header.Count-1); table.entries.Clear();
            for(int i=1;i<rows.Length;i++){var cells=Parse(rows[i]); if(cells.Count==0||string.IsNullOrWhiteSpace(cells[0]))continue; var entry=new LocalizationTable.Entry{key=cells[0]}; for(int c=1;c<header.Count;c++)entry.values.Add(c<cells.Count?cells[c]:string.Empty); table.entries.Add(entry);} 
        }
        private static string Escape(string value)=>"\""+(value??string.Empty).Replace("\"","\"\"")+"\"";
        private static List<string> Parse(string row){var result=new List<string>();var value="";var quotes=false;for(int i=0;i<row.Length;i++){if(row[i]=='\"'){if(quotes&&i+1<row.Length&&row[i+1]=='\"'){value+='\"';i++;}else quotes=!quotes;}else if(row[i]==','&&!quotes){result.Add(value);value="";}else value+=row[i];}result.Add(value);return result;}
    }
}
