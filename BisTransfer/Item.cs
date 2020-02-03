using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BisTransfer
{
    internal class Item
    {
        public Item(int id, string name, SlotEnum slot)
        {
            this.Id = id;
            this.Name = name;
            this.Slot = slot;
            this.PhaseList = new List<int>();
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public SlotEnum Slot { get; set; }
        public IList<int> PhaseList { get; set; }
        public string GetPhaseString()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var phase in this.PhaseList)
            {
                if (this.PhaseList.Last() == phase)
                {
                    sb.Append(phase.ToString());
                }
                else
                {
                    sb.Append(string.Concat(phase.ToString(), ", "));
                }                
            }

            return sb.ToString();
        }
    }
}
