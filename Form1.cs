using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Xml;

namespace GuideViewer
{
    public partial class Form1 : Form
    {
        private System.Threading.Thread _worker;
        private List<string> _xmlFilenames = new List<string>();

        private List<Channel> _channels = new List<Channel>();
        private List<Programme> _programmes = new List<Programme>();

        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Multiselect = true;
            DialogResult result = ofd.ShowDialog();
            if (result == DialogResult.OK)
            {
                _xmlFilenames.Clear();
                _xmlFilenames.AddRange(ofd.FileNames);
                _worker = new System.Threading.Thread(new System.Threading.ThreadStart(Run));
                _worker.Start();
            }
        }

        private void Run()
        {
            XmlDocument doc = new XmlDocument();
            foreach (string xmlFilename in _xmlFilenames)
            {
                doc.Load(xmlFilename);

                XmlElement elem = doc.DocumentElement;
                Dictionary<string, Channel> channels = new Dictionary<string, Channel>();

                if (elem.Name == "tv")
                {
                    foreach (XmlNode node in elem.ChildNodes)
                    {
                        if (node.Name == "channel")
                        {
                            Channel ch = new Channel();
                            ch.id = node.Attributes["id"].Value;
                            ch.name = node["display-name"].InnerText;
                            channels.Add(ch.id, ch);
                            _channels.Add(ch);
                        }
                        else if (node.Name == "programme")
                        {
                            Programme pr = new Programme();
                            string chid = node.Attributes["channel"].Value;
                            pr.channel = channels[chid];
                            pr.startTime = ParseDateTime(node.Attributes["start"].Value);
                            pr.stopTime = ParseDateTime(node.Attributes["stop"].Value);

                            pr.title = node["title"].InnerText;
                            if (node["desc"] != null)
                            {
                                pr.desc = node["desc"].InnerText;
                            }
                            if (node["category"] != null)
                            {
                                pr.category = node["category"].InnerText;
                            }
                            if (node["rating"] != null)
                            {
                                if (node["rating"]["value"] != null)
                                    pr.rating = node["rating"]["value"].InnerText;
                            }

                            _programmes.Add(pr);
                        }
                    }
                }
            }

            this.Invoke(new MethodInvoker(this.RefreshChannels));
            this.Invoke(new MethodInvoker(this.RefreshView));
        }

        private DateTime ParseDateTime(string s)
        {
            int year = Int32.Parse(s.Substring(0, 4));
            int month = Int32.Parse(s.Substring(4, 2));
            int day = Int32.Parse(s.Substring(6, 2));

            int hour = Int32.Parse(s.Substring(8, 2));
            int minute = Int32.Parse(s.Substring(10, 2));
            int second = Int32.Parse(s.Substring(12, 2));

            string tzDir = s.Substring(15, 1);
            int tzHour = Int32.Parse(s.Substring(16, 2));
            int tzMinute = Int32.Parse(s.Substring(18, 2));

            DateTime result = new DateTime(year, month, day, hour, minute, second);
            if (tzDir == "-")
            {
                result = result.AddHours(tzHour).AddMinutes(tzMinute);
            }
            if (tzDir == "+")
            {
                result = result.AddHours(-1 * tzHour).AddMinutes(-1 * tzMinute);
            }
            return result;
        }

        private void RefreshChannels()
        {
            checkedListBox1.Items.Clear();

            foreach (Channel channel in _channels)
            {
                checkedListBox1.Items.Add(channel);
            }
        }

