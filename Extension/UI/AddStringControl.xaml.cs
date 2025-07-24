using Extension.WPF;
using Microsoft.Xaml.Behaviors;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Extension.UI
{
    public partial class AddStringControl : UserControl
    {
        public static readonly DependencyProperty ResxGroupListProperty =
            DependencyProperty.Register(
                nameof(ResxGroupList),
                typeof(ObservableCollection2<ResxGroup>),
                typeof(AddStringControl)
                );

        public ObservableCollection2<ResxGroup> ResxGroupList
        {
            get => (ObservableCollection2<ResxGroup>)GetValue(ResxGroupListProperty);
            set => SetValue(ResxGroupListProperty, value);
        }



        public static readonly DependencyProperty SelectedResxGroupProperty =
            DependencyProperty.Register(
                nameof(SelectedResxGroup),
                typeof(ResxGroup),
                typeof(AddStringControl),
                new FrameworkPropertyMetadata(
                    default(ResxGroup),
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault
                    )
                );

        public ResxGroup SelectedResxGroup
        {
            get => (ResxGroup)GetValue(SelectedResxGroupProperty);
            set => SetValue(SelectedResxGroupProperty, value);
        }



        public static readonly DependencyProperty ResourceNameProperty =
            DependencyProperty.Register(
                nameof(ResourceName),
                typeof(string),
                typeof(AddStringControl),
                new FrameworkPropertyMetadata(
                    default(string),
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault
                    )
                );

        public string ResourceName
        {
            get => (string)GetValue(ResourceNameProperty);
            set => SetValue(ResourceNameProperty, value);
        }



        public static readonly DependencyProperty NeutralCommentProperty =
            DependencyProperty.Register(
                nameof(NeutralComment),
                typeof(string),
                typeof(AddStringControl),
                new FrameworkPropertyMetadata(
                    default(string),
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault
                    )
                );

        public string NeutralComment
        {
            get => (string)GetValue(NeutralCommentProperty);
            set => SetValue(NeutralCommentProperty, value);
        }



        public static readonly DependencyProperty LanguageListProperty =
            DependencyProperty.Register(
                nameof(LanguageList),
                typeof(ObservableCollection2<LanguageViewModel>),
                typeof(AddStringControl)
                );

        public ObservableCollection2<LanguageViewModel> LanguageList
        {
            get => (ObservableCollection2<LanguageViewModel>)GetValue(LanguageListProperty);
            set => SetValue(LanguageListProperty, value);
        }



        public static readonly DependencyProperty ApplyCommandProperty =
            DependencyProperty.Register(
                nameof(ApplyCommand),
                typeof(ICommand),
                typeof(AddStringControl)
                );

        public ICommand ApplyCommand
        {
            get => (ICommand)GetValue(ApplyCommandProperty);
            set => SetValue(ApplyCommandProperty, value);
        }


        public AddStringControl()
        {
            InitializeComponent();
        }

        private void TB_GotKeyboardFocus(object sender, RoutedEventArgs e)
        {
            var tb = sender as TextBox;
            tb.SelectAll();
        }

        private void ControlInternalName_Loaded(object sender, RoutedEventArgs e)
        {
            _ = Task.Run(
                async () =>
                {
                    await Task.Delay(200);

                    await FocusToListViewItemAsync(1);
                });
        }

        private async Task FocusToListViewItemAsync(
            int index
            )
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            if (LanguageListView.Items.Count <= index)
                return;

            // Прокрутить первый элемент в видимость
            LanguageListView.ScrollIntoView(LanguageListView.Items[index]);

            var container = LanguageListView.ItemContainerGenerator.ContainerFromIndex(index) as ListViewItem;
            if (container != null)
            {
                FocusTextBox(container);
            }
            else
            {
                // Если контейнер еще не сгенерирован, ждем завершения генерации
                LanguageListView.ItemContainerGenerator.StatusChanged += (s, args) =>
                {
                    if (LanguageListView.ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated)
                    {
                        container = LanguageListView.ItemContainerGenerator.ContainerFromIndex(index) as ListViewItem;
                        FocusTextBox(container);
                    }
                };
            }
        }

        private static void FocusTextBox(ListViewItem container)
        {
            if (container is null)
            {
                return;
            }

            var textBox = FindVisualChild<TextBox>(container, "TB");
            if (textBox is null)
            {
                return;
            }

            textBox.Focus();
        }

        private static T FindVisualChild<T>(DependencyObject obj, string name)
            where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                var child = VisualTreeHelper.GetChild(obj, i);
                if (child is FrameworkElement fe && fe.Name == name && child is T result)
                {
                    return result;
                }
                var found = FindVisualChild<T>(child, name);
                if (found != null)
                    return found;
            }
            return null;
        }

    }

    public sealed class LanguageViewModel : BaseViewModel
    {
        public Resx Resx
        {
            get;
        }

        public string LanguageName
        {
            get;
        }

        public string Text
        {
            get;
            set;
        }

        public LanguageViewModel(
            Resx resx,
            string text
            )
        {
            Resx = resx;
            LanguageName = resx.GetCultureName();
            Text = text;
        }
    }

    public class ListViewGridViewBehavior : Behavior<ListView>
    {
        public static readonly DependencyProperty ColumnProportionsProperty =
            DependencyProperty.Register(
                nameof(ColumnProportions),
                typeof(DoubleCollection),
                typeof(ListViewGridViewBehavior),
                new PropertyMetadata(null, OnColumnProportionsChanged));

        public DoubleCollection ColumnProportions
        {
            get => (DoubleCollection)GetValue(ColumnProportionsProperty);
            set => SetValue(ColumnProportionsProperty, value);
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.Loaded += OnLoaded;
            AssociatedObject.SizeChanged += OnSizeChanged;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.Loaded -= OnLoaded;
            AssociatedObject.SizeChanged -= OnSizeChanged;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _ = Dispatcher.BeginInvoke(new Action(UpdateColumns), DispatcherPriority.Loaded);
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateColumns();
        }

        private void UpdateColumns()
        {
            if (AssociatedObject?.View is not GridView gridView)
                return;

            var scrollViewer = GetScrollViewer(AssociatedObject);
            double totalWidth = scrollViewer?.ViewportWidth ?? AssociatedObject.ActualWidth;

            if (totalWidth <= 0)
                return;

            var proportions = ColumnProportions ?? new DoubleCollection(gridView.Columns.Cast<object>().Select(_ => 1.0));
            double sum = proportions.Sum();

            for (int i = 0; i < gridView.Columns.Count; i++)
            {
                var column = gridView.Columns[i];
                double proportion = i < proportions.Count ? proportions[i] : 1.0;
                column.Width = (totalWidth / sum) * proportion;
            }
        }

        private static void OnColumnProportionsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var behavior = (ListViewGridViewBehavior)d;
            behavior.UpdateColumns();
        }

        private static ScrollViewer GetScrollViewer(DependencyObject parent)
        {
            if (parent is ScrollViewer viewer)
                return viewer;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                var result = GetScrollViewer(child);
                if (result != null)
                    return result;
            }

            return null;
        }
    }

    public class IndexToTabIndexConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int index && parameter is string offsetStr && int.TryParse(offsetStr, out int offset))
            {
                return offset + index;
            }
            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
