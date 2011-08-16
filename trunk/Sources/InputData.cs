/// WPS.NET
/// A .NET implementation of OGC Web Processing Service v1.0.0
/// (c) brgm 2010-2011

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Runtime.Serialization;
using System.Xml;


namespace WPS.NET
{
    [Serializable()]
    public abstract class InputData : ISerializable, ICloneable
    {
        public string Identifier;
        public string Title;
        public string Abstract;
        public Dictionary<string, string> Metadata;
        public int MinOccurs;
        public int MaxOccurs;
        //public bool asReference;

        public InputData(string Identifier, string Title, string Abstract)
        {
            this.Identifier = Identifier;
            this.Title = Title;
            this.Abstract = Abstract;
            Metadata = new Dictionary<string, string>();
            MinOccurs = 1;
            MaxOccurs = 1;
            //asReference = false;
        }

        public InputData(SerializationInfo info, StreamingContext ctxt)
        {
            Identifier = (string)info.GetValue("Identifier", typeof(string));
            Title = (string)info.GetValue("Title", typeof(string));
            Abstract = (string)info.GetValue("Abstract", typeof(string));
            Metadata = (Dictionary<string, string>)info.GetValue("Metadata", typeof(Dictionary<string, string>));
            MinOccurs = (int)info.GetValue("MinOccurs", typeof(int));
            MaxOccurs = (int)info.GetValue("MaxOccurs", typeof(int));
            //asReference = (bool)info.GetValue("asReference", typeof(bool));
        }

        public virtual LiteralInput asLiteralInput() { return null; }

        public virtual ComplexInput asComplexInput() { return null; }

        public virtual BoundingBoxInput asBoundingBoxInput() { return null; }

        public virtual void GetObjectData(SerializationInfo info, StreamingContext ctxt)
        {
            info.AddValue("Identifier", Identifier);
            info.AddValue("Title", Title);
            info.AddValue("Abstract", Abstract);
            info.AddValue("Metadata", Metadata);
            info.AddValue("MinOccurs", MinOccurs);
            info.AddValue("MaxOccurs", MaxOccurs);
            //info.AddValue("asReference", asReference);
        }

        public void AddMetadata(string key, string value)
        {
            Metadata.Add(key, value);
        }

        public virtual bool ParseValue(XmlNode node) { return false; }

        public virtual bool ParseValue(string str) { return false; }
        
        public virtual bool IsValueAllowed() { return true; }

        public string GetXmlDescription()
        {
            string ret = "<wps:Input minOccurs='" + MinOccurs + "' maxOccurs='" + MaxOccurs + "'>" +
                "<ows:Identifier>" + Identifier + "</ows:Identifier>" +
                (String.IsNullOrEmpty(Title) ? "" : "<ows:Title>" + Utils.FormatStringForXml(Title) + "</ows:Title>") +
                (String.IsNullOrEmpty(Abstract) ? "" : "<ows:Abstract>" + Utils.FormatStringForXml(Abstract) + "</ows:Abstract>");
            foreach (KeyValuePair<string, string> kvp in Metadata)
                ret += "<ows:Metadata " + kvp.Key + "='" + kvp.Value + "'/>";
            ret += GetInnerXmlDescription();
            ret += "</wps:Input>";

            return ret;
        }

        protected abstract string GetInnerXmlDescription();

        public string GetXmlValue()
        {
            string ret = "<wps:Input>" +
                "<ows:Identifier>" + Identifier + "</ows:Identifier>" +
                 (String.IsNullOrEmpty(Title) ? "" : "<ows:Title>" + Utils.FormatStringForXml(Title) + "</ows:Title>");
            ret += GetInnerXmlValue();
            ret += "</wps:Input>";
            return ret;
        }

        protected abstract string GetInnerXmlValue();

        public override string ToString() { return ""; }

        public virtual InputData Clone() { return (InputData)this.MemberwiseClone(); }

        object ICloneable.Clone() { return Clone(); }
    }

    [Serializable()]
    class NullData : InputData
    {
        public NullData() : base(null,null,null) { }
        public NullData(SerializationInfo info, StreamingContext ctxt)
            : base(info, ctxt) { }

