// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
//
// Generated with Bot Builder V4 SDK Template for Visual Studio CoreBot v4.13.1
using Microsoft.Bot.Builder.Dialogs;
using FAQChatbot.Services;
using FAQChatbot.Models;
using Serilog;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using System.Data;

namespace FAQChatbot.Dialogs
{
    public class MainDialog : ComponentDialog
    {
        protected readonly ILogger _logger;
        private readonly StateService _stateService;

        // Dependency injection uses this constructor to instantiate MainDialog
        public MainDialog(ILogger<MainDialog> logger,StateService stateService,
                        //IHttpClientFactory httpClientFactory, IConfiguration configuration,                        
                        MainMenuDialog mainMenuDialog,ApplicationDialog applicationDialog,ContactUsDialog contactUsDialog
                        )
            : base(nameof(MainDialog))
        {
            _stateService = stateService ?? throw new ArgumentNullException(nameof(stateService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            AddDialog(mainMenuDialog);
            AddDialog(contactUsDialog);
            AddDialog(applicationDialog);
            //AddDialog(new MainMenuDialog(logger:logger,stateService:stateService));            
            //AddDialog(new ContactUs(logger: logger,stateService));            
            //AddDialog(new ApplicationDialog(httpClientFactory,logger: logger, configuration: configuration, stateService: stateService));
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                IntroStepAsync,
                ActStepAsync,
                FinalStepAsync,
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }        

        private async Task<DialogTurnResult> IntroStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {        
            DialogTurnResult result = null;    
            try{
                //Log.Information("Loaded {methodName} of class {className},",nameof(IntroStepAsync),nameof(MainDialog));
                var welcomeText = stepContext.Options?.ToString() ?? null;
                //DataTable dt=null;
                //dt.Columns.Add("test", typeof(string));
                result = await stepContext.BeginDialogAsync(nameof(MainMenuDialog), welcomeText, cancellationToken);                             
            }catch(Exception ex){
                Log.Error("An unhandled exception occured {ClassName} - {MethodName} - {error}", nameof(MainDialog), nameof(IntroStepAsync),ex);
                //Log.Fatal(ex.StackTrace);
                //throw ex.InnerException;
            }
            return result;                  
        }

        private async Task<DialogTurnResult> ActStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            DialogTurnResult result = null;
            string promptMessage = string.Empty;
            try{
            if ((string)stepContext.Result== ConstantService.Contactus) 
            { 
                result = await stepContext.BeginDialogAsync(nameof(ContactUsDialog), null, cancellationToken);
                
            }
            else if ((string)stepContext.Result == ConstantService.FAQ)
            {               
                result = await stepContext.BeginDialogAsync(nameof(ApplicationDialog), null, cancellationToken);
            }
            else if ((string)stepContext.Result == ConstantService.GSD)
            {                
                promptMessage = $"Please contact GSD, Call us@ 123-123-1234.\r\nTo go to back to Main Menu, please click on the {ConstantService.Back2MainMenu} button.";
                result = await stepContext.NextAsync(promptMessage, cancellationToken);
            }            
            else if ((string)stepContext.Result == ConstantService.Back2MainMenu)
            {
                promptMessage = $"How can I help you today?";                
                result = await stepContext.NextAsync(promptMessage, cancellationToken);                
            }
            else
            {                
                result = await stepContext.NextAsync(null, cancellationToken);
            }
            }catch(Exception ex){
                Log.Error("An unhandled exception occurred {error}",ex);
            }
            return result;            
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            DialogTurnResult result =null;   
            try{
                var resultText = stepContext.Result?.ToString();
                var promptMessage = resultText ?? "What else can I do for you?";
                result = await stepContext.ReplaceDialogAsync(InitialDialogId, promptMessage, cancellationToken);
            }catch(Exception ex)
            {
                Log.Error("An unhandled exception occurred {error}",ex);
            }
            return result;            
        }
    }
}
