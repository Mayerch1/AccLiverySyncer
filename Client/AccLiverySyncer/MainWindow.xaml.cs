﻿using AccLiverySyncer.Model;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Mayerch1.GithubUpdateCheck;
using System.Windows.Media;

namespace AccLiverySyncer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        GithubUpdateCheck repo = new GithubUpdateCheck("Mayerch1", "AccLiverySyncer");
        const string version = "0.1.0.0";




        public MainWindow()
        {
            InitializeComponent();
            LoadConfig();


            // show settings screen if token is empty
            if (!String.IsNullOrWhiteSpace(Box_Password.Text))
            {
                InitAsync();
            }
        }



        private async void InitAsync()
        {
            await TryLoginAsync();
            await UpdateLiveryListAsync();

            if(await repo.IsUpdateAvailableAsync(version, VersionChange.Minor))
            {
                Lbl_Info.Content = "Update available";
                TabItem_Update.Visibility = Visibility.Visible;
            }
        }

        public void LoadConfig()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "/ACCLiverySyncer";
            var file = appData + "/Settings.json";


            if (!File.Exists(file))
            {
                // assume default acc location
                var accPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                accPath += @"\Assetto Corsa Competizione\Customs\Liveries";
                Box_ACCPath.Text = accPath;
                Box_Intro_ACCPath.Text = accPath;

                TabItem_Introduction.Visibility = Visibility.Visible;
                Tab_Main.SelectedIndex = 3;
            }
            else
            {
                using (StreamReader r = new StreamReader(file))
                {
                    string json = r.ReadToEnd();
                    Model.Settings sets = JsonConvert.DeserializeObject<Model.Settings>(json);

                    Box_ACCPath.Text = sets.ACCPath;
                    Box_Discord.Text = sets.DiscordId;
                    //Box_Steam.Text = sets.SteamId;
                    Box_Password.Text = sets.Token;

                    Box_Host.Text = sets.Host;
                    Connector.hostUri = sets.Host;
                }

            }
        }


        public void SaveConfig()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "/ACCLiverySyncer";
            var file = appData + "/Settings.json";


            if (!Directory.Exists(appData))
            {
                Directory.CreateDirectory(appData);
            }

            Model.Settings sets = new Settings
            {
                DiscordId = Box_Discord.Text,
                //SteamId = Box_Steam.Text,
                ACCPath = Box_ACCPath.Text,
                Token = Box_Password.Text,
                Host = Box_Host.Text

            };

            File.WriteAllText(file, JsonConvert.SerializeObject(sets));
        }



        private async Task TryLoginAsync()
        {
            string discordId = Box_Discord.Text;

            //long steamId;
            //bool steamIdValid = long.TryParse(Box_Steam.Text, out steamId);

            var token = Box_Password.Text;


            // check for misformed/missing discordId

            Lbl_Info.Content = "Logging in...";


            HttpStatusCode status;
            try
            {
                status = await Connector.Login(discordId, token);
            }
            catch (System.UriFormatException)
            {
                Lbl_Info.Content = "Invalid Host Url";
                return;
            }

            if (status == HttpStatusCode.OK)
            {
                Lbl_Info.Content = "Logged In";
            }
            else if(status == HttpStatusCode.Unauthorized || status == HttpStatusCode.NotFound)
            {
                // do not disclose reason for no login
                Lbl_Info.Content = "Invalid Credentials. Keep the token empty if you need to register";
            }
            else if(status == HttpStatusCode.BadGateway || status == 0)
            {
                Lbl_Info.Content = "Cannot reach the server";
            }
            else
            {
                Lbl_Info.Content = "Unexpected error on login (" + status + ") . Please contact the developer";
            }
        }


       

        private async Task UpdateLiveryListAsync()
        {

            try
            {
                LiveryController.liveries = await Connector.GetLiveries();
            }
            catch (System.UriFormatException)
            {
                Lbl_Info.Content = "Invalid Host Url";
                return;
            }
            

            LiveryController.RefreshInstalled(Box_ACCPath.Text);
            LiveryController.RefreshUpdateNeeded(Box_ACCPath.Text);

            List_Liveries.ItemsSource = LiveryController.liveries;
        }



        private void SetImage(Image img, string imagePath)
        {
            if (File.Exists(imagePath))
            {
                Uri uri = new Uri(imagePath);

                if (File.Exists(uri.LocalPath))
                {
                    BitmapImage bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.UriSource = uri;
                    bmp.EndInit();


                    img.Source = bmp;
                }
            }
        }


        /// <summary>
        /// clears all image viewers, this allows deletion of locked images
        /// </summary>
        private void ReleaseImageLocks()
        {
            // TODO: release resource correct for deletion
            //Img_decal.Source = null;
            //Img_list_decal.Source = null;
            //Img_sponsor.Source = null;
            //Img_list_sponsor.Source = null;
        }



        private async void Button_Validate_Click(object sender, RoutedEventArgs e)
        {
            await TryLoginAsync();
            SaveConfig(); // update config
        }


        private async void Button_Register_Click(object sender, RoutedEventArgs e)
        {
            string discordId = Box_Discord.Text;

            if(discordId.Length > 40)
            {
                Lbl_Info.Content = "Username too long (40 chars max)";
                return;
            }
            else if (System.Text.Encoding.UTF8.GetByteCount(discordId) != discordId.Length)
            {
                Lbl_Info.Content = "Only ASCII characters allowed";
                return;
            }

             
            
            Lbl_Info.Content = "Sending register request...";

            string token = Box_Password.Text;

            Tuple<HttpStatusCode, string> response = await Connector.Register(discordId);

            HttpStatusCode code = response.Item1;
            string rcvToken = response.Item2;

            if(code == HttpStatusCode.Created)
            {
                Box_Password.Text = rcvToken;
                Lbl_Info.Content = "Logged In"; // register method does an automatic login
                SaveConfig(); // update the config
            }
            else if(code == HttpStatusCode.Conflict)
            {
                // display error
                Lbl_Info.Content = "Credentials already in use. Contact the developer if you lost your token.";
            }
            else
            {
                Lbl_Info.Content = "Unexpected error on login (" + code + ") . Please contact the developer";
            }
            
            
        }


        private void Box_Password_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (String.IsNullOrWhiteSpace(Box_Password.Text))
            {
                Btn_Register.IsEnabled = true;
                Btn_Validate.IsEnabled = false;
            }
            else
            {
                Btn_Register.IsEnabled = false;
                Btn_Validate.IsEnabled = true;
            }
        }


        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            Lbl_Info.Content = "Fetching from server...";

            await UpdateLiveryListAsync();

            Lbl_Info.Content = "";
        }



        private async void Btn_Down_All_Click(object sender, RoutedEventArgs e)
        {
            Lbl_Info.Content = "Downloading all liveries, this can take a while...";


            ReleaseImageLocks();

            string err = await LiveryController.UpdateAllRemoteLiveries(Box_ACCPath.Text);
            Lbl_Info.Content = err;


            //  this will update the installed/update needed columns
            LiveryController.RefreshInstalled(Box_ACCPath.Text);
            LiveryController.RefreshUpdateNeeded(Box_ACCPath.Text);
            List_Liveries.ItemsSource = LiveryController.liveries;
        }



        private async void Btn_Down_Selection_Click(object sender, RoutedEventArgs e)
        {
            var item = List_Liveries.SelectedItem;
            
            if(item is Livery liv)
            {
                // release access to files
                ReleaseImageLocks();

                Lbl_Info.Content = "Downloading...";

                string err = await LiveryController.UpdateRemoteLivery(Box_ACCPath.Text, List_Liveries.SelectedIndex);
                Lbl_Info.Content = err;


                //  this will update the installed/update needed columns
                await UpdateLiveryListAsync();
            }
            else
            {
                Lbl_Info.Content = "Please select an entry from the list";
            }

        }


        private async void Button_DeleteLivery_Click(object sender, RoutedEventArgs e)
        {
            var item = List_Liveries.SelectedItem;

            if (item is Livery liv)
            {
                // image can stay locked, as no local files are deleted

                Lbl_Info.Content = "Deleting remote...";

                string err = await LiveryController.DeleteRemoteLivery(List_Liveries.SelectedIndex);
                Lbl_Info.Content = err;

                // silently refresh the list
                await UpdateLiveryListAsync();
            }
            else
            {
                Lbl_Info.Content = "Please select an entry from the list";
            }
        }


        private async void Btn_Upload_Livery_Click(object sender, RoutedEventArgs e)
        {
            Lbl_Info.Content = "Uploading...";

            string err = await LiveryController.UploadLivery(Box_Livery_Upload.Text);
            Lbl_Info.Content = err;

            // silently refresh the list
            await UpdateLiveryListAsync();

        }


        private void Button_Select_Livery_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;
            dialog.InitialDirectory = Box_ACCPath.Text;

            if(dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                Box_Livery_Upload.Text = dialog.FileName;
            }

        }

        private void Box_Livery_Upload_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox box)
            {
                string accPath = Box_ACCPath.Text;

                if (Directory.Exists(box.Text))
                {

                    SetImage(Img_decal, box.Text + "/decals.png");
                    SetImage(Img_sponsor, box.Text + "/sponsors.png");
                }
            }
        }

        private void List_Liveries_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(sender is ListBox list)
            {
                var selected = list.SelectedItem;
                if (selected is Livery liv)
                {
                    Btn_Down_Selection.IsEnabled = true;
                    Btn_Delete_Selection.IsEnabled = true;

                    string accPath = Box_ACCPath.Text;

                    SetImage(Img_list_decal, accPath + "/" + liv.Name + "/decals.png");
                    SetImage(Img_list_sponsor, accPath + "/" + liv.Name + "/sponsors.png");
                }
                else
                {
                    Btn_Down_Selection.IsEnabled = false;
                    Btn_Delete_Selection.IsEnabled = false;
                }
            }
        }

        private void Box_Host_TextChanged(object sender, TextChangedEventArgs e)
        {
            if(sender is TextBox box)
            {
                // test for valid http or https url
               
                if (Uri.IsWellFormedUriString(box.Text, UriKind.Absolute))
                {
                    Connector.hostUri = box.Text;
                    Lbl_Info.Content = "";
                }
                else
                {
                    Lbl_Info.Content = "Invalid Url";
                }
            }
        }



        private void Box_Intro_Host_TextChanged(object sender, TextChangedEventArgs e)
        {
            Box_Host.Text = ((TextBox)sender).Text;
            Stack_Intro_ACCPath.Visibility = Visibility.Visible;
            Dock_Intro_FinishButton.Visibility = Visibility.Visible;
        }

        private void Button_Open_Update_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("explorer.exe", "https://github.com/Mayerch1/AccLiverySyncer/releases/latest");
            
        }

        private void Button_OpenWiki_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("explorer.exe", "https://github.com/Mayerch1/AccLiverySyncer/wiki");
        }


        private void Button_Intro_Register_Click(object sender, RoutedEventArgs e)
        {
            Stack_Intro_HostUrl.Visibility = Visibility.Visible;
            Button_Register_Click(sender, e);
        }


        private void Box_Intro_Discord_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox box)
            {

                // if has only acsii characters
                if (System.Text.Encoding.UTF8.GetByteCount(box.Text) == box.Text.Length)
                {
                    Stack_Intro_Register.Visibility = Visibility.Visible;
                    Box_Discord.Text = box.Text;
                }
                else if(box.Text.Length > 40)
                {
                    Lbl_Info.Content = "Username cannot exceed 40 characters";
                }
                else if(box.Text.Length < 5)
                {
                    Lbl_Info.Content = "Username must have at least 5 characters";
                }
                else
                {
                    Stack_Intro_Register.Visibility = Visibility.Hidden;
                    Lbl_Info.Content = "Invalid characters in username";
                }
            }
        }

        private void Button_Finish_Setup_Click(object sender, RoutedEventArgs e)
        {
            TabItem_Introduction.Visibility = Visibility.Hidden;
            Tab_Main.SelectedIndex = 0;
            SaveConfig();
        }

        private void Box_Intro_ACCPath_TextChanged(object sender, TextChangedEventArgs e)
        {
            Box_ACCPath.Text = ((TextBox)sender).Text;
        }
    }
}
