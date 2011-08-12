using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WPS.NET
{
    public enum ProcessState
    {
        Accepted = 0,
        Started,
        Paused,
        Succeeded,
        Failed
    }
}