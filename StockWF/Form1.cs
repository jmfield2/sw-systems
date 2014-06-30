using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Collections; // For ArrayList, etc
using System.Net; // WebUtility, etc
using System.IO;

namespace StockWF
{
    /// <summary>
    /// Interaction logic for Form1
    /// </summary>
    public partial class Form1 : Form
    {

        // storage for each candlestick 
        protected List<aCandleStick> candleSticks;
        private String headers; // Used to save headers from loadFrom* functions

        /// <summary>
        /// Constructor
        /// </summary>
        public Form1()
        {
            candleSticks = new List<aCandleStick>(); // Initialize list
            headers = null;

            // initialize .net components
            InitializeComponent();

            typeChoice.SelectedIndex = 0; // Set default to 'Daily'

            startDate.Value = DateTime.Now;
            endDate.Value = DateTime.Now;

            // Initialize data directory

            // XXX Application.StartupPath 
            //system.io.directory.GetCurrentDirectory()
            //setCurrentDirectory()

            try
            {
                System.IO.Directory.CreateDirectory("TICKERS"); // Create folder
                System.IO.Directory.CreateDirectory("TICKERS/DAILY"); // Daily
                System.IO.Directory.CreateDirectory("TICKERS/WEEKLY"); // Weekly
                System.IO.Directory.CreateDirectory("TICKERS/MONTHLY"); // Monthly

                // Find all tickers saved and add to listbox
                HashSet<String> names = new HashSet<string>(); // Use hashset for uniqueness

                String[] files = Directory.GetFiles("TICKERS/DAILY");
                foreach(String file in files) names.Add(Path.GetFileNameWithoutExtension(file)); // get file-part

                files = Directory.GetFiles("TICKERS/WEEKLY");
                foreach (String file in files) names.Add(Path.GetFileNameWithoutExtension(file)); // get file-part

                files = Directory.GetFiles("TICKERS/MONTHLY");
                foreach (String file in files) names.Add(Path.GetFileNameWithoutExtension(file)); // get file-part

                foreach (String name in names) if (!listBox1.Items.Contains(name)) listBox1.Items.Add(name); // If not there, add to list
                
            }
            catch (Exception e) {
                MessageBox.Show("Error: {}", e.ToString()); // tell user about error
                Application.Exit(); // Close program
            }

        }

        /// <summary>
        /// Helper function to validate the expected input controls (txtName, typeChoice, startDate, endDate) and add them to the errors array and p dictionary
        /// </summary>
        private void parseForm(ArrayList errors, Dictionary<String, String> p)
        {
            // txtName
            if (txtName.Text.Length <= 0) errors.Add("Name is required");
            else p.Add("s", txtName.Text); // set S key to txtName value for TICKER

            // typeChoice
            // Set the parameter based on the three choices, and default to Daily
            switch (typeChoice.SelectedItem.ToString())
            {
                default:
                case "Daily":
                    p.Add("g", "d"); // Set G key to D for daily
                    break;
                case "Weekly":
                    p.Add("g", "w"); // Set G key to W for weekly
                    break;
                case "Monthly":
                    p.Add("g", "m"); // Set G key to M for monthly
                    break;
            }

            // startDate 
            // Check for a valid selection, then set month=a, day=b, and year=c
            if (startDate.Value.ToString() == "") errors.Add("Start date is required");
            else
            {
                p.Add("a", Convert.ToString(startDate.Value.Month - 1)); // set A key to MONTH-1 for start date
                p.Add("b", Convert.ToString(startDate.Value.Day)); // set B key to DAY for start date
                p.Add("c", Convert.ToString(startDate.Value.Year)); // set C key to YEAR for start date
            }

            // endDate
            // Check for a valid selection, then set month=d, day=e, and year=f
            if (startDate.Value.ToString() == "") errors.Add("End date is required");
            else
            {
                p.Add("d", Convert.ToString(endDate.Value.Month - 1)); // set D key to MONTH-1 for end date
                p.Add("e", Convert.ToString(endDate.Value.Day)); // set E key to DAY for end date
                p.Add("f", Convert.ToString(endDate.Value.Year)); // set F key to YEAR for end date
            }
        }

        /// <summary>
        /// Helper function to show a msgbox with errors and return boolean true/false
        /// </summary>
        /// <param name="errors">ArrayList</param>
        /// <returns>Boolean</returns>        
        private bool validateForm(ArrayList errors)
        {
            // If errors are set, dont continue
            if (errors.Count > 0)
            {
                // Show a Message Box with the errors array joined with commas
                MessageBox.Show(String.Format("Errors:\n{0}", String.Join("\n", errors.ToArray())));
                return false; // And bail from click handler
            }

            return true;
        }

