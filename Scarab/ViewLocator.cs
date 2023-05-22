using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Scarab.ViewModels;

namespace Scarab
{
    public class ViewLocator : IDataTemplate
    {
        public Control Build(object? data)
        {
            string? name = data?.GetType().FullName?.Replace("ViewModel", "View");

            if (string.IsNullOrEmpty(name))
                throw new InvalidOperationException($"{nameof(name)}: {name}");

            var type = Type.GetType(name);

            if (type == null) 
                return new TextBlock { Text = "Not Found: " + name };
            
            var ctrl = (Control) Activator.CreateInstance(type)!;
            ctrl.DataContext = data;

            return ctrl;

        }

        public bool Match(object? data)
        {
            return data is ViewModelBase;
        }
    }
}