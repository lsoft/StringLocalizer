using EnvDTE;
using Extension.Helper;
using Extension.WPF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Extension.UI.ViewModels
{
    public sealed class AddStringViewModel : BaseViewModel
    {
        private ICommand _currentApplyCommand;
        private ResxGroup _selectedCurrentResxGroup;
        private readonly string _defaultText;

        public AddResourceResult? Result
        {
            get;
            private set;
        }

        public Action<bool?> CloseWindow
        {
            get;
            set;
        }

        public ObservableCollection2<ResxGroup> CurrentResxGroupList
        {
            get;
        }

        public ResxGroup SelectedCurrentResxGroup
        {
            get => _selectedCurrentResxGroup;
            set
            {
                if (ReferenceEquals(_selectedCurrentResxGroup, value))
                {
                    return;
                }

                _selectedCurrentResxGroup = value;
                FillCurrentLanguageList();
                OnPropertyChanged();
            }
        }

        public string CurrentResourceName
        {
            get;
            set;
        }

        public string CurrentNeutralComment
        {
            get;
            set;
        }

        public ObservableCollection2<LanguageViewModel> CurrentLanguageList
        {
            get;
        }


        public ICommand CurrentApplyCommand
        {
            get
            {
                if (_currentApplyCommand is null)
                {
                    _currentApplyCommand = new AsyncRelayCommand(
                        async a =>
                        {
                            foreach (var language in CurrentLanguageList)
                            {
                                language.Resx.AddNewResource(
                                    CurrentResourceName,
                                    CurrentNeutralComment,
                                    language.Text
                                    );
                            }

                            Result = new AddResourceResult(
                                SelectedCurrentResxGroup,
                                CurrentResourceName
                                );
                            CloseWindow(true);
                        },
                        a =>
                        {
                            if (string.IsNullOrEmpty(CurrentResourceName))
                            {
                                return false;
                            }
                            if (SelectedCurrentResxGroup.CheckForExistingKey(CurrentResourceName))
                            {
                                return false;
                            }
                            if (CloseWindow is null)
                            {
                                return false;
                            }

                            return true;
                        });
                }

                return _currentApplyCommand;
            }
        }

        public AddStringViewModel(
            string neutralComment,
            IReadOnlyList<ResxGroup> resxGroups,
            string defaultText
            )
        {
            if (neutralComment is null)
            {
                throw new ArgumentNullException(nameof(neutralComment));
            }

            if (resxGroups is null)
            {
                throw new ArgumentNullException(nameof(resxGroups));
            }

            if (defaultText is null)
            {
                throw new ArgumentNullException(nameof(defaultText));
            }

            CurrentResxGroupList = new ObservableCollection2<ResxGroup>();
            resxGroups.ForEach(g => CurrentResxGroupList.Add(g));
            _selectedCurrentResxGroup = CurrentResxGroupList.First();

            CurrentLanguageList = new ObservableCollection2<LanguageViewModel>();
            CurrentResourceName = GenerateResourceName(defaultText);
            CurrentNeutralComment = neutralComment;
            _defaultText = defaultText;
            
            FillCurrentLanguageList();
        }

        private void FillCurrentLanguageList()
        {
            CurrentLanguageList.Clear();
            SelectedCurrentResxGroup.ResxList
                .OrderByDescending(r => r.IsNeutralCulture)
                .ForEach(r =>
                    CurrentLanguageList.Add(
                        new LanguageViewModel(
                            r,
                            _defaultText
                            )
                        )
                    );
        }

        private string GenerateResourceName(string text)
        {
            var sb = new StringBuilder();

            var index = 0;
            foreach (var c in text)
            {
                if (char.IsDigit(c) || char.IsLetter(c))
                {
                    sb.Append(c);
                }
                else
                {
                    if (index >= 32)
                    {
                        break;
                    }
                    sb.Append('_');
                }

                index++;
            }

            return sb.ToString().Trim('_');
        }
    }

    public sealed class AddResourceResult
    {
        public ResxGroup Group
        {
            get;
        }

        public string ResourceName
        {
            get;
        }

        public AddResourceResult(
            ResxGroup group,
            string resourceName
            )
        {
            if (group is null)
            {
                throw new ArgumentNullException(nameof(group));
            }

            if (string.IsNullOrEmpty(resourceName))
            {
                throw new ArgumentException($"'{nameof(resourceName)}' cannot be null or empty.", nameof(resourceName));
            }

            Group = group;
            ResourceName = resourceName;
        }
    }
}
