using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sereno.Network.MessageHandler
{
    public class CurrentActionMessage : ServerMessage 
    {
        public int CurrentAction = 0;

        public CurrentActionMessage(ServerType type) : base(type)
        {}

        public override void Push(Int32 value)
        {
            CurrentAction = value;
        }

        public override byte GetCurrentType()
        {
            return (byte)'I';
        }

        public override int GetMaxCursor()
        {
            return 0;
        }
    }
}
