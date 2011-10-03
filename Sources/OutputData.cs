/// WPS.NET
/// A .NET implementation of OGC Web Processing Service v1.0.0
/// (c) brgm 2010-2011

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.Serialization;
using System.Xml;


namespace WPS.NET
{
    [Serializable()]
    public abstract class OutputData : ISerializable, ICloneable
    {
        public string Identifier;
        public string Title;
        public string Abstract;
        public Dictionary<string, string> Metadata;
        public bool asReference;

        public OutputData(string Identifier, string Title, string Abstract)
        {
            this.Identifier = Identifier;
            this.Title = Title;
            this.Abstract = Abstract;
            Metadata = new Dictionary<string, string>();
            this.asReference = false;
        }

        public OutputData(SerializationInfo info, StreamingContext ctxt)
        {
            Identifier = (string)info.GetValue("Identifier", typeof(string));
            Title = (string)info.GetValue("Title", typeof(string));
            Abstract = (string)info.GetValue("Abstract", typeof(string));
            Metadata = (Dictionary<string, string>)info.GetValue("Metadata", typeof(Dictionary<string, string>));
            asReference = (bool)info.GetValue("asReference", typeof(bool));
        }

        public virtual LiteralOutput asLiteralOutput() { return null; }

        public virtual ComplexOutput asComplexOutput() { return null; }

        public virtual BoundingBoxOutput asBoundingBoxOutput() { return null; }

        public virtual void GetObjectData(SerializationInfo info, StreamingContext ctxt)
        {
            info.AddValue("Identifier", Identifier);
            info.AddValue("Title", Title);
            info.AddValue("Abstract", Abstract);
            info.AddValue("Metadata", Metadata);
            info.AddValue("asReference", asReference);
        }

        public virtual bool Parse(XmlNode node) { return false; }

        public virtual bool Parse(string str) { return false; }

        public virtual bool IsValueAllowed() { return true; }

        public virtual string GetXmlDescription()
        {
            string ret = "<Output>" +
                "<ows:Identifier>" + Identifier + "</ows:Identifier>" +
                (String.IsNullOrEmpty(Title) ? "" : "<ows:Title>" + Title + "</ows:Title>") +
                (String.IsNullOrEmpty(Abstract) ? "" : "<ows:Abstract>" + Abstract + "</ows:Abstract>");
            foreach (KeyValuePair<string, string> kvp in Metadata)
                ret += "<ows:Metadata " + kvp.Key + "='" + kvp.Value + "'/>";
            ret += GetInnerXmlDescription();
            return ret + "</Output>";  //TODO: retiré 'wps:'
        }

        protected abstract string GetInnerXmlDescription();

        public virtual string GetXmlValue()
        {
            string ret = "<wps:Output>" +
                "<ows:Identifier>" + Identifier + "</ows:Identifier>" +
                (String.IsNullOrEmpty(Title) ? "" : "<ows:Title>" + Title + "</ows:Title>") +
                (String.IsNullOrEmpty(Abstract) ? "" : "<ows:Abstract>" + Abstract + "</ows:Abstract>");

            ret += GetInnerXmlValue();
            return ret + "</wps:Output>";
        }

        protected abstract string GetInnerXmlValue();

        public override string ToString() { return ""; }

        public virtual byte[] ToByteArray() { return System.Text.Encoding.UTF8.GetBytes(ToString()); }

        public virtual OutputData Clone() { return (OutputData)this.MemberwiseClone(); }

        object ICloneable.Clone() { return Clone(); }
    }

    [Serializable()]
    public class LiteralOutput : OutputData, ICloneable
    {
        public string DataType;
        public List<string> AllowedUOM;
        public string DefaultUOM;
        public string UOM; // unit of measure
        public string Value;

        public LiteralOutput(string Identifier, string Title, string Abstract)
            : this(Identifier, Title, Abstract, "string", "")
        {
        }

        public LiteralOutput(string Identifier, string Title, string Abstract, string DataType)
            : this(Identifier, Title, Abstract, DataType, "")
        {
        }

