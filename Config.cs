using System.Linq;
using System.Xml.Serialization;
using Torch;
using Torch.Collections;

namespace GridSplitNameKeeper
{
    public class Config: ViewModel
    {
        private bool _enable = true;
        private bool _rename = true;
        private bool _enableSplitCleaner;
        private int _splitThreshold = 0;
        private string _logFileName = "GridSplitNameKeeper-${shortdate}.log";


        public string LogFileName { get => _logFileName; set => SetValue(ref _logFileName, value); }

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

        [XmlIgnore] public MtObservableList<string> IgnoreBlockList { get; } = new MtObservableList<string>();

        [XmlArray(nameof(IgnoreBlockList))]
        [XmlArrayItem(nameof(IgnoreBlockList), ElementName = "Block Subtypes Only")]
        public string[] IgnoreBlocksSerial
        {
            get => IgnoreBlockList.ToArray();
            set
            {
                IgnoreBlockList.Clear();
                if (value == null) return;
                foreach (var k in value)
                    IgnoreBlockList.Add(k);
            }
        }

    }
}