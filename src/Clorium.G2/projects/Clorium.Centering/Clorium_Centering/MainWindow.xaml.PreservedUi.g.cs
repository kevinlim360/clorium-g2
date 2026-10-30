// 원본 BAML 보존 모드에서만 컴파일하는 화면 생성 코드입니다. 업무 처리는 다른 partial 파일과 공유합니다.
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Clorium_Centering.Properties;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace Clorium_Centering
{
    public partial class MainWindow
    {
        internal Button BtnLoadImages;
        internal ListBox ListBoxImages;
        internal Button BtnPlayOriginal;
        internal Button BtnStopOriginal;
        internal Slider SliderSpeedOriginal;
        internal TextBox TxtThreshold;
        internal RepeatButton BtnDownThreshold;
        internal RepeatButton BtnUpThreshold;
        internal Button BtnTestThreshold;
        internal TextBox TxtCropRatio;
        internal RepeatButton BtnDownCrop;
        internal RepeatButton BtnUpCrop;
        internal CheckBox ChkSaveTransparent;
        internal CheckBox ChkSaveCenter;
        internal CheckBox ChkSaveMag;
        internal Button BtnCorrectAll;
        internal Button BtnCorrectStop;
        internal ProgressBar ProgressBarCorrection;
        internal Button BtnLoadFolder;
        internal ListBox ListBoxCorrected;
        internal Button BtnPlay;
        internal Button BtnStop;
        internal Slider SliderSpeed;
        internal Image ImgPreview;
        internal ProgressRingControl ProgressRing;
        private bool _contentLoaded;
        [DebuggerNonUserCode]
        [GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
        public void InitializeComponent()
        {
            if (!_contentLoaded)
            {
                _contentLoaded = true;
                Uri resourceLocator = new Uri("/Clorium Centering;component/mainwindow.xaml", UriKind.Relative);
                Application.LoadComponent(this, resourceLocator);
            }
        }

        [DebuggerNonUserCode]
        [GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
        internal Delegate _CreateDelegate(Type delegateType, string handler)
        {
            return Delegate.CreateDelegate(delegateType, this, handler);
        }

        [DebuggerNonUserCode]
        [GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        void IComponentConnector.Connect(int connectionId, object target)
        {
            switch (connectionId)
            {
                case 1:
                    BtnLoadImages = (Button)target;
                    BtnLoadImages.Click += BtnLoadImages_Click;
                    break;
                case 2:
                    ListBoxImages = (ListBox)target;
                    ListBoxImages.SelectionChanged += ListBoxImages_SelectionChanged;
                    break;
                case 4:
                    BtnPlayOriginal = (Button)target;
                    BtnPlayOriginal.Click += BtnPlayOriginal_Click;
                    break;
                case 5:
                    BtnStopOriginal = (Button)target;
                    BtnStopOriginal.Click += BtnStopOriginal_Click;
                    break;
                case 6:
                    SliderSpeedOriginal = (Slider)target;
                    break;
                case 7:
                    TxtThreshold = (TextBox)target;
                    TxtThreshold.TextChanged += TxtThreshold_TextChanged;
                    break;
                case 8:
                    BtnDownThreshold = (RepeatButton)target;
                    BtnDownThreshold.Click += BtnDownThreshold_Click;
                    break;
                case 9:
                    BtnUpThreshold = (RepeatButton)target;
                    BtnUpThreshold.Click += BtnUpThreshold_Click;
                    break;
                case 10:
                    BtnTestThreshold = (Button)target;
                    BtnTestThreshold.Click += BtnPreviewThreshold_Click;
                    break;
                case 11:
                    TxtCropRatio = (TextBox)target;
                    TxtCropRatio.TextChanged += TxtCropRatio_TextChanged;
                    break;
                case 12:
                    BtnDownCrop = (RepeatButton)target;
                    BtnDownCrop.Click += BtnDownCrop_Click;
                    break;
                case 13:
                    BtnUpCrop = (RepeatButton)target;
                    BtnUpCrop.Click += BtnUpCrop_Click;
                    break;
                case 14:
                    ChkSaveTransparent = (CheckBox)target;
                    break;
                case 15:
                    ChkSaveCenter = (CheckBox)target;
                    break;
                case 16:
                    ChkSaveMag = (CheckBox)target;
                    break;
                case 17:
                    BtnCorrectAll = (Button)target;
                    BtnCorrectAll.Click += BtnCorrectAll_Click;
                    break;
                case 18:
                    BtnCorrectStop = (Button)target;
                    BtnCorrectStop.Click += BtnCorrectStop_Click;
                    break;
                case 19:
                    ProgressBarCorrection = (ProgressBar)target;
                    break;
                case 20:
                    BtnLoadFolder = (Button)target;
                    BtnLoadFolder.Click += BtnLoadFolder_Click;
                    break;
                case 21:
                    ListBoxCorrected = (ListBox)target;
                    ListBoxCorrected.SelectionChanged += ListBoxCorrected_SelectionChanged;
                    break;
                case 22:
                    BtnPlay = (Button)target;
                    BtnPlay.Click += BtnPlay_Click;
                    break;
                case 23:
                    BtnStop = (Button)target;
                    BtnStop.Click += BtnStop_Click;
                    break;
                case 24:
                    SliderSpeed = (Slider)target;
                    SliderSpeed.ValueChanged += SliderSpeed_ValueChanged;
                    break;
                case 25:
                    ImgPreview = (Image)target;
                    break;
                case 26:
                    ProgressRing = (ProgressRingControl)target;
                    break;
                default:
                    _contentLoaded = true;
                    break;
            }
        }

        [DebuggerNonUserCode]
        [GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        void IStyleConnector.Connect(int connectionId, object target)
        {
            if (connectionId == 3)
            {
                EventSetter eventSetter = new EventSetter();
                eventSetter.Event = Control.MouseDoubleClickEvent;
                eventSetter.Handler = new MouseButtonEventHandler(ListBoxItem_MouseDoubleClick);
                ((Style)target).Setters.Add(eventSetter);
            }
        }
    }
}
