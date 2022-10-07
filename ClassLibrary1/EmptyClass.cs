using System;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using NuGet_ArcFace_Embedder;


namespace NuGet_ArcFace_Functions
{
    public class img_comp
    {
        public float Length(float[] v) => (float)Math.Sqrt(v.Select(x => x * x).Sum());
        public float Distance(float[] v1, float[] v2) => Length(v1.Zip(v2).Select(p => p.First - p.Second).ToArray());
        public float Similarity(float[] v1, float[] v2) => v1.Zip(v2).Select(p => p.First * p.Second).Sum();

        public delegate T CalculationCallback<T>(float[] v1, float[] v2);

        public T Execute<T>(Image<Rgb24> img1, Image<Rgb24> img2, CalculationCallback<T> callback)
        {

            float[] embeddings1 = embedder.GetEmbeddings(img1);
            float[] embeddings2 = embedder.GetEmbeddings(img2);
            return callback(embeddings1, embeddings2);
        }


        public async Task<float> ExecuteAsync(Image<Rgb24> img1, Image<Rgb24> img2, CalculationCallback<float> callback)
        {
            Func<float> embeddings =
                () =>
                {
                    float[] embeddings1 = embedder.GetEmbeddings(img1);
                    float[] embeddings2 = embedder.GetEmbeddings(img2);
                    return callback(embeddings1, embeddings2);
                };
            var res = Task<float>.Run(embeddings);
            return res.Result;
        }

        public float Distance(Image<Rgb24> img1, Image<Rgb24> img2)
        { return Execute<float>(img1, img2, Distance); }

        public float Similarity(Image<Rgb24> img1, Image<Rgb24> img2)
        { return Execute<float>(img1, img2, Similarity); }

        public (float distance, float similarity) Distance_and_Similarity(Image<Rgb24> img1, Image<Rgb24> img2)
        { return (Execute<float>(img1, img2, Distance), Execute<float>(img1, img2, Similarity)); }

        public async Task<float> AsyncDistance(Image<Rgb24> img1, Image<Rgb24> img2)
        {
            var res = await ExecuteAsync(img1, img2, Distance);
            return res;
        }

        public async Task<float> AsyncSimilarity(Image<Rgb24> img1, Image<Rgb24> img2)
        {
            var res = await ExecuteAsync(img1, img2, Similarity);
            return res;
        }

        public img_embed embedder;

        public img_comp()
        {
            this.embedder = new img_embed();
        }

        public void result(Image<Rgb24> img1, Image<Rgb24> img2)
        {
            float[] embeddings1 = embedder.GetEmbeddings(img1);
            float[] embeddings2 = embedder.GetEmbeddings(img2);
            Console.WriteLine($"Distance =  {Distance(embeddings1, embeddings2) * Distance(embeddings1, embeddings2)}");
            Console.WriteLine($"Similarity =  {Similarity(embeddings1, embeddings2)}");
        }
    }
}