using System;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace Arebis.Logging.GrayLog
{
    /// <summary>
    /// A GrayLog related exception.
    /// </summary>
    [Serializable]
    public class GrayLoggingException : Exception
    {
        /// <summary>
        /// Constructs a default GrayLoggingException.
        /// </summary>
        public GrayLoggingException()
            : this("An exception occurred.")
        { }

        /// <summary>
        /// Constructs a GrayLoggingException given a custom message.
        /// </summary>
        public GrayLoggingException(string message)
            : this(message, null)
        { }

        /// <summary>
        /// Constructs a GrayLoggingException for an inner exception.
        /// </summary>
        public GrayLoggingException(string message, Exception innerException)
            : base(message, innerException)
        { }

        /// <summary>
        /// Deserialization constructor.
        /// </summary>
        protected GrayLoggingException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }

        /// <summary>
        /// Serializes the current exception object.
        /// </summary>
        [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
        }
    }
}
