// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
//
// Generated with Bot Builder V4 SDK Template for Visual Studio CoreBot v4.13.1

using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using FAQChatbot.Services;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveCards;
using Newtonsoft.Json.Linq;
using Serilog;

namespace FAQChatbot.Bots
{
    public class DialogAndWelcomeBot<T> : DialogBot<T>
        where T : Dialog
    {
        private readonly StateService _stateService;
         private readonly ILogger<DialogAndWelcomeBot<T>> _logger;
        public DialogAndWelcomeBot(StateService stateService, T dialog, ILogger<DialogAndWelcomeBot<T>> logger)
            : base(stateService, dialog, logger)
        {
            _stateService = stateService ?? throw new ArgumentNullException(nameof(stateService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override Task OnEventAsync(ITurnContext<IEventActivity> turnContext, CancellationToken cancellationToken)
        {
            return base.OnEventAsync(turnContext, cancellationToken);
        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            try{
            
            foreach (var member in membersAdded)
            {
                // Greet anyone that was not the target (recipient) of this message.
                // To learn more about Adaptive Cards, see https://aka.ms/msbot-adaptivecards for more details.
                if (member.Id != turnContext.Activity.Recipient.Id)
                {                    
                    IMessageActivity messageActivity = null;
                    string[] paths = [".", "Cards", "welcomeCard.json"];
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

                    await turnContext.SendActivityAsync(messageActivity, cancellationToken);
                    await Dialog.RunAsync(turnContext, _stateService.ConversationState.CreateProperty<DialogState>("DialogState"), cancellationToken);
                }
            }
            }catch(Exception ex){
                Log.Error("An unhandled exception occured {error}",ex);
            }            
        }
    }
}