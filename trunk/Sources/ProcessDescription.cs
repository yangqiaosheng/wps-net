/// WPS.NET
/// A .NET implementation of OGC Web Processing Service v1.0.0
/// (c) brgm 2010-2011

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Reflection;

namespace WPS.NET
{
    [Serializable()]
    public class ProcessDescription : ISerializable
    {
        public string Identifier;
        public string Title;
        public string Abstract;
        public string Version;
        public string Language;

        public Dictionary<string, List<string>> Metadata;

        public List<InputData> inputs;
        public List<OutputData> outputs;

        public bool storeSupported;
        public bool statusSupported;

        public string processClass;
        public string processMethod;
        public string processStartDate;

        public ProcessDescription(string Identifier, string Title, string Abstract, string Version)
            : this(Identifier, Title, Abstract, Version, Global.DefaultLanguage)
        {
        }

        public ProcessDescription(string Identifier, string Title, string Abstract, string Version, string language)
        {
            this.Identifier = Identifier;
            this.Title = Title;
            this.Abstract = Abstract;
            this.Version = Version;
            this.Language = language;

            Metadata = new Dictionary<string, List<string>>();

            inputs = new List<InputData>();
            outputs = new List<OutputData>();

            storeSupported = false;
            statusSupported = false;

            processClass = "WPSProcess." + Identifier;
            processMethod = "Execute";
        }

        public ProcessDescription(SerializationInfo info, StreamingContext ctxt)
        {
            Identifier = (string)info.GetValue("Identifier", typeof(string));
            Title = (string)info.GetValue("Title", typeof(string));
            Abstract = (string)info.GetValue("Abstract", typeof(string));
            Version = (string)info.GetValue("Version", typeof(string));
            Language = (string)info.GetValue("Language", typeof(string));
            Metadata = (Dictionary<string, List<string>>)info.GetValue("Metadata", typeof(Dictionary<string, List<string>>));
            inputs = (List<InputData>)info.GetValue("inputs", typeof(List<InputData>));
            outputs = (List<OutputData>)info.GetValue("outputs", typeof(List<OutputData>));
            storeSupported = (bool)info.GetValue("storeSupported", typeof(bool));
            statusSupported = (bool)info.GetValue("statusSupported", typeof(bool));
            processClass = (string)info.GetValue("processClass", typeof(string));
            processMethod = (string)info.GetValue("processMethod", typeof(string));
            processStartDate = (string)info.GetValue("processStartDate", typeof(string));
        }

        public virtual void GetObjectData(SerializationInfo info, StreamingContext ctxt)
        {
            info.AddValue("Identifier", Identifier);
            info.AddValue("Title", Title);
            info.AddValue("Abstract", Abstract);
            info.AddValue("Version", Version);
            info.AddValue("Language", Language);
            info.AddValue("Metadata", Metadata);
            info.AddValue("inputs", inputs);
            info.AddValue("outputs", outputs);
            info.AddValue("storeSupported", storeSupported);
            info.AddValue("statusSupported", statusSupported);
            info.AddValue("processClass", processClass);
            info.AddValue("processMethod", processMethod);
            info.AddValue("processStartDate", processStartDate);
        }

        public void AddMetadata(string key, string value)
        {
            List<string> values;
            if (!Metadata.TryGetValue(key, out values))
            {
                values = new List<string>();
                Metadata.Add(key, values);
            }
            values.Add(value);
        }

        public string GetProcessBriefDescription()
        {
            StringBuilder ret = new StringBuilder();

            ret.Append("<ows:Identifier>" + Identifier + "</ows:Identifier>" +
                    "<ows:Title>" + Title + "</ows:Title>" +
                    "<ows:Abstract>" + Abstract + "</ows:Abstract>");

            foreach (KeyValuePair<string, List<string>> kvp in Metadata)
                foreach (string md in kvp.Value)
                    ret.Append("<ows:Metadata " + kvp.Key + "='" + md + "'/>");

            return ret.ToString();
        }

        public string GetProcessDescriptionDocument()
        {
            StringBuilder ret = new StringBuilder();

            ret.Append("<ProcessDescription wps:processVersion='" + Version + "' storeSupported='" + storeSupported.ToString().ToLower()
                 + "' statusSupported='" + statusSupported.ToString().ToLower() + "'>");
            ret.Append(GetProcessBriefDescription());

            ret.Append("<DataInputs>");
            foreach (InputData i in inputs) ret.Append(i.GetXmlDescription());
            ret.Append("</DataInputs>");

            ret.Append("<ProcessOutputs>");
            foreach (OutputData o in outputs) ret.Append(o.GetXmlDescription());
            ret.Append("</ProcessOutputs>");

            ret.Append("</ProcessDescription>");

            return ret.ToString();
        }

