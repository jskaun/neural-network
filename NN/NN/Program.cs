using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;

namespace NN
{
    public class Program
    {
        static NeuralNetwork network;
        static DrawingWindow drawingWindow;

        static void Main(string[] args)
        {
            // open drawing window
            drawingWindow = new DrawingWindow();
            Thread thread1 = new Thread(drawingWindow.OpenWindow);
            thread1.Start();

            // load training set and pre-trained network
            ImageInput[] trainingSet = Utils.LoadImageFile("train-images.idx3-ubyte", "train-labels.idx1-ubyte");
            if (trainingSet.Length > 0)
                Console.WriteLine("Training set loaded.");
            network = LoadNetwork("pre_trained.bin");

            bool run = true;
            while (run)
            {
                // get user input
                string[] commands = Console.ReadLine().Split(' ');
                switch (commands[0])
                {
                    case "exit":
                        run = false;
                        break;

                    case "draw":
                        if (drawingWindow == null || drawingWindow.IsDisposed)
                        {
                            // open drawing window
                            drawingWindow = new DrawingWindow();
                            Thread thread = new Thread(drawingWindow.OpenWindow);
                            thread.Start();
                        }
                        break;

                    case "input":
                        if (commands.Length > 1 && commands[1] == "drawing")
                        {
                            // input from drawing window
                            InputFromDrawingWindow();
                        }
                        else
                        if (commands.Length > 1 && commands[1].EndsWith(".png"))
                        {
                            // input from .png file
                            ImageInput input = Utils.LoadPNG(commands[1]);
                            if (network.Pass(input.input) != null)
                                Console.WriteLine(NeuralNetwork.FloatsToDigit(network.GetOutput()));
                        }
                        break;

                    case "network":
                        if (commands.Length > 1 && commands[1] == "load")
                        {
                            // load network
                            if (commands.Length > 2)
                                LoadNetwork(commands[2]);
                            else
                                Console.WriteLine("No file specified.");
                        }
                        else
                        if (commands.Length > 1 && commands[1] == "save")
                        {
                            // save network
                            if (commands.Length > 2)
                                SaveNetwork(commands[2]);
                            else
                                Console.WriteLine("No file specified.");
                        }
                        else
                        if (commands.Length > 1 && commands[1] == "new")
                        {
                            // ask hidden layer count
                            Console.Write("Hidden layer count: ");
                            int hiddenLayers = 0;
                            if (Int32.TryParse(Console.ReadLine(), out hiddenLayers) && hiddenLayers >= 1)
                            {
                                // ask node count
                                Console.Write("Nodes per hidden layer: ");
                                int hiddenLayerNodes = 0;
                                if (Int32.TryParse(Console.ReadLine(), out hiddenLayerNodes) && hiddenLayerNodes >= 1)
                                {
                                    // create a new network
                                    network = new NeuralNetwork(2 + hiddenLayers, hiddenLayerNodes, 784, 10);
                                    Console.WriteLine("New network created.");
                                }
                                else
                                    Console.WriteLine("Invalid node count.");
                            }
                            else
                                Console.WriteLine("Invalid layer count.");
                        }
                        else
                        if (commands.Length > 1 && commands[1] == "info")
                        {
                            // display network info
                            if (network != null)
                            {
                                Console.WriteLine("Name: " + network.name);
                                Console.WriteLine("Hidden layers: " + network.GetHiddenLayerCount());
                                Console.WriteLine("Nodes per hidden layer: " + network.GetHiddenLayerNodeCount());
                            }
                        }
                            break;

                    case "train":
                        if (commands.Length > 1)
                        {
                            int trainingIterations = 0;
                            // get iteration count
                            if (!Int32.TryParse(commands[1], out trainingIterations))
                            {
                                Console.WriteLine("Invalid iteration count.");
                                break;
                            }
                            if (commands.Length > 2)
                            {
                                float learningRate = 0f;
                                // get learning rate
                                if (!float.TryParse(commands[2], out learningRate))
                                {
                                    Console.WriteLine("Invalid learning rate.");
                                    break;
                                }

                                Train(trainingSet, trainingIterations, learningRate);
                            }
                        }
                        else
                        {
                            // ask iteration count
                            Console.Write("Training iterations: ");
                            int trainingIterations = 0;
                            if (Int32.TryParse(Console.ReadLine(), out trainingIterations) && trainingIterations > 0)
                            {
                                // ask learning rate
                                Console.Write("Learning rate: ");
                                float learningRate = 0f;
                                if (float.TryParse(Console.ReadLine(), out learningRate) && learningRate > 0f)
                                {
                                    Train(trainingSet, trainingIterations, learningRate);
                                }
                                else
                                    Console.WriteLine("Invalid learning rate.");
                            }
                            else
                                Console.WriteLine("Invalid iteration count.");
                        }
                        break;

                    case "test":
                        if (commands.Length > 1)
                        {
                            // get iteration count
                            int iterations = 0;
                            if (!Int32.TryParse(commands[1], out iterations))
                            {
                                Console.WriteLine("Invalid iteration count.");
                                break;
                            }
                            Test(trainingSet, iterations);
                        }
                        break;

                    case "help":
                        // print info about commands
                        Console.WriteLine("'input' filename.png: input from a .png file");
                        Console.WriteLine("'input' 'drawing': input from drawing window");
                        Console.WriteLine("'network' 'save' filename: save current network");
                        Console.WriteLine("'network' 'load' filename: load network from file");
                        Console.WriteLine("'network' 'new': create a new network");
                        Console.WriteLine("'test' iterations: test network using the MNIST data set");
                        Console.WriteLine("'train': train network using the MNIST data set");
                        Console.WriteLine("'draw': open drawing control");
                        break;

                    default:
                        Console.WriteLine("Invalid command.");
                        break;
                }
            }
        }

