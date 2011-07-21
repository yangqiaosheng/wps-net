using System;
using System.Collections.Generic;
using System.Configuration;

namespace WPS.NET
{
    public class Global : System.Web.HttpApplication
    {
        public static string WPSVersion = "1.0.0";
        public static string DefaultLanguage = "en-US";
        public static List<string> SupportedLanguages = new List<string>() { "en-US", "fr-FR" };
        public static string OWSNamespace = "http://www.opengis.net/ows/1.1";
        public static string WPSNamespace = "http://www.opengis.net/wps/" + WPSVersion;
        public static string XLinkNamespace = "http://www.w3.org/1999/xlink";
        public static string XSINamespace = "http://www.w3.org/2001/XMLSchema-instance";

        public static string XmlHeader = "<?xml version='1.0' encoding='UTF-8' ?>";
        public static string WPSXmlSchemas = "xmlns:wps='" + WPSNamespace + "' xmlns:ows='" + OWSNamespace + "' xmlns:xlink='" + XLinkNamespace + "' xmlns:xsi='" + XSINamespace + "'";
        public static string WPSServiceVersion = "service='WPS' version='" + WPSVersion + "'";
        public static string WPSExceptionSchema = "xsi:schemaLocation='" + OWSNamespace + " owsExceptionReport.xsd'";

        public static string WPSGetCapabilitiesSchema = "xsi:schemaLocation='" + WPSNamespace + " http://schemas.opengis.net/wps/" + WPSVersion + "/wpsGetCapabilities_response.xsd'";
        public static string WPSDescribeProcessSchema = "xsi:schemaLocation='" + WPSNamespace + " http://schemas.opengis.net/wps/" + WPSVersion + "/wpsDescribeProcess_response.xsd'";
        public static string WPSExecuteSchema = "xsi:schemaLocation='" + WPSNamespace + " http://schemas.opengis.net/wps/" + WPSVersion + "/wpsExecute_response.xsd'";

        public static string ProcessesBinPath = ConfigurationManager.AppSettings["ProcessesBinPath"];
        public static string ServiceCapabilitiesPath = ConfigurationManager.AppSettings["ServiceCapabilitiesPath"];

        protected void Application_Start(object sender, EventArgs e)
        {
        }

        protected void Session_Start(object sender, EventArgs e)
        {
        }

        protected void Application_BeginRequest(object sender, EventArgs e)
        {
        }

        protected void Application_AuthenticateRequest(object sender, EventArgs e)
        {
        }

        protected void Application_Error(object sender, EventArgs e)
        {
        }

        protected void Session_End(object sender, EventArgs e)
        {
        }

        protected void Application_End(object sender, EventArgs e)
        {
        }
    }
}