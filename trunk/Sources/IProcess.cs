using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WPS.NET
{
    public interface IProcess
    {
        ProcessDescription GetDescription();
        ProcessReturnValue Execute(ProcessInputParams args, ProcessReturnValue ret);
    }
}