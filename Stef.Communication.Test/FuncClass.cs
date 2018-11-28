using System;

namespace Stef.Communication.Test
{
    public class FuncClass
    {
        public int HowMany(int x)
        {
            return x * x;
        }
        public string TellMe(CustomEventData data)
        {
            Console.WriteLine(string.Concat("Server: ", data.Data?.Length, " bytes"));
            return data.FirstName;
        }
        public CustomEventData TellMe2(CustomEventData data)
        {
            data.FirstName = "Max";
            data.LastName = "Mustermann";

            return data;
        }
    }
}
