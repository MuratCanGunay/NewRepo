﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace EventProcessor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        #region Private Members

        private const int FIVE_THOUSAND_MILLISECONDS = 5000;
        private const int THREE_THOUSAND_MILLISECONDS = 3000;
        private double progressBarValue;
        private string producedEvent;
        private MyBlockingCollection<int[]> EventList;
        private bool isButtonClicked;
        BlockingCollection<Event> bc;
        #endregion

        #region Private Methods

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (isButtonClicked)
                pbStatus.Value = 0;
            //Main shared memory where event producer writes data
            //and event consumer reads
            EventList = new MyBlockingCollection<int[]>();
            bc = new BlockingCollection<Event>();
            //Create threads
            var producerThread = new Thread(() => ProduceEvent(bc));
            
           
            EventList.OnAdd -= l_OnAdd;
            EventList.OnAdd += l_OnAdd;

            producerThread.Start();
            

            isButtonClicked = true;

            btn.IsEnabled = false;
        }

        #endregion

        #region Constructors

        public MainWindow()
        {
            this.DataContext = this;
            InitializeComponent();
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// 
        /// </summary>
        public double ProgressBarValue
        {
            get { return progressBarValue; }
            set
            {
                if (progressBarValue != value)
                {
                    progressBarValue = value;
                    OnPropertyChanged("ProgressBarValue");
                }
            }
        }

        public string ProducedEvent
        {
            get
            {
                return producedEvent;
            }
            set
            {
                if (producedEvent != value)
                {
                    producedEvent = value;
                    OnPropertyChanged("ProducedEvent");
                }
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bc"></param>
        public void ProduceEvent(BlockingCollection<Event> bc)
        {
            try
            {
                var rnd = new Random();
                //event producer writes to this queue as long as there is an event that has 
                //same priority otherwise clear the queue and enqueue new element.
                Queue<Event> eventQueue = new Queue<Event>(3);

                for (int i = 0; i < 400; i++)
                {
                    int priority = rnd.Next(1, 4);

                    Event e = new Event(priority);
                    Thread.Sleep(THREE_THOUSAND_MILLISECONDS);
                    Console.WriteLine("event added with priority " + e.priority + "");
                    bc.Add(e);
                    ProgressBarValue++;
                    ProducedEvent = "Üretilen event sayısı :" + ProgressBarValue + "/400";
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
                EventList.CompleteAdding();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bc"></param>
        public void ConsumeEvent(object a)
        {
            try
            {
                string result = string.Empty;
                // Check whether there is a alert to be raised
                var curItem = EventList.GetConsumingEnumerable();
                int[] tempArray;
                var indexList = new int[3];
                while (EventList.TryTake(out tempArray, 5))
                {                 
                    indexList = tempArray;
                }

                    Thread.Sleep(FIVE_THOUSAND_MILLISECONDS);
                    Event e1 = bc.ElementAt(indexList[0]);
                    Thread.Sleep(FIVE_THOUSAND_MILLISECONDS);
                    Event e2 = bc.ElementAt(indexList[1]);
                    Thread.Sleep(FIVE_THOUSAND_MILLISECONDS);
                    Event e3 = bc.ElementAt(indexList[2]);
                    result += "E" + indexList[0] + "   " + "E" + indexList[1] + "   " + "E" + indexList[2] + Environment.NewLine;
                
                MessageBox.Show(result);

                Dispatcher.Invoke(() => { btn.IsEnabled = true; });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        //event and threadpool
        public void l_OnAdd(object sender, EventArgs e)
        {

            ThreadPool.QueueUserWorkItem(new WaitCallback(ConsumeEvent), string.Empty);
            
        }

        #endregion

        #region PropertyChanged Members

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;


        #endregion
    }

    class MyBlockingCollection<T> : BlockingCollection<T> 
    {
        public event EventHandler OnAdd;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
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
