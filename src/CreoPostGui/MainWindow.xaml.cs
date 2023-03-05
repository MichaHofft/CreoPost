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
using System.Windows.Shell;

namespace CreoPostGui
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // start with default options
        protected Options _options = new Options();

        protected FileSystemWatcher? _inputWatcher = null;

        protected string _outputText = "";

        public MainWindow()
        {
            InitializeComponent();            
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

                // adopt output filename?
                if (CheckBoxOutputAutoAdaptFn.IsChecked == true)
                {
                    // input from left
                    var iPath = TextBoxInputFn.Text;

                    // tricky as Creo filenames have two(!) periods
                    var oFn = System.IO.Path.GetFileName(iPath);
                    var pPos = oFn.IndexOf('.');
                    if (pPos >= 0)
                        oFn = oFn.Substring(0, pPos);

                    // output to right
                    var oPath = System.IO.Path.Combine(
                                System.IO.Path.GetDirectoryName(iPath) ?? "",
                                oFn + ".nc");
                    TextBoxOutputFn.Text = oPath;
                }

                // auto save?
                if (CheckBoxOutputAutoSave.IsChecked == true)
                {
                    System.IO.File.WriteAllText(TextBoxOutputFn.Text, TextBoxOutputContent.Text);
                    Log(LogLevel.Info, "Output content saved to: {0}", TextBoxOutputFn.Text);
                }
            }
            catch (Exception ex)
            {
                Log(LogLevel.Error, "Exception {0} in {1}", ex.Message, ex.StackTrace ?? "");
            }
        }

        //--------------------------------------------------------------------------------------------
        #region FileSystemWatcher

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

        private string _watcherNotifyLastFn = "";
        private DateTime _watcherNotifyLastTime = DateTime.UtcNow;

        private async void FileSystemWatcher_Notify(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType == WatcherChangeTypes.Created 
                || e.ChangeType == WatcherChangeTypes.Changed
                || e.ChangeType == WatcherChangeTypes.Renamed)
            {
                // filter out multiple notifications
                if (e.FullPath == _watcherNotifyLastFn
                    && ((DateTime.UtcNow - _watcherNotifyLastTime).TotalMilliseconds < 1500))
                {
                    // to early!
                    return;
                }

                // remember for next time
                _watcherNotifyLastFn = e.FullPath;
                _watcherNotifyLastTime = DateTime.UtcNow;

                // wait a little bit of time because of race conditions with external (writing?) application
                await Task.Delay(1500);

                // now do!!
                await this.Dispatcher.Invoke(async () =>
                {
                    Log(LogLevel.Info, "Got notification for file: {0}", e.FullPath);

                    await UiLoadInputAsync(e.FullPath);
                    if (CheckBoxInputAutoTransform.IsChecked == true)
                        UiTransform();

                    // place holder
                    await Task.Yield();
                });
            }
        }

        #endregion

        //--------------------------------------------------------------------------------------------
        #region User feedback from widgets
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

            if (sender == ButtonOutputSelect)
            {
                var dlg = new SaveFileDialog()
                {
                    Title = "Select file / path to be saved ..",
                    Filter = "G-Code (*.nc*)|*.nc*|All files (*.*)|*.*"
                };
                if (true == dlg.ShowDialog())
                {
                    // just set field
                    TextBoxOutputFn.Text = dlg.FileName;
                }
            }

            if (sender == ButtonOutputSave)
            {
                if (TextBoxOutputFn.Text.Trim() == "(not set)")
                {
                    MessageBox.Show("You have to select a filename first!", "Save G-Code");
                    return;
                }

                try
                {
                    System.IO.File.WriteAllText(TextBoxOutputFn.Text, TextBoxOutputContent.Text);
                    Log(LogLevel.Info, "Output content saved to: {0}", TextBoxOutputFn.Text);
                }
                catch (Exception ex)
                {
                    Log(LogLevel.Error, $"Exception while saving output file: {ex.Message}");
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

        #endregion

        //--------------------------------------------------------------------------------------------
        #region Logging

        private void Log(LogLevel level, string msg, params object[] args)
        {
            var st = String.Format(msg, args);
            var para = new Paragraph(new Run(st));
            para.Margin = new Thickness(0, 0, 2, 2);
            if (level == LogLevel.Important)
                para.Background = new SolidColorBrush(Color.FromRgb(0x80, 0xff, 0xdb));
            if (level == LogLevel.Error)
                para.Background = new SolidColorBrush(Color.FromRgb(0xf7, 0x25, 0x85));
            TextBoxLog.Document.Blocks.Add(para);
            TextBoxLog.ScrollToEnd();
        }

        #endregion

        //--------------------------------------------------------------------------------------------
        #region Drag files in

        private void LabelInOut_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effects = DragDropEffects.Copy;
            else
                e.Effects = DragDropEffects.None;
        }

        private async void LabelInOut_Drop(object sender, DragEventArgs e)
        {
            // Appearantly you need to figure out if OriginalSource would have handled the Drop?
            if (!e.Handled && e.Data.GetDataPresent(DataFormats.FileDrop, true))
            {
                // Note that you can have more than one file.
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                // Assuming you have one file that you care about, pass it off to whatever
                // handling code you have defined.
                if (files != null && files.Length > 0)
                {
                    string fn = files[0];
                    try
                    {
                        if (sender == LabelInput)
                        {
                            await UiLoadInputAsync(fn);
                            UpdateInputWatcher();
                        }
                    }
                    catch (Exception ex)
                    {
                        Log(LogLevel.Error, $"Exception while receiving file drop to input/ output: {ex.Message}");
                    }
                }
            }
        }

        #endregion

        //--------------------------------------------------------------------------------------------
        # region Drag files out ..

        private bool isDragging = false;
        private Point dragStartPoint = new Point(0, 0);

        private void DragSource_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            // MIHO 2020-09-14: removed this from the check below
            //// && (Math.Abs(dragStartPoint.X) < 0.001 && Math.Abs(dragStartPoint.Y) < 0.001)
            if (e.LeftButton == MouseButtonState.Pressed && !isDragging)
            {
                Point position = e.GetPosition(null);
                if (Math.Abs(position.X - dragStartPoint.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(position.Y - dragStartPoint.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    // lock
                    isDragging = true;

                    // fail safe
                    try
                    {
                        // hastily prepare temp file ..
                        string temppath = System.IO.Path.GetTempFileName();

                        if (sender == LabelInput)
                        {
                            temppath = temppath.Replace(".tmp", ".ncl");

                            if (TextBoxInputFn.Text.Trim() != "(not set)")
                                temppath = System.IO.Path.Combine(
                                    System.IO.Path.GetDirectoryName(temppath) ?? "",
                                    System.IO.Path.GetFileName(TextBoxInputFn.Text));

                            System.IO.File.WriteAllText(temppath, TextBoxInputContent.Text);
                        }

                        if (sender == LabelOutput)
                        {
                            temppath = temppath.Replace(".tmp", ".nc");

                            if (TextBoxOutputFn.Text.Trim() != "(not set)")
                                temppath = System.IO.Path.Combine(
                                    System.IO.Path.GetDirectoryName(temppath) ?? "",
                                    System.IO.Path.GetFileName(TextBoxOutputFn.Text));

                            System.IO.File.WriteAllText(temppath, TextBoxOutputContent.Text);
                        }

                        // Package the data
                        DataObject data = new DataObject();
                        data.SetFileDropList(new System.Collections.Specialized.StringCollection() { temppath });

                        // Inititate the drag-and-drop operation.
                        DragDrop.DoDragDrop(this, data, DragDropEffects.Copy | DragDropEffects.Move);
                    }
                    catch (Exception ex)
                    {
                        Log(LogLevel.Error, $"Exception while emitting file drop from input/ output: {ex.Message}");
                        return;
                    }

                    // unlock
                    isDragging = false;
                }
            }
        }

        private void DragSource_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            dragStartPoint = e.GetPosition(null);
        }

        #endregion

        //--------------------------------------------------------------------------------------------
        #region Options handling

        protected void OptionsToUi(Options opt)
        {
            CheckBoxInputAutoUpdateFile.IsChecked = opt.AutoUpdateFile;
            CheckBoxInputAutoUpdateDir.IsChecked = opt.AutoLoadFromSameDir;
            CheckBoxInputAutoTransform.IsChecked = opt.AutoTransform;
            CheckBoxOutputAutoAdaptFn.IsChecked = opt.AutoAdaptFilename;
            CheckBoxOutputAutoSave.IsChecked = opt.AutoSave;
            CheckBoxOutputAutoPasteBin.IsChecked = opt.AutoPasteBin;
            TextBoxPasteBinId.Text = opt.PasteBinId;
        }

        protected void OptionsFromUi(Options opt)
        {
            opt.AutoUpdateFile = CheckBoxInputAutoUpdateFile.IsChecked == true ;
            opt.AutoLoadFromSameDir = CheckBoxInputAutoUpdateDir.IsChecked == true ;
            opt.AutoTransform = CheckBoxInputAutoTransform.IsChecked == true ;
            opt.AutoAdaptFilename = CheckBoxOutputAutoAdaptFn.IsChecked == true ;
            opt.AutoSave = CheckBoxOutputAutoSave.IsChecked == true ;
            opt.AutoPasteBin = CheckBoxOutputAutoPasteBin.IsChecked == true;
            opt.PasteBinId = TextBoxPasteBinId.Text;
        }

        #endregion

        //--------------------------------------------------------------------------------------------
        #region Window Loaded and Exit

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // final preparation
            TextBoxLog.Document.Blocks.Clear();
            Log(LogLevel.Info, Options.PrgVersionAndCredits);
            Log(LogLevel.Info, "Application started.");

            // try load options
            var optFn = Options.GetDefaultOptionsFn();
            Options? optNew = null;
            Log(LogLevel.Info, "Try load options from: {0} ...", optFn);
            try
            {
                optNew = Options.LoadFromFile(optFn);
                if (optNew != null)
                {
                    _options = optNew;
                    Log(LogLevel.Info, ".. options successfully loaded.");
                }
            } 
            catch (Exception ex)
            {
                Log(LogLevel.Error, ".. exception while loading options: {0}", ex.Message);
            }

            // set actual options
            OptionsToUi(_options);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // try save options
            var optFn = Options.GetDefaultOptionsFn();
            Log(LogLevel.Info, "Try save options to: {0} ...", optFn);
            try
            {
                OptionsFromUi(_options);
                Options.SaveFile(optFn, _options);
                Log(LogLevel.Info, ".. options successfully saved.");
            }
            catch (Exception ex)
            {
                Log(LogLevel.Error, ".. exception while saving options: {0}", ex.Message);
            }

            // let close
            e.Cancel = false;
        }

        #endregion

    }
}
