namespace ZWaveControllerClient
{
    public enum ZWaveFunction
    {
        FUNC_ID_SERIAL_API_GET_INIT_DATA                = 0x02,
        FUNC_ID_SERIAL_API_APPL_NODE_INFORMATION        = 0x03,
        FUNC_ID_APPLICATION_COMMAND_HANDLER             = 0x04,
        FUNC_ID_ZW_GET_CONTROLLER_CAPABILITIES          = 0x05,
        FUNC_ID_SERIAL_API_SET_TIMEOUTS                 = 0x06,
        FUNC_ID_SERIAL_API_GET_CAPABILITIES             = 0x07,
        FUNC_ID_SERIAL_API_SOFT_RESET                   = 0x08,
        FUNC_ID_ZW_SEND_NODE_INFORMATION                = 0x12,
        FUNC_ID_ZW_SEND_DATA                            = 0x13,
        FUNC_ID_ZW_GET_VERSION                          = 0x15,
        FUNC_ID_ZW_R_F_POWER_LEVEL_SET                  = 0x17,
        FUNC_ID_ZW_GET_RANDOM                           = 0x1c,
        FUNC_ID_ZW_MEMORY_GET_ID                        = 0x20,
        FUNC_ID_MEMORY_GET_BYTE                         = 0x21,
        FUNC_ID_ZW_READ_MEMORY                          = 0x23,        FUNC_ID_ZW_SET_LEARN_NODE_STATE                 = 0x40,    // Not implemented
        FUNC_ID_ZW_GET_NODE_PROTOCOL_INFO               = 0x41,    // Get protocol info (baud rate, listening, etc.) for a given node
        FUNC_ID_ZW_SET_DEFAULT                          = 0x42,    // Reset controller and node info to default (original) values
        FUNC_ID_ZW_NEW_CONTROLLER                       = 0x43,    // Not implemented
        FUNC_ID_ZW_REPLICATION_COMMAND_COMPLETE         = 0x44,    // Replication send data complete
        FUNC_ID_ZW_REPLICATION_SEND_DATA                = 0x45,    // Replication send data
        FUNC_ID_ZW_ASSIGN_RETURN_ROUTE                  = 0x46,    // Assign a return route from the specified node to the controller
        FUNC_ID_ZW_DELETE_RETURN_ROUTE                  = 0x47,    // Delete all return routes from the specified node
        FUNC_ID_ZW_REQUEST_NODE_NEIGHBOR_UPDATE         = 0x48,    // Ask the specified node to update its neighbors (then read them from the controller)
        FUNC_ID_ZW_APPLICATION_UPDATE                   = 0x49,    // Get a list of supported (and controller) command classes
        FUNC_ID_ZW_ADD_NODE_TO_NETWORK                  = 0x4a,    // Control the addnode (or addcontroller) process...start, stop, etc.
        FUNC_ID_ZW_REMOVE_NODE_FROM_NETWORK             = 0x4b,    // Control the removenode (or removecontroller) process...start, stop, etc.
        FUNC_ID_ZW_CREATE_NEW_PRIMARY                   = 0x4c,    // Control the createnewprimary process...start, stop, etc.
        FUNC_ID_ZW_CONTROLLER_CHANGE                    = 0x4d,    // Control the transferprimary process...start, stop, etc.
        FUNC_ID_ZW_SET_LEARN_MODE                       = 0x50,    // Put a controller into learn mode for replication/ receipt of configuration info
        FUNC_ID_ZW_ASSIGN_SUC_RETURN_ROUTE              = 0x51,    // Assign a return route to the SUC
        FUNC_ID_ZW_ENABLE_SUC                           = 0x52,    // Make a controller a Static Update Controller
        FUNC_ID_ZW_REQUEST_NETWORK_UPDATE               = 0x53,    // Network update for a SUC(?)
        FUNC_ID_ZW_SET_SUC_NODE_ID                      = 0x54,    // Identify a Static Update Controller node id
        FUNC_ID_ZW_DELETE_SUC_RETURN_ROUTE              = 0x55,    // Remove return routes to the SUC
        FUNC_ID_ZW_GET_SUC_NODE_ID                      = 0x56,    // Try to retrieve a Static Update Controller node id (zero if no SUC present)
        FUNC_ID_ZW_REQUEST_NODE_NEIGHBOR_UPDATE_OPTIONS = 0x5a,    // Allow options for request node neighbor update
        FUNC_ID_ZW_EXPLORE_REQUEST_INCLUSION            = 0x5e,    // supports NWI
        FUNC_ID_ZW_REQUEST_NODE_INFO                    = 0x60,    // Get info (supported command classes) for the specified node
        FUNC_ID_ZW_REMOVE_FAILED_NODE_ID                = 0x61,    // Mark a specified node id as failed
        FUNC_ID_ZW_IS_FAILED_NODE_ID                    = 0x62,    // Check to see if a specified node has failed
        FUNC_ID_ZW_REPLACE_FAILED_NODE                  = 0x63,    // Remove a failed node from the controller's list (?)
        FUNC_ID_ZW_GET_ROUTING_INFO                     = 0x80,    // Get a specified node's neighbor information from the controller
        FUNC_ID_SERIAL_API_SLAVE_NODE_INFO              = 0xA0,    // Set application virtual slave node information
        FUNC_ID_APPLICATION_SLAVE_COMMAND_HANDLER       = 0xA1,    // Slave command handler
        FUNC_ID_ZW_SEND_SLAVE_NODE_INFO                 = 0xA2,    // Send a slave node information frame
        FUNC_ID_ZW_SEND_SLAVE_DATA                      = 0xA3,    // Send data from slave
        FUNC_ID_ZW_SET_SLAVE_LEARN_MODE                 = 0xA4,    // Enter slave learn mode
        FUNC_ID_ZW_GET_VIRTUAL_NODES                    = 0xA5,    // Return all virtual nodes
        FUNC_ID_ZW_IS_VIRTUAL_NODE                      = 0xA6,    // Virtual node test
        FUNC_ID_ZW_SET_PROMISCUOUS_MODE                 = 0xD0,    // Set controller into promiscuous mode to listen to all frames
        FUNC_ID_PROMISCUOUS_APPLICATION_COMMAND_HANDLER = 0xD1
    }
}
