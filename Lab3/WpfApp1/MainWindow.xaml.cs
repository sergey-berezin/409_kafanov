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
using System.Threading;
using System.Collections;
using System.IO;
using System.Collections.ObjectModel;
using Microsoft.EntityFrameworkCore;
using NuGet_ArcFace_Functions;      //подключили библиотеку классов из нашего пакета
using NuGet_ArcFace_Embedder;


namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private List<Tuple<byte[], string>> images_bytes_path;
        private CancellationTokenSource cancelTokenSource;
        private CancellationToken token;
        private bool calculations_status;
        private Dictionary<int, float[]> embeddingsDict;
        private bool running_flg = false;

        img_comp obj1;

        public MainWindow()
        {
            InitializeComponent();
            cancelTokenSource = new CancellationTokenSource();
            token = cancelTokenSource.Token;
            obj1 = new img_comp();
            calculations_status = false;
            images_bytes_path = new List<Tuple<byte[], string>>();
            embeddingsDict = new Dictionary<int, float[]>();
        }

        //Метод загружает изображения с выбранного каталога и вызввает метод, который строит сетку
        private void Button_Open_Images(object sender, RoutedEventArgs e)
        {
            Grid_Clear();
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
                    images_bytes_path.Add(Tuple.Create(System.IO.File.ReadAllBytes(path), path));
                }
            }
            Grid_Construct();
        }

        //Метод строит сетку по каталогу изображений
        private void Grid_Construct()
        {
            int n = images_bytes_path.Count;
            for (int i = 0; i < n + 1; i++)
            {
                table.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });
                table.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });

                if (i > 0)
                {
                    var image1 = new System.Windows.Controls.Image
                    {
                        Source = (BitmapSource)new ImageSourceConverter().ConvertFrom(images_bytes_path[i - 1].Item1)
                    };

                    var image2 = new System.Windows.Controls.Image
                    {
                        Source = (BitmapSource)new ImageSourceConverter().ConvertFrom(images_bytes_path[i - 1].Item1)
                    };

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
        public void Grid_Clear()
        {
            calculations_status = false;
            cancelTokenSource = new CancellationTokenSource();
            token = cancelTokenSource.Token;
            int size = images_bytes_path.Count;
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
            images_bytes_path.Clear();
        }

        //Метод начинает вычисления по заданным изображениям
        private async void Button_Start_Calculations(object sender, RoutedEventArgs e)
        {
            if (running_flg == true)
            {
                MessageBox.Show("Пожалуйста, подождите пока вычисления не закончатся");
                return;
            }
            if (images_bytes_path.Count == 0)
            {
                MessageBox.Show("Пожалуйста, выберите каталог с изображениями.");
                return;
            }
            if (calculations_status)
            {
                MessageBox.Show("Вычисления уже произведены. Пожлауйста, обновите матрицу.");
                return;
            }
            int step1 = 500 / images_bytes_path.Count;
            int step2 = 500 / (images_bytes_path.Count * images_bytes_path.Count);
            var tasks = new List<Task<float[]>>();
            var image_task = new List<int>();
            embeddingsDict.Clear();

            for (int i = 0; i < images_bytes_path.Count; i++)
            {
                try
                {
                    Image newImage = null;
                    using (var db = new ImagesContext())   //По хэш-коду ищем изображение, если хэш-код совпадает, то дальше сверяем содержмое 
                    {
                        string hash = Image.GetHash(images_bytes_path[i].Item1);
                        var q = db.Images.Where(x => x.Hash == hash)
                            .Include(x => x.Details)
                            .Where(x => Equals(x.Details.Data, images_bytes_path[i].Item1));
                        if (q.Any())
                        {
                            newImage = q.First();
                        }
                    }
                    if (newImage is not null)   //Если в базе данных уже есть данное изображение, то просто берем его
                    {
                        float[] embeddings = new float[newImage.Embedding.Length / 4];
                        Buffer.BlockCopy(newImage.Embedding, 0, embeddings, 0, newImage.Embedding.Length);
                        if (!embeddingsDict.ContainsKey(i))
                        {
                            embeddingsDict.Add(i, embeddings);
                        }
                    }
                    else                        //Иначе вычисляем embeddings для данного изображения
                    {
                        var face = SixLabors.ImageSharp.Image.Load<Rgb24>(images_bytes_path[i].Item2);
                        var task1 = obj1.EmbeddingsAsync(face, token);
                        tasks.Add(task1);
                        image_task.Add(i);  //image_task[i] показывает, что i-ый task обрабатывает image_task[i] изображение
                    }
                }
                catch (OperationCanceledException e1)
                {
                    Console.WriteLine($"{nameof(OperationCanceledException)} thrown with message: {e1.Message}");
                }
            }

            for (int i = 0; i < tasks.Count; i++)
            {
                try
                {
                    await tasks[i];
                    pbStatus.Value += step1;
                    embeddingsDict.Add(image_task[i], tasks[i].Result);

                    using (var db = new ImagesContext())           //Если tasks[i] успешно завершился, то записываем результат в базу данных
                    {
                        var newImageDetails = new ImageDetails { Data = images_bytes_path[image_task[i]].Item1 };
                        var byteArray = new byte[tasks[i].Result.Length * 4];
                        Buffer.BlockCopy(tasks[i].Result, 0, byteArray, 0, byteArray.Length);
                        Image newImage = new Image
                        {
                            Name = images_bytes_path[image_task[i]].Item2,
                            Embedding = byteArray,
                            Details = newImageDetails,
                            Hash = Image.GetHash(images_bytes_path[image_task[i]].Item1)
                        };
                        db.Add(newImage);
                        db.SaveChanges();
                    }
                }
                catch (OperationCanceledException e2)
                {
                    Console.WriteLine($"{nameof(OperationCanceledException)} thrown with message: {e2.Message}");
                }
            }

            for (int i = 0; i < images_bytes_path.Count; i++)
            {
                for (int j = 0; j < images_bytes_path.Count; j++)
                {
                    var l = new Label();
                    Grid.SetColumn(l, i + 1);
                    Grid.SetRow(l, j + 1);
                    l.HorizontalAlignment = HorizontalAlignment.Center;
                    l.VerticalAlignment = VerticalAlignment.Center;
                    l.FontSize = 12;
                    if (embeddingsDict.ContainsKey(i) == false || embeddingsDict.ContainsKey(j) == false)
                    {
                        l.Content = $"Distance: Not calculated\n Similarity: Not calculated";
                    }
                    else
                    {
                        var res1 = obj1.AsyncDistance(embeddingsDict[i], embeddingsDict[j], token);
                        await res1;
                        var res2 = obj1.AsyncSimilarity(embeddingsDict[i], embeddingsDict[j], token);
                        await res2;
                        l.Content = $"Distance: {res1.Result}\n Similarity: {res2.Result}";
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
            if (images_bytes_path.Count == 0)
            {
                MessageBox.Show("Матрица уже очищена.");
                return;
            }
            Grid_Clear();
        }

        //Метод открывает диалоговое окно с данными из базы данных
        private void Button_Open_Database(object sender, RoutedEventArgs e)
        {
            WindowDatabase windowDatabase = new WindowDatabase();
            windowDatabase.ShowDialog();
        }

        //Метод отменяет вычисления
        private void Button_Cancel_Calculations(object sender, RoutedEventArgs e)
        {
            cancelTokenSource.Cancel();
            MessageBox.Show("Вычисления прерваны.");
        }
    }
}