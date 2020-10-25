using System;
using System.Runtime.Serialization;

namespace Servo.Exceptions
{
    [Serializable]
    internal class MissingTokenException : Exception
    {
        public MissingTokenException()
        {
        }

        public MissingTokenException(string message) : base(message)
        {
        }

        public MissingTokenException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected MissingTokenException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}