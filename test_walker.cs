using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

public static class Helper {
    public static T GetParent<T>(DependencyObject element) where T : DependencyObject {
        DependencyObject parent = VisualTreeHelper.GetParent(element);
        while (parent != null) {
            if (parent is T t) return t;
            parent = VisualTreeHelper.GetParent(parent);
        }
        return null;
    }
}
