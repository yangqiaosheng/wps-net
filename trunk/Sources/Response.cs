/// WPS.NET
/// A .NET implementation of OGC Web Processing Service v1.0.0
/// (c) brgm 2010

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Xml;

namespace WPS.NET
{
    public class MyXmlNode : ISerializable
    {
        public string name;

        public MyXmlNode(string name) { this.name = name; }

        public virtual bool Parse(XmlNode node, ProcessDescription processDescription) { return false; }

        public virtual bool Parse(string str, ProcessDescription processDescription) { return false; }

        public virtual string ToXml() { return ""; }

        public override string ToString() { return ""; }

        public virtual byte[] ToByteArray() { return new byte[0]; }

        public virtual string getName() { return name; }

        public MyXmlNode(SerializationInfo info, StreamingContext ctxt)
            : this("")
        {
            name = (string)info.GetValue("name", typeof(string));
        }

        public virtual void GetObjectData(SerializationInfo info, StreamingContext ctxt)
        {
            info.AddValue("name", name);
        }
    }
        
    [Serializable()]
    public class OutputDefinitionType : MyXmlNode
    {
        public string Identifier;
        public ComplexFormat Format;

        public OutputDefinitionType(string name)
            : base(name)
        {
            Identifier = null;
            Format = new ComplexFormat(null, null, null);
        }

        public OutputDefinitionType(SerializationInfo info, StreamingContext ctxt)
            : base(info, ctxt)
        {
            Identifier = (string)info.GetValue("Identifier", typeof(string));
            Format = (ComplexFormat)info.GetValue("Format", typeof(ComplexFormat));
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext ctxt)
        {
            base.GetObjectData(info, ctxt);
            info.AddValue("Identifier", Identifier);
            info.AddValue("Format", Format);
        }

        public override bool Parse(XmlNode node, ProcessDescription processDescription)
        {
            if (node == null) return false;

            XmlNamespaceManager nsmgr = Utils.CreateWPSNamespaceManager(node.OwnerDocument);

            XmlNodeList childs = node.SelectNodes("ows:Identifier", nsmgr);
            if (childs.Count != 1)
                throw new ExceptionReport("One identifier is mandatory when requesting a raw data output but " + childs.Count + " were found.",
                    ExceptionCode.InvalidParameterValue, "wps:RawDataOutput/ows:Identifier");
            Identifier = childs[0].InnerText;
            OutputData outputData = processDescription.GetProcessOutputParameter(Identifier);
            if (outputData == null)
                throw new ExceptionReport(String.Format("The output {0} is not a valid output for the process {1}",
                    Identifier, processDescription.Identifier), ExceptionCode.InvalidParameterValue, "rawDataOutput");

            ComplexOutput processOutput = outputData.asComplexOutput();
            if (processOutput == null)
                throw new ExceptionReport(String.Format("Only ComplexOutputs can be requested as rawDataOutput but {0} is a {1}",
                    Identifier, outputData.GetType().Name), ExceptionCode.InvalidParameterValue, "rawDataOutput");

            Format = processOutput.Format;
            Format.ParseValue(node);
            return true;
        }

        public override bool Parse(string str, ProcessDescription processDescription)
        {
            if (String.IsNullOrEmpty(str)) return false;

            string[] tokens = str.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length != 1)
                throw new ExceptionReport("One identifier is mandatory when requesting a raw data output but "
                    + tokens.Length + " were found.",
                    ExceptionCode.InvalidParameterValue, "RawDataOutput");

