/// WPS.NET
/// A .NET implementation of OGC Web Processing Service v1.0.0
/// (c) brgm 2010

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace WPS.NET
{
    [Serializable()]
    public class ProcessInputParams : ISerializable
    {
        public Dictionary<string, InputData[]> parameters;

        public ProcessInputParams()
        {
            this.parameters = new Dictionary<string, InputData[]>();
        }

        public InputData GetData(string id, int index)
        {
            InputData[] arr = parameters[id];
            return arr != null && index < arr.Length ? arr[index] : new NullData();
        }

        //public InputReferenceType GetReference(string id, int index)
        //{
        //    return (InputReferenceType)((InputData)(parameters[id])[index]).Reference;
        //}

        public ProcessInputParams(SerializationInfo info, StreamingContext ctxt)
        {
            parameters = (Dictionary<string, InputData[]>)info.GetValue("parameters", typeof(Dictionary<string, InputData[]>));
        }

        public void GetObjectData(SerializationInfo info, StreamingContext ctxt)
        {
            info.AddValue("parameters", parameters);
        }
    }
}
