using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Lumafly.ViewModels;
using Lumafly.Views.Windows;

namespace Lumafly
{
    public class ViewLocator : IDataTemplate
    {
        private const string ViewNameSpace = "Views";
        
        private readonly string[] ViewSubNamespaces = 
        {
            "Pages",
            "Windows"
        };
        
        public Control Build(object? data)
        {
            var className = data?.GetType().Name.Replace("ViewModel", "View") ?? throw new InvalidOperationException();
            var baseNameSpace = typeof(ViewLocator).Namespace ?? throw new InvalidOperationException();

            Type? type = null;

            foreach (var viewSubNamespace in ViewSubNamespaces)
            {
                type ??= Type.GetType($"{baseNameSpace}.{ViewNameSpace}.{viewSubNamespace}.{className}");
            }

            if (type == null) 
                return new TextBlock { Text = "Not Found: " + data };
            
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