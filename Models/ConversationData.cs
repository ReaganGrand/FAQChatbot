using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FAQChatbot.Models
{
    public class ConversationData
    {
        // Track whether we have already asked the user's name
        public bool PromptedUserForName { get; set; } = false;
        public int PreviousQnAId { get; set; }
        public string PreviousQuery { get; set; }
        public Dictionary<string, int> PreviousContextData { get; set; }
        public int CurrentQnAId { get; set; }
        public string SelectedMainMenuOption { get; set; }
        public string SelectedAppMenuOption { get; set; }
        public string CurrentQuery { get; set; }
    }
}
