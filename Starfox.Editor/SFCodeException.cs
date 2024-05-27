using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starfox.Editor
{
    [System.Serializable]
    public class SFCodeException : Exception
    {
        public SFCodeException() { }
        public SFCodeException(string message) : base(message) { }
        public SFCodeException(string message, Exception inner) : base(message, inner) { }
        protected SFCodeException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    public class SFCodeOptimizerNotFoundException : SFCodeException
    {
        public SFCodeOptimizerNotFoundException(SFOptimizerTypeSpecifiers OptimizerType) : base(
            $"The {OptimizerType} optimizer was not found in the Code Project.")
        {

        }
    }
}
