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
        GET_ON_SCALE_DATASET  = 9, //Scale a dataset
        GET_TF_DATASET        = 10, /*!< Change the transfer function parameter of a given SubDataset*/
        GET_START_ANNOTATION  = 11, /*!< Start to create an annotation on a specific dataset*/
        GET_ANCHOR_ANNOTATION = 12, /*!< Anchor an annotation on a specific dataset on a specific position*/
        GET_CLEAR_ANNOTATIONS = 13, /*!< Clear all annotations on a specific dataset*/
        GET_ADD_SUBDATASET = 14, /*!< Add a new SubDataset*/
        GET_DEL_SUBDATASET = 15, /*!< Delete a known SubDataset*/
    }
}