        public override void GetObjectData(SerializationInfo info, StreamingContext ctxt)
        {
            base.GetObjectData(info, ctxt);
        }
        protected override string GetInnerXmlDescription() { return ""; }

        protected override string GetInnerXmlValue() { return ""; }

        public override InputData Clone() { return (InputData)this.MemberwiseClone(); }
    }

    [Serializable()]
    public class LiteralInput : InputData
    {
        public string DataType;
        public List<string> AllowedValues;
        public string Default;
        public string Value;

        public List<string> AllowedUOM;
        public string DefaultUOM;
        public string UOM;

        public LiteralInput(string Identifier, string Title, string Abstract, string DataType, string Default)
            : base(Identifier, Title, Abstract)
        {
            this.DataType = DataType;
            this.Default = Default;
            this.Value = Default;
            AllowedValues = new List<string>();

            this.DefaultUOM = null;
            this.UOM = this.DefaultUOM;
            AllowedUOM = new List<string>();
        }

        public LiteralInput(SerializationInfo info, StreamingContext ctxt)
            : base(info, ctxt)
        {
            DataType = (string)info.GetValue("DataType", typeof(string));
            AllowedValues = (List<string>)info.GetValue("AllowedValues", typeof(List<string>));
            Default = (string)info.GetValue("Default", typeof(string));
            Value = (string)info.GetValue("Value", typeof(string));
            AllowedUOM = (List<string>)info.GetValue("AllowedUOM", typeof(List<string>));
            DefaultUOM = (string)info.GetValue("DefaultUOM", typeof(string));
            UOM = (string)info.GetValue("UOM", typeof(string));
        }

        public override LiteralInput asLiteralInput() { return this; }

        public override void GetObjectData(SerializationInfo info, StreamingContext ctxt)
        {
            base.GetObjectData(info, ctxt);
            info.AddValue("DataType", DataType);
            info.AddValue("AllowedValues", AllowedValues);
            info.AddValue("Default", Default);
            info.AddValue("Value", Value);
            info.AddValue("AllowedUOM", AllowedUOM);
            info.AddValue("DefaultUOM", DefaultUOM);
            info.AddValue("UOM", UOM);
        }

        public override bool ParseValue(XmlNode node)
        {
            // TODO check DataType ?
            XmlNamespaceManager nsmgr = Utils.CreateWPSNamespaceManager(node.OwnerDocument);
            XmlNode childNode = node.SelectSingleNode("wps:Data/wps:LiteralData", nsmgr);
            if (childNode == null)
                throw new ExceptionReport("No <wps:Data>/<wps:LiteralData> tag found, check request document.",
                    ExceptionCode.InvalidParameterValue, Identifier);
            Value = childNode.InnerText;
            return true;
        }

        public override bool ParseValue(string str)
        {
            // TODO check DataType ?
            string[] tokens = str.Split(new char[] { '@' }, StringSplitOptions.RemoveEmptyEntries);
            Value = tokens[0];
            return true;
        }

        public override bool IsValueAllowed()
        {
            if (AllowedValues.Count == 0) return true;
            foreach (string v in AllowedValues) if (Utils.StrICmp(v, Value)) return true;
            return false;
        }

        protected override string GetInnerXmlDescription()
        {
            string ret = "<wps:LiteralData><ows:DataType ows:reference='http://www.w3.org/TR/xmlschema-2/#" + DataType + "'>" + DataType + "</ows:DataType>";
            if (AllowedValues.Count > 0)
            {
                ret += "<ows:AllowedValues>";
                foreach (string s in AllowedValues) ret += "<ows:Value>" + s + "</ows:Value>";
                ret += "</ows:AllowedValues>";
            }
            else ret += "<ows:AnyValue/>";

            ret += String.IsNullOrEmpty(Default) ? "" : "<DefaultValue>" + Default + "</DefaultValue>";

            return ret + "</wps:LiteralData>";
        }

        protected override string GetInnerXmlValue()
        {
            return "<wps:Data><wps:LiteralData>" + Value + "</wps:LiteralData></wps:Data>";
        }

        public override string ToString()
        {
            return Value;
        }

        public override InputData Clone() { return (InputData)this.MemberwiseClone(); }
    }

    [Serializable()]
    public class ComplexFormat : ICloneable
    {
        public string mimeType;
        public string encoding;
        public string schema;

