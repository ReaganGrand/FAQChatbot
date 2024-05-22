using Newtonsoft.Json;
using System.Collections.Generic;

namespace FAQChatbot.Models
{
    public class Menu
    {
        [JsonProperty("MenuItem")]
        public List<string> MenuItem { get; set; }
    }

    public class ContactUs
    {
        [JsonProperty("ContactUsEmail")]
        public List<string> Email { get; set; }
    }

    public class AppMenu
    {
        [JsonProperty("AppMenuItem")]
        public List<string> AppMenuItem { get; set; }
    }
}
