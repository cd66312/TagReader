using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TagReader.Models
{
    public class ModelNotify : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        public void OnPropertyChanged([CallerMemberName]string propertyName = null)
        {
            this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

