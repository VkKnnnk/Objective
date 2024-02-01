using PriceService.Models;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.IO;
using System.Text;
using MimeKit;
using MailKit.Net.Smtp;
using System.Windows.Threading;
using System.Net.Http;
using System.Net.Sockets;

namespace PriceService.View_Models
{
    public class MainWindowVM : INotifyPropertyChanged
    {
        #region Properties
        private string _inputUrl = String.Empty;
        public string InputUrl
        {
            get { return _inputUrl; }
            set
            {
                _inputUrl = value;
                OnPropertyChanged("InputUrl");
            }
        }
        private string _inputEmail = String.Empty;
        public string InputEmail
        {
            get { return _inputEmail; }
            set
            {
                _inputEmail = value;
                OnPropertyChanged("InputEmail");
            }
        }
        private string _email;
        public string Email
        {
            get { return _email; }
            set
            {
                _email = value;
                OnPropertyChanged("Email");
            }
        }
        private ObservableCollection<Apartment> _apartmentsItemSource;
        public ObservableCollection<Apartment> ApartmentsItemSource
        {
            get { return _apartmentsItemSource; }
            set
            {
                _apartmentsItemSource = value;
                OnPropertyChanged("ApartmentsItemSource");
            }
        }
        public ObservableCollection<string> LogList { get; set; }
        public ObservableCollection<Apartment> Apartments { get; set; }
        private Apartment _selectedApartment;
        public Apartment SelectedApartment
        {
            get { return _selectedApartment; }
            set
            {
                _selectedApartment = value;
                OnPropertyChanged("SelectedItem");
            }
        }
        private List<Apartment> DbApartments { get; set; }
        DispatcherTimer AppTimer;
        #endregion
        public MainWindowVM()
        {
            LogList = new ObservableCollection<string>();
            Apartments = new ObservableCollection<Apartment>();
            ApartmentsItemSource = Apartments;
            if (File.Exists("email.txt"))
            {
                using (FileStream fs = new FileStream("email.txt", FileMode.Open))
                {
                    byte[] buffer = new byte[fs.Length];
                    fs.Read(buffer, 0, buffer.Length);
                    Email = Encoding.Default.GetString(buffer);
                }
            }
            AppTimer = new DispatcherTimer();
            AppTimer.Interval = new TimeSpan(0, 0, 10);
            AppTimer.Tick += AppTimer_Tick;
        }
        #region Commands
        private ICommand _clearLogCommand;
        public ICommand ClearLogCommand =>
             _clearLogCommand ??= new RelayCommand((param) =>
            {
                LogList.Clear();
                LogList.Add("Log очищен");
            });
        private ICommand _setMonitoringStatusCommand;
        public ICommand SetMonitoringStatusCommand =>
             _setMonitoringStatusCommand ??= new RelayCommand((param) =>
            {
                SetMonitorngStatus(SelectedApartment);
            }, (param) =>
             {
                 if (SelectedApartment is not null)
                     return true;
                 else
                     return false;
             }
            );
        private ICommand _unsetMonitoringStatusCommand;
        public ICommand UnsetMonitoringStatusCommand =>
             _unsetMonitoringStatusCommand ??= new RelayCommand((param) =>
            {
                UnsetMonitoringStatus(SelectedApartment);
            }, (param) =>
             {
                 if (SelectedApartment is not null)
                     return true;
                 else
                     return false;
             });
        private ICommand _showDBApartmentsCommand;
        public ICommand ShowDBApartmentsCommand =>
             _showDBApartmentsCommand ??= new RelayCommand(async (param) =>
            {
                List<Apartment> apartments = await LoadCurrentDbApartments();
                if (apartments is not null && apartments.Count > 0)
                {
                    ApartmentsItemSource = new ObservableCollection<Apartment>(apartments);
                    LogList.Add("Выведены остлеживаемые квартиры");
                }
            });
        private ICommand _showParsedApartmentsCommand;
        public ICommand ShowParsedApartmentsCommand =>
             _showParsedApartmentsCommand ??= new RelayCommand((param) =>
            {
                ApartmentsItemSource = Apartments;
                LogList.Add("Выведены результаты парсинга");
            }
            );
        private ICommand _startMonitoringCommand;
        public ICommand StartMonitoringCommand =>
             _startMonitoringCommand ??= new RelayCommand((param) =>
            {
                LogList.Add("Слежка за изменением цены запущена");
                AppTimer.Start();
            }, (param) =>
            {
                if (String.IsNullOrEmpty(Email))
                    return false;
                else
                    return true;
            });
        private ICommand _getApartmentCommand;
        public ICommand GetApartmentCommand =>
             _getApartmentCommand ??= new RelayCommand(GetApartment, (param) =>
             {
                 if (String.IsNullOrEmpty(InputUrl))
                     return false;
                 else
                     return true;
             });
        private ICommand _setEmailCommand;
        public ICommand SetEmailCommand =>
             _setEmailCommand ??= new RelayCommand(SetEmail, (param) =>
             {
                 if (String.IsNullOrEmpty(InputEmail))
                     return false;
                 else
                     return true;
             });
        #endregion
        #region CommandsMethods
        private async void GetApartment(object param)
        {
            LogList.Add("Проверяю подключение к базе данных...");
            if (await DBWorker.CheckDBConnection())
            {
                List<Apartment> dbApartments = await DBWorker.GetApartments();
                Match match = Regex.Match(InputUrl, @"^https?://prinzip\.[^/]+/apartments/[^/]+/(\d+)/?$");
                if (match.Success)
                {
                    if (dbApartments.Select(x => x.Id).Contains(int.Parse(match.Groups[1].Value)))
                    {
                        LogList.Add("Квартира уже остлеживается");
                    }
                    else if (Apartments.Select(x => x.Id).Contains(int.Parse(match.Groups[1].Value)))
                    {
                        LogList.Add("Парсинг для этой квартиры уже выполнен");
                    }
                    else
                    {
                        Apartment apartment = await ParseApartment(InputUrl);
                        if (apartment is not null)
                            Apartments.Add(apartment);
                    }
                }
                else
                    LogList.Add("Неверный формат url");
            }
            else
                LogList.Add("Ошибка подключения к базе данных");
        }
        private async void SetEmail(object param)
        {
            string emalPattern = @"^[-\w\d_]+(?:\.[-\w\d_]+)*@[\w\d_]+(?:\.[\w\d_][-\w\d_]+)+$";
            if (Regex.IsMatch(InputEmail, emalPattern))
            {
                Email = InputEmail;
                try
                {
                    await SendMessageToEmail("Почта привязана",
                    "На эту почту будут приходить уведомления об изменении цен на отслеживаемые квартиры.");
                    MessageBox.Show("Проверьте ваш почтовый ящик. На него должно прийти письмо от Prinzip parser." +
                        "\nВ случае, если письмо не пришло, проверьте введенный почтовый адрес и повторите попытку.", "Сообщение");

                    LogList.Add("Почта добавлена");
                    var choice = MessageBox.Show("Желаете сохранить почтовый адрес?",
                        "Сообщение", MessageBoxButton.YesNo);
                    if (choice == MessageBoxResult.Yes)
                    {
                        SaveEmailToFile();
                    }
                }
                catch (SocketException)
                {
                    Email = String.Empty;
                    LogList.Add("Отсутствует подключение к сети");
                }
                catch (Exception)
                {
                    Email = String.Empty;
                    LogList.Add("Ошибка привязки почты");
                }

            }
            else
                LogList.Add(@"Формат введеной почты не соотвествует формату: ""example@gmail.com"" ");
        }
        #endregion
        #region Methods
        private void AppTimer_Tick(object sender, EventArgs e) => CheckPriceChanges();
        private async void CheckPriceChanges()
        {
            DbApartments = await LoadCurrentDbApartments();
            if (DbApartments is not null && DbApartments.Count > 0)
                DbApartments = DbApartments.Where(x => x.IsMonitorng == true).ToList();
            if (DbApartments.Count > 0)
            {
                foreach (var dbApartment in DbApartments)
                {
                    LogList.Add(dbApartment.Url);
                    Apartment parsedApartment = await ParseApartment(dbApartment.Url);
                    if (parsedApartment.Price != dbApartment.Price && parsedApartment.PriceMortgageMonthly != dbApartment.PriceMortgageMonthly)
                    {
                        await SendMessageToEmail("Цена изменилась",
                            $"Квартира:\n{dbApartment.Name}\n{dbApartment.Url}\n" +
                            $"Прошлая цена: {dbApartment.Price} руб.\n" +
                            $"Текущая цена: {parsedApartment.Price} руб.\n" +
                            $"Прошлая цена ипотеки: {dbApartment.PriceMortgageMonthly} руб./мес.\n" +
                            $"Текущая цена ипотеки: {parsedApartment.PriceMortgageMonthly} руб./мес.");
                        LogList.Add("Проверяю подключение к базе данных...");
                        if (await DBWorker.CheckDBConnection())
                        {
                            await DBWorker.UpdatePriceApartment(dbApartment, parsedApartment.Price);
                            await DBWorker.UpdatePriceMortgageMonthlyApartment(dbApartment, parsedApartment.PriceMortgageMonthly);
                        }
                        else
                        {
                            LogList.Add("Не удалось подключиться к базе данных");
                            LogList.Add("Измененная цена не обновится в БД");
                        }
                    }
                    else if (parsedApartment.Price != dbApartment.Price)
                    {
                        await SendMessageToEmail("Цена изменилась",
                            $"Квартира:\n{dbApartment.Name}\n{dbApartment.Url}\n" +
                            $"Прошлая цена: {dbApartment.Price} руб.\n" +
                            $"Текущая цена: {parsedApartment.Price} руб.\n");
                        LogList.Add("Проверяю подключение к базе данных...");
                        if (await DBWorker.CheckDBConnection())
                            await DBWorker.UpdatePriceApartment(dbApartment, parsedApartment.Price);
                        else
                        {
                            LogList.Add("Не удалось подключиться к базе данных");
                            LogList.Add("Измененная цена не обновится в БД");
                        }
                    }
                    else if (parsedApartment.PriceMortgageMonthly != dbApartment.PriceMortgageMonthly)
                    {
                        await SendMessageToEmail("Цена изменилась",
                            $"Квартира:\n{dbApartment.Name}\n{dbApartment.Url}\n" +
                            $"Прошлая цена ипотеки: {dbApartment.PriceMortgageMonthly} руб./мес.\n" +
                            $"Текущая цена ипотеки: {parsedApartment.PriceMortgageMonthly} руб./мес.");
                        LogList.Add("Проверяю подключение к базе данных...");
                        if (await DBWorker.CheckDBConnection())
                            await DBWorker.UpdatePriceMortgageMonthlyApartment(dbApartment, parsedApartment.PriceMortgageMonthly);
                        else
                        {
                            LogList.Add("Не удалось подключиться к базе данных");
                            LogList.Add("Измененная цена не обновится в БД");
                        }
                    }
                }
            }
            else
            {
                LogList.Add("Отсутсвуют квартиры для отслеживания");
            }
        }
        private async Task<List<Apartment>> LoadCurrentDbApartments()
        {
            List<Apartment> dbApartments = new();
            LogList.Add("Проверяю подключение к базе данных...");
            if (await DBWorker.CheckDBConnection())
                dbApartments = await DBWorker.GetApartments();
            else
            {
                LogList.Add("Ошибка подключения к базе данных");
                dbApartments = DbApartments;
                if (dbApartments is not null && dbApartments.Count > 0)
                    LogList.Add("Будут использованы данные, полученные от последнего подключения");
            }
            return dbApartments;
        }
        private async Task SendMessageToEmail(string subject, string body)
        {
            using var emailMessage = new MimeMessage();

            emailMessage.From.Add(new MailboxAddress("Prinzip parser", "vkt.knnnk@gmail.com"));
            emailMessage.To.Add(new MailboxAddress("", Email));
            emailMessage.Subject = subject;
            emailMessage.Body = new TextPart("Plain") { Text = body };
            try
            {
                using (var client = new SmtpClient())
                {
                    await client.ConnectAsync("smtp.gmail.com", 25, false);
                    await client.AuthenticateAsync("vkt.knnnk@gmail.com", "teeb borp lasu wzek");
                    await client.SendAsync(emailMessage);
                    await client.DisconnectAsync(true);
                }
            }
            catch (SocketException ex)
            {
                throw ex;
            }
            catch (Exception ex)
            {
                LogList.Add("Ошибка при отправке сообщения");
                LogList.Add(ex.Message);
            }
        }

