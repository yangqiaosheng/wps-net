// Process sample in C++/CLI

using namespace System;
using namespace System::Collections::Generic;
using namespace WPS::NET;

namespace WPSProcess {

	public ref class Sub
	{
	public:
		ProcessDescription^ GetDescription()
        {
            ProcessDescription^ process = gcnew ProcessDescription("Sub", "Sub", "(a - b) through WPS", "1.1" );
            process->inputs->Add(gcnew LiteralInput("a", "operand a", "abstract a", "integer", "10"));
            process->inputs->Add(gcnew LiteralInput("b", "operand b", "abstract b", "integer", "6"));
            process->inputs->Add(gcnew BoundingBoxInput("bbox", "a test bbox", "abstract bbox", "EPSG:4326"));
            process->outputs->Add(gcnew LiteralOutput("diff", "diff of a - b", "abstract a - b", "integer"));
            return process;
        }

		ProcessReturnValue^ Execute(ProcessInputParams ^args, ProcessReturnValue ^ret)
		{
			LiteralInput^ a = args->GetData("a", 0)->asLiteralInput();
			LiteralInput^ b = args->GetData("b", 0)->asLiteralInput();
			BoundingBoxInput^ bbox = args->GetData("bbox", 0)->asBoundingBoxInput();

			int r = Int32::Parse(a->ToString()) - Int32::Parse(b->ToString());

            if (ret->IsOutputIdentifierRequested("diff"))
            {
                List<OutputData^> ^outputs = ret->GetOutputsForIdentifier("diff");
                for each (OutputData ^ output in outputs)
                {
                    LiteralOutput ^ diff = output->asLiteralOutput();
                    diff->Value = r.ToString();
                    ret->AddData(diff);
                }
            }
			return ret;
		}
	};
}
