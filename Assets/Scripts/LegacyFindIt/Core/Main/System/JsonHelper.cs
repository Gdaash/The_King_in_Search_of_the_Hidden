using System.IO;
using UnityEngine;

namespace DeskCat.FindIt.Scripts.Core.Main.System
{
    public static class JsonHelper
    {
        public static string SaveToJson<T>(T objectToSave, string fileName)
        {
            string json = JsonUtility.ToJson(objectToSave, true);
            string filePath = Path.Combine(GetPath(), fileName);
            File.WriteAllText(filePath, json);
            return json;
        }

        public static T LoadFromJson<T>(string fileName) where T : new()
        {
            string filePath = Path.Combine(GetPath(),fileName);
            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                return JsonUtility.FromJson<T>(json);
            }

            return new T();
        }

        private static string GetPath()
        {
            string path = Path.Combine(Application.dataPath,"Data");
            if(!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            return path; 
        }
    }
}