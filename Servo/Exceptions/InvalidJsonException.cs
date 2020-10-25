using System;
using System.Runtime.Serialization;

namespace Servo.Exceptions
{
    [Serializable]
    internal class InvalidJsonException : Exception
    {
        public InvalidJsonException()
        {
        }

        public InvalidJsonException(string message) : base(message)
        {
        }

        public InvalidJsonException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected InvalidJsonException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}