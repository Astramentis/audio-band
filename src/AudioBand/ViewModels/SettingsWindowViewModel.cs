﻿using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using AudioBand.Commands;
using AudioBand.Messages;
using AudioBand.Settings;

namespace AudioBand.ViewModels
{
    /// <summary>
    /// View model for the settings window.
    /// </summary>
    public class SettingsWindowViewModel : ViewModelBase
    {
        private readonly IAppSettings _appSettings;
        private readonly IDialogService _dialogService;
        private readonly IMessageBus _messageBus;
        private ViewModelBase _selectedViewModel;
        private string _selectedProfileName;
        private bool _hasUnsavedChanges;
        private string _selectedViewHeader;

        /// <summary>
        /// Initializes a new instance of the <see cref="SettingsWindowViewModel"/> class.
        /// </summary>
        /// <param name="appSettings">The app settings.</param>
        /// <param name="dialogService">The dialog service.</param>
        /// <param name="viewModels">The view models.</param>
        /// <param name="messageBus">The message bus.</param>
        public SettingsWindowViewModel(IAppSettings appSettings, IDialogService dialogService, IViewModelContainer viewModels, IMessageBus messageBus)
        {
            _appSettings = appSettings;
            _dialogService = dialogService;
            _messageBus = messageBus;
            messageBus.Subscribe<EditStartMessage>(EditStartMessageOnPublished);
            ViewModels = viewModels;
            _selectedProfileName = appSettings.CurrentProfile;
            Profiles = new ObservableCollection<string>(appSettings.Profiles);

            SelectViewModelCommand = new RelayCommand<ViewModelBase>(SelectViewModelOnExecute);
            DeleteProfileCommand = new RelayCommand<string>(DeleteProfileCommandOnExecute, DeleteProfileCommandCanExecute);
            DeleteProfileCommand.Observe(Profiles);
            AddProfileCommand = new RelayCommand(AddProfileCommandOnExecute);
            RenameProfileCommand = new RelayCommand(RenameProfileCommandOnExecute);
            SaveCommand = new RelayCommand(SaveCommandOnExecute, SaveCommandCanExecute);
            SaveCommand.Observe(this, nameof(HasUnsavedChanges));
            CloseCommand = new RelayCommand(CloseCommandOnExecute);
            ImportProfilesCommand = new RelayCommand(ImportProfilesCommandOnExecute);
            ExportProfilesCommand = new RelayCommand(ExportProfilesCommandOnExecute);
        }

        /// <summary>
        /// Gets the view models.
        /// </summary>
        public IViewModelContainer ViewModels { get; }

        /// <summary>
        /// Gets or sets the currently selected view model.
        /// </summary>
        public ViewModelBase SelectedViewModel
        {
            get => _selectedViewModel;
            set => SetProperty(ref _selectedViewModel, value);
        }

