using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
namespace NuGet_ArcFace_Embedder
{

    public class img_embed
    {
        private InferenceSession Session;
        private Dictionary<string, Task<float[]>> CalculationsCollection;
        private SemaphoreSlim CancellationTokensSemaphore;
        Dictionary<string, CancellationTokenSource> CancellationTokensCollection;

        public img_embed()
        {
            this.Session = new InferenceSession("arcfaceresnet100-8.onnx");
            this.CancellationTokensSemaphore = new SemaphoreSlim(1);
            this.CalculationsCollection = new Dictionary<string, Task<float[]>>();
            this.CancellationTokensCollection = new Dictionary<string, CancellationTokenSource>();
        }

        ~img_embed()
        {
            this.Session.Dispose();
        }

        public float Length(float[] v) => (float)Math.Sqrt(v.Select(x => x * x).Sum());

        public float[] Normalize(float[] v)
        {
            var len = Length(v);
            return v.Select(x => x / len).ToArray();
        }

        public DenseTensor<float> ImageToTensor(Image<Rgb24> img)
        {
            var w = img.Width;
            var h = img.Height;
            var t = new DenseTensor<float>(new[] { 1, 3, h, w });
            img.ProcessPixelRows(pa =>
            {
                for (int y = 0; y < h; y++)
                {
                    Span<Rgb24> pixelSpan = pa.GetRowSpan(y);
                    for (int x = 0; x < w; x++)
                    {
                        t[0, 0, y, x] = pixelSpan[x].R;
                        t[0, 1, y, x] = pixelSpan[x].G;
                        t[0, 2, y, x] = pixelSpan[x].B;
                    }
                }
            });

            return t;
        }

        public async Task<float[]> GetEmbeddingsAsync(Image<Rgb24> face, CancellationToken token)
        {

            var res = await Task<Dictionary<string, float>>.Run(async () =>
            {
                token.ThrowIfCancellationRequested();
                var inputs = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor("data", ImageToTensor(face)) };
                await CancellationTokensSemaphore.WaitAsync();
                using IDisposableReadOnlyCollection<DisposableNamedOnnxValue> results = Session.Run(inputs);
                CancellationTokensSemaphore.Release();
                token.ThrowIfCancellationRequested();
                return Normalize(results.First(v => v.Name == "fc1").AsEnumerable<float>().ToArray());
            }, token);
            return res;

        }

        public float[] GetEmbeddings(Image<Rgb24> face)
        {
            var inputs = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor("data", ImageToTensor(face)) };
            using IDisposableReadOnlyCollection<DisposableNamedOnnxValue> results = Session.Run(inputs);

            return Normalize(results.First(v => v.Name == "fc1").AsEnumerable<float>().ToArray());
        }
    }
}
