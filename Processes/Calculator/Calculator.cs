using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using WPS.NET;

namespace WPSProcess
{
    public class Calculator
    {
        public ProcessDescription GetDescription()
        {
            ProcessDescription process = new ProcessDescription("Calculator", "Calculator", "Basic operations through WPS", "1.1");
            LiteralInput oper = new LiteralInput("operator", "operator", "abstract operator", "string", "add");
            oper.AllowedValues.AddRange(new string[] { "add", "sub", "mult", "div"});
            process.inputs.Add(oper);

            process.inputs.Add(new ComplexInput("a", "operand a", "abstract a", new ComplexFormat("text/xml", "uf8", "myschema.xsd")));
            process.inputs.Add(new ComplexInput("b", "operand b", "abstract b", new ComplexFormat("text/xml", "uf8", "myschema.xsd")));
            process.outputs.Add(new ComplexOutput("result", "result of operation as file", "raw abstract result of operation as file", new ComplexFormat("text/xml", "utf8", "myschema.xsd")));
            return process;
        }

        public ProcessReturnValue Execute(ProcessInputParams args, ProcessReturnValue ret)
        {
            float fresult = 0;
            float fa = 0, fb = 0;
            ComplexInput a = args.GetData("a", 0).asComplexInput();
            ComplexInput b = args.GetData("b", 0).asComplexInput();
            XmlDocument doc = new XmlDocument();
            try
            {
                doc.LoadXml(a.ToString());
                fa = float.Parse(doc.InnerText);
            }
            catch { }

            try
            {
                doc.LoadXml(b.ToString());
                fb = float.Parse(doc.InnerText);
            }
            catch { }

            LiteralInput oper = args.GetData("operator", 0).asLiteralInput();
            string myOperator = oper.ToString();

            if (ret.IsOutputIdentifierRequested("result"))
            {
                switch (myOperator)
                {
                    case "sub":
                        fresult = fa - fb;
                        break;
                    case "mult":
                        fresult = fa * fb;
                        break;
                    case "div":
                        if (fb != 0)
                            fresult = fa / fb;
                        else
                            fresult = fa;
                        break;
                    case "add":
                    default:
                        fresult = fa + fb;
                        break;
                }

                ComplexOutput result = null;
                List<OutputData> outputs = ret.GetOutputsForIdentifier("result");

                if (ret.IsRawDataOutput())
                {
                    ret.fileName = "result.xml";
                    result = outputs[0].asComplexOutput();
                    result.SetValue(String.Format("<?xml version='1.0' encoding='{0}'?>\n<number>{1}</number>",
                        result.Format.encoding, fresult));
                    ret.AddData(result);
                }
                else
                {
                    foreach (OutputData output in outputs)
                    {
                        result = output.asComplexOutput();
                        result.SetValue("<number>" + fresult + "</number>");
                        ret.AddData(result);
                    }
                }
            }

            return ret;
        }
    }
}
