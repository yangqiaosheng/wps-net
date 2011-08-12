using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WPS.NET
{
    [Serializable]
    public class ProcessProgressChangedEventArgs
    {
        public ProcessData ProcessData { get; set; }
        public ProcessProgressChangedEventArgs(ProcessData processData)
        {
            ProcessData = processData;
        }
    }

    [Serializable]
    public class ProcessData
    {
        public ProcessReturnValue ExecuteResponseValue { get; set; }
        public AppDomain ParentApplicationDomain { get; set; }
        public ProcessDescription ProcessDescription { get; set; }
        public Exception Error { get; set; }
        public AppDomain ProcessApplicationDomain { get; set; }

        public ProcessData(ProcessReturnValue executeResponseValue, ProcessDescription processDescription, AppDomain parentAppDomain, AppDomain processAppDompain, Exception processError = null)
        {
            this.ExecuteResponseValue = executeResponseValue;
            this.ParentApplicationDomain = parentAppDomain;
            this.ProcessDescription = processDescription;
            this.Error = processError;
            this.ProcessApplicationDomain = processAppDompain;
        }
    }
}
