/// WPS.NET
/// A .NET implementation of OGC Web Processing Service v1.0.0
/// (c) brgm 2010-2011

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
