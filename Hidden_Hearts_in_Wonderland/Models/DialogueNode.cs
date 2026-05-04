
using System;
using System.Collections.Generic;
using System.Text;

namespace Hidden_Hearts_in_Wonderland.Models
{
    public class DialogueNode
    {
        public string Id { get; set; }

        public string CharacterName { get; set; }

        public string Text { get; set; }

        public string Image { get; set; }

        public List<Choice> Choices { get; set; }
    }
}
