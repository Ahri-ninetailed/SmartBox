#pragma warning disable MVVMTK0045

using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace SmartBox.Models.Parser
{
    // TODO: Не уверен, что большинство свойств должны быть ObservableProperty
    public partial class Word : ObservableObject
    {
        public string Title
        {
            get
            {
                string? pluralForms = null;
                if (PluralForms.Length != 0)
                {
                    pluralForms = $" ({string.Join(", ", PluralForms)})";
                }

                string area = string.IsNullOrWhiteSpace(Area) ? "" : $" ({Area})";
                string otherForms = OtherForms.Length == 0 ? "" : $", {string.Join(", ", OtherForms)}";

                return $"{Type}{area}\n{Value}{otherForms}{pluralForms}";
            }
        }

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
        private string[] pluralForms = Array.Empty<string>();

        [ObservableProperty]
        private string[] otherForms = Array.Empty<string>();

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
