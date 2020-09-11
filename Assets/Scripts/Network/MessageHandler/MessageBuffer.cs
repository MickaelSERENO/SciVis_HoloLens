using System.ComponentModel;
using System.IO;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Sereno.Network.MessageHandler
{
    /// <summary>
    /// Callback interface used when new messages have been parsed
    /// </summary>
    public interface IMessageBufferCallback
    {
        /// <summary>
        /// Function called when the server has finished initializing this headset
        /// </summary>
        /// <param name="messageBuffer">The message buffer in use</param>
        /// <param name="msg">The message parsed containing initial values of this headset</param>
        void OnHeadsetInit(MessageBuffer messageBuffer, HeadsetInitMessage msg);

        /// <summary>
        /// Function called when an empty message has been received
        /// </summary>
        /// <param name="messageBuffer">The message buffer in use</param>
        /// <param name="msg">The message parsed. Only Type is useful</param>
        void OnEmptyMessage(MessageBuffer messageBuffer, EmptyMessage msg);

        /// <summary>
        /// Function called when a VTK Dataset message has been received
        /// </summary>
        /// <param name="messageBuffer">The message buffer in use</param>
        /// <param name="msg">The message parsed containing information to parse a VTK Dataset</param>
        void OnAddVTKDataset(MessageBuffer messageBuffer, AddVTKDatasetMessage msg);

        /// <summary>
        /// Function called when a Cloud Point Dataset message has been received
        /// </summary>
        /// <param name="messageBuffer">The message buffer in use</param>
        /// <param name="msg">The message parsed containing information to parse a Cloud Point Dataset</param>
        void OnAddCloudPointDataset(MessageBuffer messageBuffer, AddCloudDatasetMessage msg);

        /// <summary>
        /// Function called when a Rotation message has been parsed
        /// </summary>
        /// <param name="messageBuffer">The message buffer in use</param>
        /// <param name="msg">The message parsed containing information to update the dataset along the rotation</param>
        void OnRotateDataset(MessageBuffer messageBuffer, RotateDatasetMessage msg);

        /// <summary>
        /// Function called when a Move message has been parsed
        /// </summary>
        /// <param name="messageBuffer">The message buffer in use</param>
        /// <param name="msg">The message parsed containing information to update the dataset position</param>
        void OnMoveDataset(MessageBuffer messageBuffer, MoveDatasetMessage msg);

        /// <summary>
        /// Function called when a Scale message has been parsed
        /// </summary>
        /// <param name="messageBuffer">The message buffer in use</param>
        /// <param name="msg">The message parsed containing information to update the dataset scaling</param>
        void OnScaleDataset(MessageBuffer messageBuffer, ScaleDatasetMessage msg);

        /// <summary>
        /// Function called when a Transfer Function message has been parsed
        /// </summary>
        /// <param name="messageBuffer">The message buffer in use</param>
        /// <param name="msg">The message parsed containing information to update the dataset scaling</param>
        void OnTFDataset(MessageBuffer messageBuffer, TFSubDatasetMessage msg);

        /// <summary>
        /// Function called when new headsets status have been received
        /// </summary>
        /// <param name="messageBuffer">The message buffer in use</param>
        /// <param name="msg">The message parsed containing information to update all the known headsets</param>
        void OnHeadsetsStatus(MessageBuffer messageBuffer, HeadsetsStatusMessage msg);

        /// <summary>
        /// Function called when a new byte array have been received
        /// </summary>
        /// <param name="messageBuffer">The message buffer in use</param>
        /// <param name="msg">The message parsed containing the byte array to parse</param>
        void OnDefaultByteArray(MessageBuffer messageBuffer, DefaultByteArray msg);

        /// <summary>
        /// Function called when a new lock owner has been designed for a SudDataset
        /// </summary>
        /// <param name="messageBuffer">The message buffer in use</param>
        /// <param name="msg">The message parsed containing the owner information</param>
        void OnSubDatasetModificationOwner(MessageBuffer messageBuffer, SubDatasetModificationOwnerMessage msg);
        
        /// <summary>
        /// Function called when a start annotation command has been submited
        /// </summary>
        /// <param name="messageBuffer">The message buffer in use</param>
        /// <param name="msg">The message parsed containing information about the pointing interaction technique to use and the dataset to manipulate</param>
        void OnStartAnnotation(MessageBuffer messageBuffer, StartAnnotationMessage msg);

        /// <summary>
        /// Function called when a new anchored annotation command has been submited
        /// </summary>
        /// <param name="messageBuffer">The message buffer in use</param>
        /// <param name="msg">The message parsed</param>
        void OnAnchorAnnotation(MessageBuffer messageBuffer, AnchorAnnotationMessage msg);

        /// <summary>
        /// Function called when a new clear annotations command has been submited
        /// </summary>
        /// <param name="messageBuffer">The message buffer in use</param>
        /// <param name="msg">The message parsed</param>
        void OnClearAnnotations(MessageBuffer messageBuffer, ClearAnnotationsMessage msg);

        /// <summary>
        /// Function called when a new add subdataset command has been submited
        /// </summary>
        /// <param name="messageBuffer">The message buffer in use</param>
        /// <param name="msg">The message parsed</param>
        void OnAddSubDataset(MessageBuffer messageBuffer, AddSubDatasetMessage msg);

        /// <summary>
        /// Function called when the remove a known subdataset command has been submited
        /// </summary>
        /// <param name="messageBuffer">The message buffer in use</param>
        /// <param name="msg">The message parsed</param>
        void OnRemoveSubDataset(MessageBuffer messageBuffer, RemoveSubDatasetMessage msg);

        /// <summary>
        /// Function called when the set subdataset owner command has been submited
        /// </summary>
        /// <param name="messageBuffer">The message buffer in use</param>
        /// <param name="msg">The message parsed</param>
        void OnSubDatasetOwner(MessageBuffer messageBuffer, SubDatasetOwnerMessage msg);

        /// <summary>
        /// Function called when the current action has changed
        /// </summary>
        /// <param name="messageBuffer">The message buffer in use</param>
        /// <param name="msg">The message parsed</param>
        void OnCurrentAction(MessageBuffer messageBuffer, CurrentActionMessage msg);

        /// <summary>
        /// Function called when the tablet's location changes
        /// </summary>
        /// <param name="messageBuffer">The message buffer in use</param>
        /// <param name="msg">The message parsed</param>
        void OnLocation(MessageBuffer messageBuffer, LocationMessage msg);

        /// <summary>
        /// Function called when the tablet scale is received
        /// </summary>
        /// <param name="messageBuffer">The message buffer in use</param>
        /// <param name="msg">The message parsed</param>
        void OnTabletScale(MessageBuffer messageBuffer, TabletScaleMessage msg);

        /// <summary>
        /// Function called when a lasso is received
        /// </summary>
        /// <param name="messageBuffer">The message buffer in use</param>
        /// <param name="msg">The message parsed</param>
        void OnLasso(MessageBuffer messageBuffer, LassoMessage msg);

        /// <summary>
        /// Function called when the tablet confirms the selection
        /// </summary>
        /// <param name="messageBuffer">The message buffer in use</param>
        /// <param name="msg">The message parsed</param>
        void OnConfirmSelection(MessageBuffer messageBuffer, ConfirmSelectionMessage msg);

        /// <summary>
        /// Function called when a new selection input has to be performed
        /// </summary>
        /// <param name="messageBuffer">The message buffer in use</param>
        /// <param name="msg">The message parsed</param>
        void OnAddNewSelectionInput(MessageBuffer messageBuffer, AddNewSelectionInputMessage msg);

        /// <summary>
        /// Function called to toggle a map visibility
        /// </summary>
        /// <param name="messageBuffer">The message buffer in use</param>
        /// <param name="msg">The message parsed</param>
        void OnToggleMapVisibility(MessageBuffer messageBuffer, ToggleMapVisibilityMessage msg);

        /// <summary>
        /// Function called to reset a volumetric selection
        /// </summary>
        /// <param name="messageBuffer">The message buffer in use</param>
        /// <param name="msg">The message parsed</param>
        void OnResetVolumetricSelection(MessageBuffer messageBuffer, ResetVolumetricSelectionMessage msg);
    }

    /// <summary>
    /// MessageBuffer class. Parsed the messages received
    /// </summary>
    public class MessageBuffer
    {
        /// <summary>
        /// Position along the next byte array to parse
        /// </summary>
        private int    m_byteArrayPos = -1;

        /// <summary>
        /// The byte array getting parsed
        /// </summary>
        private byte[] m_byteArray    = null;

        /// <summary>
        /// The current message being parsed
        /// </summary>
        private ServerMessage m_msg = null;

        /// <summary>
        /// The data array in construction to parse either a short, an integer or a float
        /// </summary>
        private byte[] m_data = new byte[4]; //Maximum 32 bits

        /// <summary>
        /// The current position of the in-construction m_data array
        /// </summary>
        private int    m_dataPos = 0; 
        
        /// <summary>
        /// List of Listeners to call
        /// </summary>
        private List<IMessageBufferCallback> m_listeners = new List<IMessageBufferCallback>();

        /// <summary>
        /// Constructor
        /// </summary>
        public MessageBuffer()
        {}

        /// <summary>
        /// Add a new listener to call when messages are received
        /// </summary>
        /// <param name="clbk">The callback listener to call</param>
        public void AddListener(IMessageBufferCallback clbk)
        {
            m_listeners.Add(clbk);
        }

        /// <summary>
        /// Remove an already registered listener
        /// </summary>
        /// <param name="clbk">The listener to not call anymore</param>
        public void RemoveListener(IMessageBufferCallback clbk)
        {
            m_listeners.Remove(clbk);
        }

        /// <summary>
        /// Push new incoming data. If a message is successfully parsed, called all the listeners
        /// </summary>
        /// <param name="buf">The buffer data</param>
        public void Push(byte[] buf)
        {
            int bufPos = 0;
            while(bufPos < buf.Length)
            {
                //Get the message type
                if(m_msg == null)
                {
                    if(!ReadInt16(buf, bufPos, out Int16 type, out bufPos))
                        return;
                    AllocateData(type);
                }

                //Read the buffer and send the correct type to the associated servermessage
                while(m_msg != null && m_msg.GetMaxCursor() >= m_msg.Cursor && bufPos < buf.Length)
                {
                    switch(m_msg.GetCurrentType())
                    {
                        case (byte)'s':
                        {
                            if(!ReadString(buf, bufPos, out String s, out bufPos))
                                return;
                            m_msg.Push(s);
                            break;
                        }
                        case (byte)'I':
                        {
                            if(!ReadInt32(buf, bufPos, out Int32 i, out bufPos))
                                return;
                            m_msg.Push(i);
                            break;
                        }
                        case (byte)'i':
                        {
                            if(!ReadInt16(buf, bufPos, out Int16 i, out bufPos))
                                return;
                            m_msg.Push(i);
                            break;
                        }
                        case (byte)'f':
                        {
                            if(!ReadFloat(buf, bufPos, out float f, out bufPos))
                                return;
                            m_msg.Push(f);
                            break;
                        }
                        case (byte)'b':
                        {
                            m_msg.Push(buf[bufPos]);
                            bufPos++;
                            break;
                        }
                        case (byte)'a':
                        {
                            if(!ReadByteArray(buf, bufPos, out byte[] arr, out bufPos))
                                return;
                            m_msg.Push(arr);
                            break;
                        }
                    }
                }

                //Call the listeners
                if(m_msg != null && m_msg.GetMaxCursor() < m_msg.Cursor)
                {
                    switch(m_msg.Type)
                    {
                        case ServerType.GET_ON_HEADSET_INIT:
                            foreach(var l in m_listeners)
                                l.OnHeadsetInit(this, (HeadsetInitMessage)m_msg);
                            break;
                        case ServerType.GET_ADD_VTK_DATASET:
                            foreach(var l in m_listeners)
                                l.OnAddVTKDataset(this, (AddVTKDatasetMessage)m_msg);
                            break;
                        case ServerType.GET_ON_ROTATE_DATASET:
                            foreach(var l in m_listeners)
                                l.OnRotateDataset(this, (RotateDatasetMessage)m_msg);
                            break;
                        case ServerType.GET_ON_MOVE_DATASET:
                            foreach(var l in m_listeners)
                                l.OnMoveDataset(this, (MoveDatasetMessage)m_msg);
                            break;
                        case ServerType.GET_HEADSETS_STATUS:
                            foreach(var l in m_listeners)
                                l.OnHeadsetsStatus(this, (HeadsetsStatusMessage)m_msg);
                            break;
                        case ServerType.GET_ANCHOR_SEGMENT:
                            foreach(var l in m_listeners)
                                l.OnDefaultByteArray(this, (DefaultByteArray)m_msg);
                            break;
                        case ServerType.GET_ANCHOR_EOF:
                            foreach(var l in m_listeners)
                                l.OnEmptyMessage(this, (EmptyMessage)m_msg);
                            break;
                        case ServerType.GET_SUBDATASET_MODIFICATION_OWNER:
                            foreach(var l in m_listeners)
                                l.OnSubDatasetModificationOwner(this, (SubDatasetModificationOwnerMessage)m_msg);
                            break;
                        case ServerType.GET_ON_SCALE_DATASET:
                            foreach(var l in m_listeners)
                                l.OnScaleDataset(this, (ScaleDatasetMessage)m_msg);
                            break;
                        case ServerType.GET_TF_DATASET:
                            foreach (var l in m_listeners)
                                l.OnTFDataset(this, (TFSubDatasetMessage)m_msg);
                            break;
                        case ServerType.GET_START_ANNOTATION:
                            foreach (var l in m_listeners)
                                l.OnStartAnnotation(this, (StartAnnotationMessage)m_msg);
                            break;
                        case ServerType.GET_ANCHOR_ANNOTATION:
                            foreach (var l in m_listeners)
                                l.OnAnchorAnnotation(this, (AnchorAnnotationMessage)m_msg);
                            break;
                        case ServerType.GET_CLEAR_ANNOTATIONS:
                            foreach (var l in m_listeners)
                                l.OnClearAnnotations(this, (ClearAnnotationsMessage)m_msg);
                            break;
                        case ServerType.GET_ADD_SUBDATASET:
                            foreach (var l in m_listeners)
                                l.OnAddSubDataset(this, (AddSubDatasetMessage)m_msg);
                            break;
                        case ServerType.GET_DEL_SUBDATASET:
                            foreach (var l in m_listeners)
                                l.OnRemoveSubDataset(this, (RemoveSubDatasetMessage)m_msg);
                            break;
                        case ServerType.GET_SUBDATASET_OWNER:
                            foreach (var l in m_listeners)
                                l.OnSubDatasetOwner(this, (SubDatasetOwnerMessage)m_msg);
                            break;
                        case ServerType.GET_CURRENT_ACTION:
                            foreach(var l in m_listeners)
                                l.OnCurrentAction(this, (CurrentActionMessage)m_msg);
                            break;
                        case ServerType.GET_TABLET_LOCATION:
                            foreach(var l in m_listeners)
                                l.OnLocation(this, (LocationMessage)m_msg);
                            break;
                        case ServerType.GET_TABLET_SCALE:
                            foreach(var l in m_listeners)
                                l.OnTabletScale(this, (TabletScaleMessage)m_msg);
                            break;
                        case ServerType.GET_LASSO:
                            foreach(var l in m_listeners)
                                l.OnLasso(this, (LassoMessage)m_msg);
                            break;
                        case ServerType.GET_CONFIRM_SELECTION:
                            foreach(var l in m_listeners)
                                l.OnConfirmSelection(this, (ConfirmSelectionMessage)m_msg);
                            break;
                        case ServerType.GET_ADD_CLOUD_POINT_DATASET:
                            foreach(var l in m_listeners)
                                l.OnAddCloudPointDataset(this, (AddCloudDatasetMessage)m_msg);
                            break;
                        case ServerType.GET_ADD_NEW_SELECTION_INPUT:
                            foreach(var l in m_listeners)
                                l.OnAddNewSelectionInput(this, (AddNewSelectionInputMessage)m_msg);
                            break;
                        case ServerType.GET_TOGGLE_MAP_VISIBILITY:
                            foreach (var l in m_listeners)
                                l.OnToggleMapVisibility(this, (ToggleMapVisibilityMessage)m_msg);
                            break;
                        case ServerType.GET_RESET_VOLUMETRIC_SELECTION:
                            foreach (var l in m_listeners)
                                l.OnResetVolumetricSelection(this, (ResetVolumetricSelectionMessage)m_msg);
                            break;
                        default:
                            break;
                    }
                    m_msg = null;
                }
            }
        }

        /// <summary>
        /// Read a Int16 from the current buffer
        /// </summary>
        /// <param name="data">The incoming buffer</param>
        /// <param name="offset">Offset of the buffer</param>
        /// <param name="val">The value parsed (if any)</param>
        /// <param name="newOff">The new offset to apply</param>
        /// <returns>true if enough data was pushed to parsed a Int16 value, false otherwise</returns>
        private bool ReadInt16(byte[] data, int offset, out Int16 val, out int newOff)
        {
            newOff = offset;
            val = 0;
            if(data.Length+m_dataPos - offset < 2)
            {
                for(int i = offset; i < data.Length; i++, newOff++)
                    m_data[m_dataPos++] = data[i];
                return false;
            }

            for(; m_dataPos < 2; newOff++)
                m_data[m_dataPos++] = data[newOff];

            val = (Int16)((m_data[0] << 8) + 
                          (m_data[1]));
            m_dataPos = 0;
            return true;
        }

        /// <summary>
        /// Read a Int32 from the current buffer
        /// </summary>
        /// <param name="data">The incoming buffer</param>
        /// <param name="offset">Offset of the buffer</param>
        /// <param name="val">The value parsed (if any)</param>
        /// <param name="newOff">The new offset to apply</param>
        /// <returns>true if enough data was pushed to parsed a Int32 value, false otherwise</returns>
        private bool ReadInt32(byte[] data, int offset, out Int32 val, out int newOff)
        {
            val = 0;
            newOff = offset;
            if(data.Length+m_dataPos - offset < 4)
            {
                for(int i = offset; i < data.Length; i++, newOff++)
                    m_data[m_dataPos++] = data[i];
                return false;
            }

            for(; m_dataPos < 4; newOff++)
                m_data[m_dataPos++] = data[newOff];

            val = (Int32)(((m_data[0] & 0xff) << 24) + 
                          ((m_data[1] & 0xff) << 16) +
                          ((m_data[2] & 0xff) << 8)  + 
                          (m_data[3] & 0xff));
            m_dataPos = 0;
            return true;
        }

        /// <summary>
        /// Read a float from the current buffer
        /// </summary>
        /// <param name="data">The incoming buffer</param>
        /// <param name="offset">Offset of the buffer</param>
        /// <param name="val">The value parsed (if any)</param>
        /// <param name="newOff">The new offset to apply</param>
        /// <returns>true if enough data was pushed to parsed a float value, false otherwise</returns>
        private bool ReadFloat(byte[] data, int offset, out float val, out int newOff)
        {
            val = 0;
            newOff = offset;
            Int32 d;
            if(!ReadInt32(data, offset, out d, out newOff))
                return false;
            unsafe
            {
                val = *(float*)&d;
                return true;
            }
        }

        /// <summary>
        /// Read a string from the current buffer
        /// </summary>
        /// <param name="data">The incoming buffer</param>
        /// <param name="offset">Offset of the buffer</param>
        /// <param name="val">The value parsed (if any)</param>
        /// <param name="newOff">The new offset to apply</param>
        /// <returns>true if enough data was pushed to parsed a string value, false otherwise</returns>
        private bool ReadString(byte[] data, int offset, out String s, out Int32 newOffset)
        {
            s         = "";
            if(!ReadByteArray(data, offset, out byte[] stringData, out newOffset))
                return false;
            
            s = System.Text.Encoding.ASCII.GetString(stringData);
            return true;
        }

        /// <summary>
        /// Read a byte array from the current buffer
        /// </summary>
        /// <param name="data">The incoming buffer</param>
        /// <param name="offset">Offset of the buffer</param>
        /// <param name="val">The value parsed (if any)</param>
        /// <param name="newOffset">The new offset to apply</param>
        /// <returns>true if enough data was pushed to parsed a byte array value, false otherwise</returns>
        private unsafe bool ReadByteArray(byte[] data, int offset, out byte[] arr, out Int32 newOffset)
        {
            newOffset = offset;
            arr       = null;

            if(m_byteArray == null) //Read the size component
            {
                if(!ReadInt32(data, newOffset, out m_byteArrayPos, out newOffset))
                    return false;
                m_byteArray    = new byte[m_byteArrayPos];
                m_byteArrayPos = 0;
            }

            fixed(byte* pData = data, pByteArray = m_byteArray)
            {
                for(; m_byteArrayPos < m_byteArray.Length && newOffset < data.Length; m_byteArrayPos++, newOffset++)
                    pByteArray[m_byteArrayPos] = pData[newOffset];
            }

            if(m_byteArrayPos == m_byteArray.Length)
            {
                arr = m_byteArray;
                m_byteArray = null;
                return true;
            }
            
            return false;
        }

        /// <summary>
        /// Allocate the new current message
        /// </summary>
        /// <param name="type">The type of the message</param>
        private void AllocateData(Int16 type)
        {
            switch((ServerType)type)
            {
                case ServerType.GET_ON_HEADSET_INIT:
                    m_msg = new HeadsetInitMessage(ServerType.GET_ON_HEADSET_INIT);
                    break;
                case ServerType.GET_ADD_VTK_DATASET:
                    m_msg = new AddVTKDatasetMessage(ServerType.GET_ADD_VTK_DATASET);
                    break;
                case ServerType.GET_ON_ROTATE_DATASET:
                    m_msg = new RotateDatasetMessage(ServerType.GET_ON_ROTATE_DATASET);
                    break;
                case ServerType.GET_ON_MOVE_DATASET:
                    m_msg = new MoveDatasetMessage(ServerType.GET_ON_MOVE_DATASET);
                    break;
                case ServerType.GET_HEADSETS_STATUS:
                    m_msg = new HeadsetsStatusMessage(ServerType.GET_HEADSETS_STATUS);
                    break;
                case ServerType.GET_ANCHOR_SEGMENT:
                    m_msg = new DefaultByteArray(ServerType.GET_ANCHOR_SEGMENT);
                    break;
                case ServerType.GET_ANCHOR_EOF:
                    m_msg = new EmptyMessage(ServerType.GET_ANCHOR_EOF);
                    break;
                case ServerType.GET_SUBDATASET_MODIFICATION_OWNER:
                    m_msg = new SubDatasetModificationOwnerMessage(ServerType.GET_SUBDATASET_MODIFICATION_OWNER);
                    break;
                case ServerType.GET_ON_SCALE_DATASET:
                    m_msg = new ScaleDatasetMessage(ServerType.GET_ON_SCALE_DATASET);
                    break;
                case ServerType.GET_TF_DATASET:
                    m_msg = new TFSubDatasetMessage(ServerType.GET_TF_DATASET);
                    break;
                case ServerType.GET_START_ANNOTATION:
                    m_msg = new StartAnnotationMessage(ServerType.GET_START_ANNOTATION);
                    break;
                case ServerType.GET_ANCHOR_ANNOTATION:
                    m_msg = new AnchorAnnotationMessage(ServerType.GET_ANCHOR_ANNOTATION);
                    break;
                case ServerType.GET_CLEAR_ANNOTATIONS:
                    m_msg = new ClearAnnotationsMessage(ServerType.GET_CLEAR_ANNOTATIONS);
                    break;
                case ServerType.GET_ADD_SUBDATASET:
                    m_msg = new AddSubDatasetMessage(ServerType.GET_ADD_SUBDATASET);
                    break;
                case ServerType.GET_DEL_SUBDATASET:
                    m_msg = new RemoveSubDatasetMessage(ServerType.GET_DEL_SUBDATASET);
                    break;
                case ServerType.GET_SUBDATASET_OWNER:
                    m_msg = new SubDatasetOwnerMessage(ServerType.GET_SUBDATASET_OWNER);
                    break;
                case ServerType.GET_CURRENT_ACTION:
                    m_msg = new CurrentActionMessage(ServerType.GET_CURRENT_ACTION);
                    break;
                case ServerType.GET_TABLET_LOCATION:
                    m_msg = new LocationMessage(ServerType.GET_TABLET_LOCATION);
                    break;
                case ServerType.GET_TABLET_SCALE:
                    m_msg = new TabletScaleMessage(ServerType.GET_TABLET_SCALE);
                    break;
                case ServerType.GET_LASSO:
                    m_msg = new LassoMessage(ServerType.GET_LASSO);
                    break;
                case ServerType.GET_CONFIRM_SELECTION:
                    m_msg = new ConfirmSelectionMessage(ServerType.GET_CONFIRM_SELECTION);
                    break;
                case ServerType.GET_ADD_CLOUD_POINT_DATASET:
                    m_msg = new AddCloudDatasetMessage(ServerType.GET_ADD_CLOUD_POINT_DATASET);
                    break;
                case ServerType.GET_ADD_NEW_SELECTION_INPUT:
                    m_msg = new AddNewSelectionInputMessage(ServerType.GET_ADD_NEW_SELECTION_INPUT);
                    break;
                case ServerType.GET_TOGGLE_MAP_VISIBILITY:
                    m_msg = new ToggleMapVisibilityMessage(ServerType.GET_TOGGLE_MAP_VISIBILITY);
                    break;
                case ServerType.GET_RESET_VOLUMETRIC_SELECTION:
                    m_msg = new ResetVolumetricSelectionMessage(ServerType.GET_RESET_VOLUMETRIC_SELECTION);
                    break;
            }
        }
    }
}