using SilaLesaWpfApp.Model;
using System.Windows;

namespace SilaLesaWpfApp
{
    public partial class App : Application
    {
        public static CampingBookingDBEntities context = new CampingBookingDBEntities();
    }
}
