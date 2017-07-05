using System.Collections.Generic;
using System;
using System.Linq;
using System.Threading.Tasks;
using Template10.Mvvm;
using Template10.Services.NavigationService;
using Windows.UI.Xaml.Navigation;
using TagReader.Models;
using TagReader.Services;
using System.Collections.ObjectModel;

namespace TagReader.ViewModels
{
    public class MainPageViewModel : ViewModelBase
    {
        #region Vars
        public MainPageModel PageModel { get; set; } = new MainPageModel();
        public ObservableCollection<MainPageModel.ReadStack> ReadStack { get; set; } = new ObservableCollection<MainPageModel.ReadStack>();
        public ObservableCollection<MainPageModel.ReadStackTag> ReadStackTags { get; set; } = new ObservableCollection<MainPageModel.ReadStackTag>();
        
        private bool reading = false;
        private MainPageModel.ReadStack readStackInProgress = null;
        public List<string> ReaderOutput { get; set; } = new List<string>();
        #endregion

        public MainPageViewModel()
        {
            // hook up with our reader
            ReaderComs.LineReadEvent += ReaderComs_LineReadEvent;
            ReaderComs.Initialize();

        }


        #region EventHandlers

        private async void ReaderComs_LineReadEvent(object sender, string e)
        {

            // checking first to see if these were connection messages
            if (e == "ReaderConnected")
            {
                PageModel.RFIDStatus = "Ready!";
            }
            else if (e == "ReaderDisConnected")
            {
                PageModel.RFIDStatus = "Error";
            }
            else if(reading)
            {
                readerDataRecord(e);
            }

        }
        

        #endregion


        #region Methods

        private void readerDataRecord(string s)
        {
            // log it to the GUI
            if (s.Contains("tags>")) // this is the end of a readstackobject
            {
                // ok, irrelevant datas, but need to compile what we have captured as a readstackobject
                ReadStack.Add(readStackInProgress);
                // reset our readstackstrings
                readStackInProgress = null;
            }
            else
            {
                if (s.Contains("tag")) // gotta make sure this isn't irrelavent datas
                {
                    if (s.Contains("<tags list")) // this is the start of a new readstackobject
                    {
                        // create our new readstackinprogress object
                        readStackInProgress = new MainPageModel.ReadStack();

                        try
                        {
                            var datas = s.Split('"');
                            readStackInProgress.Count = datas[5];
                            readStackInProgress.Duration = datas[9];
                        }
                        catch
                        {
                            // this is just here for debugging purposes, i see no reason that it should ever catch, but, permanent breakpoint here in case
                            if (readStackInProgress.Count == null)
                            {
                                readStackInProgress.Count = "Error";
                                readStackInProgress.Duration = "Error";
                            }
                            else { readStackInProgress.Duration = "Error"; }
                        }
                    }
                    else // ok, we know this is relevant datas, it must be a tag summary
                    {
                        // double check though, even though this statement should never fail
                        if (s.Contains("tag oid"))
                        {
                            var tag = new MainPageModel.ReadStackTag();
                            try
                            {
                                var datas = s.Split('"');
                                tag.TagID = datas[1];
                                tag.TagReads = datas[3];
                                tag.Antennas = datas[9];
                            }
                            catch
                            {
                                // again, should never get here, but if so, lets take care of it
                                if (tag.TagID == null)
                                {
                                    tag.TagID = "Error";
                                    tag.TagReads = "Error";
                                    tag.Antennas = "Error";
                                }
                                else if (tag.TagReads == null)
                                {
                                    tag.TagReads = "Error";
                                    tag.Antennas = "Error";
                                }
                                else
                                {
                                    tag.Antennas = "Error";
                                }
                            }
                            readStackInProgress.Tags.Add(tag);
                        }
                    }
                }
            }
        }
        

        #endregion


        #region ButtonClicks

        public void btnStart_Click()
        {
                PageModel.btnStopEnabled = true;
                PageModel.btnStartEnabled = false;
                PageModel.RFIDStatus = "Reading...";
                readStackInProgress = null;
                reading = true;
        }

        public void btnStop_Click()
        {
            PageModel.btnStopEnabled = false;
            PageModel.btnStartEnabled = true;
            PageModel.RFIDStatus = "Stand By";
            reading = false;
        }

        public void btnReset_Click()
        {
            ReadStack.Clear();
            ReadStackTags.Clear();
        }

        private int index = 0;
        public void btnLineFeed_Click()
        {
            switch (index)
            {
                case 0:
                    readerDataRecord("<tags list=\"READ\" pwr=\"300\" cnt=\"3\" rpt=\"0\" dur=\"750\">");
                    index++;
                    break;
                case 1:
                    readerDataRecord("<tag oid=\"0x0000000000000000000193C7\" cnt=\"20\" mSec=\"534296\" mSX=\"584945\" ant=\"---1\" seq=\"3\"/>");
                    index++;
                    break;
                case 2:
                    readerDataRecord("<tag oid=\"0x000000000000000000103154\" cnt=\"7\" mSec=\"534954\" mSX=\"584764\" ant=\"---1\" seq=\"4\"/>");
                    index++;
                    break;
                case 3:
                    readerDataRecord("<tag oid=\"0x4444AD000013100608342202\" cnt=\"3\" mSec=\"584545\" mSX=\"584712\" ant=\"---1\" seq=\"5\"/>");
                    index++;
                    break;
                default:
                    readerDataRecord("</tags>");
                    index=0;
                    break;
            }
           
        }

        public void ReadStack_SelectionChanged()
        {
            //TODO try catch needs to go, debugging atm
            try
            {
                ReadStackTags.Clear();
                foreach (MainPageModel.ReadStackTag tag in ReadStack[PageModel.SelectedReadStack].Tags)
                {
                    ReadStackTags.Add(tag);
                }
            }
            catch
            {

            }
        }
        #endregion
    }
}

