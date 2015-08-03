﻿using System;
using MonoMac.AppKit;
using MonoMac.Foundation;
using System.Drawing;
using SiaqodbManager.Model;


namespace SiaqodbManager
{
	public class ObjectsDelegate:NSTableViewDelegate
	{
		private ObjectViewModelAdapter viewModel;

		public ObjectsDelegate (ObjectViewModelAdapter viewModel)
		{
			this.viewModel = viewModel;
		}

		public override void SelectionDidChange (NSNotification notification)
		{
			var table = notification.Object as CustomTable;
			if(table.CurrentCell != null && table.CurrentColumn != null){
				var column =  table.CurrentColumn;
				var columnIndex = GetColumnIndex (table,column);
				if(viewModel.Columns[column.HeaderCell.Identifier].Item2.FieldType == typeof(string)){
					viewModel.EditComplexObject (table.SelectedRow,columnIndex,column.HeaderCell.Identifier);
				}else{
					OnArrayClicked (column.HeaderCell.Identifier,table.SelectedRow,columnIndex);
					//viewModel.
				}
			}
		}

		public event EventHandler<ArrayEditArgs> ArrayClicked;

		public void OnArrayClicked(string column,int row,int columnIndex){
		
			if(ArrayClicked != null){
				ArrayClicked (this,new ArrayEditArgs{
					ColumnIndex = columnIndex,
					ColumnName = column,
					RowIndex = row,
					ViewModel = viewModel
				});
			}
		}

		public override void WillDisplayCell (NSTableView tableView, MonoMac.Foundation.NSObject cell, NSTableColumn tableColumn, int row)
		{
			var attributedCell = cell as NSCell;
			var objectSource = tableView.DataSource as ObjectsDataSource;
			var customTable = tableView as CustomTable;		

			if(attributedCell != null && objectSource != null){
				attributedCell.SetCellAttribute (NSCellAttribute.CellHighlighted,0);
				var type = objectSource.GetTypeOfColumn (tableColumn.HeaderCell.Identifier);
				if (type == null) {
					var value = attributedCell.Title;
					var attributedValue = new NSMutableAttributedString (value);
					attributedValue.AddAttribute (NSAttributedString.ForegroundColorAttributeName, NSColor.Blue, new NSRange (0, attributedValue.Length));
					attributedValue.AddAttribute (NSAttributedString.CursorAttributeName, NSCursor.PointingHandCursor, new NSRange (0,attributedValue.Length));				
					if (customTable.CurrentCell == attributedCell && row == customTable.SelectedRow) {
						attributedCell.SetCellAttribute (NSCellAttribute.CellHighlighted,1);
					}
					attributedCell.AttributedStringValue = attributedValue;
					attributedCell.Editable = false;
					attributedCell.Selectable = false;
				} else {
					var index = GetColumnIndex (tableView,tableColumn);
					if (index != 0) {
						tableColumn.Editable = true;
					} else {
						tableColumn.Editable = false;
					}
				}
			}
		}
			
		int GetColumnIndex (NSTableView tableView, NSTableColumn tableColumn)
		{
			var i = 0;
			foreach(var column in tableView.TableColumns()){
				if(column.Equals(tableColumn)){
					return i;
				}
				i++;
			}
			return -1;
		}
	}
}

