using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.Windows.Forms.DataVisualization.Charting; // For chart-related stuff

namespace StockWF
{
    /// <summary>
    /// The class controlling the windows form with our Chart control to display the candlestick chart
    /// </summary>
    public partial class Form_Chart : Form
    {

        /// <summary>
        /// Default constructor
        /// </summary>
        public Form_Chart()
        {
            InitializeComponent(); // Initialize .NET
        }

        /// <summary>
        /// Constructor accepting the string of the current ticker, and a list of candlesticks we will graph
        /// </summary>
        /// <param name="ticker"></param>
        /// <param name="rows"></param>
        public Form_Chart(String ticker, List<aCandleStick> rows)
        {

            InitializeComponent(); // Initialize .NET

            Text = "Ticker: " + ticker; // Change title to include name of stock

            // Create the price series for chart
            Series price = new Series("Price");
            chart1.Series.Add(price); // add to chart
            chart1.Series["Price"].ChartType = SeriesChartType.Candlestick; // set the type to candlestick
            chart1.Series["Price"]["OpenCloseStyle"] = "Candlestick"; // Set the behavior of open/close data
            chart1.Series["Price"]["ShowOpenClose"] = "Both"; // Show both open and close
            chart1.Series["Price"]["PointWidth"] = "1.0"; // point width of chart data points
            chart1.Series["Price"]["PriceUpColor"] = "Green"; // Green for Close > Open (went up)
            chart1.Series["Price"]["PriceDownColor"] = "Red"; // Red for Open > Close (went down)

            chart1.ChartAreas[0].AxisX.MajorGrid.Enabled = false; // Disable gridlines for x
            chart1.ChartAreas[0].AxisY.MajorGrid.Enabled = false; // Disable gridlines for y

            int cnt = 0; // counting # of points added to series

            Decimal low = 0, high = 0;

            // Iterate through data from last to first -- so date is in ascending order
            for (int i = rows.Count() - 1; i >= 0; i--)
            {
                chart1.Series["Price"].Points.AddXY(rows[i].date.ToShortDateString(), Math.Truncate((double)rows[i].high * 100.0) / 100.0); // add X-axis date, with high y-value                
                chart1.Series["Price"].Points[cnt].YValues[1] = Math.Truncate((double)rows[i].low * 100.0) / 100.0; // add another y-value for low
                chart1.Series["Price"].Points[cnt].YValues[2] = Math.Truncate((double)rows[i].open * 100.0) / 100.0; // add another y-value for open
                chart1.Series["Price"].Points[cnt].YValues[3] = Math.Truncate((double)rows[i].close * 100.0) / 100.0; // add another y-value for close

                if (rows[i].high > high) high = rows[i].high; // if high > our high, set
                if (rows[i].low < low || low == 0) low = rows[i].low; // if low < low, set

                cnt++; // increment our cnt
            }

            double diff = Math.Max((double)(high - low), 2); // Find normalized difference

            // Set the scale to be just above and below the max/min based on a normalized difference
            chart1.ChartAreas[0].AxisY.Maximum = (double)high + Math.Min(diff / 2, 25);
            chart1.ChartAreas[0].AxisY.Minimum = (double)low - Math.Min(diff / 2, 25);

            if (chart1.ChartAreas[0].AxisY.Minimum < 0) chart1.ChartAreas[0].AxisY.Minimum = 0; // if < 0, reset to 0

            label1.Text = String.Format("Low: {0} High: {1}", low.ToString("G"), high.ToString("G"));

        }
        
    }
}