        private void RefreshView()
        {
            panel1.Controls.Clear();

            if (_programmes.Count == 0)
                return;
            
            DateTime firstTime = _programmes[0].startTime;
            DateTime lastTime = _programmes[0].stopTime;
            int x;
            int y;

            const int XOFFSET = 110;
            const int YOFFSET = 20;
            const int ITEMWIDTH = 200;
            const int HOURHEIGHT = 120;

            foreach (Programme pr in _programmes)
            {
                if (firstTime > pr.startTime)
                    firstTime = pr.startTime;
                if (lastTime < pr.stopTime)
                    lastTime = pr.stopTime;
            }

            // Draw channel headings
            int pos = 0;
            foreach (Channel ch in checkedListBox1.CheckedItems)
            {
                x = pos * ITEMWIDTH;
                pos++;

                Label lb = new Label();
                lb.Text = ch.name;
                lb.Location = new Point(x + XOFFSET, 0);
                lb.Size = new Size(ITEMWIDTH - 0, YOFFSET - 0);
                lb.BorderStyle = BorderStyle.FixedSingle;
                panel1.Controls.Add(lb);
            }

            // Draw times
            DateTime currTime = new DateTime(firstTime.Year, firstTime.Month, firstTime.Day, firstTime.Hour, 0, 0);
            for (; currTime < lastTime; currTime = currTime.AddHours(1.0))
            {
                TimeSpan span = currTime.Subtract(firstTime);
                y = (int)span.TotalMinutes;
                y = (int)(y * HOURHEIGHT / 60.0);

                Label lb = new Label();
                lb.Text = currTime.ToLocalTime().ToString("ddd dd/MM/yy\r\nHH:mm");
                lb.Location = new Point(0, y + YOFFSET);
                lb.Size = new Size(XOFFSET - 10, HOURHEIGHT - 0);
                lb.BorderStyle = BorderStyle.FixedSingle;

                int col = (int)(currTime.ToLocalTime().TimeOfDay.TotalMinutes / 12.0) + 128;
                lb.BackColor = Color.FromArgb(col, col, 255);

                panel1.Controls.Add(lb);
            }

            
            // Draw programs
            int xoffset = 0;
            foreach (Programme pr in _programmes)
            {
                if (checkedListBox1.CheckedItems.Contains(pr.channel) == false)
                    continue;

                x = checkedListBox1.CheckedItems.IndexOf(pr.channel) * ITEMWIDTH;
                TimeSpan span = pr.startTime.Subtract(firstTime);
                y = (int)span.TotalMinutes;

                span = pr.stopTime.Subtract(firstTime);
                int height = (int)span.TotalMinutes - y;

                y = (int)(y * HOURHEIGHT / 60.0);
                if (height < 1)
                    height = 1;
                if (height > 600)
                    height = 600;
                height = (int)(height * HOURHEIGHT / 60.0);

                int width = ITEMWIDTH;
                width = (int)(width * 0.75);
                if (xoffset == 0)
                {
                    xoffset = 1;
                }
                else
                {
                    x += (int)(ITEMWIDTH * 0.25);
                    xoffset = 0;
                }

                Label lb = new Label();
                TimeSpan duration = pr.stopTime.Subtract(pr.startTime);
                lb.Text = pr.title + "\r\n" + pr.startTime.ToLocalTime().ToString("h:mm:ss tt") + "  " + duration.ToString();
                lb.Tag = pr;
                lb.Location = new Point(x + XOFFSET, y + YOFFSET);
                lb.Size = new Size(width - 0, height - 0);
                lb.BorderStyle = BorderStyle.FixedSingle;
                lb.MouseEnter += new EventHandler(lb_MouseEnter);
                panel1.Controls.Add(lb);
            }
        }

        void lb_MouseEnter(object sender, EventArgs e)
        {
            Label lb = sender as Label;
            Programme pr = lb.Tag as Programme;
            if (lb != null)
            {
                TimeSpan duration = pr.stopTime.Subtract(pr.startTime);
                textBox1.Text = pr.startTime.ToLocalTime().ToString("h:mm:ss tt") + "  " + duration.ToString() + "\r\n";
                textBox1.Text += pr.title + "   (" + pr.category + ")\r\n";
                textBox1.Text += pr.desc + "\r\n";
            }
        }

        private void buttonClear_Click(object sender, EventArgs e)
        {
            _programmes.Clear();
            _channels.Clear();
            RefreshChannels();
            RefreshView();
        }

        private void buttonShowChecked_Click(object sender, EventArgs e)
        {
            RefreshView();
        }

        private void checkedListBox1_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.None;
            if ((e.AllowedEffect & DragDropEffects.Copy) == 0)
                return;

            if (e.Data.GetDataPresent("FileName"))
            {
                string[] filenames = (string[])e.Data.GetData("FileDrop");
                foreach (string filename in filenames)
                {
                    System.IO.FileInfo fi = new System.IO.FileInfo(filename);
                    if (fi.Exists == false)
                        return;
                }
            }
            e.Effect = DragDropEffects.All;
        }

        private void checkedListBox1_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("FileName"))
            {
                string[] filenames = (string[])e.Data.GetData("FileDrop");
                foreach (string filename in filenames)
                {
                    System.IO.FileInfo fi = new System.IO.FileInfo(filename);
                    if (fi.Exists == false)
                        return;
                }
                _xmlFilenames.Clear();
                _xmlFilenames.AddRange(filenames);
                _worker = new System.Threading.Thread(new System.Threading.ThreadStart(Run));
                _worker.Start();
            }
        }

    }
}