using ReactiveUI;

namespace Lumafly.ViewModels
{
    public class ViewModelBase : ReactiveObject
    {
        // Needed for source generator to find it.
        protected virtual void RaisePropertyChanged(string name) => IReactiveObjectExtensions.RaisePropertyChanged(this, name);
        protected virtual void RaisePropertyChanging(string name) => IReactiveObjectExtensions.RaisePropertyChanging(this, name);
    }
}
