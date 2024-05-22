using AdaptiveCards;
using FAQChatbot.Models;
using FAQChatbot.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using Newtonsoft.Json.Linq;
using Serilog;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FAQChatbot.Dialogs
{
    public class NoMatchDialog : CancelAndHelpDialog
    {

        private readonly ILogger<NoMatchDialog> logger;
        public NoMatchDialog(ILogger<NoMatchDialog> _logger,StateService stateService, TicketDialog ticketDialog) : base(_logger, stateService,nameof(NoMatchDialog))
        {
            logger =_logger ?? throw new ArgumentNullException(nameof(_logger));
            // This array defines how the Waterfall will execute.
            var waterfallSteps = new WaterfallStep[]
            {
                InitialStepAsync,
                ActStepAsync,                
            };

            // Add named dialogs to the DialogSet. These names are saved in the dialog state.
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), waterfallSteps));
            AddDialog(ticketDialog);
            //AddDialog(new TicketDialog(stateService));
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));            
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> InitialStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {    
            DialogTurnResult result =null;
            try{
            
            List<string> YesorNo = ["Yes", "No"];
            var card = UtilityService.GetAdaptivePromptCard(YesorNo, ConstantService.NoGoodMatch);
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
                Log.Error("An unhandled exception occured {error}",ex);
            }
            return result;
        }

        private async Task<DialogTurnResult> ActStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            DialogTurnResult result = null;
            try{
                if ((bool)stepContext.Result)
                {               
                    result = await stepContext.BeginDialogAsync(nameof(TicketDialog), null, cancellationToken); 
                }
                else
                {                
                    result = await stepContext.EndDialogAsync((bool)stepContext.Result, cancellationToken);
                }
            }catch(Exception ex){
                Log.Error("An unhandled exception occured {error}",ex);
            }
            return result;         
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            DialogTurnResult result =null;
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
                Log.Error("An unhandled exception occured {error}",ex);
            }
            return result;     
        }
    }
}