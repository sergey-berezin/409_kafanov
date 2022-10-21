using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using NuGet_ArcFace_Embedder;
using NuGet_ArcFace_Functions;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ConsoleApp1
{
    class Test
    {

        static void AsyncDistTest(Image<Rgb24> img1, Image<Rgb24> img2)
        {
            var Comp = new img_comp();
            var test01 = Comp.AsyncDistance(img1, img2, Guid.NewGuid().ToString());
            var test11 = Comp.AsyncDistance(img1, img1, Guid.NewGuid().ToString());
            var ActiveTests = new List<Task> { test01, test11 };
            while (ActiveTests.Count > 0)
            {
                var finished = Task.WaitAny(ActiveTests.ToArray());
                if (ActiveTests[finished] == test11)
                {
                    Console.WriteLine("Distance Test[1,1] finished!");
                }
                else if (ActiveTests[finished] == test01)
                {
                    Console.WriteLine("Distance Test[1,2] finished!");
                }
                else
                    Console.WriteLine("ERROR!");
                ActiveTests.Remove(ActiveTests[finished]);
            }
        }

        static void AsyncCanc(Image<Rgb24> image1, Image<Rgb24> image2)
        {
            var Comp = new img_comp();
            string same_faces_test_token1 = Guid.NewGuid().ToString();
            string same_faces_test_token2 = Guid.NewGuid().ToString();

            var task1 = Comp.AsyncDistance(image1, image1, same_faces_test_token1);
            var task2 = Comp.AsyncDistance(image2, image2, Guid.NewGuid().ToString());
            var task3 = Comp.AsyncDistance(image1, image2, same_faces_test_token2);
            var task4 = Comp.AsyncDistance(image2, image1, Guid.NewGuid().ToString());

            try
            {
                Comp.Cancel(same_faces_test_token1);
            }
            catch(AggregateException) { }
            Console.WriteLine("Task 1 successfully cancelled!\n");
            try
            {
                Comp.Cancel(same_faces_test_token2);
            }
            catch (AggregateException) { }
            Console.WriteLine("Task 3 successfully cancelled!\n");
            var Task_List = new List<Task<float>> { task1, task2, task3, task4 };
            while (Task_List.Count > 0)
            {
                try
                {
                    var finished = Task.WaitAny(Task_List.ToArray());
                    int num = -1;
                    var result = Task_List[finished].Result;
                    if (Task_List[finished] == task1)
                        num = 0;
                    else if (Task_List[finished] == task2)
                        num = 1;
                    else if (Task_List[finished] == task3)
                        num = 2;
                    else if (Task_List[finished] == task4)
                        num = 3;
                    Console.WriteLine("Test " + (num + 1).ToString() + " finished!\n");
                    Task_List.RemoveAt(finished);
                }
                catch (AggregateException ae)
                {
                    foreach (Exception e in ae.InnerExceptions)
                    {
                        if (e is TaskCanceledException)
                        {
                            TaskCanceledException ex = (TaskCanceledException)e;
                            Console.WriteLine(ex.Message);
                            Task_List.Remove(Task_List.Find(x => x.Id.Equals(ex.Task.Id)));
                        }
                        else
                            Console.WriteLine(e.Message);
                    }
                }
            }
        }

        static void Main()
        {

            var img1 = Image.Load<Rgb24>("face1.png");
            var img2 = Image.Load<Rgb24>("face2.png");
            AsyncDistTest(img1, img2);
            AsyncCanc(img1, img2);
        }
    }
}