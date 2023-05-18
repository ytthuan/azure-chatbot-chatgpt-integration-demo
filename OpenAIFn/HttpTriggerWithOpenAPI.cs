using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Azure.AI.OpenAI;
using Azure;

namespace Leo.AzureOA
{
    public class HttpTriggerWithOpenAPI
    {
        private readonly ILogger<HttpTriggerWithOpenAPI> _logger;
        private const string GPT_MODEL432 = "your_chat_model_name";//gpt model name
        private const string ENDPOINT = "https://your_openai_service.openai.azure.com/";// api endpoint
   

        private static ChatCompletionsOptions chatCompletionsOptions = new ChatCompletionsOptions(){
            Temperature = (float)0.5,
            MaxTokens = 15000,//restricted the response token here
            NucleusSamplingFactor = (float)0.95,
            FrequencyPenalty = 0,
            PresencePenalty = 0};
        private readonly OpenAIClient _openAIClient;
        public HttpTriggerWithOpenAPI(ILogger<HttpTriggerWithOpenAPI> log)
        {
            _logger = log;
            var key = Environment.GetEnvironmentVariable("AzureOpenAPIKey");
            var credential = new AzureKeyCredential(key);
            var apiEndpoint = new Uri(ENDPOINT);
            _openAIClient = new OpenAIClient(apiEndpoint, credential);
            

        }

        [FunctionName("HttpTriggerWithOpenAPI")]
        [OpenApiOperation(operationId: "Run", tags: new[] { "name" })]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(ChatMessage), Required = true, Description = "The request body containing the message")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(ChatMessage), Description = "The OK response")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");


            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            ChatMessage data = JsonConvert.DeserializeObject<ChatMessage>(requestBody);
            if (data.Content.ToLower() == "clr")
            {
                chatCompletionsOptions.Messages.Clear();
                var cm = new ChatMessage(ChatRole.Assistant,"CLEARED");
                return new OkObjectResult(cm);
            }
        
            if (data != null)
            {
                try
                {
                    //Console.WriteLine($"End {chatCompletionsOptions.Messages.Count}");
                    chatCompletionsOptions.Messages.Add(data);
                    var response = await _openAIClient.GetChatCompletionsAsync(GPT_MODEL432, chatCompletionsOptions);
                    var botMessage = response.Value.Choices[0].Message.Content;
                    var botChatMessage = new ChatMessage(ChatRole.Assistant,botMessage);
                    chatCompletionsOptions.Messages.Add(botChatMessage);
                  
                    return new OkObjectResult(botChatMessage);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing request");
                    return new StatusCodeResult(StatusCodes.Status500InternalServerError);
                }
            }
            else
            {
                return new BadRequestObjectResult("Please provide a message in the request body");
            }
        }
    }
}

