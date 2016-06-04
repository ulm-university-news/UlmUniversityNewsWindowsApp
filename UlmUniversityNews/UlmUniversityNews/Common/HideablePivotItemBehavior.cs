using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using Microsoft.Xaml.Interactions.Core;
using Microsoft.Xaml.Interactions.Media;
using Microsoft.Xaml.Interactivity;
using Windows.UI.Xaml;
using System.Diagnostics;

namespace UlmUniversityNews.Common
{
    public class HideablePivotItemBehavior : Behavior<PivotItem>
    {
        #region StaticFields
        public static readonly DependencyProperty VisibleProperty = DependencyProperty.Register(
            "Visible",
            typeof(bool),
            typeof(HideablePivotItemBehavior),
            new PropertyMetadata(true, VisiblePropertyChanged));

        //public static readonly DependencyProperty TargetElement = DependencyProperty.Register(
        //    "TargetElement",
        //    typeof(DependencyObject),
        //    typeof(HideablePivotItemBehavior),
        //    new PropertyMetadata(null, TargetElementPropertyChanged)
        //    );
        #endregion StaticFields

        #region Fields
        private Pivot _parentPivot;
        private PivotItem _pivotItem;
        private int _previousPivotItemIndex;
        private int _lastPivotItemsCount;

        //private DependencyObject _parentPivotElement;
        #endregion Fields

        #region Properties
        public bool Visible
        {
            get { return (bool)this.GetValue(VisibleProperty); }
            set { this.SetValue(VisibleProperty, value); }
        }
        #endregion Properties

        /// <summary>
        /// Wird aufgerufen, wenn das Behaviour an ein XAML Element gebunden wird.
        /// </summary>
        protected override void OnAttached()
        {
            base.OnAttached();

            this._pivotItem = AssociatedObject;
        }

        //private static void TargetElementPropertyChanged(DependencyObject dpObj, DependencyPropertyChangedEventArgs change)
        //{
        //    // Prüfe, ob das DependencyObject ein HideablePivotItemBehaviour ist und ob bei change ein DependencyObject übergeben wurde.
        //    if (change.NewValue.GetType() != typeof(DependencyObject) || dpObj.GetType() != typeof(HideablePivotItemBehavior))
        //    {
        //        return;
        //    }

        //    HideablePivotItemBehavior behavior = (HideablePivotItemBehavior)dpObj;
        //    behavior._parentPivotElement = (DependencyObject) change.NewValue;
        //}

        private static void VisiblePropertyChanged(DependencyObject dpObj, DependencyPropertyChangedEventArgs change)
        {
            Debug.WriteLine("HideablePivotItemBehavior: Visibility changed.");

            // Prüfe, ob man einen bool Wert erhalten hat und ob das DependencyObject ein HideablePivotItemBehaviour ist.
            if (change.NewValue.GetType() != typeof(bool) || dpObj.GetType() != typeof(HideablePivotItemBehavior))
            {
                return;
            }

            HideablePivotItemBehavior behavior = (HideablePivotItemBehavior) dpObj;
            PivotItem pivotItem = behavior._pivotItem;

            // Das Parent Pivot Element kann erst zugewiesen werden, nachdem die Baumstruktur der View Elemente initialisiert ist.
            if(behavior._parentPivot == null)
            {
                behavior._parentPivot = (Pivot) behavior._pivotItem.Parent; // Hole das Pivot Element zum PivotItem.
                // Falls das Pivot Element nicht extrahiert werden konnte.
                if (behavior._parentPivot == null)
                {
                    return;
                }
            }

            Pivot parentPivot = behavior._parentPivot;
            // Wenn nun Visibility auf false gesetzt wurde.
            if(!(bool)change.NewValue)      
            {
                Debug.WriteLine("HideablePivotItemBehavior: Need to remove pivot item.");
                if(parentPivot.Items.Contains(behavior._pivotItem))     // Prüfe, ob das PivotItem aktuell als Item im Pivot Element registriert ist.
                {
                    behavior._previousPivotItemIndex = parentPivot.Items.IndexOf(pivotItem);
                    parentPivot.Items.Remove(pivotItem);            // Entferne das PivotItem aus der Collection von PivotItems des Pivot Elements.
                    behavior._lastPivotItemsCount = parentPivot.Items.Count;
                    Debug.WriteLine("HideablePivotItemBehavior: Pivot item with name {0} removed.", pivotItem.Name);
                }
            }
            else
            {
                Debug.WriteLine("HideablePivotItemBehavior: Need to add pivot item with name {0}.", pivotItem.Name);
                // Visibility wieder auf true gesetzt.
                if (!parentPivot.Items.Contains(pivotItem))     // Falls das PivotItem nicht schon in der Collection von PivotItems enthalten ist.
                {
                    if (behavior._lastPivotItemsCount >= parentPivot.Items.Count)   // Wurden in der Zwischenzeit bereits weitere Items entfernt?
                    {
                        parentPivot.Items.Insert(behavior._previousPivotItemIndex, pivotItem);
                    }
                    else
                    {
                        parentPivot.Items.Add(pivotItem);
                    }
                }
            }
        }
    }
}
