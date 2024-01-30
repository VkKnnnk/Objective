using PriceService.Models;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Collections.ObjectModel;

namespace PriceService.View_Models
{
    public class MainWindowVM : INotifyPropertyChanged
    {
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
        public ObservableCollection<string> LogList { get; set; }
        public ObservableCollection<Apartment> Apartments { get; set; }
        public MainWindowVM()
        {
            LogList = new ObservableCollection<string>();
            Apartments = new ObservableCollection<Apartment>();
        }
        #region Commands
        private ICommand _getApartmentCommand;
        public ICommand GetApartmentCommand
            => _getApartmentCommand ??= new RelayCommand(GetApartment);
        #endregion
        private async void GetApartment(object param)
        {
            Apartment apartment = await ParseApartment(InputUrl);
            if (apartment is not null)
                Apartments.Add(apartment);
        }
        private async Task<Apartment> ParseApartment(string url)
        {
            LogList.Add("Парсинг запущен");
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
        #region PropertyChanged
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion
    }
}
