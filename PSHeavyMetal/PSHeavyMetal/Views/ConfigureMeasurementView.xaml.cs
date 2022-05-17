﻿using PSHeavyMetal.Forms.ViewModels;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace PSHeavyMetal.Forms.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ConfigureMeasurementView : ContentPage
    {
        public ConfigureMeasurementView()
        {
            BindingContext = App.GetViewModel<ConfigureMeasurementViewModel>();
            InitializeComponent();
        }
    }
}