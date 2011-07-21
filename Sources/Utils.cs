/// WPS.NET
/// A .NET implementation of OGC Web Processing Service v1.0.0
/// (c) brgm 2010

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Security;
using System.Security.Policy;
using System.Web;
using System.Xml;

namespace WPS.NET
{
    public class Utils
    {
        // Helper functions

        public static XmlNamespaceManager CreateWPSNamespaceManager(XmlDocument doc)
        {
            // Create an XmlNamespaceManager for resolving namespaces.
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(doc.NameTable);
            nsmgr.AddNamespace("", Global.WPSNamespace);
            nsmgr.AddNamespace("wps", Global.WPSNamespace);
            nsmgr.AddNamespace("ows", Global.OWSNamespace);
            return nsmgr;
        }

        public static bool StrICmp(string s, string t)
        {
            if (s == null && t == null) return true;
            if (s == null || t == null) return false;
            return s.Equals(t, StringComparison.OrdinalIgnoreCase);
        }

        public static string EncodeURI(string str)
        {
            return HttpUtility.UrlEncode(str);
        }

        public static string DecodeURI(string str)
        {
            return HttpUtility.UrlDecode(str);
        }

        public static string FormatStringForXml(string str)
        {
            return SecurityElement.Escape(str);
        }

        public static string MapPath(string path)
        {
            return HttpContext.Current.Server.MapPath(path);
        }

        public static string GetParameter(string param, string defValue)
        {
            try
            {
                foreach (string p in HttpContext.Current.Request.Params)
                    if (Utils.StrICmp(p, param))
                        return HttpContext.Current.Request[p];
            }
            catch
            {
            }
            return defValue;
        }

        public static string GetParameter(string param)
        {
            return GetParameter(param, null);
        }

        public static string GetXmlAttributesValue(XmlNode node, string attribute, string defValue)
        {
            foreach (XmlAttribute attribNode in node.Attributes)
                if (Utils.StrICmp(attribNode.Name, attribute))
                    return attribNode.InnerText;
            return defValue;
        }

        public static string GetXmlAttributesValue(XmlNode node, string attribute)
        {
            return GetXmlAttributesValue(node, attribute, null);
        }

        public static byte[] GetReferenceDataFromGetRequest(string address)
        {
            byte[] data = null;
            HttpWebResponse response = null;
            BinaryReader reader = null;
            try
            {
                UriBuilder builder = new UriBuilder(address);
                HttpWebRequest webReq = (HttpWebRequest)WebRequest.Create(builder.Uri);
                webReq.Credentials = new NetworkCredential(builder.UserName, builder.Password);
                webReq.Method = "GET";
                // synchrone request
                response = (HttpWebResponse)webReq.GetResponse();
                if (response.ContentLength <= int.MaxValue)
                {
                    reader = new BinaryReader(response.GetResponseStream());
                    data = reader.ReadBytes((int)response.ContentLength);
                }
                else
                {
                    throw new ExceptionReport("File size too large, above "
                    + int.MaxValue / 1048576 + " bytes", ExceptionCode.FileSizeExceeded);
                }
            }
            finally
            {
                if (response != null) response.Close();
                if (reader != null) reader.Close();
            }
            return data;
        }

        public static byte[] GetReferenceDataFromPostRequest(string address, string Request, Dictionary<string,string> Header)
        {
            byte[] data = null;
            HttpWebResponse response = null;
            BinaryReader reader = null;
            try
            {
                UriBuilder builder = new UriBuilder(address);
                HttpWebRequest webReq = (HttpWebRequest)WebRequest.Create(builder.Uri);
                webReq.Credentials = new NetworkCredential(builder.UserName, builder.Password);
                webReq.Method = "POST";
                foreach (KeyValuePair<string,string> kvp in Header)
                    webReq.Headers.Add(kvp.Key, kvp.Value);

                byte[] byteArray = System.Text.Encoding.UTF8.GetBytes(Request);
                webReq.ContentLength = byteArray.Length;
                webReq.ContentType = "application/x-www-form-urlencoded";
                Stream dataStream = webReq.GetRequestStream();
                dataStream.Write(byteArray, 0, byteArray.Length);
                dataStream.Close();
                // synchrone request
                response = (HttpWebResponse)webReq.GetResponse();
                if (response.ContentLength <= int.MaxValue)
                {
                    reader = new BinaryReader(response.GetResponseStream());
                    data = reader.ReadBytes((int)response.ContentLength);
                }
                else
                {
                    throw new ExceptionReport("File size too large, above "
                    + int.MaxValue / 1048576 + " bytes", ExceptionCode.FileSizeExceeded);
                }
            }
            finally
            {
                if (response != null) response.Close();
                if (reader != null) reader.Close();
            }
            return data;
        }

