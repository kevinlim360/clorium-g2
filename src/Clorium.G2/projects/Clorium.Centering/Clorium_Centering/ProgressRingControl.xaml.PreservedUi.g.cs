// 원본 BAML 보존 모드에서만 컴파일하는 화면 생성 코드입니다. 업무 처리는 다른 partial 파일과 공유합니다.
using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Clorium_Centering
{
    public partial class ProgressRingControl
    {
        internal RotateTransform rotate;
        private bool _contentLoaded;
        [DebuggerNonUserCode]
        [GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
        public void InitializeComponent()
        {
            if (!_contentLoaded)
            {
                _contentLoaded = true;
                Uri resourceLocator = new Uri("/Clorium Centering;component/progressringcontrol.xaml", UriKind.Relative);
                Application.LoadComponent(this, resourceLocator);
            }
        }

        [DebuggerNonUserCode]
        [GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        void IComponentConnector.Connect(int connectionId, object target)
        {
            if (connectionId == 1)
            {
                rotate = (RotateTransform)target;
            }
            else
            {
                _contentLoaded = true;
            }
        }
    }
}