        private async void SaveEmailToFile()
        {
            using (FileStream fs = new FileStream("email.txt", FileMode.Create))
            {
                byte[] buffer = Encoding.Default.GetBytes(Email);
                await fs.WriteAsync(buffer, 0, buffer.Length);
            }
        }
        private async void UnsetMonitoringStatus(Apartment apartment)
        {
            LogList.Add("Проверяю подключение к базе данных...");
            if (await DBWorker.CheckDBConnection())
            {
                if (await CheckMonitoringStatus(apartment))
                {
                    await DBWorker.UpdateMonitoringApartment(apartment, false);
                    LogList.Add("Слежка отключена");
                }
                else
                {
                    LogList.Add("Слежка еще не установлена");
                }
            }
            else
                LogList.Add("Ошибка подключения к базе данных");

        }
        private async void SetMonitorngStatus(Apartment apartment)
        {
            LogList.Add("Проверяю подключение к базе данных...");
            if (await DBWorker.CheckDBConnection())
            {
                if (!await CheckMonitoringStatus(apartment))
                {
                    List<Apartment> dbApartments = await DBWorker.GetApartments();
                    if (dbApartments.Count > 0)
                    {
                        if (dbApartments.Select(x => x.Id).Contains(apartment.Id))
                        {
                            await DBWorker.UpdateMonitoringApartment(apartment, true);
                            LogList.Add("Слежка установлена");
                        }
                        else
                        {
                            apartment.IsMonitorng = true;
                            await DBWorker.AddNewApartment(apartment);
                            LogList.Add("Квартира добавлена");
                            LogList.Add("Слежка установлена");
                        }
                    }
                    else
                    {
                        apartment.IsMonitorng = true;
                        await DBWorker.AddNewApartment(apartment);
                        LogList.Add("Квартира добавлена");
                        LogList.Add("Слежка установлена");
                    }
                }
                else
                    LogList.Add("Слежка уже установлена");
            }
            else
                LogList.Add("Ошибка подключения к базе данных");
        }
        private async Task<bool> CheckMonitoringStatus(Apartment apartment)
        {
            if (await DBWorker.CheckDBConnection())
            {
                List<Apartment> dbApartments = await DBWorker.GetApartments();
                if (dbApartments.Count > 0)
                {
                    if (dbApartments.Select(x => x.Id).Contains(apartment.Id))
                    {
                        bool? dbApartmentStatus = dbApartments.FirstOrDefault(x => x.Id == apartment.Id).IsMonitorng;
                        if (dbApartmentStatus is not null)
                            return (bool)dbApartmentStatus;
                    }
                    else
                        return false;
                }
            }
            return false;
        }
        private async Task<Apartment> ParseApartment(string url)
        {
            if (url.Length != 0)
            {
                if (!url.EndsWith("/"))
                    url = url + "/?ajax=1&similar=1";
                else
                    url = url + "?ajax=1&similar=1";
            }
            var request = new GetRequest(url);
            LogList.Add("Отправляю запрос на сервер..");
            try
            {
                var source = await request.RunRequest();
                LogList.Add("Ответ получен");
                Apartment apartament = JsonConvert.DeserializeObject<Apartment>(source);
                apartament.Url = url;
                LogList.Add("Парсинг завершен успешно");
                return apartament;
            }
            catch (InvalidOperationException)
            {
                LogList.Add($"Неверный формат Url");
            }
            catch (ArgumentNullException)
            {
                LogList.Add($"Запрос не вернул данные");
            }
            catch (JsonReaderException)
            {
                LogList.Add($"Ошибка преобразования данных сервера");
            }
            catch (ArgumentException)
            {
                LogList.Add($"Url ссылка подобного формата не поддерживается");
            }
            catch (HttpRequestException)
            {
                LogList.Add($"Отсутствует подключение к сети");
            }
            catch (Exception ex)
            {
                var inf = ex.GetType();
                LogList.Add(ex.Message);
            }
            finally
            {
                LogList.Add(String.Empty);
            }
            return null;
        }
        #endregion
        #region PropertyChanged
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion
    }
}
