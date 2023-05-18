// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
//
// Generated with EchoBot .NET Template version v4.17.1

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Azure.AI.OpenAI;
using System.Net.Http;
using Newtonsoft.Json;


namespace EchoBot.Bots
{
    public class EchoBot : ActivityHandler
    {
        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            var typingActivity = new Activity { Type = ActivityTypes.Typing };
            await turnContext.SendActivityAsync(typingActivity, cancellationToken);
            string functionUrl = "<function_url>?code=<function_code>";
            var text = turnContext.Activity.Text;
            ChatMessage chatMessage = new ChatMessage(ChatRole.User,text); 
            string replyText = string.Empty;
            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response = await client.PostAsync(functionUrl,new StringContent(JsonConvert.SerializeObject(chatMessage)));

                if (response.IsSuccessStatusCode)
                {
                    replyText = JsonConvert.DeserializeObject<ChatMessage>(await response.Content.ReadAsStringAsync()).Content;
                    
                }
                else
                {
                    replyText = "You're getting trouble please contact your Admin";
                }
            }
            await turnContext.SendActivityAsync(MessageFactory.Text(replyText, replyText), cancellationToken);
        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            var welcomeText = "Chào mừng bạn đến với chatbot GPT!";
            foreach (var member in membersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    await turnContext.SendActivityAsync(MessageFactory.Text(welcomeText, welcomeText), cancellationToken);
                }
            }
        }
    }
}
