﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
//
// Generated with Bot Builder V4 SDK Template for Visual Studio CoreBot v4.13.1

using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using FAQChatbot.Services;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace FAQChatbot.Bots
{
    // This IBot implementation can run any type of Dialog. The use of type parameterization is to allows multiple different bots
    // to be run at different endpoints within the same project. This can be achieved by defining distinct Controller types
    // each with dependency on distinct IBot types, this way ASP Dependency Injection can glue everything together without ambiguity.
    // The ConversationState is used by the Dialog system. The UserState isn't, however, it might have been used in a Dialog implementation,
    // and the requirement is that all BotState objects are saved at the end of a turn.
    public class DialogBot<T> : ActivityHandler
        where T : Dialog
    {
        protected readonly Dialog Dialog;
        protected readonly ILogger<DialogBot<T>> logger;
        private readonly StateService _stateService;

        public DialogBot(StateService stateService, T dialog, ILogger<DialogBot<T>> _logger)
        {
            Dialog = dialog ?? throw new ArgumentNullException(nameof(dialog));
            logger = _logger ?? throw new ArgumentNullException(nameof(_logger));
            _stateService = stateService ?? throw new ArgumentNullException(nameof(stateService));
        }

        public override async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default)
        {
            try{
                    await base.OnTurnAsync(turnContext, cancellationToken);
                    // Save any state changes that might have occurred during the turn.
                    await _stateService.UserState.SaveChangesAsync(turnContext, false, cancellationToken);
                    await _stateService.ConversationState.SaveChangesAsync(turnContext, false, cancellationToken);
            }catch(Exception ex){
                Log.Error("An unhandled exception occured {ClassName} - {MethodName} - {error}", nameof(DialogBot<T>), nameof(OnTurnAsync),ex);
            }            
        }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            try{
                //Log.Information("Running dialog with Message Activity {stateService}",_stateService.ConversationState);
                // Run the Dialog with the new message Activity.
                if(_stateService!=null && _stateService.ConversationState!=null)
                await Dialog.RunAsync(turnContext, _stateService.ConversationState.CreateProperty<DialogState>("DialogState"), cancellationToken);
            }catch(Exception ex){
                Log.Error("An unhandled exception occured {ClassName} - {MethodName} - {error}", nameof(DialogBot<T>), nameof(OnMessageActivityAsync),ex);
            }
        }
    }
}