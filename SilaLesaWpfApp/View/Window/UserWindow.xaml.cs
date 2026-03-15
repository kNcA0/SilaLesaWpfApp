using System;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows;
using SilaLesaWpfApp.Model;

namespace SilaLesaWpfApp
{
    public partial class UserWindow : Window
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
            { "SiteCode", "Код" },
            { "SiteName", "Название" },
            { "SiteType", "Тип" },
            { "Capacity", "Вместимость" },
            { "PricePerNight", "Цена/ночь" },

            { "ServiceName", "Услуга" },
            { "Quantity", "Количество" },

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

private DataTable _customersDt;
        private DataTable _servicesDt;
        private DataTable _selectedServicesDt;

        public UserWindow()
        {
            InitializeComponent();
            Loaded += UserWindow_Loaded;
        }

        private void UserWindow_Loaded(object sender, RoutedEventArgs e)
        {
            tbHeader.Text = $"Вы вошли как: {Session.Username} ({RoleToRu(Session.Role)})";
            tbUserMsg.Text = "";
            InitSelectedServicesTable();
            LoadCustomers();
            LoadServices();

            // Default dates (today + 1 day)
            dpIn.SelectedDate = DateTime.Today.AddDays(1);
            dpOut.SelectedDate = DateTime.Today.AddDays(2);
        }

        private void InitSelectedServicesTable()
        {
            _selectedServicesDt = new DataTable();
            _selectedServicesDt.Columns.Add("ServiceID", typeof(int));
            _selectedServicesDt.Columns.Add("ServiceName", typeof(string));
            _selectedServicesDt.Columns.Add("Quantity", typeof(int));
            dgSelectedServices.ItemsSource = _selectedServicesDt.DefaultView;
        }

        private void LoadCustomers()
        {
            _customersDt = Db.Query("SELECT CustomerID, FullName, Phone FROM Customers ORDER BY FullName;");
            cbCustomer.ItemsSource = _customersDt.DefaultView;
            cbCustomer.DisplayMemberPath = "FullName";
            cbCustomer.SelectedValuePath = "CustomerID";
            if (cbCustomer.Items.Count > 0) cbCustomer.SelectedIndex = 0;
        }

        private void LoadServices()
        {
            _servicesDt = Db.Query("SELECT ServiceID, ServiceName, PricePerDay FROM Services WHERE IsActive=1 ORDER BY ServiceName;");
            cbService.ItemsSource = _servicesDt.DefaultView;
            cbService.DisplayMemberPath = "ServiceName";
            cbService.SelectedValuePath = "ServiceID";
            if (cbService.Items.Count > 0) cbService.SelectedIndex = 0;
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            var w = new LoginWindow();
            w.Show();
            Close();
        }

        private void BtnFindSites_Click(object sender, RoutedEventArgs e)
        {
            tbUserMsg.Text = "";
            var inDate = dpIn.SelectedDate;
            var outDate = dpOut.SelectedDate;

            if (inDate == null || outDate == null)
            {
                MessageBox.Show("Выберите даты заезда и выезда.");
                return;
            }
            if (outDate.Value <= inDate.Value)
            {
                MessageBox.Show("Дата выезда должна быть позже даты заезда.");
                return;
            }

            try
            {
                var dt = Db.Query(@"
SELECT s.SiteID, s.SiteCode, s.SiteName, s.SiteType, s.Capacity, s.PricePerNight
FROM Sites s
WHERE s.IsActive = 1
AND NOT EXISTS (
    SELECT 1
    FROM Bookings b
    WHERE b.SiteID = s.SiteID
      AND b.Status <> 'Cancelled'
      AND @inDate < b.CheckOutDate
      AND @outDate > b.CheckInDate
)
ORDER BY s.SiteCode;",
                    new SqlParameter("@inDate", inDate.Value.Date),
                    new SqlParameter("@outDate", outDate.Value.Date));

                dgAvailableSites.ItemsSource = dt.DefaultView;

                if (dt.Rows.Count == 0)
                    tbUserMsg.Text = "Нет свободных мест на выбранные даты.";
                else
                    tbUserMsg.Text = "Выберите место и при необходимости услуги, затем создайте бронирование.";
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка поиска мест");
            }
        }

        private void BtnAddService_Click(object sender, RoutedEventArgs e)
        {
            tbUserMsg.Text = "";

            if (cbService.SelectedValue == null)
            {
                MessageBox.Show("Выберите услугу.");
                return;
            }

            if (!int.TryParse(tbQty.Text, out var qty) || qty <= 0)
            {
                MessageBox.Show("Количество должно быть положительным числом.");
                return;
            }

            var serviceId = Convert.ToInt32(cbService.SelectedValue);
            var serviceName = ((DataRowView)cbService.SelectedItem)["ServiceName"].ToString();

            // if already selected -> sum
            foreach (DataRow row in _selectedServicesDt.Rows)
            {
                if (Convert.ToInt32(row["ServiceID"]) == serviceId)
                {
                    row["Quantity"] = Convert.ToInt32(row["Quantity"]) + qty;
                    return;
                }
            }

            var newRow = _selectedServicesDt.NewRow();
            newRow["ServiceID"] = serviceId;
            newRow["ServiceName"] = serviceName;
            newRow["Quantity"] = qty;
            _selectedServicesDt.Rows.Add(newRow);
        }

