using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Runtime.Serialization;
using System.Configuration;

namespace WPS.NET
{
    [Serializable]
    public abstract class IAsyncProcess
    {
        public ProcessReturnValue ExecuteResponseValue { get; set; }
        public ProcessDescription ProcessDescription { get; set; }
        public ProcessInputParams Args { get; set; }
        public String StoredResultPath { get; private set; }
        public AppDomain MainAppDomain { get; set; }
        public String BaseUrlToResultPath { get; private set; }
        protected string startDate;

        public IAsyncProcess(AppDomain mainDomain = null, string storedResultPath = null, string baseUrlToResultPath = null)
        {
            MainAppDomain = mainDomain;
            StoredResultPath = (storedResultPath == null ? String.Empty : storedResultPath + this.GetType().ToString() + "/");
            BaseUrlToResultPath = (baseUrlToResultPath == null ? String.Empty : baseUrlToResultPath + this.GetType().ToString() + "/");
            ExecuteResponseValue = null;
            startDate = DateTime.Now.ToString("yyyy_MM_dd_hh_mm_ss_ffff");
            ProcessDescription = this.GetDescription();
        }

        public delegate void ProgressChangedEventHandler(object sender, ProcessProgressChangedEventArgs e);
        public event ProgressChangedEventHandler ProcessProgressChanged;

        protected virtual void OnProcessProgressChanged(ProcessProgressChangedEventArgs e)
        {
            if (ProcessProgressChanged != null)
            {
                ProcessProgressChanged(this, e);
            }

        }

        public abstract ProcessDescription GetDescription();

        public abstract ProcessReturnValue Execute(ProcessInputParams args, ProcessReturnValue ret);
    }
}