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
    public partial class AdminWindow : Window
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
            { "RoleID", "ID роли" },
            { "RoleName", "Роль" },
            { "UserID", "ID пользователя" },
            { "Username", "Логин" },
            { "IsActive", "Активен" },
            { "CreatedAt", "Создан" },

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

            { "ServiceID", "ID услуги" },
            { "ServiceName", "Услуга" },
            { "ServiceType", "Тип услуги" },
            { "PricePerDay", "Цена/день" },

            { "BookingID", "ID брони" },
            { "CheckInDate", "Заезд" },
            { "CheckOutDate", "Выезд" },
            { "Status", "Статус" },
            { "CreatedBy", "Создал" },

            { "VisitID", "ID визита" },
            { "VisitStart", "Начало" },
            { "VisitEnd", "Конец" },
        };

        private void DataGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (ColumnHeadersRu.TryGetValue(e.PropertyName, out var header))
                e.Column.Header = header;

            // Показываем статусы по-русски, но в базе они остаются англ. (Booked/Completed/Cancelled)
            if (e.PropertyName == "Status" && e.Column is DataGridTextColumn textCol)
            {
                textCol.Binding = new Binding(e.PropertyName) { Converter = new StatusToRuConverter() };
            }
        }

private DataTable _rolesDt;

        public AdminWindow()
        {
            InitializeComponent();
            Loaded += AdminWindow_Loaded;
        }

        private void AdminWindow_Loaded(object sender, RoutedEventArgs e)
        {
            tbHeader.Text = $"Вы вошли как: {Session.Username} ({RoleToRu(Session.Role)})";
            RefreshAll();
        }

        private void RefreshAll()
        {
            LoadRoles();
            LoadUsers();
            LoadCustomers();
            LoadSites();
            LoadServices();
            LoadBookings();
            LoadVisits();
        }

        private void LoadRoles()
        {
            _rolesDt = Db.Query("SELECT RoleID, RoleName FROM Roles ORDER BY RoleName;");
            dgRoles.ItemsSource = _rolesDt.DefaultView;

            cbNewUserRole.ItemsSource = _rolesDt.DefaultView;
            cbNewUserRole.DisplayMemberPath = "RoleName";
            cbNewUserRole.SelectedValuePath = "RoleID";
            if (cbNewUserRole.Items.Count > 0) cbNewUserRole.SelectedIndex = 0;

            cbChangeRole.ItemsSource = _rolesDt.DefaultView;
            cbChangeRole.DisplayMemberPath = "RoleName";
            cbChangeRole.SelectedValuePath = "RoleID";
            if (cbChangeRole.Items.Count > 0) cbChangeRole.SelectedIndex = 0;
        }

        private void LoadUsers()
        {
            var dt = Db.Query(@"
SELECT u.UserID, u.Username, r.RoleName, u.IsActive, u.CreatedAt
FROM AppUsers u
JOIN Roles r ON r.RoleID = u.RoleID
ORDER BY u.UserID DESC;");
            dgUsers.ItemsSource = dt.DefaultView;
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
SELECT b.BookingID, c.FullName, c.Phone, s.SiteCode, s.SiteName,
       b.CheckInDate, b.CheckOutDate, b.Status, b.CreatedAt, u.Username AS CreatedBy
FROM Bookings b
JOIN Customers c ON c.CustomerID = b.CustomerID
JOIN Sites s ON s.SiteID = b.SiteID
JOIN AppUsers u ON u.UserID = b.CreatedByUserID
ORDER BY b.BookingID DESC;");
            dgBookings.ItemsSource = dt.DefaultView;
            cbBookingStatus.SelectedIndex = 0;
        }

        private void LoadVisits()
        {
            var dt = Db.Query(@"
SELECT v.VisitID, c.FullName, s.SiteCode, s.SiteName, v.VisitStart, v.VisitEnd, v.Notes
FROM CustomerVisits v
JOIN Customers c ON c.CustomerID = v.CustomerID
JOIN Sites s ON s.SiteID = v.SiteID
ORDER BY v.VisitID DESC;");
            dgVisits.ItemsSource = dt.DefaultView;
        }

        private void BtnRefreshAll_Click(object sender, RoutedEventArgs e)
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

        // --- Roles / Users ---
        private void BtnAddRole_Click(object sender, RoutedEventArgs e)
        {
            var roleName = (tbNewRole.Text ?? "").Trim();
            if (string.IsNullOrWhiteSpace(roleName))
            {
                MessageBox.Show("Введите название роли.");
                return;
            }

            try
            {
                Db.Exec("INSERT INTO Roles(RoleName) VALUES(@r);", new SqlParameter("@r", roleName));
                tbNewRole.Text = "";
                LoadRoles();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка добавления роли");
            }
        }

        private void BtnAddUser_Click(object sender, RoutedEventArgs e)
        {
            var username = (tbNewUser.Text ?? "").Trim();
            var pass = (tbNewPass.Text ?? "").Trim();
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(pass))
            {
                MessageBox.Show("Введите логин и пароль.");
                return;
            }
            if (cbNewUserRole.SelectedValue == null)
            {
                MessageBox.Show("Выберите роль.");
                return;
            }

            try
            {
                var roleId = Convert.ToInt32(cbNewUserRole.SelectedValue);
                Db.Exec("INSERT INTO AppUsers(Username, PasswordHash, RoleID) VALUES(@u,@p,@r);",
                    new SqlParameter("@u", username),
                    new SqlParameter("@p", pass),
                    new SqlParameter("@r", roleId));

                tbNewUser.Text = "";
                tbNewPass.Text = "";
                LoadUsers();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка добавления пользователя");
            }
        }

        private int? GetSelectedUserId()
        {
            var row = dgUsers.SelectedItem as DataRowView;
            if (row == null) return null;
            return Convert.ToInt32(row["UserID"]);
        }

        private void BtnChangeRole_Click(object sender, RoutedEventArgs e)
        {
            var userId = GetSelectedUserId();
            if (userId == null)
            {
                MessageBox.Show("Выберите пользователя в таблице «Пользователи».");
                return;
            }
            if (cbChangeRole.SelectedValue == null)
            {
                MessageBox.Show("Выберите новую роль.");
                return;
            }

            try
            {
                var newRoleId = Convert.ToInt32(cbChangeRole.SelectedValue);
                Db.Exec("UPDATE AppUsers SET RoleID=@r WHERE UserID=@id;",
                    new SqlParameter("@r", newRoleId),
                    new SqlParameter("@id", userId.Value));

                LoadUsers();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка смены роли");
            }
        }

        private void BtnToggleActive_Click(object sender, RoutedEventArgs e)
        {
            var userId = GetSelectedUserId();
            if (userId == null)
            {
                MessageBox.Show("Выберите пользователя в таблице «Пользователи».");
                return;
            }

            try
            {
                Db.Exec("UPDATE AppUsers SET IsActive = CASE WHEN IsActive=1 THEN 0 ELSE 1 END WHERE UserID=@id;",
                    new SqlParameter("@id", userId.Value));
                LoadUsers();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка переключения активности");
            }
        }

        // --- Customers ---
        private void BtnRefreshCustomers_Click(object sender, RoutedEventArgs e)
        {
            try { LoadCustomers(); } catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private void BtnAddCustomer_Click(object sender, RoutedEventArgs e)
        {
            var name = (tbCustName.Text ?? "").Trim();
            var phone = (tbCustPhone.Text ?? "").Trim();
            var email = (tbCustEmail.Text ?? "").Trim();

            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(phone))
            {
                MessageBox.Show("Введите ФИО и телефон.");
                return;
            }

            try
            {
                Db.Exec("INSERT INTO Customers(FullName, Phone, Email) VALUES(@n,@p,@e);",
                    new SqlParameter("@n", name),
                    new SqlParameter("@p", phone),
                    new SqlParameter("@e", string.IsNullOrWhiteSpace(email) ? (object)DBNull.Value : email));

                tbCustName.Text = "";
                tbCustPhone.Text = "";
                tbCustEmail.Text = "";
                LoadCustomers();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка добавления клиента");
            }
        }

        // --- Sites ---
        private void BtnRefreshSites_Click(object sender, RoutedEventArgs e)
        {
            try { LoadSites(); } catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private void BtnAddSite_Click(object sender, RoutedEventArgs e)
        {
            var code = (tbSiteCode.Text ?? "").Trim();
            var name = (tbSiteName.Text ?? "").Trim();
            var type = (tbSiteType.Text ?? "").Trim();

            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(type))
            {
                MessageBox.Show("Введите код, название и тип места.");
                return;
            }

            if (!int.TryParse(tbSiteCap.Text, out var cap) || cap <= 0)
            {
                MessageBox.Show("Вместимость должна быть положительным числом.");
                return;
            }

            if (!decimal.TryParse(tbSitePrice.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var price) &&
                !decimal.TryParse(tbSitePrice.Text, NumberStyles.Any, CultureInfo.CurrentCulture, out price))
            {
                MessageBox.Show("Цена за ночь должна быть числом.");
                return;
            }

            try
            {
                Db.Exec(@"
INSERT INTO Sites(SiteCode, SiteName, SiteType, Capacity, PricePerNight, IsActive)
VALUES(@c,@n,@t,@cap,@p,1);",
                    new SqlParameter("@c", code),
                    new SqlParameter("@n", name),
                    new SqlParameter("@t", type),
                    new SqlParameter("@cap", cap),
                    new SqlParameter("@p", price));

                tbSiteCode.Text = "";
                tbSiteName.Text = "";
                tbSiteType.Text = "";
                tbSiteCap.Text = "";
                tbSitePrice.Text = "";
                LoadSites();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка добавления места");
            }
        }

        // --- Services ---
        private void BtnRefreshServices_Click(object sender, RoutedEventArgs e)
        {
            try { LoadServices(); } catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private void BtnAddService_Click(object sender, RoutedEventArgs e)
        {
            var name = (tbSvcName.Text ?? "").Trim();
            var type = (tbSvcType.Text ?? "").Trim();

            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(type))
            {
                MessageBox.Show("Введите название услуги и тип.");
                return;
            }

            if (!decimal.TryParse(tbSvcPrice.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var price) &&
                !decimal.TryParse(tbSvcPrice.Text, NumberStyles.Any, CultureInfo.CurrentCulture, out price))
            {
                MessageBox.Show("Цена за день должна быть числом.");
                return;
            }

            try
            {
                Db.Exec("INSERT INTO Services(ServiceName, ServiceType, PricePerDay, IsActive) VALUES(@n,@t,@p,1);",
                    new SqlParameter("@n", name),
                    new SqlParameter("@t", type),
                    new SqlParameter("@p", price));

                tbSvcName.Text = "";
                tbSvcType.Text = "";
                tbSvcPrice.Text = "";
                LoadServices();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка добавления услуги");
            }
        }

        // --- Bookings ---
        private void BtnRefreshBookings_Click(object sender, RoutedEventArgs e)
        {
            try { LoadBookings(); } catch (Exception ex) { MessageBox.Show(ex.Message); }
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

        // --- Visits ---
        private void BtnRefreshVisits_Click(object sender, RoutedEventArgs e)
        {
            try { LoadVisits(); } catch (Exception ex) { MessageBox.Show(ex.Message); }
        }
    }
}
