using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Media.Imaging;
using Un4seen.Bass;
using Un4seen.Bass.AddOn.Tags;
using TagLib;
using Lyric;
using LyricShowTest;
using Un4seen.Bass.AddOn.Cd;
using Un4seen.Bass.Misc;
using System.Windows.Media;
using System.Drawing;
using System.Windows;
using HappyMaster.ViewModel;
using System.Collections.ObjectModel;
using System.Windows.Media.Animation;

namespace FramelessWPF
{
    public partial class MainWindow
    {
        
        private readonly Storyboard _storyboard;

        public MainWindow()
        {
            InitializeComponent();
            Storyboard sb = new Storyboard();
            sb.FillBehavior = FillBehavior.HoldEnd;

            var opacityAnimation = new DoubleAnimation(0, 1, new Duration(TimeSpan.FromMilliseconds(2000)));
            Storyboard.SetTargetProperty(opacityAnimation, new PropertyPath(nameof(ImageAlbumArtBig.Opacity)));
            var translateAnimation = new DoubleAnimation(-100, 0, new Duration(TimeSpan.FromMilliseconds(300)));
            Storyboard.SetTargetProperty(translateAnimation, new PropertyPath($"{nameof(ImageAlbumArtBig.RenderTransform)}.{nameof(TranslateTransform.X)}"));

            sb.Children.Add(translateAnimation);
            sb.Children.Add(opacityAnimation);

            _storyboard = sb;
        }


        private System.Windows.Threading.DispatcherTimer _maintimer = null;
        private System.Windows.Threading.DispatcherTimer _lyrictimer = null;
        private int _stream = 0;
        private string _filename = string.Empty;
        private STREAMPROC _myStreamCreate;
        private BASS_FILEPROCS _myStreamCreateUser;
        private FileStream _fs;
        LyricShow lyricShow;
        GetLyricAndLyricTime getLT;


        private int MyFileProc(int handle, IntPtr buffer, int length, IntPtr user)
        {
            byte[] data = new byte[length];
            int bytesread = _fs.Read(data, 0, length);        
            System.Runtime.InteropServices.Marshal.Copy(data, 0, buffer, bytesread);
            if (bytesread < length)
            {
                bytesread |= (int)BASSStreamProc.BASS_STREAMPROC_END; 
                _fs.Close();
            }
            return bytesread;
        }

        private void MyFileProcUserClose(IntPtr user)
        {
            if (_fs == null)
                return;
            _fs.Close();
        }

        private long MyFileProcUserLength(IntPtr user)
        {
            if (_fs == null)
                return 0L;
            long len = _fs.Length;
            return len;
        }

        private int MyFileProcUserRead(IntPtr buffer, int length, IntPtr user)
        {
            if (_fs == null)
                return 0;
            try
            {
                byte[] data = new byte[length];
                int bytesread = _fs.Read(data, 0, length);      
                Marshal.Copy(data, 0, buffer, bytesread);

                return bytesread;
            }
            catch { return 0; }
        }

        private bool MyFileProcUserSeek(long offset, IntPtr user)
        {
            if (_fs == null)
                return false;
            try
            {
                long pos = _fs.Seek(offset, SeekOrigin.Begin);
                return true;
            }
            catch
            {
                return false;
            }
        }
        private void Window_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {

            System.IntPtr hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            
            BassNet.Registration("zhaiyuhanx@hotmail.com", "2X3931422312422");
            if (Bass.BASS_Init(-1, 44100, BASSInit.BASS_DEVICE_DEFAULT, hwnd))
            {
                _myStreamCreate = new STREAMPROC(MyFileProc);

                _myStreamCreateUser = new BASS_FILEPROCS(
                    new FILECLOSEPROC(MyFileProcUserClose),
                    new FILELENPROC(MyFileProcUserLength),
                    new FILEREADPROC(MyFileProcUserRead),
                    new FILESEEKPROC(MyFileProcUserSeek));
                MainVolume.Value = Bass.BASS_GetVolume() * 100;
                if (Bass.BASS_GetVolume() >= 80)
                {
                    packicon_volumestate.Kind = MaterialDesignThemes.Wpf.PackIconKind.VolumeHigh;
                }
                _maintimer = new System.Windows.Threading.DispatcherTimer()
                {
                    Interval = TimeSpan.FromMilliseconds(500)
                };
                _maintimer.Tick += new EventHandler(update_elapsedtime);
               
                _lyrictimer = new System.Windows.Threading.DispatcherTimer()
                {
                    Interval = TimeSpan.FromMilliseconds(500)
                };
                _lyrictimer.Tick += new EventHandler(_lyrictimer_Tick);

                lyricShow = new LyricShow(CanvasLyric, StackPanelCommonLyric, CanvasFocusLyric, TBFocusLyricBack, CanvasFocusLyricFore, TBFocusLyricFore);
                getLT = new GetLyricAndLyricTime();
            }
            else
            {
                System.Windows.MessageBox.Show(this, "Bass_Init error!");
            }
        }