        private void BtnClearServices_Click(object sender, RoutedEventArgs e)
        {
            _selectedServicesDt.Rows.Clear();
        }

        private int? GetSelectedSiteId()
        {
            var row = dgAvailableSites.SelectedItem as DataRowView;
            if (row == null) return null;
            return Convert.ToInt32(row["SiteID"]);
        }

        private void BtnCreateBooking_Click(object sender, RoutedEventArgs e)
        {
            tbUserMsg.Text = "";

            if (cbCustomer.SelectedValue == null)
            {
                MessageBox.Show("Выберите клиента.");
                return;
            }

            var siteId = GetSelectedSiteId();
            if (siteId == null)
            {
                MessageBox.Show("Выберите место в таблице «Доступные места».");
                return;
            }

            var inDate = dpIn.SelectedDate;
            var outDate = dpOut.SelectedDate;
            if (inDate == null || outDate == null || outDate.Value <= inDate.Value)
            {
                MessageBox.Show("Проверьте даты.");
                return;
            }

            var customerId = Convert.ToInt32(cbCustomer.SelectedValue);

            try
            {
                // Create booking and return new BookingID
                var bookingIdObj = Db.Scalar(@"
INSERT INTO Bookings(CustomerID, SiteID, CheckInDate, CheckOutDate, Status, CreatedByUserID)
VALUES(@cid, @sid, @inDate, @outDate, 'Booked', @createdBy);
SELECT CAST(SCOPE_IDENTITY() AS INT);",
                    new SqlParameter("@cid", customerId),
                    new SqlParameter("@sid", siteId.Value),
                    new SqlParameter("@inDate", inDate.Value.Date),
                    new SqlParameter("@outDate", outDate.Value.Date),
                    new SqlParameter("@createdBy", Session.UserID));

                var bookingId = Convert.ToInt32(bookingIdObj);

                // Add extra services (optional)
                foreach (DataRow r in _selectedServicesDt.Rows)
                {
                    Db.Exec(@"
INSERT INTO BookingServices(BookingID, ServiceID, Quantity, DateFrom, DateTo)
VALUES(@bid, @sid, @qty, @df, @dt);",
                        new SqlParameter("@bid", bookingId),
                        new SqlParameter("@sid", Convert.ToInt32(r["ServiceID"])),
                        new SqlParameter("@qty", Convert.ToInt32(r["Quantity"])),
                        new SqlParameter("@df", inDate.Value.Date),
                        new SqlParameter("@dt", outDate.Value.Date));
                }

                tbUserMsg.Text = $"Бронирование создано. Номер = {bookingId}.";
                _selectedServicesDt.Rows.Clear();

                // Refresh sites and bookings
                BtnFindSites_Click(null, null);
                RefreshMyBookings();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка создания бронирования");
            }
        }

        private void RefreshMyBookings()
        {
            if (cbCustomer.SelectedValue == null)
                return;

            var customerId = Convert.ToInt32(cbCustomer.SelectedValue);

            var dt = Db.Query(@"
SELECT b.BookingID, s.SiteCode, s.SiteName, b.CheckInDate, b.CheckOutDate, b.Status
FROM Bookings b
JOIN Sites s ON s.SiteID = b.SiteID
WHERE b.CustomerID = @cid
ORDER BY b.BookingID DESC;",
                new SqlParameter("@cid", customerId));

            dgMyBookings.ItemsSource = dt.DefaultView;
        }

        private void BtnRefreshMyBookings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RefreshMyBookings();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка обновления списка бронирований");
            }
        }

        private int? GetSelectedMyBookingId()
        {
            var row = dgMyBookings.SelectedItem as DataRowView;
            if (row == null) return null;
            return Convert.ToInt32(row["BookingID"]);
        }

        private void BtnCancelBooking_Click(object sender, RoutedEventArgs e)
        {
            var bookingId = GetSelectedMyBookingId();
            if (bookingId == null)
            {
                MessageBox.Show("Выберите бронирование.");
                return;
            }

            try
            {
                // Only cancel if it is still Booked
                Db.Exec("UPDATE Bookings SET Status='Cancelled' WHERE BookingID=@id AND Status='Booked';",
                    new SqlParameter("@id", bookingId.Value));

                RefreshMyBookings();
                tbUserMsg.Text = "Бронирование отменено (если оно было в статусе «Забронировано»).";
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка отмены бронирования");
            }
        }
    }
}
