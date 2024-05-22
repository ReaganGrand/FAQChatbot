// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
//
// Generated with Bot Builder V4 SDK Template for Visual Studio CoreBot v4.13.1

using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Builder.TraceExtensions;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;
using System;

namespace FAQChatbot
{
    public class AdapterWithErrorHandler : BotFrameworkHttpAdapter
    {
        private readonly ILogger<AdapterWithErrorHandler> _logger;
        public AdapterWithErrorHandler(IConfiguration configuration, ILogger<AdapterWithErrorHandler> logger, ConversationState conversationState = null)
            : base(configuration, logger)
        {
            _logger =logger??throw new ArgumentNullException(nameof(logger));
            OnTurnError = async (turnContext, exception) =>
            {
                // Log any leaked exception from the application.
                //logger.LogError(exception, $"[OnTurnError] unhandled error : {exception.Message}");
                Log.Error("[OnTurnError] unhandled error : {exception.Message}",exception.Message);
                //var errorMessage = string.Empty;
                // Send a message to the user
                //var errorMessageText = "The bot encountered an error or bug.";
                //var errorMessage = MessageFactory.Text(exception.Message, exception.Message, InputHints.ExpectingInput);
                //await turnContext.SendActivityAsync(errorMessage);

                var errorMessageText = "An error occured! Please contact administrator.";//"To continue to run this bot, please fix the bot source code.";
                var errorMessage = MessageFactory.Text(errorMessageText, errorMessageText, InputHints.ExpectingInput);
                await turnContext.SendActivityAsync(errorMessage);

                if (conversationState != null)
                {
                    try
                    {
                        // Delete the conversationState for the current conversation to prevent the
                        // bot from getting stuck in a error-loop caused by being in a bad state.
                        // ConversationState should be thought of as similar to "cookie-state" in a Web pages.
                        await conversationState.DeleteAsync(turnContext);
                    }
                    catch (Exception e)
                    {
                        Log.Error("Exception caught on attempting to Delete ConversationState : {exception.Message}",e.Message);
                        //logger.LogError(e, $"Exception caught on attempting to Delete ConversationState : {e.Message}");
                    }
                }

                // Send a trace activity, which will be displayed in the Bot Framework Emulator
                await turnContext.TraceActivityAsync("OnTurnError Trace", exception.Message, "https://www.botframework.com/schemas/error", "TurnError");
            };
        }
    }
}
