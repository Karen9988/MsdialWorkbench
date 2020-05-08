﻿using System;
using System.Windows;
using Rfx.Riken.OsakaUniv;

namespace MsdialDimsCoreUiTestApp
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            var vm = new MainWindowVM();
            this.ChartArea.Children.Add(new ChromatogramXicUI(vm.ChromatogramXicViewModel));
        }
    }
}
