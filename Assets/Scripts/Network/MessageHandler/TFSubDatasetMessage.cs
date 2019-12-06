using System;
using Sereno.SciVis;

namespace Sereno.Network.MessageHandler
{
    public class TFSubDatasetMessage : ServerMessage
    {
        /// <summary>
        /// The structure the data for Gaussian Transfer Function for a particular property
        /// </summary>
        public struct GTFProp
        {
            /// <summary>
            /// The property ID
            /// </summary>
            public int PID;

            /// <summary>
            /// The center to apply for this property
            /// </summary>
            public float Center;

            /// <summary>
            /// The scale to apply for this property
            /// </summary>
            public float Scale;
        }

        /// <summary>
        /// The structure containing all the data for Gaussian Transfer Function
        /// </summary>
        public class GTF
        {
            /// <summary>
            /// The array containing the data per property
            /// </summary>
            public GTFProp[] Props = new GTFProp[0];
        }

        /// <summary>
        /// The DataID bound to this message
        /// </summary>
        public Int32 DataID;

        /// <summary>
        /// The SubDataID of the DataID which is being rotated
        /// </summary>
        public Int32 SubDataID;

        /// <summary>
        /// The headset ID sending this event (can be us)
        /// </summary>
        public Int32 HeadsetID;

        /// <summary>
        /// The Transfer Function ID.
        /// </summary>
        public TFType TFID = TFType.TF_NOTHING;

        /// <summary>
        /// The color type to use
        /// </summary>
        public ColorMode ColorType;

        /// <summary>
        /// The GTFData object usable only if TFID == TF_GTF or TF_TRIANGULAR_GTF
        /// </summary>
        public GTF GTFData = null;

        public TFSubDatasetMessage(ServerType type) : base(type)
        { }

        public override byte GetCurrentType()
        {
            if (Cursor <= 2)
                return (byte)'I'; //DataID, SubDataID, HeadsetID

            else if (Cursor == 3 || Cursor == 4)
                return (byte)'b'; //TFID, ColorMode

            switch(TFID)
            {
                case TFType.TF_GTF:
                case TFType.TF_TRIANGULAR_GTF:
                {
                    if (Cursor == 5)
                        return (byte)'I'; //length

                    if((Cursor-6)/3 < GTFData.Props.Length)
                    {
                        switch((Cursor-6)%3)
                        {
                            case 0: //PropID
                                return (byte)'I';
                            case 1: //Center
                            case 2: //Scale
                                return (byte)'f';
                        }
                    }
                    break;
                }
            }
            return 0;
        }

        public override void Push(Int32 value)
        {
            if (Cursor == 0)
                DataID = value;
            else if (Cursor == 1)
                SubDataID = value;
            else if (Cursor == 2)
                HeadsetID = value;
            else
            {
                switch(TFID)
                {
                    case TFType.TF_GTF:
                    case TFType.TF_TRIANGULAR_GTF:
                    {
                        if(Cursor == 5)
                            GTFData.Props = new GTFProp[value];
                        else if(Cursor >= 6)
                        {
                            int propID = (Cursor - 6)/3;
                            int offset = (Cursor - 6)%3;
                            if (propID < GTFData.Props.Length && offset == 0)
                                GTFData.Props[propID].PID = value;
                        }
                        break;
                    }
                }
            }
            base.Push(value);
        }

        public override void Push(byte value)
        {
            if(Cursor == 3) //TFID
            {
                TFID = (TFType)value;
                switch(TFID)
                {
                    case TFType.TF_GTF:
                    case TFType.TF_TRIANGULAR_GTF:
                    {
                        GTFData = new GTF();
                        break;
                    }
                }
            }

            else if(Cursor == 4) //ColorType
                ColorType = (ColorMode)value;

            base.Push(value);
        }

        public override void Push(float value)
        {
            if (Cursor >= 6)
            {
                switch (TFID)
                {
                    case TFType.TF_GTF:
                    case TFType.TF_TRIANGULAR_GTF:
                    {
                        int propID = (Cursor - 6) / 3;
                        int offset = (Cursor - 6) % 3;
                        if (propID < GTFData.Props.Length)
                        {
                            switch(offset)
                            {
                                case 1:
                                    GTFData.Props[propID].Center = value;
                                    break;
                                case 2:
                                    GTFData.Props[propID].Scale  = value;
                                    break;
                            }
                        }
                        break;
                    }
                }
            }
            base.Push(value);
        }

        public override Int32 GetMaxCursor()
        {
            int maxCursor = 4;

            switch (TFID)
            {
                case TFType.TF_GTF:
                case TFType.TF_TRIANGULAR_GTF:
                    maxCursor += 1 + 3 * GTFData.Props.Length;
                    break;
            }
            return maxCursor;
        }
    }
}