        public ComplexFormat(string mimeType, string encoding, string schema)
        {
            this.mimeType = mimeType;
            this.encoding = encoding;
            this.schema = schema;
        }

        public ComplexFormat(SerializationInfo info, StreamingContext ctxt)
        {
            mimeType = (string)info.GetValue("mimeType", typeof(string));
            encoding = (string)info.GetValue("encoding", typeof(string));
            schema = (string)info.GetValue("schema", typeof(string));
        }

        public void GetObjectData(SerializationInfo info, StreamingContext ctxt)
        {
            info.AddValue("mimeType", mimeType);
            info.AddValue("encoding", encoding);
            info.AddValue("schema", schema);
        }

        public string GetXmlDescription()
        {
            return "<wps:Format>" + (!string.IsNullOrEmpty(mimeType) ? "<ows:MimeType>" + mimeType + "</ows:MimeType>" : "") +
                 (!string.IsNullOrEmpty(encoding) ? "<ows:Encoding>" + encoding + "</ows:Encoding>" : "") +
                 (!string.IsNullOrEmpty(schema) ? "<ows:Schema>" + schema + "</ows:Schema>" : "") + "</wps:Format>";
        }

        public string GetXmlValue()
        {
            return "mimeType='" + mimeType + "'"
                + (!string.IsNullOrEmpty(encoding) ? " encoding='" + encoding + "'" : "")
                + (!string.IsNullOrEmpty(schema) ? " schema='" + schema + "'" : "");
        }

        public override string ToString()
        {
            return (!string.IsNullOrEmpty(mimeType) ? "@mimeType=" + mimeType : "")
                + (!string.IsNullOrEmpty(encoding) ? "@encoding=" + encoding : "")
                + (!string.IsNullOrEmpty(schema) ? "@schema=" + schema : "");
        }

        public bool ParseValue(XmlNode node)
        {
            if (node == null) return false;
            mimeType = Utils.GetXmlAttributesValue(node, "mimeType", mimeType);
            encoding = Utils.GetXmlAttributesValue(node, "encoding", encoding);
            schema = Utils.GetXmlAttributesValue(node, "schema", schema);
            return true;
        }

