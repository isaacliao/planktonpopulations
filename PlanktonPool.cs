using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PlanktonPopulations
{
    /// <summary>
    /// A singleton that holds a pool of Plankton objects. 
    /// Instead of having to allocate new memory every time new Plankton are needed, just get it from the pool of available plankton.
    /// Need to return Plankton to the available pool when they are done.
    /// </summary>
    class PlanktonPool
    {
        private static readonly PlanktonPool Instance = new PlanktonPool();
        private Queue<Plankton> planktonQueue = new Queue<Plankton>(Settings.PLANKTON_MAX_TOTAL);
        private Plankton[] planktonArray = new Plankton[Settings.PLANKTON_MAX_TOTAL];

        /// <summary>
        /// Private constructor to ensure that only one instance is ever created
        /// </summary>
        private PlanktonPool()
        {
            // Create a bunch of plankton during initialization
            for (int i = 0; i < Settings.PLANKTON_MAX_TOTAL; i++)
            {
                this.planktonArray[i] = new Plankton();
                this.planktonQueue.Enqueue(planktonArray[i]);
            }
        }

        /// <summary>
        /// Gets a Plankton from the pool.
        /// </summary>
        /// <returns></returns>
        public static Plankton GetPlankton()
        {
            return Instance.planktonQueue.Dequeue();
        }

        /// <summary>
        /// Returns a Plankton to the pool.
        /// </summary>
        public static void ReturnPlankton(Plankton returnPlankton)
        {
            Instance.planktonQueue.Enqueue(returnPlankton);
        }

        public static int AvailableCount()
        {
            return Instance.planktonQueue.Count();
        }
    }
}
