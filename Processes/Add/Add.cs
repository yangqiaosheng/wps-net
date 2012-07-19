// Process sample in C#

using System;
using System.Collections;
using System.Collections.Generic;
using WPS.NET;

namespace WPSProcess
{
    public class Add
    {
        public ProcessDescription GetDescription()
        {
            ProcessDescription process = new ProcessDescription("Add", "Add", "(a + b) through WPS", "1.1");
            LiteralInput a = new LiteralInput("a", "operand a", "abstract a", "integer", "88");
            a.MinOccurs = 0;
            a.MaxOccurs = 3;
            //a.AllowedValues.Add("88");
            //a.AllowedValues.Add("6");
            process.inputs.Add(a);
            process.inputs.Add(new LiteralInput("b", "operand b", "abstract b", "integer", "22"));
            process.outputs.Add(new LiteralOutput("sum", "sum of a + b", "abstract sum a + b", "integer"));
            ComplexOutput sumFile = new ComplexOutput("sumFile", "sum of a + b as file", "raw abstract sum a + b", new ComplexFormat("text/xml", "utf8", ""));
            sumFile.Formats.Add(new ComplexFormat("plain/text", "utf8", ""));
            process.outputs.Add(sumFile);
            return process;
        }

        public ProcessReturnValue Execute(ProcessInputParams args, ProcessReturnValue ret)
        {            
            int sum = 0;
            int i = 0; 
            while (true)
            {
                LiteralInput a = args.GetData("a", i++).asLiteralInput();
                if (a == null) break;
                sum += Int32.Parse(a.ToString());
            }

            LiteralInput b = args.GetData("b", 0).asLiteralInput();
            int ib = Int32.Parse(b.ToString());
            sum += ib;

            
            if (ret.IsOutputIdentifierRequested("sum"))
            {
                List<OutputData> outputs = ret.GetOutputsForIdentifier("sum");
                // Output 1: a literal containing the raw sum
                LiteralOutput sumOutput = null;
                foreach (OutputData output in outputs)
                {
                    sumOutput = output.asLiteralOutput();
                    sumOutput.Value = sum.ToString();
                    ret.AddData(sumOutput);
                } 
            }

            if (ret.IsOutputIdentifierRequested("sumFile"))
            {
                List<OutputData> outputs = ret.GetOutputsForIdentifier("sumFile");
                // Output 2: a complex data type - plain text by default
                if (ret.IsRawDataOutput())
                {
                    ComplexOutput sumOutput = outputs[0].asComplexOutput();
                    if (Utils.StrICmp(sumOutput.Format.mimeType, "text/xml"))
                    {
                        sumOutput.SetValue("<number>" + sum + "</number>");
                        ret.fileName = "result.xml";
                    }
                    else if (Utils.StrICmp(sumOutput.Format.mimeType, "plain/text"))
                    {
                        sumOutput.SetValue("sum is " + sum);
                        ret.fileName = "result.txt";
                    }
                    ret.AddData(sumOutput);
                }
                else
                {
                    ComplexOutput sumOutput = null;
                    foreach (OutputData output in outputs)
                    {
                        sumOutput = output.asComplexOutput();
                        if (Utils.StrICmp(sumOutput.Format.mimeType, "text/xml"))
                            sumOutput.SetValue("<number>" + sum + "</number>");
                        else
                            sumOutput.SetValue("sum is " + sum);
                        ret.AddData(sumOutput);
                    }
                }
            }
           
            ret.status = ProcessState.Succeeded;
            return ret;
        }
    }
}
