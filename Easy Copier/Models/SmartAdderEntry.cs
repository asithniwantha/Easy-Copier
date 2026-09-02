using CommunityToolkit.Mvvm.ComponentModel;

namespace Easy_Copier.Models
{
    public partial class SmartAdderEntry : NumberCell
    {
        public string Text
        {
            get => InputValue;
            set
            {
                if (InputValue != value)
                {
                    InputValue = value;
                    OnPropertyChanged(nameof(Text));
                }
            }
        }
    }
}
