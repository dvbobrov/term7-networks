namespace FileServer.Core.Messages {
    public enum MessageCode : byte {
        ReqList  = 0x1,
        ReqGet   = 0x2,
        ReqPut   = 0x3,
        RespList = 0x4,
        RespGet  = 0x5,
        None     = 0x0,
        Error    = 0xFF
    }

    public enum ErrorCode : byte {
        FileNotFound        = 0x1,
        TooManyConnections  = 0x2,
        MalformedMessage    = 0x3,
        InternalServerError = 0xFF
    }

    public enum MessageKind {
        RequiresResponse, NoResponse
    }
}
