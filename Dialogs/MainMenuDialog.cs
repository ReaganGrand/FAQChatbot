using AdaptiveCards;
using FAQChatbot.Models;
using FAQChatbot.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FAQChatbot.Dialogs
{
    public class MainMenuDialog : CancelAndHelpDialog
    {
        protected readonly ILogger<MainMenuDialog> logger;
        private readonly StateService _stateService;
        private List<string> _mainMenuChoices;
        // Dependency injection uses this constructor to instantiate MainDialog
        public MainMenuDialog(ILogger<MainMenuDialog> _logger, StateService stateService)
            : base(_logger,stateService,nameof(MainMenuDialog))
        {
            _stateService = stateService ?? throw new ArgumentNullException(nameof(stateService));
            logger = _logger?? throw new ArgumentNullException(nameof(_logger));;

            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));            
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
            DialogTurnResult result = null;  
            try{
                
            ConversationData conversationData = await _stateService.ConversationDataAccessor.GetAsync(stepContext.Context, () => new ConversationData(), cancellationToken);
            var welcomeText = stepContext.Options?.ToString() ?? "How can I help you today with PLM related queries/service requests? Select your option.";
            var promptMessage = UtilityService.GetAdaptiveCardsText(welcomeText);            
            await stepContext.Context.SendActivityAsync(promptMessage, cancellationToken);
            
            if (!string.IsNullOrEmpty(conversationData.SelectedMainMenuOption))
            {
                if (
                    conversationData.SelectedMainMenuOption.Equals(ConstantService.Contactus) ||
                    conversationData.SelectedMainMenuOption.Equals(ConstantService.GSD)
                    ) 
                {
                    _mainMenuChoices.Clear();
                    _mainMenuChoices.Add(ConstantService.Back2MainMenu);                    
                }
                else if (conversationData.SelectedMainMenuOption.Equals(ConstantService.Back2MainMenu) ||
                    (!string.IsNullOrEmpty(conversationData.SelectedAppMenuOption) &&
                    conversationData.SelectedAppMenuOption.Equals(ConstantService.Back2MainMenu))                    
                    )
                {
                    _mainMenuChoices.Clear();
                    _mainMenuChoices = UtilityService.GetMenuItem();
                }
                else
                {
                    _mainMenuChoices.Clear();
                    _mainMenuChoices = UtilityService.GetMenuItem();
                    _mainMenuChoices.Remove(conversationData.SelectedMainMenuOption);
                    _mainMenuChoices.Add(ConstantService.Back2MainMenu);
                }
            }
            else 
            {
                _mainMenuChoices = UtilityService.GetMenuItem(); 
            }

            var card = UtilityService.GetAdaptivePromptCard(_mainMenuChoices);
            // Retry card
            var retryCard = UtilityService.GetAdaptivePromptCard(_mainMenuChoices, ConstantService.RetryText);

            
            // Prompt
            result = await stepContext.PromptAsync(nameof(ChoicePrompt), new PromptOptions
            {
                Prompt = (Activity)MessageFactory.Attachment(new Attachment
                {
                    ContentType = AdaptiveCard.ContentType,
                    // Convert the AdaptiveCard to a JObject
                    Content = JObject.FromObject(card),
                }),
                Choices = ChoiceFactory.ToChoices(_mainMenuChoices),
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
                ConversationData conversationData = await _stateService.ConversationDataAccessor.GetAsync(stepContext.Context, () => new ConversationData(), cancellationToken);
                conversationData.SelectedMainMenuOption = ((FoundChoice)stepContext.Result).Value;
                await _stateService.ConversationDataAccessor.SetAsync(stepContext.Context, conversationData, cancellationToken);
                result = await stepContext.EndDialogAsync(((FoundChoice)stepContext.Result).Value, cancellationToken);
            }catch(Exception ex){
                Log.Error("An unhandled exception occured {error}",ex);
            }
            return result;
        }
    }
}