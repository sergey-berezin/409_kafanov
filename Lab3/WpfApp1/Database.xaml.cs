using System;
using System.Linq;
using System.Windows;
using System.Collections.ObjectModel;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;


namespace WpfApp1
{
    public class Image
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Hash { get; set; }
        public byte[] Embedding { get; set; }
        public ImageDetails Details { get; set; }

        public static string GetHash(byte[] data)
        {
            using (var sha256 = SHA256.Create())
            {
                return string.Concat(sha256.ComputeHash(data).Select(x => x.ToString("X2")));
            }
        }
    }

    public class ImageDetails
    {
        public int Id { get; set; }
        public byte[] Data { get; set; }
    }

    public class ImagesContext : DbContext
    {
        public DbSet<Image> Images { get; set; }
        public DbSet<ImageDetails> Details { get; set; }

        public ImagesContext()
        {
            Database.EnsureCreated();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder o)
        {
            o.UseSqlite("Data Source=images.db");
        }
    }
    public partial class WindowDatabase : Window
    {
        public ObservableCollection<Image> ImagesCollection { get; private set; }
        public WindowDatabase()
        {
            ImagesCollection = new ObservableCollection<Image>();

            using (var db = new ImagesContext())
            {
                foreach (var image in db.Images)
                {
                    ImagesCollection.Add(image);
                }
            }

            InitializeComponent();
            DataContext = this;
        }

        private void Button_Delete_Image(object sender, RoutedEventArgs e)
        {
            try
            {
                var image = ImagesCollection[ImagesCollectionListBox.SelectedIndex];
                using (var db = new ImagesContext())
                {
                    var deletedImage = db.Images.Where(x => x.Id == image.Id).Include(x => x.Details).First();
                    if (deletedImage == null)
                    {
                        return;
                    }
                    db.Details.Remove(deletedImage.Details);
                    db.Images.Remove(deletedImage);
                    db.SaveChanges();
                    ImagesCollection.Remove(image);
                }
            }
            catch (Exception e1)
            {
                MessageBox.Show(e1.Message);
            }
        }
    }
}
