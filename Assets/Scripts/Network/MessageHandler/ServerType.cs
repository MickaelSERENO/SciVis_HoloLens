namespace Sereno.Network.MessageHandler
{
    /// <summary>
    /// The Server message types
    /// </summary>
    public enum ServerType
    {
        GET_ADD_VTK_DATASET   = 0, //Add a new VTK Dataset
        GET_ON_ROTATE_DATASET = 2, //Rotate a dataset
        GET_ON_MOVE_DATASET   = 3, //Move a dataset
        GET_ON_HEADSET_INIT   = 4, //Initialize the headset,
        GET_HEADSETS_STATUS   = 5, //All the headsets status (this one excluded)
        GET_ANCHOR_SEGMENT    = 6, //An anchor segment
        GET_ANCHOR_EOF        = 7, //The anchor EOF signal
        GET_SUBDATASET_OWNER  = 8, //The subdataset modification owner
    }
}