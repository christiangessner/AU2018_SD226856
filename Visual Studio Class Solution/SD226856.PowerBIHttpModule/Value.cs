using Newtonsoft.Json;

namespace SD226856.PowerBIHttpModule
{
    public class Value
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }
    }
}