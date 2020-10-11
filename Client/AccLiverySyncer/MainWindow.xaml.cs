using AccLiverySyncer.Model;
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

namespace AccLiverySyncer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            LoadConfig();

            InitAsync();
            
        }



        private async void InitAsync()
        {
            await TryLoginAsync();
            await UpdateLiveryListAsync();
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
            }
            else
            {
                using (StreamReader r = new StreamReader(file))
                {
                    string json = r.ReadToEnd();
                    Model.Settings sets = JsonConvert.DeserializeObject<Model.Settings>(json);

                    Box_ACCPath.Text = sets.ACCPath;
                    Box_Discord.Text = sets.DiscordId;
                    Box_Steam.Text = sets.SteamId;
                    Box_Password.Text = sets.Token;
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
                SteamId = Box_Steam.Text,
                ACCPath = Box_ACCPath.Text,
                Token = Box_Password.Text
            };

            File.WriteAllText(file, JsonConvert.SerializeObject(sets));
        }


        

        private async Task TryLoginAsync()
        {
            long discordId;
            bool discordIdValid = long.TryParse(Box_Discord.Text, out discordId);

            long steamId;
            bool steamIdValid = long.TryParse(Box_Steam.Text, out steamId);

            var token = Box_Password.Text;


            // check for misformed/missing discordId

            Lbl_Info.Content = "Logging in...";

            var status = await Connector.Login(discordId, token);

            if (status == HttpStatusCode.OK)
            {
                Lbl_Info.Content = "Logged In";
            }
            else if(status == HttpStatusCode.Forbidden)
            {
                Lbl_Info.Content = "Invalid Credentials. Keep the token empty if you need to register";
            }
            else if(status == 0)
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
            LiveryController.liveries = await Connector.GetLiveries();

            LiveryController.RefreshInstalled(Box_ACCPath.Text);
            LiveryController.RefreshUpdateNeeded(Box_ACCPath.Text);

            List_Liveries.ItemsSource = LiveryController.liveries;
        }


        private async void Button_Validate_Click(object sender, RoutedEventArgs e)
        {
            await TryLoginAsync();
            SaveConfig(); // update config
        }


        private async void Button_Register_Click(object sender, RoutedEventArgs e)
        {
            long discordId;
            bool discordIdValid = long.TryParse(Box_Discord.Text, out discordId);

            long steamId;
            bool steamIdValid = long.TryParse(Box_Steam.Text, out steamId);


            if (discordIdValid)
            {
                Lbl_Info.Content = "Sending register request...";

                string token = Box_Password.Text;

                Tuple<HttpStatusCode, string> response = await Connector.Register(discordId);

                HttpStatusCode code = response.Item1;
                string rcvToken = response.Item2;

                if(code == HttpStatusCode.Created)
                {
                    Box_Password.Text = token;
                    Lbl_Info.Content = "Logged In";
                    SaveConfig(); // update the config
                }
                else if(code == HttpStatusCode.Conflict)
                {
                    // display error
                    Lbl_Info.Content = "Credentials already in use. Contact the developer if you lost your token.";
                }
            }
            else
            {
                Lbl_Info.Content = "Please enter your Discord Id";
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

            
            string err = await LiveryController.UpdateAllRemoteLiveries(Box_ACCPath.Text);
            Lbl_Info.Content = err;


        }



        private async void Btn_Down_Selection_Click(object sender, RoutedEventArgs e)
        {
            var item = List_Liveries.SelectedItem;
            
            if(item is Livery liv)
            {
                Lbl_Info.Content = "Downloading...";

                string err = await LiveryController.UpdateRemoteLivery(Box_ACCPath.Text, List_Liveries.SelectedIndex);
                Lbl_Info.Content = err;
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

                    Uri decalUri = new Uri(box.Text + "/decals.png");
                    Uri sponsorUri = new Uri(box.Text + "/sponsors.png");
                    // load the images

                    if (File.Exists(decalUri.LocalPath)){
                        Img_decal.Source = new BitmapImage(decalUri);
                    }

                    if (File.Exists(sponsorUri.LocalPath))
                    {
                        Img_sponsor.Source = new BitmapImage(sponsorUri);
                    }

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


                    string accPath = Box_ACCPath.Text;

                    if (Directory.Exists(accPath + "/" + liv.Name))
                    {

                        Uri decalUri = new Uri(accPath + "/" + liv.Name + "/decals.png");
                        Uri sponsorUri = new Uri(accPath + "/" + liv.Name + "/sponsors.png");
                        // load the images

                        if (File.Exists(decalUri.LocalPath))
                        {
                            Img_list_decal.Source = new BitmapImage(decalUri);
                        }

                        if (File.Exists(sponsorUri.LocalPath))
                        {
                            Img_list_sponsor.Source = new BitmapImage(sponsorUri);
                        }

                    }
                }
            }
        }
    }
}
