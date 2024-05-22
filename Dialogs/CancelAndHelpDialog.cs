// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
//
// Generated with Bot Builder V4 SDK Template for Visual Studio CoreBot v4.13.1

using FAQChatbot.Models;
using FAQChatbot.Services;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FAQChatbot.Dialogs
{
    
    public class CancelAndHelpDialog : ComponentDialog
    {
        private const string HelpMsgText = "Hi! I am a Help Bot. What can I do for you today?";
        private const string CancelMsgText = "Cancelling your operations";
        private const string GreetMsgText = "I am fine. How can I help you today?";
        private readonly StateService _stateService;
        protected readonly ILogger<CancelAndHelpDialog> _logger;
        public CancelAndHelpDialog(ILogger<CancelAndHelpDialog> logger,StateService stateService,string dialogId):base(dialogId)
        {
            _logger= logger ?? throw new ArgumentNullException(nameof(logger));
            _stateService = stateService ?? throw new ArgumentNullException(nameof(stateService));
        }

        protected override async Task<DialogTurnResult> OnContinueDialogAsync(DialogContext innerDc, CancellationToken cancellationToken = default)
        {
            /*var result = await InterruptAsync(innerDc, cancellationToken);
            if (result != null)
            {
                return result;
            }

            return await base.OnContinueDialogAsync(innerDc, cancellationToken);*/
            //DialogTurnResult result = null;
            try{
                var txt = innerDc.Context.Activity.Text;
                dynamic val=innerDc.Context.Activity.Value;

                if(!string.IsNullOrEmpty(txt) && val == null)
                {
                    var result = await InterruptAsync(innerDc, cancellationToken);
                    if (result != null)
                    {
                        return result;
                    }                
                }                                
            }catch(Exception ex){
                Log.Error("An unhandled exception occured {error}",ex);
            }
            return await base.OnContinueDialogAsync(innerDc, cancellationToken);;
        }

        private async Task<DialogTurnResult> InterruptAsync(DialogContext innerDc, CancellationToken cancellationToken)
        {
            DialogTurnResult result =null;
            try{
                if (innerDc.Context.Activity.Type == ActivityTypes.Message)
                {
                    var text = innerDc.Context.Activity.Text.ToLowerInvariant();

                    switch (text)
                    {
                        case "help":
                        case "?":
                        //case "hi":
                        case "hello":
                        case "heyo":
                        await ResetState(innerDc, cancellationToken);
                            result = await innerDc.ReplaceDialogAsync(nameof(MainDialog), HelpMsgText, cancellationToken: cancellationToken);
                            break;
                        case "how are you":
                        case "how are you?":
                            await ResetState(innerDc, cancellationToken);                        
                            result = await innerDc.ReplaceDialogAsync(nameof(MainDialog), GreetMsgText, cancellationToken: cancellationToken);
                            break;
                        case "cancel":
                        case "bye bye":
                        case "bye":
                        case "startover":
                        case "start over":
                        case "quit":
                            await ResetState(innerDc, cancellationToken);                        
                            await innerDc.Context.SendActivityAsync(UtilityService.GetAdaptiveCardsText(CancelMsgText), cancellationToken);
                            result = await innerDc.CancelAllDialogsAsync(cancelParents: false, "cancelEvent", "1234", cancellationToken: cancellationToken);
                            break;
                        default:
                            result = null;
                            break;
                    }
                }                
            }
            catch(Exception ex){
                Log.Error("An unhandled exception occured {error}",ex);
            }
            return result;         
        }

        private async Task ResetState(DialogContext innerDc, CancellationToken cancellationToken)
        {
            try{
                ConversationData conversationData = await _stateService.ConversationDataAccessor.GetAsync(innerDc.Context, () => new ConversationData(), cancellationToken);
                conversationData.SelectedMainMenuOption = string.Empty;
                conversationData.SelectedAppMenuOption = string.Empty;
                await _stateService.ConversationDataAccessor.SetAsync(innerDc.Context, conversationData, cancellationToken);

            }catch(Exception ex){
                Log.Error("An unhandled exception occured {error}",ex);
            }            
        }
    }
}