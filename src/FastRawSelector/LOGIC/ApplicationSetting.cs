using System;
using System.IO;
using YamlDotNet.Serialization;

namespace FastRawSelector.LOGIC
{
    [Serializable]
    public class ApplicationSetting
    {
        public static readonly string Location = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "FastRawSelectorSetting.yaml");
        public bool AllowNotRawImage { get; set; } = false;
        public bool IsExifVisible { get; set; } = false;
        public static ApplicationSetting Load()
        {
            if (!File.Exists(Location))
                using (var fs = File.Create(Location)) { }

            ApplicationSetting setting = null;
            try
            {
                setting = Deserialize(Location);
            }
            catch (Exception)
            {
            }

            if (setting == null)
            {
                setting = new ApplicationSetting();
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
        private static void Serialize(ApplicationSetting setting, string path)
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
        private static ApplicationSetting Deserialize(string path)
        {
            using (var sr = new StreamReader(path))
            {
                using (var input = new StringReader(sr.ReadToEnd()))
                {
                    var deserializer = new DeserializerBuilder().IgnoreUnmatchedProperties().Build();
                    return deserializer.Deserialize<ApplicationSetting>(input);
                }
            }
        }


    }
}
