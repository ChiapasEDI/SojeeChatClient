using Newtonsoft.Json;
using System.Text;
using System.Text.Json;
using System.Xml.Serialization;

namespace SojeeChat
{
    [Serializable]
    public class AIInferenceProfile
    {
        public string? prompt;
        public int max_new_tokens = 650;
        public string? preset = "";
        public bool do_sample = true;
        public double temperature = .001;
        public double top_p = .1;
        public int top_k = 40;
        public double typical_p = 1;
        public double epsilon_cutoff = 0;
        public double eta_cutoff = 0;
        public double tfs = 1;
        public double top_a = 0;
        public double repetition_penalty = 1.18;
        public int repetition_penalty_range = 0;
        public int min_length = 0;
        public int no_repeat_ngram_size = 0;
        public int num_beams = 1;
        public int penalty_alpha = 0 ;
        public int length_penalty = 1;
        public bool early_stopping = false;
        public int mirostat_mode = 0;
        public double mirostat_tau = 5;
        public double mirostat_eta = 0.1;
        public int seed = -1;
        public bool add_bos_token = true;
        public int truncation_length = 8192;
        public bool ban_eos_token = false;
        public bool skip_special_tokens = true;
        public string[] stopping_strings;

        [NonSerialized]
        public string? topicText;

        public AIInferenceProfile(string topic)
        {
            StringBuilder bldr = new StringBuilder();
            bool mode = false;
            string[] lines = File.ReadAllLines(Path.Join(Path.GetDirectoryName(ChatBotParameters.ConfigPath), $"prompt_{topic.ToLower()}.txt"));
            foreach (string line in lines)
            {
                if (!mode)
                {
                    if (line.StartsWith("-"))
                        mode = true;
                    else
                    {
                        string[] lineSplit = line.Split(':');
                        string var = lineSplit[0].ToLower();
                        string val = lineSplit[1];

                        switch (var)
                        {
                            case "max_new_tokens": this.max_new_tokens = int.Parse(val); break;
                            case "preset": this.preset = val; break;
                            case "do_sample": this.do_sample = val.ToLower() == "true"; break;
                            case "temperature": this.temperature = double.Parse(val); break;
                            case "top_p": this.top_p = double.Parse(val); break;
                            case "top_k": this.top_k = int.Parse(val); break;
                            case "typical_p": this.typical_p = double.Parse(val); break;
                            case "epsilon_cutoff": this.epsilon_cutoff = double.Parse(val); break;
                            case "eta_cutoff": this.eta_cutoff = double.Parse(val); break;
                            case "tfs": this.tfs = double.Parse(val); break;
                            case "top_a": this.top_a = double.Parse(val); break;
                            case "repetition_penalty": this.repetition_penalty = double.Parse(val); break;
                            case "repetition_penalty_range": this.repetition_penalty_range = int.Parse(val); break;
                            case "min_length": this.min_length = int.Parse(val); break;
                            case "no_repeat_ngram_size": this.no_repeat_ngram_size = int.Parse(val); break;
                            case "num_beams": this.num_beams = int.Parse(val); break;
                            case "pentaty_alpha": this.penalty_alpha = int.Parse(val); break;
                            case "length_penalty": this.length_penalty = int.Parse(val); break;
                            case "early_stopping": this.early_stopping = val.ToLower() == "true"; break;
                            case "mirostat_mode": this.mirostat_mode = int.Parse(val); break;
                            case "mirostat_tau": this.mirostat_tau = double.Parse(val); break;
                            case "mirostat_eta": this.mirostat_eta = double.Parse(val); break;
                            case "seed": this.seed = int.Parse(val); break;
                            case "add_bos_token": this.add_bos_token = val.ToLower() == "true"; break;
                            case "truncation_length": this.truncation_length = int.Parse(val); break;
                            case "ban_eos_token": this.ban_eos_token = val.ToLower() == "true"; break;
                            case "skip_special_tokens": this.skip_special_tokens = val.ToLower() == "true"; break;
                        }
                    }
                }
                else
                    bldr.AppendLine(line);
            }
            this.topicText = bldr.ToString();
        }
    }

    [Serializable]
    public class ChatBotParameters
    {
        public string systemPrompt { get; set; } = "";
        public string questionPrompt { get; set; } = "Q: ";
        public string answerPrompt { get; set; } = "A: ";
        public string emptyResponse { get; set; } = "";

        public List<string> topics { get; set; } = new List<string>();

        public string aiapiUrl { get; set; } = "";

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
            var configJson = System.Text.Json.JsonSerializer.Serialize(this);
            File.WriteAllText(ConfigPath, configJson);
        }

        public static ChatBotParameters LoadFromDisk()
        {
            string storedConfigJson = "";

            // Load AppConfig from local storage
            if (File.Exists(ConfigPath))
                storedConfigJson = File.ReadAllText(ConfigPath);

            if (!string.IsNullOrEmpty(storedConfigJson))
            {
                return System.Text.Json.JsonSerializer.Deserialize<ChatBotParameters>(storedConfigJson);                
            }
            else
            {
                // Initialize with default values if not found in local storage
                return new ChatBotParameters();
            }
        }
    }
}