        /// <summary>
        /// Download data from yahoo using data in dictionary and returning a streamreader from stream
        /// </summary>
        /// <param name="p"></param>
        /// <param name="s"></param>
        /// <returns></returns>
        private StreamReader downloadFromYahoo(Dictionary<String, String> p, Stream s)
        {
            // Query string parameters from:
            // https://code.google.com/p/yahoo-finance-managed/wiki/csvHistQuotesDownload

            String url = "http://ichart.yahoo.com/table.csv?"; // Base url

            StringBuilder qstr = new StringBuilder(); // Use a stringbuilder to incrementally build our query string
            for (int i = 0; i < p.Count; i++)
            { // Iterate each element of our dict
                qstr.AppendFormat("{0}={1}", p.ElementAt(i).Key, WebUtility.UrlEncode(p.ElementAt(i).Value)); // Append key=value to query string where value is properly urlencoded
                if (i < p.Count) qstr.AppendFormat("&"); // If not at the end of the dict, add a & 
            }

            // Use WebClient to download CSV-formatted data
            WebClient wc = new WebClient(); // Initialize object
            StreamReader sr; // Object to hold our stream

            try
            {
                s = wc.OpenRead(url + qstr.ToString()); // Open for reading
                sr = new StreamReader(s, System.Text.Encoding.UTF8); // Create Streamreader from Webclient stream
            }
            catch (System.Net.WebException ex)
            {
                MessageBox.Show(String.Format("A Web Exception ({0}) occurred!", ex.ToString())); // Let user know about error
                return null; // bail
            }

            return sr;
        }

        /// <summary>
        /// Helper function to create relative file path for tickers/type/name.csv
        /// </summary>
        /// <param name="name"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        private String buildFilePath(String name, String type)
        {

            StringBuilder fb = new StringBuilder(); // use a stringbuilder to build our path

            fb.Append("TICKERS/"); // base directory

            // build filepath by requested data type
            if (type == "d") fb.Append("DAILY/"); // Add Daily path
            else if (type == "w") fb.Append("WEEKLY/"); // Add Weekly path
            else if (type == "m") fb.Append("MONTHLY/"); // Add Monthly path

            fb.Append(name.ToUpper()); // Add filename as ticker
            fb.Append(".csv"); // Add file suffix

            return fb.ToString();
        }

        /// <summary>
        /// Load data for ticker in dictionary from filesystem
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        private List<aCandleStick> loadDataFromFile(Dictionary<String, String> p)
        {
            Stream ws = null; // Stream for web access
            FileStream fs = null; // Stream for file access
            StreamReader sr = null; // Object to hold stream
            List<aCandleStick> candleSticks = new List<aCandleStick>(); // list of candle sticks            

            DateTime start = new DateTime(int.Parse(p["c"]), int.Parse(p["a"])+1, int.Parse(p["b"]));
            DateTime end = new DateTime(int.Parse(p["f"]), int.Parse(p["d"])+1, int.Parse(p["e"]));

            String filename = buildFilePath(p["s"], p["g"]);

            try
            {
                fs = new FileStream(filename, FileMode.OpenOrCreate); // open file
                sr = new StreamReader(fs);
            }
            catch (Exception ex)
            {
                MessageBox.Show(String.Format("An error occurred while accessing the file {0}: {1}", filename, ex.ToString())); // let user know        
                return null; // bail
            }            

            String tmp_headers = sr.ReadLine(); // Read line of headers
            if (tmp_headers != null && tmp_headers.Length > 0) headers = tmp_headers; // in case file was empty, dont trash previous headers

            while (!sr.EndOfStream) // loop each line of stream
            {
                aCandleStick tmp = new aCandleStick(); // initialize a new candle stick

                tmp.setType(p["g"]);  // set daily type for candlestick

                if (tmp.load(sr))
                { 
                    // if aCandleStick successfully loaded a candle stick                    

                    if (tmp.date >= start && tmp.date <= end) // and within the range we want
                        candleSticks.Add(tmp); // add to our list
                }
            }

            // Cleanup streamreader, then ws or fs stream
            sr.Close();
            if (fs != null) fs.Close();

            return candleSticks;
        }