        private void _lyrictimer_Tick(object sender, EventArgs e)
        {
            LyricShow.refreshLyricShow(Bass.BASS_ChannelBytes2Seconds(_stream, Bass.BASS_ChannelGetPosition(_stream)));

        }
        public string m_totaltime;
        private void update_elapsedtime(object sender,EventArgs e)
        {
            if (Bass.BASS_ChannelIsActive(_stream) == BASSActive.BASS_ACTIVE_PLAYING)
            {
                double totaltime = Bass.BASS_ChannelBytes2Seconds(_stream, Bass.BASS_ChannelGetLength(_stream));
                double elapsedtime = Bass.BASS_ChannelBytes2Seconds(_stream, Bass.BASS_ChannelGetPosition(_stream));
                double remainingtime = totaltime - elapsedtime;
                LabelTime.Content = String.Format(Utils.FixTimespan(elapsedtime, "MMSS"));
                LabelLeftTime.Content = " -" + String.Format(Utils.FixTimespan(remainingtime, "MMSS"));
                MainProgressBar.Value = (int)Bass.BASS_ChannelBytes2Seconds(_stream, Bass.BASS_ChannelGetPosition(_stream));
            }else if (Bass.BASS_ChannelIsActive(_stream) == BASSActive.BASS_ACTIVE_STOPPED)
            {
                _maintimer.Stop();
                iconPlay.Kind = MaterialDesignThemes.Wpf.PackIconKind.Play;
                Bass.BASS_ChannelStop(_stream);
                LabelTime.Content =m_totaltime;
                LabelLeftTime.Content = String.Format(Utils.FixTimespan(00000, "MMSS"));
                System.Diagnostics.Debug.Write("stoped");
                return;
            }          
        }

        void Play(String m_filename)
        {
            switch (Bass.BASS_ChannelIsActive(_stream))
            {
                case BASSActive.BASS_ACTIVE_STOPPED:
                    Bass.BASS_StreamFree(_stream);
                    if (!string.IsNullOrEmpty(m_filename))
                    {
                        _fs = System.IO.File.OpenRead(m_filename);
                        _stream = Bass.BASS_StreamCreateFileUser(BASSStreamSystem.STREAMFILE_NOBUFFER, BASSFlag.BASS_SAMPLE_FLOAT | BASSFlag.BASS_STREAM_PRESCAN | BASSFlag.BASS_STREAM_AUTOFREE, _myStreamCreateUser, IntPtr.Zero);
                        if (_stream != 0)
                        {
                            m_totaltime = String.Format(Utils.FixTimespan(Bass.BASS_ChannelBytes2Seconds(_stream, Bass.BASS_ChannelGetLength(_stream)), "MMSS"));
                            Bass.BASS_ChannelPlay(_stream, true);
                            MainProgressBar.Maximum = (int)Bass.BASS_ChannelBytes2Seconds(_stream, Bass.BASS_ChannelGetLength(_stream));
                            _maintimer.Start();
                            iconPlay.Kind= MaterialDesignThemes.Wpf.PackIconKind.Pause;
                            //
                            
                        }
                    }
                    break;
                case BASSActive.BASS_ACTIVE_PLAYING:
                    Bass.BASS_ChannelPause(_stream);
                    iconPlay.Kind= MaterialDesignThemes.Wpf.PackIconKind.Play;
                    break;
                case BASSActive.BASS_ACTIVE_PAUSED:
                    Bass.BASS_ChannelPlay(_stream, false);
                    iconPlay.Kind = MaterialDesignThemes.Wpf.PackIconKind.Pause;
                    break;
            }
        }

