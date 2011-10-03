using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WPS.NET;
using System.Threading;

namespace WPSProcess
{
    /// <summary>
    /// This process counts a user-defined (as a parameter) number of seconds asynchronously. 
    /// This is an example of async process for developpers.
    /// </summary>
    
    [Serializable()]
    public class AsyncClock : IAsyncProcess
    {
        
        public AsyncClock()
            : base (null, null, null)
        {
        }

        /// <summary>
        /// This constructor calls the base constructor. It is used to store every useful data the 
        /// process needs in order to be run asynchronously
        /// </summary>
        /// <param name="mainDomain">The main AppDomain, which is the System.AppDomain of the main application 
        /// (the WPS.NET server instance so to speak). This will be useful since this process will 
        /// be run in a dedicated AppDomain and can't otherwise interact with the main server instance.</param>
        /// <param name="storedResultPath">The static path where the process' result will be stored on the server</param>
        /// <param name="baseUrlToResultPath">The base url to access this path, can be useful 
        /// to provide a direct url to the response to the client.</param>
        public AsyncClock(AppDomain mainDomain, string storedResultPath, string baseUrlToResultPath)
            : base(mainDomain, storedResultPath, baseUrlToResultPath)
        {
        }

        /// <summary>
        /// This method is called by the DescribeProcess part of WPS.NET, it must be implemented.
        /// </summary>
        /// <returns>The process description</returns>
        public override ProcessDescription GetDescription()
        {
            ///This is where we create the process description
            ProcessDescription process = new ProcessDescription("AsyncClock", "Async Clock", "Counts a user-defined number of seconds asynchronously (This is an example of async process for developpers)", "1.1");

            ///This check is meant to make sure everything is in it's place so that the process 
            ///can be called asynchronously without failing miserably.
            if (this is IAsyncProcess && this.MainAppDomain != null)
            {
                ///Is this process implements IAsyncProcess and if the Main Application Domain is provided, 
                ///we can assume that status will be supported (provided status updates are properly coded).
                process.statusSupported = true;

                if (this.BaseUrlToResultPath != String.Empty && this.StoredResultPath != string.Empty)
                    ///If the necessary informations for responses and results storage are provided
                    ///we can enable storage support.
                    process.storeSupported = true;
            }

            ///This is the declaration of the numberOfSeconds parameter
            ///It has 1 minoccurs and 1 maxoccurs, it is mandatory.
            LiteralInput numberOfSeconds = new LiteralInput("numberOfSeconds", "Number of seconds", "The number of seconds this process will count", "integer", "100");
            numberOfSeconds.MinOccurs = 1;
            numberOfSeconds.MaxOccurs = 1;

            ///Dont forget to add the previously created parameter in the inputs collection of the process.
            process.inputs.Add(numberOfSeconds);

            ///A start date should be also specified
            process.processStartDate = this.startDate;

            ///Specify the output of the process
            LiteralOutput output = new LiteralOutput("AsyncClockResult", "Async Clock Result", "A string containing the start datetime and the end datetime", "string");
            output.Value = String.Empty;
            process.outputs.Add(output);

            ///Return the description of the process.
            return process;
        }

