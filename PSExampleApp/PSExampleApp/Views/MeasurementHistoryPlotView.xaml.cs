using PSExampleApp.Forms.ViewModels;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace PSExampleApp.Forms.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MeasurementHistoryPlotView : ContentPage
    {
        public MeasurementHistoryPlotView()
        {
            BindingContext = App.GetViewModel<MeasurementHistoryPlotViewModel>();
            InitializeComponent();
        }
    }
}