        public class PlayList
        {
            public string Title { get; set; }
            public string Album { get; set; }
            public string Artist { get; set; }
            public string TotalTime { get; set; }
        }
        private void GetTagInformation(String m_filename)
        {
            //get information
            TagLib.Tag tag = TagLib.File.Create(m_filename).Tag;
            if (tag.Pictures.Length > 0)
            {
                using (MemoryStream albumArtworkMemStream = new MemoryStream(tag.Pictures[0].Data.Data))
                {
                    try
                    {
                        BitmapImage albumImage = new BitmapImage();
                        albumImage.BeginInit();
                        albumImage.CacheOption = BitmapCacheOption.OnLoad;
                        albumImage.StreamSource = albumArtworkMemStream;
                        albumImage.DecodePixelHeight = 50;
                        albumImage.DecodePixelWidth = 50;
                        albumImage.EndInit();
                        ImageAlbumArt.Source = albumImage.Clone();
                        using (MemoryStream albumArtworkMemStream2 = new MemoryStream(tag.Pictures[0].Data.Data))
                        {
                            BitmapImage albumImage2 = new BitmapImage();
                            albumImage2.BeginInit();
                            albumImage2.CacheOption = BitmapCacheOption.OnLoad;
                            albumImage2.StreamSource = albumArtworkMemStream2;
                            albumImage2.DecodePixelHeight = 200;
                            albumImage2.DecodePixelWidth = 200;
                            albumImage2.EndInit();
                            ImageAlbumArtBig.Source = albumImage2;
                            albumArtworkMemStream2.Close();
                        }
                        _storyboard.Begin(ImageAlbumArtBig);
                    }
                    catch (NotSupportedException)
                    {
                        ImageAlbumArt.Source = null;
                    }
                    albumArtworkMemStream.Close();
                }
            }
            else
            {
                ImageAlbumArt.Source = null;
            }

            string blankspace = "    ";
            TAG_INFO info = new TAG_INFO(m_filename);
            if (BassTags.BASS_TAG_GetFromFile(_stream, info))
            {
                LabelTitle.Content = info.title;
                LabelArtist.Content = info.artist;
                LabelTitleForCard.Text = info.title;
                LabelAlbumForCard.Text = info.album;
                System.Text.StringBuilder albumtext = new System.Text.StringBuilder();
                albumtext.Append(info.artist);
                albumtext.Append(" ");
                albumtext.Append(info.year);
                LabelArtistForCard.Text = albumtext.ToString();
                albumtext.Remove(0, albumtext.Length);
                albumtext.Append("   比特率");
                albumtext.Append(blankspace);
                albumtext.Append(info.bitrate);
                albumtext.Append("kbps");
                ListViewItemBit.Content = albumtext;
                System.Text.StringBuilder n_rating = new System.Text.StringBuilder();
                n_rating.Append("   采样率");
                n_rating.Append(blankspace);
                n_rating.Append("44.100");
                ListViewItemRate.Content = n_rating;
                
            }
            else
            {
                var tfile = TagLib.File.Create(m_filename);
                LabelTitle.Content = tfile.Tag.Title;
                LabelArtist.Content = tfile.Tag.Performers[0];
                LabelTitleForCard.Text = tfile.Tag.Title;
                LabelAlbumForCard.Text = tfile.Tag.Album;
                System.Text.StringBuilder albumtext = new System.Text.StringBuilder();
                albumtext.Append(tfile.Tag.Performers[0]);
                albumtext.Append(" ");
                albumtext.Append(tfile.Tag.Year);
                LabelArtistForCard.Text = tfile.Tag.Performers[0];
                albumtext.Remove(0, albumtext.Length);
                albumtext.Append("   比特率");
                albumtext.Append(blankspace);
                albumtext.Append(tfile.Properties.AudioBitrate);
                albumtext.Append("kbps");
                ListViewItemBit.Content = albumtext;
                System.Text.StringBuilder n_rating = new System.Text.StringBuilder();
                n_rating.Append("   采样率");
                n_rating.Append(blankspace);
                n_rating.Append(String.Format("{0:##,###}", tfile.Properties.AudioSampleRate));
                ListViewItemRate.Content = n_rating;
                PlayList memberData = new PlayList();
                memberData.Title = tfile.Tag.Title;
                memberData.Artist = tfile.Tag.Performers[0];
                memberData.Album = tfile.Tag.Album;
     
                this.tb1.Items.Add(memberData); 
            }
        
            FileInfo _fileinfo = new FileInfo(info.filename);
            System.Text.StringBuilder n_writetime = new System.Text.StringBuilder();
            n_writetime.Append("修改时间");
            n_writetime.Append(blankspace);
            n_writetime.Append(_fileinfo.CreationTime);
            ListViewItemWriteTime.Content = n_writetime;
            System.Text.StringBuilder n_addtime = new System.Text.StringBuilder();
            n_addtime.Append("添加时间");
            n_addtime.Append(blankspace);
            n_addtime.Append(_fileinfo.LastWriteTime);
            ListViewItemAddTime.Content = n_addtime;
            System.Text.StringBuilder n_filesize = new System.Text.StringBuilder();
            string _filesize = _fileinfo.Length / 1024 / 1024 + "." + Math.Round((double)(_fileinfo.Length / 1024 % 1024 / 10), 2) + "MB";
            n_filesize.Append("文件大小");
            n_filesize.Append(blankspace);
            n_filesize.Append(_filesize);
            ListViewItemFileSize.Content = n_filesize;
            System.Text.StringBuilder n_filepath = new System.Text.StringBuilder();
            n_filepath.Append("文件位置");
            n_filepath.Append(blankspace);
            n_filepath.Append(_fileinfo.DirectoryName);
            ListViewItemFilePath.Content = n_filepath;
        }
        private void MenuItem_OpenFile_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            OpenFileDialog _openfile = new OpenFileDialog()
            {
                //dialog.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
                Title = "打开音频文件",
                //Filter = "音乐文件 (*.mp3)|*.mp3*|所有文件 (*.*)|*.*",
                RestoreDirectory = true
            };
            if (_openfile.ShowDialog() == true) 
            {
                if (System.IO.File.Exists(_openfile.FileName)) 
                {
                    Bass.BASS_ChannelStop(_stream);
                    Bass.BASS_StreamFree(_stream);                
                    _filename = _openfile.FileName;
                    Console.WriteLine(_filename);
                    Play(_filename);
                    GetTagInformation(_filename);

                }
                else
                {
                    _filename = string.Empty;
                }
            }
        }

