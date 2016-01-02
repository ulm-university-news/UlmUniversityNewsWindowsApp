using Microsoft.Xaml.Interactivity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace UlmUniversityNews.Common
{
    /// <summary>
    /// Oberklasse eines Behaviors.
    /// </summary>
    public abstract class Behavior : DependencyObject, IBehavior
    {
        protected DependencyObject AssociatedObject { get; set; }

        DependencyObject IBehavior.AssociatedObject
        {
            get { return this.AssociatedObject; }
        }

        public void Attach(Windows.UI.Xaml.DependencyObject associatedObject)
        {
            AssociatedObject = associatedObject;
            OnAttached();
        }

        public void Detach()
        {
            OnDetaching();
        }

        protected virtual void OnAttached()
        {

        }

        protected virtual void OnDetaching()
        {

        }
    }

    /// <summary>
    /// Eine generische Oberklasse für ein Behavior.
    /// </summary>
    /// <typeparam name="T">Der Typ des assoziierten XAML Objekts.</typeparam>
    public abstract class Behavior<T> : Behavior where T : DependencyObject
    {
        protected T AssociatedObject
        {
            get { return base.AssociatedObject as T; }
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            if(this.AssociatedObject == null)
            {
                throw new InvalidOperationException("Associated Object doesn't have the right type.");
            }
        }
    }
}
