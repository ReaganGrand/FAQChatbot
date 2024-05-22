using AdaptiveCards;
using FAQChatbot.Models;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.QnA.Models;
using Microsoft.Bot.Builder.AI.QnA;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RichardSzalay.MockHttp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using AdaptiveExpressions.Properties;
using static System.Net.Mime.MediaTypeNames;
using Microsoft.Bot.Schema.Teams;
using System.Security.Cryptography;
using System.Text;

namespace FAQChatbot.Services
{
    public static class UtilityService
    {
        public static Activity GetAdaptiveCardsText(string text)
        {
            var card = new AdaptiveCard(new AdaptiveSchemaVersion(1, 0))
            {
                Body = new List<AdaptiveElement>()
                {
                    new AdaptiveTextBlock()
                    {
                        Separator = true,
                        Wrap = true,
                        Color = AdaptiveTextColor.Accent,                        
                        Style = AdaptiveTextBlockStyle.Paragraph,
                        Text = text
                    },
                },
            };

            return (Activity)MessageFactory.Attachment(new Attachment
            {
                ContentType = AdaptiveCard.ContentType,
                Content = JObject.FromObject(card),
            },
            inputHint: InputHints.AcceptingInput);
        }

        public static List<string> GetMenuItem()
        {            
            string[] paths = [".", "Cards", "menuItem.json"];
            var menuJson = File.ReadAllText(Path.Combine(paths),encoding:Encoding.UTF8);
            var menu = JsonConvert.DeserializeObject<Menu>(menuJson);
            return menu.MenuItem;            
        }

        public static List<string> GetAppMenuItem()
        {
            string[] paths = [".", "Cards", "menuItem.json"];
            var menuJson = File.ReadAllText(Path.Combine(paths), encoding: Encoding.UTF8);
            var menu = JsonConvert.DeserializeObject<AppMenu>(menuJson);
            return menu.AppMenuItem;
        }

        public static List<string> GetContactUsEmail()
        {
            string[] paths = [".", "Cards", "menuItem.json"];
            var contactUsEmailJson = File.ReadAllText(Path.Combine(paths), encoding: Encoding.UTF8);
            var contactUs = JsonConvert.DeserializeObject<ContactUs>(contactUsEmailJson);
            return contactUs.Email;
        }

        public static AdaptiveCard GetAdaptivePromptCard(List<string> menuOptions) {
            return new AdaptiveCard(new AdaptiveSchemaVersion(1, 0))
            {
                // Use LINQ to turn the choices into submit actions
                Actions = menuOptions.Select(choice => new AdaptiveSubmitAction
                {
                    Title = choice,
                    Data = choice,  // This will be a string
                }).ToList<AdaptiveAction>(),
            };
        }

        public static AdaptiveCard GetSuggestionAdaptivePromptCard(List<string> menuOptions,string text)
        {
            return new AdaptiveCard(new AdaptiveSchemaVersion(1, 0))
            {
                //Title=text,
                // Use LINQ to turn the choices into submit actions
                Actions = menuOptions.Select(choice => new AdaptiveSubmitAction
                {
                    Title = choice,
                    Data = choice,  // This will be a string
                }).ToList<AdaptiveAction>(),
            };
        }

        public static AdaptiveCard GetAdaptivePromptCard(List<string> menuOptions,string text)
        {
            return new AdaptiveCard(new AdaptiveSchemaVersion(1, 0))
            {                
                Body =
                [
                    new AdaptiveTextBlock()
                    {
                        Separator = true,
                        Wrap = true,
                        Color = AdaptiveTextColor.Accent,
                        Style = AdaptiveTextBlockStyle.Paragraph,
                        Text = text
                    },
                ],
                
                // Use LINQ to turn the choices into submit actions
                Actions = menuOptions.Select(choice => new AdaptiveSubmitAction
                {
                    Title = choice,
                    Data = choice,  // This will be a string
                }).ToList<AdaptiveAction>(),

            };
        }
        
