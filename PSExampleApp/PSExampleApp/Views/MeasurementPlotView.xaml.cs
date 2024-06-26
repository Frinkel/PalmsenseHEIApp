﻿using PSExampleApp.Forms.ViewModels;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace PSExampleApp.Forms.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MeasurementPlotView : ContentPage
    {
        public MeasurementPlotView()
        {
            BindingContext = App.GetViewModel<MeasurementPlotViewModel>();
            InitializeComponent();
        }
    }
}