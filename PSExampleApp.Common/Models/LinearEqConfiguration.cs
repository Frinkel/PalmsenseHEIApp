using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace PSExampleApp.Common.Models
{
    /// <summary>
    /// This class represents the measurement configuration of a heavy metal measurement
    /// </summary>
    public class LinearEqConfiguration : DataObject
    {
        /// <summary>
        /// Slope of the linear equation
        /// </summary>
        public double? Slope { get; set; } = null;

        /// <summary>
        /// Intercept of the linear equation
        /// </summary>
        public double? Intercept { get; set; } = null;

        /// <summary>
        /// Concentration unit calculated from the linear equation
        /// </summary>
        public string Unit { get; set; }
    }
}