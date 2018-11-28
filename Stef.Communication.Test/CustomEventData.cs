using System;

namespace Stef.Communication.Test
{
    public class CustomEventData
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public byte[] Data { get; set; }

        public override string ToString()
        {
            return string.Concat(FirstName, " ", LastName);
        }
    }
}
