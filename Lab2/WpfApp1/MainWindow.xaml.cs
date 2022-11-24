using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using NuGet_ArcFace_Functions;      //подключили библиотеку классов из нашего пакета
using System.Threading;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private List<string> list_images_path;
        private List<Image<Rgb24>> list_images;
        private CancellationTokenSource cancelTokenSource;
        private CancellationToken token;
        private bool calculations_status;
        private bool running_flg = false;
        img_comp obj1;
        public MainWindow()
        {
            InitializeComponent();
            cancelTokenSource = new CancellationTokenSource();
            token = cancelTokenSource.Token;
            list_images_path = new List<string>();
            list_images = new List<Image<Rgb24>>();
            obj1 = new img_comp();
            calculations_status = false;
        }

        //Метод загружает изображения с выбранного каталога и вызввает метод, который строит сетку
        private void Button_Open_Images(object sender, RoutedEventArgs e)
        {
            Gride_Clear();
            Microsoft.Win32.OpenFileDialog ofd = new Microsoft.Win32.OpenFileDialog();
            ofd.Multiselect = true;
            ofd.Filter = "Images (*.jpg, *.png)|*.jpg;*.png";
            var projectRootFolder = System.IO.Path.GetFullPath("../../../../Images");
            ofd.InitialDirectory = projectRootFolder;
            var response = ofd.ShowDialog();
            if (response == true)
            {
                foreach (var path in ofd.FileNames)
                {
                    var face = SixLabors.ImageSharp.Image.Load<Rgb24>(path);
                    list_images.Add(face);
                    list_images_path.Add(path);
                }
            }
            Grid_Construct();
        }

        //Метод строит сетку по каталогу изображений
        private void Grid_Construct()
        {
            int n = list_images_path.Count;
            for (int i = 0; i < n + 1; i++)
            {
                table.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });
                table.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });

                if (i > 0)
                {
                    var uri = new System.Uri(list_images_path[i - 1]);
                    var bitmap = new BitmapImage(uri);

                    var image1 = new System.Windows.Controls.Image();
                    image1.Source = bitmap;
                    var image2 = new System.Windows.Controls.Image();
                    image2.Source = bitmap;

                    Grid.SetColumn(image1, 0);
                    Grid.SetRow(image1, i);
                    table.Children.Add(image1);

                    Grid.SetColumn(image2, i);
                    Grid.SetRow(image2, 0);
                    table.Children.Add(image2);
                }
            }
        }

        //метод очищает сетку, массивы с изображениями, обновляет токены
        public void Gride_Clear()
        {
            calculations_status = false;
            cancelTokenSource = new CancellationTokenSource();
            token = cancelTokenSource.Token;
            int size = list_images.Count;
            if (size == 0)
                return;
            table.Children.Clear();
            pbStatus.Value = 0;
            for (int i = 0; i < size + 1; i++)
            {
                table.RowDefinitions.Clear();
            }
            for (int i = 0; i < size + 1; i++)
            {
                table.ColumnDefinitions.Clear();
            }
            list_images_path.Clear();
            list_images.Clear();
        }

        //Метод начинает вычисления по заданным изображениям
        private async void Button_Start_Calculations(object sender, RoutedEventArgs e)
        {
            if (running_flg == true)
            {
                MessageBox.Show("Пожалуйста, подождите пока вычисления не закончатся");
                return;
            }
            if (list_images.Count == 0)
            {
                MessageBox.Show("Пожалуйста, выберите каталог с изображениями.");
                return;
            }
            if (calculations_status)
            {
                MessageBox.Show("Вычисления уже произведены. Пожалуйста, обновите матрицу.");
                return;
            }
            running_flg = true;
            int step1 = 250 / list_images.Count;
            int step2 = 500 / (list_images.Count * list_images.Count);
            var tasks = new List<Task>();

            pbStatus.Value += step1 * list_images.Count;

            for (int i = 0; i < list_images.Count; i++)
            {
                for (int j = 0; j < list_images.Count; j++)
                {
                    var l = new Label();
                    Grid.SetColumn(l, i + 1);
                    Grid.SetRow(l, j + 1);
                    l.HorizontalAlignment = HorizontalAlignment.Center;
                    l.VerticalAlignment = VerticalAlignment.Center;
                    l.FontSize = 12;
                    if (token.IsCancellationRequested)
                    {
                        l.Content = $"Distance: Not calculated\n Similarity: Not calculated";
                    }
                    else
                    {
                        var res1 = obj1.AsyncDistance(list_images[i], list_images[j], token);
                        await res1;
                        var res2 = obj1.AsyncSimilarity(list_images[i], list_images[j], token);
                        await res2;
                        l.Content = $"Distance: {res1.Result}\n Similarity: {res2.Result}\n";
                        pbStatus.Value += step2;
                    }
                    table.Children.Add(l);
                }
            }
            if (!token.IsCancellationRequested)
            {
                pbStatus.Value = 1000;
            }
            calculations_status = true;
            running_flg = false;
        }

        private void Button_Grid_Clear(object sender, RoutedEventArgs e)
        {
            if (list_images.Count == 0)
            {
                MessageBox.Show("Матрица уже очищена.");
                return;
            }
            Gride_Clear();
        }

        //Метод отменяет вычисления
        private void Button_Cancel_Calculations(object sender, RoutedEventArgs e)
        {
            cancelTokenSource.Cancel();
            MessageBox.Show("Вычисления прерваны.");
        }
    }
}
