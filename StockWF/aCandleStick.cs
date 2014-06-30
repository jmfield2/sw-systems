using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockWF
{

    /// <summary>
    /// A class representing one candle stick and including the corresponding date, open, close, high, low, and color
    /// </summary>
    public class aCandleStick
    {
        // Candlestick Anatomy: http://www.bonigala.com/anatomy-of-a-candlestick
        // High <-> Close <-> Open <-> Low
        // If Open > Close color=black
        // If Open < Close color=red

        // Private member variables
        private Decimal _open, _close, _high, _low, _volume, _adjclose;
        private String _color; // The color based on candlestick logic
        private String _type; // Daily, Weekly, Monthly
        private DateTime _date; // Date for this candlestick

        // Public read-only accessors
        public Decimal open { get { return _open; } }
        public Decimal close { get { return _close; } }
        public Decimal high { get { return _high; } }
        public Decimal low { get { return _low; } }
        public Decimal volume { get { return _volume; } }
        public Decimal adjclose { get { return _adjclose; } }

        public DateTime date { get { return _date; } }
        public String color { get { return _color; } }
        public String type { get { return _type; } }

        /// <summary>
        /// Default Constructor
        /// </summary>
        public aCandleStick()
        {
            // Set local members to defaults
            _open = _close = _high = _low = 0;
            _date = DateTime.MinValue;
            _color = "";
            _type = "Daily";
        }

        /// <summary>
        /// Setter for candlestick type (Daily, Weekly, or Monthly)
        /// </summary>
        /// <param name="t"></param>
        public void setType(String t) {
            if (t == "d" || t == "Daily") this._type = "Daily";
            else if (t == "w" || t == "Weekly") this._type = "Weekly";
            else if (t == "m" || t == "Monthy") this._type = "Monthly";
            else throw new ArgumentException("aCandleStick::setType Expected Daily, Weekly, or Monthly.");
        }

        /// <summary>
        /// Based on candlestick date and type, return the next 'business' day (M-F)
        /// </summary>
        /// <returns></returns>
        public DateTime getNextDate()
        {            
            DateTime day = this._date; // copy our date to this variable

            if (this.type == "Weekly")
            {
                day = day.AddDays(7); 
                return day;
            }
            else if (this.type == "Monthly")
            {
                day = day.AddMonths(1);
                return day;
            }

            // For each of the days of the week, add corresponding # of days until next business day (M-F)
            switch (day.DayOfWeek)
            {
                case DayOfWeek.Sunday:
                case DayOfWeek.Monday:
                case DayOfWeek.Tuesday:
                case DayOfWeek.Wednesday:
                case DayOfWeek.Thursday:
                    day = day.AddDays(1);
                    break;
                case DayOfWeek.Friday:
                    day = day.AddDays(3);
                    break;
                case DayOfWeek.Saturday:
                    day = day.AddDays(2);
                    break;
                default: // in case datetime is weird
                    break;
            }

            if (day > DateTime.Now) return DateTime.Now; // Don't go into the future

            // XXX Respect the standard holidays that the US Stock Exchange observes

            return day; 
        }     

        /// <summary>
        /// Load one entry from the file
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public bool load(System.IO.StreamReader file)
        {

            // load one instance from one line
            String line = file.ReadLine();
            if (line.Length <= 0) return false; // Read an empty line ... no bueno

            char delim = ','; // CSV delimiter
            String[] columns = line.Split(delim); // Split line into respective columns by delimiter
            
            // Pre-define the columns in the order we get them from Yahoo
            String[] headers = {"Date", "Open", "High", "Low", "Close", "Volume", "Adj Close"};
            
            if (columns.Count() != headers.Count()) return false; // Bail because our line did not have all the required columns

            for (int i = 0; i < columns.Count(); i++)
            {
                // Based on column index, set corresponding member variable
                switch (headers[i])
                {
                    case "Date":
                        _date = DateTime.Parse(columns[i]); // set local date to this 
                        break;
                    case "High":
                        _high = Decimal.Parse(columns[i]); // set local high to parsed value
                        break;
                    case "Low":
                        _low = Decimal.Parse(columns[i]); // set local low to parsed value
                        break;
                    case "Open":
                        _open = Decimal.Parse(columns[i]); // set local open to parsed value
                        break;
                    case "Close":
                        _close = Decimal.Parse(columns[i]); // set local close to parsed value
                        break;
                    case "Volume":
                        _volume = Decimal.Parse(columns[i]); // set local volume to parsed value
                        break;
                    case "Adj Close":
                        _adjclose = Decimal.Parse(columns[i]); // set local adjusted close to parsed value
                        break;
                    default: // If CSV headers don't match what we expect, throw exception
                        throw new Exception(headers[i] + " not matched");
                }
            }

            return true;
        }

        /// <summary>
        /// Save itself to a file as a one line comma-delimited string
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public bool save(System.IO.StreamWriter file)
        {

            List<String> values = new List<String>(); // list of values

            values.Add(String.Format("{0}/{1}/{2}", this.date.Month, this.date.Day, this.date.Year)); // Date
            values.Add(this.open.ToString()); // Open
            values.Add(this.high.ToString()); // High
            values.Add(this.low.ToString()); // Low
            values.Add(this.close.ToString()); // Close
            values.Add(this.volume.ToString()); // Volume
            values.Add(this.adjclose.ToString()); // Adj Close

            file.WriteLine(String.Join(",", values.ToArray())); // Write joined line back to file

            return true;
        }

        /// <summary>
        /// Append new candlesticks to file in the correct place .. Assumption: candleSticks is SORTED by date already
        /// </summary>
        /// <param name="file"></param>
        /// <param name="candleSticks"></param>
        /// <returns></returns>
        public bool append(System.IO.StreamWriter file, List<aCandleStick> candleSticks)
        {
            // data comes in with NEW on top and OLD on the bottom
            List<aCandleStick> toDelete = new List<aCandleStick>();

            // Basically, iterate the candlesticks checking them against ourself and saving them along the way
            foreach (aCandleStick tmp in candleSticks)
            {
                if (tmp.date > this.date)
                {
                    tmp.save(file);
                    toDelete.Add(tmp);                    
                }
                else if (tmp.date == this.date)
                {
                    toDelete.Add(tmp);
                    break;
                }
                else break;
            }

            this.save(file);

            // Delete the candlesticks we already added so we dont duplicate next time
            foreach (aCandleStick tmp in toDelete)
            {
                candleSticks.Remove(tmp);
            }

            return true;
        }
        
    }
}
