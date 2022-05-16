﻿using Microsoft.Extensions.DependencyInjection;
using PSHeavyMetal.Forms.Navigation;
using PSHeavyMetal.Forms.ViewModels;
using PSHeavyMetal.Forms.Views;
using System;
using Xamarin.Forms;

namespace PSHeavyMetal.Forms
{
    public partial class App : Application
    {
        private static IServiceProvider ServiceProvider;

        public App()
        {
            InitializeComponent();
            ServiceProvider = Startup.Init();

            var navigationPage = new NavigationPage(new LoginView())
            {
                BarBackgroundColor = Color.Transparent,
                BackgroundColor = Color.Transparent,
            };

            MainPage = new CustomFlyOutPage()
            {
                Flyout = new MainMenuView(),
                Detail = navigationPage,
                BackgroundImageSource = "background.jpeg",
            };

            NavigationDispatcher.Instance.Initialize(navigationPage.Navigation);
        }

        public static BaseViewModel GetViewModel<T>() where T : BaseViewModel => ServiceProvider.GetService<T>();

        protected override void OnStart()
        {
        }

        protected override void OnSleep()
        {
        }

        protected override void OnResume()
        {
        }
    }
}