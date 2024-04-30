using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Service;

namespace ClientWPFGUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        static private Service.Service Service =
            new Service.Service("localhost",5250);

        static private UserSettingsManager userSettingsManager = new UserSettingsManager();
        public MainWindow()
        {
            InitializeComponent();
            this.UserNameBox.Text = userSettingsManager.Username;

            SystemResponses(0); //Say "Hi" to new user

            DispatcherTimer timer = new DispatcherTimer(); //Always update ListView
            timer.Tick += Timer_Tick;
            timer.Interval = new TimeSpan(0, 0, 1);
            timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            UpdateView();
        }

        private void SystemResponses(int a)
        {
            switch(a)
            {
                case 0:
                    Service.PostAsyncDesktop
                    (
                        new ModelsLibrary.Messages.MessageRequest
                        (
                            $"Welcome, {UserNameBox.Text}", "System"
                        )
                    );
                    break;

                case 1:
                    Service.PostAsyncDesktop
                    (
                        new ModelsLibrary.Messages.MessageRequest
                        (
                            $"User {UserNameBox.Text} changed username", "System"
                        )
                    );
                    break;
            }
        }

        private void UserNameUse_Click(object sender, RoutedEventArgs e)
        {
            UsernameDialog dlg = new UsernameDialog(userSettingsManager.Username);
            dlg.ShowDialog();
            
            UserNameBox.Text = dlg.username;
            userSettingsManager.SetUsername(dlg.username);
            SystemResponses(1); //tell everyone that user changed name
        }

        private void SendMessage_Click(object sender, RoutedEventArgs e)
        {
            SendMessageToServer();
        }

        private void SendMessageToServer()
        {
            try
            {
                Service.SetNewAddress(this.IPBox.Text, this.PortBox.Text);
            }
            catch
            {
                MessageBox.Show("Неверный адрес сервера", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }



            try
            {
                Service.PostAsyncDesktop
                (
                    new ModelsLibrary.Messages.MessageRequest
                    (
                        this.Input.Text, userSettingsManager.Username
                    )
                );
                Input.Text = string.Empty;
            }
            catch (System.Net.Http.HttpRequestException)
            {
                MessageBox.Show("Ошибка сервера\nПопробуйте обратиться к администратору", "Плохой запрос на сервер", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }



        private void UpdateView()
        {
            Task.Run(() =>
            {
                Thread.Sleep(2000);
                Service.GetAsyncDesktop();


                bool serverFailed=false;
                serverFailed = Task.Run<bool>(() =>
                {
                    for (byte i=0;i!=50;++i)
                    {
                        Thread.Sleep(100);
                        if (Service.Messages.Count != 0)
                            return false;
                    }
                    return Service.Messages.Count == 0;
                }).Result;



                while (Service.Messages.Count == 0)
                    if (serverFailed) break;

                if (Service.Messages.Count == 0)
                {
                    MessageBox.Show("Нет элементов в списке сообщений", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                //this.listViewMessages.Items.Clear();
                Dispatcher.Invoke(new Action(() =>
                {
                    this.listViewMessages.Items.Clear();
                    foreach (var mItem in  Service.Messages)
                    {
                        ListViewItem item = new ListViewItem();
                        var data = new
                        { 
                            ID = mItem.Id, 
                            User = mItem.Username, 
                            Time = mItem.DateTime.ToShortDateString()+" "+ mItem.DateTime.ToShortTimeString()+":"+ mItem.DateTime.Second,
                            Message = mItem.Content 
                        };
                        item.Content = data;
                        this.listViewMessages.Items.Add(item);
                    } 
                }));
            });
        }


        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!char.IsDigit(e.Text, e.Text.Length - 1))
                e.Handled = true;
        }

        private void Input_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && Input.Text != "") SendMessageToServer();
        }
    }
}