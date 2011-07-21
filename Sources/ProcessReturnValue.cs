/// WPS.NET
/// A .NET implementation of OGC Web Processing Service v1.0.0
/// (c) brgm 2010

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace WPS.NET
{
    [Serializable()]
    public class ProcessReturnValue : ISerializable
    {
        public string fileName;
        public bool status;
        public string statusMessage;
        public List<OutputData> returnValues;
        public ResponseFormType responseForm;

        public ProcessReturnValue()
        {
            this.fileName = "result";        
            this.status = true;
            this.statusMessage = "";
            this.returnValues = new List<OutputData>();
            this.responseForm = new ResponseFormType("wps:ResponseForm");
        }               
    
        public ProcessReturnValue(SerializationInfo info, StreamingContext ctxt)
        {
            fileName = (string)info.GetValue("fileName", typeof(string));
            status = (bool)info.GetValue("status", typeof(bool));
            statusMessage = (string)info.GetValue("statusMessage", typeof(string));
            returnValues = (List<OutputData>)info.GetValue("returnValues", typeof(List<OutputData>));
            responseForm = (ResponseFormType)info.GetValue("responseForm", typeof(ResponseFormType));
        }
  
        public void GetObjectData(SerializationInfo info, StreamingContext ctxt)
        {
            info.AddValue("fileName", fileName); 
            info.AddValue("status", status);
            info.AddValue("statusMessage", statusMessage);
            info.AddValue("returnValues", returnValues);           
            info.AddValue("responseForm", responseForm);
        }

        public void SetErrorStatus(string str)
        {
            status = false;
            statusMessage = str;
        }

        public void AddComplexData(string id, string fileName, string mimeType, string s)
        {
            this.fileName = fileName;
            ComplexOutput data = new ComplexOutput(id, "", "", new ComplexFormat(mimeType,"",""));
            data.Value = System.Text.Encoding.UTF8.GetBytes(s);
            returnValues.Add(data);
        }

        public void AddComplexData(string id, string fileName, string mimeType, byte[] b)
        {
            this.fileName = fileName;
            ComplexOutput data = new ComplexOutput(id, "", "", mimeType);
            data.Value = b;
            returnValues.Add(data);
        }

        public void AddLiteralData(string id, string dataType, string s)
        {
            LiteralOutput o = new LiteralOutput(id, "", "", dataType);
            o.Value = s; 
            returnValues.Add(o);
        }

        public void AddData(OutputData data)
        {
            returnValues.Add(data);
        }

        // Helpers

        public List<string> GetOutputIdentifiers()
        {
            List<string> outputIdentifiers = new List<string>();
            if (IsRawDataOutput())
                outputIdentifiers.Add(responseForm.outputDefinition.Identifier);
            else if (responseForm.responseDocument != null)
                outputIdentifiers.AddRange(responseForm.responseDocument.GetIdentifiers());
            return outputIdentifiers;
        }

        public List<OutputData> GetOutputsForIdentifier(string identifier)
        {
            if (IsRawDataOutput() && Utils.StrICmp(responseForm.outputDefinition.Identifier, identifier))
                return new List<OutputData>() { new ComplexOutput(identifier, null, null, responseForm.outputDefinition.Format) };
            else if (responseForm.responseDocument != null)
                return responseForm.responseDocument.Outputs.FindAll(
                    delegate(OutputData output) { return Utils.StrICmp(output.Identifier, identifier); });
            return new List<OutputData>();
        }

        public bool IsOutputIdentifierRequested(string identifier)
        {
            List<string> ids = GetOutputIdentifiers();
            // should not happen because all id are added when none are requested
            //if (ids == null || ids.Length == 0) return true; // for now, by default, all identifiers are requested
            foreach (string id in ids) if (id == identifier) return true;
            return false;
        }

        public bool IsRawDataOutput()
        {
            return responseForm.outputDefinition != null;
        }
    }   
}
