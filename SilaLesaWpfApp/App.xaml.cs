using System.Windows;

namespace SilaLesaWpfApp
{
    public partial class App : Application
    {
        public static Model.CampingBookingDBEntities context = new Model.CampingBookingDBEntities();
    }
}
