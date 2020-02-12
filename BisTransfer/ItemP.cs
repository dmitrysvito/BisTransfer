using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BisTransfer
{
    public partial class Item
    {
        public Item()
        {
            this.Slot = SlotEnum.None;
            this.PhaseList = new List<int>();
        }
        public Item(int id, string name, SlotEnum slot = SlotEnum.None)
        {
            this.Id = id;
            this.Name = name;
            this.Slot = slot;
            this.PhaseList = new List<int>();
        }

        [NotMapped]
        public SlotEnum Slot { get; set; }
        [NotMapped]
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
