// BlueBrick, a LEGO(c) layout editor.
// Copyright (C) 2008 Alban NANTY
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, version 3 of the License.
// see http://www.fsf.org/licensing/licenses/gpl.html
// and http://www.gnu.org/licenses/
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.

using System;
using System.Collections.Generic;
using System.Text;
using BlueBrick.MapData;

namespace BlueBrick.Actions.Items
{
	class GroupItems : Action
	{
		List<Layer.LayerItem> mItemsToGroup = new List<Layer.LayerItem>();
		Layer.Group mGroup = new Layer.Group();

		public GroupItems(List<Layer.LayerItem> itemsToGroup)
		{
			// create a search list that we will expend and to keep the original selection intact
			List<Layer.LayerItem> searchList = new List<Layer.LayerItem>(itemsToGroup);

			// save the item list but don't add them in the group in the constructor.
			// we do that in the redo. And we only group the top items of the tree.
			// we cannot use a foreach keyword here because it through an exception when
			// the list is modified during the iteration, which is exactly what I want to do
			for (int i = 0; i < searchList.Count; ++i)
			{
				Layer.LayerItem item = searchList[i];
				if (!mItemsToGroup.Contains(item))
				{
					if (item.Group == null)
						mItemsToGroup.Add(item);
					else if (!searchList.Contains(item.Group))
						searchList.Add(item.Group);
				}
			}
		}

		public override string getName()
		{
			return BlueBrick.Properties.Resources.ActionGroupItems;
		}

		public override void redo()
		{
			// add all the items in the group
			foreach (Layer.LayerItem item in mItemsToGroup)
				mGroup.addItem(item);
		}

		public override void undo()
		{
			// remove all the items from the group
			foreach (Layer.LayerItem item in mItemsToGroup)
				mGroup.removeItem(item);
		}
	}
}
