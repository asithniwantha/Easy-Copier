using CommunityToolkit.WinUI;
using Easy_Copier.Models;
using Easy_Copier.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.Xaml.Interactivity;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.System;

namespace Easy_Copier.Behaviors
{
    /// <summary>
    /// Restricts a <see cref="TextBox"/> to numeric input and implements SmartAdder's
    /// specialized keyboard navigation: Enter/Down/Plus/Minus move to the next row, Up moves
    /// to the previous row, and Delete removes the current row.
    /// </summary>
    public sealed class NumericTextBoxBehavior : Behavior<TextBox>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.PreviewKeyDown += AssociatedObject_PreviewKeyDown;
            AssociatedObject.BeforeTextChanging += AssociatedObject_BeforeTextChanging;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.PreviewKeyDown -= AssociatedObject_PreviewKeyDown;
            AssociatedObject.BeforeTextChanging -= AssociatedObject_BeforeTextChanging;
            base.OnDetaching();
        }

        private static void AssociatedObject_BeforeTextChanging(TextBox sender, TextBoxBeforeTextChangingEventArgs args)
        {
            args.Cancel = !IsValidNumberText(args.NewText);
        }

        private static bool IsValidNumberText(string text)
        {
            if (text.Length == 0)
            {
                return true;
            }

            if (text[0] == '-' && text.Length == 1)
            {
                return true;
            }

            int decimalPointCount = 0;
            for (int i = 0; i < text.Length; i++)
            {
                char character = text[i];
                if (character == '-' && i == 0)
                {
                    continue;
                }

                if (character == '.')
                {
                    decimalPointCount++;
                    if (decimalPointCount > 1)
                    {
                        return false;
                    }
                }
                else if (character is < '0' or > '9')
                {
                    return false;
                }
            }

            return true;
        }

        private void AssociatedObject_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
        {
            switch (e.Key)
            {
                case VirtualKey.Enter:
                case VirtualKey.Down:
                case VirtualKey.Add:
                    MoveFocus(forward: true);
                    e.Handled = true;
                    break;

                case VirtualKey.Subtract:
                    AssociatedObject.SelectAll();
                    MoveFocus(forward: true);
                    e.Handled = true;
                    break;

                case VirtualKey.Up:
                    MoveFocus(forward: false);
                    e.Handled = true;
                    break;

                case VirtualKey.Delete:
                    RemoveCurrentEntry();
                    e.Handled = true;
                    break;
            }
        }

        private void MoveFocus(bool forward)
        {
            ItemsControl? itemsControl = AssociatedObject.FindAscendant<ItemsControl>();
            if (itemsControl == null)
            {
                return;
            }

            List<TextBox> textBoxes = [.. itemsControl.FindDescendants().OfType<TextBox>()];
            int currentIndex = textBoxes.IndexOf(AssociatedObject);
            if (currentIndex < 0)
            {
                return;
            }

            int targetIndex = forward ? currentIndex + 1 : currentIndex - 1;
            if (forward && targetIndex >= textBoxes.Count && itemsControl.DataContext is SmartAdderViewModel viewModel)
            {
                viewModel.EnsureNextEntry();
                AssociatedObject.DispatcherQueue.TryEnqueue(() => FocusEntryAt(itemsControl, currentIndex + 1));
                return;
            }

            if (targetIndex >= 0 && targetIndex < textBoxes.Count)
            {
                FocusTextBox(textBoxes[targetIndex]);
            }
        }

        private void RemoveCurrentEntry()
        {
            ItemsControl? itemsControl = AssociatedObject.FindAscendant<ItemsControl>();
            if (itemsControl?.DataContext is not SmartAdderViewModel viewModel ||
                AssociatedObject.DataContext is not SmartAdderEntry entry)
            {
                return;
            }

            viewModel.RemoveEntry(entry);
            AssociatedObject.DispatcherQueue.TryEnqueue(() => FocusBottomEntry(itemsControl));
        }

        private static void FocusEntryAt(ItemsControl itemsControl, int index)
        {
            List<TextBox> textBoxes = [.. itemsControl.FindDescendants().OfType<TextBox>()];
            if (index >= 0 && index < textBoxes.Count)
            {
                FocusTextBox(textBoxes[index]);
            }
        }

        private static void FocusBottomEntry(ItemsControl itemsControl)
        {
            List<TextBox> textBoxes = [.. itemsControl.FindDescendants().OfType<TextBox>()];
            if (textBoxes.Count > 0)
            {
                FocusTextBox(textBoxes[^1]);
            }
        }

        private static void FocusTextBox(TextBox textBox)
        {
            _ = textBox.Focus(FocusState.Keyboard);
            textBox.Select(textBox.Text.Length, 0);
        }
    }
}
