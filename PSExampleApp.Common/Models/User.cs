using System.Collections.Generic;

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
            Unit = "mIU/L"
        };
        public List<MeasurementInfo> Measurements { get; set; } = new List<MeasurementInfo>();
        public string Password { get; set; }
    }
}