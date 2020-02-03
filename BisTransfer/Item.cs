using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BisTransfer
{
    internal class Item
    {
        public Item(int id, string name, SlotEnum slot, Phase phase)
        {
            this.Id = id;
            this.Name = name;
            this.Slot = slot;
            this.Phase = phase;
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public SlotEnum Slot { get; set; }
        public Phase Phase { get; set; }
        public int PhaseNumber { get { return this.Phase.Id; } }
    }
}
