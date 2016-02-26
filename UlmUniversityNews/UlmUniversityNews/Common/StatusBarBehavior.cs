using Microsoft.Xaml.Interactivity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.ViewManagement;
using Windows.UI;
using System.Diagnostics;

namespace UlmUniversityNews.Common
{
    /// <summary>
    /// Das StatusBarBehavior ist ein View Behavior, welches es erlaubt die StatusBar aus dem XAML Code Files
    /// mit Parametern zu initialisieren.
    /// Die Implementierung ist angelehnt an: https://marcominerva.wordpress.com/2014/09/11/behaviors-to-handle-statusbar-and-progressindicator-in-windows-phone-8-1-apps/
    /// </summary>
    public class StatusBarBehavior : Behavior
    {
        #region Fields
        private const string IsVisiblePropertyName = "IsVisible";
        private const string ForegroundColorPropertyName = "ForegroundColor";
        private const string BackgroundColorPropertyName = "BackgroundColor";
        private const string BackgroundOpacityPropertyName = "BackgroundOpacity";
        #endregion Fields

        #region DependencyProperties
        public static readonly DependencyProperty IsVisibleProperty =
            DependencyProperty.Register(
            IsVisiblePropertyName,
            typeof(bool),
            typeof(StatusBarBehavior),
            new PropertyMetadata(true, OnIsVisibleChanged)
            );

        public static readonly DependencyProperty ForegroundColorProperty =
            DependencyProperty.Register(
            ForegroundColorPropertyName,
            typeof(Color),
            typeof(StatusBarBehavior),
            new PropertyMetadata(null, OnForegroundColorChanged)
            );

        public static readonly DependencyProperty BackgroundColorProperty =
            DependencyProperty.Register(
            BackgroundColorPropertyName,
            typeof(Color),
            typeof(StatusBarBehavior),
            new PropertyMetadata(null, OnBackgroundColorChanged)
            );

        public static readonly DependencyProperty BackgroundOpacityProperty =
            DependencyProperty.Register(
            BackgroundOpacityPropertyName,
            typeof(double),
            typeof(StatusBarBehavior),
            new PropertyMetadata(null, OnBackgroundOpacityChanged)
            );
        #endregion DependencyProperties

        #region PropertyChangedHandler
        /// <summary>
        /// Eventhandler, der aufgerufen wird, wenn sich der Wert des IsVisibleProperty der StatusBar geändert hat.
        /// Zeigt je nach Wert des Property die StatusBar an oder nicht.
        /// </summary>
        /// <param name="d">Das zugehörige DependencyObject.</param>
        /// <param name="e">Der Eventparameter, die den neuen Wert für die Sichtbarkeit enthält.</param>
        private static async void OnIsVisibleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            StatusBar statusBar = StatusBar.GetForCurrentView();
            if ((bool) e.NewValue)
            {
                await statusBar.ShowAsync();
            }
            else
            {
                await statusBar.HideAsync();
            }
        }

        /// <summary>
        /// Eventhandler, der aufgerufen wird, wenn sich der Wert des ForegroundColorProperty geändert hat.
        /// Aktualisiert die Schrift- und Iconfarben der StatusBar.
        /// </summary>
        /// <param name="d">Das zugehörige DependencyObject.</param>
        /// <param name="e">Der Eventparameter, die den neuen Wert für die Farbe enthält.</param>
        private static void OnForegroundColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            StatusBar.GetForCurrentView().ForegroundColor = (Color) e.NewValue;
        }

        /// <summary>
        /// Eventhandler, der aufgerufen wird, wenn sich der Wert des BackgroundColorProperty geändert hat.
        /// Aktualisiert die Hintergrundfarbe der StatusBar.
        /// </summary>
        /// <param name="d">Das zugehörige DependencyObject.</param>
        /// <param name="e">Der Eventparameter, die den neuen Wert für die Farbe enthält.</param>
        private static void OnBackgroundColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // Debug.WriteLine("In OnBackgroundColorChanged. The new value is {0}.", e.NewValue);

            StatusBar.GetForCurrentView().BackgroundColor = (Color) e.NewValue;
        }

        /// <summary>
        /// Eventhandler, der aufgerufen wird, wenn sich der Wert des BackgroundOpacityProperty geändert hat.
        /// Aktualisiert den Transparenzwert der Hintergrundfarbe der StatusBar.
        /// </summary>
        /// <param name="d">Das zugehörige DependencyObject.</param>
        /// <param name="e">Der Eventparameter, die den neuen Wert für die Farbe enthält.</param>
        private static void OnBackgroundOpacityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // Debug.WriteLine("In OnBackgroundOpacityChanged. The new value is {0}.", e.NewValue);
            double backgroundOpacity = (double)e.NewValue;

            if (backgroundOpacity > 1.0f)
            {
                backgroundOpacity = 1.0f;
            }
            else if(backgroundOpacity < 0.0f)
            {
                backgroundOpacity = 0.0f;
            }

            StatusBar.GetForCurrentView().BackgroundOpacity = backgroundOpacity;
        }
        #endregion PropertyChangedHandler

        #region AccessFields
        /// <summary>
        /// IsVisible bietet Zugriff auf das IsVisibleProperty und erlaubt es dessen Wert abzufragen und zu ändern.
        /// </summary>
        public bool IsVisible
        {
            get { return (bool)GetValue(IsVisibleProperty); }
            set { SetValue(IsVisibleProperty, value); }
        }

        /// <summary>
        /// ForegroundColor bietet Zugriff auf das ForegroundColorProperty und erlaubt es dessen Wert abzufragen und zu ändern.
        /// </summary>
        public Color ForegroundColor
        {
            get { return (Color)GetValue(ForegroundColorProperty); }
            set { SetValue(ForegroundColorProperty, value); }
        }

        /// <summary>
        /// BackgroundColor bietet Zugriff auf das BackgroundColorProperty und erlaubt es dessen Wert abzufagen und zu ändern.
        /// </summary>
        public Color BackgroundColor
        {
            get { return (Color)GetValue(BackgroundColorProperty); }
            set { SetValue(BackgroundColorProperty, value); }
        }

        /// <summary>
        /// BackgroundOpacity bietet Zugriff auf das BackgroundOpacityProperty und erlaubt es dessen Wert abzufragen und zu ändern.
        /// </summary>
        public double BackgroundOpacity
        {
            get { return (double)GetValue(BackgroundOpacityProperty); }
            set { SetValue(BackgroundOpacityProperty, value); }
        }
        #endregion AccessFields
    }
}
