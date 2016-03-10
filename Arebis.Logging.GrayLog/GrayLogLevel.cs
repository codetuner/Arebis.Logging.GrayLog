using System;

namespace Arebis.Logging.GrayLog
{
    /// <summary>
    /// Standard Syslog levels to be used according to the GELF format specification.
    /// </summary>
    /// <see href="http://docs.graylog.org/en/latest/pages/gelf.html"/>
    [Serializable]
    public enum SyslogLevel
    {
        /// <summary>
        /// System is unusable
        /// </summary>
        Emergency = 0,

        /// <summary>
        /// Action must be taken immediately.
        /// </summary>
        Alert = 1,

        /// <summary>
        /// Critical conditions.
        /// </summary>
        Critical = 2,

        /// <summary>
        /// Error conditions.
        /// </summary>
        Error = 3,

        /// <summary>
        /// Warning conditions.
        /// </summary>
        Warning = 4,

        /// <summary>
        /// Normal but significant condition.
        /// </summary>
        Notice = 5,

        /// <summary>
        /// Informational messages.
        /// </summary>
        Informational = 6,

        /// <summary>
        /// Debug-level messages.
        /// </summary>
        Debug = 7,
    }
}
