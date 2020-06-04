﻿using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

using CompMs.Graphics.Core.Base;

namespace CompMs.Graphics.GraphAxis
{
    public class HorizontalColorAxisControl : ChartBaseControl
    {
        #region DependencyProperty
        public static readonly DependencyProperty LabelBrushesProperty = DependencyProperty.Register(
            nameof(LabelBrushes), typeof(IList<Brush>), typeof(HorizontalColorAxisControl),
            new PropertyMetadata(null)
            );

        public static readonly DependencyProperty IdentityPropertyNameProperty = DependencyProperty.Register(
            nameof(IdentityPropertyName), typeof(string), typeof(HorizontalColorAxisControl),
            new PropertyMetadata(null, OnIdentityPropertyNamePropertyChanged)
            );

        public static readonly DependencyProperty FocussedItemProperty = DependencyProperty.Register(
            nameof(FocussedItem), typeof(object), typeof(HorizontalColorAxisControl),
            new PropertyMetadata(default(object))
            );
        #endregion

        #region Property
        public IList<Brush> LabelBrushes
        {
            get => (IList<Brush>)GetValue(LabelBrushesProperty);
            set => SetValue(LabelBrushesProperty, value);
        }

        public string IdentityPropertyName
        {
            get => (string)GetValue(IdentityPropertyNameProperty);
            set => SetValue(IdentityPropertyNameProperty, value);
        }

        public object FocussedItem
        {
            get => (object)GetValue(FocussedItemProperty);
            set => SetValue(FocussedItemProperty, value);
        }
        #endregion

        #region field
        private PropertyInfo iPropertyReflection;
        #endregion

        public HorizontalColorAxisControl()
        {
            MouseMove += VisualFocusOnMouseOver;
        }

        protected override void Update()
        {
            if (HorizontalAxis == null) return;

            var memo = new Dictionary<object, int>();
            var id = 0;
            Func<object, object> toKey = null;
            Func<object, Brush> toBrush = null;

            visualChildren.Clear();
            foreach (var data in HorizontalAxis.GetLabelTicks())
            {
                if (data.TickType != TickType.LongTick) continue;

                if (IdentityPropertyName != null && iPropertyReflection == null)
                    iPropertyReflection = data.Source.GetType().GetProperty(IdentityPropertyName);

                if (toKey == null)
                {
                    if (iPropertyReflection == null)
                        toKey = o => o;
                    else
                        toKey = o => iPropertyReflection.GetValue(o);

                    if (!(toKey(data.Source) is Brush) && LabelBrushes == null)
                        return;
                }

                if (toBrush == null)
                    toBrush = o =>
                    {
                        var x = toKey(o);
                        if (x is Brush b) return b;
                        if (!memo.ContainsKey(x)) memo[x] = id++;
                        return LabelBrushes[memo[x] % LabelBrushes.Count];
                    };

                var xorigin = HorizontalAxis.ValueToRenderPosition(data.Center - data.Width / 2) * ActualWidth;
                var xwidth = (HorizontalAxis.ValueToRenderPosition(data.Width) - HorizontalAxis.ValueToRenderPosition(0)) * ActualWidth;

                var dv = new AnnotatedDrawingVisual(data.Source);
                dv.Clip = new RectangleGeometry(new Rect(RenderSize));
                var dc = dv.RenderOpen();
                dc.DrawRectangle(toBrush(data.Source), null, new Rect(xorigin, 0, xwidth, ActualHeight));
                dc.Close();
                visualChildren.Add(dv);
            }
        }

        #region Event handler
        static void OnIdentityPropertyNamePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is HorizontalColorAxisControl chart)
                chart.iPropertyReflection = null;
        }
        #endregion

        #region Mouse event
        void VisualFocusOnMouseOver(object sender, MouseEventArgs e)
        {
            var pt = e.GetPosition(this);

            VisualTreeHelper.HitTest(this,
                new HitTestFilterCallback(VisualHitTestFilter),
                new HitTestResultCallback(VisualFocusHitTest),
                new PointHitTestParameters(pt)
                );
        }

        HitTestFilterBehavior VisualHitTestFilter(DependencyObject d)
        {
            if (d is AnnotatedDrawingVisual)
                return HitTestFilterBehavior.Continue;
            return HitTestFilterBehavior.ContinueSkipSelf;
        }

        HitTestResultBehavior VisualFocusHitTest(HitTestResult result)
        {
            FocussedItem = ((AnnotatedDrawingVisual)result.VisualHit).Annotation;
            return HitTestResultBehavior.Stop;
        }

        protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters)
        {
            return new PointHitTestResult(this, hitTestParameters.HitPoint);
        }
        #endregion
    }
}