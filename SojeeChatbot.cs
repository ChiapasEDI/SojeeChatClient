using System;
using System.Net.Http;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Reflection.PortableExecutable;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using static MudBlazor.CategoryTypes;
using System.Xml.Serialization;
using Serilog;

namespace SojeeChat
{
    public class ChatbotClient
    {
        // Model Specific prompts
        //internal static string questionPrompt = "Q: ";
        //internal static string answerPrompt = "A: ";

        public class QueryProcessor
        {
            public ChatBotParameters cbParams;

            private readonly Queue<QueryItem> queryQueue = new Queue<QueryItem>();
            private readonly Dictionary<Guid, TaskCompletionSource<Conversation>> queryCompletionSources = new Dictionary<Guid, TaskCompletionSource<Conversation>>();
            private readonly Timer timer;
            private readonly object lockObject = new object();

            public QueryProcessor()
            {
                // The timer will tick every 100 milliseconds to process items from the queue.
                timer = new Timer(ProcessQueue, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(100));
                cbParams = ChatBotParameters.LoadFromDisk();
            }

            // Define a class to represent query items.
            private class QueryItem
            {
                public Guid Id { get; set; }
                public Conversation? Query { get; set; }
            }

            public Task<Conversation> SubmitQuery(Conversation convoItem)
            {
                var completionSource = new TaskCompletionSource<Conversation>();
                var queryItem = new QueryItem { Id = Guid.NewGuid(), Query = convoItem };

                lock (lockObject)
                {
                    queryCompletionSources[queryItem.Id] = completionSource;
                    queryQueue.Enqueue(queryItem);
                }

                return completionSource.Task;
            }

            private async void ProcessQueue(object state)
            {
                List<QueryItem> itemsToProcess;

                lock (lockObject)
                {
                    itemsToProcess = queryQueue.ToList();
                    queryQueue.Clear();
                }

                foreach (var item in itemsToProcess)
                {
                    Conversation convo = item.Query;
                    // Get the chatbot response for this conversation

                    await convo.GetConversationResponse();

                    // Complete the corresponding Task with the response
                    TaskCompletionSource<Conversation> completionSource;
                    lock (lockObject)
                    {
                        queryCompletionSources.TryGetValue(item.Id, out completionSource);
                        queryCompletionSources.Remove(item.Id);
                    }

                    if (completionSource != null)
                    {
                        completionSource.SetResult(convo);
                    }
                }
            }
        }

        // SOJEE Conversation
        // 
        // This class is instanced when a new conversation session is started from the SOJEE chatbot interface.  It times out after five minutes.
        public class Conversation
        {
            // Unique ID for this conversation
            public Guid Id = Guid.NewGuid();
            public DateTime initialQuestion = DateTime.Now;
            public ChatBotParameters _botParams;

            private class Entry
            {
                internal string question = "";
                internal string answer = "";
            }

            public SortedList<DateTime, (string, string)> convoEntries = new SortedList<DateTime, (string, string)>();

            // Build the conversation prompt for the conversation, then send it to the SojeeQuestion handler for classfication and answering
            public async Task<string> GetConversationResponse()
            {
                // Turn the existing conversation into a prompt
                StringBuilder bldr = new StringBuilder();
                foreach (var item in convoEntries)
                {
                    bldr.AppendLine($"{_botParams.questionPrompt}" + item.Value.Item1);

                    if (!string.IsNullOrEmpty(item.Value.Item2))
                        bldr.AppendLine($"{_botParams.answerPrompt}" + item.Value.Item2);
                }
                DateTime oldTS = convoEntries.Last().Key;
                string newQuery = convoEntries.Last().Value.Item1;
                convoEntries.RemoveAt(convoEntries.Count - 1);
                string response = await SojeeQuestion(bldr.ToString(), _botParams);

                // Don't record an off-topic Q/A pair.
                if (!response.Contains("Sorry"))
                {
                    Console.WriteLine($"Conversation:{this.Id} Q:{newQuery} A:{response}");
                    Log.Information($"Conversation:{this.Id} Q:{newQuery} A:{response}");
                    convoEntries.Add(oldTS, new(newQuery, response)); ;
                }
                else
                {
                    Console.WriteLine($"Conversation:{this.Id} Q:{newQuery}");
                    Log.Information($"Conversation:{this.Id} Q:{newQuery}");
                }

                return response;
            }

        }

