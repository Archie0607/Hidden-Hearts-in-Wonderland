using System;
using System.Collections.Generic;
using System.Text;

namespace Hidden_Hearts_in_Wonderland.Models
{
    public class Choice
    {
        public string Text { get; set; }
        public string NextNodeId { get; set; }
        public int AffectionChange { get; set; }
        public bool IsMiniGame { get; set; }
    }
}
