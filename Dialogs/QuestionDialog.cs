using AdaptiveCards;
using FAQChatbot.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using Serilog;

namespace FAQChatbot.Dialogs
{
    public class QuestionDialog:CancelAndHelpDialog
    {
        private readonly ILogger<QuestionDialog> logger;
        private readonly StateService _stateService;
        public QuestionDialog(ILogger<QuestionDialog> _logger,StateService stateService,CQADialog cQADialog)                            
            :base(_logger,stateService,nameof(QuestionDialog))
        {
            logger=_logger??throw new ArgumentNullException(nameof(_logger));
            _stateService=stateService??throw new ArgumentNullException(nameof(stateService));
            // This array defines how the Waterfall will execute.
            var waterfallSteps = new WaterfallStep[]
            {
                InitialStepAsync,
                ActStepAsync,                
                MoreQuestionStepAsync,
                FinalStepAsync,                
            };

            // Add named dialogs to the DialogSet. These names are saved in the dialog state.
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), waterfallSteps));
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(cQADialog);                 
            //AddDialog(new CQADialog(httpClientFactory,configuration,stateService));            
            InitialDialogId = nameof(WaterfallDialog);
        }
                
        private async Task<DialogTurnResult> InitialStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            DialogTurnResult result = null;
            try{
                var questionText = stepContext.Options?.ToString()??"Please type your question!";
                var card = UtilityService.GetAdaptivePromptCard(questionText);
                result = await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions
                {                
                    Prompt = (Activity)MessageFactory.Attachment(new Attachment
                    {
                        ContentType = AdaptiveCard.ContentType,                    
                        Content = JObject.FromObject(card),

                    }),
                }, cancellationToken) ;
            }catch(Exception ex){
                Log.Error("An unhandled exception occured {error}",ex);
            }
            return result;
        }

        private async Task<DialogTurnResult> ActStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            DialogTurnResult result = null;
            try{
                result = await stepContext.BeginDialogAsync(nameof(CQADialog), stepContext.Result, cancellationToken);
            }catch(Exception ex){
                Log.Error("An unhandled exception occured {error}",ex);
            }
            return result;
        }

        private async Task<DialogTurnResult> MoreQuestionStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            DialogTurnResult result = null;
            try{
            
            List<string> YesorNo = ["Yes", "No"];
            var card = UtilityService.GetAdaptivePromptCard(YesorNo, ConstantService.MoreQuestionText);
            var retryCard = UtilityService.GetAdaptivePromptCard(YesorNo, ConstantService.RetryText);

            result = await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions
            {
                Prompt = (Activity)MessageFactory.Attachment(new Attachment
                {
                    ContentType = AdaptiveCard.ContentType,
                    // Convert the AdaptiveCard to a JObject
                    Content = JObject.FromObject(card),

                }),
                Choices = ChoiceFactory.ToChoices(YesorNo),
                // Don't render the choices outside the card
                Style = ListStyle.None,
                RetryPrompt = (Activity)MessageFactory.Attachment(new Attachment
                {
                    ContentType = AdaptiveCard.ContentType,
                    // Convert the AdaptiveCard to a JObject
                    Content = JObject.FromObject(retryCard),
                }),
            },
                cancellationToken);

            }catch(Exception ex){
                Log.Error("An unhandled exception occured {error} at {methodname} of {classname}",ex,nameof(MoreQuestionStepAsync),nameof(QuestionDialog));
            }
            return result;            
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            DialogTurnResult result = null;
            try{
                if ((bool)stepContext.Result)
                {               
                    result = await stepContext.BeginDialogAsync(InitialDialogId, "Please type your next query!", cancellationToken);
                }
                else
                {                
                    result = await stepContext.EndDialogAsync(null, cancellationToken);
                }

            }catch(Exception ex){
                Log.Error("An unhandled exception occured {error} at {methodname} of {classname}",ex,nameof(FinalStepAsync),nameof(QuestionDialog));
            }
            return result;            
        }
    }
}