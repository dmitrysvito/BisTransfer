using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BisTransfer
{
    internal class Character
    {
        public Character(int id, CharacterEnum title, SpecEnum spec = SpecEnum.None)
        {
            this.Id = id;
            this.Title = title;
            this.Spec = spec;
            this.Raw = new List<IList<object>>();
            this.PhaseList = new List<Phase>();
        }

        public int Id { get; set; }
        public CharacterEnum Title { get; set; }
        public SpecEnum Spec { get; set; }
        public List<Item> Items { get; set; }
        public List<Phase> PhaseList { get; set; }
        public IList<IList<object>> Raw { get; internal set; }
    }
}