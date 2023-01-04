using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace RunCommandDocker.CustomControls
{
    /// <summary>
    /// Interaction logic for IconButton.xaml
    /// </summary>
    public partial class IconButton : Button
    {
        public static readonly DependencyProperty IconProperty = DependencyProperty.Register("Icon", typeof(string), typeof(IconButton), null);
        [Browsable(true)]
        [Category("Common")]
        public string Icon
        {
            get
            {
                return (string)GetValue(IconProperty);
            }
            set
            {
                SetValue(IconProperty, value);
            }
        }


        public static readonly DependencyProperty CornerRadiusProperty = DependencyProperty.Register("CornerRadius", typeof(CornerRadius), typeof(IconButton), null);
        [Browsable(true)]
        [Category("Appearence")]
        public CornerRadius CornerRadius
        {
            get
            {
                return (CornerRadius)GetValue(CornerRadiusProperty);
            }
            set
            {
                SetValue(CornerRadiusProperty, value);
            }
        }
        public IconButton()
        {
            InitializeComponent();
        }
    }
}
