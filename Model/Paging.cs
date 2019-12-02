
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace funda.Model
{
    public class Paging
    {
        [JsonProperty("AantalPaginas")]
        public int TotalPages { get; set; }

        [JsonProperty("HuidigePagina")]
        public int CurrentPage { get; set; }
    }
}
