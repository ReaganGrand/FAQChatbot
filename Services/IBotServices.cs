using Microsoft.Bot.Builder.AI.QnA;

namespace FAQChatbot.Services
{
    public interface IBotServices
    {        
        CustomQuestionAnswering CQAService { get; }
    }
}
