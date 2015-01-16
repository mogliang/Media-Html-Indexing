using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PicSiteGenerator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void selectpic_click(object sender, RoutedEventArgs e)
        {

            var dialog = new FolderBrowserDialog();
            var result = dialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                img_tb.Text = dialog.SelectedPath;
            }
        }

        private void selectsite_click(object sender, RoutedEventArgs e)
        {
            var dialog = new FolderBrowserDialog();
            var result = dialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                var count = Directory.EnumerateFileSystemEntries(dialog.SelectedPath).Count();
                if (count > 0)
                {
                    info_tb.Text = "网站目录不为空！";
                }
                else
                    site_tb.Text = dialog.SelectedPath;
            }
        }

        private void pathtype_check(object sender, RoutedEventArgs e)
        {

        }

        private void generate_click(object sender, RoutedEventArgs e)
        {
            //try
            //{
                info_tb.Text = "";

                var imgpath = img_tb.Text;
                var sitepath = site_tb.Text;
                if (string.IsNullOrWhiteSpace(imgpath) ||
                    string.IsNullOrWhiteSpace(sitepath))
                {
                    info_tb.Text = "路径未设定。";
                    return;
                }

                if (g1_r1.IsChecked.Value)
                    Logic.PathMode = "relative";
                else
                    Logic.PathMode = "absolute";
                Logic.PageSize = (int)sd1.Value;

                Logic.Traverse(imgpath, sitepath, (int)sd2.Value);
                Logic.GenerateIndex(title_tbx.Text, sitepath);

                info_tb.Text = "生成完毕!";

                Process.Start(site_tb.Text + "\\index.html");
            //}
            //catch (Exception ex)
            //{
            //    info_tb.Text += "出错！ " + ex.Message;
            //}
        }
    }
}