        /// <summary>
        /// Gets or sets the selected profile name.
        /// </summary>
        public string SelectedProfileName
        {
            get => _selectedProfileName;
            set
            {
                if (SetProperty(ref _selectedProfileName, value))
                {
                    EndEdits();
                    _appSettings.CurrentProfile = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the current title of the view.
        /// </summary>
        public string SelectedViewHeader
        {
            get => _selectedViewHeader;
            set => SetProperty(ref _selectedViewHeader, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether there are unsaved changes.
        /// </summary>
        public bool HasUnsavedChanges
        {
            get => _hasUnsavedChanges;
            set => SetProperty(ref _hasUnsavedChanges, value);
        }

        /// <summary>
        /// Gets the list of profiles.
        /// </summary>
        public ObservableCollection<string> Profiles { get; }

        /// <summary>
        /// Gets the command to select the view model.
        /// </summary>
        public RelayCommand<ViewModelBase> SelectViewModelCommand { get; }

        /// <summary>
        /// Gets the command to delete a profile.
        /// </summary>
        public RelayCommand<string> DeleteProfileCommand { get; }

        /// <summary>
        /// Gets the command to add a profile.
        /// </summary>
        public RelayCommand AddProfileCommand { get; }

        /// <summary>
        /// Gets the command to rename the current profile.
        /// </summary>
        public RelayCommand RenameProfileCommand { get; }

        /// <summary>
        /// Gets the command to save settings.
        /// </summary>
        public RelayCommand SaveCommand { get; }

        /// <summary>
        /// Gets the command to close the settings editor.
        /// </summary>
        public RelayCommand CloseCommand { get; }

        /// <summary>
        /// Gets the command to import profiles.
        /// </summary>
        public RelayCommand ImportProfilesCommand { get; }

        /// <summary>
        /// Gets the command to export profiles.
        /// </summary>
        public RelayCommand ExportProfilesCommand { get; }

        private void SelectViewModelOnExecute(ViewModelBase viewModel)
        {
            SelectedViewModel = viewModel;
        }

        private void DeleteProfileCommandOnExecute(string profileName)
        {
            Debug.Assert(Profiles.Count > 1, "Should not be able to delete profiles if there is only one");

            var deleteConfirmed = _dialogService.ShowConfirmationDialog(ConfirmationDialogType.DeleteProfile, profileName);
            if (!deleteConfirmed)
            {
                return;
            }

            _appSettings.DeleteProfile(profileName);
            Profiles.Remove(profileName);

            _appSettings.CurrentProfile = Profiles[0];
            SelectedProfileName = Profiles[0];
            _appSettings.Save();
        }

        private bool DeleteProfileCommandCanExecute(string obj)
        {
            return Profiles.Count > 1;
        }

        private void AddProfileCommandOnExecute()
        {
            const string NewProfileName = "New Profile";
            string newprofile = NewProfileName;
            int count = 1;
            while (Profiles.Contains(newprofile))
            {
                newprofile = $"{NewProfileName} {count++}";
            }

            _appSettings.CreateProfile(newprofile);
            Profiles.Add(newprofile);
        }

        private void RenameProfileCommandOnExecute()
        {
            string newProfileName = _dialogService.ShowRenameDialog(SelectedProfileName, Profiles.ToList());
            if (newProfileName == null || newProfileName == SelectedProfileName)
            {
                return;
            }

            _appSettings.RenameCurrentProfile(newProfileName);
            var index = Profiles.IndexOf(SelectedProfileName);
            Profiles[index] = newProfileName;
            SelectedProfileName = newProfileName;
        }

        private void CloseCommandOnExecute()
        {
            if (!HasUnsavedChanges)
            {
                _messageBus.Publish(SettingsWindowMessage.CloseWindow);
                return;
            }

            var discardChanges = _dialogService.ShowConfirmationDialog(ConfirmationDialogType.DiscardChanges);
            if (discardChanges)
            {
                CancelEdits();
                _messageBus.Publish(SettingsWindowMessage.CloseWindow);
            }
        }

        private void SaveCommandOnExecute()
        {
            EndEdits();
            _appSettings.Save();
        }

        private bool SaveCommandCanExecute()
        {
            return HasUnsavedChanges;
        }

        private void EndEdits()
        {
            _messageBus.Publish(EditEndMessage.Accepted);
            HasUnsavedChanges = false;
        }

        private void CancelEdits()
        {
            _messageBus.Publish(EditEndMessage.Cancelled);
            HasUnsavedChanges = false;
        }

        private void EditStartMessageOnPublished(EditStartMessage obj)
        {
            HasUnsavedChanges = true;
        }

        private void ExportProfilesCommandOnExecute()
        {
            var exportPath = _dialogService.ShowExportProfilesDialog();
            if (exportPath == null)
            {
                return;
            }

            _appSettings.ExportProfilesToPath(exportPath);
        }

        private void ImportProfilesCommandOnExecute()
        {
            try
            {
                var profilesPath = _dialogService.ShowImportProfilesDialog();
                if (profilesPath == null)
                {
                    return;
                }

                _appSettings.ImportProfilesFromPath(profilesPath);
                foreach (var newProfile in _appSettings.Profiles.Where(p => !Profiles.Contains(p)))
                {
                    // Should not be too slow unless a lot of profiles.
                    Profiles.Add(newProfile);
                }

                _appSettings.Save();
            }
            catch (Exception e)
            {
                Logger.Error(e, "Error with importing profiles");
            }
        }
    }
}
