using System;
using System.Collections.Generic;
using System.Text;

namespace Hidden_Hearts_in_Wonderland.Models
{
    public class PlayerState
    {
        public Dictionary<string, int> AffectionPoints { get; set; } = new();

        public void AddAffection(string character, int amount)
        {
            if (!AffectionPoints.ContainsKey(character))
                AffectionPoints[character] = 0;

            AffectionPoints[character] += amount;
        }

        public int GetAffection(string character)
        {
            return AffectionPoints.ContainsKey(character)
                ? AffectionPoints[character]
                : 0;
        }
    }
}
