using System;
using FileServer.Core.Messages;

namespace FileServer.Server
{
    internal class MessageException : Exception
    {
        public MessageException(ErrorCode errorCode)
        {
            ErrorCode = errorCode;
        }

        public ErrorCode ErrorCode { get; private set; }
    }
}