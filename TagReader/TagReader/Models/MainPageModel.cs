using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TagReader.Models
{
    public class MainPageModel : ModelNotify
    {
        private int selectedReadStack;
        public int SelectedReadStack
        {
            get { return selectedReadStack; }
            set { selectedReadStack = value; this.OnPropertyChanged(); }
        }

        private string rFIDStatus = "N/A";
        public string RFIDStatus
        {
            get { return rFIDStatus; }
            set { rFIDStatus = value; this.OnPropertyChanged(); }
        }

        private bool _btnStartEnabled = true;
        public bool btnStartEnabled
        {
            get { return _btnStartEnabled; }
            set { _btnStartEnabled = value; this.OnPropertyChanged(); }
        }

        private bool _btnStopEnabled = false;
        public bool btnStopEnabled
        {
            get { return _btnStopEnabled; }
            set { _btnStopEnabled = value; this.OnPropertyChanged(); }
        }

        public class ReadStack : ModelNotify
        {
            private string count = null;
            public string Count
            {
                get { return count; }
                set { count = value; this.OnPropertyChanged(); }
            }

            private string duration = null;
            public string Duration
            {
                get { return duration; }
                set { duration = value; this.OnPropertyChanged(); }
            }

            private ObservableCollection<ReadStackTag> tags = new ObservableCollection<ReadStackTag>();
            public ObservableCollection<ReadStackTag> Tags
            {
                get { return tags; }
                set { tags = value; this.OnPropertyChanged(); }
            }

        }

        public class ReadStackTag : ModelNotify
        {
            private string tagID = null;
            public string TagID
            {
                get { return tagID; }
                set { tagID = value; this.OnPropertyChanged(); }
            }

            private string tagReads = null;
            public string TagReads
            {
                get { return tagReads; }
                set { tagReads = value; this.OnPropertyChanged(); }
            }

            public decimal Persistence = 0;

            private string antennas = null;
            public string Antennas
            {
                get { return antennas; }
                set { antennas = value; this.OnPropertyChanged(); }
            }
        }
    }
}
