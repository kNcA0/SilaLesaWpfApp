using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Linq;
using SilaLesaWpfApp.Model;

namespace SilaLesaWpfApp
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
            Loaded += LoginWindow_Loaded;
        }

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

        private void LoginWindow_Loaded(object sender, RoutedEventArgs e)
        {
            tbMessage.Text = "";
            try
            {
                cbRole.ItemsSource = App.context.Roles;
                cbRole.DisplayMemberPath = "RoleID";
                cbRole.SelectedValuePath = "RoleName";
                cbRole.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                // fallback (if DB not connected yet)
                var dt = new DataTable();
                dt.Columns.Add("RoleName", typeof(string));
                dt.Columns.Add("RoleRu", typeof(string));
                dt.Rows.Add("admin", RoleToRu("admin"));
                dt.Rows.Add("moderator", RoleToRu("moderator"));
                dt.Rows.Add("user", RoleToRu("user"));

                cbRole.ItemsSource = dt.DefaultView;
                cbRole.DisplayMemberPath = "RoleRu";
                cbRole.SelectedValuePath = "RoleName";
                cbRole.SelectedIndex = 0;

                tbMessage.Text = "Ошибка: " + ex.Message;
            }

            // Demo accounts (from SQL script):
            // admin / admin123
            // moderator / moderator_123
            // user / user_123
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            tbMessage.Text = "";

            var username = (tbUsername.Text ?? "").Trim();
            var password = pbPassword.Password ?? "";
            var role = cbRole.SelectedValue == null ? "" : cbRole.SelectedValue.ToString();

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(role))
            {
                tbMessage.Text = "Введите логин, пароль и роль.";
                return;
            }

            try
            {
                var user = App.context.AppUsers.FirstOrDefault(u => (u.Username == tbUsername.Text && u.PasswordHash == pbPassword.Password));

                if (user == null)
                {
                    tbMessage.Text = "Не удалось войти. Проверьте логин/пароль/роль.";
                    return;
                }

                Session.UserID = user.UserID;
                Session.Username = user.Username;
                Session.Role = user.Roles.RoleName;

                Window next;
                if (Session.Role == "admin")
                    next = new AdminWindow();
                else if (Session.Role == "moderator")
                    next = new ModeratorWindow();
                else
                    next = new UserWindow();

                next.Show();
                Close();
            }
            catch (Exception ex)
            {
                tbMessage.Text = "Ошибка базы данных: " + ex.Message;
            }
        }
    }
}
