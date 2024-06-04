using Newtonsoft.Json;
using PalmSens;
using PalmSens.Analysis;
using PalmSens.Core.Simplified.Data;
using PalmSens.Core.Simplified.XF.Application.Services;
using PalmSens.Devices;
using PSExampleApp.Common.Models;
using PSExampleApp.Core.Repositories;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static PalmSens.Core.Simplified.PSCommSimple;

namespace PSExampleApp.Core.Services
{
    public class MeasurementService : IMeasurementService
    {
        private const string defaultPbConfiguration = "pbmethod";

        private readonly InstrumentService _instrumentService;
        private readonly ILoadAssetsService _loadAssetsService;
        private readonly ILoadSavePlatformService _loadSavePlatformService;
        private readonly IMeasurementRepository _measurementRepository;
        private readonly IUserService _userService;
        private HeavyMetalMeasurement _activeMeasurement;

        public MeasurementService(
            IMeasurementRepository repository,
            InstrumentService instrumentService,
            ILoadSavePlatformService loadSavePlatformService,
            ILoadAssetsService loadAssetsService,
            IUserService userService)
        {
            _measurementRepository = repository;
            _instrumentService = instrumentService;
            _loadSavePlatformService = loadSavePlatformService;
            _loadAssetsService = loadAssetsService;
            _userService = userService;
        }

        public event SimpleCurveStartReceivingDataHandler DataReceived
        {
            add => _instrumentService.SimpleCurveStartReceivingData += value;
            remove => _instrumentService.SimpleCurveStartReceivingData -= value;
        }

        public event EventHandler<HeavyMetalMeasurement> MeasurementChanged;

        public event EventHandler MeasurementEnded
        {
            add => _instrumentService.MeasurementEnded += value;
            remove => _instrumentService.MeasurementEnded -= value;
        }

        public event EventHandler MeasurementStarted
        {
            add => _instrumentService.MeasurementStarted += value;
            remove => _instrumentService.MeasurementStarted -= value;
        }

        public HeavyMetalMeasurement ActiveMeasurement
        {
            get => _activeMeasurement;
            private set
            {
                _activeMeasurement = value;
                MeasurementChanged?.Invoke(this, value);
            }
        }

        public DeviceCapabilities Capabilities
        {
            get => _instrumentService.Capabilities;
        }

        public void CalculateConcentration()
        {
            var peakList = new List<Peak>();

            //We assume that the result of the measurement will be in 1 curve
            if (ActiveMeasurement.Measurement.SimpleCurveCollection.Count > 1)
                throw new InvalidOperationException("The measurement has multiple curves");

            var activeCurve = ActiveMeasurement.Measurement.SimpleCurveCollection[0];

            for (int i = 0; i < activeCurve.Peaks.nPeaks; i++)
            {
                //We first put the peaks in a list. Easier to filter this way
                peakList.Add(activeCurve.Peaks[i]);
            }

            //Here we filter based on the expected peak and the peakwidth. If the PeakX value falls within the width then the peak fill be used.
            //If we find multiple peaks then we select the one one with the highest peak value
            var peakWidthLeft = ActiveMeasurement.Configuration.ConcentrationMethod.PeakWindowXMin;
            var peakWidthRight = ActiveMeasurement.Configuration.ConcentrationMethod.PeakWindowXMax;
            var filteredPeak = peakList.Where(x => x.PeakX >= peakWidthLeft && x.PeakX <= peakWidthRight).OrderByDescending(y => y.PeakValue).FirstOrDefault();

            //If we can't find any filtered peaks then we leave the concentration at 0
            if (filteredPeak == null)
                return;

            ActiveMeasurement.Concentration = Math.Truncate(ActiveMeasurement.Configuration.ConcentrationMethod.CalibrationCurveSlope * filteredPeak.PeakValue + ActiveMeasurement.Configuration.ConcentrationMethod.CalibrationCurveOffset);
        }

        public HeavyMetalMeasurement CreateMeasurement(MeasurementConfiguration configuration)
        {
            var measurement = new HeavyMetalMeasurement
            {
                Id = Guid.NewGuid(),
                Configuration = configuration,
            };

            this.ActiveMeasurement = measurement;
            return measurement;
        }

        public async Task DeleteMeasurement(Guid id)
        {
            await _measurementRepository.DeleteMeasurement(id);
            await _userService.DeleteMeasurementInfo(id);
        }

        public async Task DeleteMeasurementConfiguration(Guid id)
        {
            await _measurementRepository.DeleteMeasurementConfiguration(id);
        }

        public async Task InitializeMeasurementConfigurations()
        {
            var existingConfigurations = await _measurementRepository.LoadAllConfigurations();

            if (existingConfigurations != null && !existingConfigurations.Any())
            {
                var configuration = await LoadMeasurementConfigurationFromFile(defaultPbConfiguration);
                configuration.IsDefault = true;

                await _measurementRepository.SaveMeasurementConfiguration(configuration);
            }
        }

        public async Task<IEnumerable<MeasurementConfiguration>> LoadAllMeasurementConfigurationsAsync()
        {
            return await _measurementRepository.LoadAllConfigurations();
        }

