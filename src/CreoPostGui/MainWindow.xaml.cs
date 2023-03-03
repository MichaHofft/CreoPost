using CreoPost;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CreoPostGui
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        protected FileSystemWatcher? _inputWatcher = null;

        protected string _outputText = "";

        public MainWindow()
        {
            InitializeComponent();
            TextBoxLog.Document.Blocks.Clear();
            Log(LogLevel.Info, "CreoPost v.0.1 (c) by Michael Hoffmeister.");
            Log(LogLevel.Info, "Application started.");
        }

        private async Task UiLoadInputAsync(string fn)
        {
            // show fn
            TextBoxInputFn.Text = fn;

            // log
            Log(LogLevel.Important, "Load input: {0}", fn);

            // read text
            try
            {
                // read
                var txt = await System.IO.File.ReadAllTextAsync(fn);
                TextBoxInputContent.Text = txt;
            } 
            catch (Exception ex)
            {
                Log(LogLevel.Error, "Exception {0} in {1}", ex.Message, ex.StackTrace ?? "");
            }
        }

        private void UiTransform()
        {
            // safe way
            try
            {
                // log
                Log(LogLevel.Info, "Parsing text file to A/BCL commands at {0}", DateTime.Now.ToShortTimeString());

                // try convert into NCL lines
                var ncl = new NclReader();
                ncl.Log = this.Log;
                ncl.ReadNclFromText(TextBoxInputContent.Text);

                // error
                if (ncl.ErrorNum > 0)
                {
                    Log(LogLevel.Error, $"There are {ncl.ErrorNum} errors. Aborting!");
                    return;
                }

                // log
                var startTime = DateTime.UtcNow;
                Log(LogLevel.Info, "Starting transformation to G-Code ..");

                // create G-Code
                var gcode = new GcodeWriter();

                // add header
                GcodeTemplates.AddHeaderLikeFreeCadGrbl(gcode);

                // post processor
                var post = new PostProcNclToGcode();
                post.Log = this.Log;
                var res = post.NctToGcode(ncl, gcode);
                if (!res)
                {
                    Log(LogLevel.Error, $"There were errors. Aborting!");
                    return;
                }
                Log(LogLevel.Important, $"Transformation successfully in {(DateTime.UtcNow - startTime).TotalMilliseconds}ms.");

                // footer
                GcodeTemplates.AddFooterLikeFreeCadGrbl(gcode);

                // produce output text
                var outputText = gcode.WriteLinesToText();
                TextBoxOutputContent.Text = outputText;
            }
            catch (Exception ex)
            {
                Log(LogLevel.Error, "Exception {0} in {1}", ex.Message, ex.StackTrace ?? "");
            }
        }

        protected void UpdateInputWatcher()
        {
            if (CheckBoxInputAutoUpdateDir?.IsChecked == true)
            {
                // (re-) configure to directory watch
                try
                {
                    if (_inputWatcher == null)
                        _inputWatcher = new FileSystemWatcher();

                    _inputWatcher.Path = System.IO.Path.GetDirectoryName(TextBoxInputFn.Text) ?? "";
                    _inputWatcher.Filter = "*.ncl*";

                    _inputWatcher.NotifyFilter = NotifyFilters.Attributes
                                     | NotifyFilters.CreationTime
                                     | NotifyFilters.DirectoryName
                                     | NotifyFilters.FileName
                                     | NotifyFilters.LastAccess
                                     | NotifyFilters.LastWrite
                                     | NotifyFilters.Security
                                     | NotifyFilters.Size;

                    _inputWatcher.Changed += FileSystemWatcher_Notify;
                    _inputWatcher.Created += FileSystemWatcher_Notify;

                    _inputWatcher.IncludeSubdirectories = false;
                    _inputWatcher.EnableRaisingEvents = true;
                } 
                catch (Exception ex)
                {
                    ;
                }
            }
            else
            if (CheckBoxInputAutoUpdateFile?.IsChecked == true)
            {
                try
                { 
                    // (re-) configure to directory watch
                    if (_inputWatcher == null)
                        _inputWatcher = new FileSystemWatcher();

                    _inputWatcher.Path = System.IO.Path.GetDirectoryName(TextBoxInputFn.Text) ?? "";
                    _inputWatcher.Filter = System.IO.Path.GetFileName(TextBoxInputFn.Text) ?? ""; ;

                    _inputWatcher.NotifyFilter = NotifyFilters.Attributes
                                     | NotifyFilters.CreationTime
                                     | NotifyFilters.DirectoryName
                                     | NotifyFilters.FileName
                                     | NotifyFilters.LastWrite
                                     | NotifyFilters.Security
                                     | NotifyFilters.Size;

                    _inputWatcher.Changed += FileSystemWatcher_Notify;
                    _inputWatcher.Created += FileSystemWatcher_Notify;

                    _inputWatcher.IncludeSubdirectories = false;
                    _inputWatcher.EnableRaisingEvents = true;
                }
                catch (Exception ex)
                {
                    ;
                }
            }
            else
            {
                // switch off
                if (_inputWatcher != null)
                {
                    _inputWatcher.EnableRaisingEvents = false;
                    _inputWatcher = null;
                }
            }
        }

        private async void FileSystemWatcher_Notify(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType == WatcherChangeTypes.Created 
                || e.ChangeType == WatcherChangeTypes.Changed
                || e.ChangeType == WatcherChangeTypes.Renamed)
            {
                await this.Dispatcher.Invoke(async () =>
                {
                    await UiLoadInputAsync(e.FullPath);
                });
            }
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            if (sender == ButtonInputLoad)
            {
                var dlg = new OpenFileDialog()
                {
                    Title = "Select file / path to be loaded ..",
                    Filter = "Creo ACL/NCL (*.ncl*)|*.ncl*|All files (*.*)|*.*"
                };
                if (true == dlg.ShowDialog())
                {
                    await UiLoadInputAsync(dlg.FileName);
                    UpdateInputWatcher();
                }
            }

            if (sender == ButtonLogClear)
            {
                TextBoxLog.Document.Blocks.Clear();
            }

            if (sender == ButtonTransform)
            {
                UiTransform();
            }
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (sender == CheckBoxInputAutoUpdateFile 
                || sender == CheckBoxInputAutoUpdateDir)
            {
                this.Dispatcher.Invoke(() =>
                {
                    UpdateInputWatcher();
                });
            }
        }

        private void Log(LogLevel level, string msg, params object[] args)
        {
            var st = String.Format(msg, args);
            var para = new Paragraph(new Run(st));
            para.Margin = new Thickness(0, 0, 2, 2);
            if (level == LogLevel.Important)
                para.Background = Brushes.LightBlue;
            if (level == LogLevel.Error)
                para.Background = Brushes.LightPink;
            TextBoxLog.Document.Blocks.Add(para);
            TextBoxLog.ScrollToEnd();
        }
    }
}