        public static AdaptiveCard GetAdaptivePromptCard(string text)
        {
            return new AdaptiveCard(new AdaptiveSchemaVersion(1, 0))
            {
                Body =
                [
                    new AdaptiveTextBlock()
                    {
                        Separator = true,
                        Wrap = true,
                        Color = AdaptiveTextColor.Accent,
                        Style = AdaptiveTextBlockStyle.Paragraph,
                        Text = text,
                        
                    },                    
                    
                ],
                
            };
        }

        public static IActivity GetAdaptiveCardResponse(QueryResult result, BoolExpression displayPreciseAnswerOnly)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }           

            var card = new AdaptiveCard(new AdaptiveSchemaVersion(1, 0));
            AdaptiveTextBlock adaptiveTextBlock = new AdaptiveTextBlock();
            
            adaptiveTextBlock.Text = result.Answer;
            adaptiveTextBlock.Wrap = true;
            adaptiveTextBlock.Color = AdaptiveTextColor.Accent;
            adaptiveTextBlock.Separator = true;
            adaptiveTextBlock.Style = AdaptiveTextBlockStyle.Paragraph;
            card.Body.Add(adaptiveTextBlock);
            
            
            if (!string.IsNullOrWhiteSpace(result?.AnswerSpan?.Text))
            {
                card.Body.Clear();
                AdaptiveTextBlock answerSpanTextBlock = new AdaptiveTextBlock();
                answerSpanTextBlock.Text = result.AnswerSpan.Text;
                answerSpanTextBlock.Wrap = true;
                answerSpanTextBlock.Color = AdaptiveTextColor.Accent;              

                answerSpanTextBlock.Separator = true;
                answerSpanTextBlock.Style = AdaptiveTextBlockStyle.Paragraph;
                //answerSpanTextBlock.FontType = AdaptiveFontType.Monospace;
                card.Body.Add(answerSpanTextBlock);
                // For content choice Precise only
                if (displayPreciseAnswerOnly.Value == false)
                {
                    AdaptiveTextBlock preciousAnswerTextBlock = new AdaptiveTextBlock();
                    preciousAnswerTextBlock.Text = result.Answer;
                    preciousAnswerTextBlock.Wrap = true;
                    preciousAnswerTextBlock.Color = AdaptiveTextColor.Accent;
                    preciousAnswerTextBlock.Separator = true;
                    preciousAnswerTextBlock.Style = AdaptiveTextBlockStyle.Paragraph;
                    card.Body.Add(preciousAnswerTextBlock);
                }
            }