        public async Task<HeavyMetalMeasurement> LoadMeasurement(Guid id)
        {
            var savedMeasurement = await _measurementRepository.LoadMeasurement(id);

            var heavyMetalMeasurement = new HeavyMetalMeasurement
            {
                Concentration = savedMeasurement.Concentration,
                Configuration = savedMeasurement.Configuration,
                MeasurementImages = savedMeasurement.SaveImages,
                Id = id,
                Name = savedMeasurement.Name,
                HeiConcentration = savedMeasurement.HeiConcentration,
            };

            using (var stream = new MemoryStream(savedMeasurement.SerializedMeasurement))
            {
                heavyMetalMeasurement.Measurement = _loadSavePlatformService.LoadMeasurement(stream);
            }

            ActiveMeasurement = heavyMetalMeasurement;

            return heavyMetalMeasurement;
        }

        public async Task<MeasurementConfiguration> LoadMeasurementConfigurationFromFile(string filename)
        {
            using (var filestream = _loadAssetsService.LoadFile($"{filename}.json"))
            {
                var jsonString = await filestream.ReadToEndAsync();
                return JsonConvert.DeserializeObject<MeasurementConfiguration>(jsonString);
            }
        }

        public void ResetMeasurement()
        {
            ActiveMeasurement = null;
        }

        public async Task SaveMeasurement(HeavyMetalMeasurement measurement)
        {
            byte[] array;

            using (var memoryStream = new MemoryStream())
            {
                await _loadSavePlatformService.SaveMeasurementToStreamAsync(measurement.Measurement, memoryStream);
                array = memoryStream.ToArray();
            }

            var savedMeasurement = new SavedMeasurement
            {
                Concentration = measurement.Concentration,
                SerializedMeasurement = array,
                SaveImages = measurement.MeasurementImages,
                Configuration = measurement.Configuration,
                Id = measurement.Id,
                Name = measurement.Name,
                HeiConcentration = measurement.HeiConcentration,
            };

            try
            {
                await _measurementRepository.SaveMeasurement(savedMeasurement);
                await _userService.SaveMeasurementInfo(measurement);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Saving of the measurment failed {ex}");
                throw;
            }
        }

        public async Task SaveMeasurementConfiguration(MeasurementConfiguration configuration)
        {
            await _measurementRepository.SaveMeasurementConfiguration(configuration);
        }

        public async Task<SimpleMeasurement> StartMeasurement(Method method)
        {
            _instrumentService.InitializeInstrument(method);
            return await _instrumentService.MeasureAsync(method);
        }

        /// <summary>
        /// Loads the current active measurement, if there is none set, we set it by loading the latest measurement.
        /// </summary>
        /// <returns></returns>
        public async Task<HeavyMetalMeasurement> GetActiveMeasurement()
        {
            // Custom check to figure gather the newest active measurement if there's none in the measurement service
            if (ActiveMeasurement == null)
            {
                var measurementdId = _userService.ActiveUser.Measurements.OrderByDescending(m => m.MeasurementDate).FirstOrDefault().Id;
                ActiveMeasurement = await LoadMeasurement(measurementdId);
                _activeMeasurement = ActiveMeasurement;
            }
            
            return _activeMeasurement;
        }

        public double HeiCalculateConcentration(double targetFrequency)
        {
            double impedanceValue = GetValuesFromTargetFreq(targetFrequency);
            double concentration = CalculateConcentration(impedanceValue);
            ActiveMeasurement.SetHeiConcentration = concentration;
            return ActiveMeasurement.HeiConcentration;
        }

        private double GetValuesFromTargetFreq(double targetFrequency)
        {
            var baseArray = ActiveMeasurement.Measurement.Measurement.EISdata.FirstOrDefault().EISDataSet.GetDataArrays();
            var frequencyArray = baseArray.FirstOrDefault(a => a.ArrayType == 5);
            var impedanceArray = baseArray.FirstOrDefault(a => a.ArrayType == 10);
            var phaseArray = baseArray.FirstOrDefault(a => a.ArrayType == 6);

            if (frequencyArray == null || impedanceArray == null || phaseArray == null)
            {
                Debug.WriteLine("One or more required data arrays are missing.");
                throw new Exception("One or more required data arrays are missing.");
            }

            double[] frequencyValues = frequencyArray.GetValues();

            int index = Array.FindIndex(frequencyValues, freq => Math.Floor(freq) == targetFrequency);

            if (index != -1)
            {
                Debug.WriteLine($"Index of target frequency ({targetFrequency} Hz): {index}");

                // Retrieve corresponding impedance and phase values using the index
                double[] impedanceData = impedanceArray.GetValues();
                double[] phaseData = phaseArray.GetValues();

                if (index < impedanceData.Length && index < phaseData.Length)
                {
                    double impedanceValue = impedanceData[index];
                    double phaseValue = phaseData[index];
                    Debug.WriteLine($"At {targetFrequency} Hz, Impedance (|Z|): {impedanceValue}, Phase: {phaseValue} degrees");
                    return impedanceValue;
                }
            }
            else
            {
                Debug.WriteLine($"Frequency {targetFrequency} Hz not found.");
                throw new Exception($"Frequency {targetFrequency} Hz not found.");
            }

            throw new Exception("Target frequency calculation error.");
        }

        private double CalculateConcentration(double impedanceValue)
        {
            // Formula: x = (y - 0.01696) / 0.02704
            double intercept = _userService.ActiveUser?.UserLinearEquationConfiguration?.Intercept ?? 0.01696; // DEFAULT
            double slope = _userService.ActiveUser?.UserLinearEquationConfiguration?.Slope ?? 0.02704; // DEFAULT
            return (impedanceValue - intercept) / slope;
        }

        public void SetActiveMeasurement(HeavyMetalMeasurement _activeMeasurement)
        {
            ActiveMeasurement = _activeMeasurement;
        }
    }
}