        //private readonly static string apiUrl = "http://96.86.129.154:5000/api/v1/generate"; // Replace this with your API URL

        public static async Task<string> GetResponseFromApi(string Prompt, ChatBotParameters cbParams)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    // Prepare the request data
                    var requestData = new
                    {
                        prompt = Prompt,
                        max_new_tokens = 700,
                        preset = "None",
                        do_sample = false,
                        temperature = 0.1,
                        top_p = .1,
                        top_k = 40,
                        typical_p = 1,
                        epsilon_cutoff = 0,
                        eta_cutoff = 0,
                        tfs = 1,
                        top_a = 0,
                        repetition_penalty = 1.18,
                        repetition_penalty_range = 0,
                        min_length = 0,
                        no_repeat_ngram_size = 0,
                        num_beams = 1,
                        penalty_alpha = 0,
                        length_penalty = 1,
                        early_stopping = false,
                        mirostat_mode = 0,
                        mirostat_tau = 5,
                        mirostat_eta = .1,
                        seed = -1,
                        add_bos_token = true,
                        truncation_length = 8192,
                        ban_eos_token = false,
                        skip_special_tokens = true,
                        stopping_strings = new string[] { $"\n{cbParams.questionPrompt}" }
                    };

                    // Serialize the data to JSON
                    var jsonContent = new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(requestData), System.Text.Encoding.UTF8, "application/json");

                    // Send the POST request to the API
                    client.Timeout = new TimeSpan(0, 3, 0);
                    HttpResponseMessage response = await client.PostAsync(cbParams.aiapiUrl, jsonContent);
                    // Check if the request was successful
                    if (response.IsSuccessStatusCode)
                    {
                        // Read the response content as a string
                        try
                        {
                            string responseBody = await response.Content.ReadAsStringAsync();
                            return responseBody;
                        }
                        catch
                        {
                            return "";
                        }

                    }
                    else
                    {
                        // Handle the case when the request is not successful (e.g., handle errors)
                        // For example:
                        Console.WriteLine($"Error: {response.StatusCode} - {response.ReasonPhrase}");
                        return "";
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle any exceptions that might occur during the API call
                Console.WriteLine($"Exception: {ex.Message}");
                return "";
            }
        }

        public static async Task<string> SojeeQuestion(string initialQuestion, ChatBotParameters cbParams)
        {
            // Sample question
            string prompt2 = initialQuestion + "\nCLASSIFICATION:";

            string initialText = (File.ReadAllText(Path.Join(AppDomain.CurrentDomain.BaseDirectory,"prompt_initial.txt")));
            // Get the response from the API
            string response = await GetResponseFromApi(initialText + prompt2, cbParams);

            string actualResponse = "";

            if (response != "")
            {
                // Display the response
                JObject o = (JObject)JToken.Parse(response);

                try
                {
                    actualResponse = o["results"][0]["text"].ToString().Trim().ToUpper();
                }
                catch
                {
                    actualResponse = "";
                }
            }
            else
                return cbParams.emptyResponse;

            //Console.WriteLine($"Category: {actualResponse}");

            string response2 = "";

            // Sometimes the AI Chatbot gives an explanation of the classification - cut it here to just the first word.
            if (actualResponse.Contains('\r'))
                actualResponse = actualResponse.Split('\r')[0];
            if (actualResponse.Contains(':'))
                actualResponse = actualResponse.Split(':')[0];
            if (actualResponse.Contains(' '))
                actualResponse = actualResponse.Split(' ')[0];

            if (cbParams.dctTopics.ContainsKey(actualResponse.ToUpper()))
            {
                response2  = await GetResponseFromApi(cbParams.dctTopics[actualResponse.ToUpper()] + initialQuestion + $"\n{cbParams.answerPrompt}", cbParams);
            }
            else
                return cbParams.emptyResponse;

            if (response2 != "")
            {
                JObject o2 = (JObject)JToken.Parse(response2);
                string actualResponse2 = "";
                try
                {
                    actualResponse2 = o2["results"][0]["text"].ToString().Trim();
                }
                catch
                {
                    actualResponse2 = "";
                }
                
                return actualResponse2;
                //Console.WriteLine($"Response: {actualResponse2}");
            }
            else
                return cbParams.emptyResponse; //"Sorry, but I am limited to discussing about topics related to Chiapas EDI Technologies and its products.";
        }
    }
}