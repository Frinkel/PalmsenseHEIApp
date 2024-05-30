using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using PSExampleApp.Core.Services;
using System;
using System.Windows.Input;
using Xamarin.CommunityToolkit.ObjectModel;

namespace PSExampleApp.Forms.ViewModels
{
    public class MeasurementHistoryPlotViewModel : BaseAppViewModel
    {
        private readonly IMeasurementService _measurementService;
        private readonly IUserService _userService;
        private LineSeries _lineSeries;

        public MeasurementHistoryPlotViewModel(IUserService userService, IMeasurementService measurementService, IAppConfigurationService appConfigurationService) : base(appConfigurationService)
        {
            _measurementService = measurementService;
            _userService = userService;
            OnPageAppearingCommand = CommandFactory.Create(OnPageAppearing);
        }


        public ICommand OnPageAppearingCommand { get; set; }

        public PlotModel PlotModel { get; set; } = new PlotModel();

        private async void OnPageAppearing()
        {

            _lineSeries = new LineSeries()
            {
                Title = "Concentration over time",
            };

            foreach (var mInfo in _userService.ActiveUser.Measurements)
            {
                var measurement = await _measurementService.LoadMeasurement(mInfo.Id);
                if (mInfo.MeasurementDate > DateTime.MinValue && mInfo.MeasurementDate < DateTime.MaxValue)
                {
                    _lineSeries.Points.Add(new DataPoint(DateTimeAxis.ToDouble(mInfo.MeasurementDate), measurement.HeiConcentration));
                }
            }

            var xAxis = new DateTimeAxis
            {
                IsAxisVisible = true,
                Position = AxisPosition.Bottom,
                Title = "Time",
                StringFormat = "dd/MM/yyyy",
                IntervalType = DateTimeIntervalType.Days,
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.Dot
            };

            var yAxis = new LinearAxis
            {
                IsAxisVisible = true,
                Position = AxisPosition.Left,
                Title = "Concentration",
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.Dot
            };

            PlotModel.Axes.Clear();
            PlotModel.Series.Clear();
            PlotModel.Axes.Add(xAxis);
            PlotModel.Axes.Add(yAxis);
            PlotModel.Series.Add(_lineSeries);

            PlotModel.InvalidatePlot(true);
        }
    }
}