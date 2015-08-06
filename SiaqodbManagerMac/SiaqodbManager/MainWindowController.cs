﻿
using System;
using System.Collections.Generic;
using System.Linq;
using MonoMac.Foundation;
using MonoMac.AppKit;
using System.ComponentModel;
using SiaqodbManager.ViewModel;
using SiaqodbManager.DataSourcesAdapters;
using MonoMac.ObjCRuntime;
using SiaqodbManager.Util;
using SiaqodbManager.Entities;
using SiaqodbManager.Controls;
using SiaqodbManager.CustomWindow;
using SiaqodbManager.Model;
using System.IO;
using SiaqodbManager.CostumWindow;
using MonoMac.WebKit;
using System.Diagnostics;

namespace SiaqodbManager
{
	public partial class MainWindowController : MonoMac.AppKit.NSWindowController
	{
		#region Constructors

		// Called when created from unmanaged code
		public MainWindowController (IntPtr handle) : base (handle)
		{
			Initialize ();
		}
		
		// Called when created directly from a XIB file
		[Export ("initWithCoder:")]
		public MainWindowController (NSCoder coder) : base (coder)
		{
			Initialize ();
		}
		
		// Call to load from the XIB/NIB file
		public MainWindowController () : base ("MainWindow")
		{
			Initialize ();
		}

		public MainWindowController (MainViewModelAdapter mainViewModel): base ("MainWindow")
		{
			this.mainViewModel = mainViewModel;
			Initialize ();
		}

		
		// Shared initialization code
		void Initialize ()
		{
			Instance = this;
			TablesDictionry = new Dictionary<string, NSTableView> ();
			LinqTabDictionary = new Dictionary<NSTabViewItem, QueryViewModelAdapter> ();
			LinqRelatedActions = new List<NSControl> ();
			//add the buttons that should be enabled or disabled acording with the current document type
			LinqRelatedMenuItem = new List<NSMenuItem> ();
		}

		#endregion
		private MainViewModelAdapter mainViewModel;

		public static MainWindowController Instance{ get; set;}

		public static PropertyChangedEventHandler BindHandler{ get { return Instance.PropretyChangeHandler;} }

		private List<NSControl> LinqRelatedActions;

		private List<NSMenuItem>  LinqRelatedMenuItem;

		private Dictionary<string,NSTableView> TablesDictionry; 
		private Dictionary<NSTabViewItem,QueryViewModelAdapter> LinqTabDictionary; 


		public void PropretyChangeHandler (object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			Console.WriteLine (e.PropertyName);

			switch (e.PropertyName) {
				case "TypesList":
					{
						if(mainViewModel != null)
						{
							TypesView.DataSource = new TypesDataSource (mainViewModel.TypesList);
						}
						return;
					}
			}

			NSObject.FromObject (sender).WillChangeValue (e.PropertyName);
			NSObject.FromObject (sender).DidChangeValue (e.PropertyName);
		}

		public void OnEncryption (object sender, EventArgs e)
		{
			OnEncryption (NSObject.FromObject(sender));
		}
	

		partial void OnEncryption (NSObject sender)
		{
			var controller = new EncryptionWindowController ();
			var number = NSApplication.SharedApplication.RunModalForWindow(controller.Window);

			foreach(var label in TablesDictionry.Keys){
				var tabItem = TabView.Items.FirstOrDefault(t => t.Label == label);
				if(tabItem != null){
					TabView.Remove(tabItem);
				}
			}
			TablesDictionry.Clear();
		}

		public void OnExecuteLinq (object sender, EventArgs e)
		{
			ExecuteButton.PerformClick (NSObject.FromObject(sender));
		}

		public void OnOpenLinq (object sender, EventArgs e)
		{
			OnLinqOpen (NSObject.FromObject(sender));
		}

		partial void OnLinqOpen (NSObject sender)
		{
			var fileDialog = new OpenFileService("","Select file");

			var file = fileDialog.OpenDialog();
			if(!string.IsNullOrEmpty(file)){
				using(var sr = new StreamReader(file)){
					string  s = sr.ReadToEnd();
					OnLinqTab(sender, new EventArgs());
					var queryView = LinqTabDictionary[TabView.Selected];
					TabView.Selected.Label = file;
					queryView.Linq = new NSAttributedString(s);
				}
			}
		}

		public void OnSaveLinq (object sender, EventArgs e)
		{
			SaveLinqFile.PerformClick (NSObject.FromObject(sender));
		}