        static void Train(ImageInput[] trainingSet, int trainingIterations = 0, float learningRate = 0f)
        {
            if (trainingSet.Length == 0)
            {
                Console.WriteLine("No training set loaded.");
                return;
            }

            // train
            Random rnd = new Random();
            int[] scores = new int[100];
            for (int i = 0; i < trainingIterations; i++)
            {
                int n = (int)(rnd.NextDouble() * trainingSet.Length); // random sample
                network.Train(trainingSet[n].input, NeuralNetwork.DigitToFloats(trainingSet[n].label), learningRate);

                scores[i % 100] = NeuralNetwork.FloatsToDigit(network.GetOutput()) == trainingSet[n].label ? 1 : 0;
                if (i % 10 == 0)
                {
                    // calculate and print accuracy every 10 samples
                    Console.Write("Training(" + i + "). Accuracy of the last 100 samples: " + scores.Sum() + "%               ");
                    Console.CursorLeft = 0;
                }
            }
            Console.CursorTop++;
        }

        static void Test(ImageInput[] testingSet, int iterations)
        {
            if (testingSet.Length == 0)
            {
                Console.WriteLine("No training set loaded.");
                return;
            }

            // test
            Random rnd = new Random();
            int[] scores = new int[100];
            for (int i = 0; i < iterations; i++)
            {
                int n = (int)(rnd.NextDouble() * testingSet.Length); // random sample
                network.Pass(testingSet[n].input);

                scores[i % 100] = NeuralNetwork.FloatsToDigit(network.GetOutput()) == testingSet[n].label ? 1 : 0;
                if (i % 10 == 0)
                {
                    // calculate and print accuracy every 10 samples
                    Console.Write("Testing(" + i + "). Accuracy of the last 100 samples: " + scores.Sum() + "%               ");
                    Console.CursorLeft = 0;
                }
            }
            Console.CursorTop++;
        }

        public static void InputFromDrawingWindow()
        {
            // analyze bitmap from drawing window
            if (drawingWindow != null && !drawingWindow.IsDisposed)
            {
                // window exists
                Bitmap bmp = drawingWindow.GetResizedBitmap();
                byte[] bitmapPixels = Utils.BitmapToByteArray(bmp);
                float[] input = Utils.NormalizeVector(Utils.PixelsToFloats(bitmapPixels, 4));
                if (network != null)
                {
                    // network exists
                    if (network.Pass(input) != null)
                        Console.WriteLine(NeuralNetwork.FloatsToDigit(network.GetOutput()));
                }
                else
                {
                    Console.WriteLine("No network loaded.");
                }
            }
            else
                Console.WriteLine("No drawing window control found.");
        }

        static void SaveNetwork(string fileName)
        {
            Stream fileStream = null;
            try
            {
                fileStream = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None);
                IFormatter formatter = new BinaryFormatter();
                network.name = fileName;
                formatter.Serialize(fileStream, network);
                Console.WriteLine("Network " + fileName + " saved.");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine("Error saving file.");
            }
            finally
            {
                if (fileStream != null)
                    fileStream.Close();
            }
        }

        static NeuralNetwork LoadNetwork(string fileName)
        {
            Stream fileStream = null;
            try
            {
                fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.None);
                IFormatter formatter = new BinaryFormatter();
                NeuralNetwork nw = (NeuralNetwork)formatter.Deserialize(fileStream);
                nw.name = fileName;
                if (nw.inputSize != 784 || nw.outputSize != 10)
                {
                    Console.WriteLine("Invalid network");
                    return null;
                }
                Console.WriteLine("Network " + fileName + " loaded.");
                return nw;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine("Error loading " + fileName);
                return null;
            }
            finally
            {
                if (fileStream != null)
                 fileStream.Close();
            }
        }
    }
}
