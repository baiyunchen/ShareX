﻿#region License Information (GPL v3)

/*
    ShareX - A program that allows you to take screenshots and share any file type
    Copyright (c) 2007-2016 ShareX Team

    This program is free software; you can redistribute it and/or
    modify it under the terms of the GNU General Public License
    as published by the Free Software Foundation; either version 2
    of the License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program; if not, write to the Free Software
    Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.

    Optionally you can also view the license at <http://www.gnu.org/licenses/>.
*/

#endregion License Information (GPL v3)

using ShareX.HelpersLib;
using ShareX.ScreenCaptureLib.Properties;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace ShareX.ScreenCaptureLib
{
    internal partial class ShapeManager
    {
        public bool IsMenuCollapsed { get; private set; }

        internal TextAnimation MenuTextAnimation = new TextAnimation(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(0.5));

        private Form menuForm;
        private ToolStripEx tsMain;
        private ToolStripButton tsbBorderColor, tsbFillColor, tsbHighlightColor, tsbUndoObject, tsbDeleteAll;
        private ToolStripDropDownButton tsddbShapeOptions;
        private ToolStripMenuItem tsmiQuickCrop, tsmiRegionCapture;
        private ToolStripLabeledNumericUpDown tslnudBorderSize, tslnudCornerRadius, tslnudBlurRadius, tslnudPixelateSize;
        private ToolStripLabel tslDragLeft;

        private void CreateToolbar()
        {
            menuForm = new Form()
            {
                AutoScaleDimensions = new SizeF(6F, 13F),
                AutoScaleMode = AutoScaleMode.Font,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ClientSize = new Size(759, 509),
                FormBorderStyle = FormBorderStyle.None,
                Location = new Point(200, 200),
                ShowInTaskbar = false,
                StartPosition = FormStartPosition.Manual,
                Text = "ShareX - Region capture menu",
                TopMost = true
            };

            menuForm.KeyUp += MenuForm_KeyUp;
            menuForm.LocationChanged += MenuForm_LocationChanged;

            menuForm.SuspendLayout();

            tsMain = new ToolStripEx()
            {
                AutoSize = true,
                CanOverflow = false,
                ClickThrough = true,
                Dock = DockStyle.None,
                GripStyle = ToolStripGripStyle.Hidden,
                Location = new Point(0, 0),
                MinimumSize = new Size(10, 30),
                Padding = new Padding(0, 0, 0, 0),
                Renderer = new CustomToolStripProfessionalRenderer(),
                TabIndex = 0,
                ShowItemToolTips = false
            };

            tsMain.MouseLeave += TsMain_MouseLeave;

            tsMain.SuspendLayout();

            // https://www.medo64.com/2014/01/scaling-toolstrip-with-dpi/
            using (Graphics g = menuForm.CreateGraphics())
            {
                double scale = Math.Max(g.DpiX, g.DpiY) / 96.0;
                double newScale = ((int)Math.Floor(scale * 100) / 25 * 25) / 100.0;
                if (newScale > 1)
                {
                    int newWidth = (int)(tsMain.ImageScalingSize.Width * newScale);
                    int newHeight = (int)(tsMain.ImageScalingSize.Height * newScale);
                    tsMain.ImageScalingSize = new Size(newWidth, newHeight);
                }
            }

            menuForm.Controls.Add(tsMain);

            tslDragLeft = new ToolStripLabel()
            {
                DisplayStyle = ToolStripItemDisplayStyle.Image,
                Image = Resources.ui_radio_button_uncheck,
                Margin = new Padding(2, 0, 2, 0),
                Padding = new Padding(2)
            };

            tsMain.Items.Add(tslDragLeft);

            #region Editor mode

            if (form.Mode == RegionCaptureMode.Editor)
            {
                ToolStripButton tsbCompleteEdit = new ToolStripButton("Run after capture tasks");
                tsbCompleteEdit.DisplayStyle = ToolStripItemDisplayStyle.Image;
                tsbCompleteEdit.Image = Resources.tick;
                tsbCompleteEdit.MouseDown += (sender, e) => form.Close(RegionResult.AnnotateRunAfterCaptureTasks);
                tsMain.Items.Add(tsbCompleteEdit);

                ToolStripButton tsbSaveImage = new ToolStripButton("Save image");
                tsbSaveImage.DisplayStyle = ToolStripItemDisplayStyle.Image;
                tsbSaveImage.Enabled = !string.IsNullOrEmpty(form.ImageFilePath);
                tsbSaveImage.Image = Resources.disk_black;
                tsbSaveImage.MouseDown += (sender, e) => form.Close(RegionResult.AnnotateSaveImage);
                tsMain.Items.Add(tsbSaveImage);

                ToolStripButton tsbSaveImageAs = new ToolStripButton("Save image as...");
                tsbSaveImageAs.DisplayStyle = ToolStripItemDisplayStyle.Image;
                tsbSaveImageAs.Image = Resources.disks_black;
                tsbSaveImageAs.MouseDown += (sender, e) => form.Close(RegionResult.AnnotateSaveImageAs);
                tsMain.Items.Add(tsbSaveImageAs);

                ToolStripButton tsbCopyImage = new ToolStripButton("Copy image to clipboard");
                tsbCopyImage.DisplayStyle = ToolStripItemDisplayStyle.Image;
                tsbCopyImage.Image = Resources.clipboard;
                tsbCopyImage.MouseDown += (sender, e) => form.Close(RegionResult.AnnotateCopyImage);
                tsMain.Items.Add(tsbCopyImage);

                ToolStripButton tsbUploadImage = new ToolStripButton("Upload image");
                tsbUploadImage.DisplayStyle = ToolStripItemDisplayStyle.Image;
                tsbUploadImage.Image = Resources.drive_globe;
                tsbUploadImage.MouseDown += (sender, e) => form.Close(RegionResult.AnnotateUploadImage);
                tsMain.Items.Add(tsbUploadImage);

                ToolStripButton tsbPrintImage = new ToolStripButton("Print image...");
                tsbPrintImage.DisplayStyle = ToolStripItemDisplayStyle.Image;
                tsbPrintImage.Image = Resources.printer;
                tsbPrintImage.MouseDown += (sender, e) => form.Close(RegionResult.AnnotatePrintImage);
                tsMain.Items.Add(tsbPrintImage);

                tsMain.Items.Add(new ToolStripSeparator());
            }

            #endregion Editor mode

            #region Tools

            foreach (ShapeType shapeType in Helpers.GetEnums<ShapeType>())
            {
                if (form.Mode == RegionCaptureMode.Editor)
                {
                    if (IsShapeTypeRegion(shapeType))
                    {
                        continue;
                    }
                }
                else if (shapeType == ShapeType.DrawingRectangle)
                {
                    tsMain.Items.Add(new ToolStripSeparator());
                }

                ToolStripButton tsbShapeType = new ToolStripButton(shapeType.GetLocalizedDescription());
                tsbShapeType.DisplayStyle = ToolStripItemDisplayStyle.Image;

                Image img = null;

                switch (shapeType)
                {
                    case ShapeType.RegionRectangle:
                        img = Resources.layer_shape_region;
                        break;
                    case ShapeType.RegionRoundedRectangle:
                        img = Resources.layer_shape_round_region;
                        break;
                    case ShapeType.RegionEllipse:
                        img = Resources.layer_shape_ellipse_region;
                        break;
                    case ShapeType.RegionFreehand:
                        img = Resources.layer_shape_polygon;
                        break;
                    case ShapeType.DrawingRectangle:
                        img = Resources.layer_shape;
                        break;
                    case ShapeType.DrawingRoundedRectangle:
                        img = Resources.layer_shape_round;
                        break;
                    case ShapeType.DrawingEllipse:
                        img = Resources.layer_shape_ellipse;
                        break;
                    case ShapeType.DrawingFreehand:
                        img = Resources.layer_shape_curve;
                        break;
                    case ShapeType.DrawingLine:
                        img = Resources.layer_shape_line;
                        break;
                    case ShapeType.DrawingArrow:
                        img = Resources.layer_shape_arrow;
                        break;
                    case ShapeType.DrawingText:
                        img = Resources.layer_shape_text;
                        break;
                    case ShapeType.DrawingSpeechBalloon:
                        img = Resources.balloon_box_left;
                        break;
                    case ShapeType.DrawingStep:
                        img = Resources.counter_reset;
                        break;
                    case ShapeType.DrawingImage:
                        img = Resources.image;
                        break;
                    case ShapeType.EffectBlur:
                        img = Resources.layer_shade;
                        break;
                    case ShapeType.EffectPixelate:
                        img = Resources.grid;
                        break;
                    case ShapeType.EffectHighlight:
                        img = Resources.highlighter_text;
                        break;
                }

                tsbShapeType.Image = img;
                tsbShapeType.Checked = shapeType == CurrentShapeType;
                tsbShapeType.Tag = shapeType;

                tsbShapeType.MouseDown += (sender, e) =>
                {
                    tsbShapeType.RadioCheck();
                    CurrentShapeType = shapeType;
                };

                tsMain.Items.Add(tsbShapeType);
            }

            #endregion Tools

            #region Selected object

            tsMain.Items.Add(new ToolStripSeparator());

            tsbBorderColor = new ToolStripButton(Resources.ShapeManager_CreateContextMenu_Border_color___);
            tsbBorderColor.DisplayStyle = ToolStripItemDisplayStyle.Image;
            tsbBorderColor.Click += (sender, e) =>
            {
                PauseForm();

                ShapeType shapeType = CurrentShapeType;

                Color borderColor;

                if (shapeType == ShapeType.DrawingText || shapeType == ShapeType.DrawingSpeechBalloon)
                {
                    borderColor = AnnotationOptions.TextBorderColor;
                }
                else if (shapeType == ShapeType.DrawingStep)
                {
                    borderColor = AnnotationOptions.StepBorderColor;
                }
                else
                {
                    borderColor = AnnotationOptions.BorderColor;
                }

                using (ColorPickerForm dialogColor = new ColorPickerForm(borderColor))
                {
                    if (dialogColor.ShowDialog() == DialogResult.OK)
                    {
                        if (shapeType == ShapeType.DrawingText || shapeType == ShapeType.DrawingSpeechBalloon)
                        {
                            AnnotationOptions.TextBorderColor = dialogColor.NewColor;
                        }
                        else if (shapeType == ShapeType.DrawingStep)
                        {
                            AnnotationOptions.StepBorderColor = dialogColor.NewColor;
                        }
                        else
                        {
                            AnnotationOptions.BorderColor = dialogColor.NewColor;
                        }

                        UpdateMenu();
                        UpdateCurrentShape();
                    }
                }

                ResumeForm();
            };
            tsMain.Items.Add(tsbBorderColor);

            tsbFillColor = new ToolStripButton(Resources.ShapeManager_CreateContextMenu_Fill_color___);
            tsbFillColor.DisplayStyle = ToolStripItemDisplayStyle.Image;
            tsbFillColor.Click += (sender, e) =>
            {
                PauseForm();

                ShapeType shapeType = CurrentShapeType;

                Color fillColor;

                if (shapeType == ShapeType.DrawingText || shapeType == ShapeType.DrawingSpeechBalloon)
                {
                    fillColor = AnnotationOptions.TextFillColor;
                }
                else if (shapeType == ShapeType.DrawingStep)
                {
                    fillColor = AnnotationOptions.StepFillColor;
                }
                else
                {
                    fillColor = AnnotationOptions.FillColor;
                }

                using (ColorPickerForm dialogColor = new ColorPickerForm(fillColor))
                {
                    if (dialogColor.ShowDialog() == DialogResult.OK)
                    {
                        if (shapeType == ShapeType.DrawingText || shapeType == ShapeType.DrawingSpeechBalloon)
                        {
                            AnnotationOptions.TextFillColor = dialogColor.NewColor;
                        }
                        else if (shapeType == ShapeType.DrawingStep)
                        {
                            AnnotationOptions.StepFillColor = dialogColor.NewColor;
                        }
                        else
                        {
                            AnnotationOptions.FillColor = dialogColor.NewColor;
                        }

                        UpdateMenu();
                        UpdateCurrentShape();
                    }
                }

                ResumeForm();
            };
            tsMain.Items.Add(tsbFillColor);

            tsbHighlightColor = new ToolStripButton(Resources.ShapeManager_CreateContextMenu_Highlight_color___);
            tsbHighlightColor.DisplayStyle = ToolStripItemDisplayStyle.Image;
            tsbHighlightColor.Click += (sender, e) =>
            {
                PauseForm();

                using (ColorPickerForm dialogColor = new ColorPickerForm(AnnotationOptions.HighlightColor))
                {
                    if (dialogColor.ShowDialog() == DialogResult.OK)
                    {
                        AnnotationOptions.HighlightColor = dialogColor.NewColor;
                        UpdateMenu();
                        UpdateCurrentShape();
                    }
                }

                ResumeForm();
            };
            tsMain.Items.Add(tsbHighlightColor);

            tsddbShapeOptions = new ToolStripDropDownButton("Shape options");
            tsddbShapeOptions.DisplayStyle = ToolStripItemDisplayStyle.Image;
            tsddbShapeOptions.HideImageMargin();
            tsddbShapeOptions.Image = Resources.layer__pencil;
            tsMain.Items.Add(tsddbShapeOptions);

            tslnudBorderSize = new ToolStripLabeledNumericUpDown(Resources.ShapeManager_CreateContextMenu_Border_size_);
            tslnudBorderSize.Content.Minimum = 0;
            tslnudBorderSize.Content.Maximum = 20;
            tslnudBorderSize.Content.ValueChanged = (sender, e) =>
            {
                ShapeType shapeType = CurrentShapeType;

                int borderSize = (int)tslnudBorderSize.Content.Value;

                if (shapeType == ShapeType.DrawingText || shapeType == ShapeType.DrawingSpeechBalloon)
                {
                    AnnotationOptions.TextBorderSize = borderSize;
                }
                else if (shapeType == ShapeType.DrawingStep)
                {
                    AnnotationOptions.StepBorderSize = borderSize;
                }
                else
                {
                    AnnotationOptions.BorderSize = borderSize;
                }

                UpdateCurrentShape();
            };
            tsddbShapeOptions.DropDownItems.Add(tslnudBorderSize);

            tslnudCornerRadius = new ToolStripLabeledNumericUpDown(Resources.ShapeManager_CreateContextMenu_Corner_radius_);
            tslnudCornerRadius.Content.Minimum = 0;
            tslnudCornerRadius.Content.Maximum = 150;
            tslnudCornerRadius.Content.Increment = 3;
            tslnudCornerRadius.Content.ValueChanged = (sender, e) =>
            {
                ShapeType shapeType = CurrentShapeType;

                if (shapeType == ShapeType.RegionRoundedRectangle || shapeType == ShapeType.DrawingRoundedRectangle)
                {
                    AnnotationOptions.RoundedRectangleRadius = (int)tslnudCornerRadius.Content.Value;
                }
                else if (shapeType == ShapeType.DrawingText)
                {
                    AnnotationOptions.TextCornerRadius = (int)tslnudCornerRadius.Content.Value;
                }

                UpdateCurrentShape();
            };
            tsddbShapeOptions.DropDownItems.Add(tslnudCornerRadius);

            tslnudBlurRadius = new ToolStripLabeledNumericUpDown(Resources.ShapeManager_CreateContextMenu_Blur_radius_);
            tslnudBlurRadius.Content.Minimum = 3;
            tslnudBlurRadius.Content.Maximum = 199;
            tslnudBlurRadius.Content.Increment = 2;
            tslnudBlurRadius.Content.ValueChanged = (sender, e) =>
            {
                AnnotationOptions.BlurRadius = (int)tslnudBlurRadius.Content.Value;
                UpdateCurrentShape();
            };
            tsddbShapeOptions.DropDownItems.Add(tslnudBlurRadius);

            tslnudPixelateSize = new ToolStripLabeledNumericUpDown(Resources.ShapeManager_CreateContextMenu_Pixel_size_);
            tslnudPixelateSize.Content.Minimum = 2;
            tslnudPixelateSize.Content.Maximum = 100;
            tslnudPixelateSize.Content.ValueChanged = (sender, e) =>
            {
                AnnotationOptions.PixelateSize = (int)tslnudPixelateSize.Content.Value;
                UpdateCurrentShape();
            };
            tsddbShapeOptions.DropDownItems.Add(tslnudPixelateSize);

            // In dropdown menu if only last item is visible then menu opens at 0, 0 position on first open, so need to add dummy item to solve this weird bug...
            tsddbShapeOptions.DropDownItems.Add(new ToolStripSeparator() { Visible = false });

            tsbUndoObject = new ToolStripButton("Undo object");
            tsbUndoObject.DisplayStyle = ToolStripItemDisplayStyle.Image;
            tsbUndoObject.Image = Resources.arrow_circle_225_left;
            tsbUndoObject.MouseDown += (sender, e) => UndoShape();
            tsMain.Items.Add(tsbUndoObject);

            tsbDeleteAll = new ToolStripButton(Resources.ShapeManager_CreateContextMenu_Delete_all_objects);
            tsbDeleteAll.DisplayStyle = ToolStripItemDisplayStyle.Image;
            tsbDeleteAll.Image = Resources.eraser;
            tsbDeleteAll.MouseDown += (sender, e) => DeleteAllShapes();
            tsMain.Items.Add(tsbDeleteAll);

            #endregion Selected object

            #region Capture

            if (form.Mode != RegionCaptureMode.Editor)
            {
                tsMain.Items.Add(new ToolStripSeparator());

                ToolStripDropDownButton tsddbCapture = new ToolStripDropDownButton("Capture");
                tsddbCapture.DisplayStyle = ToolStripItemDisplayStyle.Image;
                tsddbCapture.Image = Resources.camera;
                tsMain.Items.Add(tsddbCapture);

                tsmiRegionCapture = new ToolStripMenuItem("Capture regions");
                tsmiRegionCapture.Image = Resources.layer;
                tsmiRegionCapture.MouseDown += (sender, e) =>
                {
                    form.UpdateRegionPath();
                    form.Close(RegionResult.Region);
                };
                tsddbCapture.DropDownItems.Add(tsmiRegionCapture);

                if (RegionCaptureForm.LastRegionFillPath != null)
                {
                    ToolStripMenuItem tsmiLastRegionCapture = new ToolStripMenuItem("Capture last region");
                    tsmiLastRegionCapture.Image = Resources.layers;
                    tsmiLastRegionCapture.MouseDown += (sender, e) => form.Close(RegionResult.LastRegion);
                    tsddbCapture.DropDownItems.Add(tsmiLastRegionCapture);
                }

                ToolStripMenuItem tsmiFullscreenCapture = new ToolStripMenuItem(Resources.ShapeManager_CreateContextMenu_Capture_fullscreen);
                tsmiFullscreenCapture.Image = Resources.layer_fullscreen;
                tsmiFullscreenCapture.MouseDown += (sender, e) => form.Close(RegionResult.Fullscreen);
                tsddbCapture.DropDownItems.Add(tsmiFullscreenCapture);

                ToolStripMenuItem tsmiActiveMonitorCapture = new ToolStripMenuItem(Resources.ShapeManager_CreateContextMenu_Capture_active_monitor);
                tsmiActiveMonitorCapture.Image = Resources.monitor;
                tsmiActiveMonitorCapture.MouseDown += (sender, e) => form.Close(RegionResult.ActiveMonitor);
                tsddbCapture.DropDownItems.Add(tsmiActiveMonitorCapture);

                ToolStripMenuItem tsmiMonitorCapture = new ToolStripMenuItem(Resources.ShapeManager_CreateContextMenu_Capture_monitor);
                tsmiMonitorCapture.HideImageMargin();
                tsmiMonitorCapture.Image = Resources.monitor_window;
                tsddbCapture.DropDownItems.Add(tsmiMonitorCapture);

                Screen[] screens = Screen.AllScreens;

                for (int i = 0; i < screens.Length; i++)
                {
                    Screen screen = screens[i];
                    ToolStripMenuItem tsmi = new ToolStripMenuItem(string.Format("{0}. {1}x{2}", i + 1, screen.Bounds.Width, screen.Bounds.Height));
                    int index = i;
                    tsmi.MouseDown += (sender, e) =>
                    {
                        form.MonitorIndex = index;
                        form.Close(RegionResult.Monitor);
                    };
                    tsmiMonitorCapture.DropDownItems.Add(tsmi);
                }
            }

            #endregion Capture

            #region Options

            if (form.Mode != RegionCaptureMode.Editor)
            {
                tsMain.Items.Add(new ToolStripSeparator());

                ToolStripDropDownButton tsddbOptions = new ToolStripDropDownButton(Resources.ShapeManager_CreateContextMenu_Options);
                tsddbOptions.DisplayStyle = ToolStripItemDisplayStyle.Image;
                tsddbOptions.Image = Resources.gear;
                tsMain.Items.Add(tsddbOptions);

                tsmiQuickCrop = new ToolStripMenuItem(Resources.ShapeManager_CreateContextMenu_Multi_region_mode);
                tsmiQuickCrop.Checked = !Config.QuickCrop;
                tsmiQuickCrop.CheckOnClick = true;
                tsmiQuickCrop.Click += (sender, e) => Config.QuickCrop = !tsmiQuickCrop.Checked;
                tsddbOptions.DropDownItems.Add(tsmiQuickCrop);

                ToolStripMenuItem tsmiTips = new ToolStripMenuItem(Resources.ShapeManager_CreateContextMenu_Show_tips);
                tsmiTips.Checked = Config.ShowHotkeys;
                tsmiTips.CheckOnClick = true;
                tsmiTips.Click += (sender, e) => Config.ShowHotkeys = tsmiTips.Checked;
                tsddbOptions.DropDownItems.Add(tsmiTips);

                ToolStripMenuItem tsmiShowInfo = new ToolStripMenuItem(Resources.ShapeManager_CreateContextMenu_Show_position_and_size_info);
                tsmiShowInfo.Checked = Config.ShowInfo;
                tsmiShowInfo.CheckOnClick = true;
                tsmiShowInfo.Click += (sender, e) => Config.ShowInfo = tsmiShowInfo.Checked;
                tsddbOptions.DropDownItems.Add(tsmiShowInfo);

                ToolStripMenuItem tsmiShowMagnifier = new ToolStripMenuItem(Resources.ShapeManager_CreateContextMenu_Show_magnifier);
                tsmiShowMagnifier.Checked = Config.ShowMagnifier;
                tsmiShowMagnifier.CheckOnClick = true;
                tsmiShowMagnifier.Click += (sender, e) => Config.ShowMagnifier = tsmiShowMagnifier.Checked;
                tsddbOptions.DropDownItems.Add(tsmiShowMagnifier);

                ToolStripMenuItem tsmiUseSquareMagnifier = new ToolStripMenuItem(Resources.ShapeManager_CreateContextMenu_Square_shape_magnifier);
                tsmiUseSquareMagnifier.Checked = Config.UseSquareMagnifier;
                tsmiUseSquareMagnifier.CheckOnClick = true;
                tsmiUseSquareMagnifier.Click += (sender, e) => Config.UseSquareMagnifier = tsmiUseSquareMagnifier.Checked;
                tsddbOptions.DropDownItems.Add(tsmiUseSquareMagnifier);

                ToolStripLabeledNumericUpDown tslnudMagnifierPixelCount = new ToolStripLabeledNumericUpDown(Resources.ShapeManager_CreateContextMenu_Magnifier_pixel_count_);
                tslnudMagnifierPixelCount.Content.Minimum = RegionCaptureOptions.MagnifierPixelCountMinimum;
                tslnudMagnifierPixelCount.Content.Maximum = RegionCaptureOptions.MagnifierPixelCountMaximum;
                tslnudMagnifierPixelCount.Content.Increment = 2;
                tslnudMagnifierPixelCount.Content.Value = Config.MagnifierPixelCount;
                tslnudMagnifierPixelCount.Content.ValueChanged = (sender, e) => Config.MagnifierPixelCount = (int)tslnudMagnifierPixelCount.Content.Value;
                tsddbOptions.DropDownItems.Add(tslnudMagnifierPixelCount);

                ToolStripLabeledNumericUpDown tslnudMagnifierPixelSize = new ToolStripLabeledNumericUpDown(Resources.ShapeManager_CreateContextMenu_Magnifier_pixel_size_);
                tslnudMagnifierPixelSize.Content.Minimum = RegionCaptureOptions.MagnifierPixelSizeMinimum;
                tslnudMagnifierPixelSize.Content.Maximum = RegionCaptureOptions.MagnifierPixelSizeMaximum;
                tslnudMagnifierPixelSize.Content.Value = Config.MagnifierPixelSize;
                tslnudMagnifierPixelSize.Content.ValueChanged = (sender, e) => Config.MagnifierPixelSize = (int)tslnudMagnifierPixelSize.Content.Value;
                tsddbOptions.DropDownItems.Add(tslnudMagnifierPixelSize);

                ToolStripMenuItem tsmiShowCrosshair = new ToolStripMenuItem(Resources.ShapeManager_CreateContextMenu_Show_screen_wide_crosshair);
                tsmiShowCrosshair.Checked = Config.ShowCrosshair;
                tsmiShowCrosshair.CheckOnClick = true;
                tsmiShowCrosshair.Click += (sender, e) => Config.ShowCrosshair = tsmiShowCrosshair.Checked;
                tsddbOptions.DropDownItems.Add(tsmiShowCrosshair);

                ToolStripMenuItem tsmiFixedSize = new ToolStripMenuItem(Resources.ShapeManager_CreateContextMenu_Fixed_size_region_mode);
                tsmiFixedSize.Checked = Config.IsFixedSize;
                tsmiFixedSize.CheckOnClick = true;
                tsmiFixedSize.Click += (sender, e) => Config.IsFixedSize = tsmiFixedSize.Checked;
                tsddbOptions.DropDownItems.Add(tsmiFixedSize);

                ToolStripDoubleLabeledNumericUpDown tslnudFixedSize = new ToolStripDoubleLabeledNumericUpDown(Resources.ShapeManager_CreateContextMenu_Width_,
                    Resources.ShapeManager_CreateContextMenu_Height_);
                tslnudFixedSize.Content.Minimum = 10;
                tslnudFixedSize.Content.Maximum = 10000;
                tslnudFixedSize.Content.Increment = 10;
                tslnudFixedSize.Content.Value = Config.FixedSize.Width;
                tslnudFixedSize.Content.Value2 = Config.FixedSize.Height;
                tslnudFixedSize.Content.ValueChanged = (sender, e) => Config.FixedSize = new Size((int)tslnudFixedSize.Content.Value, (int)tslnudFixedSize.Content.Value2);
                tsddbOptions.DropDownItems.Add(tslnudFixedSize);

                ToolStripMenuItem tsmiShowFPS = new ToolStripMenuItem(Resources.ShapeManager_CreateContextMenu_Show_FPS);
                tsmiShowFPS.Checked = Config.ShowFPS;
                tsmiShowFPS.CheckOnClick = true;
                tsmiShowFPS.Click += (sender, e) => Config.ShowFPS = tsmiShowFPS.Checked;
                tsddbOptions.DropDownItems.Add(tsmiShowFPS);

                ToolStripMenuItem tsmiRememberMenuState = new ToolStripMenuItem("Remember menu state");
                tsmiRememberMenuState.Checked = Config.RememberMenuState;
                tsmiRememberMenuState.CheckOnClick = true;
                tsmiRememberMenuState.Click += (sender, e) => Config.RememberMenuState = tsmiRememberMenuState.Checked;
                tsddbOptions.DropDownItems.Add(tsmiRememberMenuState);
            }

            #endregion Options

            ToolStripLabel tslDragRight = new ToolStripLabel()
            {
                Alignment = ToolStripItemAlignment.Right,
                DisplayStyle = ToolStripItemDisplayStyle.Image,
                Image = Resources.ui_radio_button_uncheck,
                Margin = new Padding(0, 0, 2, 0),
                Padding = new Padding(2)
            };

            tsMain.Items.Add(tslDragRight);

            tslDragLeft.MouseDown += TslDrag_MouseDown;
            tslDragRight.MouseDown += TslDrag_MouseDown;
            tslDragLeft.MouseEnter += TslDrag_MouseEnter;
            tslDragRight.MouseEnter += TslDrag_MouseEnter;
            tslDragLeft.MouseLeave += TslDrag_MouseLeave;
            tslDragRight.MouseLeave += TslDrag_MouseLeave;

            tsMain.ResumeLayout(false);
            tsMain.PerformLayout();
            menuForm.ResumeLayout(false);

            menuForm.Show(form);

            foreach (ToolStripItem tsi in tsMain.Items.OfType<ToolStripItem>())
            {
                if (!string.IsNullOrEmpty(tsi.Text))
                {
                    tsi.MouseEnter += (sender, e) =>
                    {
                        Point pos = CaptureHelpers.ScreenToClient(menuForm.PointToScreen(tsi.Bounds.Location));
                        pos.Y += tsi.Height + 8;
                        MenuTextAnimation.Position = pos;
                        MenuTextAnimation.Start(tsi.Text);
                    };

                    tsi.MouseLeave += TsMain_MouseLeave;
                }
            }

            UpdateMenu();

            CurrentShapeChanged += shape => UpdateMenu();
            CurrentShapeTypeChanged += shapeType => UpdateMenu();
            ShapeCreated += shape => UpdateMenu();

            ConfigureMenuState();

            form.Activate();
        }

        private void MenuForm_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyData == Keys.Escape)
            {
                form.Close();
            }
        }

        private void MenuForm_LocationChanged(object sender, EventArgs e)
        {
            CheckMenuPosition();
        }

        private void TsMain_MouseLeave(object sender, EventArgs e)
        {
            MenuTextAnimation.Stop();
        }

        private void TslDrag_MouseEnter(object sender, EventArgs e)
        {
            menuForm.Cursor = Cursors.SizeAll;
        }

        private void TslDrag_MouseLeave(object sender, EventArgs e)
        {
            menuForm.Cursor = Cursors.Default;
        }

        private void TslDrag_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                NativeMethods.ReleaseCapture();
                NativeMethods.DefWindowProc(menuForm.Handle, (uint)WindowsMessages.SYSCOMMAND, (UIntPtr)NativeConstants.MOUSE_MOVE, IntPtr.Zero);
            }
            else if (e.Button == MouseButtons.Right)
            {
                SetMenuCollapsed(!IsMenuCollapsed);
                CheckMenuPosition();
            }
        }

        private void ConfigureMenuState()
        {
            if (Config.RememberMenuState)
            {
                SetMenuCollapsed(Config.MenuCollapsed);
            }

            Rectangle rectScreen = CaptureHelpers.GetScreenBounds();

            if (Config.RememberMenuState && rectScreen.Contains(Config.MenuPosition))
            {
                menuForm.Location = Config.MenuPosition;
            }
            else
            {
                Rectangle rectActiveScreen = CaptureHelpers.GetActiveScreenBounds();

                if (tsMain.Width < rectActiveScreen.Width)
                {
                    menuForm.Location = new Point(rectActiveScreen.X + rectActiveScreen.Width / 2 - tsMain.Width / 2, rectActiveScreen.Y + 20);
                }
                else
                {
                    menuForm.Location = rectActiveScreen.Location;
                }
            }
        }

        private void CheckMenuPosition()
        {
            Rectangle rectMenu = menuForm.Bounds;
            Rectangle rectScreen = CaptureHelpers.GetScreenBounds();
            Point pos = rectMenu.Location;

            if (rectMenu.Width < rectScreen.Width)
            {
                if (rectMenu.X < rectScreen.X)
                {
                    pos.X = rectScreen.X;
                }
                else if (rectMenu.Right > rectScreen.Right)
                {
                    pos.X = rectScreen.Right - rectMenu.Width;
                }
            }

            if (rectMenu.Height < rectScreen.Height)
            {
                if (rectMenu.Y < rectScreen.Y)
                {
                    pos.Y = rectScreen.Y;
                }
                else if (rectMenu.Bottom > rectScreen.Bottom)
                {
                    pos.Y = rectScreen.Bottom - rectMenu.Height;
                }
            }

            if (pos != rectMenu.Location)
            {
                menuForm.Location = pos;
            }

            if (Config.RememberMenuState)
            {
                Config.MenuPosition = pos;
            }
        }

        private void SetMenuCollapsed(bool isCollapsed)
        {
            if (IsMenuCollapsed == isCollapsed)
            {
                return;
            }

            IsMenuCollapsed = isCollapsed;

            if (IsMenuCollapsed)
            {
                foreach (ToolStripItem tsi in tsMain.Items.OfType<ToolStripItem>())
                {
                    if (tsi == tslDragLeft)
                    {
                        continue;
                    }

                    tsi.Visible = false;
                }
            }
            else
            {
                foreach (ToolStripItem tsi in tsMain.Items.OfType<ToolStripItem>())
                {
                    tsi.Visible = true;
                }

                UpdateMenu();
            }

            if (Config.RememberMenuState)
            {
                Config.MenuCollapsed = IsMenuCollapsed;
            }
        }

        private void UpdateMenu()
        {
            if (menuForm == null) return;

            ShapeType shapeType = CurrentShapeType;

            foreach (ToolStripButton tsb in tsMain.Items.OfType<ToolStripButton>().Where(x => x.Tag is ShapeType))
            {
                if ((ShapeType)tsb.Tag == shapeType)
                {
                    tsb.RadioCheck();
                    break;
                }
            }

            Color borderColor;

            if (shapeType == ShapeType.DrawingText || shapeType == ShapeType.DrawingSpeechBalloon)
            {
                borderColor = AnnotationOptions.TextBorderColor;
            }
            else if (shapeType == ShapeType.DrawingStep)
            {
                borderColor = AnnotationOptions.StepBorderColor;
            }
            else
            {
                borderColor = AnnotationOptions.BorderColor;
            }

            if (tsbBorderColor.Image != null) tsbBorderColor.Image.Dispose();
            tsbBorderColor.Image = ImageHelpers.CreateColorPickerIcon(borderColor, new Rectangle(0, 0, 16, 16), 8);

            int borderSize;

            if (shapeType == ShapeType.DrawingText || shapeType == ShapeType.DrawingSpeechBalloon)
            {
                borderSize = AnnotationOptions.TextBorderSize;
            }
            else if (shapeType == ShapeType.DrawingStep)
            {
                borderSize = AnnotationOptions.StepBorderSize;
            }
            else
            {
                borderSize = AnnotationOptions.BorderSize;
            }

            tslnudBorderSize.Content.Value = borderSize;

            Color fillColor;

            if (shapeType == ShapeType.DrawingText || shapeType == ShapeType.DrawingSpeechBalloon)
            {
                fillColor = AnnotationOptions.TextFillColor;
            }
            else if (shapeType == ShapeType.DrawingStep)
            {
                fillColor = AnnotationOptions.StepFillColor;
            }
            else
            {
                fillColor = AnnotationOptions.FillColor;
            }

            if (tsbFillColor.Image != null) tsbFillColor.Image.Dispose();
            tsbFillColor.Image = ImageHelpers.CreateColorPickerIcon(fillColor, new Rectangle(0, 0, 16, 16));

            int cornerRadius = 0;

            if (shapeType == ShapeType.RegionRoundedRectangle || shapeType == ShapeType.DrawingRoundedRectangle)
            {
                cornerRadius = AnnotationOptions.RoundedRectangleRadius;
            }
            else if (shapeType == ShapeType.DrawingText)
            {
                cornerRadius = AnnotationOptions.TextCornerRadius;
            }

            tslnudCornerRadius.Content.Value = cornerRadius;

            tslnudBlurRadius.Content.Value = AnnotationOptions.BlurRadius;

            tslnudPixelateSize.Content.Value = AnnotationOptions.PixelateSize;

            if (tsbHighlightColor.Image != null) tsbHighlightColor.Image.Dispose();
            tsbHighlightColor.Image = ImageHelpers.CreateColorPickerIcon(AnnotationOptions.HighlightColor, new Rectangle(0, 0, 16, 16));

            switch (shapeType)
            {
                default:
                    tsddbShapeOptions.Visible = false;
                    break;
                case ShapeType.RegionRoundedRectangle:
                case ShapeType.DrawingRectangle:
                case ShapeType.DrawingRoundedRectangle:
                case ShapeType.DrawingEllipse:
                case ShapeType.DrawingFreehand:
                case ShapeType.DrawingLine:
                case ShapeType.DrawingArrow:
                case ShapeType.DrawingText:
                case ShapeType.DrawingSpeechBalloon:
                case ShapeType.DrawingStep:
                case ShapeType.EffectBlur:
                case ShapeType.EffectPixelate:
                    tsddbShapeOptions.Visible = true;
                    break;
            }

            tsbUndoObject.Enabled = tsbDeleteAll.Enabled = Shapes.Count > 0;

            switch (shapeType)
            {
                default:
                    tsbBorderColor.Visible = false;
                    tslnudBorderSize.Visible = false;
                    break;
                case ShapeType.DrawingRectangle:
                case ShapeType.DrawingRoundedRectangle:
                case ShapeType.DrawingEllipse:
                case ShapeType.DrawingFreehand:
                case ShapeType.DrawingLine:
                case ShapeType.DrawingArrow:
                case ShapeType.DrawingText:
                case ShapeType.DrawingSpeechBalloon:
                case ShapeType.DrawingStep:
                    tsbBorderColor.Visible = true;
                    tslnudBorderSize.Visible = true;
                    break;
            }

            switch (shapeType)
            {
                default:
                    tsbFillColor.Visible = false;
                    break;
                case ShapeType.DrawingRectangle:
                case ShapeType.DrawingRoundedRectangle:
                case ShapeType.DrawingEllipse:
                case ShapeType.DrawingText:
                case ShapeType.DrawingSpeechBalloon:
                case ShapeType.DrawingStep:
                    tsbFillColor.Visible = true;
                    break;
            }

            switch (shapeType)
            {
                default:
                    tslnudCornerRadius.Visible = false;
                    break;
                case ShapeType.RegionRoundedRectangle:
                case ShapeType.DrawingRoundedRectangle:
                case ShapeType.DrawingText:
                    tslnudCornerRadius.Visible = true;
                    break;
            }

            tslnudBlurRadius.Visible = shapeType == ShapeType.EffectBlur;
            tslnudPixelateSize.Visible = shapeType == ShapeType.EffectPixelate;
            tsbHighlightColor.Visible = shapeType == ShapeType.EffectHighlight;

            if (tsmiRegionCapture != null)
            {
                tsmiRegionCapture.Visible = !Config.QuickCrop && ValidRegions.Length > 0;
            }
        }
    }
}