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
        // This class is instanced when a new conversation session is started from the SOJEE chatbot interface.
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
                    Console.WriteLine($"Conversation:{DateTime.Now.ToShortTimeString}/{this.Id} Q:{newQuery} A:{response}");
                    Log.Information($"Conversation:{this.Id} Q:{newQuery} A:{response}");
                    convoEntries.Add(oldTS, new(newQuery, response)); ;
                }
                else
                {
                    Console.WriteLine($"Conversation:{DateTime.Now.ToShortTimeString}/{this.Id} Q:{newQuery}");
                    Log.Information($"Conversation:{this.Id} Q:{newQuery}");
                }

                return response;
            }

        }

        public static async Task<string> GetResponseFromApi(string Prompt, ChatBotParameters cbParams, AIInferenceProfile profile)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    // Serialize the data to JSON
                    profile.prompt = Prompt;
                    profile.stopping_strings = new string[] { $"\n{cbParams.questionPrompt}" };

                    var requestData = new
                    {
                        prompt = Prompt,
                        max_new_tokens = profile.max_new_tokens,
                        preset = profile.preset,
                        do_sample = profile.do_sample,
                        temperature = profile.temperature,
                        top_p = profile.top_p,
                        top_k = profile.top_k,
                        typical_p = profile.typical_p,
                        epsilon_cutoff = profile.epsilon_cutoff,
                        eta_cutoff = profile.eta_cutoff,
                        tfs = profile.tfs,
                        top_a = profile.top_a,
                        repetition_penalty = profile.repetition_penalty,
                        repetition_penalty_range = profile.repetition_penalty_range,
                        min_length = profile.min_length,
                        no_repeat_ngram_size = profile.no_repeat_ngram_size,
                        num_beams = profile.num_beams,
                        penalty_alpha = profile.penalty_alpha,
                        length_penalty = profile.length_penalty,
                        early_stopping = profile.early_stopping,
                        mirostat_mode = profile.mirostat_mode,
                        mirostat_tau = profile.mirostat_tau,
                        mirostat_eta = profile.mirostat_eta,
                        seed = profile.seed,
                        add_bos_token = profile.add_bos_token,
                        truncation_length = profile.truncation_length,
                        ban_eos_token = profile.ban_eos_token,
                        skip_special_tokens = profile.skip_special_tokens,
                        stopping_strings = new string[] { $"\n{cbParams.questionPrompt}", "Q: " }
                    };

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

            AIInferenceProfile initialPrompt = new AIInferenceProfile("initial");
            initialPrompt.max_new_tokens = 8;

            // Get the response from the API
            string response = await GetResponseFromApi(cbParams.systemPrompt + initialPrompt.topicText + prompt2, cbParams, initialPrompt);

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

            if (cbParams.topics.Contains(actualResponse.ToLower()))
            {
                AIInferenceProfile topicPrompt = new AIInferenceProfile(actualResponse.ToLower());
                if (!string.IsNullOrEmpty(topicPrompt.topicText))
                {
                    response2 = await GetResponseFromApi(cbParams.systemPrompt + topicPrompt.topicText + initialQuestion + $"\n{cbParams.answerPrompt}", cbParams, topicPrompt);
                }
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