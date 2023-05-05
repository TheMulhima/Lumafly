using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using ReactiveUI;

namespace Scarab.Models
{
    [Serializable]
    public class Profile : INotifyPropertyChanged
    {
        /// <summary>
        /// The currently selected profile.
        /// </summary>
        [JsonIgnore]
        public static Profile? CurrentProfile { get; set; }

        /// <summary>
        /// Whether the profile is the currently selected one.
        /// </summary>
        [JsonIgnore]
        public bool Current
        {
            get => CurrentProfile == this;
            set
            {
                OnPropertyChanged();
                CurrentProfile = value ? this : null;
            }
        }

        public Profile(string? name, string[] modNames)
        {
            Name = name;
            ModNames = modNames;
        }

        /// <summary>
        /// The profile's name.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// A list of the names of the mods in the profile.
        /// </summary>
        public string[] ModNames { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
