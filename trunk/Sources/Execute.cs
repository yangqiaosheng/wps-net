/// WPS.NET
/// A .NET implementation of OGC Web Processing Service v1.0.0
/// (c) brgm 2010

using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Xml;

namespace WPS.NET
{
    public class Execute
    {
        /// <summary>
        /// Execute is called when the client wants to consume a service
        /// </summary>

        public static XmlDocument RunFromHTTPGet(string language)
        {
            ResponseFormType responseForm = new ResponseFormType("wps:ResponseForm");
            string processId = Utils.GetParameter("Identifier");

            if (string.IsNullOrEmpty(processId))
                throw new ExceptionReport(ExceptionCode.MissingParameterValue, "Identifier");

            List<InputData> processInputParams = null;
            List<OutputData> processOutputParams = null;
            ProcessDescription processDescription = null;

            processDescription = ProcessDescription.GetProcessDescription(processId);
            processInputParams = processDescription.GetProcessInputParameters();
            processOutputParams = processDescription.GetProcessOutputParameters();

            List<InputData> inputParams = new List<InputData>();
            //string p = Utils.DecodeURI(Utils.GetParameter("DataInputs"));
            string p = Utils.GetParameter("DataInputs");

            ExceptionReport exception = null;

            if (!String.IsNullOrEmpty(p))
            {
                string[] tokens = p.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string param in tokens)
                {
                    string[] kv = param.Split(new char[] { '=' }, 2, StringSplitOptions.RemoveEmptyEntries);
                    if (kv.Length == 2)
                    {
                        InputData input = processDescription.GetProcessInputParameter(kv[0]);
                        if (input != null)
                        {
                            InputData myinput = input.Clone();
                            try
                            {
                                myinput.ParseValue(kv[1]);
                            }
                            catch (ExceptionReport e)
                            {
                                exception = new ExceptionReport(e, exception);
                            }
                            inputParams.Add(myinput);
                        }
                        else
                        {
                            exception = new ExceptionReport(exception, "The parameter " + kv[0] +
                                " is not a valid parameter for this execute request!", ExceptionCode.InvalidParameterValue, kv[0]);
                        }
                    }
                }
            }

            if (exception != null)
                throw exception;

            //List<string> outputIds = new List<string>();

            responseForm.Parse(processDescription);

            return Run(processDescription, inputParams, responseForm);
        }

        public static XmlDocument RunFromHTTPPost(XmlNode requestNode, string language)
        {
            XmlNamespaceManager nsmgr = Utils.CreateWPSNamespaceManager(requestNode.OwnerDocument);
            XmlNode processNode = requestNode.SelectSingleNode("ows:Identifier", nsmgr);

            if (processNode == null || string.IsNullOrEmpty(processNode.InnerText))
                throw new ExceptionReport(ExceptionCode.MissingParameterValue, "ows:Identifier");

            string processId = processNode.InnerText;

            //List<InputData> processInputParams = null;
            //List<OutputData> processOutputParams = null;
            ProcessDescription processDescription = null;

            processDescription = ProcessDescription.GetProcessDescription(processId);
            //processInputParams = processDescription.GetProcessInputParameters();
            //processOutputParams = processDescription.GetProcessOutputParameters();

            List<InputData> inputParams = new List<InputData>();

            ExceptionReport exception = null;

            XmlNodeList inputs = requestNode.SelectNodes("wps:DataInputs/wps:Input", nsmgr);
            foreach (XmlNode node in inputs)
            {
                XmlNode nodeid = node.SelectSingleNode("ows:Identifier", nsmgr);
                if (nodeid == null)
                {
                    exception = new ExceptionReport(exception, "The parameter <ows:Identifier> is missing!",
                        ExceptionCode.MissingParameterValue, "ows:Identifier");
                    continue;
                }

                InputData input = processDescription.GetProcessInputParameter(nodeid.InnerText);
                if (input == null)
                {
                    exception = new ExceptionReport(exception, "The parameter " + nodeid.InnerText +
                        " is not a valid parameter for this execute request!",
                        ExceptionCode.InvalidParameterValue, nodeid.InnerText);
                    continue;
                }
                InputData myinput = input.Clone();
                try
                {
                    myinput.ParseValue(node);
                    inputParams.Add(myinput);
                }
                catch (ExceptionReport e)
                {
                    exception = new ExceptionReport(e, exception);
                }
            }

            if (exception != null)
                throw exception;

            ResponseFormType responseForm = new ResponseFormType("wps:ResponseForm");

            XmlNode responseFormNode = requestNode.SelectSingleNode("wps:ResponseForm", nsmgr);
            responseForm.Parse(responseFormNode, processDescription);

            return Execute.Run(processDescription, inputParams, responseForm);
        }