        public LiteralOutput(string Identifier, string Title, string Abstract, string DataType, string DefaultUOM)
            : base(Identifier, Title, Abstract)
        {
            this.DataType = DataType;
            this.DefaultUOM = DefaultUOM;
            this.UOM = DefaultUOM;
            AllowedUOM = new List<string>();
        }

        public LiteralOutput(SerializationInfo info, StreamingContext ctxt)
            : base(info, ctxt)
        {
            DataType = (string)info.GetValue("DataType", typeof(string));
            Value = (string)info.GetValue("Value", typeof(string));
            UOM = (string)info.GetValue("UOM", typeof(string));
            DefaultUOM = (string)info.GetValue("DefaultUOM", typeof(string));
            AllowedUOM = (List<string>)info.GetValue("AllowedUOM", typeof(List<string>));
        }

        public override LiteralOutput asLiteralOutput() { return this; }

        public override void GetObjectData(SerializationInfo info, StreamingContext ctxt)
        {
            base.GetObjectData(info, ctxt);
            info.AddValue("DataType", DataType);
            info.AddValue("Value", Value);
            info.AddValue("UOM", UOM);
            info.AddValue("DefaultUOM", DefaultUOM);
            info.AddValue("AllowedUOM", AllowedUOM);
        }

        public override bool Parse(XmlNode node)
        {
            UOM = Utils.GetXmlAttributesValue(node, "uom");
            return true;
        }

