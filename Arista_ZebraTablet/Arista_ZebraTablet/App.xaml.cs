namespace Arista_ZebraTablet
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new MainPage()) { Title = "ARISTA Barcode Scanner" };
        }
    }
}
