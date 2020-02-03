using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BisTransfer
{
    internal class Phase
    {
        const string sheetName = "Equipment & enchants!";

        public Phase(int id, string range, IList<IList<object>> raw = null)
        {
            this.Id = id;
            this.Range = range;
            if (raw == null)
            {
                this.Raw = new List<IList<object>>();
            }
            else
            {
                this.Raw = raw;
            }            
        }

        public int Id { get; set; }
        public string Range { get; set; }
        public IList<object> CharacterRaw { get; set; }
        public IList<IList<object>> Raw { get; internal set; }

        public string GetPhaseString() {
            return sheetName + this.Range;
        }
    }
}
