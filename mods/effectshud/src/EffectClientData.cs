using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace effectshud.src
{
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class EffectClientData
    {
        public string typeId;
        public int duration;
        public int tier;
        public bool infinite;
    }
}
