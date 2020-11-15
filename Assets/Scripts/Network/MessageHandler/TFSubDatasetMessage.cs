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
        /// The structure containing all the data for Merge Transfer Function
        /// </summary>
        public class MergeTF
        {
            /// <summary>
            /// The linear t parameter
            /// </summary>
            public float t = 0.0f;

            /// <summary>
            /// The first transfer function message to interpolate
            /// </summary>
            public TFSubDatasetMessage tf1;

            /// <summary>
            /// The second transfer function message to interpolate
            /// </summary>
            public TFSubDatasetMessage tf2;

            public MergeTF()
            {
                tf1 = new TFSubDatasetMessage(ServerType.GET_TF_DATASET);
                tf2 = new TFSubDatasetMessage(ServerType.GET_TF_DATASET);

                //Skip useless IDs
                tf1.Cursor = 3; 
                tf2.Cursor = 3; 
            }
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
        /// The timestep cursor
        /// </summary>
        public float Timestep;

        /// <summary>
        /// The TFData object being used
        /// </summary>
        private Object m_tfData = null;

        public TFSubDatasetMessage(ServerType type) : base(type)
        { }

        public override byte GetCurrentType()
        {
            if (Cursor <= 2)
                return (byte)'I'; //DataID, SubDataID, HeadsetID

            else if (Cursor == 3 || Cursor == 4)
                return (byte)'b'; //TFID, ColorMode

            else if (Cursor == 5)
                return (byte)'f'; //Timestep

            switch(TFID)
            {
                case TFType.TF_GTF:
                case TFType.TF_TRIANGULAR_GTF:
                {
                    if (Cursor == 6)
                        return (byte)'I'; //length

                    if((Cursor-7)/3 < GTFData.Props.Length)
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

                case TFType.TF_MERGE:
                {
                    if (Cursor == 6)
                        return (byte)'f'; //t

                    if(MergeTFData.tf1.Cursor <= MergeTFData.tf1.GetMaxCursor())
                        return MergeTFData.tf1.GetCurrentType();
                    else if (MergeTFData.tf2.Cursor <= MergeTFData.tf2.GetMaxCursor())
                        return MergeTFData.tf2.GetCurrentType();

                    break;
                }

                default:
                    break;
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
                        if(Cursor == 6)
                            GTFData.Props = new GTFProp[value];
                        else if(Cursor >= 7)
                        {
                            int propID = (Cursor - 7)/3;
                            int offset = (Cursor - 7)%3;
                            if (propID < GTFData.Props.Length && offset == 0)
                                GTFData.Props[propID].PID = value;
                        }
                        break;
                    }
                    case TFType.TF_MERGE:
                    {
                        if (Cursor > 6)
                        {
                            if (MergeTFData.tf1.Cursor <= MergeTFData.tf1.GetMaxCursor())
                                MergeTFData.tf1.Push(value);
                            else if (MergeTFData.tf2.Cursor <= MergeTFData.tf2.GetMaxCursor())
                                MergeTFData.tf2.Push(value);
                        }
                        break;
                    }
                    default:
                        break;
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
                        m_tfData = new GTF();
                        break;
                    }

                    case TFType.TF_MERGE:
                    {
                        m_tfData = new MergeTF();
                        break;
                    }
                    default:
                        break;
                }
            }

            else if(Cursor == 4) //ColorType
                ColorType = (ColorMode)value;

            else
            {
                switch (TFID)
                {
                    case TFType.TF_MERGE:
                    {
                        if(Cursor > 6)
                        {
                            if(MergeTFData.tf1.Cursor <= MergeTFData.tf1.GetMaxCursor())
                                MergeTFData.tf1.Push(value);
                            else if(MergeTFData.tf2.Cursor <= MergeTFData.tf2.GetMaxCursor())
                                MergeTFData.tf2.Push(value);
                        }
                        break;
                    }
                    default:
                        break;
                }
            }

            base.Push(value);
        }

        public override void Push(float value)
        {
            if (Cursor == 5)
                Timestep = value;
            else
            { 
                switch (TFID)
                {
                    case TFType.TF_GTF:
                    case TFType.TF_TRIANGULAR_GTF:
                    {
                        if (Cursor >= 7)
                        {
                            int propID = (Cursor - 7) / 3;
                            int offset = (Cursor - 7) % 3;
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
                        }
                        break;
                    }

                    case TFType.TF_MERGE:
                    {
                        if (Cursor == 6)
                            MergeTFData.t = value;
                        else
                        {
                            if (MergeTFData.tf1.Cursor <= MergeTFData.tf1.GetMaxCursor())
                                MergeTFData.tf1.Push(value);
                            else if (MergeTFData.tf2.Cursor <= MergeTFData.tf2.GetMaxCursor())
                                MergeTFData.tf2.Push(value);
                        }
                        break;
                    }
                    default:
                        break;
                }
            }
            base.Push(value);
        }

        public override Int32 GetMaxCursor()
        {
            int maxCursor = 5;

            switch (TFID)
            {
                case TFType.TF_GTF:
                case TFType.TF_TRIANGULAR_GTF:
                    maxCursor += 1 + 3 * GTFData.Props.Length;
                    break;
                case TFType.TF_MERGE:
                    maxCursor += 1 + MergeTFData.tf1.GetMaxCursor() + MergeTFData.tf2.GetMaxCursor() - 4; //-4 == datasetID + subDatasetID + headsetID for BOTH transfer functions (we ignore them).
                                                                                                          // We remind that the current cursor is included (hence -4 and not -6)
                    break;
                default:
                    break;
            }
            return maxCursor;
        }

        /// <summary>
        /// The Gaussian Transfer Function Data usable only if TFID == TF_GTF or TFID == TF_TRIANGULAR_GTF
        /// </summary>
        public GTF GTFData { get => (GTF)m_tfData; }

        /// <summary>
        /// The Merge Transfer Function Data usable only if TFID == TF_MERGE
        /// </summary>
        public MergeTF MergeTFData { get => (MergeTF)m_tfData; }
    }
}