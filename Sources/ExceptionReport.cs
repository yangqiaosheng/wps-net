/// WPS.NET
/// A .NET implementation of OGC Web Processing Service v1.0.0
/// (c) brgm 2010

using System;
using System.Runtime.Serialization;
using System.Xml;

namespace WPS.NET
{
    public enum ExceptionCode
    {
        NoApplicableCode,
        OperationNotSupported,
        MissingParameterValue,
        InvalidParameterValue,
        VersionNegotiationFailed,
        InvalidUpdateSequence,
        OptionNotSupported,
        // Execute Codes
        ServerBusy,
        FileSizeExceeded,
        NotEnoughStorage,
        StorageNotSupported
    }

    /// <summary>
    /// This class creates formatted exception reports when errors occurs during a WPS request.
    /// Chaining of Exceptions is possible (i.e. many errors are possible)
    /// When chaining, innerException is the previous error which has occurred
    /// </summary>
    [Serializable()]
    public class ExceptionReport : Exception
    {
        readonly public ExceptionCode ExceptionCode;
        readonly public string Locator;

        public ExceptionReport(ExceptionReport exception, string Message, ExceptionCode ExceptionCode, string Locator)
            : base(Message, exception)
        {
            this.ExceptionCode = ExceptionCode;
            this.Locator = Locator;
        }

        public ExceptionReport(ExceptionReport exception, string Message, ExceptionCode ExceptionCode)
            : base(Message, exception)
        { }

        public ExceptionReport(ExceptionReport exception, string Message)
            : this(exception, Message, ExceptionCode.NoApplicableCode, "")
        { }

        public ExceptionReport(ExceptionReport exception, ExceptionCode ExceptionCode, string Locator)
            : this(exception, "", ExceptionCode, Locator)
        { }

        public ExceptionReport(ExceptionReport exception, ExceptionCode ExceptionCode)
            : this(exception, "", ExceptionCode, "")
        { }

        public ExceptionReport(string Message, ExceptionCode ExceptionCode, string Locator)
            : base(Message) 
        {
            this.ExceptionCode = ExceptionCode;
            this.Locator = Locator;
        }

        public ExceptionReport(ExceptionCode ExceptionCode, string Locator)
            : this("", ExceptionCode, Locator)
        { }

        public ExceptionReport(ExceptionCode ExceptionCode)
            : this("", ExceptionCode, "")
        { }

        public ExceptionReport(string Message, ExceptionCode ExceptionCode)
            : this(Message, ExceptionCode, "")
        { }

        public ExceptionReport(string Message)
            : this(Message, ExceptionCode.NoApplicableCode, "")
        { }

        public ExceptionReport(ExceptionReport exception)
            : this(exception.Message, exception.ExceptionCode, exception.Locator)
        { }

        public ExceptionReport(ExceptionReport exception, ExceptionReport innerException)
            : this(innerException, exception.Message, exception.ExceptionCode, exception.Locator)
        { }

        protected ExceptionReport(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            ExceptionCode = (ExceptionCode)info.GetValue("ExceptionCode", typeof(ExceptionCode));
            Locator = (string)info.GetValue("Locator", typeof(string));
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("ExceptionCode", ExceptionCode);
            info.AddValue("Locator", Locator);
        }

        public override string ToString()
        {
            string ret = "<Exception exceptionCode='" + ExceptionCode
                + (!string.IsNullOrEmpty(Locator) ? "' locator='" + Locator : "")
                + (!string.IsNullOrEmpty(Message) ? "'><ExceptionText>" + Utils.FormatStringForXml(Message) + "</ExceptionText>\n</Exception>" : "'/>");
            if (InnerException != null) ret = InnerException.ToString() + "\n" + ret;
            return ret;
        }

        public XmlDocument GetReport()
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(Global.XmlHeader + "<ExceptionReport version='" + Global.WPSVersion +
                "' xml:lang='" + Global.DefaultLanguage + "' "                
                + "xmlns='" + Global.OWSNamespace + "' xmlns:xsi='" + Global.XSINamespace + "' "
                + Global.WPSExceptionSchema + ">\n"
                + ToString()
                + "</ExceptionReport>");
            return doc;
        }
    }
}
