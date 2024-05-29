using PalmSens.Core.Simplified.Data;
using PalmSens.Data;
using PalmSens.DataFiles;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Forms;
using PalmSens.Core.Simplified.XF.Application.Services;


namespace PalmSens.Core.Simplified.XF.Application.Services
{
    public class SimpleLoadSaveFunctions : ILoadSavePlatformService
    {
        /// <summary>
        /// Loads a collection of simplemeasurements from a *.pssession file from your assets folder.
        /// </summary>
        /// <param name="streamReader">The stream reader referencing the *.pssession file.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentException">Stream reader cannot be null</exception>
        /// <exception cref="System.Exception">An error occured while loading, please make sure the file in the stream reader is valid</exception>
        public SimpleMeasurement LoadMeasurement(Stream stream)
        {
            SessionManager session = new SessionManager();

            try
            {
                session.Load(stream, "");
            }
            catch (Exception ex)
            {
                throw new Exception($"An error occured while loading, please make sure the file in the stream reader is valid \n {ex}");
            }

            var measurement = session.Measurements.FirstOrDefault();

            if (measurement == null)
                throw new InvalidOperationException("The session doesn't contain a measurement");

            return new SimpleMeasurement(measurement);
        }

        /// <summary>
        /// Loads a method from a *.psmethod file from your assets folder.
        /// </summary>
        /// <param name="streamReader">The stream reader referencing the *.psmethod file.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentException">Stream reader cannot be null</exception>
        /// <exception cref="System.Exception">An error occured while loading, please make sure the file path is correct and the file is valid</exception>
        public Method LoadMethod(StreamReader streamReader)
        {
            if (streamReader == null)
                throw new ArgumentException("Stream reader cannot be null");

            try
            {
                return MethodFile2.FromStream(streamReader);
            }
            catch (Exception ex)
            {
                throw new Exception($"An error occured while loading, please make sure the file path is correct and the file is valid \n {ex}");
            }
        }

        /// <summary>
        /// Saves a simplemeasurement to a *.pssession file.
        /// </summary>
        /// <param name="simpleMeasurement">The simple measurement.</param>
        /// <param name="filepath">The filepath of the *.pssession file.</param>
        /// <exception cref="System.ArgumentException">File path must be specified</exception>
        /// <exception cref="System.ArgumentNullException">SimpleMeasurement cannot be null</exception>
        /// <exception cref="System.Exception">An error occured while saving, please make sure the file path is correct</exception>
        public async Task SaveMeasurementToStreamAsync(SimpleMeasurement simpleMeasurement, Stream stream)
        {
            if (simpleMeasurement == null)
                throw new ArgumentNullException("SimpleMeasurement cannot be null");

            SessionManager session = new SessionManager();
            session.AddMeasurement(simpleMeasurement.Measurement);
            session.MethodForEditor = simpleMeasurement.Measurement.Method;

            try
            {
                var progress = new Progress<bool>();
                await session.SaveAsync(stream, "", System.Threading.CancellationToken.None, progress);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                throw;
            }
        }

        /// <summary>
        /// Loads a SimpleMeasurement from a .pssession file located in the Android assets folder.
        /// </summary>
        /// <param name="assetName">The filename of the .pssession file in the assets folder.</param>
        /// <returns>A task that represents the asynchronous operation, containing the loaded SimpleMeasurement.</returns>
        /// <exception cref="FileNotFoundException">Thrown if the asset is not found.</exception>
        /// <exception cref="Exception">A general exception for other errors.</exception>
        public async Task<SimpleMeasurement> LoadMeasurementFromAssetAsync(string assetName)
        {
            var assetService = DependencyService.Get<ILoadAssetsService>();
            if (assetService == null)
            {
                throw new InvalidOperationException("Asset service not available");
            }

            try
            {
                using (var streamReader = assetService.LoadFile(assetName))
                {
                    if (streamReader == null || !streamReader.BaseStream.CanRead)
                    {
                        throw new FileNotFoundException("Unable to open file stream for reading.", assetName);
                    }

                    var measurement = LoadMeasurement(streamReader.BaseStream);
                    return measurement;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to load the measurement from asset '{assetName}': {ex.Message}", ex);
            }
        }

    }
}
