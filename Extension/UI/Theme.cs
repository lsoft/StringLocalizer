﻿using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Microsoft.VisualStudio.PlatformUI;

namespace Extension.UI
{
    public static class VsTheme
    {
        private static readonly Dictionary<UIElement, bool> _isUsingVsTheme = new Dictionary<UIElement, bool>();
        private static readonly Dictionary<UIElement, object> _originalBackgrounds = new Dictionary<UIElement, object>();

        public static DependencyProperty UseVsThemeProperty = DependencyProperty.RegisterAttached("UseVsTheme", typeof(bool), typeof(VsTheme), new PropertyMetadata(false, UseVsThemePropertyChanged));

        private static void UseVsThemePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            SetUseVsTheme((FrameworkElement)d, (bool)e.NewValue);
        }

        public static void SetUseVsTheme(FrameworkElement element, bool value)
        {
            if (value)
            {
                if (!_originalBackgrounds.ContainsKey(element) && element is System.Windows.Controls.Control c)
                {
                    _originalBackgrounds[element] = c.Background;
                }

                element.ShouldBeThemed();
            }
            else
            {
                element.ShouldNotBeThemed();
            }

            _isUsingVsTheme[element] = value;
        }

        public static bool GetUseVsTheme(UIElement element)
        {
            return _isUsingVsTheme.TryGetValue(element, out var value) && value;
        }

        private static ResourceDictionary BuildThemeResources()
        {
            var allResources = new ResourceDictionary();

            try
            {
                var shellResources = (ResourceDictionary)Application.LoadComponent(new Uri("Microsoft.VisualStudio.Platform.WindowManagement;component/Themes/ThemedDialogDefaultStyles.xaml", UriKind.Relative));
                var scrollStyleContainer = (ResourceDictionary)Application.LoadComponent(new Uri("Microsoft.VisualStudio.Shell.UI.Internal;component/Styles/ScrollBarStyle.xaml", UriKind.Relative));
                allResources.MergedDictionaries.Add(shellResources);
                allResources.MergedDictionaries.Add(scrollStyleContainer);
                allResources[typeof(ScrollViewer)] = new Style
                {
                    TargetType = typeof(ScrollViewer),
                    BasedOn = (Style)scrollStyleContainer[VsResourceKeys.ScrollViewerStyleKey]
                };

                allResources[typeof(TextBox)] = new Style
                {
                    TargetType = typeof(TextBox),
                    BasedOn = (Style)shellResources[typeof(TextBox)],
                    Setters =
                    {
                        new Setter(System.Windows.Controls.Control.PaddingProperty, new Thickness(2, 3, 2, 3))
                    }
                };

                allResources[typeof(ComboBox)] = new Style
                {
                    TargetType = typeof(ComboBox),
                    BasedOn = (Style)shellResources[typeof(ComboBox)],
                    Setters =
                    {
                        new Setter(System.Windows.Controls.Control.PaddingProperty, new Thickness(2, 3, 2, 3))
                    }
                };
            }
            catch
            {
            }

            return allResources;
        }

        private static ResourceDictionary ThemeResources { get; } = BuildThemeResources();

        private static void ShouldBeThemed(this FrameworkElement control)
        {
            if (control.Resources == null)
            {
                control.Resources = ThemeResources;
            }
            else if (control.Resources != ThemeResources)
            {
                var d = new ResourceDictionary();
                d.MergedDictionaries.Add(ThemeResources);
                d.MergedDictionaries.Add(control.Resources);
                control.Resources = null;
                control.Resources = d;
            }

            if (control is System.Windows.Controls.Control c)
            {
                c.SetResourceReference(System.Windows.Controls.Control.BackgroundProperty, (string)EnvironmentColors.StartPageTabBackgroundBrushKey);
            }
        }

        private static void ShouldNotBeThemed(this FrameworkElement control)
        {
            if (control.Resources != null)
            {
                if (control.Resources == ThemeResources)
                {
                    control.Resources = new ResourceDictionary();
                }
                else
                {
                    control.Resources.MergedDictionaries.Remove(ThemeResources);
                }
            }

            //If we're themed now and we're something with a background property, reset it
            if (GetUseVsTheme(control) && control is System.Windows.Controls.Control c)
            {
                if (_originalBackgrounds.TryGetValue(control, out var background))
                {
                    c.SetValue(System.Windows.Controls.Control.BackgroundProperty, background);
                }
                else
                {
                    c.ClearValue(System.Windows.Controls.Control.BackgroundProperty);
                }
            }
        }
    }
}
