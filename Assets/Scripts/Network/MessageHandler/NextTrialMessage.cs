namespace Sereno.Network.MessageHandler
{
    public class NextTrialMessage : ServerMessage
    {
        /// <summary>
        /// Which tangible mode (technique ID) are we?
        /// </summary>
        public int  TangibleMode;

        /// <summary>
        /// What is the current trial ID (i.e., dataset ID. This is to couple with SubTrialID and TangibleMode)
        /// </summary>
        public int  TrialID;

        /// <summary>
        /// In what sub trial are we in the current dataset for the current technique?
        /// </summary>
        public int  SubTrialID;

        /// <summary>
        /// Are we in the training mode? If yes, this trial does not count yet!
        /// </summary>
        public bool InTraining;

        public NextTrialMessage(ServerType type) : base(type)
        {}

        public override void Push(int value)
        {
            if (Cursor == 0)
                TangibleMode = value;
            else if (Cursor == 1)
                TrialID = value;
            else if (Cursor == 2)
                SubTrialID = value;
            base.Push(value);
        }

        public override void Push(byte value)
        {
            if (Cursor == 3)
                InTraining = (value != 0);
            base.Push(value);
        }

        public override byte GetCurrentType()
        {
            if (Cursor <= 2)
                return (byte)'I';
            else if (Cursor == 3)
                return (byte)'b';
            return 0;
        }

        public override int GetMaxCursor()
        {
            return 3;
        }
    }
}