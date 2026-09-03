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
    public sealed class NumericTextBoxBehavior : Behavior<TextBox>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.PreviewKeyDown += AssociatedObject_PreviewKeyDown;
            AssociatedObject.TextChanging += AssociatedObject_TextChanging;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.PreviewKeyDown -= AssociatedObject_PreviewKeyDown;
            AssociatedObject.TextChanging -= AssociatedObject_TextChanging;
            base.OnDetaching();
        }

        private void AssociatedObject_TextChanging(TextBox sender, TextBoxTextChangingEventArgs args)
        {
            string originalText = sender.Text;
            string sanitizedText = SanitizeText(originalText);

            if (originalText != sanitizedText)
            {
                int cursorPosition = sender.SelectionStart;
                sender.Text = sanitizedText;

                // Try to keep cursor in a reasonable place
                sender.SelectionStart = Math.Min(cursorPosition, sanitizedText.Length);
            }
        }

        private static string SanitizeText(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;

            var chars = new List<char>();
            bool hasDecimal = false;
            bool isFirst = true;

            foreach (char c in input)
            {
                if (c == '-' && isFirst)
                {
                    chars.Add(c);
                }
                else if (c == '.')
                {
                    if (!hasDecimal)
                    {
                        chars.Add(c);
                        hasDecimal = true;
                    }
                }
                else if (char.IsDigit(c))
                {
                    chars.Add(c);
                }
                isFirst = false;
            }

            return new string(chars.ToArray());
        }

        private void AssociatedObject_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
        {
            switch (e.Key)
            {
                case VirtualKey.Enter:
                case VirtualKey.Down:
                case VirtualKey.Add:
                case (VirtualKey)187:
                    MoveFocus(forward: true);
                    e.Handled = true;
                    break;

                case VirtualKey.Subtract:
                case (VirtualKey)189:
                    HandleMinusKey(e);
                    break;

                case VirtualKey.Up:
                    MoveFocus(forward: false);
                    e.Handled = true;
                    break;

                case VirtualKey.Delete:
                    if (string.IsNullOrEmpty(AssociatedObject.Text))
                    {
                        RemoveCurrentEntryAndFocusPrevious();
                        e.Handled = true;
                    }
                    break;
            }
        }

        private void HandleMinusKey(KeyRoutedEventArgs e)
        {
            ItemsControl? itemsControl = AssociatedObject.FindAscendant<ItemsControl>();
            if (itemsControl == null) return;

            List<TextBox> textBoxes = [.. itemsControl.FindDescendants().OfType<TextBox>()];
            int currentIndex = textBoxes.IndexOf(AssociatedObject);
            if (currentIndex < 0) return;

            bool isFirstCell = (currentIndex == 0);
            bool isEmpty = string.IsNullOrEmpty(AssociatedObject.Text);

            if (isEmpty)
            {
                if (isFirstCell)
                {
                    e.Handled = true;
                    return;
                }
                else
                {
                    // Not first cell, empty: insert minus, do not move focus.
                    AssociatedObject.Text = "-";
                    AssociatedObject.SelectionStart = 1;
                    e.Handled = true;
                }
            }
            else
            {
                // Not empty: move focus to next cell and insert minus
                if (currentIndex + 1 < textBoxes.Count)
                {
                    TextBox nextBox = textBoxes[currentIndex + 1];
                    AssociatedObject.DispatcherQueue.TryEnqueue(() =>
                    {
                        nextBox.Text = "-";
                        _ = nextBox.Focus(FocusState.Keyboard);
                        nextBox.SelectionStart = 1;
                    });
                }
                e.Handled = true;
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

            // Note: ViewModel automatically adds empty bottom cells when populated,
            // so if we are at the end, the next cell might already be there,
            // but if it isn't, we can't navigate forward. Let's just focus if within bounds.
            if (targetIndex >= 0 && targetIndex < textBoxes.Count)
            {
                TextBox targetBox = textBoxes[targetIndex];
                AssociatedObject.DispatcherQueue.TryEnqueue(() =>
                {
                    FocusTextBox(targetBox);
                });
            }
        }

        private void RemoveCurrentEntryAndFocusPrevious()
        {
            ItemsControl? itemsControl = AssociatedObject.FindAscendant<ItemsControl>();
            if (itemsControl?.DataContext is not SmartAdderViewModel viewModel ||
                AssociatedObject.DataContext is not NumberCell cell)
            {
                return;
            }

            List<TextBox> textBoxes = [.. itemsControl.FindDescendants().OfType<TextBox>()];
            int currentIndex = textBoxes.IndexOf(AssociatedObject);

            viewModel.DeleteCellCommand.Execute(cell);

            int targetIndex = currentIndex - 1;
            if (targetIndex >= 0)
            {
                AssociatedObject.DispatcherQueue.TryEnqueue(() =>
                {
                    // Find text boxes again after modification
                    var newTextBoxes = itemsControl.FindDescendants().OfType<TextBox>().ToList();
                    if (targetIndex < newTextBoxes.Count)
                    {
                        FocusTextBox(newTextBoxes[targetIndex]);
                    }
                });
            }
        }

        private static void FocusTextBox(TextBox textBox)
        {
            _ = textBox.Focus(FocusState.Keyboard);
            textBox.Select(textBox.Text.Length, 0);
        }
    }
}
