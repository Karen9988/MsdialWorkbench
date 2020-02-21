﻿using System;
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
using System.Windows.Shapes;

namespace Rfx.Riken.OsakaUniv {
    /// <summary>
    /// Interaction logic for ShortMessageWindow.xaml
    /// </summary>
    public partial class ShortMessageWindow : Window {
        public ShortMessageWindow() {
            InitializeComponent();
        }
        public ShortMessageWindow(string text) {
            InitializeComponent();
            Label_MessageTitle.Text = text;
        }
    }
}
