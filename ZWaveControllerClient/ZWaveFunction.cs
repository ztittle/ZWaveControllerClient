namespace ZWaveControllerClient
{
    /// <summary>
    /// See http://www.openzwave.com/dev/Defs_8h_source.html and https://github.com/yepher/RaZBerry
    /// </summary>
    public enum ZWaveFunction : byte
    {
        None = 0x00,
        DiscoveryNodes = 0x02,
        SerialApiApplNodeInformation = 0x03,
        ApplicationCommandHandler = 0x04,
        GetControllerCapabilities = 0x05,
        SerialApiSetTimeouts = 0x06,
        SerialGetCapabilities = 0x07,
        SerialApiSoftReset = 0x08,
        SetRFReceiveMode = 0x10,
        SetSleepMode = 0x11,
        SendNodeInformation = 0x12,
        SendData = 0x13,
        SendDataMulti = 0x14,
        GetVersion = 0x15,
        SendDataAbort = 0x16,
        RFPowerLevelSet = 0x17,
        SendDataMeta = 0x18,
        GetRandom = 0x1c,
        MemoryGetId = 0x20,
        MemoryGetByte = 0x21,
        MemoryPutByte = 0x22,
        MemoryGetBuffer = 0x23,
        MemoryPutBuffer = 0x24,
        ClockSet = 0x30,
        ClockGet = 0x31,
        ClockCompare = 0x32,
        RtcTimerCreate = 0x33,
        RtcTimerRead = 0x34,
        RtcTimerDelete = 0x35,
        RtcTimerCall = 0x36,
        /// <summary>
        /// Not implemented
        /// </summary>
        SetLearnNodeState = 0x40,
        /// <summary>
        /// Get protocol info (baud rate, listening, etc.) for a given node
        /// </summary>
        GetNodeProtocolInfo = 0x41,
        /// <summary>
        /// Reset controller and node info to default (original) values
        /// </summary>
        SetDefault = 0x42,
        /// <summary>
        /// Not implemented
        /// </summary>
        NewController = 0x43,
        /// <summary>
        /// Replication send data complete
        /// </summary>
        ReplicationCommandComplete = 0x44,
        /// <summary>
        /// Replication send data
        /// </summary>
        ReplicationSendData = 0x45,
        /// <summary>
        /// Assign a return route from the specified node to the controller
        /// </summary>
        AssignReturnRoute = 0x46,
        /// <summary>
        /// Delete all return routes from the specified node
        /// </summary>
        DeleteReturnRoute = 0x47,
        /// <summary>
        /// Ask the specified node to update its neighbors (then read them from the controller)
        /// </summary>
        RequestNodeNeighborUpdate = 0x48,
        /// <summary>
        /// Get a list of supported (and controller) command classes
        /// </summary>
        ApplicationUpdate = 0x49,
        /// <summary>
        /// Control the addnode (or addcontroller) process...start, stop, etc.
        /// </summary>
        AddNodeToNetwork = 0x4a,
        /// <summary>
        /// Control the removenode (or removecontroller) process...start, stop, etc.
        /// </summary>
        RemoveNodeFromNetwork = 0x4b,
        /// <summary>
        /// Control the createnewprimary process...start, stop, etc.
        /// </summary>
        CreateNewPrimary = 0x4c,
        /// <summary>
        /// Control the transferprimary process...start, stop, etc.
        /// </summary>
        ControllerChange = 0x4d,
        /// <summary>
        /// Put a controller into learn mode for replication/ receipt of configuration info
        /// </summary>
        SetLearnMode = 0x50,
        /// <summary>
        /// Assign a return route to the SUC
        /// </summary>
        AssignSucReturnRoute = 0x51,
        /// <summary>
        /// Make a controller a Static Update Controller
        /// </summary>
        EnableSuc = 0x52,
        /// <summary>
        /// Network update for a SUC(?)
        /// </summary>
        RequestNetworkUpdate = 0x53,
        /// <summary>
        /// Identify a Static Update Controller node id
        /// </summary>
        SetSucNodeId = 0x54,
        /// <summary>
        /// Remove return routes to the SUC
        /// </summary>
        DeleteSucReturnRoute = 0x55,
        /// <summary>
        /// Try to retrieve a Static Update Controller node id (zero if no SUC present)
        /// </summary>
        GetSucNodeId = 0x56,
        SendSucId = 0x57,
        RediscoveryNeeded = 0x59,
        /// <summary>
        /// Allow options for request node neighbor update
        /// </summary>
        RequestNodeNeighborUpdateOptions = 0x5a,
        /// <summary>
        /// supports NWI
        /// </summary>
        ExploreRequestInclusion = 0x5e,
        /// <summary>
        /// Get info (supported command classes) for the specified node
        /// </summary>
        RequestNodeInfo = 0x60,
        /// <summary>
        /// Mark a specified node id as failed
        /// </summary>
        RemoveFailedNodeId = 0x61,
        /// <summary>
        /// Check to see if a specified node has failed
        /// </summary>
        IsFailedNode = 0x62,
        /// <summary>
        /// Remove a failed node from the controller's list (?)
        /// </summary>
        ReplaceFailedNode = 0x63,
        TimerStart = 0x70,
        TimerRestart = 0x71,
        TimerCancel = 0x72,
        TimerCall = 0x73,
        /// <summary>
        /// Get a specified node's neighbor information from the controller
        /// </summary>
        GetRoutingTableLine = 0x80,
        GetTXCounter = 0x81,
        ResetTXCounter = 0x82,
        StoreNodeInfo = 0x83,
        StoreHomeId = 0x84,
        LockRouteResponse = 0x90,
        SendDataRouteDemo = 0x91,
        SerialApiTest = 0x95,
        /// <summary>
        /// Set application virtual slave node information
        /// </summary>
        SerialApiSlaveNodeInfo = 0xA0,
        /// <summary>
        /// Slave command handler
        /// </summary>
        ApplicationSlaveCommandHandler = 0xA1,
        /// <summary>
        /// Send a slave node information frame
        /// </summary>
        SendSlaveNodeInfo = 0xA2,
        /// <summary>
        /// Send data from slave
        /// </summary>
        SendSlaveData = 0xA3,
        /// <summary>
        /// Enter slave learn mode
        /// </summary>
        SetSlaveLearnMode = 0xA4,
        /// <summary>
        /// Return all virtual nodes
        /// </summary>
        GetVirtualNodes = 0xA5,
        /// <summary>
        /// Virtual node test
        /// </summary>
        IsVirtualNode = 0xA6,
        /// <summary>
        /// Set controller into promiscuous mode to listen to all frames
        /// </summary>
        SetPromiscuousMode = 0xD0,
        PromiscuousApplicationCommandHandler = 0xD1
    }
}
