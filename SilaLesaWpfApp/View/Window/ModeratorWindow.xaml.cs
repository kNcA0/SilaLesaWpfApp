using System;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Collections.Generic;
using System.Windows.Data;
using System.Windows;
using System.Windows.Controls;
using SilaLesaWpfApp.Model;

namespace SilaLesaWpfApp
{
    public partial class ModeratorWindow : Window
    {
        
        private static string RoleToRu(string role)
        {
            switch (role)
            {
                case "admin": return "Администратор";
                case "moderator": return "Модератор";
                case "user": return "Пользователь";
                default: return role;
            }
        }

        private static string StatusToRu(string status)
        {
            switch (status)
            {
                case "Booked": return "Забронировано";
                case "Completed": return "Завершено";
                case "Cancelled": return "Отменено";
                default: return status;
            }
        }

        private class StatusToRuConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                return StatusToRu(value?.ToString() ?? "");
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                return Binding.DoNothing;
            }
        }

        private static readonly Dictionary<string, string> ColumnHeadersRu = new Dictionary<string, string>
        {
            { "CustomerID", "ID клиента" },
            { "FullName", "ФИО" },
            { "Phone", "Телефон" },
            { "Email", "Эл. почта" },
            { "Notes", "Примечания" },

            { "SiteID", "ID места" },
            { "SiteCode", "Код" },
            { "SiteName", "Название" },
            { "SiteType", "Тип" },
            { "Capacity", "Вместимость" },
            { "PricePerNight", "Цена/ночь" },
            { "IsActive", "Активен" },

            { "ServiceID", "ID услуги" },
            { "ServiceName", "Услуга" },
            { "ServiceType", "Тип услуги" },
            { "PricePerDay", "Цена/день" },

            { "BookingID", "ID брони" },
            { "CheckInDate", "Заезд" },
            { "CheckOutDate", "Выезд" },
            { "Status", "Статус" },
        };

        private void DataGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (ColumnHeadersRu.TryGetValue(e.PropertyName, out var header))
                e.Column.Header = header;

            if (e.PropertyName == "Status" && e.Column is DataGridTextColumn textCol)
            {
                textCol.Binding = new Binding(e.PropertyName) { Converter = new StatusToRuConverter() };
            }
        }

public ModeratorWindow()
        {
            InitializeComponent();
            Loaded += ModeratorWindow_Loaded;
        }

        private void ModeratorWindow_Loaded(object sender, RoutedEventArgs e)
        {
            tbHeader.Text = $"Вы вошли как: {Session.Username} ({RoleToRu(Session.Role)})";
            cbBookingStatus.SelectedIndex = 0;
            RefreshAll();
        }

        private void RefreshAll()
        {
            LoadCustomers();
            LoadSites();
            LoadServices();
            LoadBookings();
        }

        private void LoadCustomers()
        {
            var dt = Db.Query("SELECT CustomerID, FullName, Phone, Email, Notes FROM Customers ORDER BY CustomerID DESC;");
            dgCustomers.ItemsSource = dt.DefaultView;
        }

        private void LoadSites()
        {
            var dt = Db.Query("SELECT SiteID, SiteCode, SiteName, SiteType, Capacity, PricePerNight, IsActive FROM Sites ORDER BY SiteID DESC;");
            dgSites.ItemsSource = dt.DefaultView;
        }

        private void LoadServices()
        {
            var dt = Db.Query("SELECT ServiceID, ServiceName, ServiceType, PricePerDay, IsActive FROM Services ORDER BY ServiceID DESC;");
            dgServices.ItemsSource = dt.DefaultView;
        }

        private void LoadBookings()
        {
            var dt = Db.Query(@"
SELECT b.BookingID, c.FullName, s.SiteCode, s.SiteName, b.CheckInDate, b.CheckOutDate, b.Status
FROM Bookings b
JOIN Customers c ON c.CustomerID = b.CustomerID
JOIN Sites s ON s.SiteID = b.SiteID
ORDER BY b.BookingID DESC;");
            dgBookings.ItemsSource = dt.DefaultView;
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RefreshAll();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка обновления");
            }
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            var w = new LoginWindow();
            w.Show();
            Close();
        }

        private int? GetSelectedBookingId()
        {
            var row = dgBookings.SelectedItem as DataRowView;
            if (row == null) return null;
            return Convert.ToInt32(row["BookingID"]);
        }

        private void BtnUpdateBookingStatus_Click(object sender, RoutedEventArgs e)
        {
            var bookingId = GetSelectedBookingId();
            if (bookingId == null)
            {
                MessageBox.Show("Выберите бронирование.");
                return;
            }

            var item = cbBookingStatus.SelectedItem as ComboBoxItem;
            if (item == null)
            {
                MessageBox.Show("Выберите статус.");
                return;
            }

            var status = item.Tag != null ? item.Tag.ToString() : item.Content.ToString();

            try
            {
                Db.Exec("UPDATE Bookings SET Status=@s WHERE BookingID=@id;",
                    new SqlParameter("@s", status),
                    new SqlParameter("@id", bookingId.Value));
                LoadBookings();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка обновления бронирования");
            }
        }
    }
}
