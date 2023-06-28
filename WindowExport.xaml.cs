using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Forms;

using ICSharpCode.ILSpy;
using ICSharpCode.ILSpy.TextView;
using ICSharpCode.ILSpy.TreeNodes;
using ICSharpCode.TreeView;

namespace BatchExportPlugin
{
    public partial class WindowExport : Window
    {
        String versionPlugin = "v1.0.0";
        char seperator = '|';
        IList<SharpTreeNode> multipleExport = new List<SharpTreeNode>();
        public WindowExport()
        {
            InitializeComponent();
        }

        private void buttonExport_Click(object sender, RoutedEventArgs e)
        {
            // deactivate export button so user can not click it more than once, re-enable after process is finished
            this.buttonExport.IsEnabled = false;
            int countLine = this.textBoxInput.LineCount;
            bool? singleExport = this.checkBox_SingleExport.IsChecked;
            var currentLanguage = MainWindow.Instance.CurrentLanguage;
            var options = MainWindow.Instance.CreateDecompilationOptions();
            options.FullDecompilation = true;
            bool match = false;
            int countSeperator = 0;
            String[] inputSplitted = Array.Empty<String>();
            multipleExport.Clear();
            for (int i = 0; i < countLine; i++)
            {
                // reset seperator count and splitted string for every line
                countSeperator = 0;
                inputSplitted = Array.Empty<String>();
                // trim to remove \r\n at the end
                String lineCurrent = this.textBoxInput.GetLineText(i).Trim();
                foreach (char c in lineCurrent)
                {
                    if (c == seperator) countSeperator++;
                }
                // after we read the current line we want to update the progress label
                // an easy label.text = "new text" seems not to be possible so we have to do some thread/worker/invoke thingies.
                // this was the result of abusing search engines for hours.
                textblockProgress.Dispatcher.Invoke((MethodInvoker)delegate {
                    textblockProgress.Text = "current line: " + (i + 1).ToString() + "/" + countLine + " - " + lineCurrent;
                }, System.Windows.Threading.DispatcherPriority.Background);
                if (countSeperator > 0)
                {
                    // from here on our string has at least one seperator so it will be splitted
                    // into a minimum of two sub-strings. no need to handle only one sub-string
                    // since this is not the intention of this plugin
                    inputSplitted = lineCurrent.Split(seperator);
                    if (inputSplitted.Count() > 1)
                    {
                        match = false;
                        // counter for the current position inside the splitted string
                        int iString = 0;
                        var asmTreeCurrent = MainWindow.Instance.AssemblyTreeView;
                        SharpTreeNode temp, backup = null;
                        // loop through every loaded assembly
                        for (int iAssembly = 0; iAssembly < asmTreeCurrent.Items.Count; iAssembly++)
                        {
                            temp = asmTreeCurrent.Items[iAssembly] as SharpTreeNode;
                            // backup our main-item to revert any changes later
                            backup = asmTreeCurrent.Items[iAssembly] as SharpTreeNode;
                            // we just want to check assembly names of first level here, no children
                            if (temp.Level > 1)	continue;
                            // check if current assembly matches our input, if so goto next step
                            if (temp.Text.ToString().StartsWith(inputSplitted[iString]))
                            {
                                // increase our string counter since we have matched the first element
                                iString++;
                                // set item to expanded, else the childrens are not visible sometimes
                                temp.IsExpanded = true;
                                // loop through all childen of a node
                                for (int iLoop = 0; iLoop < temp.Children.Count; iLoop++)
                                {
                                    // check if the investigated element is on the same 'deepness' level as our string
                                    // if not this is a wrong element and we can take the next one
                                    // may decrease search time
                                    if (temp.Children[iLoop].Level != iString + 1) continue;
                                    if (temp.Children[iLoop].Text.ToString().StartsWith(inputSplitted[iString]))
                                    {
                                        // check if we reached the end of our string
                                        if (iString == inputSplitted.Count() - 1)
                                        {
                                            // we have found our last element, so we want to save it
                                            if (singleExport.Value)
                                            {
                                                IList<SharpTreeNode> output = new SharpTreeNode[1];
                                                SharpTreeNode node = temp.Children[iLoop];
                                                output[0] = node;
                                                DecompilerTextView fileWriter = new DecompilerTextView();
                                                fileWriter.SaveToDisk(currentLanguage, output.OfType<ILSpyTreeNode>(), options);
                                            }
                                            // if the user wants to export everyhting as one file we will just store it for later
                                            else
                                            {
                                                SharpTreeNode node = temp.Children[iLoop];
                                                multipleExport.Add(node);
                                            }
                                            match = true;
                                            break;
                                        }
                                        // else we do not have the last element so we want to check if the exact name matches
                                        else if (temp.Children[iLoop].Text.ToString() == inputSplitted[iString]){
                                            // set temp to our found childen
                                            temp = temp.Children[iLoop];
                                            // as before, set it to expanded so childrens are visible
                                            temp.IsExpanded = true;
                                            // reset loopIndex to 0 to search from the beginning in the new child
                                            iLoop = 0;
                                            // increase string counter since we have found the current element of our string
                                            iString++;
                                        }
                                    }
                                }
                            }
                            // before checking the next assembly collapse the current assembly so the asmTree.Items.Count
                            // will go to a lower number, may decrease search time
                            backup.IsExpanded = false;
                            // break loop if we found our desired childen
                            if (match) break;
                            // else reset stringcounter to 0 to search for the desired function in the next assembly
                            else iString = 0;
                        }
                    }
                }
            }
            // after every line was looped we want to check if the user wants to output it as one file
            // also check if the output has content
            if (!singleExport.Value && multipleExport.Count > 0)
            {
                DecompilerTextView fileWriter = new DecompilerTextView();
                fileWriter.SaveToDisk(currentLanguage, multipleExport.OfType<ILSpyTreeNode>(), options);
            }
            this.buttonExport.IsEnabled = true;
        }

        private void buttonClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            // Form_Load function, do initial setup here
            this.MinWidth = 800;
            this.MinHeight = 530;
            this.MaxWidth = 800;
            this.MaxHeight = 530;
            this.label_textBoxDescription.Content = "enter one assembly per line, seperator is pipe (|). (executable|tree|node|function|whatever)";
            this.label_textBoxDescription.Content += "\nlines with less than one seperator will be ignored.";
            this.textBoxInput.Text = string.Empty;
            this.textBoxInput.AcceptsReturn = true;
            this.textblockProgress.Text = string.Empty;
            this.checkBox_SingleExport.Content = "Single Export\n(a save dialog will be shown for every assembly)";
            this.Title = "Batch Export " + versionPlugin;
        }
    }
}
