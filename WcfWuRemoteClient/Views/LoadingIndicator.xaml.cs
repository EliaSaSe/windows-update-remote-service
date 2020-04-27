/*
    Windows Update Remote Service
    Copyright(C) 2016-2020  Elia Seikritt

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU Lesser General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
    GNU General Public License for more details.

    You should have received a copy of the GNU Lesser General Public License
    along with this program.If not, see<https://www.gnu.org/licenses/>.
*/
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace WcfWuRemoteClient.Views
{
    /// <summary>
    /// Interaktionslogik für LoadingIndicator.xaml
    /// </summary>
    public partial class LoadingIndicator : UserControl
    {
        public LoadingIndicator()
        {
            InitializeComponent();
        }

        public void BeginLoadingIndication()
        {
            EllipseLoadingIndicator.Visibility = Visibility.Visible;
            Storyboard sb = this.FindResource("StoryLoadingIndicator") as Storyboard;
            sb.Begin();
        }

        public void StopLoadingIndication()
        {
            EllipseLoadingIndicator.Visibility = Visibility.Hidden;
            Storyboard sb = this.FindResource("StoryLoadingIndicator") as Storyboard;
            sb.Stop();
        }
    }
}