        public static XmlDocument Run(ProcessDescription processDescription, List<InputData> inputParams, ResponseFormType responseForm)
        {
            /* error is unreachable because check (via throwing an exception) is done before
            if (processDescription == null)
                throw new ExceptionReport("The ows:Identifier tag of the process can't be found in the xml file. It must be placed under the root 'Execute' node.",
                    ExceptionCode.MissingParameterValue);*/

            string processId = processDescription.Identifier;

            List<InputData> processInputParams = processDescription.GetProcessInputParameters();
            List<OutputData> processOutputParams = processDescription.GetProcessOutputParameters();

            ExceptionReport exception = null;

            ProcessInputParams args = new ProcessInputParams();

            // Get and check input parameters
            foreach (InputData processInputParam in processInputParams)
            {
                int occurs = 0;
                bool loop = processInputParam.MaxOccurs > 0 || processInputParam.MaxOccurs == -1;
                List<InputData> iargs = new List<InputData>();
                while (loop)
                {
                    loop = false;
                    foreach (InputData input in inputParams)
                    {
                        if (input.Identifier != processInputParam.Identifier) continue;
                        if (!input.IsValueAllowed())
                            exception = new ExceptionReport(exception, "The parameter "
                                + input.Identifier + " has not a valid value!",
                                ExceptionCode.InvalidParameterValue, input.Identifier);
                        occurs++;
                        iargs.Add(input.Clone());
                        inputParams.Remove(input);
                        loop = true;
                        break;
                    }
                }

                if (occurs < processInputParam.MinOccurs || (occurs > processInputParam.MaxOccurs && processInputParam.MaxOccurs != -1))
                    exception = new ExceptionReport(exception, "The parameter "
                        + processInputParam.Identifier + " has " + occurs
                        + " occurrences but it should have at least " + processInputParam.MinOccurs
                       + " and at most " + processInputParam.MaxOccurs + " occurrences.",
                       ExceptionCode.InvalidParameterValue, processInputParam.Identifier);

                // default value for LiteralData
                if (occurs == 0 && processInputParam.asLiteralInput() != null
                    && !String.IsNullOrEmpty(processInputParam.asLiteralInput().Default))
                    iargs.Add(processInputParam);

                args.parameters[processInputParam.Identifier] = iargs.ToArray();
            }

            if (exception != null)
                throw exception;

            ProcessReturnValue result = null;
            try
            {
                result = processDescription.CallProcess(args, responseForm);
            }
            catch (ExceptionReport e)
            {
                if (responseForm.responseDocument != null && responseForm.responseDocument.status)
                    exception = e;
                else
                    throw;// new ExceptionReport(e, "Error during process...", ExceptionCode.NoApplicableCode);
            }

            int requestedOutputCount = result.GetOutputIdentifiers().Count;
            int returnedOutputCount = result.returnValues.Count;

            // Problem during the process (validity check is done before launching the process)!
            if (requestedOutputCount != returnedOutputCount)
                throw new ExceptionReport(String.Format("The process has generated {0} output{1} but {2} {3} requested. Contact the administrator to fix the process issue.",
                    returnedOutputCount, returnedOutputCount > 1 ? "s" : "",
                    requestedOutputCount, requestedOutputCount > 1 ? "were" : "was"),
                    ExceptionCode.NoApplicableCode);

            if (responseForm.outputDefinition != null)
            {
                
                OutputData data = result.returnValues[0];

                if (result.fileName == "") result.fileName = processId + "RawDataOuput";
                HttpContext.Current.Response.Clear();
                HttpContext.Current.Response.ClearHeaders();
                HttpContext.Current.Response.StatusCode = 200;
                HttpContext.Current.Response.Buffer = true;
                // not needed because rawdataoutput can only concern a ComplexOutput
                //string mimeType = (data is ComplexOutput) ? ((ComplexOutput)data).format.mimeType : "text/plain";
                string mimeType = data.asComplexOutput().Format.mimeType;
                HttpContext.Current.Response.ContentType = mimeType;
                string dispo = true ? "inline" : "attachment";
                HttpContext.Current.Response.AddHeader("Content-Disposition", dispo + "; filename=" + System.Uri.EscapeDataString(result.fileName));
                HttpContext.Current.Response.AddHeader("Access-Control-Allow-Origin", "*");
                byte[] content = data.ToByteArray();
                HttpContext.Current.Response.AddHeader("Content-Length", "" + content.Length);
                HttpContext.Current.Response.AddHeader("cache-control", "must-revalidate");
                HttpContext.Current.Response.OutputStream.Write(content, 0, content.Length);
                HttpContext.Current.Response.Flush();
                HttpContext.Current.ApplicationInstance.CompleteRequest();

                return new XmlDocument();
            }
            else
            {
                // Format the response message

                StringBuilder retString = new StringBuilder();

                retString.Append(Global.XmlHeader + "<wps:ExecuteResponse " + Global.WPSServiceVersion
                    + " xml:lang='" + processDescription.Language + "' serviceInstance='"
                    + HttpContext.Current.Request.Url.AbsoluteUri.Split('?')[0]
                    + "?service=WPS&amp;Request=GetCapabilities' "
                    + Global.WPSXmlSchemas + " " + Global.WPSExecuteSchema + ">");

                retString.Append("<wps:Process><ows:Identifier>" + processDescription.Identifier + "</ows:Identifier><ows:Title>"
                    + processDescription.Title + "</ows:Title></wps:Process>");
                if (responseForm.responseDocument.status)
                    retString.Append("<wps:Status creationTime=\"" + System.DateTime.Now.ToString("s") + "\">" +
                        (result.status ? "<wps:ProcessSucceeded>" +
                            (result.statusMessage != "" ? result.statusMessage : "Process completed successfully.") + "</wps:ProcessSucceeded>"
                        : "<wps:ProcessFailed>" + (exception != null ? exception.GetReport()
                        : new ExceptionReport("Failed to execute WPS process", ExceptionCode.NoApplicableCode).GetReport())
                        + "</wps:ProcessFailed>")
                        + "</wps:Status>");

                if (responseForm.responseDocument.lineage)
                {
                    retString.Append("<wps:DataInputs>");
                    foreach (KeyValuePair<string, InputData[]> ent in args.parameters)
                        foreach (InputData processInputParam in ent.Value)
                            retString.Append(processInputParam.GetXmlValue());
                    retString.Append("</wps:DataInputs>");

                    retString.Append("<wps:OutputDefinitions>");
                    // TODO do not retrieve output from returnvalues (may not be the same as request)
                    //foreach (OutputData processOutputParam in result.returnValues)
                    foreach (OutputData processOutputParam in responseForm.responseDocument.Outputs)
                        retString.Append(processOutputParam.GetXmlDescription());
                    retString.Append("</wps:OutputDefinitions>");
                }

                retString.Append("<wps:ProcessOutputs>");
                foreach (OutputData outputData in result.returnValues)
                    retString.Append(outputData.GetXmlValue());
                retString.Append("</wps:ProcessOutputs>");
                retString.Append("</wps:ExecuteResponse>");

                try
                {
                    XmlDocument doc = new XmlDocument();
                    doc.LoadXml(retString.ToString());
                    HttpContext.Current.Response.StatusCode = 200;
                    return doc;
                }
                catch (Exception e)
                {
                    throw new ExceptionReport("The service execution has encountered an error while formatting the result stream. Check the parameters values.\n" + e.ToString());
                }
            }
        }
    }
}
