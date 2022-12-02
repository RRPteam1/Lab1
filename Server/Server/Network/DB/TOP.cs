using Newtonsoft.Json;

namespace Server.Network.DB
{
    public class TOP
    {
        [JsonProperty("name")]
        public string name { get; set; }

        [JsonProperty("score")]
        public int score { get; set; }

        public override string ToString() => $"{name}\t{score}\n";
    }
}