		public void OnReference (object sender, EventArgs e)
		{
			OnReferences (NSObject.FromObject(sender));
		}

		partial void OnReferences (NSObject sender)
		{
			var controller = new ReferenceWindowController ();
			NSApplication.SharedApplication.RunModalForWindow(controller.Window);
		}
			

		//BIND THE LOGIN PANEL
		public  override void AwakeFromNib ()
		{

			base.AwakeFromNib ();
			EncryptionViewModel.Instance.SetEncryptionSettings ();

			BindButton (mainViewModel,"ConnectCommand",ConnectButton);
			PathInput.Bind ("value",mainViewModel,"SelectedPath",BindingUtil.ContinuouslyUpdatesValue);

			CreateLink (ForumLink,"Visit forum","http://forum.siaqodb.com/");
			CreateLink (BlogLink,"Visit blog","http://siaqodb.com/?page_id=13");
			CreateLink (EmailLink,"Send us an email","mailto:support@siaqodb.com");
			//EmailLink.CellClicked += EmailClicked;
			AddDemoVideo ();

			TabView.DidSelect += OnTabSelectionChanged;
			AddButton.Activated += OnAddRow;
			RemoveButton.Activated += OnRemoveRow;
			CloseTabButton.Activated += OnCloseTab;
			LinqButton.Activated += OnLinqTab;


			//types tree view
			TypesView.Delegate = new TypesDelegate (this);

			//add button to be enabled or disabled
			LinqRelatedActions.Add (SaveLinqFile);
			LinqRelatedActions.Add (ExecuteButton);
			SetLinqActionEnabled (false);

		} 

		void AddDemoVideo ()
		{
			var document = StartView.MainFrame.DomDocument;
		
			//<iframe width="420" height="315" src="https://www.youtube.com/embed/eUvTmL4Kxmc" frameborder="0" allowfullscreen></iframe>
		
			var frame = document.CreateElement ("iframe");

			var srcAttr = document.CreateAttribute ("src");
			srcAttr.Value = "https://www.youtube.com/embed/eUvTmL4Kxmc";
			frame.SetAttributeNode (srcAttr);

			var allowFullScreen = document.CreateAttribute ("allowfullscreen");;
			frame.SetAttributeNode (allowFullScreen);

			var height = document.CreateAttribute ("height");
			height.Value = "100%";
			frame.SetAttributeNode (height);

			var width = document.CreateAttribute ("width");
			width.Value = "100%";
			frame.SetAttributeNode (width);

			StartView.MainFrame.DomDocument.body.AppendChild (frame);
		}

		void CreateLink (NSTextView textView,string text,string link)
		{
			var str = new NSMutableAttributedString (new NSString (text));
			str.AddAttribute (NSAttributedString.LinkAttributeName, new NSString (link), new NSRange (0, text.Length));
			textView.InsertText (str);
			textView.Editable = false;
		}

		void CreateLink(NSTextView textView,string text){
			var str = new NSMutableAttributedString (new NSString (text));

			str.AddAttribute (NSAttributedString.ForegroundColorAttributeName, NSColor.Blue, new NSRange (0, text.Length));
			str.AddAttribute (NSAttributedString.UnderlineColorAttributeName, NSColor.Blue, new NSRange (0, text.Length));
			str.AddAttribute (NSAttributedString.CursorAttributeName, NSCursor.PointingHandCursor, new NSRange (0,text.Length));				

			str.AddAttribute (NSAttributedString.UnderlineStyleAttributeName, NSObject.FromObject(NSUnderlineStyle.Single), new NSRange (0, text.Length));
			textView.InsertText (str);
			textView.Editable = false;
		}


		public void CreateObjectsTable (MetaTypeViewModelAdapter metaType)
		{
			CreateObjectsTable (metaType,null);
		}

		void CreateObjectsTable (MetaTypeViewModelAdapter metaType,List<int> oids)
		{
			var tabViewItem = new NSTabViewItem ();
			var tableView = new CustomTable ();
			//table managing section
			var objectAdapter = mainViewModel.CreateObjectsView (metaType, oids);
			var objDelegate = new ObjectsDelegate (objectAdapter);
			objDelegate.ArrayClicked += OnArrayClicked;
			tableView.Delegate = objDelegate;
			objectAdapter.OpenObjects += OpenObjects;
			ObjectsTabBuilder.AddColumnsAndData (tableView, objectAdapter);
			var tableContainer = ObjectsTabBuilder.TableActionsLayout (tableView);
			ObjectsTabBuilder.CostumizeTable (tableView);
			tabViewItem.Label = metaType.Name;
			tabViewItem.View.AddSubview (tableContainer);
			TablesDictionry [metaType.Name] = tableView;
			TabView.Add (tabViewItem);
			TabView.Select (tabViewItem);
		}