        public static AppDomain CreateDomain()
        {
            // Appdomainsetup contains the settings of the appdomain we are creating
            AppDomainSetup operationDomainSetup = new AppDomainSetup();
            operationDomainSetup.ApplicationBase = AppDomain.CurrentDomain.BaseDirectory;
            operationDomainSetup.ApplicationName = "operationDomain";
            operationDomainSetup.PrivateBinPath = AppDomain.CurrentDomain.RelativeSearchPath;
            // The evidences of the new domain are get from the main one.
            Evidence evidence = null; //new Evidence(AppDomain.CurrentDomain.Evidence);
            return AppDomain.CreateDomain("operationDomain", evidence, operationDomainSetup);
        }

        /// <summary>
        /// AssemblyLoader class is used to load assemblies in secondary application domains.
        /// </summary>
        public class AssemblyLoader : MarshalByRefObject
        {
            private Assembly _assembly; // The associated assembly

            public static AssemblyLoader Create(AppDomain domain)
            {
                return (AssemblyLoader)domain.CreateInstanceAndUnwrap(typeof(AssemblyLoader).Assembly.FullName, typeof(AssemblyLoader).FullName);
            }

            public override object InitializeLifetimeService()
            {
                return null;
            }

            public void Load(string assemblyPath)
            {
                _assembly = Assembly.Load(AssemblyName.GetAssemblyName(assemblyPath));
            }

            // Returns the value of a class property
            public object GetProperty(string typeName, string propertyName, params object[] parameters)
            {
                Type ProcessType = _assembly.GetType(typeName);
                Object ProcessInstance = Activator.CreateInstance(ProcessType);
                try
                {
                    return ProcessType.InvokeMember(propertyName, BindingFlags.Public | BindingFlags.GetField | BindingFlags.GetProperty, null, ProcessInstance, parameters);
                    //return ProcessType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance).GetValue(ProcessInstance, null);
                }
                catch (TargetInvocationException ex)
                {
                    throw ex.InnerException;
                }

            }

            // Returns the result of the execution of a method
            public object ExecuteMethod(string typeName, string methodName, object[] parameters)
            {
                Type ProcessType = _assembly.GetType(typeName);
                Object ProcessInstance = Activator.CreateInstance(ProcessType);
                try
                {
                    //return ProcessType.InvokeMember(methodName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.InvokeMethod, null, ProcessInstance, parameters);
                    return ProcessType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance).Invoke(ProcessInstance, parameters);
                }
                catch (TargetInvocationException ex)
                {
                    throw ex.InnerException;
                }
            }
        }

        /// <summary>
        /// FileLoader class is used to get a local filepath from a local or remote file.
        /// If remote file, it retrieves it in a temp folder and deletes it on object destruction.
        /// If local file, the filename is prefixed with WPS_DB_PATH
        /// </summary>
        [Obsolete("Use references instead")]
        public class FileLoader
        {
            private Uri _uri; // The contained uri
            private string _fileName;

            public FileLoader(string url)
            {
                _uri = new Uri(url, UriKind.RelativeOrAbsolute);
                if (_uri.IsAbsoluteUri)
                {
                    if (_uri.Scheme == Uri.UriSchemeFile)
                        throw new ExceptionReport("Failed: absolute file path are not allowed on the server!");

                    // Download the remote file in a temporary file (preserve the extension of the requested file)
                    _fileName = Path.GetTempPath() + Path.GetFileNameWithoutExtension(Path.GetRandomFileName()) + "-" + Path.GetFileName(_uri.AbsolutePath);
                    WebClient wb = new WebClient();
                    try
                    {
                        wb.DownloadFile(_uri, _fileName);
                    }
                    catch (WebException /*e*/)
                    {
                        throw new ExceptionReport("Failed: " + url + " is not available...");
                    }
                }
                else
                {	// Relative and local path
                    string wpsPath = Environment.GetEnvironmentVariable("WPS_DB_PATH");
                    if (String.IsNullOrEmpty(wpsPath))
                        throw new ExceptionReport("Failed: Environment variable WPS_DB_PATH is not set...");

                    _fileName = System.IO.Path.Combine(wpsPath, url);
                    if (!File.Exists(_fileName))
                        throw new ExceptionReport("Failed: " + url + " not found on the server...");
                }
            }

            ~FileLoader()
            {
                if (_uri.IsAbsoluteUri && _fileName.StartsWith(Path.GetTempPath())) File.Delete(_fileName);
            }

            public string FileName
            {
                get { return _fileName; }
            }
        }
    }
}