            return (Activity)MessageFactory.Attachment(new Attachment
            {
                ContentType = AdaptiveCard.ContentType,
                Content = JObject.FromObject(card)
            });
        }

        public static IMessageActivity GetSuggestionsCard(List<string> suggestionsList, string cardTitle, string cardNoMatchText)
        {
            if (suggestionsList == null)
            {
                throw new ArgumentNullException(nameof(suggestionsList));
            }

            if (cardTitle == null)
            {
                throw new ArgumentNullException(nameof(cardTitle));
            }

            if (cardNoMatchText == null)
            {
                throw new ArgumentNullException(nameof(cardNoMatchText));
            }

            var chatActivity = Activity.CreateMessageActivity();
            chatActivity.Text = cardTitle;
            var buttonList = new List<CardAction>();

            // Add all suggestions
            foreach (var suggestion in suggestionsList)
            {
                buttonList.Add(
                    new CardAction()
                    {
                        Value = suggestion,
                        Type = "imBack",
                        Title = suggestion,
                    });
            }

            // Add No match text
            buttonList.Add(
                new CardAction()
                {
                    Value = cardNoMatchText,
                    Type = "imBack",
                    Title = cardNoMatchText
                });

            var plCard = new HeroCard()
            {
                Buttons = buttonList
            };

            // Create the attachment.
            var attachment = plCard.ToAttachment();

            chatActivity.Attachments.Add(attachment);

            return chatActivity;
        }
              
        /// <summary>       
        /// Return a stock Mocked Qna thats loaded with LanguageService_ReturnsAnswer.json
        /// Used for tests that just require any old qna instance.
        /// </summary>
        /// <returns>The <see cref="LanguageService"/>.</returns>
        public static CustomQuestionAnswering QnaReturnsAnswer(string fileName)
        {
             /// <summary>
             /// Defines the _endpointKey.
             /// </summary>
         const string _projectName = "dummy-project";

        /// <summary>
        /// Defines the _endpoint.
        /// </summary>
         const string _endpoint = "https://dummy-hostname.cognitiveservices.azure.com";

        /// <summary>
        /// Defines the api-version.
        /// </summary>
         const string _apiVersion = "2021-10-01";

        /// <summary>
        /// Defines the endpoint key.
        /// </summary>
         const string _endpointKey = "dummy-key";
            //LanguageService_ReturnsAnswer.json
            //LanguageService_ReturnAnswer_withPrompts
            //LanguageService_ReturnsAnswerWithContext
            //LanguageService_ReturnsAnswer_WhenNoAnswerFoundInKb
            //LanguageService_TopNAnswer
            //LanguageService_ReturnsAnswerWithContext1
            //LanguageService_ReturnsAnswerWithoutContext
            // Mock Qna
            var stream = File.OpenRead(Path.Combine(Environment.CurrentDirectory, "TestData/LanguageService", fileName));
            //var stream1 = File.OpenRead(Path.Combine(Environment.CurrentDirectory, "TestData/LanguageService", "LanguageService_ReturnAnswer_MultiTurnLevel1.json"));
            var uri = $"{_endpoint}/language/:query-knowledgebases?projectName={_projectName}&deploymentName=production&api-version={_apiVersion}";
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When(HttpMethod.Post, uri)
                   .Respond("application/json", stream);
           
            //mockHttp.When(HttpMethod.Post, GetRequestUrl()).WithContent("{\"question\":\"Q12\",\"top\":3,\"filters\":{\"MetadataFilter\":{\"Metadata\":[],\"LogicalOperation\":\"AND\"},\"SourceFilter\":[],\"LogicalOperation\":null},\"confidenceScoreThreshold\":0.3,\"context\":{\"previousQnAId\":0,\"previousUserQuery\":\"\"},\"qnaId\":0,\"rankerType\":\"Default\",\"answerSpanRequest\":{\"enable\":true},\"includeUnstructuredSources\":true,\"userId\":\"user1\"}")
            //   .Respond("application/json", GetResponse("LanguageService_ReturnsAnswer_WhenNoAnswerFoundInKb.json"));
            //mockHttp.When(HttpMethod.Post, GetRequestUrl()).WithContent("{\"question\":\"Issues related to KB\",\"top\":3,\"filters\":{\"MetadataFilter\":{\"Metadata\":[],\"LogicalOperation\":\"AND\"},\"SourceFilter\":[],\"LogicalOperation\":null},\"confidenceScoreThreshold\":0.3,\"context\":{\"previousQnAId\":0,\"previousUserQuery\":\"\"},\"qnaId\":0,\"rankerType\":\"Default\",\"answerSpanRequest\":{\"enable\":true},\"includeUnstructuredSources\":true,\"userId\":\"user1\"}")
            //   .Respond("application/json", GetResponse("LanguageService_ReturnAnswer_withPrompts.json"));


            var client = new HttpClient(mockHttp);
            return new CustomQuestionAnswering(new QnAMakerEndpoint
            {
                KnowledgeBaseId = _projectName,
                EndpointKey = _endpointKey,
                Host = _endpoint,
                QnAServiceType = ServiceType.Language
            }, null, client);
            //var qna = GetLanguageService(
            //    mockHttp,
            //    new QnAMakerEndpoint
            //    {
            //        KnowledgeBaseId = _projectName,
            //        EndpointKey = _endpointKey,
            //        Host = _endpoint,
            //        QnAServiceType = ServiceType.Language
            //    },
            //    null
            //    );
            //return qna;
        }


        /// <summary>
        /// Get Card for Default QnA Maker scenario.
        /// </summary>
        /// <param name="result">Result to be dispalyed as prompts.</param>
        /// <param name="displayPreciseAnswerOnly">Choice to render precise answer.</param>
        /// <param name="useTeamsAdaptiveCard">Choose whether to use a Teams-formatted Adaptive card.</param>
        /// <returns>IMessageActivity.</returns>
        public static IMessageActivity GetQnADefaultResponse(QueryResult result, BoolExpression displayPreciseAnswerOnly, BoolExpression useTeamsAdaptiveCard)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            var chatActivity = Activity.CreateMessageActivity();
            chatActivity.Text = result.Answer;

            List<CardAction> buttonList = null;
            if (result?.Context?.Prompts != null &&
                result.Context.Prompts.Any())
            {
                buttonList = new List<CardAction>();

                // Add all prompt
                foreach (var prompt in result.Context.Prompts)
                {
                    buttonList.Add(
                        new CardAction()
                        {
                            Value = prompt.QnaId,
                            Type = "messageBack",
                            Title = prompt.DisplayText,
                            Text = prompt.DisplayText,
                            DisplayText = prompt.DisplayText,
                        });
                }
            }

            string cardText = null;
            if (!string.IsNullOrWhiteSpace(result?.AnswerSpan?.Text))
            {
                chatActivity.Text = result.AnswerSpan.Text;

                // For content choice Precise only
                if (displayPreciseAnswerOnly.Value == false)
                {
                    cardText = result.Answer;
                }
            }
            //else { cardText= result.Answer; }

            if (buttonList != null || !string.IsNullOrWhiteSpace(cardText))
            {
                var useAdaptive = useTeamsAdaptiveCard == null ? false : useTeamsAdaptiveCard.Value;
                var cardAttachment = useAdaptive ? CreateAdaptiveCardAttachment(cardText, buttonList) : CreateHeroCardAttachment(cardText, buttonList);
                //var cardAttachment = CreateAdaptiveCardAttachment(cardText, buttonList);
                chatActivity.Attachments.Add(cardAttachment);
            }

            return chatActivity;
        }


        /// <summary>
        /// Get Card for Default QnA Maker scenario.
        /// </summary>
        /// <param name="result">Result to be dispalyed as prompts.</param>
        /// <param name="displayPreciseAnswerOnly">Choice to render precise answer.</param>
        /// <param name="useTeamsAdaptiveCard">Choose whether to use a Teams-formatted Adaptive card.</param>
        /// <returns>IMessageActivity.</returns>
        public static Activity Testing(QueryResult result, BoolExpression displayPreciseAnswerOnly, BoolExpression useTeamsAdaptiveCard)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            //var chatActivity = Activity.CreateMessageActivity();
            //chatActivity.Text = result.Answer;

            List<CardAction> buttonList = null;
            if (result?.Context?.Prompts != null &&
                result.Context.Prompts.Any())
            {
                buttonList = new List<CardAction>();

                // Add all prompt
                foreach (var prompt in result.Context.Prompts)
                {
                    buttonList.Add(
                        new CardAction()
                        {
                            Value = prompt.QnaId,
                            Type = "messageBack",
                            Title = prompt.DisplayText,
                            Text = prompt.DisplayText,
                            DisplayText = prompt.DisplayText,
                        });
                }
            }

            string cardText = null;
            if (!string.IsNullOrWhiteSpace(result?.AnswerSpan?.Text))
            {
                //chatActivity.Text = result.AnswerSpan.Text;

                // For content choice Precise only
                if (displayPreciseAnswerOnly.Value == false)
                {
                    cardText = result.Answer;
                }
            }
            //else { cardText= result.Answer; }

            AdaptiveCard card = null;

            

            if (buttonList != null || !string.IsNullOrWhiteSpace(cardText))
            {
                card = new AdaptiveCard(new AdaptiveSchemaVersion(1, 0))
                {
                    Body =
                        [
                            new AdaptiveTextBlock()
                            {
                                Separator = true,
                                Wrap = true,
                                Color = AdaptiveTextColor.Accent,
                                Style = AdaptiveTextBlockStyle.Paragraph,
                                Text = (!string.IsNullOrWhiteSpace(cardText) ? cardText : string.Empty)
                            },
                            new AdaptiveTextBlock()
                            {
                                Separator = true,
                                Wrap = true,
                                Color = AdaptiveTextColor.Accent,
                                Style = AdaptiveTextBlockStyle.Paragraph,
                                Text = (!string.IsNullOrWhiteSpace(result.Answer) ? result.Answer : string.Empty)
                            },
                        ],
                    Actions = buttonList.Select(button => new AdaptiveSubmitAction
                    {
                        Title = button.Text,
                        Data = button.Value,
                        //Type = "imBack",
                        //Mode = AdaptiveActionMode.Primary,

                    }).ToList<AdaptiveAction>(),

                };
            }

            // Create and return the card as an attachment
            var adaptiveCard = (Activity)MessageFactory.Attachment(new Attachment()
            {
                ContentType = AdaptiveCard.ContentType,
                Content = JObject.FromObject(card),
            });

            return adaptiveCard;

            //var adaptiveCard = new Attachment()
            //{
            //    ContentType = AdaptiveCard.ContentType,
            //    Content = JObject.FromObject(card),
            //};

            //chatActivity.Attachments.Add(adaptiveCard);

            //return chatActivity;
        }

        /// <summary>
        /// Get a Teams-formatted Adaptive Card as Attachment to be returned in the QnA response. Max width and height of response are controlled by Teams.
        /// </summary>
        /// <param name="cardText">String of text to be added to the card.</param>
        /// <param name="buttonList">List of CardAction representing buttons to be added to the card.</param>
        /// <returns>Attachment.</returns>
        private static Attachment CreateAdaptiveCardAttachment(string cardText, List<CardAction> buttonList)
        {
            // If there are buttons, create an array of buttons for the card.
            // Each button is represented by a Dictionary containing the required fields for each button.
            var cardButtons = buttonList?.Select(button =>
                new Dictionary<string, object>
                {
                    { "type", "Action.Submit" },
                    { "title", button.Title },
                    {
                        "data",
                        new Dictionary<string, object>
                        {
                            {
                                "msteams",
                                new Dictionary<string, object>
                                {
                                    { "type", "messageBack" },
                                    { "displayText", button.DisplayText },
                                    { "text", button.Text },
                                    { "value", button.Value }
                                }
                            }
                        }
                    }
                }).ToArray();

            // Create a dictionary to represent the completed Adaptive card
            // msteams field is also a dictionary
            // body field is an array containing a dictionary
            
            
            var card = new Dictionary<string, object>
            {
                { "$schema", "http://adaptivecards.io/schemas/adaptive-card.json" },
                { "type", "AdaptiveCard" },
                { "version", "1.0" },
                {
                   "msteams",
                   new Dictionary<string, string>
                   {
                       { "width", "full" },
                       { "height", "full" }
                   }
                },
                {
                    "body",
                    new Dictionary<string, string>[]
                    {
                        new Dictionary<string, string>
                        {
                            { "type", "TextBlock" },
                            {"color","Accent" },
                            {"style","Paragraph" },
                            {"wrap","True" },
                            { "text", (!string.IsNullOrWhiteSpace(cardText) ? cardText : string.Empty) }
                        }
                    }
                }
            };

            // If there are buttons, add the buttons array to the card. "actions" must be formatted as an array.
            if (cardButtons != null)
            {
                card.Add("actions", cardButtons);
            }

            // Create and return the card as an attachment
            var adaptiveCard = new Attachment()
            {
                ContentType = "application/vnd.microsoft.card.adaptive",
                Content = card
            };
            return adaptiveCard;
        }

        /// <summary>
        /// Get a Hero Card as Attachment to be returned in the QnA response.
        /// </summary>
        /// <param name="cardText">string of text to be added to the card.</param>
        /// <param name="buttonList">List of CardAction representing buttons to be added to the card.</param>
        /// <returns>Attachment.</returns>
        private static Attachment CreateHeroCardAttachment(string cardText, List<CardAction> buttonList)
        {
            // Create a new hero card, add the text and buttons if they exist
            var card = new HeroCard();

            if (buttonList != null)
            {
                card.Buttons = buttonList;
            }

            if (!string.IsNullOrWhiteSpace(cardText))
            {
                card.Text = cardText;                
            }

            // Return the card as an attachment
            return card.ToAttachment();
        }

        public static string ImageToBase64(string imagePath)
        {
            //string imagePath = @"E:\images\sample.png";
            byte[] imageBytes = File.ReadAllBytes(imagePath);
            string base64String = Convert.ToBase64String(imageBytes);
            return base64String;
        }
    }
}
