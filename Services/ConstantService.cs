using System.Collections.Generic;

namespace FAQChatbot.Services
{
    public static class ConstantService
    {       
        public const string FAQ = "FAQ", Contactus = "Contact us", GSD = "GSD", Back2MainMenu = "Back to Main Menu",
            NoThanks = "No, Thanks!", APLM = "APLM",IQ = "IQ", SBM = "SBM",NoGoodMatch= "No answers were found. Would you like to send an e-mail to support team?",
            RetryText = "You have selected an invalid options. I can help you with the following options.",
            MoreQuestionText = "Would you like to Ask more questions?",OK="Ok.",
            AppMenuSecondMessageText = $"To go to back to Main Menu, please click on the {Back2MainMenu} button.",
            AppMenuFirstMessageText = $"Please select an application to proceed further. To go to back to Main Menu, please click on the {Back2MainMenu} button.",
            ActiveLearningCardTitle = "Did you mean:", ActiveLearningCardNoMatchText = "None of the above.", ActiveLearningCardNoMatchResponse = "Thanks for the feedback.",
            DefaultNoAnswer = "No answers found.",CancelAndHelp="CancelAndHelp";
    }
}
