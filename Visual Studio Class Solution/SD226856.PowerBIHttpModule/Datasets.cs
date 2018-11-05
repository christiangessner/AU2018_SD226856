using Newtonsoft.Json;

namespace SD226856.PowerBIHttpModule
{
    public class Datasets
    {
        [JsonProperty(PropertyName = "value")]
        public Value[] Value { get; set; }
    }
}
