// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
//
// Generated with Bot Builder V4 SDK Template for Visual Studio CoreBot v4.18.1

using AdaptiveCards;
using AdaptiveCards.Templating;
using FAQChatbot.Models;
using FAQChatbot.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FAQChatbot.Dialogs
{
    public class TicketDialog : CancelAndHelpDialog
    {
        private const string IssueDescriptionStepMsgText = "Please describe your issue.";
        private const string EmailStepMsgText = "Please provide your e-mail.";
        private const string PhoneNumberStepMsgText = "Please provide your mobile number.";
        private readonly StateService _stateService;
        private readonly ILogger<TicketDialog> logger;
        private ConversationData conversationData;

        public TicketDialog(ILogger<TicketDialog> _logger,StateService stateService):base(_logger,stateService,nameof(TicketDialog))
        {
            _stateService = stateService??throw new ArgumentNullException(nameof(stateService));
            logger =_logger?? throw new ArgumentNullException(nameof(_logger));

            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));
            var waterfallSteps = new WaterfallStep[]
            {                
                TicketFormStepAsync,
                ActStepAsync,
                ConfirmStepAsync,
                FinalStepAsync,
            };
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), waterfallSteps));
            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }
        
        private async Task<DialogTurnResult> TicketFormStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            DialogTurnResult result = null;
            try{
            
            var promptMessage = stepContext.Options?.ToString()??"Please fill below details.";

            await stepContext.Context.SendActivityAsync(UtilityService.GetAdaptiveCardsText(promptMessage), cancellationToken).ConfigureAwait(false);

            conversationData = await _stateService.ConversationDataAccessor.GetAsync(stepContext.Context, () => new ConversationData(), cancellationToken: cancellationToken);

            var paths = new[] { ".","Cards","TicketForm.json"};
            var templateJson = File.ReadAllText(Path.Combine(paths), Encoding.UTF8);
            // Create a Template instance from the template payload
            AdaptiveCardTemplate template = new(templateJson);

            // You can use any serializable object as your data
            var ticketFormData = new
            {
            body= $"We just need a few more details to send an email to {conversationData.SelectedAppMenuOption} support team DL!",
            disclaimer= "Don't worry, we'll never save your information.",
            currentquery = conversationData.CurrentQuery,
            properties = new[]{
                                new {
                                            id = "emailId",
                                            label="Your email",
                                            placeholderText = "Your email",
                                            error="Please enter a valid email address",
                                            validation="^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+[.][A-Za-z0-9-]{2,4}$"},
                                        new {
                                            id = "phonenumberId",
                                            label="Phone Number (xxx-xxx-xxxx)",
                                            placeholderText = "Your Phone number",
                                            error="Invalid phone number. Use the specified format: 3 numbers, hyphen, 3 numbers, hyphen and 4 numbers",
                                            validation="^[0-9]{3}-[0-9]{3}-[0-9]{4}$"}
                               },
            };

            // "Expand" the template - this generates the final Adaptive Card payload
            string cardJson = template.Expand(ticketFormData);

            var adaptiveCardAttachment=MessageFactory.Attachment(new Attachment()
            { 
                Content =JsonConvert.DeserializeObject(cardJson),
                ContentType=AdaptiveCard.ContentType
            
            });
            await stepContext.Context.SendActivityAsync(adaptiveCardAttachment, cancellationToken);

            result = new DialogTurnResult(DialogTurnStatus.Waiting);

            }catch(Exception ex){
                Log.Error("An unhandled exception occured {error}",ex);
            }
            return result;
        }

        private async Task<DialogTurnResult> ActStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            DialogTurnResult result = null;
            try{
                    var txt = stepContext.Context.Activity.Text;
                    string val = stepContext.Context.Activity.Value?.ToString();
                    
                    if (string.IsNullOrEmpty(txt) && val != null)
                    {
                        TicketDetails ticketDetails = JsonConvert.DeserializeObject<TicketDetails>(val);               
                        result = await stepContext.NextAsync(ticketDetails, cancellationToken);
                    }
                    else
                    {
                        result = await stepContext.BeginDialogAsync(InitialDialogId,"Please fill the required information to proceed.", cancellationToken);
                    }

            }catch(Exception ex){
                Log.Error("An unhandled exception occured {error}",ex);
            }
            return result;
        }

        private async Task<DialogTurnResult> ConfirmStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            DialogTurnResult result = null;
            try{
            
            var ticketDetails = (TicketDetails)stepContext.Result;
            List<string> YesorNo = ["Yes", "No"];

            var paths = new[] { ".", "Cards", "TicketDetail.json" };
            var templateJson = File.ReadAllText(Path.Combine(paths), Encoding.UTF8);
            // Create a Template instance from the template payload
            AdaptiveCardTemplate template = new(templateJson);

            // You can use any serializable object as your data
            var ticketFormData = new
            {                
                confirmation = "Ticket detail",
                issuedesc = WebUtility.HtmlDecode(ticketDetails.UserQuestion),
                emailid=ticketDetails.UserEmail,
                phonenumber=ticketDetails.UserPhone,
            };

            // "Expand" the template - this generates the final Adaptive Card payload
            string cardJson = template.Expand(ticketFormData);

            var adaptiveCardAttachment = MessageFactory.Attachment(new Attachment()
            {
                Content = JsonConvert.DeserializeObject(cardJson),
                ContentType = AdaptiveCard.ContentType

            });

            await stepContext.Context.SendActivityAsync(adaptiveCardAttachment, cancellationToken);
            //return new DialogTurnResult(DialogTurnStatus.Waiting);
            var messageText = $"Please review the ticket detail and confirm, I have you sending e-mail.";
            var card = UtilityService.GetAdaptivePromptCard(YesorNo,messageText);
            var retryCard = UtilityService.GetAdaptivePromptCard(YesorNo, ConstantService.RetryText);

            result = await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions
            {
                Prompt = (Activity)MessageFactory.Attachment(new Attachment
                {
                    ContentType = AdaptiveCard.ContentType,
                    Content = JObject.FromObject(card),

                }),
                Choices = ChoiceFactory.ToChoices(YesorNo),
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

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            DialogTurnResult result = null;
            try{
                if ((bool)stepContext.Result)
                {
                    var messageText = $"I have you sent an e-mail to support team";
                    await stepContext.Context.SendActivityAsync(UtilityService.GetAdaptiveCardsText(messageText), cancellationToken);
                    result = await stepContext.EndDialogAsync(null, cancellationToken);
                }
                else
                {                
                    result = await stepContext.ReplaceDialogAsync(InitialDialogId,"Please correct the details.",cancellationToken);
                }
            }catch(Exception ex){
                Log.Error("An unhandled exception occured {error}",ex);
            }
            return result;          
        }
    }
}
