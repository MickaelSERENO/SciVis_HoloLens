namespace Sereno.Network.MessageHandler
{
    /// <summary>
    /// The Server message types
    /// </summary>
    public enum ServerType
    {
        GET_ADD_VTK_DATASET                     = 0, //Add a new VTK Dataset
        GET_ON_ROTATE_DATASET                   = 2, //Rotate a dataset
        GET_ON_MOVE_DATASET                     = 3, //Move a dataset
        GET_ON_HEADSET_INIT                     = 4, //Initialize the headset,
        GET_HEADSETS_STATUS                     = 5, //All the headsets status (this one excluded)
        GET_ANCHOR_SEGMENT                      = 6, //An anchor segment
        GET_ANCHOR_EOF                          = 7, //The anchor EOF signal
        GET_SUBDATASET_MODIFICATION_OWNER       = 8, //The subdataset modification owner
        GET_ON_SCALE_DATASET                    = 9, //Scale a dataset
        GET_TF_DATASET                          = 10, /*!< Change the transfer function parameter of a given SubDataset*/
        GET_START_ANNOTATION                    = 11, /*!< Start to create an annotation on a specific dataset*/
        GET_ANCHOR_ANNOTATION                   = 12, /*!< Anchor an annotation on a specific dataset on a specific position*/
        GET_CLEAR_ANNOTATIONS                   = 13, /*!< Clear all annotations on a specific dataset*/
        GET_ADD_SUBDATASET                      = 14, /*!< Add a new SubDataset*/
        GET_DEL_SUBDATASET                      = 15, /*!< Delete a known SubDataset*/
        GET_SUBDATASET_OWNER                    = 16, /*!< Change the headset owning this SubDataset*/
        GET_CURRENT_ACTION                      = 17, /*!< Get the current action of this headset*/
        GET_TABLET_LOCATION                     = 19, /*!< Get the tablet's virtual location*/
        GET_TABLET_SCALE                        = 20, /*!< Get the tablet's scale*/
        GET_LASSO                               = 21, /*!< Get the tablet's virtual location*/
        GET_CONFIRM_SELECTION                   = 22, /*!< Confirm a selection*/
        GET_ADD_CLOUD_POINT_DATASET             = 23, /*!< Add cloud point dataset message*/
        GET_ADD_NEW_SELECTION_INPUT             = 24, /*!< Add a new selection entry*/
        GET_TOGGLE_MAP_VISIBILITY               = 25, /*!< Toggle the map visibility*/
        GET_SUBDATASET_VOLUMETRIC_MASK          = 26, /*!< Get the volumetric mask binary data*/
        GET_RESET_VOLUMETRIC_SELECTION          = 27, /*!< Reset one volumetric selection performed by a user*/
        GET_ADD_LOG_ANNOTATION                  = 28, /*!< Add a log annotation*/
        GET_ADD_LOG_ANNOTATION_POSITION         = 29, /*!< Register a log annotation position*/
        GET_SET_LOG_ANNOTATION_POSITION_INDEXES = 30, /*!< Change the headers of a log annotation position*/
        GET_LINK_LOG_ANNOT_POS_SD               = 31, /*!< Link a log annotation position to a subdataset*/
        GET_SET_SUBDATASET_CLIPPING             = 32, /*!< Set the clipping values of a subdataset*/
    }
}