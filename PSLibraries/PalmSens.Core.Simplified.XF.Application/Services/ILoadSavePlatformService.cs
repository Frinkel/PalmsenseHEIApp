﻿using PalmSens.Core.Simplified.Data;
using System.IO;
using System.Threading.Tasks;

namespace PalmSens.Core.Simplified.XF.Application.Services
{
    public interface ILoadSavePlatformService
    {
        /// <summary>
        /// Loads a measurement based on a byte array generated by the SaveMeasurementToArray
        /// </summary>
        /// <param name="streamReader"></param>
        /// <returns></returns>
        public SimpleMeasurement LoadMeasurement(Stream stream);

        /// <summary>
        /// Loads a method from a file
        /// </summary>
        /// <param name="streamReader"></param>
        /// <returns></returns>
        public Method LoadMethod(StreamReader streamReader);

        /// <summary>
        /// Serializes a simple measurement toe
        /// </summary>
        /// <param name="simpleMeasurement"></param>
        /// <returns></returns>
        public Task SaveMeasurementToStreamAsync(SimpleMeasurement simpleMeasurement, Stream stream);
    }
}