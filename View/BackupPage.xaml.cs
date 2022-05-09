using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Projet_ProgSys_Graphical;
using Projet_ProgSys.Model;
using Projet_ProgSys.ViewModel;
using System.Threading.Tasks;
using System.Threading;



namespace Projet_ProgSys.View
{
    /// <summary>
    /// Logique d'interaction pour BackupPage.xaml
    /// </summary>
    public partial class BackupPage : Page
    {
        bool IsPaused = false;

        List<BackupModel> backupModels = DatabaseViewModel.ListBackups();

        public BackupPage()
        {
            InitializeComponent();
            /*
            foreach (BackupModel backupModel in backupModels)
            {
                backups.Items.Add(backupModel);
            }*/
            backups.ItemsSource = backupModels;
            Thread thread1 = new Thread(UpdateDatagrid);
            thread1.Start();
        }

        public void RefreshDatagrid()
        {
            this.Dispatcher.Invoke(() =>
            {
                backups.ItemsSource = null;
                backups.ItemsSource = backupModels;
            });
        }

        public void UpdateDatagrid()
        {
            while (true)
            {
                Thread.Sleep(10000);
                List<BackupModel> backupModelsCopy = backupModels;
                List<StateLogModel> backupModelsState = BackupViewModelRewrite.ListState();

                foreach(BackupModel backupModel in backupModels)
                {
                    string finalstatus;
                    var index = backupModelsState.FindIndex(o => o.Name == backupModel.Name);
                    string test = backupModelsState[index].State;
                    if (index >= 0)
                    {
                        if (backupModelsState[index].State != "PAUSED" && backupModelsState[index].State != "FINISHED" && backupModelsState[index].State != "STOPPED")
                        {
                            double percentage = 100 - (((double)backupModelsState[index].TotalFilesSizeRemaining / backupModelsState[index].TotalFilesSize) * 100);
                            finalstatus = String.Format("{0:0}%", percentage);
                        }
                        else
                        {
                            finalstatus = backupModelsState[index].State;

                        }
                        backupModel.Status = finalstatus;
                    }
                    
                }

                RefreshDatagrid();
            }
            
        }

        public void NewBackup(object sender, RoutedEventArgs e)
        {
            PromptNewBackup window1 = new PromptNewBackup();
            if (window1.ShowDialog() == true)
            {
                BackupModel backup = window1.Answer;
                backup.Name = DatabaseViewModel.IsAlreadyInserted(backup.Name);
                DatabaseViewModel.CreateBackup(backup);
                BackupViewModelRewrite.CreateStateLog();
                backupModels.Add(backup);
                RefreshDatagrid();
            }
        }

        public void EditBackup(object sender, RoutedEventArgs e)
        {
            if((BackupModel)backups.SelectedItem != null)
            {
                var oldbackup = (BackupModel)backups.SelectedItem;
                string oldname = oldbackup.Name;
                PromptEditBackup window1 = new PromptEditBackup(oldbackup);
                if (window1.ShowDialog() == true)
                {
                    BackupModel backup = window1.Answer;
                    DatabaseViewModel.UpdateBackup(backup, oldname);
                    BackupViewModelRewrite.CreateStateLog();
                    backupModels.Remove(oldbackup);
                    backupModels.Add(backup);
                    RefreshDatagrid();
                }
            }
        }

        public void DeleteBackup(object sender, RoutedEventArgs e)
        {
            if ((BackupModel)backups.SelectedItem != null)
            {
                var oldbackup = (BackupModel)backups.SelectedItem;
                PromptVerification window1 = new PromptVerification();
                if (window1.ShowDialog() == true)
                {
                    DatabaseViewModel.DeleteBackup(oldbackup);
                    backupModels.Remove(oldbackup);
                    RefreshDatagrid();
                }
            }
        }

        public void PauseResume(object sender, RoutedEventArgs e)
        {
            if ((BackupModel)backups.SelectedItem != null)
            {
                var oldbackup = (BackupModel)backups.SelectedItem;
                BackupViewModelRewrite.SetStatus(oldbackup);
            }
        }

        public void PauseResumeAll(object sender, RoutedEventArgs e)
        {
            if (DatabaseViewModel.NumberBackups() != 0)
            {
                List<BackupModel> ListOfBackups = DatabaseViewModel.ListBackups();
                foreach(BackupModel oldbackup in ListOfBackups)
                {
                    if (IsPaused)
                    {
                        BackupViewModelRewrite.SetResume(oldbackup);
                        BackupViewModelRewrite.UpdateStateLog(oldbackup.Name, "RUNNING");
                        IsPaused = false;
                    }
                    else
                    {
                        BackupViewModelRewrite.SetPause(oldbackup);
                        BackupViewModelRewrite.UpdateStateLog(oldbackup.Name, "PAUSED");
                        IsPaused = true;
                    }
                }
            }
        }

        public void StartBackup(object sender, RoutedEventArgs e)
        {
            if ((BackupModel)backups.SelectedItem != null)
            {
                var oldbackup = (BackupModel)backups.SelectedItem;
                _ = Task.Run(() => BackupViewModelRewrite.StartBackup(oldbackup));
            }
        }

        public void StartAllBackup(object sender, RoutedEventArgs e)
        {
            if(DatabaseViewModel.NumberBackups() != 0)
            {
                List<BackupModel> ListOfBackups = DatabaseViewModel.ListBackups();
                foreach(BackupModel backup in ListOfBackups)
                {
                    _ = Task.Run(() => BackupViewModelRewrite.StartBackup(backup));
                }
            }
        }

        public void StopBackup(object sender, RoutedEventArgs e)
        {
            if ((BackupModel)backups.SelectedItem != null)
            {
                var oldbackup = (BackupModel)backups.SelectedItem;
                BackupViewModelRewrite.StopBackup(oldbackup);
                BackupViewModelRewrite.UpdateStateLog(oldbackup.Name, "STOPPED");
            }
        }

        public void StopAllBackup(object sender, RoutedEventArgs e)
        {
            if (DatabaseViewModel.NumberBackups() != 0)
            {
                List<BackupModel> ListOfBackups = DatabaseViewModel.ListBackups();
                foreach (BackupModel oldbackup in ListOfBackups)
                {
                    BackupViewModelRewrite.StopBackup(oldbackup);
                    BackupViewModelRewrite.UpdateStateLog(oldbackup.Name, "STOPPED");
                }
            }
        }
    }
}