            string[] kv = str.Split(new char[] { '@' }, StringSplitOptions.RemoveEmptyEntries);
            if (kv.Length > 0)
            {
                OutputData outputData = processDescription.GetProcessOutputParameter(kv[0]);
                if (outputData == null)
                    throw new ExceptionReport(String.Format("The output {0} is not a valid output for the process {1}",
                        kv[0], processDescription.Identifier), ExceptionCode.InvalidParameterValue, "rawDataOutput");
                    
                ComplexOutput output = outputData.asComplexOutput();
                if (output == null)
                    throw new ExceptionReport(String.Format("Only ComplexOutputs can be requested as rawDataOutput but {0} is a {1}",
                        kv[0], outputData.GetType().ToString()), ExceptionCode.InvalidParameterValue, "rawDataOutput");

                Identifier = kv[0];
                Format = output.Format; // default format
                Format.ParseValue(str);
                if (output.Formats.Find(delegate(ComplexFormat cf) { return cf.Equals(Format); }) == null)
                    throw new ExceptionReport(string.Format("Requested format for the output {0} is not supported", kv[0]),
                        ExceptionCode.InvalidParameterValue, kv[0]);
            }

            return false;
        }

        public override string ToXml()
        {
            string ret = "<" + getName() + " " + Format.GetXmlValue() + ">";
            ret += "<ows:Identifier>" + Identifier + "</ows:Identifier>";
            ret += "</" + getName() + ">";
            return ret;
        }

