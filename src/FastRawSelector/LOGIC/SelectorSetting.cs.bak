using System;
using System.Collections.Generic;
using System.IO;
using YamlDotNet.Serialization;

namespace FastRawSelector.LOGIC
{
    [Serializable]
    public class SelectorSetting
    {
        public static string Location = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "FastRawSelector.yaml");
        public HashSet<string> SelectedSet { get; set; } = new HashSet<string>();
        public static SelectorSetting Load(string path)
        {

            Location = Path.Combine(path, "FastRawSelector.yaml");

            if (!File.Exists(Location))
                using (var fs = File.Create(Location)) { }

            SelectorSetting setting = null;
            try
            {
                setting = Deserialize(Location);
            }
            catch (Exception)
            {
            }

            if (setting == null)
            {
                setting = new SelectorSetting();
                setting.Save(false);
            }

            return setting;
        }

        /// <summary>
        /// 저장
        /// </summary>
        /// <param name="tabDataClearFlg"></param>
        public void Save(bool tabDataClearFlg = true)
        {
            Serialize(this, Location);
        }

        /// <summary>
        /// 파일로 출력
        /// </summary>
        /// <param name="setting"></param>
        /// <param name="path"></param>
        private static void Serialize(SelectorSetting setting, string path)
        {
            var serializer = new SerializerBuilder().Build();
            var yml = serializer.Serialize(setting);
            using (var sr = new StreamWriter(path))
            {
                sr.Write(yml);
            }
        }

        /// <summary>
        /// 파일에서 불러오기
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private static SelectorSetting Deserialize(string path)
        {
            using (var sr = new StreamReader(path))
            {
                using (var input = new StringReader(sr.ReadToEnd()))
                {
                    var deserializer = new DeserializerBuilder().IgnoreUnmatchedProperties().Build();
                    return deserializer.Deserialize<SelectorSetting>(input);
                }
            }
        }


    }
}
