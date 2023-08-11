using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor.Services;
using System.Reflection.PortableExecutable;
using static SojeeChat.ChatbotClient;

// TODO: 1. Add spinning indicator to indicate when a response is pending - done
//       2. Add some introductory text and some sample questions. - done
//       3. Find all crash bugs.
//       4. Logging! 
//       5. Interactivity

// The model that needs to be loaded is: TheBloke_OpenAssistant-Llama2-13B-Orca-8K-3319-GPTQ_gptq-8bit-64g-actorder_True w/ Triton on, and Truncate the prompt up to this length = 8192, Custom stop "\nQ:", with the API enabled.
// The server @ http://192.168.1.120:5000/api/v1/generate (run with text-generation-webui) works fine.

namespace SojeeChat
{

    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddRazorPages();
            builder.Services.AddServerSideBlazor();
            builder.Services.AddSingleton<ChatBotParameters>();
            builder.Services.AddMudServices();
            builder.Services.AddSingleton<QueryProcessor>();
            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                //app.UseHsts();
            }

            //app.UseHttpsRedirection();

            app.UseStaticFiles();

            app.UseRouting(); 

            app.MapBlazorHub();
            app.MapFallbackToPage("/_Host");

            app.Run();
        }
    }
}