        /// <summary>
        /// Load data from yahoo into list of candlesticks
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        private List<aCandleStick> loadDataFromYahoo(Dictionary<String, String> p)
        {

            Stream ws = null; // Stream for web access           
            StreamReader sr = null; // Object to hold stream
            List<aCandleStick> candleSticks = new List<aCandleStick>(); // list of candle sticks            

            sr = downloadFromYahoo(p, ws); // download csv from remote yahoo endpoint            
            if (sr == null)
            {
                return null;
            }

            headers = sr.ReadLine(); // Read line of headers

            while (!sr.EndOfStream) // loop each line of stream
            {
                aCandleStick tmp = new aCandleStick(); // initialize a new candle stick

                tmp.setType(p["g"]);  // set daily type for candlestick

                if (tmp.load(sr)) // if aCandleStick successfully loaded a candle stick
                    candleSticks.Add(tmp); // add to our list
            }

            // Cleanup streamreader, then ws or fs stream
            sr.Close();
            if (ws != null) ws.Close();

            return candleSticks;
        }

        /// <summary>
        /// Save ticker associated with dictionary and candlesticks to file
        /// </summary>
        /// <param name="p"></param>
        private void saveDataToFile(Dictionary<String, String> p)
        {

            StreamWriter sw = null; // Stream to write data to file
            FileStream fs = null; // Stream to file

            String filename = buildFilePath(p["s"], p["g"]); // file path

            try
            {
                fs = new FileStream(filename, FileMode.OpenOrCreate); // open file
                sw = new StreamWriter(fs); // create streamwriter from filestream
            }
            catch (Exception ex)
            {
                MessageBox.Show(String.Format("An error occurred while writing the file {0}: {1}", filename, ex.ToString())); // let user know
                return; // bail
            }

            sw.WriteLine(headers); // write headers first
            for (int i = 0; i < candleSticks.Count(); i++)
            {
                candleSticks[i].save(sw); // Save itself
            }

            // Cleanup filestreams
            sw.Close();
            fs.Close();
        }

        /// <summary>
        /// Append new sticks to the file
        /// </summary>
        /// <param name="p"></param>
        /// <param name="old"></param>
        /// <param name="newsticks"></param>
        private void appendDataToFile(Dictionary<String, String> p, List<aCandleStick> old, List<aCandleStick> newsticks) 
        {

            StreamWriter sw = null; // Stream to write data to file
            FileStream fs = null; // Stream to file

            String filename = buildFilePath(p["s"], p["g"]); // file path

            try
            {
                fs = new FileStream(filename, FileMode.OpenOrCreate); // open file
                fs.SetLength(0); // truncate
                sw = new StreamWriter(fs); // create streamwriter from filestream
            }
            catch (Exception ex)
            {
                MessageBox.Show(String.Format("An error occurred while writing the file {0}: {1}", filename, ex.ToString())); // let user know
                return; // bail
            }

            sw.WriteLine(headers); // write headers first

            // Begin by calling append() on each new candlestick so it can search the old list and add itself where it needs to
            for (int i = 0; i < newsticks.Count(); i++)
            {
                newsticks[i].append(sw, old);
            }

            // Add any remaining
            foreach (aCandleStick tmp in old)
            {
                tmp.save(sw);
            }

            // Cleanup filestreams
            sw.Close();
            fs.Close();
        }

        /// <summary>
        /// Search candlesticks for date ranges requested but not found
        /// </summary>
        /// <param name="p"></param>
        /// <param name="sticks"></param>
        /// <returns></returns>
        private List<Tuple<DateTime, DateTime>> getMissingDates(Dictionary<String, String> p, List<aCandleStick> sticks)
        {
            List<Tuple<DateTime, DateTime>> dt = new List<Tuple<DateTime, DateTime>>();

            // Create requested start and end dates
            DateTime start = new DateTime(int.Parse(p["c"]), int.Parse(p["a"])+1, int.Parse(p["b"]));
            DateTime end = new DateTime(int.Parse(p["f"]), int.Parse(p["d"])+1, int.Parse(p["e"]));

            bool isFound = false; // flag for whether we need to add more dates outside of the loop

            foreach (aCandleStick oneStick in sticks)
            {
                Console.WriteLine(oneStick.type + " " +  oneStick.date + " " + start.ToShortDateString() + " " + end.ToShortDateString());

                // if the date in the stick is OLDER than start, assume we have found a requested date
                if (oneStick.date <= start)
                {
                    isFound = true;
                }
                else
                {
                    isFound = false;
                    // Otherwise, add from start to this date to list
                    Tuple<DateTime, DateTime> d = new Tuple<DateTime, DateTime>(start, oneStick.date);
                    dt.Add(d);
                }

                start = oneStick.getNextDate(); // if we are at start, get next date    
                
            }
            if (start < end) isFound = false;

            // If last search was unsuccessful, add range to list
            if (!isFound && start < end) {
                Tuple<DateTime, DateTime> d = new Tuple<DateTime, DateTime>(start, end);
                dt.Add(d);
            }

            return dt;
        }

