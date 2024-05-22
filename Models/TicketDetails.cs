using Newtonsoft.Json;

namespace FAQChatbot.Models
{
    public class TicketDetails
    {
        [JsonProperty("issueDescId")]
        public string UserQuestion { get; set; }
        [JsonProperty("emailId")]
        public string UserEmail { get; set; }
        [JsonProperty("phonenumberId")]
        public string UserPhone { get; set; }
    }
}
