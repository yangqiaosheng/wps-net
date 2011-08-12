/// WPS.NET
/// A .NET implementation of OGC Web Processing Service v1.0.0
/// (c) brgm 2010

using System;
using System.ComponentModel;
using System.IO;
using System.Web;
using System.Web.Services;
using System.Xml;
using System.Threading;

namespace WPS.NET
{
    /// <summary>
    /// WPService is the service implementation
    /// </summary>
    
    [WebService(Namespace = "http://wps.brgm.fr/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [ToolboxItem(false)]
    //[System.Web.Script.Services.ScriptService] // Authorize the script call via ASP.NET AJAX 

    public class WPService : System.Web.Services.WebService
    {
        /// <summary>
        /// WPS is the gate to access the different services of the server. 
        /// It determines which operation - GetCapabilities, DescribeProcess or Execute - must be performed.
        /// </summary>
        [WebMethod]
        public XmlDocument WPS()
        {
            this.Context.Response.ContentType = "text/xml";
            HttpContext.Current.Response.AddHeader("Access-Control-Allow-Origin", "*");

            try
            {
                if (HttpContext.Current.Request.HttpMethod == "GET")
                    return RunFromHTTPGet();
                else
                    return RunFromHTTPPost();
            }
            catch (ExceptionReport e)
            {
                HttpContext.Current.Response.StatusCode = 200;
                return e.GetReport();
                //XmlDocument report = Utils.GetExceptionReport();
                // report != null => chaining of exceptions (created with Utils.makeException)
                // static variable exception should be destroyed
                //return report != null ? report : e.GetReport();
                //return Utils.MakeError(e.Message);          
            }
                // this catch is TEMP since every error should be encapsulated in a ExceptionReport
            catch (Exception e)
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(Global.XmlHeader + "<Error>Unexpected error, contact your administrator.\n"
                    + Utils.FormatStringForXml(e.Message) + "</Error>");
                HttpContext.Current.Response.StatusCode = 200;
                return doc;
            }
        }

        public XmlDocument RunFromHTTPGet()
        {
            string request = Utils.GetParameter("request");
            string service = Utils.GetParameter("service");
            string version = Utils.GetParameter("version");
            string language = Utils.GetParameter("language", Global.DefaultLanguage);

            ExceptionReport exception = null;

            if (service != "WPS")
                exception = new ExceptionReport(exception, "The service requested must be a WPS service.",
                    ExceptionCode.InvalidParameterValue, "service");

            if (!Global.SupportedLanguages.Contains(language))
                exception = new ExceptionReport(exception, "The language '" + language + "' is not supported by this WPS service. " +
                    "Use one of the followings: " + string.Join(", ", Global.SupportedLanguages.ToArray()),
                    ExceptionCode.InvalidParameterValue, "language");

            if (string.IsNullOrEmpty(request))
                exception = new ExceptionReport(exception, ExceptionCode.MissingParameterValue, "request");

            if (exception != null)
                throw exception;

            // Execute an operation depending of the Request
            // version attribute is not present in a GetCApabilities request
            if (Utils.StrICmp(request, "GetCapabilities"))
                return GetCapabilities.RunFromHTTPGet(language);

            // TODO handle many versions is possible. Do it ?
            if (string.IsNullOrEmpty(version))
                throw new ExceptionReport(ExceptionCode.MissingParameterValue, "version");

            if (version != Global.WPSVersion)
                throw new ExceptionReport("The requested version '" + version + "' is not supported by this WPS server.",
                    ExceptionCode.VersionNegotiationFailed, "version");

            if (Utils.StrICmp(request, "DescribeProcess"))
                return DescribeProcess.RunFromHTTPGet(language);

            if (Utils.StrICmp(request, "Execute"))
                return Execute.RunFromHTTPGet(language);

            throw new ExceptionReport("The requested operation '" + request + "' is unknown.",
                ExceptionCode.InvalidParameterValue, "request");
        }

        public XmlDocument RunFromHTTPPost()
        {
            XmlDocument doc = new XmlDocument();
         
            try
            {
                StreamReader MyStreamReader = new StreamReader(this.Context.Request.InputStream);
                doc.LoadXml(MyStreamReader.ReadToEnd());
                MyStreamReader.Close();
            }
            catch (Exception e)
            {
                throw new ExceptionReport("Error when reading posted data. The xml syntax seems incorrect:\n" + e.Message);
            }

            XmlNamespaceManager nsmgr = Utils.CreateWPSNamespaceManager(doc);

            XmlNode requestNode = doc.DocumentElement;

            string service = Utils.GetXmlAttributesValue(requestNode, "service");
            string version = Utils.GetXmlAttributesValue(requestNode, "version");
            string language = Utils.GetXmlAttributesValue(requestNode, "language", Global.DefaultLanguage);

            ExceptionReport exception = null;

            if (service != "WPS")
                exception = new ExceptionReport("The service requested must be a WPS service.",
                    ExceptionCode.InvalidParameterValue, "service");

            if (!Global.SupportedLanguages.Contains(language))
                exception = new ExceptionReport(exception, "The language '" + language + "' is not supported by this WPS service. " +
                    "Use one of the followings: " + string.Join(", ", Global.SupportedLanguages.ToArray()),
                    ExceptionCode.InvalidParameterValue, "language");

            if (string.IsNullOrEmpty(requestNode.Name))
                exception = new ExceptionReport(exception, ExceptionCode.MissingParameterValue, "request");

            if (exception != null)
                throw exception;

            if (doc.SelectSingleNode("/wps:GetCapabilities", nsmgr) == requestNode)
                return GetCapabilities.RunFromHTTPPost(requestNode, language);

            if (string.IsNullOrEmpty(version))
                throw new ExceptionReport(ExceptionCode.MissingParameterValue, "version");
            else if (version != Global.WPSVersion)
                throw new ExceptionReport("The requested version '" + version + "' is not supported by this WPS server.",
                    ExceptionCode.VersionNegotiationFailed, "version");

            else if (doc.SelectSingleNode("/wps:DescribeProcess", nsmgr) == requestNode)
                return DescribeProcess.RunFromHTTPPost(requestNode, language);

            else if (doc.SelectSingleNode("/wps:Execute", nsmgr) == requestNode)
                return Execute.RunFromHTTPPost(requestNode, language);

            throw new ExceptionReport("The requested operation '" + requestNode.Name + "' is unknown.",
                ExceptionCode.InvalidParameterValue, "request");
        }
    }
}
