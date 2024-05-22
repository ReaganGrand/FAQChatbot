using Microsoft.Bot.Builder.AI.QnA;
using Microsoft.Extensions.Configuration;
using Microsoft.Bot.Builder.AI.QnA.Models;
using System.Net.Http;

namespace FAQChatbot.Services
{
    public class BotServices:IBotServices
    {        
        public CustomQuestionAnswering CQAService { get; private set; }

        public BotServices(IConfiguration configuration)
        {
            CQAService= new CustomQuestionAnswering(new QnAMakerEndpoint
            {
                KnowledgeBaseId = configuration["ProjectName"],
                EndpointKey = configuration["LanguageEndpointKey"],
                Host = configuration["LanguageEndpointHostName"],
                QnAServiceType = ServiceType.Language,               
            });
        }        
    }
}
