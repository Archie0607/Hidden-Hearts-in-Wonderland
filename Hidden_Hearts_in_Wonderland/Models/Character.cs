using System;
using System.Collections.Generic;
using System.Text;

namespace Hidden_Hearts_in_Wonderland.Models
{
    public class Character
    {
        public string Name { get; set; }
        public string Image { get; set; }
        public string StartNodeId { get; set; }
        public string PersonalityPrompt { get; set; }
        public int AffectionScore { get; set; }
        public string AffectionText => $"สัมพันธ์: {AffectionScore}";
    }
}

