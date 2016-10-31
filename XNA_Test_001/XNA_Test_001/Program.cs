using System;
using System.Threading;

namespace Walk_ANN
{
#if WINDOWS
    static class Program
    {
        static void Main(string[] args)
        {
            Game1 gameObj = new Game1();
            gameObj.Run();
        }
    }
#endif
}