        public bool ParseValue(string str)
        {
            string[] tokens = str.Split(new char[] { '@' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string tok in tokens)
            {
                string[] kv = tok.Split(new char[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
                if (kv.Length < 2) continue;
                if (Utils.StrICmp(kv[0], "mimeType")) mimeType = Utils.DecodeURI(kv[1]);
                else if (Utils.StrICmp(kv[0], "encoding")) encoding = Utils.DecodeURI(kv[1]);
                else if (Utils.StrICmp(kv[0], "schema")) schema = Utils.DecodeURI(kv[1]);
            }
            return true;
        }

        public bool Equals(ComplexFormat format)
        {
            return Utils.StrICmp(mimeType, format.mimeType)
                && Utils.StrICmp(encoding, format.encoding)
                && Utils.StrICmp(schema, format.schema);
        }

        object ICloneable.Clone() { return Clone(); }

        public InputData Clone() { return (InputData)this.MemberwiseClone(); }
    }

    [Serializable()]
    public class ComplexInput : InputData
    {
        public List<ComplexFormat> Formats;
        public ComplexFormat Format;
        public int maximumMegabytes;
        public byte[] Value;
        public bool asReference;
        public string Reference;
        public string Method;
        public Dictionary<string,string> Header;
        public string Body;
        public bool BodyAsReference;
        public string BodyReference;

        public ComplexInput(string Identifier, string Title, string Abstract, ComplexFormat Format)
            : base(Identifier, Title, Abstract)
        {
            this.Format = Format;
            Formats = new List<ComplexFormat>();
            Formats.Add(this.Format);
            maximumMegabytes = -1;
            Method = "GET";
            asReference = false;
            Reference = null;
            Header = new Dictionary<string,string>();
            Body = null;
            BodyReference = null;
            BodyAsReference = false;
        }

        public ComplexInput(string Identifier, string Title, string Abstract, string mimeType)
            : this(Identifier, Title, Abstract, new ComplexFormat(mimeType, null, null))
        {
        }

        public ComplexInput(SerializationInfo info, StreamingContext ctxt)
            : base(info, ctxt)
        {
            Formats = (List<ComplexFormat>)info.GetValue("Formats", typeof(List<ComplexFormat>));
            Format = (ComplexFormat)info.GetValue("Format", typeof(ComplexFormat));
            maximumMegabytes = (int)info.GetValue("maximumMegabytes", typeof(int));
            Value = (byte[])info.GetValue("Value", typeof(byte[]));
            asReference = (bool)info.GetValue("asReference", typeof(bool));
            Reference = (string)info.GetValue("Reference", typeof(string));
            Method = (string)info.GetValue("Method", typeof(string));
            Header = (Dictionary<string, string>)info.GetValue("Header", typeof(Dictionary<string, string>));
            Body = (string)info.GetValue("Body", typeof(string));
            BodyReference = (string)info.GetValue("BodyReference", typeof(string));
            BodyAsReference = (bool)info.GetValue("BodyAsReference", typeof(bool));
        }

        public override ComplexInput asComplexInput() { return this; }

        public override void GetObjectData(SerializationInfo info, StreamingContext ctxt)
        {
            base.GetObjectData(info, ctxt);
            info.AddValue("Formats", Formats);
            info.AddValue("Format", Format);
            info.AddValue("maximumMegabytes", maximumMegabytes);
            info.AddValue("Value", Value);
            info.AddValue("asReference", asReference);
            info.AddValue("Reference", Reference);
            info.AddValue("Method", Method);
            info.AddValue("Header", Header);
            info.AddValue("Body", Body);
            info.AddValue("BodyReference", BodyReference);
            info.AddValue("BodyAsReference", BodyAsReference);
        }

        public override bool ParseValue(XmlNode node)
        {
            XmlNamespaceManager nsmgr = Utils.CreateWPSNamespaceManager(node.OwnerDocument);
            XmlNode childNode = node.SelectSingleNode("wps:Data/wps:ComplexData", nsmgr);
            if (childNode == null)
            {
                childNode = node.SelectSingleNode("wps:Reference", nsmgr);
                if (childNode == null)
                    throw new ExceptionReport("The parameter '" + Identifier + "' has not a valid value!",
                        ExceptionCode.InvalidParameterValue, Identifier);
                asReference = true;
                Reference = Utils.GetXmlAttributesValue(childNode, "xlink:href");
                Method = Utils.GetXmlAttributesValue(childNode, "method", Method);

                if (Utils.StrICmp(Method, "POST"))
                {
                    XmlNodeList headerList = childNode.SelectNodes("Header", nsmgr);

                    foreach (XmlNode headerItem in headerList)
                    {
                        string key = Utils.GetXmlAttributesValue(headerItem, "key");
                        string value = Utils.GetXmlAttributesValue(headerItem, "value");
                        if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(value))
                            throw new ExceptionReport(String.Format("[key:{0}; value:{1}] is not a valid request header. when parsing header data structure from the reference related to '{2}'!", key, value, Identifier), ExceptionCode.InvalidParameterValue, "Header");

                        Header.Add(key, value);
                    }

                    XmlNode bodyNode = childNode.SelectSingleNode("Body", nsmgr);
                    XmlNode bodyReferenceNode = childNode.SelectSingleNode("BodyReference", nsmgr);

                    if (bodyNode != null)
                    {
                        if (bodyReferenceNode != null)
                            throw new ExceptionReport(String.Format("Only one kind of Request Body is allowed, but Body and BodyReference were found when parsing the parameter {0}!", Identifier), ExceptionCode.InvalidParameterValue, "Reference");
                        Body = bodyNode.InnerXml;
                    }
                    else if (bodyReferenceNode != null)
                    {
                        BodyAsReference = true;
                        BodyReference = Utils.GetXmlAttributesValue(bodyReferenceNode, "xlink:href");
                        try
                        {
                            Body = System.Text.Encoding.UTF8.GetString(Utils.GetReferenceDataFromGetRequest(BodyReference));
                        }
                        catch (WebException e)
                        {
                            throw new ExceptionReport(String.Format("An error occurred when gathering the POST request for the complex input {0} from {1}: {2}", Identifier, BodyReference, e.Message), ExceptionCode.NoApplicableCode);
                        }
                        catch (Exception /*e*/)
                        {
                            throw new ExceptionReport(String.Format("An error occurred when gathering the POST request for the complex input {0} from {1}. You may contact the administrator if the problem persists.", Identifier), ExceptionCode.NoApplicableCode);
                        }
                    }
                    else
                    {
                        throw new ExceptionReport("Only one kind of Request Body is allowed, but Body and BodyReference were found!", ExceptionCode.InvalidParameterValue,
                                   "ResponseForm");
                    }
                }
                else if (!Utils.StrICmp(Method, "GET"))
                    throw new ExceptionReport(String.Format("The input {0} can not be retrieved because '{1}' is not a valid value for the parameter method!", Identifier, Method),
                        ExceptionCode.InvalidParameterValue, "method");
                GetValueFromReference();
            }
            else
            {
                // ComplexData embedded in the xml
                Value = System.Text.Encoding.UTF8.GetBytes(childNode.InnerXml);
            }
            Format.ParseValue(childNode);
            if (Formats.Find( delegate(ComplexFormat cf) { return cf.Equals(Format); }) == null)
                throw new ExceptionReport(string.Format("Requested format for the input {0} is not supported",Identifier),
                    ExceptionCode.InvalidParameterValue, Identifier);

            if (maximumMegabytes != -1 && Value.Length / 1048576 > maximumMegabytes)
                throw new ExceptionReport("Maximum file size is " + maximumMegabytes
                    + " MB, but provided data size is " + Value.Length / 1048576 + " MB.",
                    ExceptionCode.FileSizeExceeded, Identifier);
            return true;
        }

        public override bool ParseValue(string str)
        {
            string[] tokens = str.Split(new char[] { '@' });
            // using GET, ComplexInputs can only be provided as references
            // Value = System.Text.Encoding.UTF8.GetBytes(tokens.Length > 0 ? tokens[0] : str);
            if (tokens.Length > 0 && tokens[0].Length == 0)
            {
                asReference = true;
                // try to get a reference instead
                foreach (string tok in tokens)
                {
                    string[] kv = tok.Split(new char[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
                    if (kv.Length < 2) continue;
                    if (Utils.StrICmp(kv[0], "xlink:href")) Reference = Utils.DecodeURI(kv[1]);
                    else if (Utils.StrICmp(kv[0], "method")) Method = Utils.DecodeURI(kv[1]);
                }

                if (Utils.StrICmp(Method, "POST"))
                    throw new ExceptionReport("References using HTTP POST are not supported when doing a GET request.",
                    ExceptionCode.OperationNotSupported, Identifier);
                else if (!Utils.StrICmp(Method, "GET"))
                    throw new ExceptionReport(String.Format("The input '{0}' can not be retrieved because '{1}' is not a valid value for the parameter method!", Identifier, Method),
                        ExceptionCode.InvalidParameterValue, "method");

                // Launch Reference resolution
                GetValueFromReference();

            }
            else
            {
                throw new ExceptionReport("ComplexInputs can only be used as References when doing a GET request.",
                    ExceptionCode.OperationNotSupported, Identifier);
            }

            Format.ParseValue(str);
            if (Formats.Find(delegate(ComplexFormat cf) { return cf.Equals(Format); }) == null)
                throw new ExceptionReport(string.Format("Requested format for the input {0} is not supported", Identifier),
                    ExceptionCode.InvalidParameterValue, Identifier);

            if (maximumMegabytes != -1 && Value.Length / 1048576 > maximumMegabytes)
                throw new ExceptionReport("Maximum File Size is " + maximumMegabytes
                    + " MB, but provided data size is " + Value.Length / 1048576 + " MB.",
                    ExceptionCode.FileSizeExceeded, Identifier);
            return true;
        }

        private void GetValueFromReference()
        {
            try
            {
                if (Utils.StrICmp(Method, "GET"))
                    Value = Utils.GetReferenceDataFromGetRequest(Reference);
                else
                    Value = Utils.GetReferenceDataFromPostRequest(Reference, Body, Header);
            }
            catch (WebException e)
            {
                throw new ExceptionReport(String.Format("An error occurred when gathering data relative to the complex input {0} from {1}: {2}", Identifier, Reference, e.Message), ExceptionCode.NoApplicableCode);
            }
            catch (Exception /*e*/)
            {
                throw new ExceptionReport(String.Format("An error occurred when gathering data relative to the complex input {0} from {1}. You may contact the administrator if the problem persists.", Identifier), ExceptionCode.NoApplicableCode);
            }
        }

        public override bool IsValueAllowed()
        {
            //if (formats.Count == 0) return true;
            foreach (ComplexFormat f in Formats) if (Format.Equals(f)) return true;
            return false;
        }

        protected override string GetInnerXmlDescription()
        {
            string ret = "<wps:ComplexData" + ( maximumMegabytes != -1 ? String.Format(" maximumMegabytes='{0}'", maximumMegabytes) : "") + "><wps:Default>" + Format.GetXmlDescription() + "</wps:Default>";

            ret += "<ows:Supported>";
            foreach (ComplexFormat f in Formats) ret += "<ows:Format>" + f.GetXmlDescription() + "</ows:Format>";
            ret += "</ows:Supported>";

            return ret + "</wps:ComplexData>";
        }

        protected override string GetInnerXmlValue()
        {
            return asReference ? GetReferenceXmlValue() : String.Format("<wps:Data><wps:ComplexData {0}>{1}</wps:ComplexData></wps:Data>", Format.GetXmlValue(), ToString());
        }

        private string GetReferenceXmlValue()
        {
            string ret = string.Format("<wps:Reference xlink:href='{0}' method='{1}' {2}", Utils.FormatStringForXml(Reference), Method, Format.GetXmlValue());
            if (Utils.StrICmp(Method, "GET"))
                ret += "/>";
            else
            {
                ret += ">";

                foreach (KeyValuePair<string, string> kvp in Header)
                    ret += string.Format("<Header key='{0}' value='{1}'/>", kvp.Key, kvp.Value);

                if (BodyAsReference)
                    ret += string.Format("<BodyReference xlink:href='{0}'/>", Utils.FormatStringForXml(BodyReference));
                else
                    ret += string.Format("<Body>{0}</Body>", Body);
                ret += "</wps:Reference>";
            }
            return ret;
        }

        public override string ToString()
        {
            // TODO use encoding according to format?
            return System.Text.Encoding.UTF8.GetString(Value);
        }

        public override InputData Clone() { return (InputData)this.MemberwiseClone(); }
    }

    [Serializable()]
    public class BoundingBoxInput : InputData
    {
        public List<string> crss;
        public string crs;
        public int dimension;
        public double[] LowerCorner;
        public double[] UpperCorner;

        public BoundingBoxInput(string Identifier, string Title, string Abstract, string crs)
            : base(Identifier, Title, Abstract)
        {
            crss = new List<string>();
            this.crs = crs;
            crss.Add(crs);
            LowerCorner = null;
            UpperCorner = null;
        }

        public BoundingBoxInput(SerializationInfo info, StreamingContext ctxt)
            : base(info, ctxt)
        {
            crss = (List<string>)info.GetValue("crss", typeof(List<string>));
            crs = (string)info.GetValue("crs", typeof(string));
            dimension = (int)info.GetValue("dimension", typeof(int));
            LowerCorner = (double[])info.GetValue("LowerCorner", typeof(double[]));
            UpperCorner = (double[])info.GetValue("UpperCorner", typeof(double[]));
        }

        public override BoundingBoxInput asBoundingBoxInput() { return this; }

        public override void GetObjectData(SerializationInfo info, StreamingContext ctxt)
        {
            base.GetObjectData(info, ctxt);
            info.AddValue("crss", crss);
            info.AddValue("crs", crs);
            info.AddValue("dimension", dimension);
            info.AddValue("LowerCorner", LowerCorner);
            info.AddValue("UpperCorner", UpperCorner);
        }

        // TODO: control input and add exception if malformed
        public override bool ParseValue(XmlNode node)
        {
            XmlNamespaceManager nsmgr = Utils.CreateWPSNamespaceManager(node.OwnerDocument);
            XmlNode dataNode = node.SelectSingleNode("wps:Data/wps:BoundingBoxData", nsmgr);
            if (dataNode == null) return false;

            XmlNode childNode = dataNode.SelectSingleNode("ows:LowerCorner", nsmgr);
            LowerCorner = (childNode != null) ? ParseDoubleStringArray(childNode.InnerText) : null;
            childNode = dataNode.SelectSingleNode("ows:UpperCorner", nsmgr);
            UpperCorner = (childNode != null) ? ParseDoubleStringArray(childNode.InnerText) : null;
            childNode = dataNode.Attributes.GetNamedItem("crs");
            crs = (childNode != null) ? childNode.InnerText : "";
            childNode = dataNode.Attributes.GetNamedItem("dimension");
            dimension = (childNode != null) ? Int32.Parse(childNode.InnerText) : (LowerCorner != null ? LowerCorner.Length : 0);

            return LowerCorner != null && UpperCorner != null;
        }

        // TODO: control input and add exception if malformed
        protected static double[] ParseDoubleStringArray(string str)
        {
            try
            {
                CultureInfo ci = new CultureInfo("en-US");
                string[] sa = str.Split(new Char[] { ' ', ',', '\t', ';' }, StringSplitOptions.RemoveEmptyEntries);
                double[] da = new double[sa.Length];
                for (int i = 0; i < sa.Length; i++) da[i] = Double.Parse(sa[i], ci);
                return da;
            }
            catch
            {
                return null;
            }
        }

        protected static string DoubleArrayToString(double[] da)
        {
            if (da == null || da.Length == 0) return "";
            string ret = "";
            foreach (double v in da) ret += v + " ";
            return ret.Substring(0, ret.Length - 1);
        }

        // TODO: control input and add exception if malformed
        public override bool ParseValue(string str)
        {
            string[] tokens = str.Split(new Char[] { ' ', ',', '\t', ';' }, StringSplitOptions.RemoveEmptyEntries);

            if (tokens.Length % 2 != 0)
            {
                if (Char.IsLetter(tokens[tokens.Length - 1][0]))
                {
                    crs = tokens[tokens.Length - 1];
                    dimension = (tokens.Length - 1) / 2;
                }
                else
                    dimension = Int32.Parse(tokens[tokens.Length - 1]);
            }
            else
            {
                if (Char.IsLetter(tokens[tokens.Length - 2][0]))
                {
                    crs = tokens[tokens.Length - 2];
                    dimension = Int32.Parse(tokens[tokens.Length - 1]);
                }
                else dimension = tokens.Length / 2;
            }
            string lowerCorner = "";
            string upperCorner = "";
            for (int i = 0; i < dimension; i++)
            {
                lowerCorner += tokens[i] + " ";
                upperCorner += tokens[dimension + i] + " ";
            }
            LowerCorner = ParseDoubleStringArray(lowerCorner);
            UpperCorner = ParseDoubleStringArray(upperCorner);

            return true;
        }

        public override bool IsValueAllowed()
        {
            //if (crss.Count == 0) return true;
            foreach (string c in crss) if (Utils.StrICmp(c, crs)) return true;
            return false;
        }

        protected override string GetInnerXmlDescription()
        {
            string ret = "<wps:BoundingBoxData><wps:Default><wps:CRS xlink:href='" + crs + "'/></wps:Default>";

            //if (crss.Count == 0) crss.Add(crs);

            ret += "<ows:Supported>";
            foreach (string c in crss) ret += "<wps:CRS xlink:href='" + c + "'/>";
            ret += "</ows:Supported>";

            return ret + "</wps:BoundingBoxData>";
        }

        protected override string GetInnerXmlValue()
        {
            return "<wps:Data><wps:BoundingBoxData"
                   + (crs != "" ? (" crs=\"" + crs + "\"") : "")
                   + (dimension != 2 ? (" dimension=\"" + dimension + "\"") : "") + ">"
                   + "<ows:LowerCorner>" + DoubleArrayToString(LowerCorner) + "</ows:LowerCorner>"
                   + "<ows:UpperCorner>" + DoubleArrayToString(UpperCorner) + "</ows:UpperCorner>"
                   + "</wps:BoundingBoxData></wps:Data>";
        }

        public override string ToString()
        {
            return DoubleArrayToString(LowerCorner) + " " + DoubleArrayToString(UpperCorner) + (crs != "" ? ("," + crs) : "") + (dimension != 2 ? ("," + dimension) : "");
        }

        public override InputData Clone() { return (InputData)this.MemberwiseClone(); }
    }
}