        /// <summary>
        /// Click event handler for "GO!" button
        /// </summary>
        /// <param name="sender">Default</param>
        /// <param name="e">Default</param>
        private void button1_Click(object sender, EventArgs e)
        {

            ArrayList errors = new ArrayList(); // Array holding validation errors
            Dictionary<String, String> p = new Dictionary<String, String>(); // Map holding key=value pairs for input to remote API

            // We first parse and validate the values of the form fields: Name, Period, Start, End, Source
            parseForm(errors, p); // use Helper function to parse input controls
            if (!validateForm(errors)) return; // if validation fails, bail

            statusStrip1.Text = "Processing..."; // Give user feedback in status bar
            progressBar1.Value = 0; // Give user feedback in progress bar

            candleSticks.Clear();

            // Load any existing data from CACHE if possible ... noting which dates we could not fetch
            candleSticks = loadDataFromFile(p);

            // Find missing date pairs and update list
            List<aCandleStick> rev = new List<aCandleStick>(candleSticks);
            rev.Reverse(); // Since dates are end-start, reverse them
            List<Tuple<DateTime, DateTime>> dt = getMissingDates(p, rev);            

            // IF datasource == Yahoo, download non-existant data, else notify user
            if (dt.Count() > 0 && radioButton1.Checked == true)
            {
                foreach (Tuple<DateTime, DateTime> d in dt)
                {
                    // Adjust START to new date to retrieve the missing data 
                    p["a"] = (d.Item1.Month - 1).ToString();
                    p["b"] = d.Item1.Day.ToString();
                    p["c"] = d.Item1.Year.ToString();

                    // and END
                    p["d"] = (d.Item2.Month - 1).ToString();
                    p["e"] = d.Item2.Day.ToString();
                    p["f"] = d.Item2.Year.ToString();

                    Console.WriteLine(d.Item1.ToShortDateString() + " " + d.Item2.ToShortDateString());

                    List<aCandleStick> tmp = loadDataFromYahoo(p); // reload data from yahoo

                    if (tmp == null)
                    {
                        try
                        {
                            File.Delete(buildFilePath(p["s"], p["g"])); // cleanup in case yahoo failed to load by error
                        }
                        catch () { }

                        return;
                    }
                    if (tmp.Count() > 0) {

                        // Merge lists - assuming contiguous

                        int index = 0; // start at beginning
                        for (int i = 0; i < candleSticks.Count(); i++) 
                        {
                            if (candleSticks[i].date < tmp[0].date) break;
                            index = i;
                        }

                        // Add each new stick in the right place
                        foreach (aCandleStick stick in tmp)
                        {
                            if (index >= candleSticks.Count()) candleSticks.Insert(index, stick);
                            else if (candleSticks[index].date != stick.date) // duplicate check
                                candleSticks.Insert(index, stick);
                            index++; // list grows "upward" from high - low
                            //index = Math.Max(index, 0); // clamp to 0
                        }

                    }

                }
            }
            else if (dt.Count() > 0) 
            {
                List<String> tmp = new List<String>();
                foreach (Tuple<DateTime, DateTime> d in dt) tmp.Add(String.Format("{0} -> {1}", d.Item1.ToShortDateString(), d.Item2.ToShortDateString()));
                
                MessageBox.Show(String.Format("The saved data does not include the following dates, but cannot be updated because the data source was set to FILE.\n{0}", String.Join(", ", tmp.ToArray())));
            }            

            // APPEND new data to tickers/data/daily/name.csv

            // set P date range to minValue up to start so we load all of the cached data
            p["d"] = (DateTime.MaxValue.Month - 1).ToString();
            p["e"] = DateTime.MaxValue.Day.ToString();
            p["f"] = DateTime.MaxValue.Year.ToString();
            p["a"] = (DateTime.MinValue.Month - 1).ToString();
            p["b"] = DateTime.MinValue.Day.ToString();
            p["c"] = DateTime.MinValue.Year.ToString(); 
            
            List<aCandleStick> tmpsticks = loadDataFromFile(p);

            appendDataToFile(p, tmpsticks, candleSticks);

            if (candleSticks.Count() <= 0)
            {
                MessageBox.Show("Invalid date range - no data to show.");
                return; // no data, dont show blank chart
            }

            if (!listBox1.Items.Contains(p["s"])) listBox1.Items.Add(p["s"]); // add new ticker to list

            // Load form_chart
            Form_Chart f = new Form_Chart(p["s"], candleSticks);
            f.Show(); // Show it

            statusStrip1.Text = ""; // reset

        }

          
        /// <summary>
        /// UPDATE Button handler
        /// - Update saved data incrementally for start-end period for all tickers
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button3_Click(object sender, EventArgs e)
        {
            progressBar1.Value = 0; // Initialize progress bar value

            Dictionary<String, String> p = new Dictionary<String, String>();

            p["d"] = (DateTime.Now.Month - 1).ToString();
            p["e"] = DateTime.Now.Day.ToString();
            p["f"] = DateTime.Now.Year.ToString();

            int step = 100 / listBox1.Items.Count;

            for (int i = 0; i < listBox1.Items.Count; i++) // Iterate items in listbox
            {

                // Download ticker data
                p["s"] = listBox1.Items[i].ToString();
                p["g"] = "d";

                Console.WriteLine(p["s"]);
           
                // Load everything from cache
                p["a"] = (DateTime.MinValue.Month - 1).ToString();
                p["b"] = DateTime.MinValue.Day.ToString();
                p["c"] = DateTime.MinValue.Year.ToString();                 
                List<aCandleStick> tmp = loadDataFromFile(p); // see what we have already

                if (tmp.Count() > 0)
                {
                    // get last date in file -- is the FIRST entry
                    DateTime d = tmp[0].getNextDate();
                    
                    Console.WriteLine(d.ToShortDateString());

                    p["a"] = (d.Month - 1).ToString();
                    p["b"] = d.Day.ToString();
                    p["c"] = d.Year.ToString();
                    
                }
                else
                {
                    // If nothing, basically do another populate
                    p["a"] = "0"; // Jan
                    p["b"] = "1"; // 1st
                    p["c"] = "2010";
                }

                candleSticks.Clear();
                candleSticks = loadDataFromYahoo(p);

                if (candleSticks != null)
                {
                    // APPEND new data to tickers/data/daily/name.csv                
                    appendDataToFile(p, tmp, candleSticks);
                }

                progressBar1.Value += step;

            }

            progressBar1.Value = 0; // Reset progress bar to 0
        }