        private void BtnPlay_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (_stream != 0)
            {
                Play(_filename);
            }
            else
            {
                //btnPlay.Command = MaterialDesignThemes.Wpf.DialogHost.OpenDialogCommand;
                dig_ifCreatStream.IsOpen = true;
            }
            
        }

        private void MenuItem_Exit_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            this.Close();
        }

        private void MainVolume_ValueChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<double> e)
        {
            Bass.BASS_SetVolume((float)(MainVolume.Value / 100));
            ChangeVolumeIcon();
        }

        private void MainProgressBar_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (_stream != 0) 
            Bass.BASS_ChannelSetPosition(_stream, (double)MainProgressBar.Value);    
        }

        private void MainProgressBar_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            if (_stream != 0) 
            _maintimer.Start();
            Bass.BASS_ChannelSetPosition(_stream, (double)MainProgressBar.Value);
        }

        private void MainProgressBar_DragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
        {
            if (_stream != 0) 
            _maintimer.Stop();
        }

        private void Window_SizeChanged(object sender, System.Windows.SizeChangedEventArgs e)
        {

        }

        private void BtnPreviewForTaskBar_Click(object sender, EventArgs e)
        {
            
        }

        private void BtnPlayForTaskBar_Click(object sender, EventArgs e)
        {
            Play(_filename);
        }

        private void MenuItem_About_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            View.AboutView _aboutview = new View.AboutView()
            {
                ShowInTaskbar = false,
                Owner = this,
                WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner
            };
            _aboutview.ShowDialog();
        }

        private void BtnOpenLyric_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            OpenFileDialog _openfile = new OpenFileDialog()
            {
                Title = "打开歌词文件",
                Filter = "歌词文件(*.lrc)|*.lrc",
                RestoreDirectory = true
            };
            if (_openfile.ShowDialog(this) == true)
            {
                getLT.getLyricAndLyricTimeByLyricPath(_openfile.FileName);
                LyricShow.initializeLyricUI(getLT.LyricAndTimeDictionary);//解析歌词->得到歌词时间和歌词        
                LyricShow.setCommonLyricFontFamily(new System.Windows.Media.FontFamily("微软雅黑"));
                LyricShow.setCommonLyricFontSize(16);
                _lyrictimer.Start();
            }
        }

        private void MenuItem_OpenCD_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            OpenFileDialog _openfile = new OpenFileDialog()
            {
                Title = "打开CD文件",
                Filter = "CDA文件(*.cda)|*.cda",
                RestoreDirectory = true
            };
            if (_openfile.ShowDialog() == true)
            {
                if (System.IO.File.Exists(_openfile.FileName))
                {
                    Bass.BASS_ChannelStop(_stream);
                    Bass.BASS_StreamFree(_stream);
                    _filename = _openfile.FileName;
                    BassCd.BASS_CD_StreamCreateFile(_filename, BASSFlag.BASS_SAMPLE_FLOAT);
                    Bass.BASS_ChannelPlay(BassCd.BASS_CD_StreamCreateFile(_filename, BASSFlag.BASS_SAMPLE_FLOAT), false);
                }
            }
         }

        /*private void ImageAlbumArtBG_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            System.Windows.Media.ScaleTransform scaleTransform = new System.Windows.Media.ScaleTransform()
            {
                CenterX = ImageAlbumArtBG.Width/2,
                CenterY = ImageAlbumArtBG.Height / 2,
                ScaleX = 1.1,         
                ScaleY = 1.1           
            };
            ImageAlbumArtBG.RenderTransform = scaleTransform;
        }

        private void ImageAlbumArtBG_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            System.Windows.Media.ScaleTransform scaleTransform = new System.Windows.Media.ScaleTransform()
            {
                ScaleX = 1,              
                ScaleY = 1             
            };
            ImageAlbumArtBG.RenderTransform = scaleTransform;
        }*/

        private void MenuItem_MaxView_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            switch(WindowState)
            {
                case System.Windows.WindowState.Maximized:
                    WindowState = System.Windows.WindowState.Normal;
                    MenuItem_MaxView.Header = "最大化";
                    break;
                case System.Windows.WindowState.Normal:
                    WindowState = System.Windows.WindowState.Maximized;
                    MenuItem_MaxView.Header = "向下还原 ";
                    break;
                default:
                    break;
            }
        }

        private void MenuItem_MinView_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            WindowState = System.Windows.WindowState.Minimized;
        }

        private void MenuItem_CloseView_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Close();
        }

        private void DialogHost_DialogClosing(object sender, MaterialDesignThemes.Wpf.DialogClosingEventArgs eventArgs)
        {
            if (!Equals(eventArgs.Parameter, true)) return;

            if (!Equals(eventArgs.Parameter, false)) MenuItem_OpenFile_Click(null,null);
        }

        private void MenuItem_VolumeDown_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            MainVolume.Value -= 10;
            MainVolume_ValueChanged(null, null);
        }

        private void MenuItem_VolumeUp_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            MainVolume.Value += 10;
            MainVolume_ValueChanged(null, null);
        }

        private void ImageAlbumArtBig_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            System.Console.WriteLine("emit");
            
        }

        public void ChangeVolumeIcon()
        {
            if (Bass.BASS_GetVolume() * 100 == 0)
            {
                packicon_volumestate.Kind = MaterialDesignThemes.Wpf.PackIconKind.VolumeOff;
            }
            else if (Bass.BASS_GetVolume() * 100 >= 80)
            {
                packicon_volumestate.Kind = MaterialDesignThemes.Wpf.PackIconKind.VolumeHigh;
            }
            else if (Bass.BASS_GetVolume() * 100 >= 40 && Bass.BASS_GetVolume() * 100 <= 80)
            {
                packicon_volumestate.Kind = MaterialDesignThemes.Wpf.PackIconKind.VolumeMedium;
            }
            else if (Bass.BASS_GetVolume() * 100 < 40)
            {
                packicon_volumestate.Kind = MaterialDesignThemes.Wpf.PackIconKind.VolumeLow;
            }
        }
        public float oldvolume = Bass.BASS_GetVolume();
        private void BtnchangeVolume_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (Bass.BASS_GetVolume() != 0)
            {
                oldvolume = Bass.BASS_GetVolume();
                Bass.BASS_SetVolume(0);
            }else if (Bass.BASS_GetVolume() == 0)
            {
                Bass.BASS_SetVolume(oldvolume);
            }
            //MainVolume_ValueChanged(null, null);
            ChangeVolumeIcon();
            MainVolume.Value = Bass.BASS_GetVolume() * 100;
        }

        private void MenuItem_Play_Pause_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            BtnPlay_Click(null, null);
        }

        private void MenuItem_PlayFromStart_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Bass.BASS_ChannelPlay(_stream, true);
        }

        private void MenuItem_StopPlay_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Bass.BASS_ChannelStop(_stream);
            Bass.BASS_StreamFree(_stream);
            ImageAlbumArt.Source = null;
            ImageAlbumArtBig.Source = null;
            LabelAlbumForCard.Text = null;
            LabelArtistForCard.Text = null;
            LabelLeftTime.Content = null;
            LabelTime.Content = null;
            LabelTitle.Content = null;
            LabelTitle.Content = null;
            LabelTitleForCard.Text = null;
            MainProgressBar.Value = 0;
            _maintimer.Stop();
            _lyrictimer.Stop();
            _stream = 0;
            iconPlay.Kind = MaterialDesignThemes.Wpf.PackIconKind.Play;
        }

        private void BtnOpenLyricEX_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            BtnOpenLyric_Click(null, null);
        }

        private void MenuItem_OpenSettingsView_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            View.SettingsView _settingsview = new View.SettingsView()
            {
                Owner = this,
                WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner,
                ShowInTaskbar = false
            };
            _settingsview.ShowDialog();
        }

        private void BtnSaveInfo_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            SaveFileDialog _savefiledialog = new SaveFileDialog()
            {
                Filter = "txt files(*.txt)|*.txt",
                FileName = LabelTitle.Content.ToString(),
                DefaultExt = "txt",
                AddExtension = true,
                FilterIndex = 2,
                RestoreDirectory = true
            };
            bool? result = _savefiledialog.ShowDialog();
            if (result == true)
            {
                string filename = _savefiledialog.FileName;
                if (!System.IO.File.Exists(filename))
                {
                    using (StreamWriter sw = System.IO.File.CreateText(filename))
                    {
                        sw.WriteLine(LabelTitleForCard.Text);
                        sw.WriteLine(LabelArtistForCard.Text);
                        sw.WriteLine(LabelAlbumForCard.Text);
                        sw.WriteLine(ListViewItemBit.Content); 
                        sw.WriteLine(ListViewItemRate.Content);
                        sw.WriteLine(ListViewItemFileSize.Content);
                        sw.WriteLine(ListViewItemFilePath.Content);
                        sw.WriteLine(ListViewItemAddTime.Content);
                        sw.WriteLine(ListViewItemWriteTime.Content);
                    }
                }
            }

        }

        private void MenuItem_Commander_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            dig_Commander.IsOpen = true;
        }

        private void dig_Commander_DialogClosing(object sender, MaterialDesignThemes.Wpf.DialogClosingEventArgs eventArgs)
        {
            if (!Equals(eventArgs.Parameter, true)) return;

            if (!string.IsNullOrWhiteSpace(TextBoxInputCommand.Text))
            {
                Console.WriteLine(TextBoxInputCommand.Text);               
                string result = string.Empty;
                string removeblank = TextBoxInputCommand.Text.Trim().ToLower();
                string[] split = removeblank.Split(new char[2] { '(', ')' });
                switch (split[0])
                {
                    case "play":
                        if (split[1] == null)
                        {
                            BtnPlay_Click(null, null);                           
                        }else if (split[1] != null)
                        {
                            Play(split[1].Clone().ToString());
                            GetTagInformation(split[1].Clone().ToString());
                        }                        
                        break;
                    case "pause":
                        MenuItem_Play_Pause_Click(null, null);
                        break;
                    case "stop":
                        MenuItem_StopPlay_Click(null, null);
                        break;
                    case "random":
                        MenuItem_PlayFromStart_Click(null, null);
                        break;
                    case "addfile":
                        break;
                    case "openfile":
                        MenuItem_OpenFile_Click(null, null);
                        break;
                    case "volumeup":
                        try
                        {
                            System.Text.RegularExpressions.Regex _regex = new System.Text.RegularExpressions.Regex(@"^[0-9]\d*$");
                            if (_regex.IsMatch(split[1]))
                            {
                                MainVolume.Value += Convert.ToInt32(split[1]);
                                MainVolume_ValueChanged(null, null);
                            }
                            else
                            {
                                int _volume = Convert.ToInt32(split[1]);
                                MainVolume.Value += Convert.ToInt32(split[1]);
                                MainVolume_ValueChanged(null, null);
                            }
                        }
                        catch (NotSupportedException)
                        {
                            dig_Error.IsOpen = true;
                            return;
                        }
                        break;
                    case "volumedown":
                        try
                        {
                            System.Text.RegularExpressions.Regex _regex = new System.Text.RegularExpressions.Regex(@"^[0-9]\d*$");
                            if (_regex.IsMatch(split[1]))
                            {
                                MainVolume.Value -= Convert.ToInt32(split[1]);
                                MainVolume_ValueChanged(null, null);
                            }
                            else
                            {
                                int _volume = Convert.ToInt32(split[1]);
                                MainVolume.Value -= Convert.ToInt32(split[1]);
                                MainVolume_ValueChanged(null, null);
                            }
                        }
                        catch (NotSupportedException)
                        {
                            dig_Error.IsOpen = true;
                            return;
                        }
                        break;
                    case "setvolume":
                        MainVolume.Value = Convert.ToDouble(split[1]);
                        MainVolume_ValueChanged(null, null);
                        break;
                    case "openaboutview":
                        MenuItem_About_Click(null, null);
                        break;
                    case "exitapp":
                        Close();
                        break;
                    default:
                        dig_Error.IsOpen = true;
                        break;
                }

            }
                
        }

        private void Dig_Error_DialogClosing(object sender, MaterialDesignThemes.Wpf.DialogClosingEventArgs eventArgs)
        {
            return;
        }

        private void MenuItem_ToggleTopMost_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (MenuItem_ToggleTopMost.IsChecked) this.Topmost = true;
            if (!MenuItem_ToggleTopMost.IsChecked) this.Topmost = false;
        }

        private void MenuItem_CopyFile_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (!String.IsNullOrEmpty(_filename)) 
            {
                string[] file = new string[1];
                file[0] = _filename;
                System.Windows.DataObject dataObject = new System.Windows.DataObject();
                dataObject.SetData(System.Windows.DataFormats.FileDrop, file);
                System.Windows.Clipboard.SetDataObject(dataObject, true);
            }
        }

        private void TextBox_Search_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (TextBox_Search.Text != null)
            {
                Btn_Search.Visibility = System.Windows.Visibility.Visible ;
            }else if(TextBox_Search.Text==string.Empty) {
                Console.WriteLine("emit");
                Btn_Search.Visibility = System.Windows.Visibility.Hidden;
            }
        }

        private void Btn_Search_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            TextBox_Search.Text = null;
            Btn_Search.Visibility = System.Windows.Visibility.Hidden;
        }

        private void Btn_Search_GotFocus(object sender, System.Windows.RoutedEventArgs e)
        {
            if (TextBox_Search.Text != null) Btn_Search.Visibility = System.Windows.Visibility.Visible;
        }

        private void Btn_Search_LostFocus(object sender, System.Windows.RoutedEventArgs e)
        {
            Btn_Search.Visibility = System.Windows.Visibility.Hidden;
        }

        private void TextBox_Search_TextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            if (TextBox_Search.Text == null) { Btn_Search.Visibility = System.Windows.Visibility.Hidden;  }
        }

        private void MainMenu_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }
        int i = 0;
        private void MainMenu_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            i += 1;
            System.Windows.Threading.DispatcherTimer timer = new System.Windows.Threading.DispatcherTimer();
            timer.Interval = new TimeSpan(0, 0, 0, 0, 300);
            timer.Tick += (s, e1) => { timer.IsEnabled = false; i = 0; };
            timer.IsEnabled = true;

            if (i % 2 == 0)
            {
                timer.IsEnabled = false;
                i = 0;
                this.WindowState = this.WindowState == WindowState.Maximized ?
                              WindowState.Normal : WindowState.Maximized;
            }
        }





        /*
private Visuals _vis = new Visuals(); // visuals class instance
private int specIdx = 15;
private int voicePrintIdx = 0;
private void DrawSpectrum(System.Windows.Controls.Image pictureBoxSpectrum, int stream)
{
switch (specIdx)
{
// normal spectrum (width = resolution)
case 0:
pictureBoxSpectrum.Source = _vis.CreateSpectrum(stream, pictureBoxSpectrum.Width, pictureBoxSpectrum.Height, Color.Lime, Color.Red, Color.Transparent, false, false, false);
break;
// normal spectrum (full resolution)
case 1:
pictureBoxSpectrum.Source = _vis.CreateSpectrum(stream, pictureBoxSpectrum.Width, pictureBoxSpectrum.Height, Color.SteelBlue, Color.Pink, Color.Transparent, false, true, true);
break;
// line spectrum (width = resolution)
case 2:
pictureBoxSpectrum.Source = _vis.CreateSpectrumLine(stream, pictureBoxSpectrum.Width, pictureBoxSpectrum.Height, Color.Lime, Color.Red, Color.Transparent, 2, 2, false, false, false);
break;
// line spectrum (full resolution)
case 3:
pictureBoxSpectrum.Source = _vis.CreateSpectrumLine(stream, pictureBoxSpectrum.Width, pictureBoxSpectrum.Height, Color.SteelBlue, Color.Pink, Color.Transparent, 16, 4, false, true, true);
break;
// ellipse spectrum (width = resolution)
case 4:
pictureBoxSpectrum.Source = _vis.CreateSpectrumEllipse(stream, pictureBoxSpectrum.Width, pictureBoxSpectrum.Height, Color.Lime, Color.Red, Color.Transparent, 1, 2, false, false, false);
break;
// ellipse spectrum (full resolution)
case 5:
pictureBoxSpectrum.Source = _vis.CreateSpectrumEllipse(stream, pictureBoxSpectrum.Width, pictureBoxSpectrum.Height, Color.SteelBlue, Color.Pink, Color.Transparent, 2, 4, false, true, true);
break;
// dot spectrum (width = resolution)
case 6:
pictureBoxSpectrum.Source = _vis.CreateSpectrumDot(stream, pictureBoxSpectrum.Width, pictureBoxSpectrum.Height, Color.Lime, Color.Red, Color.Transparent, 1, 0, false, false, false);
break;
// dot spectrum (full resolution)
case 7:
pictureBoxSpectrum.Source = _vis.CreateSpectrumDot(stream, pictureBoxSpectrum.Width, pictureBoxSpectrum.Height, Color.SteelBlue, Color.Pink, Color.Transparent, 2, 1, false, false, true);
break;
// peak spectrum (width = resolution)
case 8:
pictureBoxSpectrum.Source = _vis.CreateSpectrumLinePeak(stream, pictureBoxSpectrum.Width, pictureBoxSpectrum.Height, Color.SeaGreen, Color.LightGreen, Color.Orange, Color.Transparent, 2, 1, 2, 10, false, false, false);
break;
// peak spectrum (full resolution)
case 9:
pictureBoxSpectrum.Source = _vis.CreateSpectrumLinePeak(stream, pictureBoxSpectrum.Width, pictureBoxSpectrum.Height, Color.GreenYellow, Color.RoyalBlue, Color.DarkOrange, Color.Transparent, 23, 5, 3, 5, false, true, true);
break;
// wave spectrum (width = resolution)
case 10:
pictureBoxSpectrum.Source = _vis.CreateSpectrumWave(stream, pictureBoxSpectrum.Width, pictureBoxSpectrum.Height, Color.Yellow, Color.Orange, Color.Transparent, 1, false, false, false);
break;
// dancing beans spectrum (width = resolution)
case 11:
pictureBoxSpectrum.Source = _vis.CreateSpectrumBean(stream, pictureBoxSpectrum.Width, pictureBoxSpectrum.Height, Color.Chocolate, Color.DarkGoldenrod, Color.Transparent, 4, false, false, true);
break;
// dancing text spectrum (width = resolution)
case 12:
pictureBoxSpectrum.Source = _vis.CreateSpectrumText(stream, pictureBoxSpectrum.Width, pictureBoxSpectrum.Height, Color.White, Color.Tomato, Color.Transparent, "BASS .NET IS GREAT PIECE! UN4SEEN ROCKS...", false, false, true);
break;
// frequency detection
case 13:
//float amp = _vis.DetectFrequency(stream, 10, 500, true);
//if (amp > 0.3)
//pictureBoxSpectrum. = Color.Transparent;
//else
//pictureBoxSpectrum.BackColor = Color.Transparent;
break;
// 3D voice print
case 14:
// we need to draw directly directly on the picture box...
// normally you would encapsulate this in your own custom control
Graphics g = Graphics.FromHwnd(((System.Windows.Interop.HwndSource)System.Windows.PresentationSource.FromVisual(pictureBoxSpectrum)).Handle);
g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
_vis.CreateSpectrum3DVoicePrint(stream, g, new Rectangle(0, 0, pictureBoxSpectrum.Width, pictureBoxSpectrum.Height), Color.Black, Color.White, voicePrintIdx, false, false);
g.Dispose();
// next call will be at the next pos
voicePrintIdx++;
if (voicePrintIdx > pictureBoxSpectrum.Width - 1)
voicePrintIdx = 0;
break;
// WaveForm
case 15:
pictureBoxSpectrum.Source = _vis.CreateWaveForm(stream, pictureBoxSpectrum.Width, pictureBoxSpectrum.Height, Color.Green, Color.Red, Color.Gray, Color.Transparent, 1, true, false, true);
break;
}
}*/
    }
}
