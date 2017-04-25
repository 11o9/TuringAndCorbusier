using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Reports
{
    /// <summary>
    /// xmlcover.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class xmlcover : UserControl
    {
        public xmlcover()
        {
            InitializeComponent();

            setDate();
        }

        public void setDate()
        {
            date.Text = DateTime.Now.ToString("yyyy-MM-dd");
        }

        public string SetTitle
        {
            set
            {
                List<int> splitIndex = new List<int>();

                if (!value.Contains(' '))
                {
                    MainWidnowGrid.Children.Add(newTitleTextBlock(new Thickness(150, 160 + 120 * 0, 0, 0), value));
                }
                int pos = value.IndexOf(' ');

                if (pos > 0)
                    splitIndex.Add(pos);

                while (pos >= 0)
                {
                    pos = value.IndexOf(' ', pos + 1);

                    if (pos > 0)
                        splitIndex.Add(pos);
                }

                List<int> newSplitIndex = new List<int>();

                if (splitIndex.Count() >= 3)
                {
                    int index_1 = getClosestIndex(value.Count() / 3, splitIndex.ToArray());
                    int index_2 = getClosestIndex(value.Count() / 3 * 2, splitIndex.ToArray());

                    if (index_1 == index_2)
                    {
                        newSplitIndex.Add(index_1);
                    }
                    else
                    {
                        newSplitIndex.Add(index_1);
                        newSplitIndex.Add(index_2);
                    }
                }
                else
                {
                    newSplitIndex = splitIndex;
                }

                List<string> output = new List<string>();

                if (splitIndex.Count() == 0)
                {
                    output.Add(value);
                }
                else
                {
                    newSplitIndex.Insert(0, -1);
                    newSplitIndex.Add(value.ToList().Count());

                    for (int i = 0; i < newSplitIndex.Count() - 1; i++)
                        output.Add(new string(value.ToList().GetRange(newSplitIndex[i] + 1, newSplitIndex[i + 1] - newSplitIndex[i] - 1).ToArray()));
                }

                int titleCount = 0;

                foreach (string i in output)
                {
                    MainWidnowGrid.Children.Add(newTitleTextBlock(new Thickness(150, 160 + 120 * titleCount, 0, 0), i));
                    titleCount += 1;
                }

                MainWidnowGrid.Children.Add(newTitleTextBlock(new Thickness(150, 160 + 120 * titleCount, 0, 0), "자동생성"));
                titleCount += 1;

                MainWidnowGrid.Children.Add(newTitleTextBlock(new Thickness(150, 160 + 120 * titleCount, 0, 0), "건축개요서"));
                titleCount += 1;
            }
        }

        private TextBlock newTitleTextBlock(Thickness margin, string text)
        {
            TextBlock tempTextblock = new TextBlock();
            tempTextblock.FontSize = 70;
            tempTextblock.Margin = margin;
            tempTextblock.Height = 100;
            tempTextblock.HorizontalAlignment = HorizontalAlignment.Stretch;
            tempTextblock.VerticalAlignment = VerticalAlignment.Top;

            tempTextblock.Text = text;

            return tempTextblock;
        }

        private int getClosestIndex(double target, int[] indexSet)
        {
            double tempDistance = double.MaxValue;
            int tempIndex = -1;

            foreach (int i in indexSet)
            {
                if (Math.Abs(target - i) < tempDistance)
                {
                    tempDistance = Math.Abs(target - i);
                    tempIndex = i;
                }
            }

            return tempIndex;
        }
    }
}
