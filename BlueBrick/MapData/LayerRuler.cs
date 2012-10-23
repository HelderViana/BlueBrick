﻿// BlueBrick, a LEGO(c) layout editor.
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
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Imaging;

namespace BlueBrick.MapData
{
	[Serializable]
	partial class LayerRuler : Layer
	{
		// all the rulers in the layer
		private List<RulerItem> mRulers = new List<RulerItem>();

		// the image attribute to draw the text including the layer transparency
		private ImageAttributes mImageAttribute = new ImageAttributes();

		// variable used during the edition
		private Ruler mCurrentlyEditedRuler = null;
		private bool mIsEditingOffsetOfRuler = false;

		#region set/get
		public override int Transparency
		{
			set
			{
				mTransparency = value;
				ColorMatrix colorMatrix = new ColorMatrix();
				colorMatrix.Matrix33 = (float)value / 100.0f;
				mImageAttribute.SetColorMatrix(colorMatrix);
				// TODO: refactor or enable the selection
//				mSelectionBrush = new SolidBrush(Color.FromArgb((BASE_SELECTION_TRANSPARENCY * value) / 100, 255, 255, 255));
			}
		}
		#endregion

		#region constructor
		public LayerRuler()
		{
		}

		public override int getNbItems()
		{
			return mRulers.Count;
		}
		#endregion

		#region action on the layer
		/// <summary>
		///	Add the specified ruler at the specified position.
		///	If the position is negative, add the item at the end
		/// </summary>
		public void addRulerItem(RulerItem rulerToAdd, int index)
		{
			if (index < 0)
				mRulers.Add(rulerToAdd);
			else
				mRulers.Insert(index, rulerToAdd);
		}

		/// <summary>
		/// Remove the specified ruler item
		/// </summary>
		/// <param name="rulerToRemove">the ruler item to remove from the layer</param>
		/// <returns>the previous index of the ruler item deleted</returns>
		public int removeRulerItem(RulerItem rulerToRemove)
		{
			int index = mRulers.IndexOf(rulerToRemove);
			if (index >= 0)
			{
				mRulers.Remove(rulerToRemove);
				// remove also the item from the selection list if in it
				if (mSelectedObjects.Contains(rulerToRemove))
					removeObjectFromSelection(rulerToRemove);
			}
			else
				index = 0;
			return index;
		}
		#endregion

		#region util functions
		/// <summary>
		/// compute the distance in stud between the given point and the currently edited ruler.
		/// </summary>
		/// <param name="pointInStud">the point in stud coord for which you want to know the distance</param>
		/// <returns>the distance in stud</returns>
		private float computePointDistanceFromCurrentRuler(PointF pointInStud)
		{
			float distance = 0.0f;
			if (mCurrentlyEditedRuler != null)
			{
				// get the two vector to make a vectorial product
				PointF unitVector = mCurrentlyEditedRuler.UnitVector;
				PointF point1ToMouse = new PointF(pointInStud.X - mCurrentlyEditedRuler.Point1.X, pointInStud.Y - mCurrentlyEditedRuler.Point1.Y);
				// compute the vectorial product (x and y are null cause z is null):
				distance = (point1ToMouse.X * unitVector.Y) - (point1ToMouse.Y * unitVector.X);
			}
			return distance;
		}
		#endregion

		#region draw/mouse event
		/// <summary>
		/// get the total area in stud covered by all the ruler items in this layer
		/// </summary>
		/// <returns></returns>
		public override RectangleF getTotalAreaInStud()
		{
			return getTotalAreaInStud(mRulers);
		}

		/// <summary>
		/// Draw the layer.
		/// </summary>
		/// <param name="g">the graphic context in which draw the layer</param>
		public override void draw(Graphics g, RectangleF areaInStud, double scalePixelPerStud)
		{
			if (!Visible)
				return;

			// draw all the rulers of the layer
			foreach (Ruler ruler in mRulers)
				ruler.draw(g, areaInStud, scalePixelPerStud, mTransparency, mImageAttribute);

			// draw the ruler we are currently creating if any
			if (mCurrentlyEditedRuler != null)
				mCurrentlyEditedRuler.draw(g, areaInStud, scalePixelPerStud, mTransparency, mImageAttribute);

			// call the base class to draw the surrounding selection rectangle
			base.draw(g, areaInStud, scalePixelPerStud);
		}

		/// <summary>
		/// Return the cursor that should be display when the mouse is above the map without mouse click
		/// </summary>
		/// <param name="mouseCoordInStud"></param>
		public override Cursor getDefaultCursorWithoutMouseClick(PointF mouseCoordInStud)
		{
			return MainForm.Instance.RulerAddPoint1Cursor;
		}

