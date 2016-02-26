using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;

namespace UlmUniversityNews.Common
{
    /// <summary>
    /// ProgressIndicatorBehavior ist ein View Behavior, welches verwendet werden kann, um eine
    /// Fortschrittsanzeige in XAML zu realisieren und auf diese per Databinding über das ViewModel
    /// zur Laufzeit Einfluss zu nehmen.
    /// Die Implementierung ist angelehnt an: https://marcominerva.wordpress.com/2014/09/11/behaviors-to-handle-statusbar-and-progressindicator-in-windows-phone-8-1-apps/
    /// </summary>
    public class ProgressIndicatorBehavior : Behavior
    {
        #region Fields
        private const string IsVisiblePropertyName = "IsVisible";
        private const string TextPropertyName = "Text";
        private const string ValuePropertyName = "Value";
        private const string IsIndeterminatePropertyName = "IsIndeterminate";
        #endregion Fields

        #region DependencyProperties
        public static readonly DependencyProperty IsVisibleProperty =
            DependencyProperty.Register(
            IsVisiblePropertyName,
            typeof(bool),
            typeof(ProgressIndicatorBehavior),
            new PropertyMetadata(false, OnIsVisibleChanged)     // Default Wert ist false.
            );

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(
            TextPropertyName,
            typeof(string),
            typeof(ProgressIndicatorBehavior),
            new PropertyMetadata(null, OnTextChanged)
            );

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(
            ValuePropertyName,
            typeof(double),
            typeof(ProgressIndicatorBehavior),
            new PropertyMetadata(null, OnValueChanged)
            );

        public static readonly DependencyProperty IsIndeterminateProperty =
            DependencyProperty.Register(
            IsIndeterminatePropertyName,
            typeof(bool),
            typeof(ProgressIndicatorBehavior),
            new PropertyMetadata(false, OnIsIndeterminateChanged)
            );
        #endregion DependencyProperties

        #region PropertyChangedHandler
        /// <summary>
        /// Eventhandler, der aufgerufen wird, wenn sich der Wert des IsVisibleProperty geändert hat.
        /// Zeigt die Fortschrittsanzeige an oder blendet sie aus, abhängig vom gesetzten Wert.
        /// </summary>
        /// <param name="d">Das zugehörige DependencyObject.</param>
        /// <param name="e">Der Eventparameter, die den neuen Wert für die Sichtbarkeit enthält.</param>
        private static async void OnIsVisibleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            StatusBarProgressIndicator progressIndicator = StatusBar.GetForCurrentView().ProgressIndicator;
            if ((bool)e.NewValue)
            {
                progressIndicator.ProgressValue = 0;
                await progressIndicator.ShowAsync();
            }
            else
            {
                await progressIndicator.HideAsync();
            }
        }

        /// <summary>
        /// Eventhandler, der aufgerufen wird, wenn sich der Wert des TextProperty geändert hat.
        /// Setzt einen neuen Wert für die Text Property des ProgressIndicator.
        /// </summary>
        /// <param name="d">Das zugehörige DependencyObject.</param>
        /// <param name="e">Der Eventparameter, die den neuen Text enthält.</param>
        private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ProgressIndicatorBehavior behavior = (ProgressIndicatorBehavior)d;
            StatusBar.GetForCurrentView().ProgressIndicator.Text = behavior.Text;
        }

        /// <summary>
        /// Eventhandler, der aufgerufen wird, wenn sich der Wert des ValueProperty geändert hat.
        /// Setzt einen neuen Wert für den ProgressValue des ProgressIndicator.
        /// </summary>
        /// <param name="d">Das zugehörige DependencyObject.</param>
        /// <param name="e">Der Eventparameter, die den neuen ProgressValue enthält.</param>
        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            double newValue = (double)e.NewValue;
            if (newValue > 1.0f)
            {
                newValue = 1.0f;
            }
            else if (newValue < 0.0f)
            {
                newValue = 0.0f;
            }

            StatusBar.GetForCurrentView().ProgressIndicator.ProgressValue = newValue;
        }

        /// <summary>
        /// Eventhandler, der aufgerufen wird, wenn sich der Wert des IsIndeterminateProperty geändert hat.
        /// Der Wert legt fest, ob es sich um eine Fortschrittsanzeige mit unbekannter Dauer handelt oder nicht.
        /// </summary>
        /// <param name="d">Das zugehörige DependencyObject.</param>
        /// <param name="e">Der Eventparameter, die den neuen IsIndeterminate Wert enthält.</param>
        private static void OnIsIndeterminateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            StatusBarProgressIndicator progressIndicator = StatusBar.GetForCurrentView().ProgressIndicator;
            if ((bool)e.NewValue){
                progressIndicator.ProgressValue = null;
            }
            else
            {
                progressIndicator.ProgressValue = 0;
            }
        }
        #endregion PropertyChangedHandler

        #region AccessFields
        /// <summary>
        /// IsVisible bietet Zugriff auf das IsVisibleProperty und erlaubt es dessen Wert abzufragen oder zu ändern.
        /// </summary>
        public bool IsVisible
        {
            get { return (bool)GetValue(IsVisibleProperty); }
            set { SetValue(IsVisibleProperty, value); }
        }

        /// <summary>
        /// Text bietet Zugriff auf das TextProperty und erlaubt es dessen Wert abzufragen oder zu ändern.
        /// </summary>
        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        /// <summary>
        /// Value bietet Zugriff auf das ValueProperty und erlaubt es dessen Wert abzufragen oder zu ändern.
        /// </summary>
        public double Value
        {
            get { return (double)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        /// <summary>
        /// IsIndeterminate bietet Zugriff auf das IsIndeterminateProperty und erlaubt es dessen Wert abzufragen oder zu ändern.
        /// </summary>
        public bool IsIndeterminate{
            get { return (bool)GetValue(IsIndeterminateProperty); }
            set { SetValue(IsIndeterminateProperty, value); }
        }
        #endregion AccessFields
    }
}
