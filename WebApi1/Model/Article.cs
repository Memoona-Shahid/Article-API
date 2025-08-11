using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace WebApi1.Model
{
    public class ArticleData
    {
        [Key]
        public int Id { get; set; }
        
       public string? Title { get; set; }

        public string? Link { get; set; }

        
        public string? Description { get; set; }

        public string? Content { get; set; }
        public string? Image_Url { get; set; }
        
        public string? Source_Name { get; set; }
        
        public string? Source_Url { get; set; }
        public string? Source_Icon { get; set; }
        public string? Language { get; set; }
        public string? Country_First { get; set; }
        public DateTime InsertedAt { get; set; }
      
        [JsonIgnore]public bool Del { get; set; } = false;
    }
}