        /// <summary>
        /// POPULATE button handler
        /// - Update ALL tickers with fresh data from 1/1/2010 to 6/18/2014
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button2_Click(object sender, EventArgs e)
        {            
            progressBar1.Value = 0; // Initialize progress bar value
            Dictionary<String, String> p = new Dictionary<String, String>();

            p["a"] = "0"; // January
            p["b"] = "1"; // 1st
            p["c"] = "2010";

            p["d"] = (DateTime.Now.Month - 1).ToString();
            p["e"] = DateTime.Now.Day.ToString();
            p["f"] = DateTime.Now.Year.ToString();

            int step = 100 / listBox1.Items.Count;

            for (int i = 0; i < listBox1.Items.Count; i++) // Iterate items in listbox
            {

                // Download ticker data
                p["s"] = listBox1.Items[i].ToString();
                p["g"] = "d";

                candleSticks.Clear();
                candleSticks = loadDataFromYahoo(p);

                if (candleSticks != null)
                {
                    // Save data to tickers/data/daily/name.csv
                    saveDataToFile(p);
                }

                //p["g"] = "w";
                //p["w"] = "m";                

                progressBar1.Value += step;

            }

            progressBar1.Value = 0; // Reset progress bar to 0
        }

        /// <summary>
        /// When a ticker is selected, update the form fields appropriately
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

            statusStrip1.Text = "Processing"; // Update the user via status bar

            // We don't update the type OR dates -- let the user choose
            // ... or, we could set the dates to the valid range available if found ...

            txtName.Text = listBox1.Text; // Update the form field

            statusStrip1.Text = ""; // Clear status bar

        }

        /// <summary>
        /// Menu strip : File->Exit
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit(); // Close program
        }

        /// <summary>
        /// FILE - CLEAR DATA menu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void clearDataToolStripMenuItem_Click(object sender, EventArgs e)
        {            
            Directory.Delete("TICKERS", true); // recursively delete tickers directory

            Directory.CreateDirectory("TICKERS"); // Create folder
            Directory.CreateDirectory("TICKERS/DAILY"); // Daily
            Directory.CreateDirectory("TICKERS/WEEKLY"); // Weekly
            Directory.CreateDirectory("TICKERS/MONTHLY"); // Monthly

        }
      

    }
}
