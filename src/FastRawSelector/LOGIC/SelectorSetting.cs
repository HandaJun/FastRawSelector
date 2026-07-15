using System;
using System.Collections.Generic;
using System.IO;
using YamlDotNet.Serialization;

namespace FastRawSelector.LOGIC
{
    [Serializable]
    public class SelectorSetting
    {
        private readonly object _sync = new object();

        /// <summary>
        /// 이 인스턴스가 저장/로드하는 YAML 경로. 직렬화 대상 아님.
        /// </summary>
        [YamlIgnore]
        public string FilePath { get; private set; }

        /// <summary>
        /// SelectedSet 접근·Save와 함께 사용할 락. 재진입 가능(Monitor).
        /// </summary>
        [YamlIgnore]
        public object SyncRoot => _sync;

        public HashSet<string> SelectedSet { get; set; } = new HashSet<string>();

        public static SelectorSetting Load(string path)
        {
            var filePath = Path.Combine(path, "FastRawSelector.yaml");

            if (!File.Exists(filePath))
                using (var fs = File.Create(filePath)) { }

            SelectorSetting setting = null;
            try
            {
                setting = Deserialize(filePath);
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
            }

            if (setting == null)
            {
                setting = new SelectorSetting();
                setting.FilePath = filePath;
                setting.Save(false);
            }
            else
            {
                setting.FilePath = filePath;
            }

            return setting;
        }

        /// <summary>
        /// 저장
        /// </summary>
        /// <param name="tabDataClearFlg"></param>
        public void Save(bool tabDataClearFlg = true)
        {
            if (string.IsNullOrEmpty(FilePath))
            {
                return;
            }

            lock (_sync)
            {
                Serialize(this, FilePath);
            }
        }

        /// <summary>
        /// 파일로 출력
        /// </summary>
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