		/// <summary>
		/// This function is called to know if this layer is interested by the specified mouse click
		/// </summary>
		/// <param name="e">the mouse event arg that describe the mouse click</param>
		/// <returns>true if this layer wants to handle it</returns>
		public override bool handleMouseDown(MouseEventArgs e, PointF mouseCoordInStud, ref Cursor preferedCursor)
		{
			if (!mIsEditingOffsetOfRuler)
				preferedCursor = MainForm.Instance.RulerAddPoint2Cursor;
			return true;
		}

		/// <summary>
		/// This function is called to know if this layer is interested by the specified mouse click
		/// </summary>
		/// <param name="e">the mouse event arg that describe the mouse click</param>
		/// <returns>true if this layer wants to handle it</returns>
		public override bool handleMouseMoveWithoutClick(MouseEventArgs e, PointF mouseCoordInStud, ref Cursor preferedCursor)
		{
			if (mIsEditingOffsetOfRuler)
			{
				float orientation = mCurrentlyEditedRuler.Orientation;
				if (orientation > 157.5f)
					preferedCursor = MainForm.Instance.RulerOffsetHorizontalCursor;
				else if (orientation > 112.5f)
					preferedCursor = MainForm.Instance.RulerOffsetDiagonalDownCursor;
				else if (orientation > 67.5f)
					preferedCursor = MainForm.Instance.RulerOffsetVerticalCursor;
				else if (orientation > 22.5f)
					preferedCursor = MainForm.Instance.RulerOffsetDiagonalUpCursor;
				else if (orientation > -22.5f)
					preferedCursor = MainForm.Instance.RulerOffsetHorizontalCursor;
				else if (orientation > -67.5f)
					preferedCursor = MainForm.Instance.RulerOffsetDiagonalDownCursor;
				else if (orientation > -112.5f)
					preferedCursor = MainForm.Instance.RulerOffsetVerticalCursor;
				else if (orientation > -157.5f)
					preferedCursor = MainForm.Instance.RulerOffsetDiagonalUpCursor;
				else
					preferedCursor = MainForm.Instance.RulerOffsetHorizontalCursor;
			}
			return mIsEditingOffsetOfRuler;
		}

		/// <summary>
		/// This method is called if the map decided that this layer should handle
		/// this mouse click
		/// </summary>
		/// <param name="e">the mouse event arg that describe the click</param>
		/// <returns>true if the view should be refreshed</returns>
		public override bool mouseDown(MouseEventArgs e, PointF mouseCoordInStud)
		{
			if (!mIsEditingOffsetOfRuler)
				mCurrentlyEditedRuler = new Ruler(mouseCoordInStud, mouseCoordInStud);
			return true;
		}

		/// <summary>
		/// This method is called when the mouse move.
		/// </summary>
		/// <param name="e">the mouse event arg that describe the mouse move</param>
		/// <returns>true if the view should be refreshed</returns>
		public override bool mouseMove(MouseEventArgs e, PointF mouseCoordInStud)
		{
			if (mCurrentlyEditedRuler != null)
			{
				if (mIsEditingOffsetOfRuler)
				{
					// adjust the offset
					mCurrentlyEditedRuler.OffsetDistance = computePointDistanceFromCurrentRuler(mouseCoordInStud);
				}
				else
				{
					// adjust the second point
					mCurrentlyEditedRuler.Point2 = mouseCoordInStud;
				}
			}
			return true;
		}

		/// <summary>
		/// This method is called when the mouse button is released.
		/// </summary>
		/// <param name="e">the mouse event arg that describe the click</param>
		/// <returns>true if the view should be refreshed</returns>
		public override bool mouseUp(MouseEventArgs e, PointF mouseCoordInStud)
		{
			if (mIsEditingOffsetOfRuler)
			{
				mCurrentlyEditedRuler.OffsetDistance = computePointDistanceFromCurrentRuler(mouseCoordInStud);
				Actions.ActionManager.Instance.doAction(new Actions.Rulers.AddRuler(this, mCurrentlyEditedRuler));
				mCurrentlyEditedRuler = null;
				mIsEditingOffsetOfRuler = false;
			}
			else
			{
				mCurrentlyEditedRuler.Point2 = mouseCoordInStud;
				mIsEditingOffsetOfRuler = true;
			}
			return false;
		}

		/// <summary>
		/// Select all the item inside the rectangle in the current selected layer
		/// </summary>
		/// <param name="selectionRectangeInStud">the rectangle in which select the items</param>
		public override void selectInRectangle(RectangleF selectionRectangeInStud)
		{
		}
		#endregion

	}
}