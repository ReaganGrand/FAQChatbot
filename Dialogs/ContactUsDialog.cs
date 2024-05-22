using AdaptiveCards;
using FAQChatbot.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;
using Serilog;

namespace FAQChatbot.Dialogs
{
    public class ContactUsDialog:CancelAndHelpDialog
    {
        protected readonly ILogger<ContactUsDialog> logger;

        public ContactUsDialog(ILogger<ContactUsDialog> _logger, StateService stateService):base(_logger,stateService,nameof(ContactUsDialog))
        {       
            logger = _logger ?? throw new ArgumentNullException(nameof(_logger));
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog)).AddStep(InitialStepAsync));
            InitialDialogId = nameof(WaterfallDialog);
        }

     
        private async Task<DialogTurnResult> InitialStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {           
            IMessageActivity messageActivity = null;
            DialogTurnResult result =null;
            try{
                string[] paths = [".", "Cards", "Contactus.json"];
                var contactUsEmailJson = File.Open(Path.Combine(paths), FileMode.Open);
                using (var reader = new StreamReader(contactUsEmailJson))
                {
                    var adaptiveCard = reader.ReadToEnd();
                    messageActivity = MessageFactory.Attachment(new Attachment()
                    {
                        ContentType = AdaptiveCard.ContentType,
                        Content = JsonConvert.DeserializeObject(adaptiveCard),
                    });                
                }
                await stepContext.Context.SendActivityAsync(messageActivity, cancellationToken);
                var promptMessage = $"To go to back to Main Menu, please click on the {ConstantService.Back2MainMenu} button.";            
                result = await stepContext.EndDialogAsync(promptMessage, cancellationToken);

            }catch(Exception ex){
                Log.Error("An unhandled exception occured {error}",ex);
            }
            return result;            
        }
    }
}