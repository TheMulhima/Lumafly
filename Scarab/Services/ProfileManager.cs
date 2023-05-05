using Microsoft.Extensions.DependencyInjection;
using Scarab.Interfaces;
using Scarab.Models;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Scarab.Services
{
    public class ProfileManager
    {
        /// <summary>
        /// Internally managed reference to application settings.
        /// </summary>
        private static ISettings? _settings;

        /// <summary>
        /// Internally managed reference to mod installer.
        /// </summary>
        private static IInstaller? _installer;

        /// <summary>
        /// Internally managed reference to mod database.
        /// </summary>
        private static IModDatabase? _modDatabase;

        /// <summary>
        /// A list of all the profiles saved by the application.
        /// </summary>
        private static ObservableCollection<Profile> ProfileList => _settings!.Profiles;

        public ProfileManager(ISettings settings, IServiceProvider serviceProvider)
        {
            _installer = serviceProvider.GetRequiredService<IInstaller>();
            _modDatabase = serviceProvider.GetRequiredService<IModDatabase>();
            _settings = settings;
            SetCurrentProfile(ProfileList.FirstOrDefault(profile => profile.Name == _settings.CurrentProfileName));
        }

        /// <summary>
        /// Set the current profile.
        /// </summary>
        /// <param name="profile">The profile to set as current.</param>
        /// <exception cref="ArgumentException">The profile does not exist in the profile list.</exception>
        public static void SetCurrentProfile(Profile? profile)
        {
            if (profile is not null && (ProfileList == null || !ProfileList.Contains(profile)))
            {
                throw new ArgumentException($"Could not set current profile: {profile.Name} not found in ProfileList!");
            }

            if (_settings != null)
            {
                _settings.CurrentProfileName = profile?.Name;
                _settings.Save();
            }

            Profile.CurrentProfile = profile;
            Task.Run(() => ActivateModsInProfile(profile));
        }

        /// <summary>
        /// Activate all the mods in the current profile.
        /// </summary>
        private static async Task ActivateModsInProfile(Profile? profile)
        {
            if (profile is not null)
            {
                bool IsInDependencyTree(string sourceName, ModItem toCheck)
                {
                    var sourceMod = _modDatabase!.Items.First(mod => mod.Name == sourceName);
                    return sourceMod.Name == toCheck.Name ||
                           sourceMod.Dependencies.Any(dep => IsInDependencyTree(dep, toCheck));
                }

                foreach (var modItem in _modDatabase!.Items)
                {
                    switch (modItem.EnabledIsChecked)
                    {
                        case false when profile.ModNames.Contains(modItem.Name):
                            await _installer!.Install(modItem, args => { }, true);
                            break;
                        case true when
                            !profile.ModNames.Any(modName => IsInDependencyTree(modName, modItem)):
                            await _installer!.Uninstall(modItem);
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Create a new profile.
        /// </summary>
        /// <param name="profileName">The name of the new profile.</param>
        /// <param name="modNames">The list of mods by name in the new profile.</param>
        /// <exception cref="ArgumentException">A profile already exists with the same name.</exception>
        public static void CreateProfile(string? profileName, string[] modNames)
        {
            if (ProfileList.Any(profile => profile.Name == profileName))
            {
                throw new ArgumentException($"Could not create profile: {profileName} already exists in ProfileList!");
            }

            var profile = new Profile(profileName, modNames);
            if (_settings != null)
            {
                _settings.Profiles.Add(profile);
                _settings.Save();
            }
        }

        /// <summary>
        /// Remove a profile by object.
        /// </summary>
        /// <param name="profile">The profile to remove.</param>
        /// <exception cref="ArgumentException">The profile to remove was not found in the profile list.</exception>
        public static void RemoveProfile(Profile profile)
        {
            if (ProfileList == null || !ProfileList.Contains(profile))
            {
                throw new ArgumentException($"Could not remove profile: {profile.Name} not found in ProfileList!");
            }

            if (_settings != null)
            {
                if (_settings.Profiles.Remove(profile))
                {
                    _settings.Save();
                }
            }
        }

        /// <summary>
        /// Remove a profile by name.
        /// </summary>
        /// <param name="profileName">The name of the profile to remove.</param>
        /// <exception cref="ArgumentException">The profile to remove could not be found.</exception>
        public static void RemoveProfile(string profileName)
        {
            var profile = ProfileList?.FirstOrDefault(profile => profile.Name == profileName);
            if (profile == null)
            {
                throw new ArgumentException($"Could not remove profile: {profileName} not found in ProfileList!");
            }

            RemoveProfile(profile);
        }
    }
}
