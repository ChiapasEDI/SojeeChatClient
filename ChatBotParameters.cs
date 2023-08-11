using System.Text.Json;
using System.Xml.Serialization;

namespace SojeeChat
{
    [Serializable]
    public class ChatBotParameters
    {
        // Model Specific prompts
        public string questionPrompt { get; set; } = "Q: ";
        public string answerPrompt { get; set; } = "A: ";
        public string emptyResponse { get; set; } = "";
        
        public List<string> topics { get; set; } = new List<string>();

        [NonSerialized]
        public Dictionary<string, string> dctTopics = new Dictionary<string, string>();

        public string basePath { get; set; } = "";

        public static string ConfigPath
        {
            get
            {
                return Path.Join(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "config.json");
            }
        }

        public void SaveConfig()
        {
            // Convert AppConfig to JSON and save to local storage
            var configJson = JsonSerializer.Serialize(this);
            File.WriteAllText(ConfigPath, configJson);
        }

        public void Regen()
        {
            dctTopics.Clear();
            foreach (var topic in topics)
            {
                if (!string.IsNullOrEmpty(topic))
                {
                    string topicFile = Path.Join(Path.GetDirectoryName(ConfigPath), "prompt_" + topic + ".txt");
                    if (File.Exists(topicFile))
                        dctTopics[topic] = File.ReadAllText(topicFile);
                }
            }
        }

        public static ChatBotParameters LoadFromDisk()
        {
            string storedConfigJson = "";

            // Load AppConfig from local storage
            if (File.Exists(ConfigPath))
                storedConfigJson = File.ReadAllText(ConfigPath);

            if (!string.IsNullOrEmpty(storedConfigJson))
            {
                var x =  JsonSerializer.Deserialize<ChatBotParameters>(storedConfigJson);
                x.Regen();
                return x;
            }
            else
            {
                // Initialize with default values if not found in local storage
                return new ChatBotParameters();
            }
        }
    }
}
