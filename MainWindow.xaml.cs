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
using System.Threading;
using System.Drawing;
using System.ComponentModel;
using System.Windows.Threading;

namespace TreeSize
{
    public partial class MainWindow : Window
    {
        string sizeDir = "";
        long _size = 0;
        static Dictionary<object, object> Dir_Size = new Dictionary<object, object>();
        static object locker = new object();
        private BackgroundWorker worker = null;
        public MainWindow()
        {          
            InitializeComponent();            
            DriveInfo[] drives = DriveInfo.GetDrives();
            foreach(DriveInfo driveInfo in drives)
            {
                long size_byte = driveInfo.TotalSize;               
                string sizeDisk = Calculator(size_byte);
                trw_Products.Items.Add(CreateTreeItem(driveInfo, sizeDisk));                
            }         
        }
        public void timer_Tick(object sender, EventArgs e)
        {
           foreach(TreeViewItem n in trw_Products.Items)
            {
                foreach (var m in Dir_Size)
                {
                    if (n.Tag == m.Key)
                    {
                        n.Header = m.Value.ToString() + " " + m.Key.ToString();
                    }
                }
            }                 
        }

        public void trw_Products_Expanded(object sender, RoutedEventArgs e)
        {
            DispatcherTimer  timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += timer_Tick;
           
            TreeViewItem  item = e.Source as TreeViewItem;
            if ((item.Items.Count == 1) && (item.Items[0] is string))
            {
                item.Items.Clear();
                DirectoryInfo expandedDir = null;
                if (item.Tag is DriveInfo)
                    expandedDir = (item.Tag as DriveInfo).RootDirectory;
                if (item.Tag is DirectoryInfo)
                    expandedDir = (item.Tag as DirectoryInfo);
                try
                {
                    int count = expandedDir.GetDirectories().Count();                   
                    foreach (DirectoryInfo subDir in expandedDir.GetDirectories())
                    {                        
                        var isHidden = (subDir.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden;
                        var isSystem = (subDir.Attributes & FileAttributes.System) == FileAttributes.System;
                        if (!isHidden && !isSystem)
                        {
                           // item.Items.Add(CreateTreeItem(subDir, sizeDir));
                            DataContainer data = new DataContainer();
                            data.path = (DirectoryInfo)subDir;
                            BackgroundWorker worker = new BackgroundWorker();
                            worker.WorkerReportsProgress = true;
                            worker.DoWork += worker_DoWork;
                            worker.ProgressChanged += worker_ProgressChanged;
                            worker.RunWorkerCompleted += worker_RunWorkerCompleted;                          
                            worker.RunWorkerAsync(data);
                            Thread.Sleep(200);                            
                            sizeDir = Calculator(data.sizeAllDir);
                            timer.Start();
                            item.Items.Add(CreateTreeItem(subDir,sizeDir));
                        }                      
                    }    
                    
                    foreach (var file in expandedDir.GetFiles())
                    {
                        var isHidden = (file.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden;
                        var isSystem = (file.Attributes & FileAttributes.System) == FileAttributes.System;
                        if (!isHidden && !isSystem)
                        {
                            long size_byte = file.Length;
                            string sizeFile = Calculator(size_byte);
                            item.Items.Add(GetItem(file, sizeFile));                       
                        }
                    }                   
                }
                catch (DirectoryNotFoundException ex)
                {
                    Console.WriteLine("Директория не найдена. Ошибка: " + ex.Message);                  
                }               
                catch (UnauthorizedAccessException ex)
                {
                    Console.WriteLine("Отсутствует доступ. Ошибка: " + ex.Message);                   
                }               
                catch (Exception ex)
                {
                    Console.WriteLine("Произошла ошибка. Обратитесь к администратору. Ошибка: " + ex.Message);
                }                
            }            
        }     
        void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {            
           // pbCalculationProgress.Value = e.ProgressPercentage;   
        }
        private void worker_DoWork(object sender, DoWorkEventArgs e)
        {           
            DataContainer data = (DataContainer)e.Argument;
            lock (locker)
            {
                 long size2 = 0;
                 size2 = GetSizeDir(data.path);
                 data.sizeAllDir = size2;                          
                 Dir_Size.Add(data.path, size2);
            }         
            e.Result = data.path;
           (sender as BackgroundWorker).ReportProgress(0, data.sizeAllDir);
            
        }
        void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            
            string rezult = e.Result.ToString();         
        }
        private TreeViewItem CreateTreeItem(object o, string sizeDisk)
        {            
            TreeViewItem item = new TreeViewItem();
            item.Header = sizeDisk + " " +  o.ToString();            
            item.Tag = o;
            item.Items.Add("Loading...");
            item.Expanded += new RoutedEventHandler(trw_Products_Expanded);
            return item;
        }       
        private TreeViewItem GetItem(FileInfo file, string sizeFile)
        {
            var item = new TreeViewItem
            {
                Header = sizeFile + "   " + file.Name,
                DataContext = file,
                Tag = file
            };
            return item;
        }
                  
         public long GetSizeDir (DirectoryInfo dir)
         {                              
                                       
                var isHidden = (dir.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden;
                var isSystem = (dir.Attributes & FileAttributes.System) == FileAttributes.System;
                if (!isHidden && !isSystem)            
                {
                    try
                    {
                        foreach (var file in dir.GetFiles())
                        {
                           _size += (new FileInfo(file.ToString())).Length;
                        }
                    }
                    catch 
                    {
                    }
                }                     
                                          
            try
            {
                foreach (DirectoryInfo subDir in dir.GetDirectories())
                {
                    var isHidden1 = (subDir.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden;
                    var isSystem1 = (subDir.Attributes & FileAttributes.System) == FileAttributes.System;
                    if (!isHidden1 && !isSystem1)
                    _size =  GetSizeDir(subDir);                  
                }
            }
            catch
            {
                
            }
            
            return _size;         
         }

        private string Calculator(long _size)
        {
            string size1 = "";           
            if (_size < 1024)
                return size1 = _size.ToString() + " b";
            _size = _size / 1024;
            if (_size < 1024)
                return size1 = _size.ToString() + " kb";
            _size = _size / 1024;
            if (_size < 1024)
                return size1 = _size.ToString() + " Mb";
            _size = _size / 1024;
            if (_size < 1024)
                return size1 = _size.ToString() + " Gb";
            _size = _size / 1024;
            return size1 = _size.ToString() + " Tb";

        }
    }
    public  class DataContainer
    {
        public  DirectoryInfo path { get; set; }
        public  long sizeAllDir { get; set; }
        public DataContainer()
        {
            sizeAllDir = 0;
        }
    }
}