        /// <summary>
        /// This is the entry point of the worker part of the process. It returns a first ExecuteResponse which
        /// will point to the stored Execute Response through it's statusLocation.
        /// </summary>
        /// <param name="args">Arguments</param>
        /// <param name="ret">Return value</param>
        /// <returns>The returnValue of the process.</returns>
        public override ProcessReturnValue Execute(ProcessInputParams args, ProcessReturnValue ret)
        {
            ///A simple check to make sure ret isn't null
            if (ret == null) ret = new ProcessReturnValue();

            ///At this time, the process is accepted and will be executed.
            ret.status = ProcessState.Accepted;

            ///Provide advancement, obviously nothing has happened yet so it's 0%
            ret.percentageCompleted = 0;

            ///Make sure ret will be available to the async method, the easiest way is to copy it into
            ///the dedicated property of the class. If the thread is not parameterized, a Args property is also
            ///available.
            this.ExecuteResponseValue = ret;

            ///This check is performed to make sure this process is called asynchronously. Async processes 
            ///are not compatible with synchronous calls at this time
            if (!ret.responseForm.responseDocument.storeExecuteResponse || !ret.responseForm.responseDocument.storeExecuteResponse)
                throw new Exception("This process must be executed asynchronously, please make sure that 'status' and 'storeExecuteResponse' are set to 'true'");


            ///Initiate and start the worker thread.
            Thread workerThread = new Thread(new ParameterizedThreadStart(this.Work));
            workerThread.IsBackground = false;
            workerThread.Start(args);

            ///Specify an empty output.
            this.ExecuteResponseValue.returnValues.Clear();
            this.ExecuteResponseValue.returnValues.Add(new LiteralOutput("AsyncClockResult", "Async Clock Result", "A string containing the start datetime and the end datetime", "string"));

            ///Return the first response which will contain the statusLocation to the stored response
            ///(XML file stored on the server which will be updated with new values each time 
            ///the ProgressChanged event is raised by this process.
            return ExecuteResponseValue;
        }

        /// <summary>
        /// This is the async working part of the process.
        /// </summary>
        /// <param name="args">The arguments passed to the parameterized thread, the ProcessInputParams</param>
        public void Work(object args)
        {
            ///Retrieve the value of the "numberOfSeconds" parameter.
            int numberOfSeconds = Int32.Parse((args as ProcessInputParams)
                                                            .GetData("numberOfSeconds", 0)
                                                            .asLiteralInput().Value);
            int percentage = 0;

            ///This ProcessData contains the response data and process progress
            ///It it passed to the Server instance via the ProgressChanged event.
            ProcessData procData = new ProcessData(this.ExecuteResponseValue,
                                                    this.ProcessDescription,
                                                    this.MainAppDomain,
                                                    AppDomain.CurrentDomain,
                                                    percentage);

            ///It is the container of the ProcessData.
            ProcessProgressChangedEventArgs eventArgs;

            ///Get the start date.
            DateTime start = DateTime.Now;

            ///Set the status of the process at Started, update the status message accordingly.
            this.ExecuteResponseValue.status = ProcessState.Started;
            this.ExecuteResponseValue.statusMessage = "In progress...";

            ///Wait n seconds where n is the number of seconds specified by the user.
            for (int s = 1; s <= numberOfSeconds; s++)
            {
                Thread.Sleep(1000);
                percentage = (int)Math.Floor(((double)s / (double)numberOfSeconds) * 100d);

                ///At each second, update the progress of the process
                this.ExecuteResponseValue.percentageCompleted = percentage;

                ///Update the data from ProcessData which will be passed to the eventhandler
                procData.Progress = percentage;
                procData.ExecuteResponseValue = this.ExecuteResponseValue;

                ///Raise the ProgressChanged event with the eventArgs data.
                eventArgs = new ProcessProgressChangedEventArgs(procData);
                this.OnProcessProgressChanged(eventArgs);

            }
            
            ///Get the end date
            DateTime end = DateTime.Now;

            ///Update one last time the process infos with the final values
            ///Also update the status and status message.
            this.ExecuteResponseValue.status = ProcessState.Succeeded;
            this.ExecuteResponseValue.statusMessage = "Work done.";

            ///Set the return value.
            this.ExecuteResponseValue.returnValues.Clear();
            this.ExecuteResponseValue.returnValues.Add(new LiteralOutput("AsyncClockResult", "Async Clock Result", "A string containing the start datetime and the end datetime", "string"));
            (this.ExecuteResponseValue.returnValues[0] as LiteralOutput).Value = start.ToLongTimeString() + " " + end.ToLongTimeString();

            procData.ExecuteResponseValue = this.ExecuteResponseValue;


            ///Raise the event. Process status is set to Succeeded, it will then be disposed by the server.
            eventArgs = new ProcessProgressChangedEventArgs(procData);
            this.OnProcessProgressChanged(eventArgs);
        }
    }
}
