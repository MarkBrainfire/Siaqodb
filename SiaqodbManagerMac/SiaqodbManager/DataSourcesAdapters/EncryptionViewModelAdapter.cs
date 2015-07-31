﻿using System;
using MonoMac.Foundation;
using SiaqodbManager.ViewModel;
using SiaqodbManager.CostumWindow;

namespace SiaqodbManager.DataSourcesAdapters
{
	public class EncryptionViewModelAdapter:AbstractViewModelAdapter
	{
		EncryptionViewModel viewModel;

		public EncryptionViewModelAdapter (EncryptionViewModel viewModel):base(viewModel)
		{
			this.viewModel = viewModel;
			viewModel.ConfirmationBox = new MessageBox (); 
		}

		[Export("Algorithm")]
		public string Algorithm{
			get{
				return viewModel.Algorithm;
			}
			set{
				viewModel.Algorithm = value;
			}
		}

		[Export("IsEncryptionChecked")]
		public bool IsEncryptionChecked{
			get{
				return viewModel.IsEncryptedChecked;
			}
			set{
				viewModel.IsEncryptedChecked = value;
			}
		}
			
		public void EncryptCommand(object obj){
			viewModel.EncryptCommand.Execute (obj);
		}
	}
}

