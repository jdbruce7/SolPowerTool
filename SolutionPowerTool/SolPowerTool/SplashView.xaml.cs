using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace SolPowerTool.App
{
    /// <summary>
    /// Interaction logic for SplashView.xaml
    /// </summary>
    public partial class SplashView : Window
    {
        private bool _okayToClose;
        private bool closeStoryBoardCompleted;

        public SplashView()
        {
            InitializeComponent();
        }

        public void SetMessage(string message)
        {
            messageLabel.Content = message;
        }

        public void SetVersion(Version version)
        {
            versionLabel.Content = version.ToString();
        }

        public void CloseView()
        {
            _okayToClose = true;
            Close();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (!_okayToClose)
            {
                e.Cancel = true;
                return;
            }

            if (!closeStoryBoardCompleted)
            {
                var closeStoryBoard = (Storyboard) FindResource("closeStoryBoard");
                closeStoryBoard.Completed += closeStoryBoard_Completed;
                closeStoryBoard.Begin(this);
                e.Cancel = true;
            }
        }

        private void Grid_MouseUp(object sender, MouseButtonEventArgs e)
        {
            CloseView();
        }

        private void closeStoryBoard_Completed(object sender, EventArgs e)
        {
            closeStoryBoardCompleted = true;
            Close();
        }
    }
}