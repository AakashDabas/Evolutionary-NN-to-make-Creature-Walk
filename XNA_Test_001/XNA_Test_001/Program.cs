using System;
using System.Threading;
using GeneticNN;

namespace Walk_ANN
{
#if WINDOWS || XBOX
    static class Program
    {
        static void Main(string[] args)
        {
            GeneticNeuralNetwork neuralObj = new GeneticNeuralNetwork(10, 10, 1);
            Game1 gameObj = new Game1();
            gameObj.start();
        }
    }
#endif
}