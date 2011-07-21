/// WPS.NET
/// A .NET implementation of OGC Web Processing Service v1.0.0
/// (c) brgm 2010

using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Xml;

namespace WPS.NET
{
    public class DescribeProcess
    {
        /// <summary>
        /// DescribeProcess returns an Xml file containing all the information about the processes requested by the client.
        /// </summary> 

        public static XmlDocument RunFromHTTPGet(string language)
        {
            string identifier = Utils.GetParameter("Identifier");
            if (string.IsNullOrEmpty(identifier))
                throw new ExceptionReport(ExceptionCode.MissingParameterValue, "Identifier");

            return Run(identifier.Split(','), language);
        }

        public static XmlDocument RunFromHTTPPost(XmlNode requestNode, string language)
        {
            XmlNamespaceManager nsmgr = Utils.CreateWPSNamespaceManager(requestNode.OwnerDocument);

            // Retrieve all the processes Identifiers requested
            XmlNodeList ids = requestNode.SelectNodes("//ows:Identifier", nsmgr);
            List<string> processes = new List<string>();
            if (ids.Count == 0)
                throw new ExceptionReport(ExceptionCode.MissingParameterValue, "ows:Identifier");

            foreach (XmlNode node in ids) processes.Add(node.InnerText);
            return Run(processes.ToArray(), language);
        }

        public static XmlDocument Run(string[] processes, string Language)
        {
            StringBuilder retString = new StringBuilder();

            retString.Append(Global.XmlHeader + "<wps:ProcessDescriptions " + Global.WPSServiceVersion + " xml:lang='" + Language + "' " + Global.WPSXmlSchemas + " " + Global.WPSDescribeProcessSchema + ">");

            // On identifier is ALL, catch all the available processes
            if (processes.Length == 1 && Utils.StrICmp(processes[0], "ALL"))
            {
                ProcessDescription[] processDescriptions = GetCapabilities.getAvailableProcesses();
                foreach (ProcessDescription process in processDescriptions)
                    retString.Append(process.GetProcessDescriptionDocument());
            }
            else
            {
                ExceptionReport exception = null;

                // Loop through all the processes and get process description
                foreach (string processId in processes)
                {
                    try
                    {
                        retString.Append(ProcessDescription.GetProcessDescription(processId).GetProcessDescriptionDocument());
                    }
                    catch (ExceptionReport e)
                    {
                        exception = new ExceptionReport(e, exception);
                    }
                }

                if (exception != null)
                    throw exception;
            }

            retString.Append("</wps:ProcessDescriptions>");

            try
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(retString.ToString());
                HttpContext.Current.Response.StatusCode = 200;
                return doc;
            }
            catch
            {
                throw new ExceptionReport("Unable to generate the description document. Contact the administrator.");
            }
        }
    }
}
