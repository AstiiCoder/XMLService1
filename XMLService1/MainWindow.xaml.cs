using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading;
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

namespace XMLService1
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //public static string DirSource = @"\\serv-01\D$\SferaConnector\EDI\4607180869999EC\4620012939998\IN\ORDER_263309";
        public static string DirSource = @"D:\";
        public static bool IsChange = true;
        FileSystemWatcher watcher = new FileSystemWatcher();
        public static System.Windows.Threading.DispatcherTimer timer;
        public static int SignalFreq = 5;
        public static string DescPath = String.Empty;
        public static string SourcePath = String.Empty;
        public static WatcherChangeTypes MesType;
        Wait waitWindow = new Wait();

        public MainWindow()
        {
            InitializeComponent();
            // Список директорий
            IniFiles ini = new IniFiles();
            List<string> Dirs = ini.ReadSection("Dir");
            foreach (string item in Dirs)
            {
                string[] s = item.Split('=');
                Dir dir = new Dir(s[0], s[1]);
                ComboBox_Directories.Items.Add(dir);
            }
            // Ещё настройки из ini-файла            
            DescPath = ini.ReadString("Main", "DescPath", String.Empty);

            //Выбор по-умолчанию - первая директория в списке
            //ComboBox_Directories.SelectedIndex = 0;
            Dir selectedItem = (Dir)ComboBox_Directories.Items[0];
            DirSource = selectedItem._DirPath;
            // Создадим таймер. Пока не запускаем.
            timer = new System.Windows.Threading.DispatcherTimer();
            timer.Tick += new EventHandler(TimerTick);
            timer.Interval = new TimeSpan(0, 0, SignalFreq);
                        
        }

        private void Fill_FileList()
        {
            if (DirSource == String.Empty) return;

            int NewFiles = 0;
            List<string> allfiles = Directory.GetFiles(DirSource).OrderByDescending(f => File.GetLastWriteTime(f)).ToList();

            foreach (string filename in allfiles)
            {
                FileAttributes attributes = File.GetAttributes(filename);
                int FileStatus = 0;
                if ((attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                    FileStatus = 1;                   
                else
                    NewFiles++;

                ListBox_Files.Items.Add(new XMLFile(System.IO.Path.GetFileName(filename),
                                                    File.GetCreationTime(filename), 
                                                    FileStatus)
                                       );
            }

            TextBox_FilesCount.Text = "Файлов: " + Convert.ToString(ListBox_Files.Items.Count);
            TextBox_NewFilesCount.Text = "Необработанных: " + Convert.ToString(NewFiles);
            if (waitWindow.IsVisible == true) waitWindow.Hide(); 
        }

        /// <summary>
        /// Слежение за директорией
        /// </summary>
        /// <param name="Dir"></param>
        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        public void RunWatching(string Dir)
        {
            
            watcher.Path = Dir;
            watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite
               | NotifyFilters.FileName | NotifyFilters.DirectoryName;
            // Маска файлов.
            watcher.Filter = "*.xml";

            // События, по которым хотим иметь уведомление.           
            watcher.Created += new FileSystemEventHandler(OnChanged);
            watcher.Deleted += new FileSystemEventHandler(OnChanged);
            //Можно ещё и за этими событиями посмотреть
            //watcher.Changed += new FileSystemEventHandler(OnChanged);
            //watcher.Renamed += new RenamedEventHandler(OnRenamed);

            // Начинаем наблюдение.
            watcher.EnableRaisingEvents = true;
        }

        private void OnChanged(object source, FileSystemEventArgs e)
        {
            // Вот такой файл у нас: e.FullPath + " " + e.ChangeType
            IsChange = true;
            MesType = e.ChangeType;
            SourcePath = e.FullPath;
        }

        private void TimerTick(object sender, EventArgs e)
        {
            if ((IsChange) && (DirSource!=String.Empty))
            {
                watcher.EnableRaisingEvents = false;
                string ShortFileName = System.IO.Path.GetFileName(SourcePath);
                if ( (MesType.ToString() == "Created") && (XMLFile.IsAlreadyExist(ShortFileName) == false) )
                {                   
                    ListBox_Journal.Items.Add(new Journal(DateTime.Now,
                         "Пришёл новый файл '" + ShortFileName + "'")
                         );
                    // Пробуем скопировать файл
                    FileInfo fileInfo = new FileInfo(DescPath);
                    if (fileInfo.IsReadOnly == true) fileInfo.IsReadOnly = false;
                    try
                    {
                        File.Copy(SourcePath, DescPath, true);
                    }
                    catch (IOException Err)
                    {
                        if (Err.Source != null)
                            MessageBox.Show("Произошла ошибка: {0}", Err.Source);
                        return;
                    }

                    ListBox_Journal.Items.Add(new Journal(DateTime.Now,
                                             "Файл '" + ShortFileName + "' скопирован в место загрузки")
                                             );
                    Thread.Sleep(2000);
                    byte R = 0;
                    Int32 IDDOC = 0;
                    string ErrorMsg = String.Empty;

                    // Загрузка файла в БД, подготовка к отправке в ЛиС
                    Class_DB_Operations.AddFile(ref R, ref IDDOC);
                    if ((R == 1) && (IDDOC != 0))
                    {
                        ListBox_Journal.Items.Add(new Journal(DateTime.Now,
                                                             "Файл '" + ShortFileName + "' обработан. Код документа: " + Convert.ToString(IDDOC))
                                                 );
                        // Отправке документа в ЛиС
                        Class_DB_Operations.OrderToFox(IDDOC, ref R, ref ErrorMsg);
                        if (R == 1)
                        {
                            // Присваиваем аттрибут "для чтения", символизирующий, что файл отправлени в "ЛиС"
                            fileInfo = new FileInfo(SourcePath);
                            fileInfo.IsReadOnly = true;
                        }
                        else
                        {
                            ListBox_Journal.Items.Add(new Journal(DateTime.Now,
                                                                 "Ошибка при отправке документа в 'ЛиС': " + Convert.ToString(R) + ", " + ErrorMsg)
                                                                 );
                        }

                    }
                    else
                    {
                        ListBox_Journal.Items.Add(new Journal(DateTime.Now,
                                     "Ошибка '" + Convert.ToString(R) + "' при обработке")
                                     );
                    }
                }
                
               
                ListBox_Files.Items.Clear();
                Thread.Sleep(1000);
                Fill_FileList() ;
                IsChange = false;
                watcher.EnableRaisingEvents = true;
            }
        }

        private void ComboBox_DirectoriesChanged(object sender, SelectionChangedEventArgs e)
        {
            // Пока переходим к другой папке - таймер выключим.
            timer.Stop();
            watcher.EnableRaisingEvents = false;

            waitWindow.Owner = this;
            waitWindow.Show();           
            ListBox_Files.Items.Clear();

            ComboBox comboBox = (ComboBox)sender;
            Dir selectedItem = (Dir)comboBox.SelectedItem;
            DirSource = selectedItem._DirPath;

            RunWatching(DirSource);
            Thread.Sleep(1000);
            IsChange = true;
            timer.Start();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            waitWindow.Close();
        }

        //private static void OnRenamed(object source, RenamedEventArgs e)
        //{
        //    MessageBox.Show("File: {0} renamed to " + e.FullPath, e.OldFullPath);
        //}

    }
}
