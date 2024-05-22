using FAQChatbot.Models;
using FAQChatbot.Services;
using Microsoft.Bot.Builder.AI.QnA;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using Microsoft.Bot.Builder.AI.QnA.Models;
using AdaptiveCards;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Newtonsoft.Json.Linq;
using Microsoft.Bot.Schema;
using Serilog;

namespace FAQChatbot.Dialogs
{
    public class CQADialog: CancelAndHelpDialog
    {
        private readonly StateService _stateService;
        private ConversationData conversationData;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _endpointKey;
        private readonly string _hostname;
        private readonly string _knowledgeBaseId;
        private readonly string _defaultWelcome = "Hello and Welcome";
        private readonly bool _enablePreciseAnswer;
        private bool _displayPreciseAnswerOnly;

        public CQADialog(IHttpClientFactory httpClientFactory,IConfiguration configuration,
                        ILogger<CQADialog> logger,NoMatchDialog noMatchDialog,
                        StateService stateService) 
            :base(logger,stateService,nameof(CQADialog))
        {
            _stateService = stateService??throw new ArgumentNullException(nameof(stateService));
            _configuration= configuration??throw new ArgumentNullException(nameof(configuration));

            _httpClientFactory = httpClientFactory??throw new ArgumentNullException(nameof(httpClientFactory));
            const string missingConfigError = "{0} is missing or empty in configuration.";

            _hostname = configuration["LanguageEndpointHostName"];
            if (string.IsNullOrEmpty(_hostname))
            {
                throw new ArgumentException(string.Format(missingConfigError, "LanguageEndpointHostName"));
            }

            _endpointKey = configuration["LanguageEndpointKey"];
            if (string.IsNullOrEmpty(_endpointKey))
            {
                throw new ArgumentException(string.Format(missingConfigError, "LanguageEndpointKey"));
            }

            _knowledgeBaseId = configuration["ProjectName"];
            if (string.IsNullOrEmpty(_knowledgeBaseId))
            {
                throw new ArgumentException(string.Format(missingConfigError, "ProjectName"));
            }

            var welcomeMsg = configuration["DefaultWelcomeMessage"];
            if (!string.IsNullOrWhiteSpace(welcomeMsg))
            {
                _defaultWelcome = welcomeMsg;
            }

            _enablePreciseAnswer = bool.Parse(configuration["EnablePreciseAnswer"]);
            _displayPreciseAnswerOnly = bool.Parse(configuration["DisplayPreciseAnswerOnly"]);

            // This array defines how the Waterfall will execute.
            var waterfallSteps = new WaterfallStep[]
            {
                InitialStepAsync,               
                CallTrainStepAsync,
                CheckForMultiTurnPromptStepAsync,
                DisplayQnAResultStepAsync,
                FinalStepAsync,                
            };

            // Add named dialogs to the DialogSet. These names are saved in the dialog state.
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), waterfallSteps));
            
            AddDialog(noMatchDialog);
            //AddDialog(new NoMatchDialog(stateService));
            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));
            
            InitialDialogId = nameof(WaterfallDialog);
        }
        
        private async Task<DialogTurnResult> InitialStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            DialogTurnResult retValue =null;   
            try{
            
            using var httpClient = _httpClientFactory.CreateClient();
            var qnaClient = CreateCustomQuestionAnsweringClient(httpClient);

            conversationData = await _stateService.ConversationDataAccessor.GetAsync(stepContext.Context, () => new ConversationData(),cancellationToken:cancellationToken);
            conversationData.CurrentQuery = stepContext.Context.Activity.Text;
            await _stateService.ConversationDataAccessor.SetAsync(stepContext.Context,conversationData,cancellationToken:cancellationToken);

            //CustomQuestionAnswering qnaClient = null;


            //qnaClient = UtilityService.QnaReturnsAnswer("LanguageService_ReturnsAnswerWithAnswerSpan.json"); 
            //qnaClient = UtilityService.QnaReturnsAnswer("LanguageService_ReturnsAnswerWithoutContext.json");
            //qnaClient = UtilityService.QnaReturnsAnswer("LanguageService_ReturnsAnswer.json");
            //qnaClient = UtilityService.QnaReturnsAnswer("LanguageService_ReturnAnswer_withPrompts.json");
            //qnaClient = UtilityService.QnaReturnsAnswer("LanguageService_TopNAnswer.json");
            //qnaClient = UtilityService.QnaReturnsAnswer("LanguageService_ReturnsAnswer_WhenNoAnswerFoundInKb.json");

            //var filters = new Filters() { };
            //if(conversationData.SelectedAppMenuOption.ToLower().Equals("aplm"))
            //filters.SourceFilter.Add("APLM User Guide 1_Initiating a DHF Design Document in APLM_Rev O.pdf");
            //filters.MetadataFilter.Metadata.Add(new KeyValuePair<string, string>("source_name_metadata", "APLM"));


            QnAMakerOptions options = new()
            {
                IncludeUnstructuredSources = true,
                Top = 3,
                EnablePreciseAnswer = _enablePreciseAnswer,
                Context = new QnARequestContext() { PreviousQnAId = 0, PreviousUserQuery = string.Empty },
                //Filters = filters,
                QnAId = 0
            };

            if (conversationData.PreviousQnAId != 0)
            {
                options.QnAId = conversationData.CurrentQnAId;
                options.Context = new QnARequestContext() { PreviousQnAId = conversationData.PreviousQnAId, PreviousUserQuery = conversationData.PreviousQuery };

                //Reset
                conversationData = await _stateService.ConversationDataAccessor.GetAsync(stepContext.Context, () => new ConversationData(), cancellationToken);
                conversationData.PreviousQnAId = 0; conversationData.PreviousQuery = string.Empty;
                conversationData.PreviousContextData = null;
                await _stateService.ConversationDataAccessor.SetAsync(stepContext.Context, conversationData, cancellationToken);

                //qnaClient = UtilityService.QnaReturnsAnswer("LanguageService_ReturnAnswer_MultiTurnLevel1.json");
            }

            
            if (conversationData.PreviousQnAId==0)
            {
               
                options = new QnAMakerOptions
                {
                    Top = 3,                    
                    QnAId = 0,
                    //IsTest = false,
                    Context = new QnARequestContext() { PreviousQnAId = 0, PreviousUserQuery = string.Empty },
                    EnablePreciseAnswer = _enablePreciseAnswer,
                    //Filters = filters,                    
                };
                //response= await qnaClient.GetAnswersRawAsync(stepContext.Context, options);

                //qnaClient = UtilityService.QnaReturnsAnswer("LanguageService_ReturnsAnswerWithAnswerSpan.json"); 
                //qnaClient = UtilityService.QnaReturnsAnswer("LanguageService_ReturnsAnswerWithoutContext.json");
                //qnaClient = UtilityService.QnaReturnsAnswer("LanguageService_ReturnsAnswer.json");
                //qnaClient = UtilityService.QnaReturnsAnswer("LanguageService_ReturnAnswer_withPrompts.json");
                //qnaClient = UtilityService.QnaReturnsAnswer("LanguageService_TopNAnswer.json"); 
                //qnaClient = UtilityService.QnaReturnsAnswer("LanguageService_ReturnsAnswer_WhenNoAnswerFoundInKb.json");
            }
            else
            {
                options = new QnAMakerOptions
                { 
                    //Filters = filters,                    
                    Top = 3,
                    EnablePreciseAnswer = _enablePreciseAnswer,
                    RankerType = "Default",
                    QnAId = conversationData.CurrentQnAId,
                    //IsTest = false,
                    Context =new QnARequestContext() { PreviousQnAId = conversationData.PreviousQnAId, PreviousUserQuery= conversationData.PreviousQuery}
                };
                //response = await qnaClient.GetAnswersRawAsync(stepContext.Context, options);

                //qnaClient = UtilityService.QnaReturnsAnswer("LanguageService_ReturnAnswer_MultiTurnLevel1.json");
                //Reset
                conversationData = await _stateService.ConversationDataAccessor.GetAsync(stepContext.Context, ()=> new ConversationData(), cancellationToken);
                conversationData.PreviousQnAId = 0; conversationData.PreviousQuery = string.Empty;
                conversationData.PreviousContextData = null;
                await _stateService.ConversationDataAccessor.SetAsync(stepContext.Context, conversationData, cancellationToken);
            }
            
            var response = await qnaClient.GetAnswersRawAsync(stepContext.Context, options);
            
            // Take this value from GetAnswerResponse 
            var isActiveLearningEnabled = response.ActiveLearningEnabled;
           
            stepContext.Values["QnAData"] = new List<QueryResult>(response.Answers);

            // Check if active learning is enabled.
            // MaximumScoreForLowScoreVariation is the score above which no need to check for feedback.
            if (response.Answers.Any() && response.Answers.First().Score <= (ActiveLearningUtils.MaximumScoreForLowScoreVariation / 100))
            {
                // Get filtered list of the response that support low score variation criteria.
                response.Answers = qnaClient.GetLowScoreVariation(response.Answers);

                if (response.Answers.Length > 1 && isActiveLearningEnabled)
                {
                    var suggestedQuestions = new List<string>();
                    foreach (var qna in response.Answers)
                    {
                        // for unstructured sources questions will be empty
                        if (qna.Questions?.Length > 0)
                        {
                            suggestedQuestions.Add(qna.Questions[0]);
                        }
                    }

                    if (suggestedQuestions.Count > 0)
                    {
                        suggestedQuestions.Add(ConstantService.ActiveLearningCardNoMatchText);                        
                        // Get active learning suggestion card activity.
                        //var message = QnACardBuilder.GetSuggestionsCard(suggestedQuestions, ConstantService.ActiveLearningCardTitle, ConstantService.ActiveLearningCardNoMatchText);
                        var message = UtilityService.GetAdaptivePromptCard(suggestedQuestions, ConstantService.ActiveLearningCardTitle);

                        var text =(Activity)MessageFactory.Attachment(new Attachment
                        {
                            ContentType = AdaptiveCard.ContentType,
                            Content = JObject.FromObject(message),
                        });
                        await stepContext.Context.SendActivityAsync(text, cancellationToken).ConfigureAwait(false);
                        return new DialogTurnResult(DialogTurnStatus.Waiting);
                    }
                }
            }

            var result = new List<QueryResult>();
            if (response.Answers.Any())
            {
                result.Add(response.Answers.First());
            }
            stepContext.Values["QnAData"] = result;
            // If card is not shown, move to next step with top QnA response.
            retValue = await stepContext.NextAsync(result, cancellationToken).ConfigureAwait(false);

            }catch(Exception ex){
                Log.Error("An unhandled exception occured {error}",ex);
            }

            return retValue;      
        }

        private async Task<DialogTurnResult> CallTrainStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            DialogTurnResult retValue = null;
            //var dialogOptions = ObjectPath.GetPathValue<QnAMakerDialogOptions>(stepContext.ActiveDialog.State, Options);
            try{
            
            var trainResponses = stepContext.Values["QnAData"] as List<QueryResult>;

            conversationData = await _stateService.ConversationDataAccessor.GetAsync(stepContext.Context, () => new ConversationData(), cancellationToken);
            var currentQuery = conversationData.CurrentQuery;//stepContext.Values["CurrentQuery"] as string;

            var reply = stepContext.Context.Activity.Text;

            if (trainResponses.Count > 1)
            {
                var qnaResult = trainResponses.FirstOrDefault(kvp => kvp.Questions[0] == reply);

                if (qnaResult != null)
                {
                    stepContext.Values["QnAData"] = new List<QueryResult>() { qnaResult };

                    var records = new FeedbackRecord[]
                    {
                        new() {
                            UserId = stepContext.Context.Activity.Id,
                            UserQuestion = currentQuery,
                            QnaId = qnaResult.Id,
                        }
                    };

                    var feedbackRecords = new FeedbackRecords { Records = records };

                    // Call Active Learning Train API
                    
                    using var httpClient = _httpClientFactory.CreateClient();
                    var qnaClient = CreateCustomQuestionAnsweringClient(httpClient);                   
                    await qnaClient.CallTrainAsync(feedbackRecords).ConfigureAwait(false);

                    retValue = await stepContext.NextAsync(new List<QueryResult>() { qnaResult }, cancellationToken).ConfigureAwait(false);
                }
                else if (reply.Equals(ConstantService.ActiveLearningCardNoMatchText, StringComparison.OrdinalIgnoreCase))
                {
                    //await stepContext.Context.SendActivityAsync(UtilityService.GetAdaptiveCardsText(ConstantService.ActiveLearningCardNoMatchResponse), cancellationToken);
                    var result = new List<QueryResult>();

                    stepContext.Values["QnAData"] = result;
                    // If card is not shown, move to next step with top QnA response.
                    retValue = await stepContext.NextAsync(result, cancellationToken).ConfigureAwait(false);
                    //return await stepContext.EndDialogAsync().ConfigureAwait(false);
                }
                else
                {
                    // restart the waterfall to step 0
                    retValue = await stepContext.NextAsync(stepContext.Result, cancellationToken).ConfigureAwait(false);
                }
            }

            retValue = await stepContext.NextAsync(stepContext.Result, cancellationToken).ConfigureAwait(false);

            }catch(Exception ex){
                Log.Error("An unhandled exception occured {error}",ex);
            }
            return retValue;
        }
        
        private async Task<DialogTurnResult> CheckForMultiTurnPromptStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            DialogTurnResult dialogTurnResult= null;

            try{
            
            if (stepContext.Result is List<QueryResult> response && response.Count > 0)
            {
                // -Check if context is present and prompt exists 
                // -If yes: Add reverse index of prompt display name and its corresponding QnA ID
                // -Set PreviousQnAId as answer.Id
                // -Display card for the prompt
                // -Wait for the reply
                // -If no: Skip to next step

                List<string> promptChoices = [];

                var answer = response.First();
                //stepContext.Values["Prompt"]
                if (answer.Context != null && answer.Context.Prompts?.Length > 0)
                {
                    Dictionary<string,int> contextData = [];
                    foreach (var prompt in answer.Context.Prompts)
                    {
                        promptChoices.Add(prompt.DisplayText);
                       contextData[prompt.DisplayText] = prompt.QnaId;
                    }
                    conversationData = await _stateService.ConversationDataAccessor.GetAsync(stepContext.Context, () => new ConversationData(), cancellationToken:cancellationToken);
                    conversationData.PreviousQnAId = answer.Id;
                    conversationData.PreviousQuery = answer.Questions[0];
                    conversationData.PreviousContextData = contextData;
                    await _stateService.ConversationDataAccessor.SetAsync(stepContext.Context, conversationData, cancellationToken);
                    
                    string resText = answer.Answer;

                    if (!string.IsNullOrWhiteSpace(answer?.AnswerSpan?.Text))
                    {
                        resText = answer.AnswerSpan.Text;
                        //_displayPreciseAnswerOnly = bool.Parse(_configuration["DisplayPreciseAnswerOnly"]);
                        // For content choice Precise only
                        if (_displayPreciseAnswerOnly == false)
                        {
                            resText = answer.Answer;
                        }
                    }

                    var card = UtilityService.GetAdaptivePromptCard(promptChoices, resText);
                    var retryCard = UtilityService.GetAdaptivePromptCard(promptChoices, string.Concat(ConstantService.RetryText,Environment.NewLine,resText));

                    dialogTurnResult = await stepContext.PromptAsync(nameof(ChoicePrompt), new PromptOptions
                    {
                        Prompt = (Activity)MessageFactory.Attachment(new Attachment
                        {
                            ContentType = AdaptiveCard.ContentType,
                            // Convert the AdaptiveCard to a JObject
                            Content = JObject.FromObject(card),
                        }),
                        Choices = ChoiceFactory.ToChoices(promptChoices),
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

                }

            }
            dialogTurnResult =  await stepContext.NextAsync(stepContext.Result, cancellationToken).ConfigureAwait(false);
            }catch(Exception ex){
             Log.Error("An unhandled exception occured {error}",ex);   
            }

            return dialogTurnResult;
        }

        private async Task<DialogTurnResult> DisplayQnAResultStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            DialogTurnResult dialogTurnResult =null;
            try{
            
            var reply = stepContext.Context.Activity.Text;

            if (reply.Equals(ConstantService.ActiveLearningCardNoMatchText, StringComparison.OrdinalIgnoreCase))
            {
                await stepContext.Context.SendActivityAsync(UtilityService.GetAdaptiveCardsText(ConstantService.ActiveLearningCardNoMatchResponse), cancellationToken);

                dialogTurnResult = await stepContext.NextAsync(null,cancellationToken:cancellationToken).ConfigureAwait(false);
            }

            conversationData = await _stateService.ConversationDataAccessor.GetAsync(stepContext.Context, () => new ConversationData(), cancellationToken: cancellationToken);
            if (conversationData.PreviousQnAId > 0)
            {
                string res = ((FoundChoice)stepContext.Result).Value;
                //conversationData.CurrentQnAId = conversationData.previousContextData[stepContext.Result.ToString()];
                conversationData.CurrentQnAId = conversationData.PreviousContextData[res];
                await _stateService.ConversationDataAccessor.SetAsync(stepContext.Context, conversationData, cancellationToken);
                dialogTurnResult = await stepContext.BeginDialogAsync(InitialDialogId, cancellationToken:cancellationToken).ConfigureAwait(false);
            }
            //If response is present then show that response, else default answer.
            var response = (List<QueryResult>)stepContext.Result;
            if (response.Count > 0 && response[0].Id != -1)
            {
                //string resText = response[0].Answer;

                //if (!string.IsNullOrWhiteSpace(response[0].AnswerSpan?.Text))
                //{
                //    resText = response[0].AnswerSpan?.Text;

                //    // For content choice Precise only
                //    if (_displayPreciseAnswerOnly == false)
                //    {
                //        resText = response[0].Answer;
                //    }
                //}
                //_displayPreciseAnswerOnly = bool.Parse(_configuration["DisplayPreciseAnswerOnly"]);
                //var message = QnACardBuilder.GetQnADefaultResponse(response.First(), _displayPreciseAnswerOnly, false);
                //await stepContext.Context.SendActivityAsync(message).ConfigureAwait(false);
                //if (response.Count == 1)
                await stepContext.Context.SendActivityAsync(UtilityService.GetAdaptiveCardResponse(response.First(),_displayPreciseAnswerOnly),cancellationToken:cancellationToken).ConfigureAwait(false);//UtilityService.GetAdaptiveCardsText(response[0].Answer)
                //else
                //await stepContext.Context.SendActivityAsync(UtilityService.GetQnADefaultResponse(response.First(), _displayPreciseAnswerOnly, false), cancellationToken: cancellationToken).ConfigureAwait(false);

                //return await stepContext.NextAsync(null, cancellationToken: cancellationToken).ConfigureAwait(false);

            }
            else
            {
                
                    if (response.Count == 1 && response[0].Id == -1)
                    {
                        // Nomatch Response from service.
                        dialogTurnResult= await stepContext.BeginDialogAsync(nameof(NoMatchDialog), null, cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                    // Empty result array received from service.
                    dialogTurnResult = await stepContext.BeginDialogAsync(nameof(NoMatchDialog), null, cancellationToken).ConfigureAwait(false);
                    }
                //}
                
            }
            dialogTurnResult = await stepContext.NextAsync(null,cancellationToken:cancellationToken).ConfigureAwait(false);            

            }catch(Exception ex){
                Log.Error("An unhandled exception occured {error}",ex);
            }
            return dialogTurnResult;            
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        { 
            DialogTurnResult dialogTurnResult=null;
            try{
                dialogTurnResult = await stepContext.EndDialogAsync(cancellationToken:cancellationToken).ConfigureAwait(false);
            }catch(Exception ex)
            {
                Log.Error("An unhandled exception occured {error}",ex);
            }
            return dialogTurnResult;
        }

        private CustomQuestionAnswering CreateCustomQuestionAnsweringClient(HttpClient httpClient)
        {
            // Create a new Custom Question Answering instance initialized with QnAMakerEndpoint.
            return new CustomQuestionAnswering(new QnAMakerEndpoint
            {
                KnowledgeBaseId = _knowledgeBaseId,
                EndpointKey = _endpointKey,
                Host = _hostname,
                QnAServiceType = ServiceType.Language
            },
           null,
           httpClient);
        }
    }
}
