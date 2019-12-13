using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NN
{
    [Serializable]
    public class Layer
    {
        public int _nodeCount;
        public Node[] nodes;
        public Layer previous;
        public float[] dy; // backpropagation temp value, delta y

        public Layer(int nodeCount, Layer previousLayer = null)
        {
            _nodeCount = nodeCount;
            previous = previousLayer;
            nodes = new Node[nodeCount];
            dy = new float[nodeCount];
            if (previous != null)
            {
                // weight setup
                Random rnd = new Random();
                for (int n = 0; n < nodes.Length; n++)
                {
                    nodes[n].weights = new float[previous._nodeCount];
                    for (int i = 0; i < previous._nodeCount; i++)
                    {
                        nodes[n].weights[i] = (float)(rnd.NextDouble() - 0.5f) / _nodeCount / 1000; // assign weights
                    }
                }
            }
        }

        public float[] GetNodeValues()
        {
            float[] output = new float[_nodeCount];
            for (int i = 0; i < _nodeCount; i++)
            {
                output[i] = nodes[i];
            }
            return output;
        }

        public void FlushDy()
        {
            for (int i = 0; i < dy.Length; i++)
            {
                dy[i] = 0f;
            }
        }
    }

    [Serializable]
    public struct Node
    {
        public float value;
        public float[] weights;

        public static implicit operator float(Node n) => n.value;
    }

    [Serializable]
    class NeuralNetwork
    {
        public int inputSize;
        public int outputSize;
        public string name = "";

        private Layer[] layers;
        private int hiddenLayerSize;

        public NeuralNetwork(int layerCount, int hiddenLayerNodes, int inputNodes, int outputNodes)
        {
            // init layers
            inputSize = inputNodes;
            outputSize = outputNodes;
            hiddenLayerSize = hiddenLayerNodes;
            layers = new Layer[layerCount];
            layers[0] = new Layer(inputNodes); // input layer
            for (int i = 1; i < layerCount; i++)
            {
                if (i + 1 == layerCount)
                {
                    // last layer
                    layers[i] = new Layer(outputNodes, layers[i - 1]);
                    break;
                }
                if (layers[i - 1] != null)
                    layers[i] = new Layer(hiddenLayerNodes, layers[i - 1]);
            }
        }

        public float sigmoid(float x, bool derivative = false)
        {
            if (!derivative)
                return (float)(1 / (1 + Math.Exp(-x)));
            else
                return x * (1 - x);
        }

        public float[] sigmoid(float[] x, bool derivative = false)
        {
            float[] output = new float[x.Length];
            for (int i = 0; i < x.Length; i++)
                output[i] = sigmoid(x[i], derivative);
            return output;
        }

        public float ReLU(float x, bool derivative = false)
        {
            if (!derivative)
                return Math.Max(0, x);
            else
                return x > 0 ? 1 : 0;
        }
        public float[] ReLU(float[] x, bool derivative = false)
        {
            float[] output = new float[x.Length];
            for (int i = 0; i < x.Length; i++)
                output[i] = ReLU(x[i], derivative);
            return output;
        }

        public static float[] Dot(float[] fa1, float[] fa2)
        {
            float[] output = new float[fa1.Length];
            for (int i = 0; i < fa1.Length; i++)
            {
                output[i] = fa1[i] * fa2[i];
            }
            return output;
        }

        public float[] Pass(float[] input)
        {
            // set input
            if (input.Length != inputSize)
            {
                Console.WriteLine("Invalid input.");
                return null;
            }
            for (int i = 0; i < input.Length; i++)
            {
                layers[0].nodes[i].value = input[i];
            }

            for (int l = 0; l < layers.Length; l++)
            {
                // layer loop
                if (layers[l].previous != null)
                {
                    for (int n = 0; n < layers[l].nodes.Length; n++)
                    {
                        // node loop
                        float nodeValue = Dot(layers[l].previous.GetNodeValues(), layers[l].nodes[n].weights).Sum(); // previous layer node values * weights summed
                        layers[l].nodes[n].value = ReLU(nodeValue); // apply activation function and set node value
                    }
                }
            }
            // output last layer
            return layers[layers.Length - 1].GetNodeValues();
        }

        public void Train(float[] input, float[] modelOutput, float speed)
        {
            float[] output = Pass(input);

            // calc MSE
            /*float[] mse = new float[output.Length];
            for (int i = 0; i < output.Length; i++)
            {
                mse[i] = (float)Math.Pow(output[i] - modelOutput[i], 2);
            }*/
            //mse = mse / (output.Length * 2);

            layers[layers.Length - 1].dy = SubtractVectors(modelOutput, output); // set up last layer for backprop
            //layers[layers.Length - 1].dy = mse;
            for (int l = layers.Length - 1; l >= 1; l--)
            {
                // layer loop (backpropagation)
                for (int n = 0; n < layers[l].nodes.Length; n++)
                {
                    // node loop
                    layers[l].dy[n] = layers[l].dy[n] * ReLU(layers[l].nodes[n], true); // node error to delta, ReLU derivative
                    float nodeDy = layers[l].dy[n];
                    for (int w = 0; w < layers[l].nodes[n].weights.Length; w++)
                    {
                        // weight loop
                        float weight = layers[l].nodes[n].weights[w];
                        layers[l - 1].dy[w] += nodeDy * weight; // backprop to next layer
                        // calc weight derivative and adjust
                        float connectedNode = layers[l - 1].nodes[w];
                        float dw = nodeDy * connectedNode;
                        layers[l].nodes[n].weights[w] += dw * speed;
                    }
                }
            }

            // clear dy values
            foreach (Layer l in layers)
                l.FlushDy();
        }

        public float[] GetOutput()
        {
            return layers[layers.Length - 1].GetNodeValues();
        }

        public int GetHiddenLayerCount()
        {
            return layers.Length - 2;
        }
        public int GetHiddenLayerNodeCount()
        {
            return hiddenLayerSize;
        }

        public static float[] SubtractVectors(float[] fa1, float[] fa2)
        {
            float[] output = new float[fa1.Length];
            for (int i = 0; i < fa1.Length; i++)
            {
                output[i] = fa1[i] - fa2[i];
            }
            return output;
        }

        public static float[] DigitToFloats(int n)
        {
            float[] output = new float[10];
            output[n] = 1f;
            return output;
        }

        public static int FloatsToDigit(float[] fa)
        {
            int largest = 0;
            for (int i = 0; i < fa.Length; i++)
            {
                if (fa[i] > fa[largest])
                    largest = i;
            }
            return largest;
        }
    }
}