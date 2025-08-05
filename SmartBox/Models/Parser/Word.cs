#pragma warning disable MVVMTK0045

using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace SmartBox.Models.Parser
{
    // Базовый класс для иерархического выбора
    public abstract partial class SelectableHierarchyNode : ObservableObject
    {
        internal bool SuppressIsSelectedChanged;

        [ObservableProperty]
        private bool isSelected;

        [ObservableProperty]
        private string value = string.Empty;

        public virtual string Title => Value;
        public virtual bool IsBold => true;

        partial void OnValueChanged(string value)
        {
            OnPropertyChanged(nameof(Title));
        }

        protected abstract void OnSelectionChangedCore(bool value);

        partial void OnIsSelectedChanged(bool value)
        {
            if (SuppressIsSelectedChanged)
            {
                return;
            }

            OnSelectionChangedCore(value);
        }

        protected void SetParentSelection<T>(T parent, bool value, Func<T, IEnumerable<SelectableHierarchyNode>> getSiblings)
            where T : SelectableHierarchyNode
        {
            if (parent == null) return;

            if (value)
            {
                parent.SuppressIsSelectedChanged = true;
                parent.IsSelected = value;
                parent.SuppressIsSelectedChanged = false;
            }
            else
            {
                if (getSiblings(parent).All(el => el.IsSelected == value))
                {
                    parent.SuppressIsSelectedChanged = true;
                    parent.IsSelected = value;
                    parent.SuppressIsSelectedChanged = false;
                }
            }
        }

        protected void SetChildrenSelection<T>(IEnumerable<T> children, bool value)
            where T : SelectableHierarchyNode
        {
            foreach (var child in children)
            {
                child.IsSelected = value;
            }
        }
    }

    // TODO: Не уверен, что большинство свойств должны быть ObservableProperty
    public partial class Word : SelectableHierarchyNode
    {
        public override string Title
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

        [ObservableProperty]
        private ObservableCollection<Meaning> children = new();

        [ObservableProperty]
        private string? area;

        [ObservableProperty]
        private string[] pluralForms = Array.Empty<string>();

        [ObservableProperty]
        private string[] otherForms = Array.Empty<string>();

        [ObservableProperty]
        private PartOfSpeech type;

        protected override void OnSelectionChangedCore(bool value)
        {
            SetChildrenSelection(Children, value);
        }
    }

    public partial class Meaning : SelectableHierarchyNode
    {
        [ObservableProperty]
        private Word parent = new();

        [ObservableProperty]
        private ObservableCollection<Example> children = new();

        protected override void OnSelectionChangedCore(bool value)
        {
            SetParentSelection(Parent, value, p => p.Children);
            SetChildrenSelection(Children, value);
        }
    }

    public partial class Example : SelectableHierarchyNode
    {
        public override bool IsBold => false;

        [ObservableProperty]
        private Meaning parent = new();

        protected override void OnSelectionChangedCore(bool value)
        {
            SetParentSelection(Parent, value, p => p.Children);

            if (Parent?.Parent != null)
            {
                SetParentSelection(Parent.Parent, value, p => p.Children);
            }
        }
    }
}