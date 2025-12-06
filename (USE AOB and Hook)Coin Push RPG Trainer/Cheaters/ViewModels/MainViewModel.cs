using System.Collections.ObjectModel;
using System.ComponentModel;
using Cheaters.Core;

namespace Cheaters.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<Env.AOBInfo> _aobInfos;
        
        public ObservableCollection<Env.AOBInfo> AOBInfos
        {
            get => _aobInfos;
            set
            {
                _aobInfos = value;
                OnPropertyChanged(nameof(AOBInfos));
            }
        }

        public MainViewModel()
        {
            AOBInfos = new ObservableCollection<Env.AOBInfo>(Env.AOBInfos);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}