		public void OpenObjects (object sender,MetaEventArgs e)
		{
			CreateObjectsTable (new MetaTypeViewModelAdapter(new MetaTypeViewModel(e.mType)),e.oids);
		}

		//strongly typed window accessor
		public new MainWindow Window {
			get {
				return (MainWindow)base.Window;
			}
		}

		void OnAddRow (object sender, EventArgs e)
		{
			var tableView = TablesDictionry[TabView.Selected.Label]; 
			var dataSource = tableView.DataSource as ObjectsDataSource;

			dataSource.AddNewRow ();
			tableView.ReloadData ();
		}

		void OnCloseTab (object sender, EventArgs e)
		{
			var tab = TabView.Selected;
			if(tab == null){
				return;
			}
			TabView.Remove (tab);

			if(TablesDictionry.ContainsKey(tab.Label)){
				TablesDictionry.Remove (tab.Label);
			}else if(LinqTabDictionary.ContainsKey(tab)){
				LinqTabDictionary.Remove(tab);
			}
		}

		public void OnLinqTab (object sender, EventArgs e)
		{
			var queryViewModel = mainViewModel.CreateQueryView (new SaveFileService ());
			var tabViewItem = LinqTabBuilder.BuildTab (queryViewModel);
			BindSelectedLinq (queryViewModel);
			LinqTabDictionary [tabViewItem] = queryViewModel;
			TabView.Add (tabViewItem);
			TabView.Select (tabViewItem);
			SetLinqActionEnabled (true);
		}

		public void RegisterLinqAction (NSMenuItem item)
		{
			LinqRelatedMenuItem.Add (item);
		}

		void SetLinqActionEnabled (bool isEnabled)
		{
			foreach(var view in LinqRelatedActions){
				view.Enabled = isEnabled;
			}
			foreach(var view in LinqRelatedMenuItem){
				view.Enabled = isEnabled;
			}
		}

		void OnTabSelectionChanged (object sender, NSTabViewItemEventArgs e)
		{
			var label = TabView.Selected.Label;
			if(TablesDictionry.ContainsKey(label)){
				TableActionButtons.Hidden = false;
				SetLinqActionEnabled (false);
			}else if(LinqTabDictionary.ContainsKey(TabView.Selected)){
				var queryViewModel = LinqTabDictionary[TabView.Selected];
				BindSelectedLinq (queryViewModel);
				SetLinqActionEnabled (true);
				TableActionButtons.Hidden = true;
			}else{
				TableActionButtons.Hidden = true;
				SetLinqActionEnabled (false);
			}
		}


		void OnRemoveRow (object sender, EventArgs e)
		{
			var tableView = TablesDictionry[TabView.Selected.Label]; 
			var dataSource = tableView.DataSource as ObjectsDataSource;

			int rowIndex = tableView.SelectedRow;

			if(rowIndex > -1){
				dataSource.RemoveRow (rowIndex);
				tableView.ReloadData ();
			}
		}

		void OnArrayClicked (object sender, ArrayEditArgs e)
		{
			var value = e.ViewModel.GetValue (e.ColumnName,e.RowIndex);
			if(value is Array){
				var controller = new ArrayWindowController (value as Array);
				NSApplication.SharedApplication.RunModalForWindow(controller.Window);
				var messageBox = new MessageBox();
				if(controller.HasValue){
					try{
						e.ViewModel.EditArray (controller.Values,e.RowIndex,e.ColumnIndex,e.ColumnName);
						messageBox.Show("The operation was successful");
					}catch(Exception ex){
						messageBox.Show("You entered the input on the wrong format");
					}
				}
			}
		}

		void EmailClicked (object sender, NSTextViewClickedEventArgs e)
		{
			//Process.Start ("mailt:support@siaqodb.com");
		}

		void BindSelectedLinq (QueryViewModelAdapter queryViewModel)
		{
			BindButton (queryViewModel, "ExecuteCommand", ExecuteButton);
			BindButton (queryViewModel, "SaveCommand", SaveLinqFile);
		}

		private void BindButton(NSObject target,string action,NSButton button){
			button.Target = target;
			button.Action = new Selector (action);
		}
	}
}
