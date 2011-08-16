/// WPS.NET
/// A .NET implementation of OGC Web Processing Service v1.0.0
/// (c) brgm 2010-2011

using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Web;
using System.Xml;

namespace WPS.NET
{
    public class GetCapabilities
    {
        /// <summary>
        /// GetCapabilities returns an Xml stream containing all the information about the service and the available processes.
        /// </summary>
        /// 

        public static XmlDocument RunFromHTTPGet(string language)
        {
            return Run(language);
        }

        public static XmlDocument RunFromHTTPPost(XmlNode requestNode, string language)
        {
            return Run(language);
        }

        public static XmlDocument Run(string language)
        {
            XmlDocument doc = new XmlDocument();
            try
            {
                doc.Load(Utils.MapPath(Global.ServiceCapabilitiesPath));
            }
            catch
            {
                throw new ExceptionReport("Unable to read the file containing the static part of the GetCapabilities information. Contact the administrator.");
            }

            // set language response
            doc.DocumentElement.SetAttribute("xml:lang", language);

            // Get the name of all the available processes
            ProcessDescription[] processes = getAvailableProcesses();

            StringBuilder descriptions = new StringBuilder();
            // Loop through all the processes and add process names and titles to the response
            foreach (ProcessDescription process in processes)
            {
                descriptions.Append("<wps:Process wps:processVersion=\"" + process.Version + "\">");
                descriptions.Append(process.GetProcessBriefDescription());
                descriptions.Append("</wps:Process>");
            }

            StringBuilder langs = new StringBuilder();
            langs.Append("<wps:Default><ows:Language>" + Global.DefaultLanguage + "</ows:Language></wps:Default><wps:Supported>");
            foreach (string lang in Global.SupportedLanguages) langs.Append("<ows:Language>" + lang + "</ows:Language>");
            langs.Append("</wps:Supported>");

            try
            {
                XmlNode node = doc.CreateElement("wps:ProcessOfferings", Global.WPSNamespace);
                doc.DocumentElement.AppendChild(node);
                node.InnerXml = descriptions.ToString();
                XmlNode lang = doc.CreateElement("wps:Languages", Global.WPSNamespace);
                doc.DocumentElement.AppendChild(lang);
                lang.InnerXml = langs.ToString();
                HttpContext.Current.Response.StatusCode = 200;
                return doc;
            }
            catch
            {
                throw new ExceptionReport("Unable to generate the capabilities document. Contact the administrator.");
            }
        }

        // Return the available processes (for which a dll is present and an xml description file is associated)
        public static ProcessDescription[] getAvailableProcesses()
        {
            // Search all the DLL files could contain process operations
            string[] processDLLPath;
            try
            {
                processDLLPath = Directory.GetFiles(Utils.MapPath(Global.ProcessesBinPath), "*.dll");
            }
            catch
            {
                throw new ExceptionReport("Unable to retrieve the list of assemblies containing the processes available on the server. Contact the administrator.");
            }

            // Retrieve the name of all the available processes
            List<string> processIdList = new List<string>();
            for (int i = 0; i < processDLLPath.Length; i++)
            {
                string processDllName = processDLLPath[i].Substring(processDLLPath[i].LastIndexOf("\\") + 1, processDLLPath[i].IndexOf(".dll") - processDLLPath[i].LastIndexOf("\\") - 1);
                processIdList.Add(processDllName);
            }

            List<ProcessDescription> processes = new List<ProcessDescription>();
            foreach (string processId in processIdList)
            {
                try
                {
                    processes.Add(ProcessDescription.GetProcessDescription(processId));
                }
                catch
                {
                }
            }

            return processes.ToArray();
        }
    }
}
