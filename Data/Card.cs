using System;

namespace gameserver.Data
{
    public class Card
    {
        public string ID { get; set; }
        public string Name { get; set; }
        public int Value { get; set; }
        public string Colour { get; set; }

        public Card()
        {
            ID = Guid.NewGuid().ToString("N");
        }

        public bool Valid(Card other)
        {
            bool valid = false;

            if (other == null)
            {
                // first card played
                return true;
            }

            if (Value >= 0 && Value <= 9)
            {
                // value cards only valid if value or colour matches
                if (Value == other.Value || Colour == other.Colour)
                {
                    valid = true;
                }
            }
            else
            {
                if (Colour == "Wild")
                {
                    // can always play a wildcard
                    valid = true;
                }
                else
                {
                    // draw two, skip or reverse
                    if (Colour == other.Colour || Name == other.Name)
                    {
                        valid = true;
                    }
                }
            }
            
            return valid;
        }
    }
}
