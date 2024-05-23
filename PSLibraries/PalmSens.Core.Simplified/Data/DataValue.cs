using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using PalmSens.Data;
using PalmSens.DataFiles;

namespace PalmSens.Core.Simplified.Data
{
    public class DataValue : IDataValue
    {
        public double Value { get; set; }
        public string Text { get; set; }

        public DataValue(double value)
        {
            Value = value;
            Text = value.ToString();
        }

        public IDataValue Copy()
        {
            return new DataValue(Value);
        }

        public string GetFormattedValue()
        {
            return Text;
        }

        public JsonBag ToJsonBag()
        {
            // Implement JSON serialization if necessary
            return new JsonBag();
        }

        public async Task ToJsonWriter(JsonWriter jw, CancellationToken cancellationToken)
        {
            // Implement JSON writing if necessary
            await Task.CompletedTask;
        }
    }
}