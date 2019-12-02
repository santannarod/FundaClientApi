
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace funda.Model
{
    public class ObjectForSale
    {
        public Guid Id { get; set; }
        [JsonProperty("MakelaarId")]
        public int RealEstateAgentId { get; set; }

        [JsonProperty("MakelaarNaam")]
        public string RealStateAgent { get; set; }

        public int Quantity { get; set; }
    }
}
