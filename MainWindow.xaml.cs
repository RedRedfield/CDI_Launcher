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
using System.Diagnostics;
using System.Threading.Tasks;

namespace CDI_Launcher
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private async void Personaje_Click(object sender, RoutedEventArgs e)
        {
            var boton = sender as Button;
            if (boton?.Tag == null) return;

            string rutaRelativa = boton.Tag.ToString() ?? string.Empty;
            
            // 1. Mostrar pantalla de carga e imagen del personaje
            // Usamos el nombre del personaje para buscar su imagen en Assets
            string nombrePersonaje = rutaRelativa.Replace("CDI/DiskInfo", "").Replace(".exe", "");
            LoadingImage.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri($"pack://application:,,,/assets/{nombrePersonaje}.png"));
            LoadingText.Text = $"INICIANDO {nombrePersonaje.ToUpper()}...";
            LoadingScreen.Visibility = Visibility.Visible;

            try 
            {
                string rutaCompleta = System.IO.Path.GetFullPath(rutaRelativa);
                ProcessStartInfo info = new ProcessStartInfo(rutaCompleta)
                {
                    WorkingDirectory = System.IO.Path.GetDirectoryName(rutaCompleta) ?? string.Empty,
                    UseShellExecute = true
                };

                Process? p = Process.Start(info);

                if (p != null)
                {
                    // 2. Correr una tarea en segundo plano para detectar la ventana
                    await Task.Run(() => 
                    {
                        // Esperamos hasta que el proceso tenga una ventana (handle)
                        // O hasta que pasen 10 segundos (por seguridad)
                        int intentos = 0;
                        while (p.MainWindowHandle == IntPtr.Zero && intentos < 40) 
                        {
                            System.Threading.Thread.Sleep(250); // Revisar cada cuarto de segundo
                            p.Refresh(); // Actualizar info del proceso
                            intentos++;
                        }
                    });

                    // 3. Cuando se detecta la ventana, minimizamos el Launcher
                    this.WindowState = WindowState.Minimized;
                    
                    // 4. Ocultamos la pantalla de carga para que cuando el usuario 
                    // vuelva a maximizar el launcher, esté el menú de nuevo.
                    LoadingScreen.Visibility = Visibility.Collapsed;

                    // ... (Después de minimizar la ventana)
                    this.WindowState = WindowState.Minimized;
                    LoadingScreen.Visibility = Visibility.Collapsed;

                    // 5. ESPERAR A QUE EL PROCESO SE CIERRE
                    // Esto se queda "escuchando" en segundo plano sin bloquear el PC
                    await Task.Run(() => p.WaitForExit());

                    // 6. RESTAURAR EL LAUNCHER
                    // Volvemos al hilo principal para tocar la ventana
                    this.WindowState = WindowState.Normal;
                    this.Activate(); // Esto la trae al frente por si quedó detrás de otras carpetas
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
                LoadingScreen.Visibility = Visibility.Collapsed;
            }
        }
    }
}