        public override bool Parse(string str)
        {
            string[] tokens = str.Split(new char[] { '@' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string tok in tokens)
            {
                string[] kv = tok.Split(new char[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
                if (kv.Length < 2) continue;
                if (Utils.StrICmp(kv[0], "UOM")) UOM = Utils.DecodeURI(kv[1]);
            }
            return true;
        }

        protected override string GetInnerXmlDescription()
        {
            return String.Format("<LiteralOutput><ows:DataType ows:reference='http://www.w3.org/TR/xmlschema-2/#{0}'{1}>{0}</ows:DataType></LiteralOutput>",  //TODO: retiré 'wps:'
                DataType, String.IsNullOrEmpty(UOM) ? "" : String.Format(" uom='{0}'",UOM));
        }

        protected override string GetInnerXmlValue()
        {
            return String.Format("<wps:Data><wps:LiteralData dataType='{0}'{1}>{2}</wps:LiteralData></wps:Data>",
                DataType, String.IsNullOrEmpty(UOM) ? "" : String.Format(" uom='{0}'", UOM), Value);
        }

        public override string ToString()
        {
            return Value;
        }

        public override OutputData Clone() { return (OutputData)this.MemberwiseClone(); }
    }

    [Serializable()]
    public class ComplexOutput : OutputData, ICloneable
    {
        public List<ComplexFormat> Formats;
        public ComplexFormat Format;
        //public int maximumMegabytes;
        public byte[] Value;

        public ComplexOutput(string Identifier, string Title, string Abstract, ComplexFormat Format)
            : base(Identifier, Title, Abstract)
        {
            this.Format = Format;
            Formats = new List<ComplexFormat>();
            this.Formats.Add(Format);
            //maximumMegabytes = -1;
        }

        public ComplexOutput(string Identifier, string Title, string Abstract, string mimeType)
            : this(Identifier, Title, Abstract, new ComplexFormat(mimeType, null, null))
        {
        }

        public ComplexOutput(SerializationInfo info, StreamingContext ctxt)
            : base(info, ctxt)
        {
            Formats = (List<ComplexFormat>)info.GetValue("formats", typeof(List<ComplexFormat>));
            Format = (ComplexFormat)info.GetValue("format", typeof(ComplexFormat));
            //maximumMegabytes = (int)info.GetValue("maximumMegabytes", typeof(int));
            Value = (byte[])info.GetValue("Value", typeof(byte[]));
        }

        public override ComplexOutput asComplexOutput() { return this; }

        public override void GetObjectData(SerializationInfo info, StreamingContext ctxt)
        {
            base.GetObjectData(info, ctxt);
            info.AddValue("formats", Formats);
            info.AddValue("format", Format);
            //info.AddValue("maximumMegabytes", maximumMegabytes);
            info.AddValue("Value", Value);
        }
        
        public override bool Parse(XmlNode node)
        {
            Format.ParseValue(node);
            if (Formats.Find(delegate(ComplexFormat cf) { return cf.Equals(Format); }) == null)
                throw new ExceptionReport(string.Format("Requested format for the output {0} is not supported", Identifier),
                    ExceptionCode.InvalidParameterValue, Identifier);
            return true;
        }

        public override bool Parse(string str)
        {
            Format.ParseValue(str);
            if (Formats.Find(delegate(ComplexFormat cf) { return cf.Equals(Format); }) == null)
                throw new ExceptionReport(string.Format("Requested format for the output {0} is not supported", Identifier),
                    ExceptionCode.InvalidParameterValue, Identifier);
            return true;
        }

        public void SetValue(string s)
        {
            Value = System.Text.Encoding.UTF8.GetBytes(s);
        }

        protected override string GetInnerXmlDescription()
        {
            string ret = "<ComplexOutput><Default>"
                + Format.GetXmlDescription() + "</Default>";  //TODO: retiré 'wps:'

            //if (formats.Count == 0) formats.Add(format);

            ret += "<ows:Supported>";
            foreach (ComplexFormat f in Formats) ret += f.GetXmlDescription();
            ret += "</ows:Supported>";

            return ret + "</ComplexOutput>";  //TODO: retiré 'wps:'
        }

        protected override string GetInnerXmlValue()
        {
            return "<wps:Data><wps:ComplexData " + Format.GetXmlValue() + ">"
                + System.Text.Encoding.UTF8.GetString(Value)
                + "</wps:ComplexData></wps:Data>";
        }

        public override string ToString()
        {
            return System.Text.Encoding.UTF8.GetString(Value);
        }

        public override OutputData Clone() { return (OutputData)this.MemberwiseClone(); }
    }

    [Serializable()]
    public class BoundingBoxOutput : OutputData, ICloneable
    {
        public List<string> crss;
        public string crs;
        public int dimension;
        public double[] LowerCorner;
        public double[] UpperCorner;

        public BoundingBoxOutput(string Identifier, string Title, string Abstract)
            : base(Identifier, Title, Abstract)
        {
            crss = new List<string>();
            LowerCorner = null;
            UpperCorner = null;
        }

        public BoundingBoxOutput(SerializationInfo info, StreamingContext ctxt)
            : base(info, ctxt)
        {
            crss = (List<string>)info.GetValue("crss", typeof(List<string>));
            crs = (string)info.GetValue("crs", typeof(string));
            dimension = (int)info.GetValue("dimension", typeof(int));
            LowerCorner = (double[])info.GetValue("LowerCorner", typeof(double[]));
            UpperCorner = (double[])info.GetValue("UpperCorner", typeof(double[]));
        }

        public override BoundingBoxOutput asBoundingBoxOutput() { return this; }

        public override void GetObjectData(SerializationInfo info, StreamingContext ctxt)
        {
            base.GetObjectData(info, ctxt);
            info.AddValue("crss", crss);
            info.AddValue("crs", crs);
            info.AddValue("dimension", dimension);
            info.AddValue("LowerCorner", LowerCorner);
            info.AddValue("UpperCorner", UpperCorner);
        }

        public override bool Parse(XmlNode node)
        {
            throw new NotImplementedException();
            //return false;
        }

        public override bool Parse(string str)
        {
            throw new NotImplementedException();
            //return true;
        }

        /*public override bool ParseValue(XmlNode node)
        {
            XmlNamespaceManager nsmgr = Utils.CreateWPSNamespaceManager(node.OwnerDocument);
            XmlNode dataNode = node.SelectSingleNode("wps:BoundingBoxData", nsmgr);
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
        }*/

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

        protected override string GetInnerXmlDescription()
        {
            string ret = "<BoundingBoxOutput><Default><CRS xlink:href='" + crs + "'/></Default>";  //TODO: retiré 'wps:'

            ret += "<ows:Supported>";
            foreach (string c in crss) ret += "<wps:CRS xlink:href='" + c + "'/>";
            ret += "</ows:Supported>";
            return ret + "</BoundingBoxOutput>";  //TODO: retiré 'wps:'
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

        public override OutputData Clone() { return (OutputData)this.MemberwiseClone(); }
    }
}