        public List<InputData> GetProcessInputParameters()
        {
            return inputs;
        }

        public InputData GetProcessInputParameter(string id)
        {
            foreach (InputData i in inputs)
                if (Utils.StrICmp(i.Identifier, id)) return i;
            return null;
        }

        public List<OutputData> GetProcessOutputParameters()
        {
            return outputs;
        }

        public OutputData GetProcessOutputParameter(string id)
        {
            foreach (OutputData o in outputs)
                if (Utils.StrICmp(o.Identifier, id)) return o;
            return null;
        }

        public static ProcessDescription GetProcessDescription(string processId)
        {
            AppDomain mainDomain = AppDomain.CurrentDomain;
            // A secondary operation domain has to be used so that the assemblies can be loaded/unloaded, as the function Assembly
            AppDomain operationDomain = null;
            ProcessDescription result = null;
            try
            {
                operationDomain = Utils.CreateDomain();
                Utils.AssemblyLoader assemblyLoader = Utils.AssemblyLoader.Create(operationDomain);
                assemblyLoader.Load(Utils.MapPath(Global.ProcessesBinPath + processId + ".dll"));
                result = (ProcessDescription)assemblyLoader.ExecuteMethod("WPSProcess." + processId, "GetDescription", null, null);
                AppDomain.Unload(operationDomain);
            }
            catch (Exception /*e*/)
            {
                if (operationDomain != null) AppDomain.Unload(operationDomain);
                throw new ExceptionReport("Unable to retrieve the description of the process '" + processId + "'.\nEnsure that the Identifier parameter really is the name of a service proposed on this server. The name is case dependent. The name of the available processes can be get with the GetCapabilities operation.", ExceptionCode.InvalidParameterValue, "Identifier");
            }

            return result;
        }

        private static Utils.AssemblyLoader asyncLoader = null;
        private static AppDomain asyncOperationDomain;

        public ProcessReturnValue CallProcess(ProcessInputParams args, ResponseFormType responseForm, bool asynchronous)
        {
            // A secondary operation domain has to be used so that the assemblies can be loaded/unloaded, as the function Assembly
            AppDomain operationDomain = null;
            ProcessReturnValue result = null;
            try
            {
                ArrayList methodArgs = new ArrayList();
                methodArgs.Add(args);
                ProcessReturnValue processReturnValue = new ProcessReturnValue();
                processReturnValue.responseForm = responseForm;
                methodArgs.Add(processReturnValue);

                operationDomain = Utils.CreateDomain();
                if (!asynchronous)
                {
                    // Load the assembly corresponding to the requested process
                    Utils.AssemblyLoader assemblyLoader = Utils.AssemblyLoader.Create(operationDomain);
                    assemblyLoader.Load(Utils.MapPath(Global.ProcessesBinPath + Identifier + ".dll"));
                    result = (ProcessReturnValue)assemblyLoader.ExecuteMethod(processClass, processMethod, methodArgs.ToArray(), null);
                    AppDomain.Unload(operationDomain);
                }
                else
                {
                    asyncOperationDomain = operationDomain;
                    asyncLoader = Utils.AssemblyLoader.Create(operationDomain);
                    asyncLoader.Load(Utils.MapPath(Global.ProcessesBinPath + Identifier + ".dll"));
                    result = (ProcessReturnValue)asyncLoader.ExecuteAsyncMethod(processClass, processMethod, methodArgs.ToArray(), new object[] { AppDomain.CurrentDomain, Global.StoredResultsPath, Utils.ResolveUrl(Global.StoredResultsPath) });
                }
            }
            catch (ExceptionReport e)
            {
                if (operationDomain != null) AppDomain.Unload(operationDomain);
                throw new ExceptionReport(e, "An error occurred when invoking the process: " + Identifier + ". Check the process parameters. If necessary, contact the administrator.");
            }
            catch (Exception e)
            {
                if (operationDomain != null) AppDomain.Unload(operationDomain);
                throw new ExceptionReport("An error occurred when invoking the process: " + Identifier + ". Contact the administrator.\n" + e.ToString());
            }
            return result;
        }
    }
}
