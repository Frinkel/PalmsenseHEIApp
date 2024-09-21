using System;
using System.Collections.Generic;
using Org.BouncyCastle.Utilities.Date;

namespace PSExampleApp.Common.Models
{
    public class User : DataObject
    {
        public bool IsAdmin { get; set; }
        public bool UseMockData { get; set; }
        public LinearEqConfiguration UserLinearEquationConfiguration { get; set; } = new LinearEqConfiguration
        {
            Intercept = 0.01696,
            Slope = 0.02704,
            //BatchNumber = -1,
            //SensorExpirationDate = DateTime.MinValue
            //Unit = "mIU/L"
        };
        public List<MeasurementInfo> Measurements { get; set; } = new List<MeasurementInfo>();
        public string Password { get; set; }
        public double TargetFrequency { get; set; } = 126;
    }
}