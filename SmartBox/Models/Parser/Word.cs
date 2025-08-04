#pragma warning disable MVVMTK0045

using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace SmartBox.Models.Parser
{
    public partial class Word : ObservableObject
    {
        public string Title => $"{Value} [{Type}]" + (string.IsNullOrEmpty(Area) ? "" : $" - {Area}");

        internal bool SuppressIsSelectedChanged;

        [ObservableProperty]
        private bool isSelected;

        [ObservableProperty]
        private ObservableCollection<Meaning> children = new();

        [ObservableProperty]
        private string value = string.Empty;

        [ObservableProperty]
        private string? area;

        [ObservableProperty]
        private PartOfSpeech type;

        partial void OnIsSelectedChanged(bool value)
        {
            if (SuppressIsSelectedChanged)
            {
                return;
            }

            foreach (Meaning meaning in Children)
            {
                meaning.IsSelected = value;
            }
        }
    }

    public partial class Meaning : ObservableObject
    {
        public string Title => Value;

        internal bool SuppressIsSelectedChanged;

        [ObservableProperty]
        private bool isSelected;

        [ObservableProperty]
        private Word parent = new();

        [ObservableProperty]
        private ObservableCollection<Example> children = new();

        [ObservableProperty]
        private string value = string.Empty;

        partial void OnIsSelectedChanged(bool value)
        {
            if (SuppressIsSelectedChanged)
            {
                return;
            }

            if (value)
            {
                Parent.SuppressIsSelectedChanged = true;
                Parent.IsSelected = value;
                Parent.SuppressIsSelectedChanged = false;
            }
            else
            {
                if (Parent.Children.All(el => el.IsSelected == value))
                {
                    Parent.SuppressIsSelectedChanged = true;
                    Parent.IsSelected = value;
                    Parent.SuppressIsSelectedChanged = false;
                }
            }

            foreach (Example example in Children)
            {
                example.IsSelected = value;
            }
        }
    }

    public partial class Example : ObservableObject
    {
        public string Title => Value;

        [ObservableProperty]
        private bool isSelected;

        [ObservableProperty]
        private Meaning parent = new();

        [ObservableProperty]
        private string value = string.Empty;

        partial void OnIsSelectedChanged(bool value)
        {
            if (value)
            {
                Parent.SuppressIsSelectedChanged = true;
                Parent.IsSelected = value;
                Parent.SuppressIsSelectedChanged = false;

                Parent.Parent.SuppressIsSelectedChanged = true;
                Parent.Parent.IsSelected = value;
                Parent.Parent.SuppressIsSelectedChanged = false;
            }
            else
            {
                if (Parent.Children.All(el => el.IsSelected == value))
                {
                    Parent.SuppressIsSelectedChanged = true;
                    Parent.IsSelected = value;
                    Parent.SuppressIsSelectedChanged = false;

                    if (Parent.Parent.Children.All(el => el.IsSelected == value))
                    {
                        Parent.Parent.SuppressIsSelectedChanged = true;
                        Parent.Parent.IsSelected = value;
                        Parent.Parent.SuppressIsSelectedChanged = false;
                    }
                }
            }
        }
    }
}
