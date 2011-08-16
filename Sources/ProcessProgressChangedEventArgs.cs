/// WPS.NET
/// A .NET implementation of OGC Web Processing Service v1.0.0
/// (c) brgm 2010-2011

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WPS.NET
{
    [Serializable]
    public class ProcessProgressChangedEventArgs
    {
        public ProcessData ProcessData;

        public ProcessProgressChangedEventArgs(ProcessData processData)
        {
            ProcessData = processData;
        }
    }

    [Serializable]
    public class ProcessData
    {
        public ProcessReturnValue ExecuteResponseValue;
        public AppDomain ParentApplicationDomain;
        public ProcessDescription ProcessDescription;
        public Exception Error;
        public AppDomain ProcessApplicationDomain;

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
