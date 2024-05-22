using FAQChatbot.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace FAQChatbot.Dialogs
{
    public class ApplicationDialog: CancelAndHelpDialog
    {      

        private readonly ILogger<ApplicationDialog> logger;
        public ApplicationDialog(ILogger<ApplicationDialog> _logger, StateService stateService,
                                QuestionDialog questionDialog,ApplicationMenuDialog applicationMenuDialog
                                //IConfiguration configuration,IHttpClientFactory httpClientFactory
                                ) 
        :base(_logger,stateService,nameof(ApplicationDialog))
        {
            logger = _logger??throw new ArgumentNullException(nameof(_logger));
            
            // This array defines how the Waterfall will execute.
            var waterfallSteps = new WaterfallStep[]
            {
                InitialStepAsync,
                ActStepAsync,
                FinalStepAsync,                
            };

            // Add named dialogs to the DialogSet. These names are saved in the dialog state.
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), waterfallSteps));
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));
            AddDialog(questionDialog);
            AddDialog(applicationMenuDialog);
            //AddDialog(new QuestionDialog(httpClientFactory:httpClientFactory, logger,configuration, stateService));
            //AddDialog(new ApplicationMenuDialog(logger,stateService));
            InitialDialogId = nameof(WaterfallDialog);
        }
                
        private async Task<DialogTurnResult> InitialStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            DialogTurnResult result = null;  
            try{
                var welcomeText = stepContext.Options?.ToString() ?? null;
                result = await stepContext.BeginDialogAsync(nameof(ApplicationMenuDialog), welcomeText, cancellationToken);  

            }catch(Exception ex){
                Log.Error("An unhandled exception occured {error}",ex);

            }
            return result;                      
        }

        private async Task<DialogTurnResult> ActStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            DialogTurnResult result = null;
            try{
                if ((string)stepContext.Result == ConstantService.APLM)
                {
                    result = await stepContext.BeginDialogAsync(nameof(QuestionDialog),null, cancellationToken);
                }
                else if ((string)stepContext.Result == ConstantService.SBM)
                {
                    result= await stepContext.BeginDialogAsync(nameof(QuestionDialog), null, cancellationToken);
                }
                else if ((string)stepContext.Result == ConstantService.IQ)
                {
                    result = await stepContext.BeginDialogAsync(nameof(QuestionDialog), null, cancellationToken);
                }
                else if ((string)stepContext.Result == ConstantService.Back2MainMenu)
                {                
                    result = await stepContext.EndDialogAsync(null,cancellationToken);
                }
                else
                {   
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text("The selected option not found."), cancellationToken);
                    result = await stepContext.NextAsync(null, cancellationToken);
                }  

            }catch(Exception ex){
                    Log.Error("An unhandled exception occured {error}",ex);
            }
            return result;
                      
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            DialogTurnResult result=null;
            try{
                var promptMessage = stepContext.Result?.ToString() ?? ConstantService.AppMenuSecondMessageText;
                 await stepContext.ReplaceDialogAsync(InitialDialogId, promptMessage, cancellationToken); 

            }catch(Exception ex){
                Log.Error("An unhandled exception occured {error}",ex);
            }
            return result;
        }

    }
}
