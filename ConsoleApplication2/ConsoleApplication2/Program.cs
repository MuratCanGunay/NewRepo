using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Collections.Concurrent;

namespace ConsoleApplication2
{
    public class Program
    {
        static void Main(string[] args)
        {
            //Main shared memory where event producer writes data
            //and event consumer reads
            BlockingCollection<Event> bc = new BlockingCollection<Event>();
            //Create threads
            var producerThread = new Thread(() => EventProducer(bc));
            var consumerThread = new Thread(() => EventConsumer(bc));
            //This is not a observer pattern implementation
            EventList.OnAdd += l_OnAdd;

            producerThread.Start();
            consumerThread.Start();

        }

        /*
         * Event producer writes data to this data structure only if 
         * there are three sequential event that has same priority
         * Event consumer pools through this collection if there is
         * a element to consume
         * 
         */
        static readonly MyBlockingCollection<int[]> EventList = new MyBlockingCollection<int[]>();

        public static void EventProducer(BlockingCollection<Event> bc)
        {

            var rnd = new Random();
            //event producer writes to this queue as long as there is an event that has 
            //same priority otherwise clear the queue and enqueue new element.
            Queue<Event> eventQueue = new Queue<Event>(3);
            
                for (int i = 0; i < 400; i++)
                {
                    int priority = rnd.Next(1, 4);

                    Event e = new Event(priority);
                    Thread.Sleep(3000);
                    Console.WriteLine("event added with priority " + e.priority + "");
                    bc.Add(e);

                    if (eventQueue.Count > 0)
                    {
                        Event tempEvent = eventQueue.Peek();

                        if (tempEvent.priority == e.priority)
                        {
                            eventQueue.Enqueue(e);
                        }
                        else
                        {
                            eventQueue.Clear();
                            eventQueue.Enqueue(e);
                        }

                    }
                    else
                    {
                        eventQueue.Enqueue(e);
                    }

                    //three sequential events have arrived 
                    if (eventQueue.Count == 3)
                    {
                        int[] indexArray = new int[3];
                        for (int j = 0; j < 3; j++)
                        {
                            Event tobeAlerted = eventQueue.Dequeue();
                            indexArray[j] = i - j;
                        }

                        EventList.Add(indexArray);

                    }
                }
            

            
        }


        public static void EventConsumer(BlockingCollection<Event> bc)
        {
                 // Check whether there is a alert to be raised
                foreach (var curItem in EventList.GetConsumingEnumerable())
                {
                    int[] a;
                    var indexList = new int[3];
                    indexList = curItem;
                    EventList.TryTake(out a);


                    Thread.Sleep(5000);
                    Event e1 = bc.ElementAt(indexList[0]);
                    Thread.Sleep(5000);
                    Event e2 = bc.ElementAt(indexList[1]);
                    Thread.Sleep(5000);
                    Event e3 = bc.ElementAt(indexList[2]);
                    Console.WriteLine("E" + indexList[0] + "   " + "E" + indexList[1] + "   " + "E" + indexList[2] + "");
                }
            
        }


        //This is not a observer implementation
        //Intention behind this method is giving 
        //Information
        public static void l_OnAdd(object sender, EventArgs e)
        {
            Console.WriteLine("Element added to queue...");
        }
    }

    //Dummy implementation
    class MyBlockingCollection<T> : BlockingCollection<T>
    {

        public event EventHandler OnAdd;

        public void Add(T item)
        {
            if (null != OnAdd)
            {
                OnAdd(this, null);
            }
            base.Add(item);
        }

    }

}
