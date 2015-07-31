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
using System.IO;
using System.Windows.Forms;
using System.Reflection;
using SiaqodbManager.ViewModel;
using SiaqodbManager.DialogService;

namespace SiaqodbManager
{
    /// <summary>
    /// Interaction logic for AddReference.xaml
    /// </summary>
    public partial class AddReference : Window
    {
        public AddReference()
        {
            InitializeComponent();
            var viewModel = new ReferencesViewModel(new ReferenceFileService());
            DataContext = viewModel;
            viewModel.ClosingRequest += CloseWindow;
        }

        private void CloseWindow(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
