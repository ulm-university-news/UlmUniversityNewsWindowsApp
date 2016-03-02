using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls.Primitives;

namespace UlmUniversityNews.Common
{
    /// <summary>
    /// Die Klasse Flyout Helper kann verwendet werden, um Flyouts dynamisch zur Laufzeit
    /// über Paramter im ViewModel aus- und einblenden zu können. 
    /// Die Implementierung basiert auf: https://marcominerva.wordpress.com/2015/01/15/how-to-open-and-close-flyouts-in-universal-apps-using-mvvm/
    /// </summary>
    public static class FlyoutHelper
    {
        #region Fields
        private const string IsOpenPropertyName = "IsOpen";
        private const string ParentPropertyName = "Parent";
        #endregion Fields

        #region DependencyProperties
        public static readonly DependencyProperty IsOpenProperty =
            DependencyProperty.RegisterAttached(
            IsOpenPropertyName,
            typeof(bool),
            typeof(FlyoutHelper),
            new PropertyMetadata(false, IsOpenChanged));

        public static readonly DependencyProperty ParentProperty =
            DependencyProperty.RegisterAttached(
            ParentPropertyName,
            typeof(FrameworkElement),
            typeof(FlyoutHelper),
            null);
        #endregion DependencyProperties

        #region EventHandler
        /// <summary>
        /// Event-Handler, der aufgerufen wird, wenn die Property IsOpen einen neuen Wert
        /// erhalten hat.
        /// </summary>
        /// <param name="d">Das DependencyObject, zu dem das Property gehört.</param>
        /// <param name="e">Die Eventparameter.</param>
        private static void IsOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Debug.WriteLine("In Flyout IsOpenChanged method. Value for d is {0}." + 
            " Value for newValue is: {1}.", d.ToString(), (bool)e.NewValue);

            FlyoutBase fb = d as FlyoutBase;
            if (fb == null)
                return;

            if ((bool)e.NewValue)
            {
                fb.Closed += fb_Closed;
                fb.ShowAt(GetParent(d));
            }
            else
            {
                fb.Closed -= fb_Closed;
                fb.Hide();
            }
        }

        /// <summary>
        /// Event-Handler, der aufgerufen wird, wenn das Flyout geschlossen wird.
        /// </summary>
        /// <param name="sender">Event-Quelle.</param>
        /// <param name="e">Eventparemeter.</param>
        static void fb_Closed(object sender, object e)
        {
            SetIsOpen(sender as DependencyObject, false);
        }
        #endregion EventHandler

        #region AccessFields
        /// <summary>
        /// Ermöglicht das Setzen des Property IsOpen.
        /// </summary>
        /// <param name="element">Das DependencyObject, zu dem das Property gehört.</param>
        /// <param name="value">Der neue Wert des Property.</param>
        public static void SetIsOpen(DependencyObject element, bool value)
        {
            element.SetValue(IsOpenProperty, value);
        }

        /// <summary>
        /// Liefert den Wert des IsOpen Property zurück.
        /// </summary>
        /// <param name="element">Das DependencyObject, zu dem das Property gehört.</param>
        /// <returns>Der Wert des IsOpen Property.</returns>
        public static bool GetIsOpen(DependencyObject element)
        {
            return (bool)element.GetValue(IsOpenProperty);
        }

        /// <summary>
        /// Liefert den Wert des Parent Property zurück.
        /// </summary>
        /// <param name="element">Das DependencyObject, zu dem das Property gehört.</param>
        /// <returns>Der Wert des Parent Property in Form eines Framework Elements.</returns>
        public static FrameworkElement GetParent(DependencyObject element)
        {
            return (FrameworkElement)element.GetValue(ParentProperty);
        }

        /// <summary>
        /// Setze den Wert des Parent Property.
        /// </summary>
        /// <param name="element">Das DependencyObject, zu dem das Property gehört.</param>
        /// <param name="value">Der neue Wert des Parent Property in Form eines Framework Elements.</param>
        public static void SetParent(DependencyObject element, FrameworkElement value)
        {
            element.SetValue(ParentProperty, value);
        }
        #endregion AccessFields
    }
}
