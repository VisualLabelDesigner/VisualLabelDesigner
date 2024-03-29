﻿// Copyright (c) 2014 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace ICSharpCode.Core.WinForms
{
	public static class ToolbarService
	{
		public static ToolStripItem[] CreateToolStripItems(string path, object owner, bool throwOnNotFound)
		{
			return CreateToolStripItems(owner, AddInTree.GetTreeNode(path, throwOnNotFound));
		}
		
		public static ToolStripItem[] CreateToolStripItems(object owner, AddInTreeNode treeNode)
		{
			if (treeNode == null)
				return new ToolStripItem[0];
			List<ToolStripItem> collection = new List<ToolStripItem>();
			foreach (ToolbarItemDescriptor descriptor in treeNode.BuildChildItems<ToolbarItemDescriptor>(owner)) {
				object item = CreateToolbarItemFromDescriptor(descriptor);
				if (item is ToolStripItem) {
					collection.Add((ToolStripItem)item);
				} else if (item is IMenuItemBuilder) {
					IMenuItemBuilder submenuBuilder = (IMenuItemBuilder)item;
					collection.AddRange(submenuBuilder.BuildItems(descriptor.Codon, owner).Cast<ToolStripItem>().ToArray());
				}
			}
			
			return collection.ToArray();
		}

		// TODO: 根据配置的type，创建toolbar
		static object CreateToolbarItemFromDescriptor(ToolbarItemDescriptor descriptor)
		{
			Codon codon = descriptor.Codon;
			object caller = descriptor.Parameter;
			string type = codon.Properties.Contains("type") ? codon.Properties["type"] : "Item";
			
			bool createCommand = codon.Properties["loadclasslazy"] == "false";
			
			switch (type) {
				case "Separator":
					return new ToolBarSeparator(codon, caller, descriptor.Conditions);
				case "CheckBox":
					return new ToolBarCheckBox(codon, caller, descriptor.Conditions);
				case "Item":
					return new ToolBarCommand(codon, caller, createCommand, descriptor.Conditions);
				case "Label":
					return new ToolBarLabel(codon, caller, descriptor.Conditions);
				case "DropDownButton":
					return new ToolBarDropDownButton(codon, caller, MenuService.ConvertSubItems(descriptor.SubItems), descriptor.Conditions);
				case "SplitButton":
					return new ToolBarSplitButton(codon, caller, MenuService.ConvertSubItems(descriptor.SubItems), descriptor.Conditions);
				case "Custom":
				case "Builder":
					return codon.AddIn.CreateObject(codon.Properties["class"]);
				default:
					throw new System.NotSupportedException("unsupported menu item type : " + type);
			}
		}
		
		public static ToolStrip CreateToolStrip(object owner, AddInTreeNode treeNode)
		{
			ToolStrip toolStrip = new ToolStrip();
			toolStrip.Items.AddRange(CreateToolStripItems(owner, treeNode));
			UpdateToolbar(toolStrip); // setting Visible is only possible after the items have been added
			new LanguageChangeWatcher(toolStrip);
			return toolStrip;
		}
		
		class LanguageChangeWatcher {
			readonly IResourceService resourceService = ServiceSingleton.GetRequiredService<IResourceService>();
			ToolStrip toolStrip;
			public LanguageChangeWatcher(ToolStrip toolStrip) {
				this.toolStrip = toolStrip;
				toolStrip.Disposed += Disposed;
				resourceService.LanguageChanged += OnLanguageChanged;
			}
			void OnLanguageChanged(object sender, EventArgs e) {
				ToolbarService.UpdateToolbarText(toolStrip);
			}
			void Disposed(object sender, EventArgs e) {
				resourceService.LanguageChanged -= OnLanguageChanged;
			}
		}
		
		public static ToolStrip CreateToolStrip(object owner, string addInTreePath)
		{
			return CreateToolStrip(owner, AddInTree.GetTreeNode(addInTreePath));
		}
		
		public static ToolStrip[] CreateToolbars(object owner, string addInTreePath)
		{
			AddInTreeNode treeNode;
			try {
				treeNode = AddInTree.GetTreeNode(addInTreePath);
			} catch (TreePathNotFoundException) {
				return null;
				
			}
			List<ToolStrip> toolBars = new List<ToolStrip>();
			foreach (AddInTreeNode childNode in treeNode.ChildNodes.Values) {
				toolBars.Add(CreateToolStrip(owner, childNode));
			}
			return toolBars.ToArray();
		}
		
		public static void UpdateToolbar(ToolStrip toolStrip)
		{
			toolStrip.SuspendLayout();
			foreach (ToolStripItem item in toolStrip.Items) {
				if (item is IStatusUpdate) {
					((IStatusUpdate)item).UpdateStatus();
				}
			}
			toolStrip.ResumeLayout();
			//toolStrip.Refresh();
		}
		
		public static void UpdateToolbarText(ToolStrip toolStrip)
		{
			toolStrip.SuspendLayout();
			foreach (ToolStripItem item in toolStrip.Items) {
				if (item is IStatusUpdate) {
					((IStatusUpdate)item).UpdateText();
				}
			}
			toolStrip.ResumeLayout();
		}
	}
}
