using AdaptiveCards;
using FAQChatbot.Models;
using FAQChatbot.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Newtonsoft.Json.Linq;
using Microsoft.Bot.Schema;
using Serilog;

namespace FAQChatbot.Dialogs
{
    public class ApplicationMenuDialog:CancelAndHelpDialog
    {
        private readonly ILogger<ApplicationMenuDialog> logger;
        private readonly StateService _stateService;
        private List<string> _appMenuChoices;

        public ApplicationMenuDialog(ILogger<ApplicationMenuDialog> _logger, StateService stateService)
            : base(_logger,stateService,nameof(ApplicationMenuDialog))
        {
            _stateService = stateService ?? throw new ArgumentNullException(nameof(stateService));
            logger = _logger?? throw new ArgumentNullException(nameof(_logger));;

            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));
            //AddDialog(new ContactUs(logger:logger,stateService:stateService));
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                IntroStepAsync,
                ActStepAsync,
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> IntroStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {   
            DialogTurnResult result =null;
            try{
                
                ConversationData conversationData = await _stateService.ConversationDataAccessor.GetAsync(stepContext.Context, () => new ConversationData(), cancellationToken);

                var welcomeText = stepContext.Options?.ToString() ?? ConstantService.AppMenuFirstMessageText;
                var promptMessage = UtilityService.GetAdaptiveCardsText(welcomeText);
                await stepContext.Context.SendActivityAsync(promptMessage, cancellationToken);

                if (!string.IsNullOrEmpty(conversationData.SelectedAppMenuOption))
                {
                    _appMenuChoices.Clear();
                    _appMenuChoices = UtilityService.GetAppMenuItem();
                    if(conversationData.SelectedAppMenuOption!=ConstantService.Back2MainMenu)
                    {
                        _appMenuChoices.Remove(conversationData.SelectedAppMenuOption);
                    }                                                              
                }
                else
                {
                    _appMenuChoices = UtilityService.GetAppMenuItem();
                }

                var card = UtilityService.GetAdaptivePromptCard(_appMenuChoices);            

                // Retry card
                var retryCard = UtilityService.GetAdaptivePromptCard(_appMenuChoices, ConstantService.RetryText);
                
                // Prompt
                result = await stepContext.PromptAsync(nameof(ChoicePrompt), new PromptOptions
                {
                    Prompt = (Activity)MessageFactory.Attachment(new Attachment
                    {
                        ContentType = AdaptiveCard.ContentType,                    
                        Content = JObject.FromObject(card),
                    }),
                    Choices = ChoiceFactory.ToChoices(_appMenuChoices),                
                    Style = ListStyle.None,
                    RetryPrompt = (Activity)MessageFactory.Attachment(new Attachment
                    {
                        ContentType = AdaptiveCard.ContentType,                    
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
            DialogTurnResult result =null;
            try{
                ConversationData conversationData = await _stateService.ConversationDataAccessor.GetAsync(stepContext.Context, () => new ConversationData(), cancellationToken);
            
                conversationData.SelectedAppMenuOption = ((FoundChoice)stepContext.Result).Value;

                await _stateService.ConversationDataAccessor.SetAsync(stepContext.Context, conversationData, cancellationToken);
                
                result = await stepContext.EndDialogAsync(((FoundChoice)stepContext.Result).Value, cancellationToken);

            }catch(Exception ex){
                Log.Error("An unhandled exception occured {error}",ex);
            }
            return result;                        
        }
    }
}
