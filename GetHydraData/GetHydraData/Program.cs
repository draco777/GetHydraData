using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Ecng.Collections;
using Ecng.Common;
using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Candles.Compression;
using StockSharp.Algo.Storages;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace GetHydraData
{
    class Program
    {
        private static CandleManager _candleManager;

        // Путь к данным истории 
        private static string _historyPath;
        private static string _Path;

        private static Security _instr1;

        // Списки для загрузки
        private static List<Security> listOfName = new List<Security>();
        private static List<TimeSpan> listOfTimeFrame = new List<TimeSpan>();   


        readonly static DateTimeOffset _startTime = new DateTimeOffset(1995, 1, 1, 0, 0, 0, TimeSpan.FromHours(3));
        readonly static DateTimeOffset _endTime = DateTimeOffset.MaxValue;

      
       

        public static Dictionary<string, StreamWriter> LisfStreamWriters = new Dictionary<string, StreamWriter>();
        private static CandleSeries _series;

        static void Main(string[] args)
        {
            _candleManager = new CandleManager();

            if (!GetSetings())
                return;

            var storageRegistry = new StorageRegistry();
            ((LocalMarketDataDrive)storageRegistry.DefaultDrive).Path = _historyPath;

            var cbs = new TradeStorageCandleBuilderSource { StorageRegistry = storageRegistry };
            _candleManager.Sources.OfType<TimeFrameCandleBuilder>().Single().Sources.Add(cbs);


            

            _candleManager.Processing += GetCandles;


            foreach (var Sec in listOfName)
            {
                foreach (var timeFrame in listOfTimeFrame)
                {
                  
                   

                _series = new CandleSeries(typeof(TimeFrameCandle), Sec, timeFrame);

                LisfStreamWriters.Add(_series.ToString(), new StreamWriter(GetFileName(_series), false));


                _candleManager.Start(_series, _startTime, _endTime);

                


 
    }

            }

     

            Console.ReadKey();

            // Закроем все потоки которые мы записывали

            foreach (var strim in LisfStreamWriters)
            {
                strim.Value.Close();   
            }

        

        }

        private static void GetCandles(CandleSeries series, Candle candle)
        {
            if (candle.State == CandleStates.Finished)
            {
                LisfStreamWriters[series.ToString()].WriteLine(candle.OpenTime.Date.ToString("d") + " " + candle.OpenTime.DateTime.ToString("T") +
                                       " " + candle.OpenPrice.ToString()+ " "+ candle.HighPrice.ToString() +" "+
                                       candle.LowPrice.ToString() +" "+candle.ClosePrice.ToString() +" "+candle.TotalVolume.ToString());

                Console.Write(".");
            }
        }

        private static string GetFileName(CandleSeries candleSeries)
        {

            // TimeFrameCandle_SBER@TQBR_00-30-00
            TimeSpan temp = (TimeSpan) candleSeries.Arg;
            string fullPath = _Path + temp.ToString("hhmmss");

            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
            }

            return fullPath + @"\"+ candleSeries.ToString().Replace("TimeFrameCandle_","") + ".txt";
        }

        private static bool GetSetings()
        {
            XmlDocument xml = new XmlDocument();
            xml.Load("Settings.xml");

            // Заполним папки для получения данных и папку назначения
            foreach (XmlNode n in xml.SelectNodes("/Robot-Settings/Settings"))
            {
                _historyPath = n.SelectSingleNode("historyPath").InnerText;
                _Path = n.SelectSingleNode("Path").InnerText;

            }



            // Заполним названия инструментов
            foreach (XmlNode n in xml.SelectNodes("/Robot-Settings/Security"))
            {
                foreach (XmlNode Node in n)
                {

                    listOfName.Add(new Security() {Id = Node.InnerText, Board = ExchangeBoard.Micex});

                }
             

            }

            // Заполним таймфреймы
            foreach (XmlNode n in xml.SelectNodes("/Robot-Settings/TimeFrame"))
            {
                foreach (XmlNode Node in n)
                {
                    if (Node.Name.ToString()== "Seconds")
                        listOfTimeFrame.Add(TimeSpan.FromSeconds((int.Parse(Node.InnerText))));
                    else
                        listOfTimeFrame.Add(TimeSpan.FromMinutes((int.Parse(Node.InnerText))));

                }

            }

            return true;
        }

    }
}
