using Android.Content;
using Android.Graphics; // Import this for Color
using AndroidX.AppCompat.Widget;
using PSExampleApp.Forms.Navigation;
using PSHeavyMetal.Droid.CustomRenderers;
using AView = Android.Views.View;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;
using Xamarin.Forms.Platform.Android.AppCompat;
using Xamarin.Forms.PlatformConfiguration;

[assembly: ExportRenderer(typeof(CustomNavigationPage), typeof(CustomNavigationPageRenderer))]
namespace PSHeavyMetal.Droid.CustomRenderers
{
    public class CustomNavigationPageRenderer : NavigationPageRenderer
    {
        public CustomNavigationPageRenderer(Context context) : base(context)
        {
        }

        IPageController PageController => Element as IPageController;
        CustomNavigationPage CustomNavigationPage => Element as CustomNavigationPage;

        protected override void OnLayout(bool changed, int l, int t, int r, int b)
        {
            CustomNavigationPage.IgnoreLayoutChange = true;
            base.OnLayout(changed, l, t, r, b);
            CustomNavigationPage.IgnoreLayoutChange = false;

            // Retrieve the Toolbar
            var toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);
            if (toolbar != null)
            {
                // Check if the NavigationIcon (back arrow) is available
                if (toolbar.NavigationIcon != null)
                {
                    // Set the color of the back arrow to black
                    toolbar.NavigationIcon.SetColorFilter(Android.Graphics.Color.Black, PorterDuff.Mode.SrcIn);
                }
            }

            int containerHeight = b - t;
            PageController.ContainerArea = new Rectangle(0, 0, Context.FromPixels(r - l), Context.FromPixels(containerHeight));

            for (var i = 0; i < ChildCount; i++)
            {
                AView child = GetChildAt(i);

                if (child is Toolbar)
                {
                    continue;
                }

                child.Layout(0, 0, r, b);
            }
        }
    }
}
