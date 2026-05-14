using System;
using System.Collections.Generic;
using System.Text;

namespace Hidden_Hearts_in_Wonderland.Models
{
    public class Choice
    {
        public string Text { get; set; } = string.Empty;
        public string NextNodeId { get; set; } = string.Empty;
        public int AffectionChange { get; set; }
        public bool IsMiniGame { get; set; }
        public string ExpectedMood { get; set; } = string.Empty;
        public int SceneChoiceIndex { get; set; } = -1;
    }
}
