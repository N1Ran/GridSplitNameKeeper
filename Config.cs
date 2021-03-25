using Torch;

namespace GridSplitNameKeeper
{
    public class Config: ViewModel
    {
        private bool _enable = true;
        private bool _rename = true;
        private bool _enableSplitCleaner;
        private int _splitThreshold = 0;


        public bool Enable
        {
            get => _enable;
            set
            {
                _enable = value;
                OnPropertyChanged();
            }
        }

        public bool KeepSplitName
        {
            get => _rename;
            set
            {
                _rename = value;
                OnPropertyChanged();
            }
        }
        public bool CleanSplits
        {
            get => _enableSplitCleaner;
            set
            {
                _enableSplitCleaner = value;
                OnPropertyChanged();
            }
        }

        public int SplitThreshold
        {
            get => _splitThreshold;
            set
            {
                _splitThreshold = value;
                OnPropertyChanged();
            }
        }
    }
}