        public override string ToString()
        {
            return Identifier + Format.ToString();
        }
    }

    [Serializable()]
    public class ResponseDocumentType : MyXmlNode
    {
        public bool lineage;
        public bool status;
        public bool storeExecuteResponse;
        public List<OutputData> Outputs;

        public ResponseDocumentType(string name)
            : base(name)
        {
            lineage = false;
            status = false;
            storeExecuteResponse = false;
            Outputs = new List<OutputData>();
        }

        public ResponseDocumentType(SerializationInfo info, StreamingContext ctxt)
            : base(info, ctxt)
        {
            lineage = (bool)info.GetValue("lineage", typeof(bool));
            status = (bool)info.GetValue("status", typeof(bool));
            Outputs = (List<OutputData>)info.GetValue("Outputs", typeof(List<OutputData>));
            storeExecuteResponse = (bool)info.GetValue("storeExecuteResponse", typeof(bool));
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext ctxt)
        {
            base.GetObjectData(info, ctxt);
            info.AddValue("lineage", lineage);
            info.AddValue("status", status);
            info.AddValue("Outputs", Outputs);
            info.AddValue("storeExecuteResponse", storeExecuteResponse);
        }

        public override bool Parse(XmlNode node, ProcessDescription processDescription)
        {
            if (node == null) return false;

            // Create an XmlNamespaceManager for resolving namespaces.
            XmlNamespaceManager nsmgr = Utils.CreateWPSNamespaceManager(node.OwnerDocument);

            lineage = Boolean.Parse(Utils.GetXmlAttributesValue(node, "lineage", "false"));
            status = Boolean.Parse(Utils.GetXmlAttributesValue(node, "status", "false"));

            XmlNodeList outputs = node.SelectNodes("wps:Output", nsmgr);
            if (outputs.Count == 0)
                throw new ExceptionReport(String.Format("No 'wps:Output' node was found inside the 'wps:ResponseDocument' node for the process '{0}'. Please check your request.",
                            processDescription.Identifier),
                            ExceptionCode.MissingParameterValue, processDescription.Identifier);

            ExceptionReport exception = null;

            foreach (XmlNode output in outputs)
            {
                XmlNode id = output.SelectSingleNode("ows:Identifier", nsmgr);
                XmlNode abst = output.SelectSingleNode("ows:Abstract", nsmgr);
                XmlNode title = output.SelectSingleNode("ows:Title", nsmgr);

                string identifier = id.InnerText;
                string titleStr = title != null ? title.InnerText : "";
                string abstStr = abst != null ? abst.InnerText : "";

                OutputData processOutput = processDescription.GetProcessOutputParameter(identifier);
                if (processOutput != null)
                {
                    OutputData myoutput = processOutput.Clone();
                    myoutput.Title = titleStr;
                    myoutput.Abstract = abstStr;
                    myoutput.asReference = Boolean.Parse(Utils.GetXmlAttributesValue(output, "asReference", "false"));
                    if (myoutput.asReference && !processDescription.storeSupported)
                        exception = new ExceptionReport(exception,
                            String.Format("The storage of response is not supported for the process {0} but is requested for the output {1}.",
                            processDescription.Identifier, identifier),
                            ExceptionCode.StorageNotSupported);
                    try
                    {
                        myoutput.Parse(output);
                        Outputs.Add(myoutput);
                    }
                    catch (ExceptionReport e)
                    {
                        exception = new ExceptionReport(e, exception);
                    }
                }
                else
                {
                    exception = new ExceptionReport(exception, String.Format("The output {0} is not a valid output for the process {1}",
                            identifier, processDescription.Identifier), ExceptionCode.InvalidParameterValue, "responseDocument");
                }
            }

            if (exception != null) throw exception;

            return true;
        }

        public override bool Parse(string str, ProcessDescription processDescription)
        {
            if (String.IsNullOrEmpty(str)) return false;

            string[] tokens = str.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            
            ExceptionReport exception = null;

            foreach (string param in tokens)
            {
                string[] kv = param.Split(new char[] { '@' }, StringSplitOptions.RemoveEmptyEntries);
                if (kv.Length > 0)
                {
                    OutputData output = processDescription.GetProcessOutputParameter(kv[0]);
                    if (output != null)
                    {
                        OutputData myoutput = output.Clone();
                        try
                        {
                            myoutput.Parse(param);
                            Outputs.Add(myoutput);
                        }
                        catch (ExceptionReport e)
                        {
                            exception = new ExceptionReport(e, exception);
                        }
                    }
                    else
                    {
                        exception = new ExceptionReport(exception, "The output "
                                + kv[0] + " is not a valid output for the process " + processDescription.Identifier,
                                ExceptionCode.InvalidParameterValue, "responseDocument");
                    }
                }
            }

            if (exception != null) throw exception;

            return tokens.Length != 0;
        }

        public override string ToXml()
        {
            string ret = "<" + getName() + base.ToXml() + " lineage='" + lineage.ToString() + "' status='" + status.ToString() + "'>";
            foreach (OutputData id in Outputs) ret += "<wps:Output><ows:Identifier>" + id.Identifier + "</ows:Identifier></wps:Output>";
            ret += "</" + getName() + ">";
            return ret;
        }

        public override string ToString()
        {
            string ret = "";
            if (Outputs.Count > 0)
            {
                foreach (OutputData id in Outputs) ret += id.Identifier + ";";
                ret = ret.Substring(0, ret.Length - 1);
                ret += "&";
            }
            ret += "lineage=" + lineage.ToString() + "&status=" + status.ToString();

            return ret;
        }

        public List<string> GetIdentifiers()
        {
            List<string> outputIdentifiers = new List<string>();
            foreach (OutputData output in Outputs)
                outputIdentifiers.Add(output.Identifier);
            return outputIdentifiers;
        }
    }

    [Serializable()]
    public class ResponseFormType : MyXmlNode
    {
        public OutputDefinitionType outputDefinition;
        public ResponseDocumentType responseDocument;

        public ResponseFormType(string name)
            : base(name)
        {
            outputDefinition = null;
            responseDocument = null;
        }

        public ResponseFormType(SerializationInfo info, StreamingContext ctxt)
            : base(info, ctxt)
        {
            outputDefinition = (OutputDefinitionType)info.GetValue("outputDefinition", typeof(OutputDefinitionType));
            responseDocument = (ResponseDocumentType)info.GetValue("responseDocument", typeof(ResponseDocumentType));
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext ctxt)
        {
            base.GetObjectData(info, ctxt);
            info.AddValue("outputDefinition", outputDefinition);
            info.AddValue("responseDocument", responseDocument);
        }

        public override bool Parse(XmlNode node, ProcessDescription processDescription)
        {
            if (node == null)
            {
                // include all responses because no requested identifier was provided
                responseDocument = new ResponseDocumentType("wps:ResponseDocument");
                responseDocument.Outputs.AddRange(processDescription.GetProcessOutputParameters());
                return true;
            }

            // Create an XmlNamespaceManager for resolving namespaces.
            XmlNamespaceManager nsmgr = Utils.CreateWPSNamespaceManager(node.OwnerDocument);

            XmlNode rawDataOutputNode = node.SelectSingleNode("wps:RawDataOutput", nsmgr);
            XmlNode responseDocNode = node.SelectSingleNode("wps:ResponseDocument", nsmgr);

            if (responseDocNode != null)
            {
                if (rawDataOutputNode != null)
                    throw new ExceptionReport("Only one kind of ResponseForm is allowed, but ResponseDocument and RawDataOutput were found!", ExceptionCode.InvalidParameterValue,
                        "ResponseForm");

                responseDocument = new ResponseDocumentType("wps:ResponseDocument");
                return responseDocument.Parse(responseDocNode, processDescription);
            }
            else if (rawDataOutputNode != null)
            {
                outputDefinition = new OutputDefinitionType("wps:RawDataOutput");
                return outputDefinition.Parse(rawDataOutputNode, processDescription);
            }
            else
                throw new ExceptionReport("A ResponseForm is provided, but neither ResponseDocument nor RawDataOutput element was found!", ExceptionCode.MissingParameterValue, "wps:ResponseForm");

        }

        public bool Parse(ProcessDescription processDescription)
        {
            string responseDocumentParam = Utils.DecodeURI(Utils.GetParameter("ResponseDocument"));
            string rawDataOutputParam = Utils.DecodeURI(Utils.GetParameter("RawDataOutput"));

            if (!String.IsNullOrEmpty(responseDocumentParam))
            {
                if (!String.IsNullOrEmpty(rawDataOutputParam))
                    throw new ExceptionReport("Only one kind of ResponseForm is allowed, but ResponseDocument and RawDataOutput were found!", ExceptionCode.InvalidParameterValue, "ResponseForm");

                responseDocument = new ResponseDocumentType("wps:ResponseDocument");
                responseDocument.lineage = Boolean.Parse(Utils.GetParameter("lineage", "false"));
                responseDocument.status = Boolean.Parse(Utils.GetParameter("status", "false"));
                responseDocument.storeExecuteResponse = Boolean.Parse(Utils.GetParameter("storeExecuteResponse", "false"));

                responseDocument.Parse(responseDocumentParam, processDescription);
            }
            else if (!String.IsNullOrEmpty(rawDataOutputParam))
            {
                outputDefinition = new OutputDefinitionType("wps:RawDataOutput");
                outputDefinition.Parse(rawDataOutputParam, processDescription);
            }
            else
            {
                // include all responses because no requested identifier was provided
                responseDocument = new ResponseDocumentType("wps:ResponseDocument");
                responseDocument.Outputs.AddRange(processDescription.GetProcessOutputParameters());
                responseDocument.lineage = Boolean.Parse(Utils.DecodeURI(Utils.GetParameter("lineage", "false")));
                responseDocument.status = Boolean.Parse(Utils.DecodeURI(Utils.GetParameter("status", "false")));
                responseDocument.storeExecuteResponse = Boolean.Parse(Utils.DecodeURI(Utils.GetParameter("storeExecuteResponse", "false")));
            }
            return true;
        }

        public override string ToXml()
        {
            string ret = "<" + getName() + base.ToXml() + ">";
            ret += (outputDefinition != null) ? outputDefinition.ToXml() : "";
            ret += (responseDocument != null) ? responseDocument.ToXml() : "";
            ret += "</" + getName() + ">";
            return ret;
        }

        public override string ToString()
        {
            string ret = "";
            ret += (outputDefinition != null) ? outputDefinition.ToString() : "";
            ret += (responseDocument != null) ? responseDocument.ToString() : "";
            return base.ToXml() + ret;
        }
    }
}
