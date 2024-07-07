using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Org.BouncyCastle.Utilities.Date;

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
        public double Slope { get; set; }

        /// <summary>
        /// Intercept of the linear equation
        /// </summary>
        public double Intercept { get; set; }

        /// <summary>
        /// The ID of the Batch
        /// </summary>
        public int BatchNumber { get; set; }

        /// <summary>
        /// The expiration date of the sensor
        /// </summary>
        public DateTime SensorExpirationDate { get; set; }

        ///// <summary>
        ///// Concentration unit calculated from the linear equation
        ///// </summary>
        //public string Unit { get; set; }
    }
}