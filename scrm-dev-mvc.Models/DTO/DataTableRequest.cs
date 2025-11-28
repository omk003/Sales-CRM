using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace scrm_dev_mvc.Models.DTO
{
    using System.Text.Json.Serialization;

    public class DataTableRequest
    {
        [JsonPropertyName("draw")]
        public int Draw { get; set; }

        [JsonPropertyName("start")]
        public int Start { get; set; }

        [JsonPropertyName("length")]
        public int Length { get; set; }

        [JsonPropertyName("search")]
        public Search Search { get; set; }

        [JsonPropertyName("order")]
        public List<Order> Order { get; set; }

        [JsonPropertyName("columns")]
        public List<Column> Columns { get; set; }
    }

    public class Search
    {
        [JsonPropertyName("value")]
        public string Value { get; set; }

        [JsonPropertyName("regex")]
        public bool Regex { get; set; } 
    }
    public class Order
    {
        [JsonPropertyName("column")]
        public int Column { get; set; }

        [JsonPropertyName("dir")]
        public string Dir { get; set; }
    }

    public class Column
    {
        [JsonPropertyName("data")]
        public string Data { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("searchable")]
        public bool Searchable { get; set; }

        [JsonPropertyName("orderable")]
        public bool Orderable { get; set; }

        [JsonPropertyName("search")]
        public Search Search { get; set; }
    }
}
