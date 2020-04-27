using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sereno.Network.MessageHandler
{
    public class CurrentActionMessage : ServerMessage 
    {
        ///<summary>
        ///The action ID to set as the current action
        ///</summary>
        public int CurrentAction = 0;

        public CurrentActionMessage(ServerType type) : base(type)
        {}

        public override void Push(Int32 value)
        {
            CurrentAction = value;
            base.Push(value);
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
