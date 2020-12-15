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
    }
}
